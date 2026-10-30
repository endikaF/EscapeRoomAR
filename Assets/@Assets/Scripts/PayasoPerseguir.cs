using System.Collections;
using UnityEngine;

// Payaso monstruo: idle hasta que entras en aggroDistance; entonces te persigue,
// te ataca al llegar y se ríe a intervalos. Se pone en la raíz del prefab.
public class PayasoPerseguir : MonoBehaviour
{
    [Header("Comportamiento")]
    [Tooltip("Distancia horizontal a la que deja el idle y va a por ti")]
    [SerializeField] private float aggroDistance = 3f;
    [SerializeField] private float speed = 1.2f;
    [Tooltip("Distancia a la que se detiene y ataca (más alta en el payaso con arma)")]
    [SerializeField] private float stopDistance = 0.8f;
    [SerializeField] private float turnSpeed = 6f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Animator (nombres de parámetros)")]
    [SerializeField] private string movingBool = "IsMoving";
    [SerializeField] private string attackTrigger = "Attack";

    [Header("Risa")]
    [SerializeField] private AudioSource laughSource;
    [SerializeField] private AudioClip[] laughClips;
    [Tooltip("Segundos entre risas (mínimo y máximo)")]
    [SerializeField] private Vector2 laughInterval = new Vector2(4f, 8f);

    private Animator anim;
    private float groundY;
    private bool aggro;
    private bool dead;
    private float nextAttackTime;
    private float stunnedUntil;
    private Coroutine laughRoutine;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        groundY = transform.position.y;
    }

    private void Update()
    {
        if (dead) return;

        if (MonsterFreeze.Frozen) return;

        var cam = Camera.main;
        if (cam == null) return;
        if (Time.time < stunnedUntil) return;

        Vector3 target = cam.transform.position;
        target.y = groundY;
        Vector3 dir = target - transform.position;
        float dist = dir.magnitude;

        Vector3 eye = transform.position + Vector3.up * 0.5f;
        bool sees = RealWorld.CanSee(eye, cam.transform.position);

        if (!aggro)
        {
            if (dist <= aggroDistance && sees) StartAggro();
            return;
        }

        if (!sees)
        {
            SetMoving(false);
            return;
        }

        Vector3 lookDir = new Vector3(dir.x, 0f, dir.z);
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, turnSpeed * Time.deltaTime);
        }

        if (dist > stopDistance)
        {
            // Nivel 2: no atravesar paredes, resbalar a lo largo de ellas
            float step = speed * Time.deltaTime;
            Vector3 move = dir.normalized;

            if (RealWorld.PathBlocked(eye, move, step, out Vector3 n))
            {
                move = Vector3.ProjectOnPlane(move, n);
                move.y = 0f;

                if (move.sqrMagnitude < 0.01f ||
                    RealWorld.PathBlocked(eye, move.normalized, step, out _))
                {
                    SetMoving(false);   // de frente contra la pared o esquina cerrada
                    move = Vector3.zero;
                }
                else
                {
                    move.Normalize();
                }
            }

            if (move != Vector3.zero)
            {
                transform.position += move * step;
                SetMoving(true);
            }
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

    private void StartAggro()
    {
        aggro = true;
        if (laughRoutine == null) laughRoutine = StartCoroutine(LaughLoop());
    }

    private IEnumerator LaughLoop()
    {
        while (!dead)
        {
            yield return new WaitForSeconds(Random.Range(laughInterval.x, laughInterval.y));
            if (dead) yield break;
            if (laughSource != null && laughClips != null && laughClips.Length > 0)
                laughSource.PlayOneShot(laughClips[Random.Range(0, laughClips.Length)]);
        }
    }

    private void SetMoving(bool value)
    {
        if (anim != null) anim.SetBool(movingBool, value);
    }

    // Lo llama MonsterHealth al recibir daño
    public void Stun(float seconds)
    {
        if (!aggro) StartAggro(); // si le disparas en idle, se activa
        if (Time.time < stunnedUntil) return;
        stunnedUntil = Time.time + seconds;
        SetMoving(false);
    }

    // Lo llama MonsterHealth al morir
    public void Die()
    {
        dead = true;
        if (laughSource != null) laughSource.Stop();
    }
}