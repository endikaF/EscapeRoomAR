using System.Collections.Generic;
using UnityEngine;

// Lee todos los MockSpawnMarker de la escena y los expone como puntos de spawn.
public class MockSpawnPointProvider : MonoBehaviour, ISpawnPointProvider
{
    private readonly List<SpawnPointData> points = new List<SpawnPointData>();

    public IReadOnlyList<SpawnPointData> GetPoints()
    {
        points.Clear();
        var markers = FindObjectsByType<MockSpawnMarker>(FindObjectsSortMode.None);
        foreach (var m in markers)
        {
            points.Add(new SpawnPointData
            {
                id = m.gameObject.GetInstanceID().ToString(),
                position = m.transform.position,
                rotation = m.transform.rotation,
                type = m.type
            });
        }
        return points;
    }
}
