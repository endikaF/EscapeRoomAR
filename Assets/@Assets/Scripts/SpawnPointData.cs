using UnityEngine;

// Estructura simple que usa todo el juego. Da igual si viene de un mock (editor)
// o de un OVRSpatialAnchor real (Quest): el SpawnManager solo conoce esto.
[System.Serializable]
public struct SpawnPointData
{
    public string id;
    public Vector3 position;
    public Quaternion rotation;
    public MonsterType type;
}
