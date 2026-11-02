// Portfolio excerpt from Joseph's local Goblin Horde Unity project (ahead of private GitHub).
// Shown for design/portfolio review only. Will not compile as a standalone Unity project.

using System;
using UnityEngine;

namespace GoblinHorde.Player
{
    /// <summary>
    /// Named tree shapes. Each template owns row/col/max/gate/requires.
    /// Gates advance by <see cref="TalentLayouts.PointsPerTier"/> per row.
    /// Every non-captstone row offers at least that many max ranks so tiers stay reachable.
    /// Requires links are sparse - only rare, related pairs (usually climax → mid Unique).
    /// </summary>
    public readonly struct TalentLayout
    {
        public readonly string Id;
        public readonly int[] Row;
        public readonly int[] Col;
        public readonly int[] Max;
        public readonly int[] Gate;
        /// <summary>Index of a required sibling, or -1. Must already have rank &gt; 0 to invest.</summary>
        public readonly int[] Requires;

        public int Count => Row != null ? Row.Length : 0;

        public TalentLayout(string id, int[] row, int[] col, int[] max, int[] gate, int[] requires)
        {
            Id = id;
            Row = row;
            Col = col;
            Max = max;
            Gate = gate;
            Requires = requires;
        }
    }

    public static class TalentLayouts
    {
        public const int PointsPerTier = 5;

        // Deep narrow. Mid Unique shares a row so the tier still offers 5 ranks.
        // Climax → prior Unique link is baked in TalentCatalog.Tree (rare, related).
        public static readonly TalentLayout Spear = Build(
            "spear",
            new[] { 0, 1, 2, 2, 3, 3, 4, 5, 6, 7 },
            new[] { 1, 1, 0, 2, 0, 2, 1, 1, 1, 1 },
            new[] { 5, 5, 3, 3, 1, 4, 5, 5, 5, 1 });

        // Early split, merge at climax.
        public static readonly TalentLayout Fork = Build(
            "fork",
            new[] { 0, 1, 1, 2, 2, 3, 3, 4, 5, 5, 6, 7 },
            new[] { 1, 0, 2, 0, 2, 0, 2, 1, 0, 2, 1, 1 },
            new[] { 5, 3, 3, 3, 3, 1, 4, 5, 3, 3, 5, 1 });

        // Fat early row, shallow climax.
        public static readonly TalentLayout Wide = Build(
            "wide",
            new[] { 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 5 },
            new[] { 0, 1, 2, 0, 2, 0, 2, 0, 2, 1, 1 },
            new[] { 5, 5, 5, 3, 3, 3, 3, 1, 4, 5, 1 });

        // Narrow → wide mid → single Unique.
        public static readonly TalentLayout Diamond = Build(
            "diamond",
            new[] { 0, 1, 2, 2, 2, 3, 3, 4, 5, 5, 6, 7 },
            new[] { 1, 1, 0, 1, 2, 0, 2, 1, 0, 2, 1, 1 },
            new[] { 5, 5, 3, 1, 3, 3, 3, 5, 3, 3, 5, 1 });

        // Steady 2-wide spine.
        public static readonly TalentLayout Ladder = Build(
            "ladder",
            new[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 5, 6 },
            new[] { 0, 2, 0, 2, 0, 2, 0, 2, 1, 1, 1 },
            new[] { 5, 5, 3, 3, 3, 3, 1, 4, 5, 5, 1 });

        // Heavy left arm + light right.
        public static readonly TalentLayout Asymmetric = Build(
            "asymmetric",
            new[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 6, 7 },
            new[] { 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 1, 1, 1 },
            new[] { 5, 3, 5, 3, 4, 1, 3, 3, 4, 3, 5, 5, 1 });

        public static TalentLayout Get(string id)
        {
            if (string.Equals(id, Spear.Id, StringComparison.Ordinal)) return Spear;
            if (string.Equals(id, Fork.Id, StringComparison.Ordinal)) return Fork;
            if (string.Equals(id, Wide.Id, StringComparison.Ordinal)) return Wide;
            if (string.Equals(id, Diamond.Id, StringComparison.Ordinal)) return Diamond;
            if (string.Equals(id, Ladder.Id, StringComparison.Ordinal)) return Ladder;
            if (string.Equals(id, Asymmetric.Id, StringComparison.Ordinal)) return Asymmetric;
            return Spear;
        }

        /// <summary>
        /// Gate(row) = row * PointsPerTier. Captstone sits on the final row and may be a lone 1-rank node.
        /// Requires stay -1 here; Tree() adds the rare climax → prior Unique link.
        /// </summary>
        static TalentLayout Build(string id, int[] row, int[] col, int[] max)
        {
            int n = row.Length;
            var gate = new int[n];
            var requires = new int[n];
            for (int i = 0; i < n; i++) requires[i] = -1;

            int maxRow = 0;
            for (int i = 0; i < n; i++) maxRow = Mathf.Max(maxRow, row[i]);

            var rowCap = new int[maxRow + 1];
            for (int i = 0; i < n; i++)
            {
                gate[i] = row[i] * PointsPerTier;
                rowCap[row[i]] += max[i];
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Validate(id, row, max, gate, rowCap, maxRow);
#endif
            return new TalentLayout(id, row, col, max, gate, requires);
        }

        static void Validate(string id, int[] row, int[] max, int[] gate, int[] rowCap, int maxRow)
        {
            for (int r = 0; r < maxRow; r++)
            {
                if (rowCap[r] < PointsPerTier)
                {
                    Debug.LogError(
                        "TalentLayouts." + id + " row " + r
                        + " offers only " + rowCap[r] + " max ranks; need at least "
                        + PointsPerTier + " (captstone row exempt).");
                }
            }

            for (int i = 0; i < gate.Length; i++)
            {
                int need = gate[i];
                if (need <= 0) continue;
                int available = 0;
                for (int j = 0; j < gate.Length; j++)
                {
                    if (row[j] < row[i]) available += max[j];
                }

                if (available < need)
                {
                    Debug.LogError(
                        "TalentLayouts." + id + " softlock: node " + i
                        + " gate " + need + " but earlier max sum is " + available);
                }
            }
        }
    }
}
