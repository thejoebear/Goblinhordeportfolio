// Portfolio excerpt from Joseph's local Goblin Horde Unity project (ahead of private GitHub).
// Shown for design/portfolio review only. Will not compile as a standalone Unity project.

using System;
using UnityEngine;
using GoblinHorde.Core;

namespace GoblinHorde.Progression
{
    public enum GearSlot
    {
        Helm = 0,
        Shoulders = 1,
        Necklace = 2,
        Cloak = 3,
        Chest = 4,
        Gloves = 5,
        Belt = 6,
        Boots = 7,
        Ring = 8
    }

    public enum GearRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    public enum GearStat
    {
        Health = 0,
        Armor = 1,
        MoveSpeed = 2,
        Damage = 3,
        AttackSpeed = 4,
        CritChance = 5,
        Area = 6,
        Pickup = 7,
        GoldFind = 8,
        Regen = 9
    }

    public struct GearStatBonus
    {
        public float Health;
        public float Armor;
        public float MoveSpeed;
        public float Damage;
        public float AttackSpeed;
        public float Crit;
        public float Area;
        public float Pickup;
        public float GoldFind;
        public float Regen;
    }

    /// <summary>
    /// Where a worn piece sits on the body. Anchor is 0-1 over the sprite, origin bottom-left.
    /// </summary>
    public struct GearMark
    {
        public int WornIndex;
        public bool Behind;
        public Vector2 Anchor;
        public Vector2 Size;
        public bool Round;

        public GearMark(int wornIndex, bool behind, float x, float y, float w, float h, bool round)
        {
            WornIndex = wornIndex;
            Behind = behind;
            Anchor = new Vector2(x, y);
            Size = new Vector2(w, h);
            Round = round;
        }
    }

    [Serializable]
    public class GearItemSave
    {
        public int id;
        public int slot;
        public int rarity;
        public string name;
        public int price;
        public float red = 1f;
        public float green = 1f;
        public float blue = 1f;
        public int stat0 = -1;
        public int stat1 = -1;
        public int stat2 = -1;
        public int stat3 = -1;
        public int stat4 = -1;
        public float value0;
        public float value1;
        public float value2;
        public float value3;
        public float value4;

        public Color Tint => new Color(red, green, blue, 1f);

        public int AffixCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < 5; i++)
                {
                    if (HasAffix(i)) count++;
                }

                return count;
            }
        }

        public bool HasAffix(int index)
        {
            int id = StatId(index);
            if (id < 0 || id >= GearCatalog.StatCount) return false;
            // Saves from before epic omit these fields, so they load as Health with a zero value.
            if (index >= 3 && StatValue(index) == 0f) return false;
            return true;
        }

        public int StatId(int index)
        {
            if (index == 0) return stat0;
            if (index == 1) return stat1;
            if (index == 2) return stat2;
            if (index == 3) return stat3;
            if (index == 4) return stat4;
            return -1;
        }

        public float StatValue(int index)
        {
            if (index == 0) return value0;
            if (index == 1) return value1;
            if (index == 2) return value2;
            if (index == 3) return value3;
            if (index == 4) return value4;
            return 0f;
        }

        public GearItemSave Clone()
        {
            return new GearItemSave
            {
                id = id,
                slot = slot,
                rarity = rarity,
                name = name,
                price = price,
                red = red,
                green = green,
                blue = blue,
                stat0 = stat0,
                stat1 = stat1,
                stat2 = stat2,
                stat3 = stat3,
                stat4 = stat4,
                value0 = value0,
                value1 = value1,
                value2 = value2,
                value3 = value3,
                value4 = value4
            };
        }
    }

    [Serializable]
    public class GearOfferSave
    {
        public bool sold;
        public GearItemSave item;
    }

    /// <summary>
    /// Rolls named gear with a short stat list. No unique item catalog.
    /// </summary>
    public static class GearCatalog
    {
        public const int WornSlotCount = 10;
        public const int StatCount = 10;

        public static readonly GearSlot[] WornSlots =
        {
            GearSlot.Helm,
            GearSlot.Shoulders,
            GearSlot.Necklace,
            GearSlot.Cloak,
            GearSlot.Chest,
            GearSlot.Gloves,
            GearSlot.Belt,
            GearSlot.Boots,
            GearSlot.Ring,
            GearSlot.Ring
        };

        public static readonly GearMark[] Marks =
        {
            new GearMark(3, true, 0.50f, 0.42f, 0.78f, 0.62f, false),
            new GearMark(0, false, 0.50f, 0.86f, 0.44f, 0.16f, true),
            new GearMark(1, false, 0.28f, 0.70f, 0.24f, 0.14f, true),
            new GearMark(1, false, 0.72f, 0.70f, 0.24f, 0.14f, true),
            new GearMark(4, false, 0.50f, 0.56f, 0.40f, 0.22f, true),
            new GearMark(6, false, 0.50f, 0.44f, 0.42f, 0.08f, true),
            new GearMark(5, false, 0.20f, 0.48f, 0.16f, 0.14f, true),
            new GearMark(5, false, 0.80f, 0.48f, 0.16f, 0.14f, true),
            new GearMark(7, false, 0.38f, 0.14f, 0.18f, 0.12f, true),
            new GearMark(7, false, 0.62f, 0.14f, 0.18f, 0.12f, true),
            new GearMark(2, false, 0.50f, 0.74f, 0.14f, 0.08f, true),
            new GearMark(8, false, 0.84f, 0.44f, 0.09f, 0.09f, true),
            new GearMark(9, false, 0.16f, 0.44f, 0.09f, 0.09f, true)
        };

        static readonly string[] SlotNames =
        {
            "Helm", "Shoulders", "Necklace", "Cloak", "Chest", "Gloves", "Belt", "Boots", "Ring"
        };

        static readonly string[] AdjA =
        {
            "Stout", "Iron", "Quick", "Keen", "Swift", "Precise", "Wide", "Long", "Gilded", "Mending"
        };

        static readonly string[] AdjB =
        {
            "Hardy", "Plated", "Fleet", "Brutal", "Hasty", "Deadly", "Vast", "Gathering", "Rich", "Vital"
        };

        static readonly GearStat[] BiasHelm = { GearStat.Health, GearStat.Armor };
        static readonly GearStat[] BiasCloak = { GearStat.Armor, GearStat.MoveSpeed };
        static readonly GearStat[] BiasShoulders = { GearStat.Armor, GearStat.Area };
        static readonly GearStat[] BiasHands = { GearStat.Damage, GearStat.AttackSpeed, GearStat.CritChance };
        static readonly GearStat[] BiasBoots = { GearStat.MoveSpeed, GearStat.Pickup };
        static readonly GearStat[] BiasNeck = { GearStat.GoldFind, GearStat.Regen };
        static readonly GearStat[] BiasBelt = { GearStat.Health, GearStat.Regen };

        static readonly float[] ValueLow =
        {
            10f, 0.5f, 0.10f, 0.04f, 0.03f, 0.02f, 0.04f, 0.20f, 0.05f, 0.05f
        };

        static readonly float[] ValueHigh =
        {
            36f, 1.6f, 0.28f, 0.12f, 0.08f, 0.05f, 0.10f, 0.55f, 0.14f, 0.14f
        };

        public static GearSlot WornSlot(int index)
        {
            if (index < 0 || index >= WornSlots.Length) return GearSlot.Helm;
            return WornSlots[index];
        }

        public static string SlotName(int slot)
        {
            if (slot < 0 || slot >= SlotNames.Length) return "Gear";
            return SlotNames[slot];
        }

        public static string RarityName(int rarity)
        {
            switch ((GearRarity)rarity)
            {
                case GearRarity.Uncommon: return "Uncommon";
                case GearRarity.Rare: return "Rare";
                case GearRarity.Epic: return "Epic";
                case GearRarity.Legendary: return "Legendary";
                default: return "Common";
            }
        }

        public static int Price(int rarity)
        {
            switch ((GearRarity)rarity)
            {
                case GearRarity.Uncommon: return Balance.GearUncommonGold;
                case GearRarity.Rare: return Balance.GearRareGold;
                case GearRarity.Epic: return Balance.GearEpicGold;
                case GearRarity.Legendary: return Balance.GearLegendaryGold;
                default: return Balance.GearCommonGold;
            }
        }

        public static int ScrapValue(GearItemSave item)
        {
            if (item == null) return 0;
            int price = item.price > 0 ? item.price : Price(item.rarity);
            return Mathf.Max(0, price * Balance.GearScrapPercent / 100);
        }

        public static int CasePrice(int tier)
        {
            if (tier >= 2) return Balance.GearWarchiefChestGold;
            if (tier == 1) return Balance.GearWarCrateGold;
            return Balance.GearCaseGold;
        }

        public static string CaseName(int tier)
        {
            if (tier >= 2) return "Warchief chest";
            if (tier == 1) return "War crate";
            return "Camp sack";
        }

        public static GearItemSave Roll(ref int nextId)
        {
            return RollCase(ref nextId, 0);
        }

        public static GearItemSave RollCase(ref int nextId, int tier)
        {
            if (nextId < 1) nextId = 1;
            int id = nextId;
            nextId++;

            GearSlot slot = (GearSlot)UnityEngine.Random.Range(0, SlotNames.Length);
            GearRarity rarity = CaseRarity(tier);
            int affixes = (int)rarity + 1;

            var item = new GearItemSave
            {
                id = id,
                slot = (int)slot,
                rarity = (int)rarity,
                price = Price((int)rarity),
                stat0 = -1,
                stat1 = -1,
                stat2 = -1,
                stat3 = -1,
                stat4 = -1
            };

            bool[] used = new bool[StatCount];
            GearStat lead = GearStat.Health;
            for (int i = 0; i < affixes; i++)
            {
                GearStat stat = PickStat(slot, used);
                used[(int)stat] = true;
                if (i == 0) lead = stat;
                SetStat(item, i, stat, RollValue(stat, rarity));
            }

            string adjective = UnityEngine.Random.Range(0, 2) == 0 ? AdjA[(int)lead] : AdjB[(int)lead];
            item.name = adjective + " " + SlotName((int)slot);
            Color tint = RarityTint(rarity, slot);
            item.red = tint.r;
            item.green = tint.g;
            item.blue = tint.b;
            return item;
        }

        static GearRarity CaseRarity(int tier)
        {
            int roll = UnityEngine.Random.Range(0, 100);
            if (tier >= 2)
            {
                if (roll < 50) return GearRarity.Rare;
                if (roll < 85) return GearRarity.Epic;
                return GearRarity.Legendary;
            }

            if (tier == 1)
            {
                if (roll < 45) return GearRarity.Uncommon;
                if (roll < 80) return GearRarity.Rare;
                if (roll < 95) return GearRarity.Epic;
                return GearRarity.Legendary;
            }

            if (roll < 52) return GearRarity.Common;
            if (roll < 82) return GearRarity.Uncommon;
            if (roll < 95) return GearRarity.Rare;
            if (roll < 99) return GearRarity.Epic;
            return GearRarity.Legendary;
        }

        public static string Describe(GearItemSave item)
        {
            if (item == null) return "";
            string stats = StatLines(item, 2);
            string text = RarityName(item.rarity) + " " + SlotName(item.slot);
            if (stats.Length > 0) text += "\n" + stats;
            return text;
        }

        public static string StatLines(GearItemSave item)
        {
            return StatLines(item, 1);
        }

        public static string StatLines(GearItemSave item, int columns)
        {
            if (item == null) return "";
            if (columns < 1) columns = 1;
            string text = "";
            int shown = 0;
            for (int i = 0; i < 5; i++)
            {
                if (!item.HasAffix(i)) continue;
                if (shown > 0) text += shown % columns == 0 ? "\n" : "   ";
                text += FormatStat(item.StatId(i), item.StatValue(i));
                shown++;
            }

            return text;
        }

        public static string FormatStat(int statId, float value)
        {
            if (statId < 0 || statId >= StatCount) return "";
            GearStat stat = (GearStat)statId;
            switch (stat)
            {
                case GearStat.Health:
                    return "+" + Mathf.Max(1, Mathf.RoundToInt(value)) + " Health";
                case GearStat.Armor:
                    return "+" + value.ToString("0.0") + " Armor";
                case GearStat.MoveSpeed:
                    return "+" + value.ToString("0.00") + " Move";
                case GearStat.Damage:
                    return "+" + Percent(value) + "% Damage";
                case GearStat.AttackSpeed:
                    return "+" + Percent(value) + "% Attack speed";
                case GearStat.CritChance:
                    return "+" + Percent(value) + "% Crit";
                case GearStat.Area:
                    return "+" + Percent(value) + "% Area";
                case GearStat.Pickup:
                    return "+" + value.ToString("0.00") + " Pickup";
                case GearStat.GoldFind:
                    return "+" + Percent(value) + "% Gold";
                case GearStat.Regen:
                    return "+" + value.ToString("0.00") + " Regen";
                default:
                    return "";
            }
        }

        public static void Accumulate(ref GearStatBonus bonus, GearItemSave item)
        {
            if (item == null) return;
            for (int i = 0; i < 5; i++)
            {
                if (!item.HasAffix(i)) continue;
                float value = item.StatValue(i);
                switch ((GearStat)item.StatId(i))
                {
                    case GearStat.Health: bonus.Health += value; break;
                    case GearStat.Armor: bonus.Armor += value; break;
                    case GearStat.MoveSpeed: bonus.MoveSpeed += value; break;
                    case GearStat.Damage: bonus.Damage += value; break;
                    case GearStat.AttackSpeed: bonus.AttackSpeed += value; break;
                    case GearStat.CritChance: bonus.Crit += value; break;
                    case GearStat.Area: bonus.Area += value; break;
                    case GearStat.Pickup: bonus.Pickup += value; break;
                    case GearStat.GoldFind: bonus.GoldFind += value; break;
                    case GearStat.Regen: bonus.Regen += value; break;
                }
            }
        }

        public static int FirstWornIndex(int[] worn, GearSlot slot)
        {
            int first = -1;
            if (worn == null) return -1;
            for (int i = 0; i < worn.Length && i < WornSlots.Length; i++)
            {
                if (WornSlots[i] != slot) continue;
                if (first < 0) first = i;
                if (worn[i] == 0) return i;
            }

            return first;
        }

        static int Percent(float value)
        {
            return Mathf.Max(1, Mathf.RoundToInt(value * 100f));
        }

        static float RollValue(GearStat stat, GearRarity rarity)
        {
            int index = (int)stat;
            float u = (int)rarity / 2f;
            float span = Mathf.LerpUnclamped(ValueLow[index], ValueHigh[index], u);
            return span * UnityEngine.Random.Range(0.9f, 1.1f);
        }

        static GearStat PickStat(GearSlot slot, bool[] used)
        {
            GearStat[] bias = Bias(slot);
            if (UnityEngine.Random.value < 0.7f)
            {
                int open = 0;
                for (int i = 0; i < bias.Length; i++)
                {
                    if (!used[(int)bias[i]]) open++;
                }

                if (open > 0)
                {
                    int pick = UnityEngine.Random.Range(0, open);
                    for (int i = 0; i < bias.Length; i++)
                    {
                        if (used[(int)bias[i]]) continue;
                        if (pick == 0) return bias[i];
                        pick--;
                    }
                }
            }

            int any = 0;
            for (int i = 0; i < StatCount; i++)
            {
                if (!used[i]) any++;
            }

            if (any <= 0) return GearStat.Health;
            int choice = UnityEngine.Random.Range(0, any);
            for (int i = 0; i < StatCount; i++)
            {
                if (used[i]) continue;
                if (choice == 0) return (GearStat)i;
                choice--;
            }

            return GearStat.Health;
        }

        static GearStat[] Bias(GearSlot slot)
        {
            switch (slot)
            {
                case GearSlot.Helm:
                case GearSlot.Chest:
                    return BiasHelm;
                case GearSlot.Cloak:
                    return BiasCloak;
                case GearSlot.Shoulders:
                    return BiasShoulders;
                case GearSlot.Gloves:
                case GearSlot.Ring:
                    return BiasHands;
                case GearSlot.Boots:
                    return BiasBoots;
                case GearSlot.Necklace:
                    return BiasNeck;
                default:
                    return BiasBelt;
            }
        }

        static void SetStat(GearItemSave item, int index, GearStat stat, float value)
        {
            if (index == 0)
            {
                item.stat0 = (int)stat;
                item.value0 = value;
            }
            else if (index == 1)
            {
                item.stat1 = (int)stat;
                item.value1 = value;
            }
            else if (index == 2)
            {
                item.stat2 = (int)stat;
                item.value2 = value;
            }
            else if (index == 3)
            {
                item.stat3 = (int)stat;
                item.value3 = value;
            }
            else if (index == 4)
            {
                item.stat4 = (int)stat;
                item.value4 = value;
            }
        }

        static Color RarityTint(GearRarity rarity, GearSlot slot)
        {
            Color color;
            switch (rarity)
            {
                case GearRarity.Uncommon:
                    color = new Color(0.42f, 0.78f, 0.40f, 1f);
                    break;
                case GearRarity.Rare:
                    color = new Color(0.38f, 0.58f, 0.95f, 1f);
                    break;
                case GearRarity.Epic:
                    color = new Color(0.64f, 0.36f, 0.90f, 1f);
                    break;
                case GearRarity.Legendary:
                    color = new Color(0.95f, 0.62f, 0.18f, 1f);
                    break;
                default:
                    color = new Color(0.78f, 0.74f, 0.66f, 1f);
                    break;
            }

            float lift = (int)slot * 0.012f;
            color.r = Mathf.Clamp01(color.r + lift);
            color.g = Mathf.Clamp01(color.g + lift * 0.5f);
            color.b = Mathf.Clamp01(color.b - lift * 0.25f);
            return color;
        }
    }
}
