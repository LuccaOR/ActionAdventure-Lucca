using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>Itens de puzzle (chaves, engrenagens...) identificados por id.</summary>
    public class PlayerInventory : MonoBehaviour
    {
        readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        readonly Dictionary<string, string> displayNames = new Dictionary<string, string>();

        /// <summary>(id, nova quantidade)</summary>
        public event Action<string, int> Changed;

        public IReadOnlyDictionary<string, int> Items => counts;

        public string GetDisplayName(string id) =>
            displayNames.TryGetValue(id, out var name) && !string.IsNullOrEmpty(name) ? name : id;

        public int GetCount(string id) => counts.TryGetValue(id, out var c) ? c : 0;

        public bool Has(string id, int amount = 1) => GetCount(id) >= amount;

        public void Add(string id, int amount = 1, string displayName = null)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return;
            if (!string.IsNullOrEmpty(displayName)) displayNames[id] = displayName;
            counts[id] = GetCount(id) + amount;
            Changed?.Invoke(id, counts[id]);
        }

        public bool Remove(string id, int amount = 1)
        {
            if (!Has(id, amount)) return false;
            int left = counts[id] - amount;
            if (left <= 0) counts.Remove(id);
            else counts[id] = left;
            Changed?.Invoke(id, Mathf.Max(0, left));
            return true;
        }
    }
}
