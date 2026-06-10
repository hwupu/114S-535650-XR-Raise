using UnityEngine;

public class ForestPatrolAnimal2 : MonoBehaviour
{
    public Transform[] patrolPoints;
    public ForestManage forestManager;

    [Header("移動設定")]
    public float moveSpeed = 2f;
    public float stopDistance = 0.5f;

    private int _currentIndex;

    void Start()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        _currentIndex = Random.Range(0, patrolPoints.Length);
    }

    void Update()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Transform target = patrolPoints[_currentIndex];

        // 水平方向移動（忽略 Y，避免動物飛起來）
        Vector3 myFlat     = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 targetFlat = new Vector3(target.position.x,    0f, target.position.z);

        float dist = Vector3.Distance(myFlat, targetFlat);

        if (dist <= stopDistance)
        {
            // 到達目標點，挑下一個（不重複當前）
            int next = _currentIndex;
            if (patrolPoints.Length > 1)
            {
                while (next == _currentIndex)
                    next = Random.Range(0, patrolPoints.Length);
            }
            _currentIndex = next;
        }
        else
        {
            // 朝目標移動
            Vector3 dir = (targetFlat - myFlat).normalized;
            transform.position += new Vector3(dir.x, 0f, dir.z) * moveSpeed * Time.deltaTime;

            // 面朝移動方向
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        forestManager?.TakeDamage();

        // 撞到玩家後換下一個點
        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            int next = _currentIndex;
            while (next == _currentIndex)
                next = Random.Range(0, patrolPoints.Length);
            _currentIndex = next;
        }
    }
}
