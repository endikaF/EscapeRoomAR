using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Puntero del mando derecho para los menús en World Space.
// Lanza un rayo desde el mando y, al pulsar el gatillo, hace clic en el Button cuyo Collider golpee.
// Sin mando (editor): clic izquierdo del ratón con un rayo desde la cámara hacia delante.
// Ponlo en un objeto vacío de la escena del menú.
public class MenuControllerPointer : MonoBehaviour
{
    [Tooltip("Objeto XR Origin: convierte la pose del mando (espacio de tracking) a mundo")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private float rayLength = 10f;
    [Tooltip("Mismo valor que uses en QuestControllerPlacement")]
    [SerializeField] private float rayPitchOffset = 0f;

    private InputAction posAction, rotAction, triggerAction;
    private LineRenderer line;

    private void Awake()
    {
        posAction = new InputAction("Position", InputActionType.Value, expectedControlType: "Vector3");
        posAction.AddBinding("<XRController>{RightHand}/pointerPosition");

        rotAction = new InputAction("Rotation", InputActionType.Value, expectedControlType: "Quaternion");
        rotAction.AddBinding("<XRController>{RightHand}/pointerRotation");

        triggerAction = new InputAction("Trigger", InputActionType.Button);
        triggerAction.AddBinding("<XRController>{RightHand}/{TriggerButton}");
        triggerAction.AddBinding("<XRController>{RightHand}/triggerPressed");
    }

    private void OnEnable()
    {
        posAction.Enable();
        rotAction.Enable();
        triggerAction.Enable();
    }

    private void OnDisable()
    {
        posAction.Disable();
        rotAction.Disable();
        triggerAction.Disable();
    }

    private void OnDestroy()
    {
        posAction.Dispose();
        rotAction.Dispose();
        triggerAction.Dispose();
    }

    private void Start()
    {
        var lineGo = new GameObject("ControllerRay");
        lineGo.transform.SetParent(transform, false);
        line = lineGo.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = line.endWidth = 0.006f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = line.endColor = Color.cyan;
        line.enabled = false;
    }

    private void Update()
    {
        bool hasController = posAction.controls.Count > 0;

        Ray ray;
        bool clicked;

        if (hasController)
        {
            Vector3 localPos = posAction.ReadValue<Vector3>();
            Quaternion localRot = rotAction.ReadValue<Quaternion>();
            if (localRot.x == 0f && localRot.y == 0f && localRot.z == 0f && localRot.w == 0f)
                localRot = Quaternion.identity;

            Vector3 pos = xrOrigin != null ? xrOrigin.TransformPoint(localPos) : localPos;
            Quaternion rot = (xrOrigin != null ? xrOrigin.rotation * localRot : localRot)
                             * Quaternion.Euler(rayPitchOffset, 0f, 0f);
            ray = new Ray(pos, rot * Vector3.forward);
            clicked = triggerAction.WasPressedThisFrame();

            line.enabled = true;
            line.SetPosition(0, ray.origin);
            line.SetPosition(1, ray.origin + ray.direction * rayLength);
        }
        else
        {
            line.enabled = false;
            var cam = Camera.main;
            if (cam == null) return;
            ray = new Ray(cam.transform.position, cam.transform.forward);
            clicked = Input.GetMouseButtonDown(0);
        }

        if (!clicked) return;

        if (Physics.Raycast(ray, out RaycastHit hit, rayLength))
        {
            var button = hit.collider.GetComponentInParent<Button>();
            if (button != null && button.interactable)
                button.onClick.Invoke();
        }
    }
}
