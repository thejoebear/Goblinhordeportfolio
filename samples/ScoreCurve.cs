// Portfolio excerpt from private repo thejoebear/Horde-Survival (branch player-characters).
// Shown for design/portfolio review only. Will not compile as a standalone Unity project.
// Live product intent: curves compare real players on Steam leaderboards (not AI ghosts).

using System;
using UnityEngine;

namespace GoblinHorde.Leaderboard
{
    /// <summary>
    /// Records the local player's real score at fixed checkpoints during a run
    /// (every 20 s, 30 points across the 600 s run) so ghosts can replay their
    /// genuine score curve instead of an approximation.
    /// </summary>
    public static class RunScoreCurve
    {
        public const int CheckpointCount = 30;
        public const float CheckpointInterval = 20f;   // 30 × 20 s = 600 s

        private static readonly int[] _points = new int[CheckpointCount];
        private static int _filled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Reset();
        }

        public static void Reset()
        {
            Array.Clear(_points, 0, CheckpointCount);
            _filled = 0;
        }

        /// <summary>
        /// Call every frame with the current run time and live score.
        /// Checkpoint i stores the score at (i+1) × 20 s.
        /// </summary>
        public static void Tick(float runSeconds, int score)
        {
            int target = Mathf.Clamp(
                Mathf.FloorToInt(runSeconds / CheckpointInterval), 0, CheckpointCount);
            while (_filled < target)
            {
                _points[_filled++] = score;
            }
        }

        /// <summary>
        /// Returns a full 30-point copy; unreached checkpoints are filled with
        /// <paramref name="finalScore"/> (playback freezes at run end anyway).
        /// </summary>
        public static int[] Snapshot(int finalScore)
        {
            int[] result = new int[CheckpointCount];
            Array.Copy(_points, result, _filled);
            for (int i = _filled; i < CheckpointCount; i++)
            {
                result[i] = finalScore;
            }

            return result;
        }
    }

    /// <summary>
    /// Packs run metadata + the score curve into a Steam leaderboard details
    /// array (max 64 int32s — we use 33).
    ///
    /// Layout:
    ///   [0]      hero detail (index into roster) — same slot as the legacy format
    ///   [1]      low byte: format version; bit 8: victory flag
    ///   [2]      run duration in whole seconds (death or victory time)
    ///   [3..32]  score at 20 s, 40 s, … 600 s
    /// </summary>
    public static class LeaderboardDetailsCodec
    {
        public const int Version = 1;
        public const int Length = 3 + RunScoreCurve.CheckpointCount;

        private const int VictoryBit = 1 << 8;

        public static int[] Encode(int heroDetail, int runSeconds, bool victory, int[] curve)
        {
            int[] d = new int[Length];
            d[0] = heroDetail;
            d[1] = Version | (victory ? VictoryBit : 0);
            d[2] = Mathf.Max(0, runSeconds);
            for (int i = 0; i < RunScoreCurve.CheckpointCount; i++)
            {
                d[3 + i] = curve != null && i < curve.Length ? curve[i] : 0;
            }

            return d;
        }

        /// <summary>
        /// Fills curve fields on <paramref name="entry"/>. Legacy entries
        /// (1-int details, hero only) leave RunSeconds at 0 = unknown.
        /// </summary>
        public static void Decode(int[] details, LeaderboardEntry entry)
        {
            entry.HeroId = LeaderboardHeroCodec.FromDetail(
                details != null && details.Length > 0 ? details[0] : 0);
            entry.RunSeconds = 0;
            entry.Victory = false;
            entry.Curve = null;

            if (details == null || details.Length < 3) return;
            if ((details[1] & 0xFF) != Version) return;

            entry.Victory = (details[1] & VictoryBit) != 0;
            entry.RunSeconds = Mathf.Max(0, details[2]);

            int n = Mathf.Min(RunScoreCurve.CheckpointCount, details.Length - 3);
            if (n <= 0) return;
            entry.Curve = new int[n];
            Array.Copy(details, 3, entry.Curve, 0, n);
        }
    }

    /// <summary>
    /// Plays back a ghost's score over time. Uses the real recorded curve when
    /// the entry has one; falls back to the GhostCurve approximation otherwise.
    /// Scores freeze at the ghost's run end (death or victory).
    /// </summary>
    public static class GhostPlayback
    {
        private const float FullRun = 600f;

        public static int ScoreAt(LeaderboardEntry entry, float runSeconds)
        {
            if (entry == null || entry.Score <= 0 || runSeconds <= 0f) return 0;

            float end = entry.RunSeconds > 0 ? entry.RunSeconds : FullRun;
            float t = Mathf.Min(runSeconds, end);

            if (entry.Curve != null && entry.Curve.Length > 0)
            {
                return FromCurve(entry.Curve, entry.Score, end, t);
            }

            // No recorded curve. If the ghost died early, infer what a full run
            // would have scored, play the natural curve, and stop at their death.
            if (end < FullRun)
            {
                float fracAtDeath = GhostCurve.FractionAt(end);
                int potential = fracAtDeath > 0.01f
                    ? Mathf.RoundToInt(entry.Score / fracAtDeath)
                    : entry.Score;
                return Mathf.Min(entry.Score, GhostCurve.ScoreAt(t, potential));
            }

            return GhostCurve.ScoreAt(t, entry.Score);
        }

        /// <summary>
        /// True once the ghost's run has ended in a death (score frozen, row
        /// should grey out). Victories and legacy entries never read as dead.
        /// </summary>
        public static bool IsDead(LeaderboardEntry entry, float runSeconds)
        {
            if (entry == null || entry.Victory) return false;
            if (entry.RunSeconds <= 0 || entry.RunSeconds >= FullRun) return false;
            return runSeconds >= entry.RunSeconds;
        }

        private static int FromCurve(int[] curve, int finalScore, float end, float t)
        {
            float curveEnd = curve.Length * RunScoreCurve.CheckpointInterval;

            if (t >= curveEnd)
            {
                // Past the last checkpoint (boss fight overtime on victories):
                // ease from the last checkpoint to the final score.
                if (end <= curveEnd) return finalScore;
                float u = Mathf.Clamp01((t - curveEnd) / (end - curveEnd));
                return Mathf.RoundToInt(Mathf.Lerp(curve[curve.Length - 1], finalScore, u));
            }

            // Checkpoint i holds the score at (i+1) × interval.
            float pos = t / RunScoreCurve.CheckpointInterval;   // 1.0 == curve[0]
            int hi = Mathf.Min(Mathf.FloorToInt(pos), curve.Length - 1);
            float frac = pos - Mathf.Floor(pos);
            int a = hi <= 0 ? 0 : curve[hi - 1];
            int b = curve[hi];
            return Mathf.RoundToInt(Mathf.Lerp(a, b, frac));
        }
    }
}
