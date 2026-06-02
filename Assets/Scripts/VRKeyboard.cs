using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// World-space keyboard canvas built entirely in code.
// Uses pixel-based coordinates (sizeDelta in px) + localScale 0.001 → 1px = 1mm in world space.
// Canvas is NOT parented to the letter — it's a free-floating scene object.
[RequireComponent(typeof(LineRenderer))]
public class VRKeyboard : MonoBehaviour
{
    public event Action<char> OnKeyTyped;
    public event Action       OnEnterPressed;
    public event Action       OnBackspacePressed;

    // ── layout constants (pixels; canvas localScale=0.001 → 1px=1mm) ────────
    private const float KeyW    = 70f;
    private const float KeyH    = 65f;
    private const float KeyGap  = 6f;
    private const float CanvasW = 920f;
    private const float CanvasH = 580f;
    private const float LineH   = 55f;

    private static readonly string[] Row0 = { "Q","W","E","R","T","Y","U","I","O","P" };
    private static readonly string[] Row1 = { "A","S","D","F","G","H","J","K","L" };
    private static readonly string[] Row2 = { "Z","X","C","V","B","N","M" };

    private static readonly Color ColNormal = new Color(0.15f, 0.15f, 0.15f, 0.92f);
    private static readonly Color ColHover  = new Color(0.20f, 0.45f, 0.85f, 1.00f);
    private static readonly Color ColEnter  = new Color(0.10f, 0.55f, 0.18f, 1.00f);
    private static readonly Color ColBksp   = new Color(0.65f, 0.32f, 0.05f, 1.00f);

    [Header("Font")]
    [Tooltip("Optional: assign a TMP Font Asset that supports Unicode music notes (♩♪♫). Leave empty to use TMP default.")]
    [SerializeField] private TMP_FontAsset overrideFont;

    [Header("Debug")]
    [SerializeField] private bool debugKeyboardInput = true;

    // ── runtime ─────────────────────────────────────────────────────────────
    private struct KeyEntry
    {
        public RectTransform Rect;
        public Image         Bg;
        public string        Value;
    }

    private Canvas          _canvas;
    private RectTransform   _canvasRect;
    private TextMeshProUGUI _terminalLine;
    private TextMeshProUGUI _typedLine;
    private TextMeshProUGUI _statusLine;
    private List<KeyEntry>  _keys = new();
    private KeyEntry?       _hoveredKey;
    private LineRenderer    _laser;
    private OVRCameraRig    _rig;
    private bool            _triggerWasDown;

    // ── public API ──────────────────────────────────────────────────────────

    public void Show(Vector3 worldPos, Quaternion rotation)
    {
        EnsureBuilt();
        _canvas.gameObject.SetActive(true);
        _canvas.transform.SetPositionAndRotation(worldPos, rotation);
        _laser.enabled = _rig != null;
        Debug.Log($"[VRKeyboard] Show at pos={worldPos} euler={rotation.eulerAngles}");
    }

    public void Hide()
    {
        if (_canvas != null) _canvas.gameObject.SetActive(false);
        if (_laser  != null) _laser.enabled = false;
    }

    public void SetTypedText(string text, Color color)
    {
        if (_typedLine == null) return;
        _typedLine.text  = "> Type: [" + text + "_]";
        _typedLine.color = color;
    }

    public void SetStatusText(string text, Color color)
    {
        if (_statusLine == null) return;
        _statusLine.text  = "> " + text;
        _statusLine.color = color;
    }

    public void SetTerminalText(string text, Color color)
    {
        EnsureBuilt();
        if (_terminalLine == null) return;
        _terminalLine.text  = text;
        _terminalLine.color = color;
    }

    // ── Unity ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        _laser = GetComponent<LineRenderer>();
        _laser.positionCount = 2;
        _laser.startWidth    = 0.003f;
        _laser.endWidth      = 0.003f;
        _laser.material      = new Material(Shader.Find("Sprites/Default"));
        _laser.startColor    = new Color(0.4f, 0.8f, 1f, 1f);
        _laser.endColor      = new Color(0.4f, 0.8f, 1f, 0.2f);
        _laser.enabled       = false;
    }

    private void Start()
    {
        _rig = FindObjectOfType<OVRCameraRig>();
    }

    private void Update()
    {
        // --- direct keyboard input (editor / no-controller debug mode) ---
        if (debugKeyboardInput && _canvas != null && _canvas.gameObject.activeSelf)
        {
            foreach (char c in Input.inputString)
            {
                if (c == '\n' || c == '\r') OnEnterPressed?.Invoke();
                else if (c == '\b')         OnBackspacePressed?.Invoke();
                else if (char.IsLetter(c))  OnKeyTyped?.Invoke(char.ToUpper(c));
                else if (c == ' ')           OnKeyTyped?.Invoke(' ');
            }
        }

        if (_canvas == null || !_canvas.gameObject.activeSelf) return;

        // --- ray source: OVR controller or Camera.main fallback ---
        Ray     ray;
        Vector3 laserOrigin;
        bool    clickThisFrame;

        if (_rig != null)
        {
            Transform rh   = _rig.rightHandAnchor;
            ray            = new Ray(rh.position, rh.forward);
            laserOrigin    = rh.position;
            bool trigDown  = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            clickThisFrame = trigDown && !_triggerWasDown;
            _triggerWasDown = trigDown;
            _laser.enabled = true;
        }
        else if (Camera.main != null)
        {
            ray            = Camera.main.ScreenPointToRay(Input.mousePosition);
            laserOrigin    = Camera.main.transform.position;
            clickThisFrame = Input.GetMouseButtonDown(0);
            _triggerWasDown = false;
            _laser.enabled = false;
        }
        else return;

        // --- raycast against keys ---
        Vector3   laserEnd = laserOrigin + ray.direction * 3f;
        KeyEntry? newHover = null;

        foreach (KeyEntry key in _keys)
        {
            if (WorldRayHitsRect(ray, key.Rect, out Vector3 hitPt))
            {
                newHover = key;
                laserEnd = hitPt;
                break;
            }
        }

        if (_laser.enabled)
        {
            _laser.SetPosition(0, laserOrigin);
            _laser.SetPosition(1, laserEnd);
        }

        // update highlight
        if (_hoveredKey.HasValue && (!newHover.HasValue || _hoveredKey.Value.Rect != newHover.Value.Rect))
            SetKeyColor(_hoveredKey.Value, DefaultColor(_hoveredKey.Value.Value));

        if (newHover.HasValue)
            SetKeyColor(newHover.Value, ColHover);

        _hoveredKey = newHover;

        // click / trigger press
        if (clickThisFrame && newHover.HasValue)
            FireKey(newHover.Value.Value);
    }

    private void OnDestroy()
    {
        if (_canvas != null) Destroy(_canvas.gameObject);
    }

    // ── key fire ────────────────────────────────────────────────────────────

    private void FireKey(string value)
    {
        Debug.Log($"[VRKeyboard] 按鍵觸發: '{value}'");
        if (value == "ENTER")        OnEnterPressed?.Invoke();
        else if (value == "BKSP")    OnBackspacePressed?.Invoke();
        else if (value == "SPACE")   OnKeyTyped?.Invoke(' ');
        else if (value.Length == 1)  OnKeyTyped?.Invoke(value[0]);
    }

    // ── raycast ─────────────────────────────────────────────────────────────

    private bool WorldRayHitsRect(Ray ray, RectTransform rect, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;
        Plane plane = new Plane(rect.forward, rect.position);
        if (!plane.Raycast(ray, out float dist)) return false;

        hitPoint = ray.GetPoint(dist);
        Vector3 local = rect.InverseTransformPoint(hitPoint);
        Vector2 size  = rect.rect.size;
        return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.y) <= size.y * 0.5f;
    }

    // ── canvas builder ──────────────────────────────────────────────────────

    private void EnsureBuilt()
    {
        if (_canvas != null) return;

        // Free-floating canvas — NOT a child of the letter
        GameObject canvasGo = new GameObject("VRKeyboard_Canvas");
        canvasGo.transform.SetParent(null);

        _canvas     = canvasGo.AddComponent<Canvas>();
        _canvasRect = canvasGo.GetComponent<RectTransform>();
        _canvas.renderMode    = RenderMode.WorldSpace;
        _canvas.worldCamera   = null;
        _canvasRect.sizeDelta  = new Vector2(CanvasW, CanvasH);
        // 1 px = 1 mm → canvas is 920mm × 580mm = 0.92m × 0.58m
        _canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // dark background
        AddPanel(canvasGo, new Color(0.05f, 0.05f, 0.08f, 0.95f), CanvasW, CanvasH, Vector2.zero, z: 1f);

        float topY = CanvasH * 0.5f - 30f;

        // terminal header (overridable via SetTerminalText)
        _terminalLine = AddLabel(canvasGo, "> printf(\"hello \");", 20, new Vector2(-CanvasW * 0.5f + 20f, topY - LineH * 0f), Color.green);
        if (overrideFont != null) _terminalLine.font = overrideFont;

        // typed line
        _typedLine = AddLabel(canvasGo, "> Type: [_]", 20, new Vector2(-CanvasW * 0.5f + 20f, topY - LineH * 1f), Color.white);

        // status line
        _statusLine = AddLabel(canvasGo, ">", 16, new Vector2(-CanvasW * 0.5f + 20f, topY - LineH * 2f), new Color(0.8f, 0.8f, 0.8f));

        // divider
        AddPanel(canvasGo, new Color(0.3f, 0.3f, 0.35f, 1f), CanvasW - 40f, 3f, new Vector2(0, topY - LineH * 2.7f), z: -1f);

        // keyboard rows
        float keyboardTop = topY - LineH * 3.3f;
        BuildRow(canvasGo, Row0, keyboardTop);
        BuildRow(canvasGo, Row1, keyboardTop - (KeyH + KeyGap));
        BuildRow(canvasGo, Row2, keyboardTop - (KeyH + KeyGap) * 2f);

        // BKSP + SPACE + ENTER
        float bottomY = keyboardTop - (KeyH + KeyGap) * 3f;
        AddKey(canvasGo, "BKSP",  ColBksp,  KeyW * 2.0f, new Vector2(-KeyW * 3.3f, bottomY));
        AddKey(canvasGo, "SPACE", ColNormal, KeyW * 3.5f, new Vector2( 0f,          bottomY));
        AddKey(canvasGo, "ENTER", ColEnter, KeyW * 2.0f, new Vector2( KeyW * 3.3f, bottomY));

        Debug.Log($"[VRKeyboard] Canvas 建立完成. sizeDelta=({CanvasW},{CanvasH}) localScale={_canvasRect.localScale}  keys={_keys.Count}");
    }

    private void BuildRow(GameObject parent, string[] letters, float yPos)
    {
        float totalW = letters.Length * (KeyW + KeyGap) - KeyGap;
        float startX = -totalW * 0.5f + KeyW * 0.5f;

        for (int i = 0; i < letters.Length; i++)
            AddKey(parent, letters[i], ColNormal, KeyW, new Vector2(startX + i * (KeyW + KeyGap), yPos));
    }

    private void AddKey(GameObject parent, string label, Color col, float width, Vector2 localPos)
    {
        GameObject go = new GameObject("Key_" + label);
        go.transform.SetParent(parent.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta     = new Vector2(width, KeyH);
        rt.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        rt.localScale    = Vector3.one;

        Image bg = go.AddComponent<Image>();
        bg.color = col;

        // label stretched to fill key
        GameObject lblGo = new GameObject("Lbl");
        lblGo.transform.SetParent(go.transform, false);
        RectTransform lr = lblGo.AddComponent<RectTransform>();
        lr.anchorMin     = Vector2.zero;
        lr.anchorMax     = Vector2.one;
        lr.sizeDelta     = Vector2.zero;
        lr.localScale    = Vector3.one;

        TextMeshProUGUI tmp = lblGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = label.Length > 1 ? 18 : 28;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        KeyEntry entry = new KeyEntry { Rect = rt, Bg = bg, Value = label };
        _keys.Add(entry);
    }

    private TextMeshProUGUI AddLabel(GameObject parent, string text, int size, Vector2 localPos, Color color)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.pivot         = new Vector2(0f, 0.5f);
        rt.sizeDelta     = new Vector2(CanvasW - 40f, LineH);
        rt.localPosition = new Vector3(localPos.x, localPos.y, -1f);
        rt.localScale    = Vector3.one;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return tmp;
    }

    private void AddPanel(GameObject parent, Color color, float width, float height, Vector2 localPos, float z = 0f)
    {
        GameObject go = new GameObject("Panel");
        go.transform.SetParent(parent.transform, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta     = new Vector2(width, height);
        rt.localPosition = new Vector3(localPos.x, localPos.y, z);
        rt.localScale    = Vector3.one;

        go.AddComponent<Image>().color = color;
    }

    private Color DefaultColor(string value)
    {
        if (value == "ENTER") return ColEnter;
        if (value == "BKSP")  return ColBksp;
        return ColNormal;
    }

    private void SetKeyColor(KeyEntry key, Color col)
    {
        if (key.Bg != null) key.Bg.color = col;
    }
}
