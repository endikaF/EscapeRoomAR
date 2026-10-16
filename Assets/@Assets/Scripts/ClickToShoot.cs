using UnityEngine;

// Arma de prueba: clic izquierdo = raycast desde la cámara. Ponlo en la Main Camera.
// En el Quest lo sustituirás por el disparo con el mando.
public class ClickToShoot : MonoBehaviour
{
    [SerializeField] private float range = 30f;
    [SerializeField] private float damage = 1f;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        var cam = Camera.main;
        if (cam == null) return;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, range))
        {
            var health = hit.collider.GetComponentInParent<MonsterHealth>();
            if (health != null) health.TakeDamage(damage);
        }
    }
}
