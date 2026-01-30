using HolenderGames.StatSystem;
using HolenderGames.UpgradesTreePro;
using TMPro;
using UnityEngine;

namespace HolenderGames.WizardDemo
{
    /// <summary>
    /// Manage the character: stats and base params, animations, projectiles and enemy targeting
    /// </summary>
    public class GameCharacter : Upgradable
    {
        private const string ANIM_SHOOT = "Shoot";
        [SerializeField] private TargetController target;
        [SerializeField] private TextMeshPro txtHealth;
        [SerializeField] private TextMeshPro txtShields;
        [SerializeField] private StatType statHP;
        [SerializeField] private StatType statDamage;
        [SerializeField] private StatType statShields;
        [SerializeField] private StatType statAttackSpeed;

        [Header("Weapon Settings")]
        public GameObject projectilePrefab;
        public Transform firePoint;
        private float FireRate = 1f; // projectiles per second
        private float Damage = 10;
        private float Health = 100;
        private float Shields = 30;
        private float fireCooldown = 0f;

        private Animator animator;

        protected override void Awake()
        {
            base.Awake();
            animator = GetComponent<Animator>();
        }

        protected override void UpdateStats()
        {
            Health = GameData.Instance.GetStat(statHP);
            Damage = GameData.Instance.GetStat(statDamage);
            Shields = GameData.Instance.GetStat(statShields);
            FireRate = GameData.Instance.GetStat(statAttackSpeed);

            txtHealth.text = Health.ToString("F0");
            txtShields.text = Shields.ToString("F0");
        }

        void Update()
        {
            fireCooldown -= Time.deltaTime;

            if (fireCooldown <= 0f)
            {
                fireCooldown = GetNextCooldown();
                StartShootAnimaton();
            }
        }

        protected float GetNextCooldown()
        {
            return 1/FireRate;
        }

        private void StartShootAnimaton()
        {
            animator.Play(ANIM_SHOOT);
        }

        public void OnShootAnimationTrigger()
        {
            ShootProjectile();
        }

        private GameObject GetNewProjectile()
        {
            GameObject projectileObj = Instantiate(projectilePrefab);
            projectileObj.transform.position = firePoint.position;
            return projectileObj;
        }

        private void ShootProjectile()
        {
           Projectile p = GetNewProjectile().GetComponent<Projectile>();
           p.SetTarget(target,Damage);
        }

    }
}

