using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EatableSnack : MonoBehaviour
{
    [SerializeField] private float eatDistance = 0.25f;

    private Rigidbody _rb;
    private Transform _head;

    private void Awake() => _rb = GetComponent<Rigidbody>();

    private void Start()
    {
        var rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null) _head = rig.centerEyeAnchor;
    }

    private void Update()
    {
        // isKinematic becomes true when OVRGrabbable picks up the object
        if (_head == null || !_rb.isKinematic) return;
        if (Vector3.Distance(transform.position, _head.position) < eatDistance)
            Eat();
    }

    private void Eat()
    {
        BodyShapeManager.Instance?.AddWeight(3);
        Destroy(gameObject);
    }
}
