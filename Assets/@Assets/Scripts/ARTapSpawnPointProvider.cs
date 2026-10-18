using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Proveedor de puntos para móvil/tablet.
// Modo Place: tocas la pantalla, se hace un raycast contra los planos detectados y se guarda un punto con el tipo elegido.
// Modo Play: devuelve esos puntos al SpawnManager (en modo Place devuelve una lista vacía para que no aparezca nada).
public class ARTapSpawnPointProvider : MonoBehaviour, ISpawnPointProvider
{
    public enum Mode { Place, Play }

    [SerializeField] private ARRaycastManager raycastManager;
    [Tooltip("Opcional: si se asigna, en modo Jugar se deja de detectar y se ocultan los planos")]
    [SerializeField] private ARPlaneManager planeManager;
    [Tooltip("En modo Jugar: true = ocultar los planos; false = dejarlos visibles pero congelados")]
    [SerializeField] private bool hidePlanesWhilePlaying = true;
    [Tooltip("Arrastra aquí el objeto XR Origin, para que los puntos se muevan con el espacio AR")]
    [SerializeField] private Transform pointsParent;
    [SerializeField] private bool showMarkersWhilePlaying = false;

    public Mode CurrentMode { get; private set; } = Mode.Place;
    public MonsterType SelectedType { get; private set; } = MonsterType.Zombie;
    public string LastMessage { get; private set; } = "Mueve el móvil para escanear el suelo y las paredes.";
    public int PointCount => placed.Count;

    // Lo rellena el HUD: los toques en esta franja inferior de la pantalla se ignoran (son de los botones)
    [NonSerialized] public float ignoreBottomPixels;

    private class Placed
    {
        public string id;
        public MonsterType type;
        public Transform root;
        public GameObject visual;
    }

    private readonly List<Placed> placed = new List<Placed>();
    private readonly List<SpawnPointData> cache = new List<SpawnPointData>();
    private static readonly List<SpawnPointData> empty = new List<SpawnPointData>();
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Update()
    {
        if (CurrentMode != Mode.Place) return;
        if (TryGetTap(out Vector2 screenPos)) TryPlace(screenPos);
    }

    private bool TryGetTap(out Vector2 pos)
    {
        pos = default;
#if UNITY_EDITOR
        // En el editor (por ejemplo con XR Simulation) se usa el ratón
        if (Input.GetMouseButtonDown(0))
        {
            pos = Input.mousePosition;
            return pos.y > ignoreBottomPixels;
        }
#else
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                pos = t.position;
                return pos.y > ignoreBottomPixels;
            }
        }
#endif
        return false;
    }

    private void TryPlace(Vector2 screenPos)
    {
        if (raycastManager == null)
        {
            LastMessage = "Falta asignar el AR Raycast Manager.";
            return;
        }

        if (!raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
        {
            LastMessage = "No hay plano ahí. Mueve el móvil para seguir escaneando.";
            return;
        }

        Pose pose = hits[0].pose;

        // Orientar el punto mirando hacia el jugador (solo en horizontal)
        Quaternion rot = pose.rotation;
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 toPlayer = cam.transform.position - pose.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f) rot = Quaternion.LookRotation(toPlayer);
        }

        var go = new GameObject($"Point_{SelectedType}");
        if (pointsParent != null) go.transform.SetParent(pointsParent, true);
        go.transform.SetPositionAndRotation(pose.position, rot);

        var p = new Placed
        {
            id = Guid.NewGuid().ToString(),
            type = SelectedType,
            root = go.transform,
            visual = CreateVisual(go.transform, SelectedType)
        };
        placed.Add(p);

        LastMessage = $"Punto {SelectedType} colocado.";
    }

    private GameObject CreateVisual(Transform parent, MonsterType type)
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(s.GetComponent<Collider>());
        s.transform.SetParent(parent, false);
        s.transform.localScale = Vector3.one * 0.15f;
        s.transform.localPosition = Vector3.up * 0.1f;
        s.GetComponent<Renderer>().material.color = type == MonsterType.Zombie ? Color.green : Color.magenta;
        return s;
    }

    // ---- API para el HUD ----

    public void SetType(MonsterType type) => SelectedType = type;

    public void SetMode(Mode mode)
    {
        CurrentMode = mode;
        bool visible = mode == Mode.Place || showMarkersWhilePlaying;
        foreach (var p in placed)
            if (p.visual != null) p.visual.SetActive(visible);

        // En modo Jugar: no detectar más planos (y ocultarlos o no, según la opción)
        if (planeManager != null)
        {
            bool scanning = mode == Mode.Place;
            planeManager.enabled = scanning;                       // deja de detectar/actualizar planos
            planeManager.SetTrackablesActive(scanning || !hidePlanesWhilePlaying); // visibilidad
        }

        LastMessage = mode == Mode.Place
            ? "Modo colocar: toca un plano para añadir un punto."
            : "Modo jugar: acércate a los puntos.";
    }

    public void UndoLast()
    {
        if (placed.Count == 0) return;
        var last = placed[placed.Count - 1];
        if (last.root != null) Destroy(last.root.gameObject);
        placed.RemoveAt(placed.Count - 1);
        LastMessage = "Último punto borrado.";
    }

    public void ClearAll()
    {
        foreach (var p in placed)
            if (p.root != null) Destroy(p.root.gameObject);
        placed.Clear();
        LastMessage = "Todos los puntos borrados.";
    }

    // ---- ISpawnPointProvider ----

    public IReadOnlyList<SpawnPointData> GetPoints()
    {
        if (CurrentMode != Mode.Play) return empty;

        cache.Clear();
        foreach (var p in placed)
        {
            if (p.root == null) continue;
            cache.Add(new SpawnPointData
            {
                id = p.id,
                position = p.root.position,
                rotation = p.root.rotation,
                type = p.type
            });
        }
        return cache;
    }
}