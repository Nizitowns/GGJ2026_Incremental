using DG.Tweening;
using HolenderGames.Sound;
using TMPro;
using UnityEngine;

namespace HolenderGames.WizardDemo
{
    /// <summary>
    /// class for managing the projectiles movement towards a target
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private const string ANIM_EXPLODE = "Explode";

        [SerializeField] protected float speed = 5f;
        [SerializeField] protected GameSound startSound;
        [SerializeField] protected GameSound endSound;
        [SerializeField] protected TextMeshPro dmgText;
        protected float damage;
        private Vector2 moveDirection;
        private TargetController target;
        private Animator animator;

        private bool isMoving = true;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }
        private void Update()
        {
            if (!isMoving) return;

            MoveUpdate();
        }
        public void SetTarget(TargetController targetController, float damage)
        {
            target = targetController;
            if(target ==null)
            {
                Destroy(gameObject);
            }    
            this.damage = damage;
            dmgText.text = ((int)damage).ToString();

            moveDirection = (target.hitPoint.position - transform.position).normalized;

            SoundManager.Instance.PlaySound(startSound);
        }

        private void MoveUpdate()
        {

            transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.hitPoint.transform.position)<= 0.1f)
            {
                target.OnHit();
                isMoving = false;
                animator.Play(ANIM_EXPLODE);

                dmgText.DOFade(1, 0.1f);
                dmgText.transform.DOMoveY(0.3f, 0.5f).onComplete = () => Destroy(gameObject,0.2f);
                SoundManager.Instance.PlaySound(endSound);
            }
        }
    }
}

