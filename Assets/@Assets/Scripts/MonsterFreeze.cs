using UnityEngine;

public static class MonsterFreeze
{
    private static int count;
    public static bool Frozen => count > 0;
    public static void Push() => count++;
    public static void Pop() { if (count > 0) count--; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => count = 0;
}