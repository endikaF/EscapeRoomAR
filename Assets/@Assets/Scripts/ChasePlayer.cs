using UnityEngine;

// Persecución hacia Camera.main.
// Animator esperado: bool "IsMoving", trigger "Attack" (opcional).
public class ChasePlayer : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField] private float stopDistance = 0.8f;
    [SerializeField] private float turnSpeed = 6f;
    [Tooltip("Zombi: true (se mueve a la altura del suelo). Araña/volador: false")]
    [SerializeField] private bool lockToGround = true;
    [SerializeField] private float attackCooldown = 1.5f;

    private Animator anim;
    private float groundY;
    private float nextAttackTime;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        groundY = transform.position.y;
    }

    private void Update()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 target = cam.transform.position;
        if (lockToGround) target.y = groundY;

        Vector3 dir = target - transform.position;
        float dist = dir.magnitude;

        // Girar hacia el jugador (solo en horizontal si va por el suelo)
        Vector3 lookDir = lockToGround ? new Vector3(dir.x, 0f, dir.z) : dir;
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
                if (anim != null) anim.SetTrigger("Attack");
                // Aquí puedes quitar vida al jugador o lanzar un sonido de ataque
            }
        }
    }

    private void SetMoving(bool moving)
    {
        if (anim != null) anim.SetBool("IsMoving", moving);
    }
}
