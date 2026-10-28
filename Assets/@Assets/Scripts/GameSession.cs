public static class GameSession
{
    public const string GameSceneName = "Gafas";

    // false = adultos (por defecto, también cuando entra el administrador). true = niños.
    public static bool KidsMode = false;

#if UNITY_EDITOR
    public static ARTapSpawnPointProvider.Mode StartMode = ARTapSpawnPointProvider.Mode.Place;
    public static bool IsAdmin = true;
#else
    public static ARTapSpawnPointProvider.Mode StartMode = ARTapSpawnPointProvider.Mode.Play;
    public static bool IsAdmin = false;
#endif
}