using UnityEngine;

public class PlayerLogic : MonoBehaviour
{
    [Header("Player Settings")]
    public float moveSpeed = 1f;

    [Header("Components")]
    public Rigidbody rb; // <-- ubah ke Rigidbody biasa, bukan Rigidbody2D
    public Animator animator;

    private Vector3 movement;
    private Vector3 lastMoveDir;

    void Update()
    {
        // --- Input Keyboard (WASD / panah) ---
        movement.x = Input.GetAxisRaw("Horizontal"); // kiri-kanan
        movement.z = Input.GetAxisRaw("Vertical");   // depan-belakang (bukan Y lagi)

        // --- Update parameter animator ---
        if (movement != Vector3.zero)
        {
            animator.SetBool("IsMoving", true);
            animator.SetFloat("MoveX", movement.x);
            animator.SetFloat("MoveY", movement.z); // karena di animasi masih pakai X/Y

            lastMoveDir = movement;
            animator.SetFloat("LastMoveX", lastMoveDir.x);
            animator.SetFloat("LastMoveY", lastMoveDir.z);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }
    }

    void FixedUpdate()
    {
        // --- Gerak di dunia 3D ---
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}
