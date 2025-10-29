using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovementLogic : MonoBehaviour
{
    public float moveSpeed = 5f; // Kecepatan gerak
    private Rigidbody rb;
    private Vector3 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Biar tidak jatuh atau miring saat tabrakan
    }

    void Update()
    {
        // Ambil input dari WASD atau Arrow Keys
        float moveX = Input.GetAxisRaw("Horizontal"); // A-D atau Panah kiri-kanan
        float moveZ = Input.GetAxisRaw("Vertical");   // W-S atau Panah atas-bawah

        // Hanya gerak dalam sumbu XZ (kanan, kiri, depan, belakang)
        moveDirection = new Vector3(moveX, 0f, moveZ).normalized;
    }

    void FixedUpdate()
    {
        // Pindahkan player secara halus sesuai arah input
        rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
    }
}
