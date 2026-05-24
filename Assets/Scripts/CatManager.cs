using UnityEngine;

public class CatManager : MonoBehaviour
{
    public static CatManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform catTransform;
    [SerializeField] private GameObject pawPrintPrefab;

    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 2f;
    [SerializeField] private Vector3 followOffset = new Vector3(0.3f, 0f, 0.8f);

    [Header("Paw Print")]
    [SerializeField] private float pawPrintSpacing = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    public bool CatSaved { get; private set; }

    private Transform _playerHead;
    private Transform _rigTransform;
    private Animator _catAnimator;
    private Vector3 _lastPawPrintPos;
    private bool _followEnabled;
    private float _lastTerrainY = 0f;

    private void Awake() => Instance = this;

    private void Start()
    {
        var rig = FindObjectOfType<OVRCameraRig>();
        if (rig != null)
        {
            _playerHead   = rig.centerEyeAnchor;
            _rigTransform = rig.transform;
        }

        if (catTransform != null)
        {
            _lastPawPrintPos = catTransform.position;
            _catAnimator = catTransform.GetComponentInChildren<Animator>();
        }

        Debug.Log("[CatManager] Initialized. CatSaved=false. PlayerHead found=" + (_playerHead != null));
    }

    private void Update()
    {
        if (enableDebugTrigger && Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("[CatManager] Debug key C pressed — force cat rescue.");
            OnCatSaved();
        }

        if (!_followEnabled || catTransform == null || _playerHead == null) return;

        FollowPlayer();
        TrySpawnPawPrint();
    }

    public void OnCatSaved()
    {
        if (CatSaved) return;

        CatSaved = true;
        _followEnabled = true;

        if (catTransform != null && _playerHead != null)
        {
            Vector3 dir = _playerHead.position - catTransform.position;
            dir.y = 0f;
            float yAngle = dir.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(dir).eulerAngles.y
                : 0f;
            catTransform.eulerAngles = new Vector3(0f, yAngle, 0f);
            Debug.Log($"[CatManager] Cat stood up facing player — Y={yAngle:F0}");
        }

        Debug.Log("[CatManager] Cat was saved! Follow behavior enabled.");
    }

    private void FollowPlayer()
    {
        Vector3 flatForward = _playerHead.forward;
        flatForward.y = 0;
        if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
        flatForward.Normalize();

        Vector3 flatRight = _playerHead.right;
        flatRight.y = 0;
        flatRight.Normalize();

        Vector3 targetPos = _playerHead.position
            + flatRight * followOffset.x
            + flatForward * followOffset.z;

        // Raycast down to keep cat on ground surface
        if (Physics.Raycast(targetPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && hit.collider is TerrainCollider)
            _lastTerrainY = hit.point.y;
        targetPos.y = _lastTerrainY;

        catTransform.position = Vector3.MoveTowards(
            catTransform.position, targetPos, followSpeed * Time.deltaTime);

        Vector3 dir = targetPos - catTransform.position;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            catTransform.rotation = Quaternion.RotateTowards(
                catTransform.rotation, targetRot, 180f * Time.deltaTime);
        }

        float distToTarget = Vector3.Distance(catTransform.position, targetPos);
        _catAnimator?.SetFloat("Vert", distToTarget > 0.15f ? 1f : 0f);
    }

    private void TrySpawnPawPrint()
    {
        if (pawPrintPrefab == null) return;
        if (Vector3.Distance(catTransform.position, _lastPawPrintPos) < pawPrintSpacing) return;

        Vector3 spawnPos = catTransform.position;
        if (Physics.Raycast(spawnPos + Vector3.up, Vector3.down, out RaycastHit hit, 10f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && hit.collider is TerrainCollider)
            spawnPos = hit.point + Vector3.up * 0.01f;

        GameObject paw = Instantiate(
            pawPrintPrefab, spawnPos,
            Quaternion.Euler(0f, catTransform.eulerAngles.y, 0f));

        _lastPawPrintPos = catTransform.position;

        Debug.Log($"[CatManager] Paw print spawned at {spawnPos}");
    }

    private void OnDrawGizmosSelected()
    {
        if (!_followEnabled || catTransform == null || _playerHead == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(catTransform.position, _playerHead.position);
    }
}
