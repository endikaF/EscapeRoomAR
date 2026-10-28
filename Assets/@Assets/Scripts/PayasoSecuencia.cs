using System.Collections;
using UnityEngine;

// Payaso decorativo: idle hasta que te acercas; entonces suena la voz (en idle o con
// una animación de hablar opcional) y después señala y se queda congelado.
public class PayasoSecuencia : MonoBehaviour
{
    [Header("Activación")]
    [SerializeField] private float triggerDistance = 2f;

    [Header("Animación")]
    [SerializeField] private Animator animator;
    [Tooltip("Opcional: estado mientras habla. Vacío = se queda en idle")]
    [SerializeField] private string talkState = "";
    [Tooltip("Nombre EXACTO del estado de señalar")]
    [SerializeField] private string pointState = "Pointing";
    [SerializeField] private float crossFade = 0.1f;

    [Header("Voz")]
    [SerializeField] private AudioSource voice;
    [Tooltip("0 = señala cuando termina la voz. Mayor que 0 = señala a esos segundos de empezar a hablar")]
    [SerializeField] private float pointAfterSeconds = 0f;

    private bool started;

    private void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (started) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 a = new Vector3(cam.transform.position.x, 0f, cam.transform.position.z);
        Vector3 b = new Vector3(transform.position.x, 0f, transform.position.z);
        if (Vector3.Distance(a, b) <= triggerDistance)
        {
            started = true;
            StartCoroutine(Sequence());
        }
    }

    private IEnumerator Sequence()
    {
        if (animator == null) yield break;

        bool hasVoice = voice != null && voice.clip != null;
        if (hasVoice) voice.Play();
        if (!string.IsNullOrEmpty(talkState)) animator.CrossFade(talkState, crossFade);

        if (pointAfterSeconds > 0f)
            yield return new WaitForSeconds(pointAfterSeconds);
        else if (hasVoice)
            while (voice.isPlaying) yield return null;

        // Señalar y esperar a que termine
        animator.CrossFade(pointState, crossFade);
        yield return null;
        while (true)
        {
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(pointState) && info.normalizedTime >= 0.98f) break;
            yield return null;
        }

        // Congelar en la pose de señalar
        animator.speed = 0f;
    }
}