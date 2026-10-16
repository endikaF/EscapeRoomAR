using UnityEngine;

// Simula un ancla en el editor. Crea un GameObject vacío, ponle este script,
// elige el tipo y colócalo donde quieras que aparezca el monstruo.
public class MockSpawnMarker : MonoBehaviour
{
    public MonsterType type = MonsterType.Zombie;

    private void OnDrawGizmos()
    {
        Gizmos.color = type == MonsterType.Zombie ? Color.green : Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.25f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.6f);
    }
}
