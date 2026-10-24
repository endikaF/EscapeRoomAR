using System.Collections.Generic;
using UnityEngine;

// Para cada punto aún no activado, si el jugador se acerca, instancia el prefab de su tipo.
// Solo habla con ISpawnPointProvider y MonsterRegistry: no sabe nada de anclas ni de Meta.
public class SpawnManager : MonoBehaviour
{
    [Tooltip("Arrastra aquí el componente que implementa ISpawnPointProvider (p. ej. MockSpawnPointProvider)")]
    [SerializeField] private MonoBehaviour providerSource;
    [SerializeField] private MonsterRegistry registry;
    [SerializeField] private float activationDistance = 3f;

    private ISpawnPointProvider provider;
    private readonly HashSet<string> usedPoints = new HashSet<string>();
    private readonly List<GameObject> spawned = new List<GameObject>();

    private void Awake()
    {
        provider = providerSource as ISpawnPointProvider;
        if (provider == null)
            Debug.LogError("SpawnManager: 'providerSource' no implementa ISpawnPointProvider.");
    }

    private void Update()
    {
        if (provider == null || registry == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 player = cam.transform.position;

        foreach (var p in provider.GetPoints())
        {
            if (usedPoints.Contains(p.id)) continue;

            // Distancia horizontal: ignora la altura de la cabeza respecto al ancla
            Vector3 a = new Vector3(player.x, 0f, player.z);
            Vector3 b = new Vector3(p.position.x, 0f, p.position.z);
            if (Vector3.Distance(a, b) > activationDistance) continue;

            Spawn(p);
        }
    }

    private void Spawn(SpawnPointData p)
    {
        usedPoints.Add(p.id);

        GameObject prefab = registry.GetPrefab(p.type);
        if (prefab == null) return;

        spawned.Add(Instantiate(prefab, p.position, p.rotation));
    }

    // Útil para reiniciar la partida (botón, tecla, etc.)
    public void ResetSpawns()
    {
        foreach (var go in spawned)
            if (go != null) Destroy(go);
        spawned.Clear();
        usedPoints.Clear();
    }

    // true cuando hay puntos y todos han hecho aparecer su monstruo
    public bool AllPointsSpawned
    {
        get
        {
            if (provider == null) return false;
            var pts = provider.GetPoints();
            if (pts.Count == 0) return false;
            foreach (var p in pts)
                if (!usedPoints.Contains(p.id)) return false;
            return true;
        }
    }
}
