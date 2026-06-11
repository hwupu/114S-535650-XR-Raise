using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(OVRCameraRig))]
public class SwingLocomotion : MonoBehaviour
{
    [Header("Speed (Normal / Min Weight)")]
    [SerializeField] private float maxSpeed         = 2.0f;
    [SerializeField] private float deadzone         = 0.02f;
    [SerializeField] private float speedSmoothing   = 8f;
    [SerializeField] private float maxSwingVelocity = 5f;

    [Header("Speed (Max Weight / Slowest)")]
    [SerializeField] private float heavyMaxSpeed = 0.4f;
    [SerializeField] private float heavyDeadzone = 1.5f;

    [Header("Anti-phase")]
    [SerializeField, Range(0f, 1f)] private float antiphaseWeight = 0.6f;

    [Header("Gravity")]
    [SerializeField] private float gravityMultiplier = 2.0f;
    [SerializeField] private float stickToGroundForce = 0.5f;
    [SerializeField] private LayerMask groundLayer = ~0;

#if UNITY_EDITOR
    [Header("── Debug Info (read-only) ──")]
    [SerializeField] private float _dbgRuntimeDeadzone;
    [SerializeField] private float _dbgRuntimeMaxSpeed;
    [SerializeField] private float _dbgCurrentSpeed;
    [SerializeField] private float _dbgSwingFraction;
    [SerializeField] private bool  _dbgIsWalking;
#endif

    private CharacterController _cc;
    private Transform _leftHand, _rightHand, _centerEye;
    private Vector3 _leftPrevPos, _rightPrevPos;
    private Vector3 _prevRigPos;
    private float _currentSpeed, _verticalVelocity;

    private float _runtimeMaxSpeed;
    private float _runtimeDeadzone;

    public bool IsWalking => _currentSpeed > 0.05f;

    // right controller Button A (Button.One) toggles locomotion on/off
    private bool _locomotionEnabled = true;

    // ── Coyote time：CC.isGrounded 短暫 False 時仍視為接地 ──
    private float _groundedTimer = 0f;
    private const float GroundedGracePeriod = 0.15f;

    // ── Debug 用：追蹤 effectivelyGrounded 狀態變化 ──
    private bool  _prevGrounded = true;
    private float _debugLogTimer = 0f;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        var rig = GetComponent<OVRCameraRig>();
        _leftHand  = rig.leftHandAnchor;
        _rightHand = rig.rightHandAnchor;
        _centerEye = rig.centerEyeAnchor;

        _runtimeMaxSpeed = maxSpeed;
        _runtimeDeadzone = deadzone;
    }

    private IEnumerator Start()
    {
        _leftPrevPos  = _leftHand.position;
        _rightPrevPos = _rightHand.position;
        _prevRigPos   = transform.position;

        // 在 yield 之前初始化：確保第一個 FixedUpdate 就有接地狀態，不會誤觸重力累積
        _groundedTimer    = GroundedGracePeriod;
        _verticalVelocity = -stickToGroundForce;

        // 等一幀讓 OVR 完成初始化後再 snap，避免 OVR 覆蓋 snap 結果
        yield return null;

        // 場景載入時將 CC 貼地，消除啟動懸空下墜
        // 從腳底稍上方往下射（0.1f），避免打到天花板
        _cc.enabled = false;
        float ccBottomOffset = _cc.center.y - _cc.height / 2f;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f,
                            Vector3.down, out RaycastHit hit, 5f,
                            groundLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y - ccBottomOffset + _cc.skinWidth;
            transform.position = pos;
        }
        _cc.enabled = true;
        _verticalVelocity = -stickToGroundForce;
        _groundedTimer = GroundedGracePeriod;
    }

    // Called by GameProgressDoor after teleport to clear residual gravity velocity
    public void ResetVelocity()
    {
        _verticalVelocity = 0f;
        _currentSpeed = 0f;
    }

    // Called by BodyShapeManager whenever weight changes; t=0 lightest, t=1 heaviest
    public void SetWeightFactor(float t)
    {
        _runtimeDeadzone = Mathf.Lerp(deadzone, heavyDeadzone, t);
        _runtimeMaxSpeed = Mathf.Lerp(maxSpeed, heavyMaxSpeed, t);
        Debug.Log($"[Weight] t={t:F2} | deadzone={_runtimeDeadzone:F3} | maxSpeed={_runtimeMaxSpeed:F2}");
#if UNITY_EDITOR
        _dbgRuntimeDeadzone = _runtimeDeadzone;
        _dbgRuntimeMaxSpeed = _runtimeMaxSpeed;
#endif
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            _locomotionEnabled = !_locomotionEnabled;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        bool wasWalking = _currentSpeed > 0.05f;

        // subtract rig's own displacement so locomotion doesn't feed back into swing detection
        Vector3 rigDisplacement = transform.position - _prevRigPos;
        _prevRigPos = transform.position;

        // always update prev positions — prevents velocity spike when re-enabling locomotion
        Vector3 leftVel  = (_leftHand.position  - _leftPrevPos  - rigDisplacement) / dt;
        Vector3 rightVel = (_rightHand.position - _rightPrevPos - rigDisplacement) / dt;
        _leftPrevPos  = _leftHand.position;
        _rightPrevPos = _rightHand.position;

        float swingFraction = 0f;

        if (_locomotionEnabled)
        {
            Vector3 headFwd = _centerEye.forward;
            headFwd.y = 0f;
            if (headFwd.sqrMagnitude < 0.001f) headFwd = transform.forward;
            headFwd.Normalize();

            float leftZ  = Vector3.Dot(leftVel,  headFwd);
            float rightZ = Vector3.Dot(rightVel, headFwd);
            float leftAbs  = Mathf.Abs(leftZ);
            float rightAbs = Mathf.Abs(rightZ);

            // both hands must move — single-hand jitter or reach-out won't trigger locomotion
            bool bothHandsActive = leftAbs > _runtimeDeadzone * 0.5f && rightAbs > _runtimeDeadzone * 0.5f;
            float rawCombined = bothHandsActive ? leftAbs + rightAbs : 0f;

            float antiphaseScore = 0f;
            if (rawCombined > _runtimeDeadzone * 2f)
                antiphaseScore = Mathf.Clamp01(
                    -(leftZ * rightZ) / (leftAbs * rightAbs + 0.0001f));

            float effectiveCombined = rawCombined *
                Mathf.Lerp(1f, antiphaseScore, antiphaseWeight);

            if (effectiveCombined > _runtimeDeadzone)
                swingFraction = Mathf.Clamp01(
                    (effectiveCombined - _runtimeDeadzone) / (maxSwingVelocity - _runtimeDeadzone));

#if UNITY_EDITOR
            Debug.Log($"[Swing] rigΔ={rigDisplacement.magnitude:F4} leftZ={leftZ:F3} rightZ={rightZ:F3} combined={effectiveCombined:F3} fraction={swingFraction:F3} speed={_currentSpeed:F3}");
#endif

            _currentSpeed = Mathf.Lerp(_currentSpeed, swingFraction * _runtimeMaxSpeed,
                                       speedSmoothing * dt);
        }
        else
        {
            _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, speedSmoothing * dt);
        }

        bool isWalkingNow = _currentSpeed > 0.05f;
        if (isWalkingNow != wasWalking)
            Debug.Log($"[Locomotion] {(isWalkingNow ? "▶ START" : "■ STOP")} | speed={_currentSpeed:F2} | deadzone={_runtimeDeadzone:F3}");

#if UNITY_EDITOR
        _dbgSwingFraction = swingFraction;
        _dbgCurrentSpeed  = _currentSpeed;
        _dbgIsWalking     = isWalkingNow;
#endif

        // recalculate heading outside the branch — needed for CC.Move regardless of toggle state
        Vector3 headFwdFinal = _centerEye.forward;
        headFwdFinal.y = 0f;
        if (headFwdFinal.sqrMagnitude < 0.001f) headFwdFinal = transform.forward;
        headFwdFinal.Normalize();

        // Coyote time：CC.isGrounded 短暫 False（地形邊緣）時，保留 0.15s 接地狀態
        if (_cc.isGrounded)
            _groundedTimer = GroundedGracePeriod;
        else if (_groundedTimer > 0f)
            _groundedTimer -= dt;

        bool effectivelyGrounded = _groundedTimer > 0f;

        if (effectivelyGrounded)
            _verticalVelocity = -stickToGroundForce;
        else
            _verticalVelocity += Physics.gravity.y * gravityMultiplier * dt;

        _cc.Move(new Vector3(
            headFwdFinal.x * _currentSpeed,
            _verticalVelocity,
            headFwdFinal.z * _currentSpeed) * dt);

        // ── Debug：effectivelyGrounded 狀態改變時 log，每 2 秒也輸出一次垂直速度 ──
        if (effectivelyGrounded != _prevGrounded)
        {
            Debug.Log($"[Swing-診斷] effectivelyGrounded={effectivelyGrounded}  CC.isGrounded={_cc.isGrounded}  vertVel={_verticalVelocity:F3}  pos={transform.position}");
            _prevGrounded = effectivelyGrounded;
        }
        _debugLogTimer += dt;
        if (_debugLogTimer >= 2f)
        {
            _debugLogTimer = 0f;
            Debug.Log($"[Swing-診斷] effectivelyGrounded={effectivelyGrounded}  CC.isGrounded={_cc.isGrounded}  vertVel={_verticalVelocity:F3}  pos={transform.position}");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_leftHand == null || _rightHand == null) return;
        Gizmos.color = _locomotionEnabled ? Color.green : Color.gray;
        Gizmos.DrawLine(_leftHand.position,
                        _leftHand.position + Vector3.up * _currentSpeed * 0.3f);
        Gizmos.color = _locomotionEnabled ? Color.red : Color.gray;
        Gizmos.DrawLine(_rightHand.position,
                        _rightHand.position + Vector3.up * _currentSpeed * 0.3f);
    }
#endif
}
