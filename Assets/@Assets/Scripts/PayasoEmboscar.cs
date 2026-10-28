using System.Collections;
using UnityEngine;

// Payaso emboscador: escondido detrás de una puerta (su punto). Sale cuando el jugador
// la SUPERA: pasa más de 'passDistance' metros más allá del punto en el sentido de su recorrido.
// Si el jugador llega a la puerta y da la vuelta, no sale.
// Al salir te persigue y, 'laughDelay' segundos después, se ríe.
// Se pone en la raíz del prefab (igual que PayasoPerseguir).
public class PayasoEmboscar : MonoBehaviour
{
    [Header("Emboscada")]
    [Tooltip("Distancia horizontal al punto a la que empieza a 'vigilar' por dónde llegas. Debe ser >= la distancia de activación del SpawnManager/Registry")]
    [SerializeField] private float watchDistance = 4f;
    [Tooltip("Metros que hay que pasar de la puerta (en el sentido en que llegaste) para que salga")]
    [SerializeField] private float passDistance = 1f;

    [Header("Persecución")]
    [SerializeField] private float speed = 1.2f;
    [SerializeField] private float stopDistance = 0.8f;
    [SerializeField] private float turnSpeed = 6f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Animator (nombres de parámetros)")]
    [SerializeField] private string movingBool = "IsMoving";
    [SerializeField] private string attackTrigger = "Attack";

    [Header("Risa")]
    [SerializeField] private AudioSource laughSource;
    [SerializeField] private AudioClip[] laughClips;
    [Tooltip("Segundos desde que sale hasta que se ríe")]
    [SerializeField] private float laughDelay = 3f;

    private Animator anim;
    private float groundY;
    private Vector3 doorPos;
    private bool tracking;
    private Vector3 entryPos;     // dónde estaba el jugador al entrar en la zona de vigilancia
    private bool ambushed;
    private bool dead;
    private float nextAttackTime;
    private float stunnedUntil;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        groundY = transform.position.y;
        doorPos = transform.position;
    }

    private void Update()
    {
        if (dead) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 player = cam.transform.position;

        if (!ambushed)
        {
            WatchForAmbush(player);
            return;
        }

        if (Time.time < stunnedUntil) return;

        Vector3 target = player;
        target.y = groundY;
        Vector3 dir = target - transform.position;
        float dist = dir.magnitude;

        Vector3 lookDir = new Vector3(dir.x, 0f, dir.z);
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, turnSpeed * Time.deltaTime);
        }

        if (dist > stopDistance)
        {
            transform.position += dir.normalized * speed * Time.deltaTime;
            SetMoving(true);
        }
        else
        {
            SetMoving(false);
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                if (anim != null) anim.SetTrigger(attackTrigger);
            }
        }
    }

    private void WatchForAmbush(Vector3 player)
    {
        Vector3 p = new Vector3(player.x, 0f, player.z);
        Vector3 d = new Vector3(doorPos.x, 0f, doorPos.z);
        float horizontal = Vector3.Distance(p, d);

        // Fuera de la zona: olvida por dónde llegó (si vuelve a entrar, se recalcula)
        if (horizontal > watchDistance)
        {
            tracking = false;
            return;
        }

        if (!tracking)
        {
            tracking = true;
            entryPos = p;
            return;
        }

        // Eje de recorrido: de donde entraste hacia la puerta
        Vector3 axis = d - entryPos;
        if (axis.sqrMagnitude < 0.01f) return;
        axis.Normalize();

        // Cuánto has pasado de la puerta en ese sentido (negativo = aún no la alcanzas / has vuelto)
        float beyond = Vector3.Dot(p - d, axis);
        if (beyond >= passDistance) Ambush();
    }

    private void Ambush()
    {
        if (ambushed) return;
        ambushed = true;
        StartCoroutine(LaughAfterDelay());
    }

    private IEnumerator LaughAfterDelay()
    {
        yield return new WaitForSeconds(laughDelay);
        if (dead) yield break;
        if (laughSource != null && laughClips != null && laughClips.Length > 0)
            laughSource.PlayOneShot(laughClips[Random.Range(0, laughClips.Length)]);
    }

    private void SetMoving(bool value)
    {
        if (anim != null) anim.SetBool(movingBool, value);
    }

    // Lo llama MonsterHealth al recibir daño
    public void Stun(float seconds)
    {
        Ambush(); // si le disparas escondido, sale
        if (Time.time < stunnedUntil) return;
        stunnedUntil = Time.time + seconds;
        SetMoving(false);
    }

    // Lo llama MonsterHealth al morir
    public void Die()
    {
        dead = true;
        StopAllCoroutines();
        if (laughSource != null) laughSource.Stop();
    }
}