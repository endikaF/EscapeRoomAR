using UnityEngine;

// Vida y muerte. Animator esperado: trigger "Die" (opcional).
public class MonsterHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float destroyDelay = 4f;

    private float health;
    private bool dead;

    private void Awake() => health = maxHealth;

    public void TakeDamage(float amount)
    {
        if (dead) return;
        health -= amount;
        if (health <= 0f) Die();
    }

    private void Die()
    {
        dead = true;

        var chase = GetComponent<ChasePlayer>();
        if (chase != null) chase.enabled = false;

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        var audio = GetComponentInChildren<AudioSource>();
        if (audio != null) audio.Stop();

        var anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("Die");

        Destroy(gameObject, destroyDelay);
    }
}
