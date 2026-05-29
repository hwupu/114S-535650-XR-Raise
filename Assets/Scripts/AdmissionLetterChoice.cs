using System.Collections;
using UnityEngine;

// Attach to BOTH admission letter prefab instances (CS and Music).
// Set 'department' on each and cross-assign 'otherLetter'.
//
// CS letter: OVRGrabbable disabled at start. When controller enters the trigger
// zone (Box Collider IsTrigger=true), keyboard appears and letter freezes in place.
// After player types "world" → grab re-enabled → player picks up letter → Choose() fires.
//
// Music letter: normal grab flow (OVRGrabbable always enabled).
[RequireComponent(typeof(Rigidbody))]
public class AdmissionLetterChoice : MonoBehaviour
{
    public enum DepartmentType { CS, Music }

    [Header("Setup")]
    [SerializeField] private DepartmentType department;
    [SerializeField] private AdmissionLetterChoice otherLetter;
    [SerializeField] private AudioClip choiceAudioClip;
    [SerializeField] private float glowIntensity = 3f;
    [SerializeField] private float floatDuration = 2f;

    [Header("CS Puzzle")]
    [SerializeField] private GameObject keyboardCanvasPrefab;

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    private static readonly WaitForSeconds WaitFlashOn  = new(0.15f);
    private static readonly WaitForSeconds WaitFlashOff = new(0.1f);

    private Rigidbody         _rb;
    private SpriteRenderer    _sprite;
    private OVRGrabbable      _ovrGrabbable;    // cached to avoid repeated GetComponent
    private bool              _chosen;
    private bool              _puzzleSolved;
    private bool              _frozenByPuzzle;  // true = we froze it, not a grab
    private GameObject        _keyboardInstance;
    private VirtualKeyboardUI _keyboard;

    // Static flag shared by both letter instances so only one choice fires.
    private static bool _eventDone;

    private void Awake()
    {
        _rb           = GetComponent<Rigidbody>();
        _sprite       = GetComponentInChildren<SpriteRenderer>();
        _ovrGrabbable = GetComponent<OVRGrabbable>();

        // Prevent CS letter from being grabbed before puzzle is solved.
        if (department == DepartmentType.CS)
            SetGrabEnabled(false);
    }

    // Reset static flag when scene loads (prevents stale state in Editor).
    private void OnEnable() => _eventDone = false;

    private void Update()
    {
        if (_chosen || _eventDone) return;

        if (enableDebugTrigger)
        {
            if (Input.GetKeyDown(KeyCode.C) && department == DepartmentType.CS)    ForceChoose();
            if (Input.GetKeyDown(KeyCode.M) && department == DepartmentType.Music) ForceChoose();
        }

        // OVRGrabbable sets isKinematic=true when held.
        // Only treat it as a real grab when we haven't self-frozen the letter.
        if (_rb.isKinematic && !_frozenByPuzzle)
            Choose();
    }

    // Controller enters the letter's trigger zone (Box Collider IsTrigger=true).
    // For CS: freeze and show keyboard. Tag your hand/controller objects as "Hand".
    private void OnTriggerEnter(Collider other)
    {
        if (department != DepartmentType.CS || _puzzleSolved || _chosen || _eventDone) return;
        if (other.CompareTag("Hand") || other.CompareTag("Player"))
        {
            if (_keyboardInstance == null || !_keyboardInstance.activeSelf)
                FreezeAndShowKeyboard();
        }
    }

    private void ForceChoose()
    {
        if (_chosen || _eventDone) return;
        _puzzleSolved    = true;
        _frozenByPuzzle  = false;
        SetGrabEnabled(true);
        Choose();
    }

    // Freeze the letter in place and show the keyboard — no flying away.
    private void FreezeAndShowKeyboard()
    {
        _frozenByPuzzle          = true;
        _rb.velocity             = Vector3.zero;
        _rb.angularVelocity      = Vector3.zero;
        _rb.useGravity           = false;
        _rb.isKinematic          = true;
        ShowKeyboard();
    }

    private void ShowKeyboard()
    {
        if (Camera.main == null) return;
        Transform cam = Camera.main.transform;

        Vector3    pos = cam.position + cam.forward * 0.7f + Vector3.down * 0.1f;
        Quaternion rot = Quaternion.LookRotation(cam.forward);

        if (_keyboardInstance == null && keyboardCanvasPrefab != null)
        {
            _keyboardInstance = Instantiate(keyboardCanvasPrefab, pos, rot);
            _keyboard = _keyboardInstance.GetComponent<VirtualKeyboardUI>();
        }
        else if (_keyboardInstance != null)
        {
            _keyboardInstance.transform.SetPositionAndRotation(pos, rot);
            _keyboardInstance.SetActive(true);
        }

        if (_keyboard != null) _keyboard.Init(OnKeyboardSubmit);
    }

    private void OnKeyboardSubmit(string word)
    {
        if (word.ToLower() == "world")
        {
            _puzzleSolved   = true;
            _frozenByPuzzle = false;
            _rb.isKinematic = false;
            _rb.useGravity  = false;   // keep floating so player can grab easily
            _rb.velocity    = Vector3.zero;
            SetGrabEnabled(true);
            if (_keyboardInstance != null) _keyboardInstance.SetActive(false);
            StartCoroutine(FlashGreen());
            Debug.Log("[CSPuzzle] Solved! Grab the letter to confirm your choice.");
        }
        else
        {
            if (_keyboard != null) _keyboard.ShowError($"\"{word}\" is wrong — type: world");
        }
    }

    // Enables or disables the grab component so controller cannot pick up the letter.
    private void SetGrabEnabled(bool enabled)
    {
        if (_ovrGrabbable != null) _ovrGrabbable.enabled = enabled;

        // If using newer Meta XR Interaction SDK (Oculus.Interaction), uncomment:
        // var grabInteractable = GetComponent<Oculus.Interaction.GrabInteractable>();
        // if (grabInteractable != null) grabInteractable.enabled = enabled;
    }

    private void Choose()
    {
        if (_chosen || _eventDone) return;
        _chosen    = true;
        _eventDone = true;

        // Glow: tint sprite to bright yellow-white
        if (_sprite != null)
            _sprite.color = new Color(glowIntensity, glowIntensity * 0.9f, 0.4f, 1f);

        if (choiceAudioClip != null)
            AudioSource.PlayClipAtPoint(choiceAudioClip, transform.position);

        if (otherLetter != null)
            otherLetter.StartCoroutine(otherLetter.FloatAway());

        bool isCS = department == DepartmentType.CS;
        Debug.Log($"[AdmissionLetterChoice] Chose: {department}");
        if (GameManager.Instance != null) GameManager.Instance.RecordDepartmentChoice(isCS);
    }

    // Called on the letter that was NOT chosen — floats up and fades out.
    public IEnumerator FloatAway()
    {
        float   elapsed  = 0f;
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

    // Green blink when the puzzle is solved successfully.
    private IEnumerator FlashGreen()
    {
        if (_sprite == null) yield break;
        for (int i = 0; i < 3; i++)
        {
            _sprite.color = new Color(0.2f, 1f, 0.3f, 1f);
            yield return WaitFlashOn;
            _sprite.color = Color.white;
            yield return WaitFlashOff;
        }
    }
}
