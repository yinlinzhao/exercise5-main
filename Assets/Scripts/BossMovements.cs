using UnityEngine;

public class BossMovements : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Movement")]
    [SerializeField] private float wanderSpeed = 0.8f;
    [SerializeField] private Vector2 wanderDistanceRange = new Vector2(0.75f, 1.75f);
    [SerializeField] private float wanderPauseSeconds = 0.4f;
    [SerializeField] private float arriveDistance = 0.05f;

    [Header("Wander Bounds")]
    [SerializeField] private float maxWanderRadius = 4f;

    private Vector3 wanderDestination;
    private bool hasWanderDestination;
    private float nextWanderPickTime;
    private Vector3 originPosition;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        originPosition = transform.position;
    }

    // Call from Visual Scripting: FacePlayer(target)
    public void FacePlayer(GameObject target)
    {
        if (spriteRenderer == null || target == null) return;

        // Player left of boss => flipX = true
        spriteRenderer.flipX = target.transform.position.x < transform.position.x;
    }

    // Wander: gently pick a random direction and slowly move a short distance.
    // Intended to be called continuously (e.g., from FixedUpdate via Visual Scripting).
    public void Wander()
    {
        if (Time.time < nextWanderPickTime) return;

        Vector3 pos = transform.position;
        if (!hasWanderDestination)
        {
            PickNewWanderDestination(pos);
        }

        if (MoveTowards(wanderDestination, wanderSpeed))
        {
            hasWanderDestination = false;
            nextWanderPickTime = Time.time + wanderPauseSeconds;
        }
    }

    // Chase: move toward the provided target with a provided speed (nice for Visual Scripting).
    public void Chase(GameObject target, float speed)
    {
        if (target == null) return;

        Vector3 pos = transform.position;
        Vector3 toTarget = target.transform.position - pos;

        // Only move in 2D plane
        Vector2 direction = new Vector2(toTarget.x, toTarget.y);
        if (direction.magnitude <= arriveDistance) return;

        direction.Normalize();
        MoveInDirection(direction, Mathf.Max(0f, speed));

        // Optional: keep sprite facing the target
        FacePlayer(target);
    }

    private void PickNewWanderDestination(Vector3 fromPosition)
    {
        Vector2 dir = Random.insideUnitCircle;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();

        float min = Mathf.Min(wanderDistanceRange.x, wanderDistanceRange.y);
        float max = Mathf.Max(wanderDistanceRange.x, wanderDistanceRange.y);
        float distance = Random.Range(min, max);

        Vector3 candidate = fromPosition + new Vector3(dir.x, dir.y, 0f) * distance;
        candidate.z = fromPosition.z;

        // Keep wander destinations within a radius of where the boss started.
        if (maxWanderRadius > 0f)
        {
            Vector3 offsetFromOrigin = candidate - originPosition;
            offsetFromOrigin.z = 0f;
            if (offsetFromOrigin.magnitude > maxWanderRadius)
            {
                Vector3 clamped = originPosition + Vector3.ClampMagnitude(offsetFromOrigin, maxWanderRadius);
                clamped.z = fromPosition.z;
                candidate = clamped;
            }
        }

        wanderDestination = candidate;
        hasWanderDestination = true;

        // Make wandering look natural by flipping based on movement direction
        if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.001f)
            spriteRenderer.flipX = dir.x < 0f;
    }

    private bool MoveTowards(Vector3 destination, float speed)
    {
        Vector3 pos = transform.position;
        Vector3 toDest = destination - pos;

        Vector2 delta2D = new Vector2(toDest.x, toDest.y);
        if (delta2D.magnitude <= arriveDistance) return true;

        delta2D.Normalize();
        MoveInDirection(delta2D, speed);
        return false;
    }

    private void MoveInDirection(Vector2 direction, float speed)
    {
        float dt = Time.deltaTime;
        Vector2 step = direction * speed * dt;

        transform.position += new Vector3(step.x, step.y, 0f);

        if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.001f)
            spriteRenderer.flipX = direction.x < 0f;
    }
}