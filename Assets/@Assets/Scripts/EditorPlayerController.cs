using UnityEngine;

// Solo para probar en el editor: WASD para moverte, ratón para mirar.
// Ponlo en la Main Camera. En el Quest se desactiva/elimina (lo sustituye el XR Origin).
// Requiere: Project Settings > Player > Active Input Handling = "Both" (o "Input Manager (Old)").
public class EditorPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float lookSensitivity = 2f;

    private float yaw, pitch;

    private void Start()
    {
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        yaw += Input.GetAxis("Mouse X") * lookSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -80f, 80f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Moverse en horizontal, sin cambiar la altura
        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 move = fwd * Input.GetAxis("Vertical") + right * Input.GetAxis("Horizontal");
        transform.position += move * moveSpeed * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;
    }
}
