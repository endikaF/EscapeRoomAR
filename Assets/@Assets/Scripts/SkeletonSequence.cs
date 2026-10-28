using System.Collections;
using UnityEngine;

public class SkeletonSequence : MonoBehaviour
{
    [Header("Activación")]
    [SerializeField] private float triggerDistance = 2f;

    [Header("Tapa")]
    [SerializeField] private Transform lid;               // la tapa, con su pivote en la bisagra/borde
    [SerializeField] private Vector3 hingeAxis = Vector3.right; // eje local de giro: X
    [SerializeField] private float openAngle = 110f;            // si abre al revés, pon -110
    [SerializeField] private float lidOpenTime = 1.5f;
    [SerializeField] private AudioSource lidSound;         // opcional

    [Header("Esqueleto")]
    [SerializeField] private Animator skeletonAnimator;
    [SerializeField] private string sitUpState = "tumbado_Incorporar";   // nombres EXACTOS de los estados
    [SerializeField] private string lookState = "Girar cabeza izq";
    [SerializeField] private string talkState = "Hablar";
    [SerializeField] private float crossFade = 0.1f;

    [Header("Voz")]
    [SerializeField] private AudioSource voice;            // con el clip asignado, Play On Awake desactivado
    [Tooltip("Solo si no hay audio de voz: segundos que 'habla' el esqueleto. Con voz asignada se ignora")]
    [SerializeField] private float fakeSpeechDuration = 5f;

    [Header("Versión niños (opcional)")]
    [SerializeField] private AudioClip voiceClipAdult;
    [SerializeField] private AudioClip voiceClipKids;
    [Tooltip("Dejar vacío para usar el estado de adultos")]
    private string sitUpStateKids;
    private string lookStateKids;
    private string talkStateKids;

    [Header("Retraso antes de hablar")]
    [SerializeField] private float talkDelayAdult = 0f;
    [SerializeField] private float talkDelayKids = 7f;

    private bool started;

    public static event System.Action SpeechFinished;

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
        SelectVoiceClip();

        // 1) Abrir tapa (giro relativo sobre un solo eje)
        if (lidSound != null) lidSound.Play();
        Quaternion from = lid.localRotation;
        Quaternion to = from * Quaternion.AngleAxis(openAngle, hingeAxis.normalized);
        for (float t = 0; t < 1f; t += Time.deltaTime / lidOpenTime)
        {
            lid.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        lid.localRotation = to;
        if (lidSound != null) lidSound.Stop();

        // 2) Incorporarse y 3) girar cabeza (cada uno espera a que termine el anterior)
        yield return PlayAndWait(Pick(sitUpState, sitUpStateKids));
        yield return PlayAndWait(Pick(lookState, lookStateKids));

        // 4) Hablar: animación en bucle y audio a la vez
        skeletonAnimator.CrossFade(Pick(talkState, talkStateKids), crossFade);
        bool hasVoice = voice != null && voice.clip != null;
        if (hasVoice)
        {
            voice.Play();
            while (voice.isPlaying) yield return null;
        }
        else
        {
            yield return new WaitForSeconds(fakeSpeechDuration);   // simula la voz
        }
        SpeechFinished?.Invoke();

        // 5) Fin del audio: terminar el ciclo de hablar en curso y congelar la pose
        float n = skeletonAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        float target = Mathf.Ceil(n);                 // siguiente final de ciclo

        skeletonAnimator.speed = 0f;
    }

    private IEnumerator PlayAndWait(string stateName)
    {
        skeletonAnimator.CrossFade(stateName, crossFade);
        yield return null; // dejar que arranque la transición
        // esperar a que el estado esté activo y haya acabado
        while (true)
        {
            var info = skeletonAnimator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(stateName) && info.normalizedTime >= 0.98f) break;
            yield return null;
        }
    }

    private string Pick(string adult, string kids)
    {
        return GameSession.KidsMode && !string.IsNullOrEmpty(kids) ? kids : adult;
    }

    private void SelectVoiceClip()
    {
        if (voice == null) return;
        AudioClip chosen = GameSession.KidsMode ? voiceClipKids : voiceClipAdult;
        if (chosen != null) voice.clip = chosen; // si no hay clip específico, se queda el que tenga el AudioSource
    }
}