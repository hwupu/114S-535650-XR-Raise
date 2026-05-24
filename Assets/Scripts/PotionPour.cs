using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PotionPour : MonoBehaviour
{
    // OVRGrabbable sets the Rigidbody to isKinematic=true when the object is held.
    // Pouring is detected when held AND tilted upside-down (transform.up.y < tiltThreshold).
    [SerializeField] private float tiltThreshold = -0.5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    private Rigidbody _rb;
    private bool _forcePoured;

    public bool IsHeld    => _rb != null && _rb.isKinematic;
    public bool IsPoured  => _forcePoured || (IsHeld && transform.up.y < tiltThreshold);

    private void Awake() => _rb = GetComponent<Rigidbody>();

    private void Update()
    {
        if (enableDebugTrigger && Input.GetKeyDown(KeyCode.P))
        {
            _forcePoured = !_forcePoured;
            Debug.Log($"[PotionPour] Debug force pour toggled: {_forcePoured}");
        }
    }
}
