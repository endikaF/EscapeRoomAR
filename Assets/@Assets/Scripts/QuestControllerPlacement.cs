using UnityEngine;
using UnityEngine.InputSystem;

// Control con el mando derecho (Quest real o simulador de XR Interaction Toolkit).
//   Gatillo : modo Colocar = poner punto | modo Jugar = disparar
//   A       : cambiar tipo (Zombie / Spider)
//   B       : alternar Colocar / Jugar
//   Grip    : deshacer último punto
// Usa acciones del Input System sobre <XRController>{RightHand}, que es lo que exponen tanto el plugin OpenXR
// como el simulador de XRI. Si no hay ningún mando, en el editor valen atajos de teclado: 1 = tipo, Espacio = modo, Backspace = deshacer.
public class QuestControllerPlacement : MonoBehaviour
{
    [SerializeField] private ARTapSpawnPointProvider provider;
    [SerializeField] private SpawnManager spawnManager;
    [Tooltip("Objeto XR Origin: convierte la pose del mando (espacio de tracking) a mundo")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private float damage = 1f;
    [Tooltip("Si en el Quest el rayo sale inclinado respecto a tu mano, corrígelo aquí (grados, en cabeceo)")]
    [SerializeField] private float rayPitchOffset = 0f;

    private InputAction posAction, rotAction, triggerAction, gripAction, aAction, bAction;
    private LineRenderer line;
    private TextMesh hud;

    private void Awake()
    {
        posAction = new InputAction("Position", InputActionType.Value, expectedControlType: "Vector3");
        posAction.AddBinding("<XRController>{RightHand}/devicePosition");

        rotAction = new InputAction("Rotation", InputActionType.Value, expectedControlType: "Quaternion");
        rotAction.AddBinding("<XRController>{RightHand}/deviceRotation");

        triggerAction = MakeButton("Trigger", "{TriggerButton}", "triggerPressed");
        gripAction = MakeButton("Grip", "{GripButton}", "gripPressed");
        aAction = MakeButton("A", "{PrimaryButton}", "primaryButton");
        bAction = MakeButton("B", "{SecondaryButton}", "secondaryButton");
    }

    private static InputAction MakeButton(string name, params string[] controls)
    {
        var action = new InputAction(name, InputActionType.Button);
        foreach (var c in controls)
            action.AddBinding($"<XRController>{{RightHand}}/{c}");
        return action;
    }

    private void OnEnable()
    {
        posAction.Enable(); rotAction.Enable();
        triggerAction.Enable(); gripAction.Enable(); aAction.Enable(); bAction.Enable();
    }

    private void OnDisable()
    {
        posAction.Disable(); rotAction.Disable();
        triggerAction.Disable(); gripAction.Disable(); aAction.Disable(); bAction.Disable();
    }

    private void OnDestroy()
    {
        posAction.Dispose(); rotAction.Dispose();
        triggerAction.Dispose(); gripAction.Dispose(); aAction.Dispose(); bAction.Dispose();
    }

    private void Start()
    {
        // Línea del rayo
        var lineGo = new GameObject("ControllerRay");
        lineGo.transform.SetParent(transform, false);
        line = lineGo.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = line.endWidth = 0.006f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.enabled = false;

        // Texto de estado pegado a la cámara
        var cam = Camera.main;
        if (cam != null)
        {
            var go = new GameObject("StatusText");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0f, -0.22f, 1.0f);
            hud = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hud.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            hud.anchor = TextAnchor.MiddleCenter;
            hud.alignment = TextAlignment.Center;
            hud.characterSize = 0.012f;
            hud.fontSize = 48;
            hud.color = Color.white;
        }
    }

    private void Update()
    {
        if (provider == null) return;

        bool hasController = posAction.controls.Count > 0;

        // Con mando presente, el provider ignora el ratón (así no se coloca dos veces en el editor)
        provider.suppressMouseInput = hasController;

        if (!hasController)
        {
            line.enabled = false;
            EditorShortcuts();
            UpdateHud();
            return;
        }

        Vector3 localPos = posAction.ReadValue<Vector3>();
        Quaternion localRot = rotAction.ReadValue<Quaternion>();
        if (localRot.x == 0f && localRot.y == 0f && localRot.z == 0f && localRot.w == 0f)
            localRot = Quaternion.identity;

        Vector3 pos = xrOrigin != null ? xrOrigin.TransformPoint(localPos) : localPos;
        Quaternion rot = (xrOrigin != null ? xrOrigin.rotation * localRot : localRot) * Quaternion.Euler(rayPitchOffset, 0f, 0f);
        var ray = new Ray(pos, rot * Vector3.forward);

        bool placing = provider.CurrentMode == ARTapSpawnPointProvider.Mode.Place;

        // Rayo visible
        line.enabled = true;
        line.SetPosition(0, ray.origin);
        line.SetPosition(1, ray.origin + ray.direction * rayLength);
        line.startColor = line.endColor = placing ? Color.cyan : Color.red;

        // Gatillo
        if (triggerAction.WasPressedThisFrame())
        {
            if (placing) provider.PlaceFromRay(ray);
            else Shoot(ray);
        }

        // A: cambiar tipo
        if (aAction.WasPressedThisFrame() && placing) CycleType();

        // B: alternar modo
        if (bAction.WasPressedThisFrame()) ToggleMode();

        // Grip: deshacer
        if (gripAction.WasPressedThisFrame() && placing) provider.UndoLast();

        UpdateHud();
    }

    private void CycleType()
    {
        provider.SetType(provider.SelectedType == MonsterType.Zombie ? MonsterType.Spider : MonsterType.Zombie);
    }

    private void ToggleMode()
    {
        if (provider.CurrentMode == ARTapSpawnPointProvider.Mode.Place)
        {
            provider.SetMode(ARTapSpawnPointProvider.Mode.Play);
        }
        else
        {
            if (spawnManager != null) spawnManager.ResetSpawns();
            provider.SetMode(ARTapSpawnPointProvider.Mode.Place);
        }
    }

    // Solo en el editor y sin mando: 1 = cambiar tipo, Espacio = alternar modo, Backspace = deshacer.
    private void EditorShortcuts()
    {
#if UNITY_EDITOR
        var kb = Keyboard.current;
        if (kb == null) return;

        bool placing = provider.CurrentMode == ARTapSpawnPointProvider.Mode.Place;
        if (kb.digit1Key.wasPressedThisFrame && placing) CycleType();
        if (kb.spaceKey.wasPressedThisFrame) ToggleMode();
        if (kb.backspaceKey.wasPressedThisFrame && placing) provider.UndoLast();
#endif
    }

    private void Shoot(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, rayLength))
        {
            var health = hit.collider.GetComponentInParent<MonsterHealth>();
            if (health != null) health.TakeDamage(damage);
        }
    }

    private void UpdateHud()
    {
        if (hud == null || provider == null) return;

        string mode = provider.CurrentMode == ARTapSpawnPointProvider.Mode.Place ? "COLOCAR" : "JUGAR";
        hud.text = $"{mode} | {provider.SelectedType} | Puntos: {provider.PointCount}\n{provider.LastMessage}";
    }
}