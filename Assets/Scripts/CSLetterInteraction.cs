using System.Collections;
using UnityEngine;

// Attach to the CS admission letter prefab INSTEAD OF AdmissionLetterChoice.
// The Music letter keeps AdmissionLetterChoice unchanged.
//
// State flow:
//   Idle → (grab attempt) → ChallengeActive → (type "world" + Enter) → Solved → (grab) → Chosen
//
// Grab detection mirrors the project pattern: OVRGrabbable sets Rigidbody.isKinematic = true.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VRKeyboard))]
public class CSLetterInteraction : MonoBehaviour
{
    private enum State { Idle, ChallengeActive, Solved, Chosen }

    [Header("References")]
    [SerializeField] private AdmissionLetterChoice musicLetter;

    [Header("Escape Settings")]
    [SerializeField] private float escapeRadiusMin = 1.0f;
    [SerializeField] private float escapeRadiusMax = 2.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip escapeSound;
    [SerializeField] private AudioClip solveSound;

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    private State          _state = State.Idle;
    private Rigidbody      _rb;
    private SpriteRenderer _sprite;
    private Color          _originalColor;
    private VRKeyboard     _keyboard;
    private OVRCameraRig   _rig;
    private string         _typedText = "";

    // ── Unity ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb      = GetComponent<Rigidbody>();
        _sprite  = GetComponentInChildren<SpriteRenderer>();
        _keyboard = GetComponent<VRKeyboard>();

        if (_sprite != null) _originalColor = _sprite.color;
    }

    private void Start()
    {
        _rig = FindObjectOfType<OVRCameraRig>();

        _keyboard.OnKeyTyped       += OnKeyTyped;
        _keyboard.OnEnterPressed   += OnEnterPressed;
        _keyboard.OnBackspacePressed += OnBackspacePressed;

        _keyboard.Hide();
    }

    private void OnDestroy()
    {
        if (_keyboard == null) return;
        _keyboard.OnKeyTyped         -= OnKeyTyped;
        _keyboard.OnEnterPressed     -= OnEnterPressed;
        _keyboard.OnBackspacePressed -= OnBackspacePressed;
    }

    private void Update()
    {
        if (_state == State.Chosen) return;

        DebugInput();

        switch (_state)
        {
            case State.Idle:
                if (_rb.isKinematic)
                {
                    ForceRelease();
                    StartCoroutine(FlashCoroutine(Color.white, 0.15f));
                    EscapeToRandomPosition();
                    PlaySound(escapeSound);
                    ShowKeyboard();
                    _state = State.ChallengeActive;
                    Debug.Log($"[CSLetter] Idle → ChallengeActive. 通知書逃到 {transform.position}");
                }
                break;

            case State.ChallengeActive:
                // block every grab attempt until puzzle is solved
                if (_rb.isKinematic)
                {
                    ForceRelease();
                    StartCoroutine(FlashCoroutine(Color.red, 0.2f));
                    EscapeToRandomPosition();
                    PlaySound(escapeSound);
                    _keyboard.SetStatusText("Grab blocked! Type 'world' first.", Color.red);
                    Debug.Log($"[CSLetter] 抓取被阻擋！通知書逃到 {transform.position}");
                }
                break;

            case State.Solved:
                if (_rb.isKinematic)
                    Choose();
                break;
        }
    }

    // ── keyboard callbacks ──────────────────────────────────────────────────

    private void OnKeyTyped(char c)
    {
        if (_state != State.ChallengeActive) return;
        _typedText += char.ToLower(c);
        _keyboard.SetTypedText(_typedText, Color.white);
        _keyboard.SetStatusText("Press ENTER to confirm.", new Color(0.8f, 0.8f, 0.8f));
        Debug.Log($"[CSLetter] 輸入中: '{_typedText}'");
    }

    private void OnEnterPressed()
    {
        if (_state != State.ChallengeActive) return;

        if (_typedText.ToLower() == "world")
        {
            _state = State.Solved;
            _keyboard.SetTypedText(_typedText, Color.green);
            _keyboard.SetStatusText("Correct! Now grab the letter.", Color.green);
            StartCoroutine(FlashCoroutine(new Color(0.3f, 1f, 0.3f), 0.4f));
            PlaySound(solveSound);
            Debug.Log("[CSLetter] ✓ 正確！ChallengeActive → Solved。可以抓取通知書。");
        }
        else
        {
            // wrong answer — flash, escape, reset
            _keyboard.SetTypedText(_typedText, Color.red);
            _keyboard.SetStatusText("Wrong! Hint: printf(\"hello \") ______", Color.red);
            StartCoroutine(FlashCoroutine(Color.red, 0.2f));
            EscapeToRandomPosition();
            PlaySound(escapeSound);
            Debug.Log($"[CSLetter] ✗ 答錯: '{_typedText}'。通知書逃到 {transform.position}。清除輸入。");
            _typedText = "";

            // delay clearing typed display so player can read the red text
            StartCoroutine(ClearTypedAfterDelay(1.0f));
        }
    }

    private void OnBackspacePressed()
    {
        if (_state != State.ChallengeActive || _typedText.Length == 0) return;
        _typedText = _typedText.Substring(0, _typedText.Length - 1);
        _keyboard.SetTypedText(_typedText, Color.white);
        Debug.Log($"[CSLetter] Backspace，目前: '{_typedText}'");
    }

    // ── choose ──────────────────────────────────────────────────────────────

    private void Choose()
    {
        _state = State.Chosen;
        AdmissionLetterChoice.MarkEventDone();
        musicLetter?.StartCoroutine(musicLetter.FloatAway());
        GameManager.Instance?.RecordDepartmentChoice(true);
        _keyboard.Hide();
        if (ForestManage.Instance != null) ForestManage.Instance.OnMakeParentChoice();
        Debug.Log("[CSLetter] ★ CS 系選定（安全選擇）→ ForestManage.OnMakeParentChoice()");
    }

    // ── public float-away (called by MusicalLetterInteraction when Music is chosen) ──

    public IEnumerator FloatAway()
    {
        _state = State.Chosen;
        if (_keyboard != null) _keyboard.Hide();
        float elapsed  = 0f;
        Vector3 startPos = transform.position;
        Color   startCol = _sprite != null ? _sprite.color : Color.white;

        while (elapsed < 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 2f;
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

    // ── helpers ─────────────────────────────────────────────────────────────

    private void ForceRelease()
    {
        OVRGrabbable grabbable = GetComponent<OVRGrabbable>();
        if (grabbable != null && grabbable.isGrabbed)
            grabbable.grabbedBy.ForceRelease(grabbable);

        _rb.isKinematic    = false;
        _rb.velocity        = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    private void EscapeToRandomPosition()
    {
        float y     = transform.position.y;
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist  = UnityEngine.Random.Range(escapeRadiusMin, escapeRadiusMax);

        Vector3 offset = new Vector3(Mathf.Sin(angle) * dist, 0f, Mathf.Cos(angle) * dist);
        transform.position = new Vector3(
            transform.position.x + offset.x,
            y,
            transform.position.z + offset.z
        );
    }

    private void ShowKeyboard()
    {
        Transform head = _rig   != null ? _rig.centerEyeAnchor
                       : Camera.main != null ? Camera.main.transform
                       : null;
        if (head == null) return;

        Vector3 pos    = head.position + head.forward * 1.5f;
        pos.y          = head.position.y - 0.1f;
        Quaternion rot = Quaternion.LookRotation(pos - head.position);

        _typedText = "";
        _keyboard.SetTypedText("", Color.white);
        _keyboard.SetStatusText("Spell 'world' to unlock the letter.", new Color(0.8f, 0.8f, 0.8f));
        _keyboard.Show(pos, rot);
        Debug.Log($"[CSLetter] 鍵盤出現在 pos={pos}  euler={rot.eulerAngles}");
    }

    private IEnumerator FlashCoroutine(Color flashColor, float duration)
    {
        if (_sprite == null) yield break;
        _sprite.color = flashColor;
        yield return new WaitForSeconds(duration);
        if (_sprite != null) _sprite.color = _originalColor;
    }

    private IEnumerator ClearTypedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _typedText = "";
        if (_state == State.ChallengeActive)
            _keyboard.SetTypedText("", Color.white);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    // ── debug ────────────────────────────────────────────────────────────────

    private void DebugInput()
    {
        if (!enableDebugTrigger) return;

        // C = force-solve (skip typing, go to Solved)
        if (Input.GetKeyDown(KeyCode.C) && _state == State.ChallengeActive)
        {
            _typedText = "world";
            _keyboard.SetTypedText(_typedText, Color.green);
            _keyboard.SetStatusText("(Debug) Correct! Now grab the letter.", Color.green);
            _state = State.Solved;
        }

        // K = show keyboard (for testing without grabbing)
        if (Input.GetKeyDown(KeyCode.K) && _state == State.Idle)
        {
            ShowKeyboard();
            _state = State.ChallengeActive;
        }

        // G = simulate grab (Idle → ChallengeActive, or Solved → Chosen)
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (_state == State.Idle)
            {
                StartCoroutine(FlashCoroutine(Color.white, 0.15f));
                EscapeToRandomPosition();
                ShowKeyboard();
                _state = State.ChallengeActive;
                Debug.Log($"[CSLetter] [G鍵] 模擬抓取 → ChallengeActive。通知書逃到 {transform.position}");
            }
            else if (_state == State.Solved)
            {
                Debug.Log("[CSLetter] [G鍵] 模擬最終抓取 → Choose()");
                Choose();
            }
        }
    }
}
