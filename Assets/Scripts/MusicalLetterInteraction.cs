using System.Collections;
using UnityEngine;

// Attach to the Music admission letter prefab INSTEAD OF AdmissionLetterChoice.
// The CS letter keeps CSLetterInteraction.
//
// State flow:
//   Idle → (grab) → ChallengeActive → (type "do re mi" + Enter) → Solved → (grab) → Chosen
//
// Target phrase: "do re mi" (spaces required; normalised before comparison).
// Sound effect plays TWICE on first escape.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VRKeyboard))]
public class MusicalLetterInteraction : MonoBehaviour
{
    private enum State { Idle, ChallengeActive, Solved, Chosen }

    private const string TargetPhrase   = "do re mi";
    // ♩♪♫ require a font supporting U+2669-U+266B (e.g. NotoSans).
    // Assign VRKeyboard.overrideFont in Inspector to use Unicode symbols.
    // Default: ASCII fallback so any TMP font works out of the box.
    private const string TerminalHeader = "♩ Do  ♪ Re  ♫ Mi";

    [Header("References")]
    [SerializeField] private CSLetterInteraction csLetter;

    [Header("Escape Settings")]
    [SerializeField] private float escapeRadiusMin = 1.0f;
    [SerializeField] private float escapeRadiusMax = 2.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip musicSound;   // played twice on first grab
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
        _rb       = GetComponent<Rigidbody>();
        _sprite   = GetComponentInChildren<SpriteRenderer>();
        _keyboard = GetComponent<VRKeyboard>();

        if (_sprite != null) _originalColor = _sprite.color;
    }

    private void Start()
    {
        _rig = FindObjectOfType<OVRCameraRig>();

        _keyboard.OnKeyTyped         += OnKeyTyped;
        _keyboard.OnEnterPressed     += OnEnterPressed;
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
                    StartCoroutine(PlayTwice(musicSound));
                    ShowKeyboard();
                    _state = State.ChallengeActive;
                    Debug.Log($"[Musical] Idle → ChallengeActive. 通知書逃到 {transform.position}");
                }
                break;

            case State.ChallengeActive:
                if (_rb.isKinematic)
                {
                    ForceRelease();
                    StartCoroutine(FlashCoroutine(Color.red, 0.2f));
                    EscapeToRandomPosition();
                    StartCoroutine(PlayTwice(musicSound));
                    _keyboard.SetStatusText("Grab blocked! Type 'do re mi' first.", Color.red);
                    Debug.Log($"[Musical] 抓取被阻擋！通知書逃到 {transform.position}");
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
        // preserve spaces; lowercase letters
        _typedText += (c == ' ') ? ' ' : char.ToLower(c);
        _keyboard.SetTypedText(_typedText, Color.white);
        _keyboard.SetStatusText("Press ENTER to confirm.", new Color(0.8f, 0.8f, 0.8f));
        Debug.Log($"[Musical] 輸入中: '{_typedText}'");
    }

    private void OnEnterPressed()
    {
        if (_state != State.ChallengeActive) return;

        string normalised = string.Join(" ",
            _typedText.Trim().ToLower()
                      .Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));

        if (normalised == TargetPhrase)
        {
            _state = State.Solved;
            _keyboard.SetTypedText(_typedText, Color.green);
            _keyboard.SetStatusText("Correct! Now grab the letter.", Color.green);
            StartCoroutine(FlashCoroutine(new Color(0.3f, 1f, 0.3f), 0.4f));
            PlaySound(solveSound);
            Debug.Log("[Musical] ✓ 正確！ChallengeActive → Solved。可以抓取通知書。");
        }
        else
        {
            _keyboard.SetTypedText(_typedText, Color.red);
            _keyboard.SetStatusText("Wrong! Hint: ♩ do  ♪ re  ♫ mi", Color.red);
            StartCoroutine(FlashCoroutine(Color.red, 0.2f));
            EscapeToRandomPosition();
            StartCoroutine(PlayTwice(musicSound));
            Debug.Log($"[Musical] ✗ 答錯: '{_typedText}'（normalised='{normalised}'）。通知書逃到 {transform.position}。");
            _typedText = "";
            StartCoroutine(ClearTypedAfterDelay(1.0f));
        }
    }

    private void OnBackspacePressed()
    {
        if (_state != State.ChallengeActive || _typedText.Length == 0) return;
        _typedText = _typedText.Substring(0, _typedText.Length - 1);
        _keyboard.SetTypedText(_typedText, Color.white);
        Debug.Log($"[Musical] Backspace，目前: '{_typedText}'");
    }

    // ── choose ──────────────────────────────────────────────────────────────

    private void Choose()
    {
        _state = State.Chosen;
        AdmissionLetterChoice.MarkEventDone();
        if (csLetter != null) csLetter.StartCoroutine(csLetter.FloatAway());
        GameManager.Instance?.RecordDepartmentChoice(false);
        _keyboard.Hide();
        if (ForestManage.Instance != null) ForestManage.Instance.OnMakeSelfChoice();
        Debug.Log("[Musical] ★ 音樂系選定（危險選擇）→ ForestManage.OnMakeSelfChoice()");
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
        Transform head = _rig     != null ? _rig.centerEyeAnchor
                       : Camera.main != null ? Camera.main.transform
                       : null;
        if (head == null) return;

        Vector3 pos    = head.position + head.forward * 1.5f;
        pos.y          = head.position.y - 0.1f;
        Quaternion rot = Quaternion.LookRotation(pos - head.position);

        _typedText = "";
        _keyboard.SetTerminalText(TerminalHeader, new Color(1f, 0.85f, 0.2f));
        _keyboard.SetTypedText("", Color.white);
        _keyboard.SetStatusText("Spell 'do re mi' to unlock the letter.", new Color(0.8f, 0.8f, 0.8f));
        _keyboard.Show(pos, rot);
        Debug.Log($"[Musical] 鍵盤出現在 pos={pos}  euler={rot.eulerAngles}");
    }

    private IEnumerator PlayTwice(AudioClip clip)
    {
        if (clip == null) yield break;
        AudioSource.PlayClipAtPoint(clip, transform.position);
        yield return new WaitForSeconds(clip.length + 0.3f);
        AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position);
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

    // ── debug ────────────────────────────────────────────────────────────────

    private void DebugInput()
    {
        if (!enableDebugTrigger) return;

        // K = show keyboard (Idle only)
        if (Input.GetKeyDown(KeyCode.K) && _state == State.Idle)
        {
            ShowKeyboard();
            _state = State.ChallengeActive;
            Debug.Log("[Musical] [K鍵] 顯示鍵盤 → ChallengeActive");
        }

        // D from Idle    = show keyboard only (let player type)
        // D from Active  = force-solve (skip typing shortcut)
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (_state == State.Idle)
            {
                ShowKeyboard();
                _state = State.ChallengeActive;
                Debug.Log("[Musical] [D鍵] 顯示鍵盤 → ChallengeActive（自行打字）");
            }
            else if (_state == State.ChallengeActive)
            {
                _typedText = TargetPhrase;
                _keyboard.SetTypedText(_typedText, Color.green);
                _keyboard.SetStatusText("(Debug) Correct! Now grab the letter.", Color.green);
                _state = State.Solved;
                Debug.Log("[Musical] [D鍵] 強制解謎 → Solved");
            }
        }

        // J = simulate grab（Musical 專用，避免與 CS 的 G 鍵衝突）
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (_state == State.Idle)
            {
                StartCoroutine(FlashCoroutine(Color.white, 0.15f));
                EscapeToRandomPosition();
                StartCoroutine(PlayTwice(musicSound));
                ShowKeyboard();
                _state = State.ChallengeActive;
                Debug.Log($"[Musical] [J鍵] 模擬抓取 → ChallengeActive。通知書逃到 {transform.position}");
            }
            else if (_state == State.Solved)
            {
                Debug.Log("[Musical] [J鍵] 模擬最終抓取 → Choose()");
                Choose();
            }
        }
    }
}
