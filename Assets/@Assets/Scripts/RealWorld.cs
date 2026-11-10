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

    // true si avanzar 'stepLength' en 'dir' chocaría con una pared real.
    // 'radius' es el grosor del cuerpo. 'normal' es la normal de la pared golpeada.
    public static bool PathBlocked(Vector3 origin, Vector3 dir, float stepLength,
                                   out Vector3 normal, float radius = 0.3f)
    {
        normal = Vector3.zero;
        if (mask < 0) mask = LayerMask.GetMask("RealWorld");
        if (dir.sqrMagnitude < 0.0001f) return false;

        if (Physics.SphereCast(origin, radius, dir.normalized, out RaycastHit hit,
                               stepLength + 0.1f, mask, QueryTriggerInteraction.Ignore))
        {
            normal = hit.normal;
            return true;
        }
        return false;
    }
}