using UnityEngine;
using UnityEngine.InputSystem;

// Control con los mandos (Quest real o simulador de XR Interaction Toolkit).
//
// MODO COLOCAR (solo mando derecho):
//   Gatillo : poner punto
//   A       : cambiar tipo (Zombie / Spider)
//   B       : pasar a Jugar (solo administrador)
//   Grip    : toque corto = deshacer último punto | mantener 1,5 s = borrar todos
//
// MODO JUGAR (los dos mandos):
//   Gatillo izquierdo y derecho : disparar (cada mano con su rayo y su cadencia)
//   A, B y Grip están DESHABILITADOS: no hacen nada.
//   La salida al menú la gestiona ExitToMenuGesture.
//
// Usa acciones del Input System sobre <XRController>{RightHand} / {LeftHand}.
// Si no hay ningún mando, en el editor valen atajos de teclado:
//   1 = tipo, Espacio = modo, Backspace = deshacer, Supr = borrar todos.
public class QuestControllerPlacement : MonoBehaviour
{
    [SerializeField] private ARTapSpawnPointProvider provider;
    [SerializeField] private SpawnManager spawnManager;
    [Tooltip("Objeto XR Origin: convierte la pose del mando (espacio de tracking) a mundo")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private float damage = 1f;
    [Tooltip("Segundos mínimos entre disparos de un mismo mando")]
    [SerializeField] private float shootCooldown = 0.25f;
    [Tooltip("Segundos de grip para borrar todos los puntos")]
    [SerializeField] private float clearHoldSeconds = 1.5f;
    [Tooltip("Si en el Quest el rayo sale inclinado respecto a tu mano, corrígelo aquí (grados, en cabeceo)")]
    [SerializeField] private float rayPitchOffset = 0f;

    // Datos de cada mano: pose del rayo, gatillo, línea visible y cadencia
    private class Hand
    {
        public InputAction pos, rot, trigger;
        public LineRenderer line;
        public float nextShootTime;
    }

    private Hand right, left;

    // Botones solo del mando derecho (se deshabilitan en modo Jugar)
    private InputAction gripAction, aAction, bAction;
    private bool buttonsEnabled = true;

    private TextMesh hud;
    private float gripDownTime;

    private void Awake()
    {
        right = MakeHand("RightHand");
        left = MakeHand("LeftHand");

        gripAction = MakeButton("RightHand", "Grip", "{GripButton}", "gripPressed");
        aAction = MakeButton("RightHand", "A", "{PrimaryButton}", "primaryButton");
        bAction = MakeButton("RightHand", "B", "{SecondaryButton}", "secondaryButton");
    }

    private static Hand MakeHand(string side)
    {
        var h = new Hand();

        h.pos = new InputAction("Position" + side, InputActionType.Value, expectedControlType: "Vector3");
        h.pos.AddBinding($"<XRController>{{{side}}}/pointerPosition");

        h.rot = new InputAction("Rotation" + side, InputActionType.Value, expectedControlType: "Quaternion");
        h.rot.AddBinding($"<XRController>{{{side}}}/pointerRotation");

        h.trigger = MakeButton(side, "Trigger" + side, "{TriggerButton}", "triggerPressed");
        return h;
    }

    private static InputAction MakeButton(string side, string name, params string[] controls)
    {
        var action = new InputAction(name, InputActionType.Button);
        foreach (var c in controls)
            action.AddBinding($"<XRController>{{{side}}}/{c}");
        return action;
    }

    private void OnEnable()
    {
        EnableHand(right); EnableHand(left);
        gripAction.Enable(); aAction.Enable(); bAction.Enable();
        buttonsEnabled = true;
    }

    private void OnDisable()
    {
        DisableHand(right); DisableHand(left);
        gripAction.Disable(); aAction.Disable(); bAction.Disable();
    }

    private void OnDestroy()
    {
        DisposeHand(right); DisposeHand(left);
        gripAction.Dispose(); aAction.Dispose(); bAction.Dispose();
    }

    private static void EnableHand(Hand h) { h.pos.Enable(); h.rot.Enable(); h.trigger.Enable(); }
    private static void DisableHand(Hand h) { h.pos.Disable(); h.rot.Disable(); h.trigger.Disable(); }
    private static void DisposeHand(Hand h) { h.pos.Dispose(); h.rot.Dispose(); h.trigger.Dispose(); }

    private void Start()
    {
        right.line = CreateLine("ControllerRay_Right");
        left.line = CreateLine("ControllerRay_Left");

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

    private LineRenderer CreateLine(string name)
    {
        var lineGo = new GameObject(name);
        lineGo.transform.SetParent(transform, false);
        var line = lineGo.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = line.endWidth = 0.006f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.enabled = false;
        return line;
    }

    private void Update()
    {
        if (provider == null) return;

        bool hasRight = right.pos.controls.Count > 0;
        bool hasLeft = left.pos.controls.Count > 0;
        bool hasController = hasRight || hasLeft;

        // Con mando presente, el provider ignora el ratón (así no se coloca dos veces en el editor)
        provider.suppressMouseInput = hasController;

        if (!hasController)
        {
            right.line.enabled = false;
            left.line.enabled = false;
            EditorShortcuts();
            UpdateHud();
            return;
        }

        bool placing = provider.CurrentMode == ARTapSpawnPointProvider.Mode.Place;

        // En Jugar, A / B / Grip ni se leen: no pueden desencadenar nada
        SetButtonsEnabled(placing);

        if (placing)
        {
            left.line.enabled = false;
            UpdatePlaceRight();
        }
        else
        {
            UpdatePlayHand(right);
            UpdatePlayHand(left);
        }

        UpdateHud();
    }

    // ---- Modo Colocar: solo mando derecho ----

    private void UpdatePlaceRight()
    {
        if (!TryGetRay(right, out Ray ray))
        {
            right.line.enabled = false;
            return;
        }

        ShowRay(right, ray, Color.cyan);

        if (right.trigger.WasPressedThisFrame()) provider.PlaceFromRay(ray);

        if (aAction.WasPressedThisFrame()) CycleType();

        if (bAction.WasPressedThisFrame()) ToggleMode();

        // Grip: toque corto = deshacer, mantener = borrar todos
        if (gripAction.WasPressedThisFrame()) gripDownTime = Time.time;
        if (gripAction.WasReleasedThisFrame())
        {
            if (Time.time - gripDownTime >= clearHoldSeconds) provider.ClearAll();
            else provider.UndoLast();
        }
    }

    // ---- Modo Jugar: los dos mandos, solo gatillo ----

    private void UpdatePlayHand(Hand h)
    {
        if (!TryGetRay(h, out Ray ray))
        {
            h.line.enabled = false;
            return;
        }

        ShowRay(h, ray, Color.red);

        if (h.trigger.WasPressedThisFrame() && Time.time >= h.nextShootTime)
        {
            h.nextShootTime = Time.time + shootCooldown;
            Shoot(ray);
        }
    }

    // ---- Utilidades ----

    private void SetButtonsEnabled(bool enabled)
    {
        if (enabled == buttonsEnabled) return;
        buttonsEnabled = enabled;

        if (enabled)
        {
            gripAction.Enable(); aAction.Enable(); bAction.Enable();
        }
        else
        {
            gripAction.Disable(); aAction.Disable(); bAction.Disable();
        }
    }

    private bool TryGetRay(Hand h, out Ray ray)
    {
        ray = default;
        if (h.pos.controls.Count == 0) return false;

        Vector3 localPos = h.pos.ReadValue<Vector3>();
        Quaternion localRot = h.rot.ReadValue<Quaternion>();
        if (localRot.x == 0f && localRot.y == 0f && localRot.z == 0f && localRot.w == 0f)
            localRot = Quaternion.identity;

        Vector3 pos = xrOrigin != null ? xrOrigin.TransformPoint(localPos) : localPos;
        Quaternion rot = (xrOrigin != null ? xrOrigin.rotation * localRot : localRot) * Quaternion.Euler(rayPitchOffset, 0f, 0f);
        ray = new Ray(pos, rot * Vector3.forward);
        return true;
    }

    private void ShowRay(Hand h, Ray ray, Color color)
    {
        h.line.enabled = true;
        h.line.SetPosition(0, ray.origin);
        h.line.SetPosition(1, ray.origin + ray.direction * rayLength);
        h.line.startColor = h.line.endColor = color;
    }

    private void CycleType()
    {
        int count = System.Enum.GetValues(typeof(MonsterType)).Length;
        provider.SetType((MonsterType)(((int)provider.SelectedType + 1) % count));
    }

    private void ToggleMode()
    {
        if (!GameSession.IsAdmin) return;

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

    // Solo en el editor y sin mando: 1 = cambiar tipo, Espacio = alternar modo, Backspace = deshacer, Supr = borrar todos.
    private void EditorShortcuts()
    {
#if UNITY_EDITOR
        var kb = Keyboard.current;
        if (kb == null) return;

        bool placing = provider.CurrentMode == ARTapSpawnPointProvider.Mode.Place;
        if (kb.digit1Key.wasPressedThisFrame && placing) CycleType();
        if (kb.spaceKey.wasPressedThisFrame) ToggleMode();
        if (kb.backspaceKey.wasPressedThisFrame && placing) provider.UndoLast();
        if (kb.deleteKey.wasPressedThisFrame && placing) provider.ClearAll();
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