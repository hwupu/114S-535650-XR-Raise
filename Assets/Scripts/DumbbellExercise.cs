using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DumbbellExercise : MonoBehaviour
{
    [Header("Exercise Detection")]
    [SerializeField] private int   repsRequired      = 5;
    [SerializeField] private float upVelThreshold    = 0.4f;
    [SerializeField] private float downVelThreshold  = 0.4f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   repSound;
    [SerializeField] private AudioClip   completionSound;

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    public int CurrentReps { get; private set; }

    private Rigidbody _rb;
    private bool IsHeld => _rb != null && _rb.isKinematic;

    private float _prevY;

    private enum Phase { Neutral, MovingUp, MovingDown }
    private Phase _phase;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _prevY = transform.position.y;
    }

    private void Update()
    {
        HandleDebugTrigger();

        if (!IsHeld)
        {
            _prevY  = transform.position.y;
            _phase  = Phase.Neutral;
            return;
        }

        float dt = Time.deltaTime;
        float yVel = dt > 0f ? (transform.position.y - _prevY) / dt : 0f;
        _prevY = transform.position.y;

        if (yVel > upVelThreshold)
        {
            if (_phase == Phase.MovingDown)
            {
                CurrentReps++;
                if (audioSource != null && repSound != null)
                    audioSource.PlayOneShot(repSound);
            }
            _phase = Phase.MovingUp;
        }
        else if (yVel < -downVelThreshold)
        {
            _phase = Phase.MovingDown;
        }

        if (CurrentReps >= repsRequired)
        {
            if (BodyShapeManager.Instance != null)
                BodyShapeManager.Instance.AddWeight(-1);
            if (audioSource != null && completionSound != null)
                audioSource.PlayOneShot(completionSound);
            CurrentReps = 0;
            _phase = Phase.Neutral;
        }
    }

    private void HandleDebugTrigger()
    {
        if (!enableDebugTrigger) return;
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
            if (BodyShapeManager.Instance != null) BodyShapeManager.Instance.AddWeight(3);
#endif
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            if (BodyShapeManager.Instance != null) BodyShapeManager.Instance.AddWeight(3);
    }
}
