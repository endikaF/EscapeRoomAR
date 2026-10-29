using System.Collections.Generic;
using UnityEngine;

public class FreezeAnimators : MonoBehaviour
{
    private readonly Dictionary<Animator, float> speeds = new Dictionary<Animator, float>();

    private void LateUpdate()
    {
        if (MonsterFreeze.Frozen)
        {
            foreach (var a in GetComponentsInChildren<Animator>())
                if (a.speed != 0f) { speeds[a] = a.speed; a.speed = 0f; }
        }
        else if (speeds.Count > 0)
        {
            foreach (var kv in speeds) if (kv.Key != null) kv.Key.speed = kv.Value;
            speeds.Clear();
        }
    }
}