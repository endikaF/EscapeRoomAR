using UnityEngine;

// Línea de visión contra los planos reales (capa "RealWorld").
public static class RealWorld
{
    private static int mask = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init() => Physics.queriesHitBackfaces = true;

    public static bool CanSee(Vector3 from, Vector3 to)
    {
        if (mask < 0) mask = LayerMask.GetMask("RealWorld");
        return !Physics.Linecast(from, to, mask, QueryTriggerInteraction.Ignore);
    }

    // true si, al avanzar 'stepLength' en 'dir', el monstruo chocaría con una pared real.
    // 'margin' deja un colchón para que no se meta unos centímetros en la pared.
    public static bool PathBlocked(Vector3 origin, Vector3 dir, float stepLength, float margin = 0.25f)
    {
        if (mask < 0) mask = LayerMask.GetMask("RealWorld");
        if (dir.sqrMagnitude < 0.0001f) return false;
        Vector3 end = origin + dir.normalized * (stepLength + margin);
        return Physics.Linecast(origin, end, mask, QueryTriggerInteraction.Ignore);
    }
}