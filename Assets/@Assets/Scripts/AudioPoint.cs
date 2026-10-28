using System.Collections;
using UnityEngine;

// Prefab de un punto de audio (Audio_reloj / Audio_demo). Lo instancia el SpawnManager
// cuando el jugador se acerca. Reproduce su AudioSource y avisa al ThemeMusicController.
// Prefab: objeto vacío + AudioSource (Play On Awake desactivado, Loop desactivado) + este script.
public class AudioPoint : MonoBehaviour
{
    public enum Kind { Clock, Demogorgon }

    [SerializeField] private Kind kind = Kind.Clock;
    [SerializeField] private AudioSource source;
    [Tooltip("Segundos que suena como máximo. 0 = el clip entero. Reloj: 3")]
    [SerializeField] private float playSeconds = 0f;
    [Tooltip("Fade de salida si se corta antes de que acabe el clip")]
    [SerializeField] private float fadeOutTime = 0.4f;

    // Se lanza al empezar y al terminar el sonido de este punto
    public static event System.Action<Kind> Started;
    public static event System.Action<Kind> Finished;

    private void Awake()
    {
        if (source == null) source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        // Reloj: espacial (suena en el túnel). Demogorgon: 2D.
        source.spatialBlend = kind == Kind.Clock ? 1f : 0f;
    }

    private void Start()
    {
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        Started?.Invoke(kind);

        if (source.clip == null)
        {
            Debug.LogWarning($"AudioPoint ({kind}): el AudioSource no tiene clip.");
            Finished?.Invoke(kind);
            yield break;
        }

        float baseVolume = source.volume;
        float limit = playSeconds > 0f ? Mathf.Min(playSeconds, source.clip.length) : source.clip.length;
        source.Play();

        float t = 0f;
        while (source.isPlaying && t < limit)
        {
            t += Time.deltaTime;
            // Si hay que cortar antes del final del clip, fade en los últimos segundos
            if (playSeconds > 0f && playSeconds < source.clip.length && fadeOutTime > 0f)
            {
                float remaining = limit - t;
                if (remaining < fadeOutTime)
                    source.volume = baseVolume * Mathf.Clamp01(remaining / fadeOutTime);
            }
            yield return null;
        }

        source.Stop();
        source.volume = baseVolume;
        Finished?.Invoke(kind);
    }
}
