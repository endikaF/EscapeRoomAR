using System.Collections;
using UnityEngine;

// Murmullo de murciélagos: sube suave (fade in) cuando el esqueleto termina de hablar
// y se corta cuando aparece una bandada de murciélagos (que trae su propio sonido de vuelo).
// Ponlo en un objeto vacío de la escena Gafas, con un AudioSource.
// Requiere:
//   - SkeletonSequence: public static event System.Action SpeechFinished; (lanzado al acabar la voz)
//   - BatSwarm:         public static event System.Action Spawned;        (lanzado en Start)
public class BatMurmur : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField] private float fadeInTime = 3f;
    [Tooltip("Fade corto al parar para evitar un chasquido. 0 = corte inmediato")]
    [SerializeField] private float fadeOutTime = 0.3f;

    private float baseVolume;
    private Coroutine routine;

    private bool batsSpawned;

    private void Awake()
    {
        baseVolume = source.volume;   // el volumen "normal" es el que dejes en el inspector
        source.loop = true;
        source.playOnAwake = false;
    }

    private void OnEnable()
    {
        SkeletonSequence.SpeechFinished += OnSpeechFinished;
        BatSwarm.Spawned += OnBatsSpawned;
    }

    private void OnDisable()
    {
        SkeletonSequence.SpeechFinished -= OnSpeechFinished;
        BatSwarm.Spawned -= OnBatsSpawned;
    }

    private void OnSpeechFinished()
    {
        if (batsSpawned) return;   // NUEVA: si ya salieron los murciélagos, no suena el murmullo
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeIn());
    }

    private void OnBatsSpawned()
    {
        batsSpawned = true;   // NUEVA
        if (!source.isPlaying) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeOutAndStop());
    }

    private IEnumerator FadeIn()
    {
        source.volume = 0f;
        source.Play();
        for (float t = 0f; t < 1f; t += Time.deltaTime / fadeInTime)
        {
            source.volume = Mathf.Lerp(0f, baseVolume, t);
            yield return null;
        }
        source.volume = baseVolume;
    }

    private IEnumerator FadeOutAndStop()
    {
        float start = source.volume;
        if (fadeOutTime > 0f)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / fadeOutTime)
            {
                source.volume = Mathf.Lerp(start, 0f, t);
                yield return null;
            }
        }
        source.Stop();
        source.volume = baseVolume;
    }
}
