using System.Collections.Generic;
using UnityEngine;

// Bandada de murciélagos. Va en un prefab vacío (BatSwarm) y se instancia en el punto de spawn.
// Cada murciélago nace en el punto (creciendo desde tamaño 0), vuela hacia el jugador,
// le pasa por al lado y se aleja. La bandada se destruye sola pasado 'lifetime'.
// Animator esperado en el murciélago: bool "Fast", trigger "Attack".
public class BatSwarm : MonoBehaviour
{
    [Header("Bandada")]
    [SerializeField] private GameObject batPrefab;
    [SerializeField] private int count = 12;
    [Tooltip("Radio alrededor del punto donde nacen")]
    [SerializeField] private float spawnRadius = 1.0f;
    [SerializeField] private Vector2 spawnHeight = new Vector2(0.8f, 2.0f);
    [Tooltip("Los murciélagos van saliendo repartidos durante este tiempo (s)")]
    [SerializeField] private float spawnDuration = 1.0f;
    [SerializeField] private float scaleInTime = 0.5f;

    [Header("Movimiento")]
    [SerializeField] private Vector2 speedRange = new Vector2(2.5f, 4.0f);
    [Tooltip("Dispersión del punto al que apunta cada murciélago alrededor de tu cabeza")]
    [SerializeField] private float targetSpread = 0.4f;
    [Tooltip("Al llegar a esta distancia de ti, lanza el ataque y sigue de largo")]
    [SerializeField] private float passDistance = 0.8f;
    [SerializeField] private float exitDistance = 8f;
    [SerializeField] private float turnSpeed = 8f;
    [SerializeField] private float wobble = 0.3f;

    [Header("Duración")]
    [SerializeField] private float lifetime = 10f;

    [Header("Sonido")]
    [SerializeField] private AudioSource flightSound;
    [Tooltip("Segundos antes del final en los que el volumen baja hasta 0")]
    [SerializeField] private float fadeOutTime = 1.5f;
    [Tooltip("El sonido sigue al centro de la bandada (útil con Spatial Blend = 1)")]
    [SerializeField] private bool soundFollowsSwarm = true;

    private class Bat
    {
        public Transform t;
        public Animator anim;
        public float speed;
        public float startTime;
        public float phase;
        public Vector3 offset;
        public Vector3 exitTarget;
        public bool passed;
        public bool started;
        public Vector3 baseScale;
    }

    private readonly List<Bat> bats = new List<Bat>();
    private float birth;

    private float soundBaseVolume = 1f;

    public static event System.Action Spawned;

    private void Start()
    {
        birth = Time.time;
        if (batPrefab == null)
        {
            Debug.LogWarning("BatSwarm: falta asignar 'batPrefab'.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 c = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = transform.position + new Vector3(c.x, Random.Range(spawnHeight.x, spawnHeight.y), c.y);

            var go = Instantiate(batPrefab, pos, transform.rotation, transform);
            var b = new Bat
            {
                t = go.transform,
                anim = go.GetComponentInChildren<Animator>(),
                speed = Random.Range(speedRange.x, speedRange.y),
                startTime = birth + Random.Range(0f, spawnDuration),
                phase = Random.Range(0f, 10f),
                offset = new Vector3(
                    Random.Range(-targetSpread, targetSpread),
                    Random.Range(-targetSpread, targetSpread),
                    Random.Range(-targetSpread, targetSpread)),
                baseScale = go.transform.localScale
            };

            // Oculto hasta su momento de salida
            go.transform.localScale = Vector3.zero;

            if (b.anim != null)
            {
                b.anim.applyRootMotion = false;
                b.anim.SetBool("Fast", true);
                // Desfasar el aleteo para que no vayan sincronizados
                b.anim.speed = Random.Range(0.9f, 1.2f);
                b.anim.Update(Random.Range(0f, 1f));
            }
            bats.Add(b);
        }

        Spawned?.Invoke();

        Destroy(gameObject, lifetime);

        if (flightSound != null)
        {
            soundBaseVolume = flightSound.volume;
            flightSound.loop = true;
            flightSound.Play();
        }
    }

    private void Update()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 player = cam.transform.position;

        foreach (var b in bats)
        {
            if (b.t == null) continue;

            if (Time.time < b.startTime) continue;
            b.started = true;

            // Crecer desde 0
            float k = scaleInTime > 0f ? Mathf.Clamp01((Time.time - b.startTime) / scaleInTime) : 1f;
            b.t.localScale = b.baseScale * k;

            Vector3 target;
            if (!b.passed)
            {
                target = player + b.offset;
                if (Vector3.Distance(b.t.position, target) < passDistance)
                {
                    b.passed = true;
                    Vector3 away = (b.t.position - player);
                    if (away.sqrMagnitude < 0.01f) away = b.t.forward;
                    // Seguir de largo por el otro lado de ti
                    Vector3 forward = (target - b.t.position).normalized;
                    b.exitTarget = b.t.position + forward * exitDistance + Vector3.up * Random.Range(0.5f, 2f);
                    if (b.anim != null)
                    {
                        b.anim.SetTrigger("Attack");
                        b.anim.SetBool("Fast", false);
                    }
                }
            }
            target = b.passed ? b.exitTarget : player + b.offset;

            Vector3 dir = target - b.t.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Vector3 d = dir.normalized;
                Quaternion rot = Quaternion.LookRotation(d);
                b.t.rotation = Quaternion.Slerp(b.t.rotation, rot, turnSpeed * Time.deltaTime);

                // Vaivén lateral para que no vuelen en línea recta perfecta
                Vector3 side = Vector3.Cross(Vector3.up, d) * Mathf.Sin(Time.time * 3f + b.phase) * wobble;
                b.t.position += (d * b.speed + side) * Time.deltaTime;
            }
        }
        UpdateSound();
    }

    private void UpdateSound()
    {
        if (flightSound == null) return;

        // Fade-out suave antes de que se destruya la bandada
        float remaining = birth + lifetime - Time.time;
        if (remaining < fadeOutTime)
            flightSound.volume = soundBaseVolume * Mathf.Clamp01(remaining / fadeOutTime);

        // Que el sonido acompañe a los murciélagos en vez de quedarse en el punto de spawn
        if (soundFollowsSwarm && bats.Count > 0)
        {
            Vector3 sum = Vector3.zero;
            int n = 0;
            foreach (var b in bats)
            {
                if (b.t == null) continue;
                sum += b.t.position;
                n++;
            }
            if (n > 0) flightSound.transform.position = sum / n;
        }
    }
}
