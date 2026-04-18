using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleMove : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody rb;

    void Start()
    {
        // Grab the Rigidbody we just attached to the player
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate() // FixedUpdate is always used for Physics!
    {
        // Get standard WASD input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Calculate the direction
        Vector3 move = transform.right * x + transform.forward * z;

        // Apply movement velocity, but keep the current Y velocity so gravity still pulls you down slopes
        rb.velocity = new Vector3(move.x * speed, rb.velocity.y, move.z * speed);
    }
}