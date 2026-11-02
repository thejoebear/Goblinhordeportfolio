// Portfolio excerpt from private repo thejoebear/Horde-Survival (branch player-characters).
// Shown for design/portfolio review only. Will not compile as a standalone Unity project.

using System;
using UnityEngine;
using GoblinHorde.Player;

namespace GoblinHorde.Managers
{
    /// <summary>
    /// Tracks enemy kill counts for player statistics and horde progression.
    /// </summary>
    public class KillCounter : MonoBehaviour
    {
        public static KillCounter Instance { get; private set; }

        public event Action<int> OnKillCountChanged;

        public int TotalKills { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void RegisterKill()
        {
            RegisterKills(1);
        }

        public void RegisterKills(int count)
        {
            if (count <= 0) return;
            TotalKills += count;
            OnKillCountChanged?.Invoke(TotalKills);

            // Trash/elite/champion score buckets are registered by the death
            // source (TrashHorde / EnemyBase) so specials are not double-counted.

            PlayerStats stats = PlayerController.Instance != null ? PlayerController.Instance.Stats : null;
            if (stats != null && stats.HealOnKill > 0f)
            {
                // Cap mass-clear heal spikes so supers don't hitch the sustain bucket.
                float heal = stats.HealOnKill * Mathf.Min(count, 24);
                stats.QueueHeal(heal);
            }
        }

        public void AddKill() => RegisterKill();

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
