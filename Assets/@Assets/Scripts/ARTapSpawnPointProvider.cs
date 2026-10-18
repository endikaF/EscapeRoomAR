using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Proveedor de puntos para móvil/tablet y Quest.
// Modo Place: colocas puntos sobre planos detectados (toque, ratón o rayo del mando).
// Modo Play : entrega esos puntos al SpawnManager (en modo Place devuelve una lista vacía).
//
// Persistencia (solo si hay un ARAnchorManager asignado y el dispositivo soporta guardar anclas, p. ej. Quest 3):
//  - Cada punto es un ARAnchor guardado con TrySaveAnchorAsync.
//  - El GUID del ancla y el tipo de monstruo se guardan en un JSON en Application.persistentDataPath.
//  - Al abrir la app se leen los GUID y se cargan con TryLoadAnchorAsync.
// Sin ARAnchorManager (móvil) funciona como antes: puntos sin guardar.
public class ARTapSpawnPointProvider : MonoBehaviour, ISpawnPointProvider
{
    public enum Mode { Place, Play }

    [Header("AR")]
    [SerializeField] private ARRaycastManager raycastManager;
    [Tooltip("Opcional: si se asigna, en modo Jugar se deja de detectar planos")]
    [SerializeField] private ARPlaneManager planeManager;
    [Tooltip("En modo Jugar: true = ocultar los planos; false = dejarlos visibles pero congelados")]
    [SerializeField] private bool hidePlanesWhilePlaying = true;
    [Tooltip("Solo sin anclas: objeto XR Origin, para que los puntos se muevan con el espacio AR")]
    [SerializeField] private Transform pointsParent;
    [SerializeField] private bool showMarkersWhilePlaying = false;

    [Header("Persistencia (Quest)")]
    [Tooltip("Opcional: AR Anchor Manager del XR Origin. Si se deja vacío no hay persistencia (móvil)")]
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private string saveFileName = "spawnpoints.json";
    [SerializeField] private int loadRetries = 5;
    [SerializeField] private float loadRetryDelay = 3f;

    public Mode CurrentMode { get; private set; } = Mode.Place;
    public MonsterType SelectedType { get; private set; } = MonsterType.Zombie;
    public string LastMessage { get; private set; } = "Mueve el dispositivo para escanear el suelo y las paredes.";
    public int PointCount => placed.Count;

    // Lo rellena el HUD móvil: los toques en esta franja inferior de la pantalla se ignoran
    [NonSerialized] public float ignoreBottomPixels;

    // Lo pone a true QuestControllerPlacement cuando hay un mando (real o simulado): ignora el ratón en el editor
    [NonSerialized] public bool suppressMouseInput;

    // ---- Datos ----

    private class Placed
    {
        public string id;
        public MonsterType type;
        public Transform root;
        public GameObject visual;
        public ARAnchor anchor;
        public SerializableGuid guid;
        public bool saved;
    }

    [Serializable]
    public class SavedPoint
    {
        public SerializableGuid guid;
        public int type;
    }

    [Serializable]
    public class SaveData
    {
        public List<SavedPoint> points = new List<SavedPoint>();
    }

    private readonly List<Placed> placed = new List<Placed>();
    private List<SavedPoint> unloaded = new List<SavedPoint>(); // guardados que aún no se han podido cargar
    private readonly List<SpawnPointData> cache = new List<SpawnPointData>();
    private static readonly List<SpawnPointData> empty = new List<SpawnPointData>();
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    // ---- Ciclo de vida ----

    private async void Start()
    {
        if (anchorManager == null) return;

        try { await LoadSavedPoints(); }
        catch (Exception e) { Debug.LogWarning($"Carga de puntos guardados: {e.Message}"); }
    }

    private void Update()
    {
        if (CurrentMode != Mode.Place) return;
        if (TryGetTap(out Vector2 screenPos)) TryPlace(screenPos);
    }

    // ---- Entrada ----

    private bool TryGetTap(out Vector2 pos)
    {
        pos = default;
#if UNITY_EDITOR
        // En el editor (por ejemplo con XR Simulation) se usa el ratón
        if (!suppressMouseInput && Input.GetMouseButtonDown(0))
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
            LastMessage = "No hay plano ahí. Mueve el dispositivo para seguir escaneando.";
            return;
        }

        PlacePose(hits[0].pose);
    }

    // Colocar desde un rayo en el mundo (mando del Quest)
    public void PlaceFromRay(Ray ray)
    {
        if (CurrentMode != Mode.Place) return;

        if (raycastManager == null)
        {
            LastMessage = "Falta asignar el AR Raycast Manager.";
            return;
        }

        if (!raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon))
        {
            LastMessage = "No hay plano ahí. Apunta a una pared, suelo o techo.";
            return;
        }

        PlacePose(hits[0].pose);
    }

    // ---- Colocar ----

    private async void PlacePose(Pose pose)
    {
        MonsterType type = SelectedType;

        // Orientar el punto mirando hacia el jugador (solo en horizontal)
        Quaternion rot = pose.rotation;
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 toPlayer = cam.transform.position - pose.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f) rot = Quaternion.LookRotation(toPlayer);
        }

        try
        {
            Transform root;
            ARAnchor anchor = null;

            if (anchorManager != null)
            {
                var result = await anchorManager.TryAddAnchorAsync(new Pose(pose.position, rot));
                if (!result.status.IsSuccess())
                {
                    LastMessage = "No se pudo crear el ancla.";
                    return;
                }
                anchor = result.value;
                root = anchor.transform;
            }
            else
            {
                var go = new GameObject($"Point_{type}");
                if (pointsParent != null) go.transform.SetParent(pointsParent, true);
                go.transform.SetPositionAndRotation(pose.position, rot);
                root = go.transform;
            }

            var p = new Placed
            {
                id = anchor != null ? anchor.trackableId.ToString() : Guid.NewGuid().ToString(),
                type = type,
                root = root,
                anchor = anchor
            };
            p.visual = CreateVisual(root, type);
            p.visual.SetActive(CurrentMode == Mode.Place || showMarkersWhilePlaying);
            placed.Add(p);

            LastMessage = $"Punto {type} colocado.";

            if (anchor != null && PersistenceAvailable())
                await SavePoint(p);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PlacePose: {e.Message}");
            LastMessage = "Error al colocar el punto.";
        }
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

    // ---- Persistencia ----

    private bool PersistenceAvailable()
    {
        if (anchorManager == null || !anchorManager.enabled) return false;
        var d = anchorManager.descriptor;
        return d != null && d.supportsSaveAnchor && d.supportsLoadAnchor;
    }

    private async Awaitable SavePoint(Placed p)
    {
        if (p.anchor == null) return;

        var res = await anchorManager.TrySaveAnchorAsync(p.anchor);

        // Si lo borraron mientras se guardaba, limpiar lo guardado
        if (p.root == null)
        {
            if (res.status.IsSuccess()) EraseSaved(res.value);
            return;
        }

        if (!res.status.IsSuccess())
        {
            LastMessage = "Punto colocado, pero no se pudo guardar.";
            return;
        }

        p.guid = res.value;
        p.saved = true;
        WriteFile();
        LastMessage = $"Punto {p.type} guardado.";
    }

    private async Awaitable LoadSavedPoints()
    {
        // Esperar a que el tracking esté listo (máx. 20 s)
        float waited = 0f;
        while (ARSession.state != ARSessionState.SessionTracking && waited < 20f)
        {
            waited += Time.unscaledDeltaTime;
            await Awaitable.NextFrameAsync();
        }

        if (!PersistenceAvailable())
        {
            LastMessage = "Este dispositivo no permite guardar anclas: los puntos no se guardarán.";
            return;
        }

        unloaded = ReadFile().points;
        int total = unloaded.Count;
        if (total == 0) return;

        for (int attempt = 0; attempt < loadRetries && unloaded.Count > 0; attempt++)
        {
            LastMessage = $"Cargando puntos guardados ({total - unloaded.Count}/{total})...";

            for (int i = unloaded.Count - 1; i >= 0; i--)
            {
                var sp = unloaded[i];
                var guid = sp.guid;
                var res = await anchorManager.TryLoadAnchorAsync(guid);
                if (res.status.IsSuccess())
                {
                    RegisterLoaded(res.value, (MonsterType)sp.type, guid);
                    unloaded.RemoveAt(i);
                }
            }

            if (unloaded.Count > 0) await Awaitable.WaitForSecondsAsync(loadRetryDelay);
        }

        LastMessage = unloaded.Count == 0
            ? $"{placed.Count} puntos cargados."
            : $"{placed.Count} cargados, {unloaded.Count} sin cargar. Mira por la habitación o reabre la app.";
    }

    private void RegisterLoaded(ARAnchor anchor, MonsterType type, SerializableGuid guid)
    {
        var p = new Placed
        {
            id = guid.ToString(),
            type = type,
            root = anchor.transform,
            anchor = anchor,
            guid = guid,
            saved = true
        };
        p.visual = CreateVisual(anchor.transform, type);
        p.visual.SetActive(CurrentMode == Mode.Place || showMarkersWhilePlaying);
        placed.Add(p);
    }

    private async void EraseSaved(SerializableGuid guid)
    {
        try
        {
            var d = anchorManager != null ? anchorManager.descriptor : null;
            if (d != null && d.supportsEraseAnchor)
                await anchorManager.TryEraseAnchorAsync(guid);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Borrado de ancla: {e.Message}");
        }
    }

    private SaveData ReadFile()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (data != null && data.points != null) return data;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Lectura de {saveFileName}: {e.Message}");
        }
        return new SaveData();
    }

    private void WriteFile()
    {
        if (anchorManager == null) return;

        var data = new SaveData();
        foreach (var p in placed)
        {
            if (!p.saved) continue;
            data.points.Add(new SavedPoint
            {
                guid = p.guid,
                type = (int)p.type
            });
        }
        data.points.AddRange(unloaded); // los que aún no han cargado no se pierden

        try { File.WriteAllText(SavePath, JsonUtility.ToJson(data)); }
        catch (Exception e) { Debug.LogWarning($"Escritura de {saveFileName}: {e.Message}"); }
    }

    // ---- API para el HUD y el mando ----

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
            planeManager.enabled = scanning;
            planeManager.SetTrackablesActive(scanning || !hidePlanesWhilePlaying);
        }

        LastMessage = mode == Mode.Place
            ? "Modo colocar: apunta a un plano para añadir un punto."
            : "Modo jugar: acércate a los puntos.";
    }

    public void UndoLast()
    {
        if (placed.Count == 0) return;

        var last = placed[placed.Count - 1];
        placed.RemoveAt(placed.Count - 1);
        RemovePoint(last);
        WriteFile();
        LastMessage = "Último punto borrado.";
    }

    public void ClearAll()
    {
        foreach (var p in placed) RemovePoint(p);
        placed.Clear();

        // También olvida los guardados que no llegaron a cargar
        foreach (var sp in unloaded)
            EraseSaved(sp.guid);
        unloaded.Clear();

        WriteFile();
        LastMessage = "Todos los puntos borrados.";
    }

    private void RemovePoint(Placed p)
    {
        if (p.saved) EraseSaved(p.guid);
        if (p.root != null) Destroy(p.root.gameObject);
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