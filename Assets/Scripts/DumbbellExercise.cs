using UnityEngine;

[RequireComponent(typeof(OVRCameraRig))]
public class DumbbellExercise : MonoBehaviour
{
    [Header("Exercise Detection")]
    [SerializeField] private int   repsRequired      = 5;
    [SerializeField] private float upVelThreshold    = 0.4f;
    [SerializeField] private float downVelThreshold  = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    public int CurrentReps { get; private set; }

    private Transform _leftHand, _rightHand;
    private float _leftPrevY, _rightPrevY;

    private enum Phase { Neutral, MovingUp, MovingDown }
    private Phase _phase;

    private void Awake()
    {
        var rig = GetComponent<OVRCameraRig>();
        _leftHand  = rig.leftHandAnchor;
        _rightHand = rig.rightHandAnchor;
    }

    private void Start()
    {
        _leftPrevY  = _leftHand.position.y;
        _rightPrevY = _rightHand.position.y;
    }

    private void Update()
    {
        HandleDebugTrigger();

        float dt = Time.deltaTime;

        // always update prevY — prevents velocity spike if detection starts mid-frame
        float leftYVel  = dt > 0f ? (_leftHand.position.y  - _leftPrevY)  / dt : 0f;
        float rightYVel = dt > 0f ? (_rightHand.position.y - _rightPrevY) / dt : 0f;
        _leftPrevY  = _leftHand.position.y;
        _rightPrevY = _rightHand.position.y;

        float avgYVel = (leftYVel + rightYVel) * 0.5f;

        // dumbbell motion: both hands co-phase (same vertical direction)
        // count a rep each time hands complete a down-stroke then return up
        if (avgYVel > upVelThreshold)
        {
            if (_phase == Phase.MovingDown)
                CurrentReps++;
            _phase = Phase.MovingUp;
        }
        else if (avgYVel < -downVelThreshold)
        {
            _phase = Phase.MovingDown;
        }

        if (CurrentReps >= repsRequired)
        {
            BodyShapeManager.Instance?.AddWeight(-1);
            CurrentReps = 0;
            _phase = Phase.Neutral;
        }
    }

    private void HandleDebugTrigger()
    {
        if (!enableDebugTrigger) return;
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
            BodyShapeManager.Instance?.AddWeight(3);
#endif
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            BodyShapeManager.Instance?.AddWeight(3);
    }
}
