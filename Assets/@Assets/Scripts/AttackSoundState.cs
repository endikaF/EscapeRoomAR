using UnityEngine;

public class AttackSoundState : StateMachineBehaviour
{
    [Tooltip("0 = inicio de la animación, 1 = final. Sube el valor hasta que coincida con el golpe")]
    [Range(0f, 1f)] public float playAtNormalizedTime = 0.3f;

    private bool played;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        played = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (played) return;
        if (stateInfo.normalizedTime >= playAtNormalizedTime)
        {
            played = true;
            var spider = animator.GetComponentInParent<SpiderCrawl>();
            if (spider != null) spider.PlayAttackSound();
        }
    }
}