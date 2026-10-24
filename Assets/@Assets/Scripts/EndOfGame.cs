using UnityEngine;

// Fin de partida opcional: vuelve al menú cuando
//  - todos los puntos ya han hecho aparecer su monstruo y no queda ninguno vivo (tras delayAfterLastKill), o
//  - se acaba el tiempo (si useTimer está activado; el tiempo cuenta desde que se entra en modo Jugar).
// Requiere el método AllPointsSpawned en SpawnManager (ver instrucciones).
// Si no lo quieres, desactiva o quita este componente: el juego funciona igual.
public class EndOfGame : MonoBehaviour
{
    [SerializeField] private ARTapSpawnPointProvider provider;
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private ExitToMenuGesture exit;
    [SerializeField] private bool endWhenAllKilled = true;
    [SerializeField] private float delayAfterLastKill = 5f;
    [SerializeField] private bool useTimer = false;
    [SerializeField] private float timeLimitSeconds = 300f;

    private float playStartTime = -1f;
    private float emptySince = -1f;

    private void Update()
    {
        if (provider == null || spawnManager == null || exit == null) return;

        // Solo en modo Jugar
        if (provider.CurrentMode != ARTapSpawnPointProvider.Mode.Play)
        {
            playStartTime = -1f;
            emptySince = -1f;
            return;
        }

        if (playStartTime < 0f) playStartTime = Time.time;

        if (useTimer && Time.time - playStartTime >= timeLimitSeconds)
        {
            exit.GoToMenu();
            return;
        }

        if (!endWhenAllKilled || !spawnManager.AllPointsSpawned) return;

        // MonsterHealth se destruye destroyDelay s después de morir, así que cuenta como vivo hasta entonces
        int alive = FindObjectsByType<MonsterHealth>(FindObjectsSortMode.None).Length;
        if (alive > 0)
        {
            emptySince = -1f;
            return;
        }

        if (emptySince < 0f) emptySince = Time.time;
        if (Time.time - emptySince >= delayAfterLastKill) exit.GoToMenu();
    }
}
