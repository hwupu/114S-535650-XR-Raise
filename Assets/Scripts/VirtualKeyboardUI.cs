using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// World-space virtual keyboard for the CS letter puzzle.
// Attach to a World Space Canvas prefab. Assign the four References fields
// and the Right Hand Anchor in the Inspector.
public class VirtualKeyboardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text  displayText;
    [SerializeField] private TMP_Text  errorText;
    [SerializeField] private Transform keyboardContainer;
    [SerializeField] private Button    keyButtonPrefab;

    [Header("Layout")]
    [SerializeField] private float keySize    = 70f;
    [SerializeField] private float keySpacing = 8f;

    // Auto-resolved at runtime from OVRCameraRig — no inspector assignment needed.
    private Transform rightHandAnchor;

    private static readonly string[] Rows = { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };

    private string       _input = "";
    private Action<string> _onSubmit;
    private bool         _built;

    // Called by AdmissionLetterChoice right after the keyboard is shown.
    public void Init(Action<string> onSubmit)
    {
        if (!_built) BuildKeyboard();
        _onSubmit = onSubmit;
        _input    = "";
        UpdateDisplay();
        StopAllCoroutines();
        if (errorText != null) errorText.gameObject.SetActive(false);
    }

    // Shows a red error message that auto-hides after 2 seconds.
    public void ShowError(string msg)
    {
        if (errorText == null) return;
        StopAllCoroutines();
        errorText.text = msg;
        errorText.gameObject.SetActive(true);
        StartCoroutine(HideErrorAfter(2f));
    }

    public void OnKeyPress(string letter)
    {
        _input += letter;
        UpdateDisplay();
    }

    public void OnBackspace()
    {
        if (_input.Length > 0)
            _input = _input.Substring(0, _input.Length - 1);
        UpdateDisplay();
    }

    public void OnEnter()
    {
        string submitted = _input;
        _input = "";
        UpdateDisplay();
        _onSubmit?.Invoke(submitted);
    }

    private void Start()
    {
        // Find the right hand anchor from OVRCameraRig at runtime.
        // This avoids the prefab-vs-scene-object reference problem.
        var rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null) rightHandAnchor = rig.rightHandAnchor;

        if (!_built) BuildKeyboard();
        UpdateDisplay();
        if (errorText != null) errorText.gameObject.SetActive(false);
    }

    // Detect right-hand trigger press and click whichever key the controller points at.
    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            ClickAtControllerRay();
    }

    // Cast a ray from the right hand, intersect with this canvas plane, then find
    // which button's RectTransform contains the hit point.
    private void ClickAtControllerRay()
    {
        if (rightHandAnchor == null || keyboardContainer == null) return;

        var ray   = new Ray(rightHandAnchor.position, rightHandAnchor.forward);
        var plane = new Plane(-transform.forward, transform.position);

        if (!plane.Raycast(ray, out float dist)) return;

        var hitWorld = ray.GetPoint(dist);

        foreach (var btn in keyboardContainer.GetComponentsInChildren<Button>())
        {
            var rt      = btn.GetComponent<RectTransform>();
            var localPt = rt.InverseTransformPoint(hitWorld);
            if (rt.rect.Contains(new Vector2(localPt.x, localPt.y)))
            {
                btn.onClick.Invoke();
                return;
            }
        }
    }

    private void UpdateDisplay()
    {
        if (displayText != null)
            displayText.text = "printf(\"hello " + _input + "_\")";
    }

    // Builds QWERTY rows + special keys dynamically from keyButtonPrefab.
    private void BuildKeyboard()
    {
        if (_built || keyboardContainer == null || keyButtonPrefab == null) return;
        _built = true;

        foreach (string row in Rows)
        {
            var rowGO = CreateRow();
            foreach (char c in row)
            {
                string letter = c.ToString();
                var btn = CreateKey(rowGO.transform, letter, keySize);
                btn.onClick.AddListener(() => OnKeyPress(letter));
            }
        }

        // Special-keys row: Backspace + Enter
        var specialRow = CreateRow();
        var bsBtn      = CreateKey(specialRow.transform, "⌫", keySize * 1.5f);
        bsBtn.onClick.AddListener(OnBackspace);
        var enterBtn   = CreateKey(specialRow.transform, "Enter", keySize * 2.5f);
        enterBtn.onClick.AddListener(OnEnter);

        // Force layout recalculation immediately so buttons get correct sizes
        // before TMP_Text tries to render — prevents "Enter" from wrapping vertically.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(keyboardContainer as RectTransform);
    }

    // Creates a horizontal row container inside keyboardContainer.
    private GameObject CreateRow()
    {
        var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(keyboardContainer, false);

        var hlg = go.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing              = keySpacing;
        hlg.childAlignment       = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        // Give the row an explicit preferred height so the VerticalLayoutGroup
        // on KeyboardPanel doesn't force-expand and squish all rows equally.
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = keySize + keySpacing;
        le.flexibleWidth   = 1f;  // stretch to full container width

        return go;
    }

    // Instantiates one key button and configures its size.
    private Button CreateKey(Transform parent, string label, float width)
    {
        var btn = Instantiate(keyButtonPrefab, parent, false);
        btn.name = label;

        // LayoutElement drives the size inside HorizontalLayoutGroup.
        var le = btn.gameObject.GetComponent<LayoutElement>();
        if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.preferredHeight = keySize;

        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = label;

        return btn;
    }

    private IEnumerator HideErrorAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (errorText != null) errorText.gameObject.SetActive(false);
    }
}
