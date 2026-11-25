using SlimeHunter.Player;
using UnityEngine;

namespace SlimeHunter.Puzzles
{
    /// <summary>
    /// Acha o jogador dentro do volume do collider deste objeto, consultando a física a cada frame.
    /// Mais confiável que OnTriggerStay com CharacterController (que pode não disparar parado).
    /// Consulta só a camada Player: em áreas grandes (hub) o volume encosta em dezenas de colliders.
    /// </summary>
    static class PlayerZone
    {
        static readonly Collider[] Hits = new Collider[16];
        static int playerMask = -1;

        static int PlayerMask
        {
            get
            {
                if (playerMask == -1)
                {
                    int layer = LayerMask.NameToLayer("Player");
                    playerMask = layer >= 0 ? 1 << layer : ~0;
                }
                return playerMask;
            }
        }

        public static PlayerSize Find(Collider zone)
        {
            if (zone == null) return null;
            Bounds b = zone.bounds;
            int n = Physics.OverlapBoxNonAlloc(b.center, b.extents, Hits, Quaternion.identity, PlayerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var size = Hits[i].GetComponentInParent<PlayerSize>();
                if (size != null) return size;
            }
            return null;
        }
    }
}
