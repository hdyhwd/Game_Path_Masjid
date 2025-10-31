using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovementLogic : MonoBehaviour
{
    public float moveSpeed = 5f; // Kecepatan gerak
    public float rotationSpeed = 10f; // Kecepatan rotasi
    private Rigidbody rb;
    private Vector3 moveDirection;

    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // biar gak jatuh pas tabrakan
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Ambil input
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // Arah gerak
        moveDirection = new Vector3(moveX, 0f, moveZ).normalized;

        // Status animasi
        bool isWalking = moveDirection.magnitude > 0;
        if (anim != null)
            anim.SetBool("Walk", isWalking);

        // Rotasi ke arah gerak
        if (isWalking)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        // Gerak fisik
        rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
    }
}
