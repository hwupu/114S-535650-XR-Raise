using UnityEngine;

// Place at the hub area in SceneForest2.
// Assign 3 fragment GameObjects (initially inactive in scene) and a reveal SFX.
// Listens to GameManager.OnEventCompleted and reveals one fragment per event.
[RequireComponent(typeof(AudioSource))]
public class FragmentManager : MonoBehaviour
{
    [Header("Fragments — assign in order (index 0 = Event 1, etc.)")]
    [SerializeField] private GameObject[] fragments = new GameObject[3];

    [Header("Audio")]
    [SerializeField] private AudioClip revealSFX;

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    private AudioSource _audio;

    private void Awake() => _audio = GetComponent<AudioSource>();

    private void Start()
    {
        foreach (var f in fragments)
            if (f != null) f.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.OnEventCompleted += OnEventCompleted;
        else
            Debug.LogWarning("[FragmentManager] GameManager.Instance not found on Start.");
    }

    private void Update()
    {
        if (!enableDebugTrigger) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) RevealFragment(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) RevealFragment(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) RevealFragment(2);
    }

    private void OnEventCompleted(int eventNumber)
    {
        RevealFragment(eventNumber - 1);
    }

    private void RevealFragment(int index)
    {
        if (index < 0 || index >= fragments.Length) return;
        if (fragments[index] == null) return;

        fragments[index].SetActive(true);
        if (revealSFX != null) _audio.PlayOneShot(revealSFX);
        Debug.Log($"[FragmentManager] Fragment {index + 1} revealed.");
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnEventCompleted -= OnEventCompleted;
    }
}
