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
    [SerializeField] private float heavyMaxSpeed = 0.6f;
    [SerializeField] private float heavyDeadzone = 0.5f;

    [Header("Anti-phase")]
    [SerializeField, Range(0f, 1f)] private float antiphaseWeight = 0.6f;

    [Header("Gravity")]
    [SerializeField] private float gravityMultiplier = 2.0f;
    [SerializeField] private float stickToGroundForce = 10f;

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

    // right controller Button A (Button.One) toggles locomotion on/off
    private bool _locomotionEnabled = true;

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

    private void Start()
    {
        _leftPrevPos  = _leftHand.position;
        _rightPrevPos = _rightHand.position;
        _prevRigPos   = transform.position;
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

        _verticalVelocity = _cc.isGrounded
            ? -stickToGroundForce
            : _verticalVelocity + Physics.gravity.y * gravityMultiplier * dt;

        _cc.Move(new Vector3(
            headFwdFinal.x * _currentSpeed,
            _verticalVelocity,
            headFwdFinal.z * _currentSpeed) * dt);
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
