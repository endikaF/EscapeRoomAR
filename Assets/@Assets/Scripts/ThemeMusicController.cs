using System.Collections;
using UnityEngine;

// Música de fondo (Stranger Things): empieza cuando acaba el reloj, baja de volumen mientras
// suena el demogorgon y vuelve a subir después. Sigue en bucle hasta el final de la partida.
// Ponlo en un objeto vacío de la escena Gafas, con un AudioSource (clip = canción, 2D).
public class ThemeMusicController : MonoBehaviour
{
    [SerializeField] private AudioSource music;
    [Range(0f, 1f)][SerializeField] private float duckedFraction = 0.2f;
    [SerializeField] private float fadeInTime = 1.5f;
    [SerializeField] private float duckTime = 0.5f;
    [SerializeField] private float restoreTime = 2f;

    private float baseVolume;
    private Coroutine routine;

    private void Awake()
    {
        baseVolume = music.volume;   // el volumen normal es el que dejes en el inspector
        music.loop = true;
        music.playOnAwake = false;
        music.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        AudioPoint.Finished += OnFinished;
        AudioPoint.Started += OnStarted;
    }

    private void OnDisable()
    {
        AudioPoint.Finished -= OnFinished;
        AudioPoint.Started -= OnStarted;
    }

    private void OnStarted(AudioPoint.Kind kind)
    {
        // Solo bajamos si la música ya está sonando
        if (kind == AudioPoint.Kind.Demogorgon && music.isPlaying)
            Fade(baseVolume * duckedFraction, duckTime, false);
    }

    private void OnFinished(AudioPoint.Kind kind)
    {
        if (kind == AudioPoint.Kind.Clock)
        {
            if (music.isPlaying) return;
            music.volume = 0f;
            music.Play();
            Fade(baseVolume, fadeInTime, false);
        }
        else if (kind == AudioPoint.Kind.Demogorgon && music.isPlaying)
        {
            Fade(baseVolume, restoreTime, false);
        }
    }

    private void Fade(float target, float time, bool stopAtEnd)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeRoutine(target, time, stopAtEnd));
    }

    private IEnumerator FadeRoutine(float target, float time, bool stopAtEnd)
    {
        float start = music.volume;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, time))
        {
            music.volume = Mathf.Lerp(start, target, t);
            yield return null;
        }
        music.volume = target;
        if (stopAtEnd) music.Stop();
    }
}
