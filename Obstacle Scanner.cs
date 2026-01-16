using UnityEngine;

public class ObstacleScanner : MonoBehaviour
{
    public float scanRadius = 3f;
    public LayerMask obstacleMask;

    public bool IsPathClear(Vector3 direction, float distance)
    {
        if (distance <= 0) return true;
        foreach (Vector3 origin in new[] {
            transform.position,
            transform.position + transform.right * 0.2f,
            transform.position - transform.right * 0.2f
        })
        {
            if (Physics.Raycast(origin, direction, distance, obstacleMask)) return false;
        }
        return true;
    }
}