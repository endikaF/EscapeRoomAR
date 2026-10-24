using System.Collections.Generic;
using UnityEngine;

// Crear el asset: clic derecho en Project > Create > Halloween > Monster Registry
[CreateAssetMenu(menuName = "Halloween/Monster Registry", fileName = "MonsterRegistry")]
public class MonsterRegistry : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public MonsterType type;
        public GameObject prefab;
        [Tooltip("0 = usar la distancia por defecto del SpawnManager")]
        public float activationDistance = 0f;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public GameObject GetPrefab(MonsterType type)
    {
        foreach (var e in entries)
            if (e.type == type) return e.prefab;

        Debug.LogWarning($"MonsterRegistry: no hay prefab asignado para {type}");
        return null;
    }

    public float GetActivationDistance(MonsterType type, float defaultDistance)
    {
        foreach (var e in entries)
            if (e.type == type && e.activationDistance > 0f) return e.activationDistance;
        return defaultDistance;
    }
}
