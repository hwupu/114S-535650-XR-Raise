using UnityEngine;
using UnityEngine.AI;


public class ForestPatrolAnimal2 : MonoBehaviour
{
    public Transform[] patrolPoints;       // 巡邏點
    public ForestManage forestManager;
    public float minDistanceToTarget = 1.2f;


    private NavMeshAgent agent;
    private int currentTargetIndex;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        MoveToNextRandomPoint();
    }


    void Update()
    {
        // 接近目標點時，自動切換到下一個隨機巡邏點
        if (!agent.pathPending && agent.remainingDistance < minDistanceToTarget)
        {
            MoveToNextRandomPoint();
        }
    }


    void MoveToNextRandomPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;


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
            MoveToNextRandomPoint();
        }
    }
}

