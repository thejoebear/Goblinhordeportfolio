// Portfolio excerpt from private repo thejoebear/Horde-Survival (branch player-characters).
// Shown for design/portfolio review only. Will not compile as a standalone Unity project.

using System;
using System.Collections.Generic;

namespace GoblinHorde.Progression
{
    /// <summary>
    /// Weighted draft picking and card weights. Kept free of Unity types so
    /// editor self-checks can exercise the rules without entering play mode.
    /// </summary>
    public static class DraftMath
    {
        public const int PhoenixWeight = 2;
        /// <summary>
        /// Chance a level-up pauses for a new-weapon / combine pick.
        /// Other levels auto-apply an upgrade, evolve, stat, or signature.
        /// Deprecated for level-ups: milestones pause for large stats instead.
        /// Kept for any legacy callers / self-checks that still reference it.
        /// </summary>
        public const int WeaponOfferChancePercent = 40;
        public const int MilestoneEveryLevels = 10;

        public static bool IsMilestoneLevel(int level)
        {
            return level > 0 && level % MilestoneEveryLevels == 0;
        }

        public static bool IsCrumbStat(StatType stat)
        {
            switch (stat)
            {
                case StatType.ProjectileCount:
                case StatType.Pierce:
                case StatType.Revival:
                case StatType.CritDamage:
                case StatType.GoldFind:
                case StatType.IFrames:
                case StatType.CooldownReduction: // merged into AttackSpeed
                case StatType.StaminaRegen:
                    return false;
                default:
                    return true;
            }
        }

        public static float MilestoneAmount(StatType stat)
        {
            switch (stat)
            {
                case StatType.Damage: return 0.18f;
                case StatType.AttackSpeed: return 0.15f;
                case StatType.CooldownReduction: return 0.14f;
                case StatType.MaxHealth: return 70f;
                case StatType.MoveSpeed: return 0.18f;
                case StatType.Armor: return 3.5f;
                case StatType.Area: return 0.18f;
                case StatType.PickupRadius: return 2.75f;
                case StatType.ProjectileCount: return 1f;
                case StatType.Pierce: return 1f;
                case StatType.CritChance: return 0.15f;
                case StatType.CritDamage: return 0.25f;
                case StatType.StaminaRegen: return 0.40f;
                case StatType.XpGain: return 0.20f;
                case StatType.GoldFind: return 0.25f;
                case StatType.Regen: return 0.85f;
                case StatType.Lifesteal: return 0.03f;
                case StatType.Luck: return 0.30f;
                case StatType.IFrames: return 0.18f;
                case StatType.HealOnKill: return 0.4f;
                case StatType.Revival: return 1f;
                default: return 0.15f;
            }
        }

        public static int MilestoneWeight(StatType stat, int level)
        {
            bool late = level >= 30;
            switch (stat)
            {
                case StatType.Damage:
                case StatType.AttackSpeed:
                case StatType.Area:
                case StatType.MaxHealth:
                case StatType.MoveSpeed:
                case StatType.StaminaRegen:
                    return late ? 14 : 18;

                case StatType.Armor:
                case StatType.PickupRadius:
                    return 10;

                case StatType.CritChance:
                case StatType.CritDamage:
                case StatType.Regen:
                case StatType.HealOnKill:
                case StatType.XpGain:
                case StatType.Pierce:
                    return 7;

                case StatType.CooldownReduction:
                    return 0; // merged into AttackSpeed
                case StatType.ProjectileCount:
                    return 5;

                case StatType.Lifesteal:
                case StatType.IFrames:
                    return late ? 5 : 3;

                case StatType.Luck:
                case StatType.GoldFind:
                    return late ? 4 : 2;

                case StatType.Revival:
                    return late ? PhoenixWeight + 1 : 1;

                default:
                    return 6;
            }
        }

        public static int DefaultWeight(UpgradeType type, StatType stat, string id)
        {
            switch (type)
            {
                case UpgradeType.EvolveWeapon: return 22;
                case UpgradeType.Signature: return 20;
                case UpgradeType.CombineWeapon: return 22;
                case UpgradeType.UpgradeWeapon: return 16;
                case UpgradeType.NewWeapon: return 14;
                case UpgradeType.StatBoost: return StatWeight(stat, id);
                default: return 8;
            }
        }

        public static int WeightFor(UpgradeData upgrade)
        {
            if (upgrade == null) return 0;
            if (upgrade.DraftWeight > 0) return upgrade.DraftWeight;
            return DefaultWeight(upgrade.UpgradeType, upgrade.StatType, upgrade.ID);
        }

        public static bool IsWeaponCard(UpgradeType type)
        {
            return type == UpgradeType.NewWeapon
                || type == UpgradeType.UpgradeWeapon
                || type == UpgradeType.EvolveWeapon
                || type == UpgradeType.CombineWeapon;
        }

        public static bool IsWeaponCard(UpgradeData upgrade)
        {
            return upgrade != null && IsWeaponCard(upgrade.UpgradeType);
        }

        /// <summary>
        /// Chests still pause for new weapons / combines. Level-ups use milestones instead.
        /// </summary>
        public static bool PausesForDraft(UpgradeType type)
        {
            return type == UpgradeType.NewWeapon
                || type == UpgradeType.CombineWeapon;
        }

        public static bool PausesForDraft(UpgradeData upgrade)
        {
            return upgrade != null && PausesForDraft(upgrade.UpgradeType);
        }

        public static bool StatsClash(UpgradeData a, UpgradeData b)
        {
            if (a == null || b == null) return false;
            if (a.UpgradeType != UpgradeType.StatBoost || b.UpgradeType != UpgradeType.StatBoost)
            {
                return false;
            }

            return a.StatType == b.StatType;
        }

        public static T PickOne<T>(IList<T> pool, Func<T, int> weightOf, Func<int, int> nextExclusive)
        {
            if (pool == null || pool.Count == 0) return default;
            if (nextExclusive == null) return pool[0];

            int total = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                total += Positive(weightOf(pool[i]));
            }

            if (total <= 0)
            {
                return pool[nextExclusive(pool.Count)];
            }

            int roll = nextExclusive(total);
            int acc = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                acc += Positive(weightOf(pool[i]));
                if (roll < acc)
                {
                    return pool[i];
                }
            }

            return pool[pool.Count - 1];
        }

        public static List<T> PickUnique<T>(
            IList<T> pool,
            int count,
            Func<T, int> weightOf,
            Func<int, int> nextExclusive,
            Func<T, T, bool> clashesWith = null)
        {
            List<T> selected = new List<T>();
            if (pool == null || pool.Count == 0 || count <= 0) return selected;

            List<T> remaining = new List<T>(pool);
            int pickCount = count < remaining.Count ? count : remaining.Count;
            for (int i = 0; i < pickCount; i++)
            {
                if (remaining.Count == 0) break;
                T picked = PickOne(remaining, weightOf, nextExclusive);
                selected.Add(picked);
                remaining.Remove(picked);
                if (clashesWith == null) continue;
                for (int j = remaining.Count - 1; j >= 0; j--)
                {
                    if (clashesWith(picked, remaining[j]))
                    {
                        remaining.RemoveAt(j);
                    }
                }
            }

            return selected;
        }

        public static void EnsureWeaponPresent<T>(
            List<T> selected,
            IList<T> pool,
            Func<T, bool> isWeapon,
            Func<T, int> weightOf,
            Func<int, int> nextExclusive,
            Func<T, bool> preferWeapon = null)
        {
            if (selected == null || selected.Count == 0 || pool == null) return;

            for (int i = 0; i < selected.Count; i++)
            {
                if (isWeapon(selected[i])) return;
            }

            List<T> weapons = new List<T>();
            List<T> preferred = preferWeapon != null ? new List<T>() : null;
            for (int i = 0; i < pool.Count; i++)
            {
                if (!isWeapon(pool[i])) continue;
                weapons.Add(pool[i]);
                if (preferred != null && preferWeapon(pool[i]))
                {
                    preferred.Add(pool[i]);
                }
            }

            if (weapons.Count == 0) return;

            T weapon = PickOne(
                preferred != null && preferred.Count > 0 ? preferred : weapons,
                weightOf,
                nextExclusive);

            for (int i = selected.Count - 1; i >= 0; i--)
            {
                if (!isWeapon(selected[i]))
                {
                    selected[i] = weapon;
                    return;
                }
            }

            selected[selected.Count - 1] = weapon;
        }

        public static string RunSelfCheck()
        {
            if (DefaultWeight(UpgradeType.EvolveWeapon, StatType.Damage, "") <= PhoenixWeight)
            {
                return "Evolve cards should outweigh Phoenix Down.";
            }

            if (DefaultWeight(UpgradeType.CombineWeapon, StatType.Damage, "") <= PhoenixWeight)
            {
                return "Combine cards should outweigh Phoenix Down.";
            }

            if (!IsWeaponCard(UpgradeType.CombineWeapon))
            {
                return "Combine cards should count as weapon cards.";
            }

            if (!IsMilestoneLevel(10) || !IsMilestoneLevel(20) || IsMilestoneLevel(11))
            {
                return "Milestones should land on every 10 levels.";
            }

            if (IsCrumbStat(StatType.ProjectileCount) || IsCrumbStat(StatType.Revival))
            {
                return "Projectile and revival should not be crumb stats.";
            }

            if (MilestoneAmount(StatType.Damage) < 0.14f
                || MilestoneAmount(StatType.MaxHealth) < 50f)
            {
                return "Milestone amounts should clearly beat crumbs.";
            }

            if (MilestoneWeight(StatType.Damage, 10)
                <= MilestoneWeight(StatType.Revival, 10))
            {
                return "Early milestones should prefer combat stats over revival.";
            }

            if (!PausesForDraft(UpgradeType.NewWeapon)
                || !PausesForDraft(UpgradeType.CombineWeapon))
            {
                return "New weapon and combine cards should pause for a pick.";
            }

            if (PausesForDraft(UpgradeType.UpgradeWeapon)
                || PausesForDraft(UpgradeType.EvolveWeapon)
                || PausesForDraft(UpgradeType.StatBoost)
                || PausesForDraft(UpgradeType.Signature))
            {
                return "Upgrades, evolves, stats, and signature should auto-apply.";
            }

            if (DefaultWeight(UpgradeType.UpgradeWeapon, StatType.Damage, "")
                <= DefaultWeight(UpgradeType.StatBoost, StatType.Luck, "lucky_charm"))
            {
                return "Weapon upgrades should outweigh luck cards.";
            }

            if (DefaultWeight(UpgradeType.StatBoost, StatType.Damage, "sharpened_blade")
                <= DefaultWeight(UpgradeType.StatBoost, StatType.Revival, "phoenix_down"))
            {
                return "Damage cards should outweigh Phoenix Down.";
            }

            List<string> pool = new List<string> { "magnet", "regen", "weapon" };
            List<string> picked = new List<string> { "magnet", "regen", "luck" };
            EnsureWeaponPresent(picked, pool, s => s == "weapon", s => 1, n => 0);
            if (!picked.Contains("weapon"))
            {
                return "Weapon floor failed to inject a weapon card.";
            }

            List<string> mixed = new List<string> { "upgrade", "new" };
            List<string> statsOnly = new List<string> { "magnet", "regen", "luck" };
            EnsureWeaponPresent(statsOnly, mixed, s => s == "upgrade" || s == "new", s => s == "upgrade" ? 16 : 4, n => 0, s => s == "new");
            if (!statsOnly.Contains("new"))
            {
                return "Weapon floor should prefer a new weapon over an upgrade.";
            }

            List<string> twins = new List<string> { "nova_a", "nova_b", "zap" };
            List<string> distinct = PickUnique(twins, 2, s => 1, n => 0, (a, b) => a == b);
            if (distinct.Count < 2)
            {
                return "Draft should allow two weapons from the same family.";
            }

            List<string> onePool = new List<string> { "a", "b", "c" };
            string one = PickOne(onePool, s => s == "c" ? 100 : 1, n => 2);
            if (one != "a" && one != "b" && one != "c")
            {
                return "Weighted pick returned nothing.";
            }

            return null;
        }

        private static int StatWeight(StatType stat, string id)
        {
            if (id == "phoenix_down") return PhoenixWeight;

            switch (stat)
            {
                // Core power: always relevant regardless of build.
                case StatType.MaxHealth:
                case StatType.Damage:
                case StatType.AttackSpeed:
                    return 12;

                // Strong generalist picks.
                case StatType.Area:
                case StatType.Armor:
                case StatType.MoveSpeed:
                case StatType.PickupRadius:
                    return 9;

                // Build enablers and steady utility.
                case StatType.Duration:
                case StatType.CritChance:
                case StatType.CritDamage:
                case StatType.Regen:
                case StatType.HealOnKill:
                case StatType.XpGain:
                case StatType.Pierce:
                    return 6;

                // Rare spike: +1 projectile on every weapon is the biggest
                // single power jump in the pool, so it shows up sparingly.
                case StatType.ProjectileCount:
                    return 5;

                // Niche and economy picks.
                case StatType.Luck:
                case StatType.Lifesteal:
                case StatType.GoldFind:
                case StatType.MagnetSpeed:
                case StatType.HurtboxSize:
                case StatType.IFrames:
                    return 4;

                case StatType.KnockbackDealt:
                case StatType.KnockbackTaken:
                    return 0; // combat knockback removed
                case StatType.StaminaRegen:
                    return 0; // milestone-only
                case StatType.CooldownReduction:
                    return 0; // merged into AttackSpeed
                case StatType.Revival:
                    return PhoenixWeight;
                default:
                    return 6;
            }
        }

        private static int Positive(int weight)
        {
            return weight < 0 ? 0 : weight;
        }
    }
}
