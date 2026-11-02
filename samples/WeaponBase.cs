// Portfolio excerpt from private repo thejoebear/Horde-Survival (branch player-characters).
// Shown for design/portfolio review only. Will not compile as a standalone Unity project.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoblinHorde.Player;
using GoblinHorde.Enemies;
using GoblinHorde.Utils;
using GoblinHorde.World;
using GoblinHorde.Audio;

namespace GoblinHorde.Weapons
{
    /// <summary>
    /// Abstract weapon base class taking PlayerStats into account for attack speed and damage scaling.
    /// Manages weapon level, cooldown timer loop, target detection, and level upgrades.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        public const int MaxLevel = 5;

        [Header("Weapon Identity")]
        [SerializeField] protected string weaponName = "Base Weapon";
        [SerializeField] [TextArea(2, 3)] protected string description = "An auto-attacking weapon.";
        [SerializeField] protected Sprite icon;
        [SerializeField] protected int weaponLevel = 1;
        [SerializeField] protected bool isEvolved;
        [SerializeField] protected bool isSuper;

        [Header("Base Weapon Stats")]
        [SerializeField] protected float baseCooldown = 1f;
        [SerializeField] protected float baseDamage = 10f;
        [SerializeField] protected int projectileCount = 1;
        [SerializeField] protected int pierceCount = 1;
        [SerializeField] protected float projectileSpeed = 10f;
        [SerializeField] protected float attackRange = 10f;

        [Header("Prefab Reference")]
        [SerializeField] protected GameObject projectilePrefab;

        protected PlayerStats playerStats;
        protected PlayerController playerController;
        protected float fireTimer = 0f;

        public string WeaponName => weaponName;
        public virtual string Description => description;
        public virtual Sprite Icon => icon;
        public virtual WeaponTag Tags => WeaponTag.None;
        public float DamageDealtThisRun { get; private set; }
        public int WeaponLevel => weaponLevel;
        public bool IsEvolved => isEvolved;
        public bool IsSuper => isSuper;
        public bool IsMaxLevel => weaponLevel >= MaxLevel;
        public bool CanUpgrade => !IsMaxLevel;
        public bool CanEvolve => IsMaxLevel && !isEvolved;
        public bool CanCombine => isEvolved && !isSuper;
        public virtual int SilhouetteFamilyId => 0;
        public int FamilySynergyRank =>
            GoblinHorde.Progression.WeaponInventory.Instance != null
                ? GoblinHorde.Progression.WeaponInventory.Instance.FamilySynergyRank(SilhouetteFamilyId)
                : 0;
        public string RankLabel => isSuper ? "Super" : (isEvolved ? "Evolved" : $"Lv {weaponLevel}");
        public string RankLabelShort => isSuper ? "Super" : (isEvolved ? "Evo" : $"Lv {weaponLevel}");
        public Color RankColor => isSuper ? new Color(1f, 0.82f, 0.28f, 1f) : (isEvolved ? new Color(0.85f, 0.55f, 1f, 1f) : new Color(0.62f, 0.58f, 0.5f, 1f));
        public virtual Sprite RiderIcon => null;
        public virtual string RiderName => null;
        public virtual WeaponTag PrimaryTags => Tags;
        public virtual WeaponTag RiderTags => WeaponTag.None;
        public float BaseCooldown => baseCooldown;
        public float BaseDamage => baseDamage;
        public int ProjectileCount => projectileCount;
        public int PierceCount => pierceCount;
        public float ProjectileSpeed => projectileSpeed;
        public float AttackRange => attackRange;
        public GameObject ProjectilePrefab
        {
            get => projectilePrefab;
            set => projectilePrefab = value;
        }

        /// <summary>
        /// Calculated damage factoring in PlayerStats damage multiplier.
        /// Stash passives are applied directly to PlayerStats, so they are included here.
        /// </summary>
        public virtual float CurrentDamage
        {
            get
            {
                float mult = (playerStats != null) ? playerStats.DamageMultiplier : 1f;
                return baseDamage * mult * (1f + FamilySynergyRank * Core.Balance.FamilySynergyDamage);
            }
        }

        public float RollHitDamage()
        {
            float damage = CurrentDamage;
            if (playerStats != null && playerStats.CritChance > 0f && UnityEngine.Random.value < playerStats.CritChance)
            {
                damage *= playerStats.CritDamageMultiplier;
            }

            return damage;
        }

        public bool IsLikelyCritical(float rolledDamage)
        {
            if (playerStats == null || playerStats.CritChance <= 0f) return false;
            float critThreshold = CurrentDamage * Mathf.Max(1.25f, playerStats.CritDamageMultiplier) * 0.98f;
            return rolledDamage >= critThreshold;
        }

        public float AreaMultiplier
        {
            get
            {
                float area = playerStats != null ? playerStats.AreaMultiplier : 1f;
                return area * (1f + FamilySynergyRank * Core.Balance.FamilySynergyArea);
            }
        }
        public float DurationMultiplier => playerStats != null ? playerStats.DurationMultiplier : 1f;
        public float KnockbackMultiplier => playerStats != null ? playerStats.KnockbackDealtMultiplier : 1f;
        public int CurrentProjectileCount =>
            Mathf.Max(1, projectileCount + (playerStats != null ? playerStats.ExtraProjectiles : 0) + FamilySynergyRank);
        public int CurrentPierce => Mathf.Max(1, pierceCount + (playerStats != null ? playerStats.ExtraPierce : 0));
        public float CurrentRange => Mathf.Min(attackRange * AreaMultiplier, ArenaMap.CombatRangeCap);

        /// <summary>
        /// Calculated attack cooldown from attack speed only (CDR merged into ASPD).
        /// </summary>
        public virtual float CurrentCooldown
        {
            get
            {
                float speedMult = (playerStats != null) ? playerStats.AttackSpeedMultiplier : 1f;
                speedMult += FamilySynergyRank * Core.Balance.FamilySynergyAttackSpeed;
                float effectiveCd = baseCooldown / Mathf.Max(0.1f, speedMult);
                return Mathf.Max(0.05f, effectiveCd);
            }
        }

        /// <summary>
        /// Stat granted passively to the player while this weapon sits in the stash.
        /// Subclasses override to theme the bonus to the weapon's identity.
        /// </summary>
        public virtual Progression.StatType StashStatType => Progression.StatType.Damage;

        /// <summary>
        /// Amount of the stash stat granted, scaled by weapon level and evolution.
        /// </summary>
        public virtual float StashStatAmount
        {
            get
            {
                float amount = StashAmountPerLevel(StashStatType) * weaponLevel;
                if (isEvolved)
                {
                    amount *= Core.Balance.StashEvolvedMultiplier;
                }
                return amount;
            }
        }

        protected static float StashAmountPerLevel(Progression.StatType statType)
        {
            switch (statType)
            {
                case Progression.StatType.Damage: return Core.Balance.StashDamagePerLevel;
                case Progression.StatType.AttackSpeed: return Core.Balance.StashAttackSpeedPerLevel;
                case Progression.StatType.CooldownReduction: return Core.Balance.StashAttackSpeedPerLevel; // CDR merged into ASPD
                case Progression.StatType.MoveSpeed: return Core.Balance.StashMoveSpeedPerLevel;
                case Progression.StatType.MaxHealth: return Core.Balance.StashMaxHealthPerLevel;
                case Progression.StatType.Armor: return Core.Balance.StashArmorPerLevel;
                case Progression.StatType.PickupRadius: return Core.Balance.StashPickupRadiusPerLevel;
                case Progression.StatType.GoldFind: return Core.Balance.StashGoldFindPerLevel;
                case Progression.StatType.XpGain: return Core.Balance.StashXpGainPerLevel;
                case Progression.StatType.IFrames: return Core.Balance.StashIFramesPerLevel;
                case Progression.StatType.HealOnKill: return Core.Balance.StashHealOnKillPerLevel;
                case Progression.StatType.Area: return Core.Balance.StashAreaPerLevel;
                case Progression.StatType.Duration: return Core.Balance.StashDurationPerLevel;
                case Progression.StatType.Luck: return Core.Balance.StashLuckPerLevel;
                case Progression.StatType.Regen: return Core.Balance.StashRegenPerLevel;
                case Progression.StatType.Lifesteal: return Core.Balance.StashLifestealPerLevel;
                case Progression.StatType.MagnetSpeed: return Core.Balance.StashMagnetSpeedPerLevel;
                case Progression.StatType.KnockbackDealt: return Core.Balance.StashKnockbackDealtPerLevel;
                case Progression.StatType.KnockbackTaken: return Core.Balance.StashKnockbackTakenPerLevel;
                case Progression.StatType.CritChance: return Core.Balance.StashCritChancePerLevel;
                case Progression.StatType.CritDamage: return Core.Balance.StashCritDamagePerLevel;
                default: return 0f;
            }
        }

        protected virtual void Awake()
        {
            playerController = GetComponentInParent<PlayerController>();
            if (playerController == null)
            {
                playerController = PlayerController.Instance;
            }

            if (playerController != null)
            {
                playerStats = playerController.Stats;
            }

            if (playerStats == null)
            {
                playerStats = GetComponentInParent<PlayerStats>();
            }
        }

        protected virtual void Start()
        {
            if (playerController == null) playerController = PlayerController.Instance;
            if (playerStats == null && playerController != null) playerStats = playerController.Stats;

            fireTimer = CurrentCooldown; // Fire soon after starting
        }

        protected virtual void Update()
        {
            if (playerStats != null && playerStats.IsDead) return;

            TickAutoFire();
        }

        /// <summary>
        /// Default: fire every <see cref="CurrentCooldown"/>. Catalog weapons with
        /// discrete attack VFX override so presentation finishes before the wait.
        /// </summary>
        protected virtual void TickAutoFire()
        {
            fireTimer += Time.deltaTime;
            if (fireTimer >= CurrentCooldown)
            {
                if (CanAttack())
                {
                    Attack();
                    fireTimer = 0f;
                }
            }
        }

        /// <summary>
        /// Condition check before attacking.
        /// </summary>
        protected virtual bool CanAttack()
        {
            if (projectilePrefab == null) return false;
            return true;
        }

        /// <summary>
        /// Abstract attack method implemented by specific weapon types.
        /// </summary>
        protected abstract void Attack();

        /// <summary>
        /// Upgrades weapon level and scales base weapon stats.
        /// </summary>
        public virtual void UpgradeLevel()
        {
            if (IsMaxLevel) return;

            weaponLevel++;
            baseDamage += baseDamage * 0.28f;
            if (weaponLevel % 2 == 0)
            {
                projectileCount++;
            }
            if (weaponLevel % 3 == 0)
            {
                pierceCount++;
            }
            baseCooldown = Mathf.Max(0.1f, baseCooldown * 0.95f);
        }

        public virtual void Evolve()
        {
            if (isEvolved || !IsMaxLevel) return;

            isEvolved = true;
            if (!weaponName.StartsWith("Evolved "))
            {
                weaponName = "Evolved " + weaponName;
            }

            baseDamage *= 1.75f;
            projectileCount += 2;
            pierceCount += 2;
            baseCooldown = Mathf.Max(0.08f, baseCooldown * 0.8f);
            attackRange += 1.5f;
        }

        /// <summary>
        /// Helper to find the single nearest active enemy in range.
        /// </summary>
        protected EnemyBase GetNearestEnemy(Vector3 origin, float range)
        {
            if (EnemyHorde.Instance != null)
            {
                return EnemyHorde.Instance.GetNearest(origin, range);
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range);
            EnemyBase nearest = null;
            float minSqrDist = float.MaxValue;

            foreach (var hit in hits)
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null) enemy = hit.GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead && GoblinHorde.World.ArenaMap.IsInCombatView(enemy.WorldPosition))
                {
                    float sqrDist = (enemy.WorldPosition - origin).sqrMagnitude;
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        nearest = enemy;
                    }
                }
            }

            return nearest;
        }

        /// <summary>
        /// Helper to find N nearest active enemies in range.
        /// </summary>
        private readonly List<EnemyBase> _nearestBuffer = new List<EnemyBase>(8);

        protected List<EnemyBase> GetNearestEnemies(Vector3 origin, float range, int count)
        {
            if (EnemyHorde.Instance != null)
            {
                EnemyHorde.Instance.GetNearestMany(origin, range, count, _nearestBuffer);
                return _nearestBuffer;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range);
            List<(EnemyBase enemy, float sqrDist)> enemyList = new List<(EnemyBase, float)>();

            foreach (var hit in hits)
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null) enemy = hit.GetComponentInParent<EnemyBase>();

                if (enemy != null && !enemy.IsDead && GoblinHorde.World.ArenaMap.IsInCombatView(enemy.WorldPosition))
                {
                    float sqrDist = (enemy.WorldPosition - origin).sqrMagnitude;
                    enemyList.Add((enemy, sqrDist));
                }
            }

            enemyList.Sort((a, b) => a.sqrDist.CompareTo(b.sqrDist));

            List<EnemyBase> result = new List<EnemyBase>();
            for (int i = 0; i < Mathf.Min(count, enemyList.Count); i++)
            {
                result.Add(enemyList[i].enemy);
            }

            return result;
        }

        public void RecordDamage(float amount)
        {
            if (amount <= 0f) return;

            DamageDealtThisRun += amount;
            if (playerStats != null)
            {
                playerStats.QueueLifesteal(amount);
            }
        }

        public virtual SfxId HitSfx => SfxId.ImpactArcane;
        public virtual SfxId FireSfx => SfxId.WeaponFire;

        public virtual void OnDealtDamage(EnemyBase enemy, float netDamage)
        {
            // Hit SFX muted for now — per-theme impacts stay wired, not played.
        }

        public virtual void BecomeSuper(WeaponBase rider)
        {
            if (!CanCombine || rider == null || rider == this || !rider.CanCombine) return;

            isSuper = true;
            weaponName = SuperDisplayName(weaponName);
            baseDamage += rider.BaseDamage;
            baseCooldown = Mathf.Max(0.08f, baseCooldown * 0.92f);
            projectileCount = Mathf.Max(projectileCount, rider.ProjectileCount) + 1;
            pierceCount = Mathf.Max(pierceCount, rider.PierceCount);
            attackRange += Mathf.Max(0.75f, rider.AttackRange * 0.25f);
            DamageDealtThisRun += rider.DamageDealtThisRun;
        }

        public static string SuperDisplayName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Super Weapon";
            if (name.StartsWith("Super ")) return name;
            if (name.StartsWith("Evolved ")) name = name.Substring("Evolved ".Length);
            return "Super " + name;
        }

        /// <summary>
        /// Helper method to spawn projectile from ObjectPoolManager or Instantiate.
        /// </summary>
        protected GameObject SpawnProjectile(Vector3 position, Quaternion rotation)
        {
            if (projectilePrefab == null) return null;

            GameObject spawned = ObjectPoolManager.Instance != null
                ? ObjectPoolManager.Instance.Get(projectilePrefab, position, rotation)
                : Instantiate(projectilePrefab, position, rotation);

            if (spawned != null)
            {
                PlayerProjectile projectile = spawned.GetComponent<PlayerProjectile>();
                if (projectile != null)
                {
                    projectile.SetSource(this);
                }
            }

            return spawned;
        }
    }
}
