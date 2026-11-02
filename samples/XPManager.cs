// Portfolio excerpt from private repo thejoebear/Horde-Survival (branch player-characters).
// Shown for design/portfolio review only. Will not compile as a standalone Unity project.

using System;
using UnityEngine;
using GoblinHorde.Core;
using GoblinHorde.Player;
using GoblinHorde.Progression;
using GoblinHorde.UI;

namespace GoblinHorde.Managers
{
    /// <summary>
    /// Manages player XP accumulation, leveling up, and draft or auto-stat upgrades.
    /// Kills queue XP; one LateUpdate flush applies the bar, events, and drafts.
    /// Runs after ObjectPoolManager so deferred CompleteDeath grants land same frame.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class XPManager : MonoBehaviour
    {
        public static XPManager Instance { get; private set; }

        public event Action<int, int, int> OnXPChanged; // currentXP, currentLevel, xpToNextLevel
        public event Action<int> OnLevelUp;
        public event Action<int> LevelUpEvent; // Alias event for level up listeners

        [Header("XP & Level Settings")]
        [SerializeField] private int currentXP = 0;
        [SerializeField] private int currentLevel = 1;

        private int _pendingXp;
        private bool _flushing;

        public int CurrentXP
        {
            get
            {
                BankPendingXp();
                return currentXP;
            }
        }

        public int CurrentLevel
        {
            get
            {
                BankPendingXp();
                return currentLevel;
            }
        }

        public int XPToNextLevel
        {
            get
            {
                BankPendingXp();
                return GetXPRequiredForLevel(currentLevel);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _pendingXp = 0;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt > 0f) GoblinHorde.Pickups.XPCrystal.TickAll(dt);

            BankPendingXp();
            if (CanConvertLevels())
            {
                ConvertLevels(Balance.MaxLevelsPerXpGrant);
            }
        }

        /// <summary>
        /// Queues XP from a kill or gem. Multiplier is applied here so a batched
        /// flush matches per-grant rounding (1 XP * 1.25x stays 1, not 1.25n).
        /// </summary>
        public void AddXP(int amount)
        {
            if (amount <= 0) return;
            _pendingXp += ScaleGrantedXp(amount);
        }

        /// <summary>
        /// Adds exactly enough XP for <paramref name="count"/> level-ups.
        /// Ignores the XP-gain multiplier so a boon is ten levels, not more.
        /// </summary>
        public void GrantExactLevels(int count)
        {
            if (count <= 0) return;
            BankPendingXp();
            int sum = 0;
            int level = currentLevel;
            for (int i = 0; i < count; i++)
            {
                sum += GetXPRequiredForLevel(level);
                level++;
            }

            currentXP += sum;
            ConvertLevels(count);
        }

        /// <summary>
        /// Turns remaining banked XP into levels. Used at run end so recap gold
        /// sees the level the player actually earned.
        /// </summary>
        public void SettleRunXp()
        {
            BankPendingXp();
            ConvertLevels(256);
        }

        /// <summary>
        /// Dev/test: set hero level without firing draft level-ups.
        /// </summary>
        public void DebugSetLevel(int level)
        {
            BankPendingXp();
            currentLevel = Mathf.Max(1, level);
            currentXP = 0;
            _pendingXp = 0;
            OnXPChanged?.Invoke(currentXP, currentLevel, GetXPRequiredForLevel(currentLevel));
        }

        private void BankPendingXp()
        {
            if (_pendingXp <= 0) return;
            currentXP += _pendingXp;
            _pendingXp = 0;
        }

        private static bool CanConvertLevels()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return true;
            GameState state = gm.CurrentState;
            return state == GameState.Playing || state == GameState.BossFight;
        }

        private void ConvertLevels(int maxLevels)
        {
            if (_flushing || maxLevels <= 0) return;

            int need = GetXPRequiredForLevel(currentLevel);
            if (currentXP < need || need <= 0) return;

            _flushing = true;
            int levelsGained = 0;
            int safety = 0;
            while (currentXP >= need && need > 0 && safety < maxLevels)
            {
                currentXP -= need;
                currentLevel++;
                levelsGained++;
                safety++;
                OnLevelUp?.Invoke(currentLevel);
                LevelUpEvent?.Invoke(currentLevel);
                need = GetXPRequiredForLevel(currentLevel);
            }

            if (levelsGained > 0)
            {
                OnXPChanged?.Invoke(currentXP, currentLevel, need);
            }

            if (levelsGained > 0 && GameManager.Instance != null)
            {
                int firstGained = currentLevel - levelsGained + 1;
                for (int i = 0; i < levelsGained; i++)
                {
                    GameManager.Instance.RequestLevelUp(firstGained + i);
                }
            }

            _flushing = false;
        }

        private static int ScaleGrantedXp(int amount)
        {
            if (PlayerController.Instance == null || PlayerController.Instance.Stats == null)
            {
                return amount;
            }

            float xpMult = PlayerController.Instance.Stats.XpGainMultiplier;
            if (xpMult == 1f)
            {
                return amount;
            }

            return Mathf.Max(1, Mathf.RoundToInt(amount * xpMult));
        }

        /// <summary>
        /// Applies a crumb + random weapon level without pausing (used by tools / fallbacks).
        /// </summary>
        public static void ApplyRandomLevelUp()
        {
            int level = Instance != null ? Instance.CurrentLevel : 0;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ApplyNormalLevelRewards(level);
                return;
            }

            UpgradeData picked = UpgradeDatabase.PickRandomLevelUp();
            if (picked != null && WeaponInventory.Instance != null)
            {
                WeaponInventory.Instance.ApplyUpgrade(picked);
            }

            if (HUDManager.Instance != null)
            {
                string title = picked != null ? picked.FormatPayoff() : "Upgrade";
                HUDManager.Instance.ShowLevelUpBanner(level, title);
            }
        }

        /// <summary>
        /// Calculates required XP for given level using formula requiredXP = base * level^xpGrowthFactor
        /// </summary>
        public int GetXPRequiredForLevel(int level)
        {
            int lvl = Mathf.Max(1, level);
            if (lvl <= Balance.EarlyLevelCap)
            {
                return Balance.EarlyXpBase + Balance.EarlyXpPerLevel * (lvl - 1);
            }

            double earlyEnd = Balance.EarlyXpBase + Balance.EarlyXpPerLevel * (Balance.EarlyLevelCap - 1);
            if (lvl <= Balance.MidLevelCap)
            {
                return ClampXpCost(earlyEnd * System.Math.Pow(lvl / (double)Balance.EarlyLevelCap, Balance.MidXpGrowth));
            }

            double midEnd = earlyEnd * System.Math.Pow(
                Balance.MidLevelCap / (double)Balance.EarlyLevelCap,
                Balance.MidXpGrowth);
            return ClampXpCost(midEnd * System.Math.Pow(lvl / (double)Balance.MidLevelCap, Balance.LateXpGrowth));
        }

        private static int ClampXpCost(double cost)
        {
            if (cost >= int.MaxValue) return int.MaxValue;
            return Mathf.Max(1, (int)System.Math.Round(cost));
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
