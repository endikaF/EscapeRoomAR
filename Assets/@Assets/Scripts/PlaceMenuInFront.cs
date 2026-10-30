using System.Collections;
using UnityEngine;

// Coloca el menú (Canvas en World Space) delante del jugador al abrir la escena.
public class PlaceMenuInFront : MonoBehaviour
{
    [SerializeField] private float distance = 1.5f;
    [Tooltip("Altura respecto a los ojos (negativo = más bajo)")]
    [SerializeField] private float heightOffset = -0.1f;
    [Tooltip("Segundos de espera para que el tracking del casco esté listo")]
    [SerializeField] private float initialDelay = 0.5f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(initialDelay);

        var cam = Camera.main;
        while (cam == null) { yield return null; cam = Camera.main; }

        // Dirección horizontal hacia donde miras (ignora el cabeceo)
        Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        fwd.Normalize();

        Vector3 pos = cam.transform.position + fwd * distance;
        pos.y = cam.transform.position.y + heightOffset;

        transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd, Vector3.up));
    }
}