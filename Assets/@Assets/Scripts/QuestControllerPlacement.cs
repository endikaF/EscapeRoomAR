using UnityEngine;
using UnityEngine.XR;

// Control con el mando derecho del Quest (sustituye a la UI táctil y a ClickToShoot).
//   Gatillo : modo Colocar = poner punto | modo Jugar = disparar
//   A       : cambiar tipo (Zombie / Spider)
//   B       : alternar Colocar / Jugar
//   Grip    : deshacer último punto
// Lee el mando con UnityEngine.XR.InputDevices, así que no necesita prefabs de mandos.
public class QuestControllerPlacement : MonoBehaviour
{
    [SerializeField] private ARTapSpawnPointProvider provider;
    [SerializeField] private SpawnManager spawnManager;
    [Tooltip("Objeto XR Origin: convierte la pose del mando (espacio de tracking) a mundo")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private float damage = 1f;

    private InputDevice right;
    private LineRenderer line;
    private TextMesh hud;
    private bool prevTrigger, prevA, prevB, prevGrip;

    private void Start()
    {
        // Línea del rayo
        var lineGo = new GameObject("ControllerRay");
        lineGo.transform.SetParent(transform, false);
        line = lineGo.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = line.endWidth = 0.006f;
        line.material = new Material(Shader.Find("Sprites/Default"));

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

        if (!right.isValid) right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!right.isValid) { EditorShortcuts(); UpdateHud(); return; }

        if (!right.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPos) ||
            !right.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localRot))
        {
            UpdateHud();
            return;
        }

        Vector3 pos = xrOrigin != null ? xrOrigin.TransformPoint(localPos) : localPos;
        Quaternion rot = xrOrigin != null ? xrOrigin.rotation * localRot : localRot;
        var ray = new Ray(pos, rot * Vector3.forward);

        bool placing = provider.CurrentMode == ARTapSpawnPointProvider.Mode.Place;

        // Rayo visible
        line.SetPosition(0, ray.origin);
        line.SetPosition(1, ray.origin + ray.direction * rayLength);
        line.startColor = line.endColor = placing ? Color.cyan : Color.red;

        // Gatillo
        if (Pressed(CommonUsages.triggerButton, ref prevTrigger))
        {
            if (placing) provider.PlaceFromRay(ray);
            else Shoot(ray);
        }

        // A: cambiar tipo
        if (Pressed(CommonUsages.primaryButton, ref prevA) && placing)
            CycleType();

        // B: alternar modo
        if (Pressed(CommonUsages.secondaryButton, ref prevB))
            ToggleMode();

        // Grip: deshacer
        if (Pressed(CommonUsages.gripButton, ref prevGrip) && placing)
            provider.UndoLast();

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

    // Solo en el editor (XR Simulation, sin mando): 1 = cambiar tipo, Espacio = alternar modo, Backspace = deshacer.
    // Colocar puntos con clic izquierdo lo hace ARTapSpawnPointProvider; disparar, ClickToShoot.
    private void EditorShortcuts()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1) && provider.CurrentMode == ARTapSpawnPointProvider.Mode.Place) CycleType();
        if (Input.GetKeyDown(KeyCode.Space)) ToggleMode();
        if (Input.GetKeyDown(KeyCode.Backspace) && provider.CurrentMode == ARTapSpawnPointProvider.Mode.Place) provider.UndoLast();
#endif
    }

    private bool Pressed(InputFeatureUsage<bool> usage, ref bool previous)
    {
        right.TryGetFeatureValue(usage, out bool now);
        bool down = now && !previous;
        previous = now;
        return down;
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