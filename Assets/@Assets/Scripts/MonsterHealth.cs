using UnityEngine;

// Vida y muerte. Animator esperado: trigger "Die" (opcional).
public class MonsterHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float destroyDelay = 4f;
    [SerializeField] private float hitStunTime = 0.6f;

    private float health;
    private bool dead;

    private void Awake() => health = maxHealth;

    public void TakeDamage(float amount)
    {
        if (dead) return;
        health -= amount;
        if (health <= 0f) { Die(); return; }

        var chase = GetComponent<ChasePlayer>();
        if (chase != null) chase.Stun(hitStunTime);

        var crawl = GetComponent<SpiderCrawl>();
        if (crawl != null) crawl.Stun(hitStunTime);

        var anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("Hit");

        var payaso = GetComponent<PayasoPerseguir>();
        if (payaso != null) payaso.Stun(hitStunTime);

        var emboscar = GetComponent<PayasoEmboscar>();
        if (emboscar != null) emboscar.Stun(hitStunTime);
    }

    private void Die()
    {
        dead = true;

        var chase = GetComponent<ChasePlayer>();
        if (chase != null) chase.enabled = false;

        var crawl = GetComponent<SpiderCrawl>();
        if (crawl != null) crawl.Die();

        var payaso = GetComponent<PayasoPerseguir>();
        if (payaso != null) payaso.Die();

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        var audio = GetComponentInChildren<AudioSource>();
        if (audio != null) audio.Stop();

        var anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("Die");

        var emboscar = GetComponent<PayasoEmboscar>();
        if (emboscar != null) emboscar.Die();

        Destroy(gameObject, destroyDelay);
    }
}
