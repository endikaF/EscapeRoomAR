// Estado que pasa del menú a la escena de juego. Al ser estático sobrevive al cambio de escena.
// En el editor, si abres la escena de juego directamente, arranca como administrador en modo Colocar
// (así puedes seguir probando sin pasar por el menú). En una build arranca como cliente en modo Jugar.
public static class GameSession
{
    public const string GameSceneName = "Gafas";

#if UNITY_EDITOR
    public static ARTapSpawnPointProvider.Mode StartMode = ARTapSpawnPointProvider.Mode.Place;
    public static bool IsAdmin = true;
#else
    public static ARTapSpawnPointProvider.Mode StartMode = ARTapSpawnPointProvider.Mode.Play;
    public static bool IsAdmin = false;
#endif
}
