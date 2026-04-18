using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [SerializeField] float mouseSensitivity = 1000f;
    public Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        // This locks your mouse cursor to the center of the screen and hides it
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Get the mouse movement
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Calculate the up/down camera rotation
        xRotation -= mouseY;

        // Clamp it so you can't break your neck looking too far backward
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply the up/down rotation to the Camera
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Apply the left/right rotation to the Player Capsule
        playerBody.Rotate(Vector3.up * mouseX);
    }
}