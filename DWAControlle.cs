using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class DWAController : MonoBehaviour
{
    [Header("Movement")]
    public float linearSpeed = 1.5f;
    public float planningHorizon = 1.2f;
    public float stuckThreshold = 1.5f;
    public float directionDuration = 1.0f;
    public float escapeTime = 1.5f;
    public float obstacleAvoidanceDistance = 1.8f;
    public float scanRadius = 4f; // أضيفت
    public LayerMask obstacleMask;

    [Header("Smoothing")]
    public float velocitySmoothFactor = 0.1f;
    public float rotationSmoothFactor = 0.05f;

    [Header("Voice")]
    public string[] commands = { "go to kitchen", "go to bedroom", "go to living room" };
    public ConfidenceLevel minConfidence = ConfidenceLevel.Medium;

    [Header("References")]
    public Transform target;
    public ObstacleScanner scanner;

    private Rigidbody rb;
    private Vector3 currentDirection;
    private float directionTimer;
    private float stuckTimer;
    private bool isEscaping;
    private KeywordRecognizer keywordRecognizer;
    private Vector3 smoothedVelocity;
    private Quaternion smoothedRotation;
    private Vector3 lastPos; // أضيفت

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentDirection = transform.forward;
        smoothedRotation = rb.rotation;
        lastPos = transform.position; // تهيئة

        keywordRecognizer = new KeywordRecognizer(commands, minConfidence);
        keywordRecognizer.OnPhraseRecognized += args => {
            if (LocationManager.Instance == null) return;
            switch (args.text.Split(' ').Last())
            {
                case "kitchen": target = LocationManager.Instance.kitchen; break;
                case "bedroom": target = LocationManager.Instance.bedroom; break;
                case "room": target = LocationManager.Instance.livingroom; break;
            }
        };
        keywordRecognizer.Start();
    }

    void FixedUpdate()
    {
        if (target == null || HasReachedTarget())
        {
            rb.velocity = Vector3.zero;
            return;
        }

        directionTimer -= Time.fixedDeltaTime;
        bool forceRecalc = directionTimer <= 0.1f || !scanner.IsPathClear(currentDirection, linearSpeed * planningHorizon * 0.6f);

        if (directionTimer <= 0 || forceRecalc)
        {
            Vector3 best = PickBestDirection();
            if (best != Vector3.zero) currentDirection = best;
            directionTimer = directionDuration;
        }

        if (!scanner.IsPathClear(currentDirection, linearSpeed * planningHorizon * 0.4f))
        {
            rb.velocity = Vector3.zero;
            directionTimer = 0;
            return;
        }

        Vector3 safeDir = GetSafeDirection();
        float distToTarget = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(target.position.x, target.position.z)
        );

        if (distToTarget < 2f)
        {
            Vector3 direct = (target.position - transform.position).normalized;
            if (scanner.IsPathClear(direct, distToTarget * 0.9f)) safeDir = direct;
        }

        smoothedVelocity = Vector3.Lerp(smoothedVelocity, safeDir * linearSpeed, velocitySmoothFactor);
        rb.velocity = new Vector3(smoothedVelocity.x, rb.velocity.y, smoothedVelocity.z);

        if (safeDir != Vector3.zero)
        {
            smoothedRotation = Quaternion.Slerp(smoothedRotation, Quaternion.LookRotation(safeDir), rotationSmoothFactor);
            rb.MoveRotation(smoothedRotation);
        }

        // تحديث stuckTimer بناءً على الحركة
        float movedDist = Vector3.Distance(transform.position, lastPos);
        if (movedDist < 0.05f)
        {
            stuckTimer += Time.fixedDeltaTime;
        }
        else
        {
            stuckTimer = 0;
        }
        lastPos = transform.position;

        // التحقق من الحاجة للهروب
        bool isSpinning = rb.angularVelocity.magnitude > 30f && movedDist < 0.1f;
        if ((stuckTimer > stuckThreshold || isSpinning) && !isEscaping)
        {
            isEscaping = true;
            stuckTimer = 0;
            currentDirection = FindSafeEscapeDirection();
            directionTimer = escapeTime;
        }
        else if (movedDist > 0.2f)
        {
            isEscaping = false;
        }
    }

    private Vector3 PickBestDirection()
    {
        Vector3 bestDir = Vector3.zero;
        float bestScore = float.MinValue;
        Vector3 toTarget = (target.position - transform.position).normalized;
        float distToTarget = Vector3.Distance(transform.position, target.position);
        float[] angles = distToTarget < 3f ? new float[] { -30, -15, 0, 15, 30 } : new float[] { -60, -30, 0, 30, 60 };

        foreach (float angle in angles)
        {
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            if (scanner.IsPathClear(dir, linearSpeed * planningHorizon))
            {
                float score = Vector3.Dot(dir, toTarget) * (distToTarget < 4f ? 1.5f : 1f);
                if (!Physics.Raycast(transform.position, dir, obstacleAvoidanceDistance, obstacleMask)) score += 0.3f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }
        }
        return bestDir;
    }

    private Vector3 GetSafeDirection()
    {
        Vector3 dir = Vector3.Lerp(currentDirection, (target.position - transform.position).normalized, 0.15f);
        if (Physics.Raycast(transform.position, dir, obstacleAvoidanceDistance * 0.8f, obstacleMask))
        {
            Vector3 left = Quaternion.Euler(0, -40, 0) * dir;
            Vector3 right = Quaternion.Euler(0, 40, 0) * dir;
            bool leftClear = !Physics.Raycast(transform.position, left, obstacleAvoidanceDistance, obstacleMask);
            bool rightClear = !Physics.Raycast(transform.position, right, obstacleAvoidanceDistance, obstacleMask);

            if (leftClear && rightClear) dir = Quaternion.Euler(0, Random.Range(-30f, 30f), 0) * dir;
            else if (leftClear) dir = left;
            else if (rightClear) dir = right;
            else dir = -dir;
        }
        return dir;
    }

    // أضيفت الدالة المفقودة
    private Vector3 FindSafeEscapeDirection()
    {
        Vector3[] escapeDirs = { -transform.forward, transform.right, -transform.right, transform.forward };
        foreach (Vector3 dir in escapeDirs)
        {
            if (scanner.IsPathClear(dir, scanRadius * 1.2f)) return dir;
        }
        return -transform.forward; // افتراضي
    }

    private bool HasReachedTarget() => target && Vector2.Distance(
        new Vector2(transform.position.x, transform.position.z),
        new Vector2(target.position.x, target.position.z)) < 1.2f;

    void OnDestroy() => keywordRecognizer?.Stop();
}