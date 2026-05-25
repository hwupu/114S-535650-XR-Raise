using System.Collections;
using UnityEngine;

// Attach to BOTH admission letter prefab instances (CS and Music).
// Set 'department' on each and cross-assign 'otherLetter'.
// Grab detection mirrors EatableSnack: OVRGrabbable sets Rigidbody.isKinematic = true.
[RequireComponent(typeof(Rigidbody))]
public class AdmissionLetterChoice : MonoBehaviour
{
    public enum DepartmentType { CS, Music }

    [Header("Setup")]
    [SerializeField] private DepartmentType department;
    [SerializeField] private AdmissionLetterChoice otherLetter;
    [SerializeField] private AudioClip choiceAudioClip;
    [SerializeField] private float glowIntensity = 3f;
    [SerializeField] private float floatDuration  = 2f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    private Rigidbody       _rb;
    private SpriteRenderer  _sprite;
    private bool            _chosen;

    // Static flag shared by both letter instances so only one choice fires
    private static bool _eventDone;

    private void Awake()
    {
        _rb     = GetComponent<Rigidbody>();
        _sprite = GetComponentInChildren<SpriteRenderer>();
    }

    // Reset static flag when scene loads (prevents stale state in Editor)
    private void OnEnable() => _eventDone = false;

    private void Update()
    {
        if (_chosen || _eventDone) return;

        if (enableDebugTrigger)
        {
            if (Input.GetKeyDown(KeyCode.C) && department == DepartmentType.CS)   ForceChoose();
            if (Input.GetKeyDown(KeyCode.M) && department == DepartmentType.Music) ForceChoose();
        }

        // OVRGrabbable sets isKinematic true when held
        if (_rb.isKinematic) Choose();
    }

    private void ForceChoose()
    {
        if (_chosen || _eventDone) return;
        Choose();
    }

    private void Choose()
    {
        if (_chosen || _eventDone) return;
        _chosen    = true;
        _eventDone = true;

        // Glow: tint sprite to a bright yellow-white color
        if (_sprite != null)
            _sprite.color = new Color(glowIntensity, glowIntensity * 0.9f, 0.4f, 1f);

        // Play corresponding sound
        if (choiceAudioClip != null)
            AudioSource.PlayClipAtPoint(choiceAudioClip, transform.position);

        // Dim and float away the other letter
        if (otherLetter != null)
            otherLetter.StartCoroutine(otherLetter.FloatAway());

        bool isCS = department == DepartmentType.CS;
        Debug.Log($"[AdmissionLetterChoice] Chose: {department}");
        GameManager.Instance?.RecordDepartmentChoice(isCS);
    }

    // Called on the letter that was NOT chosen — floats up and fades out
    public IEnumerator FloatAway()
    {
        float elapsed    = 0f;
        Vector3 startPos = transform.position;
        Color   startCol = _sprite != null ? _sprite.color : Color.white;

        while (elapsed < floatDuration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / floatDuration;

            transform.position = startPos + Vector3.up * (t * 1.5f);

            if (_sprite != null)
            {
                Color c = startCol;
                c.a = Mathf.Lerp(1f, 0f, t);
                _sprite.color = c;
            }

            yield return null;
        }

        gameObject.SetActive(false);
    }
}
