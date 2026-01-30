using UnityEngine;
using DG.Tweening;

namespace HolenderGames.WizardDemo
{
    /// <summary>
    /// float effect for ui elements
    /// </summary>
    public class FloatUI : MonoBehaviour
    {
        [SerializeField] private Ease ease = Ease.InOutSine;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private float height = 1.05f;

        private void Start()
        {
            transform.DOLocalMoveY(height, duration).SetEase(ease).SetLoops(-1, LoopType.Yoyo);
        }
    }
}

