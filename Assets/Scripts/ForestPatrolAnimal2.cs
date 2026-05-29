using UnityEngine;
using UnityEngine.AI;

public class ForestPatrolAnimal2 : MonoBehaviour
{
    public Transform[] patrolPoints;       
    public ForestManage forestManager; 
    public float minDistanceToTarget = 1.2f;

    private NavMeshAgent agent;
    private int currentTargetIndex;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        // 生成時延遲一點點時間再開始走，確保它已經降落到 NavMesh 上
        Invoke("MoveToNextRandomPoint", 0.1f); 
    }

    void Update()
    {
        // 【新增防呆檢查】：確定它真的活著，且腳確實踩在藍色網格上，才去算距離
        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            if (!agent.pathPending && agent.remainingDistance < minDistanceToTarget)
            {
                MoveToNextRandomPoint();
            }
        }
    }

    void MoveToNextRandomPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        
        // 【新增防呆檢查】：不在網格上就不准下指令
        if (!agent.isOnNavMesh) return; 

        currentTargetIndex = Random.Range(0, patrolPoints.Length);
        agent.SetDestination(patrolPoints[currentTargetIndex].position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (forestManager != null)
            {
                forestManager.TakeDamage();
            }
            
            // 撞到玩家後，確定還在網格上才換方向跑
            if (agent.isOnNavMesh)
            {
                MoveToNextRandomPoint();
            }
        }
    }
}