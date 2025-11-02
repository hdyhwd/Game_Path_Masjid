using UnityEngine;
using UnityEngine.AI;

public class EnemyLogic : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform player;

    [Header("Chase Settings")]
    public float chaseRange = 10f;
    public float stopChaseRange = 15f;
    public float stopRange = 2f;
    public float rotationSpeed = 5f;

    [Header("Patrol Settings")]
    public float patrolRange = 8f;
    public float patrolWaitTime = 2f;
    public float reachPointDistance = 1f;

    [Header("Return Settings")]
    public float returnToSpawnDistance = 2f;
    public float maxDistanceFromSpawn = 20f;

    [Header("Jump Settings")]
    public float obstacleDetectionDistance = 2.5f;
    public float obstacleDetectionHeight = 0.5f;
    public float jumpForce = 7f;
    public float jumpCooldown = 1.5f;
    public float groundCheckDistance = 1.2f; // LEBIH PANJANG
    public LayerMask obstacleLayer;
    public LayerMask groundLayer;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private Vector3 spawnPoint;
    private Vector3 currentPatrolPoint;
    private float waitTimer;
    private bool isWaiting = false;
    private float lastJumpTime = -999f;
    private bool isGrounded = true;
    private bool isJumping = false; // FLAG PENTING

    private enum State { Patrolling, Chasing, Attacking, ReturningToSpawn }
    private State currentState = State.Patrolling;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // SMOOTH PHYSICS
        
        spawnPoint = transform.position;
        SetNewPatrolPoint();
    }

    void Update()
    {
        CheckGround();
        
        // HANYA CEK OBSTACLE KALAU GAK LAGI JUMPING
        if (!isJumping)
        {
            DetectAndJumpObstacle();
        }

        float distanceToPlayer = Vector3.Distance(player.position, transform.position);
        float distanceToSpawn = Vector3.Distance(spawnPoint, transform.position);

        if (distanceToSpawn > maxDistanceFromSpawn && currentState != State.ReturningToSpawn)
        {
            currentState = State.ReturningToSpawn;
        }

        switch (currentState)
        {
            case State.Patrolling:
                Patrol();
                if (distanceToPlayer <= chaseRange)
                {
                    currentState = State.Chasing;
                    isWaiting = false;
                }
                break;

            case State.Chasing:
                if (distanceToPlayer <= stopRange)
                {
                    currentState = State.Attacking;
                }
                else if (distanceToPlayer > stopChaseRange)
                {
                    currentState = State.ReturningToSpawn;
                }
                else
                {
                    ChasePlayer();
                }
                break;

            case State.Attacking:
                agent.isStopped = true;
                FacePlayer();

                if (distanceToPlayer > stopRange + 0.5f && distanceToPlayer <= stopChaseRange)
                {
                    currentState = State.Chasing;
                }
                else if (distanceToPlayer > stopChaseRange)
                {
                    currentState = State.ReturningToSpawn;
                }
                break;

            case State.ReturningToSpawn:
                ReturnToSpawn();
                
                if (distanceToSpawn <= returnToSpawnDistance)
                {
                    currentState = State.Patrolling;
                    SetNewPatrolPoint();
                }
                else if (distanceToPlayer <= chaseRange && distanceToSpawn <= maxDistanceFromSpawn * 0.8f)
                {
                    currentState = State.Chasing;
                }
                break;
        }
    }

    void CheckGround()
    {
        // RAYCAST LEBIH PANJANG + CEK DARI CENTER
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance, groundLayer);
        
        // Debug visual
        Debug.DrawRay(rayOrigin, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    void DetectAndJumpObstacle()
    {
        if (currentState == State.Attacking || !isGrounded) return;
        if (Time.time - lastJumpTime < jumpCooldown) return;

        // CEK APAKAH AGENT LAGI NYOBA GERAK (ada path)
        if (!agent.hasPath || agent.velocity.magnitude < 0.1f) return;

        // RAYCAST KE DEPAN
        Vector3 rayOrigin = transform.position + Vector3.up * obstacleDetectionHeight;
        Vector3 direction = agent.velocity.normalized; // PAKAI ARAH VELOCITY, BUKAN FORWARD

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, direction, out hit, obstacleDetectionDistance, obstacleLayer))
        {
            // CEK TINGGI OBSTACLE - HANYA JUMP KALAU OBSTACLE GAK TERLALU TINGGI
            if (hit.point.y - transform.position.y < 2f) // Max 2 meter tinggi obstacle
            {
                Jump();
            }
        }
        
        // Debug visual
        Debug.DrawRay(rayOrigin, direction * obstacleDetectionDistance, Color.magenta);
    }

    void Jump()
    {
        if (!isGrounded || isJumping) return;

        isJumping = true;
        
        // SIMPAN DESTINATION SEBELUM DISABLE
        Vector3 targetDestination = agent.destination;
        
        // DISABLE AGENT
        agent.enabled = false;

        // RESET VELOCITY Y, TAPI PERTAHANKAN HORIZONTAL
        Vector3 currentVel = rb.velocity;
        rb.velocity = new Vector3(currentVel.x, 0, currentVel.z);
        
        // JUMP!
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        
        // TAMBAHIN FORWARD FORCE DIKIT BIAR MAJU
        rb.AddForce(transform.forward * jumpForce * 0.3f, ForceMode.Impulse);

        lastJumpTime = Time.time;

        // CEK LANDING SETIAP FRAME
        StartCoroutine(WaitForLanding(targetDestination));
    }

    System.Collections.IEnumerator WaitForLanding(Vector3 targetDest)
    {
        // TUNGGU SAMPE GROUNDED LAGI
        yield return new WaitForSeconds(0.3f); // Delay kecil biar jump dulu
        
        while (!isGrounded)
        {
            yield return null; // Tunggu frame berikutnya
        }
        
        // TUNGGU DIKIT LAGI BIAR STABIL
        yield return new WaitForSeconds(0.2f);
        
        // RE-ENABLE AGENT
        if (!agent.enabled)
        {
            agent.enabled = true;
            
            // WARP KE POSISI SEKARANG (SINKRONISASI)
            agent.Warp(transform.position);
            
            // SET DESTINATION LAGI
            agent.SetDestination(targetDest);
        }
        
        isJumping = false;
    }

    void Patrol()
    {
        if (isWaiting)
        {
            agent.isStopped = true;
            waitTimer += Time.deltaTime;

            if (waitTimer >= patrolWaitTime)
            {
                isWaiting = false;
                SetNewPatrolPoint();
            }
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(currentPatrolPoint);

        if (!agent.pathPending && agent.remainingDistance <= reachPointDistance)
        {
            isWaiting = true;
            waitTimer = 0f;
        }
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void ReturnToSpawn()
    {
        agent.isStopped = false;
        agent.SetDestination(spawnPoint);
    }

    void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }

    void SetNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRange;
        randomDirection += spawnPoint;
        randomDirection.y = spawnPoint.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRange, NavMesh.AllAreas))
        {
            currentPatrolPoint = hit.position;
        }
        else
        {
            currentPatrolPoint = spawnPoint;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Area chase
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Area stop chase
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopChaseRange);

        // Area attack
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stopRange);

        if (Application.isPlaying)
        {
            // Area patrol
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spawnPoint, patrolRange);

            // Max distance
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(spawnPoint, maxDistanceFromSpawn);

            // Current patrol target
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentPatrolPoint, 0.5f);
            Gizmos.DrawLine(transform.position, currentPatrolPoint);

            // Return line
            if (currentState == State.ReturningToSpawn)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }

            // Ground check ray
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * groundCheckDistance);
        }
    }
}