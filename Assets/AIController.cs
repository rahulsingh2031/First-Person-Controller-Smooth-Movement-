using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIController : MonoBehaviour
{
    NavMeshAgent navMeshAgent;
    public Transform target;
    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }
    Vector3 targetAns;
    private void Update()
    {
        Vector3 targetDestination = target.position;
        targetDestination.y = transform.position.y;
        Vector3 offset = (target.position - transform.position).normalized;
        targetAns = targetDestination - offset * 4;
        navMeshAgent.SetDestination(targetAns);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(targetAns, 0.3f);
    }
}
