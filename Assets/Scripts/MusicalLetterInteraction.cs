using System.Collections;
using UnityEngine;
using Oculus.Interaction;

// Attach to the Music admission letter prefab INSTEAD OF AdmissionLetterChoice.
// The CS letter uses CSLetterInteraction.
//
// State flow:  Idle → (grip grab) → Active → (type "do re mi" + Enter) → Chosen
//
// Grab detection uses transform.parent edge-detection:
//   grabStarted = parent changed to a grab-system hand (not _initialParent)
// No escape mechanic — the letter follows the controller normally.
// Letter pulses red while Active; turns green when solved then grows 2× and disappears.
//
// Target phrase: "do re mi" (spaces required; normalised before comparison).
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VRKeyboard))]
public class MusicalLetterInteraction : MonoBehaviour
{
    private enum State { Idle, Active, Chosen }

    private const string TargetPhrase   = "do re mi";
    private const string TerminalHeader = "♩ Do  ♪ Re  ♫ Mi";

    [Header("References")]
    [SerializeField] private CSLetterInteraction csLetter;

    [Header("Audio")]
    [SerializeField] private AudioClip musicSound;
    [SerializeField] private AudioClip solveSound;

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    private State          _state = State.Idle;
    private Rigidbody      _rb;
    private Grabbable      _grabbable;
    private SpriteRenderer _sprite;
    private Color          _originalColor;
    private VRKeyboard     _keyboard;
    private OVRCameraRig   _rig;
    private string         _typedText = "";
    private bool           _wasGrabbed = false;
    private float          _dbgTimer   = 0f;
    private Coroutine      _pulseCoroutine = null;

    // ── Unity ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb        = GetComponent<Rigidbody>();
        _grabbable = GetComponent<Grabbable>();
        _sprite    = GetComponentInChildren<SpriteRenderer>();
        _keyboard  = GetComponent<VRKeyboard>();

        if (_sprite != null) _originalColor = _sprite.color;
        Debug.Log($"[Musical] Awake: _grabbable={(_grabbable==null?"NULL":"OK")}, _keyboard={(_keyboard==null?"NULL":"OK")}, _rb={(_rb==null?"NULL":"OK")}");
    }

    private void Start()
    {
        _rig = FindObjectOfType<OVRCameraRig>();

        _rb.isKinematic = true;
        _rb.useGravity  = false;

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

        bool isGrabbed   = _grabbable != null && _grabbable.SelectingPointsCount > 0;
        bool grabStarted = isGrabbed  && !_wasGrabbed;
        bool grabEnded   = !isGrabbed && _wasGrabbed;
        _wasGrabbed = isGrabbed;

        _dbgTimer += Time.deltaTime;
        if (_dbgTimer >= 2f)
        {
            _dbgTimer = 0f;
            string grabStatus = _grabbable == null ? "NULL" : (_grabbable.SelectingPointsCount > 0 ? "GRABBED" : "not-grabbed");
            Debug.Log($"[Musical] poll: state={_state}, grab={grabStatus}, parent={transform.parent?.name ?? "none"}");
        }

        switch (_state)
        {
            case State.Idle:
                if (grabStarted)
                {
                    if (csLetter != null) csLetter.LoseFocus();
                    StartCoroutine(PlayTwice(musicSound));
                    ShowKeyboard();
                    StartPulse();
                    _state = State.Active;
                    Debug.Log("[Musical] Idle → Active. VRKeyboard 出現。");
                }
                break;

            case State.Active:
                if (grabEnded)
                {
                    _rb.isKinematic     = true;
                    _rb.velocity        = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }
                if (grabStarted)
                {
                    if (csLetter != null) csLetter.LoseFocus();
                    StartCoroutine(PlayTwice(musicSound));
                    ShowKeyboard();
                    StartPulse();
                    Debug.Log("[Musical] 重新抓取，鍵盤重新定位。");
                }
                break;
        }
    }

    // ── public: called by CS letter when it takes focus ─────────────────────

    public void LoseFocus()
    {
        if (_state != State.Active) return;
        _keyboard.Hide();
        StopPulse();
    }

    // ── keyboard callbacks ──────────────────────────────────────────────────

    private void OnKeyTyped(char c)
    {
        if (_state != State.Active) return;
        _typedText += (c == ' ') ? ' ' : char.ToLower(c);
        _keyboard.SetTypedText(_typedText, Color.white);
        _keyboard.SetStatusText("Press ENTER to confirm.", new Color(0.8f, 0.8f, 0.8f));
        Debug.Log($"[Musical] 輸入中: '{_typedText}'");
    }

    private void OnEnterPressed()
    {
        if (_state != State.Active) return;

        string normalised = string.Join(" ",
            _typedText.Trim().ToLower()
                      .Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));

        if (normalised == TargetPhrase)
        {
            _state = State.Chosen;
            _keyboard.Hide();
            StopPulse();
            GameManager.Instance?.RecordDepartmentChoice(false);
            if (ForestManage.Instance != null)
                ForestManage.Instance.OnMakeSelfChoice();
            else
                Debug.LogWarning("[Musical] ForestManage.Instance = NULL → 危險選擇未觸發！請確認場景中有 ForestManage 物件。");
            if (csLetter != null) { csLetter.LoseFocus(); csLetter.gameObject.SetActive(false); }
            PlaySound(solveSound);
            Debug.Log("[Musical] ★ 音樂系選定（危險選擇）→ ForestManage.OnMakeSelfChoice()");
            StartCoroutine(ChosenCelebration());
        }
        else
        {
            _keyboard.SetTypedText(_typedText, Color.red);
            _keyboard.SetStatusText("Wrong! Hint: ♩ do  ♪ re  ♫ mi", Color.red);
            Debug.Log($"[Musical] ✗ 答錯: '{_typedText}'（normalised='{normalised}'）。");
            _typedText = "";
            StartCoroutine(ClearTypedAfterDelay(1.0f));
        }
    }

    private void OnBackspacePressed()
    {
        if (_state != State.Active || _typedText.Length == 0) return;
        _typedText = _typedText.Substring(0, _typedText.Length - 1);
        _keyboard.SetTypedText(_typedText, Color.white);
        Debug.Log($"[Musical] Backspace，目前: '{_typedText}'");
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private IEnumerator RedPulse()
    {
        while (true)
        {
            SetColor(Color.red);
            yield return new WaitForSeconds(0.3f);
            SetColor(_originalColor);
            yield return new WaitForSeconds(0.3f);
        }
    }

    private void StartPulse()
    {
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(RedPulse());
    }

    private void StopPulse()
    {
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
        SetColor(_originalColor);
    }

    private IEnumerator ChosenCelebration()
    {
        OVRGrabbable ovr = GetComponent<OVRGrabbable>();
        if (ovr != null && ovr.isGrabbed)
            ovr.grabbedBy.ForceRelease(ovr);
        transform.SetParent(null);
        _rb.isKinematic     = true;
        _rb.velocity        = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        SetColor(Color.green);

        Vector3 startScale = transform.localScale;
        float   elapsed    = 0f;
        float   duration   = 0.8f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, startScale * 2f, elapsed / duration);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private void SetColor(Color color)
    {
        if (_sprite != null)
        {
            _sprite.color = color;
            return;
        }
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            rend.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            mpb.SetColor("_Color",     color);
            rend.SetPropertyBlock(mpb);
        }
    }

    private void ShowKeyboard()
    {
        if (_rig == null) _rig = FindObjectOfType<OVRCameraRig>();
        Transform head = _rig      != null ? _rig.centerEyeAnchor
                       : Camera.main != null ? Camera.main.transform
                       : null;
        if (head == null) return;

        Vector3 pos    = head.position + head.forward * 1.5f;
        pos.y          = head.position.y - 0.1f;
        Quaternion rot = Quaternion.LookRotation(pos - head.position);

        _typedText = "";
        _keyboard.SetTerminalText(TerminalHeader, new Color(1f, 0.85f, 0.2f));
        _keyboard.SetTypedText("", Color.white);
        _keyboard.SetStatusText("Spell 'do re mi' to choose Music.", new Color(0.8f, 0.8f, 0.8f));
        _keyboard.Show(pos, rot);
        Debug.Log($"[Musical] VRKeyboard 顯示於 pos={pos}");
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

    private IEnumerator ClearTypedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _typedText = "";
        if (_state == State.Active)
            _keyboard.SetTypedText("", Color.white);
    }

    // ── debug ────────────────────────────────────────────────────────────────

    private void DebugInput()
    {
        if (!enableDebugTrigger) return;

        // J = simulate grab (Idle → Active)
        if (Input.GetKeyDown(KeyCode.J) && _state == State.Idle)
        {
            if (csLetter != null) csLetter.LoseFocus();
            StartCoroutine(PlayTwice(musicSound));
            ShowKeyboard();
            StartPulse();
            _state = State.Active;
            Debug.Log("[Musical] [J鍵] Idle → Active");
        }

        // K = show keyboard only (Idle → Active)
        if (Input.GetKeyDown(KeyCode.K) && _state == State.Idle)
        {
            ShowKeyboard();
            StartPulse();
            _state = State.Active;
            Debug.Log("[Musical] [K鍵] Idle → Active（僅鍵盤）");
        }

        // D = force-solve
        if (Input.GetKeyDown(KeyCode.D) && _state == State.Active)
        {
            _typedText = TargetPhrase;
            OnEnterPressed();
            Debug.Log("[Musical] [D鍵] 強制解謎");
        }
    }
}
