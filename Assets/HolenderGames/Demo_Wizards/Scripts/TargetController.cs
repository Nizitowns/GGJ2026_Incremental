using UnityEngine;
using DG.Tweening;

namespace HolenderGames.WizardDemo
{
    /// <summary>
    /// Manage the target getting hit
    /// </summary>
    public class TargetController : MonoBehaviour
    {
        [SerializeField] public Transform hitPoint;

        public void OnHit()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(new Vector3(0.15f,0.1f,0f), 0.15f);
        }
    }
}

