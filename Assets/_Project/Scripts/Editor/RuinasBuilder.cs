using SlimeHunter.Interaction;
using SlimeHunter.Puzzles;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using static SlimeHunter.EditorTools.MapBuilder;
using static SlimeHunter.EditorTools.PlayerSetup;
using static SlimeHunter.EditorTools.TutorialBuilder;

namespace SlimeHunter.EditorTools
{
    /// <summary>
    /// Fase 5 do mapa: 6 Ruínas do Monólito (Notion, Parte 2).
    ///   • Sala em ruínas (15×15) com 3 slimes melee e o monólito.
    ///   • Placa de peso GIGANTE que só afunda no tamanho MÁXIMO (5): o jogador precisa derrotar e
    ///     absorver os slimes. Ela abre a laje na parede leste e revela a escadaria.
    ///   • Escadaria (rampa) desce 4 m até a câmara subterrânea em cruz, com a Chave 2 (Rubi) no fundo.
    /// Coordenadas em mundo (Layout.Ruinas = centro da sala, Layout.RuinasCamara = centro da cruz).
    /// </summary>
    static class RuinasBuilder
    {
        const float RoomHalf = Layout.RuinasSize / 2f;   // 7.5
        const float WallH = 5f;
        const float ChamberY = -4f;                     // piso da câmara
        const float ChamberTop = 1.5f;                  // teto da câmara (acima do nível do chão para caber o slime grande na escada)
        const float StairEndX = 70f;                    // rampa: x 62.5 (y 0) → 70 (y −4), ~28°
        const float PlateSize = 4f;

        static Material stone, darkStone, mossStone;
        static Vector3 R => Layout.Ruinas;
        static Vector3 K => Layout.RuinasCamara;

        public static void Build(Transform p)
        {
            stone = Mat("M_Ruina", new Color(0.62f, 0.58f, 0.5f));
            darkStone = Mat("M_RuinaEscura", new Color(0.4f, 0.37f, 0.33f));
            mossStone = Mat("M_RuinaMusgo", new Color(0.45f, 0.55f, 0.4f));
            var melee = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/MeleeSlime.prefab");
            var gooOrb = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/GooOrb.prefab");
            var key = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Key_Dourada.prefab");

            var room = Group(p, "Sala do Monolito");
            RoomWalls(room);
            Monolith(room);

            var enemies = Group(room, "Slimes da Ruina");
            foreach (var pos in new[] { new Vector3(51.5f, 0.5f, -10f), new Vector3(52f, 0.5f, 0f), new Vector3(55f, 0.5f, -10.5f) })
                Inst(melee, pos).transform.SetParent(enemies, true);
            // Gosma que reaparece: se a luta der errado, ainda dá para chegar ao tamanho 5 (sem travar o jogo).
            var spring = Inst(gooOrb, new Vector3(60.5f, 0.6f, 1f));
            spring.name = "GosmaQueReaparece";
            spring.transform.SetParent(room, true);
            SetFloat(spring.GetComponent<Pickup>(), "respawnSeconds", 10f);
            Hint(room, new Vector3(60.5f, 1.5f, 1f), new Vector3(3f, 3f, 3f), "Esta gosma volta a cada 10 s: use-a se a luta te deixar pequeno.");

            // Laje que esconde a escadaria (na abertura da parede leste) + placa gigante que abre a laje.
            var slab = Box("LajeDaEscadaria", room, new Vector3(R.x + RoomHalf + 0.5f, WallH / 2f, R.z), new Vector3(1f, WallH, 3f), darkStone);
            slab.isStatic = false;
            var door = slab.AddComponent<MechanismDoor>();
            // Afunda até abaixo da rampa (senão o topo da laje vira um degrau e o slime grande bate no teto).
            SetVector3(door, "openOffset", new Vector3(0f, -(WallH + 1.6f), 0f));
            var plate = GiantPlate(room, new Vector3(R.x + 2f, 0f, R.z), 5f);
            UnityEventTools.AddPersistentListener(plate.OnActivated, door.Open);
            var reveal = plate.gameObject.AddComponent<World.EventHint>();
            SetString(reveal, "text", "A placa afundou! Uma escadaria se abriu na parede leste.");
            UnityEventTools.AddPersistentListener(plate.OnActivated, reveal.Show);
            EditorUtility.SetDirty(plate);

            Hint(room, new Vector3(R.x - 5.5f, 1.5f, R.z), new Vector3(3f, 3f, 5f),
                "Placa gigante: só afunda no tamanho MÁXIMO (5). Derrote e absorva os slimes da ruína!");
            Checkpoint(room, new Vector3(R.x - 6f, 1f, R.z), new Vector3(2.5f, 2f, 4f));

            Chamber(Group(p, "Camara Subterranea"), key);
        }

        // ---------- Sala ----------

        static void RoomWalls(Transform p)
        {
            float x0 = R.x - RoomHalf, x1 = R.x + RoomHalf, z0 = R.z - RoomHalf, z1 = R.z + RoomHalf;
            // Norte e sul inteiras; oeste com a entrada (z −2.5 a +2.5 local) por onde chega o caminho;
            // leste com a abertura de 3 m da escadaria (fechada pela laje).
            RuinWall(p, new Vector3(x0, 0f, z1 + 0.5f), new Vector3(x1, 0f, z1 + 0.5f));
            RuinWall(p, new Vector3(x0, 0f, z0 - 0.5f), new Vector3(x1, 0f, z0 - 0.5f));
            RuinWall(p, new Vector3(x0 - 0.5f, 0f, z0 - 1f), new Vector3(x0 - 0.5f, 0f, R.z - 2.5f));
            RuinWall(p, new Vector3(x0 - 0.5f, 0f, R.z + 2.5f), new Vector3(x0 - 0.5f, 0f, z1 + 1f));
            RuinWall(p, new Vector3(x1 + 0.5f, 0f, z0 - 1f), new Vector3(x1 + 0.5f, 0f, R.z - 1.5f));
            RuinWall(p, new Vector3(x1 + 0.5f, 0f, R.z + 1.5f), new Vector3(x1 + 0.5f, 0f, z1 + 1f));
            // Moldura da abertura da escadaria.
            Box("Lintel", p, new Vector3(x1 + 0.5f, WallH + 0.4f, R.z), new Vector3(1.4f, 0.8f, 4.4f), darkStone);
        }

        /// <summary>Muro em ruínas entre a e b: 5 m de base (não dá para pular) com ameias quebradas por cima.</summary>
        static void RuinWall(Transform p, Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            var wall = Box("Muro", p, (a + b) / 2f + Vector3.up * (WallH / 2f), new Vector3(1f, WallH, len), stone);
            wall.transform.rotation = Quaternion.LookRotation(d.normalized);
            int n = Mathf.Max(1, Mathf.FloorToInt(len / 2.5f));
            for (int i = 0; i < n; i++)
            {
                if ((i * 7) % 3 == 0) continue; // falhas nas ameias
                float t = (i + 0.5f) / n;
                float h = 0.6f + ((i * 5) % 4) * 0.35f;
                var merlon = Box("Ameia", p, Vector3.Lerp(a, b, t) + Vector3.up * (WallH + h / 2f), new Vector3(1f, h, 1.4f), i % 2 == 0 ? stone : mossStone);
                merlon.transform.rotation = wall.transform.rotation;
            }
        }

        static void Monolith(Transform p)
        {
            Box("Monolito", p, new Vector3(R.x + 4.5f, 4.5f, R.z + 5f), new Vector3(1.8f, 9f, 1.2f), darkStone)
                .transform.rotation = Quaternion.Euler(0f, 20f, 3f);
            Box("ColunaQuebrada", p, new Vector3(R.x - 3f, 1.2f, R.z + 5f), new Vector3(1f, 2.4f, 1f), stone);
            Box("ColunaQuebrada", p, new Vector3(R.x - 3f, 0.9f, R.z - 5.5f), new Vector3(1f, 1.8f, 1f), mossStone);
            Box("ColunaCaida", p, new Vector3(R.x + 1f, 0.4f, R.z - 5.5f), new Vector3(3.5f, 0.8f, 0.8f), stone)
                .transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        }

        static WeightPlate GiantPlate(Transform p, Vector3 pos, float required)
        {
            var root = new GameObject("PlacaGigante");
            root.transform.SetParent(p, false);
            root.transform.position = pos;
            var zone = root.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 1f, 0f);
            zone.size = new Vector3(PlateSize, 2f, PlateSize);
            var visual = Box("Placa", root.transform, Vector3.zero, new Vector3(PlateSize, 0.2f, PlateSize), Mat("M_Plate", Color.white));
            visual.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            visual.isStatic = false;
            // Runas na borda da placa.
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f * Mathf.Deg2Rad;
                var rune = Box("Runa", root.transform, Vector3.zero, new Vector3(0.5f, 0.05f, 0.5f), Mat("M_RunaRuina", new Color(0.4f, 0.85f, 1f)));
                rune.transform.localPosition = new Vector3(Mathf.Cos(a) * (PlateSize / 2f + 0.6f), 0.03f, Mathf.Sin(a) * (PlateSize / 2f + 0.6f));
                Object.DestroyImmediate(rune.GetComponent<Collider>());
            }
            var plate = root.AddComponent<WeightPlate>();
            SetRef(plate, "plate", visual.transform);
            SetFloat(plate, "requiredSize", required);
            return plate;
        }

        // ---------- Escadaria + câmara em cruz ----------

        static void Chamber(Transform p, GameObject keyPrefab)
        {
            float sx0 = R.x + RoomHalf;              // 62.5: topo da escada (porta)
            // Rampa (colisão) + degraus só visuais (degraus de verdade travariam o slime pequeno).
            Path(p, "Escadaria (rampa)", new Vector3(sx0, 0f, K.z), new Vector3(StairEndX, ChamberY, K.z), 3f, darkStone);
            for (int i = 0; i < 12; i++)
            {
                float t = (i + 0.5f) / 12f;
                Vector3 s = Vector3.Lerp(new Vector3(sx0, 0f, K.z), new Vector3(StairEndX, ChamberY, K.z), t);
                var step = Box("Degrau", p, s + Vector3.up * 0.02f, new Vector3(0.6f, 0.15f, 3f), stone);
                Object.DestroyImmediate(step.GetComponent<Collider>());
            }
            // Paredes do poço da escada entre a sala e a câmara.
            Box("Poco_N", p, new Vector3((sx0 + 64.5f) / 2f, (ChamberY + ChamberTop) / 2f, K.z + 1.75f), new Vector3(64.5f - sx0, ChamberTop - ChamberY, 0.5f), darkStone);
            Box("Poco_S", p, new Vector3((sx0 + 64.5f) / 2f, (ChamberY + ChamberTop) / 2f, K.z - 1.75f), new Vector3(64.5f - sx0, ChamberTop - ChamberY, 0.5f), darkStone);

            // Paredes da cruz: braço horizontal x 64.5–79.5 (z ±2.5), braço vertical x 69.5–74.5 (z ±7.5).
            float hx0 = K.x - 7.5f, hx1 = K.x + 7.5f, vx0 = K.x - 2.5f, vx1 = K.x + 2.5f;
            float hz0 = K.z - 2.5f, hz1 = K.z + 2.5f, vz0 = K.z - 7.5f, vz1 = K.z + 7.5f;
            CWall(p, hx0, hz1, vx0, hz1); CWall(p, vx1, hz1, hx1, hz1);   // norte do braço horizontal
            CWall(p, hx0, hz0, vx0, hz0); CWall(p, vx1, hz0, hx1, hz0);   // sul do braço horizontal
            CWall(p, hx1, hz0, hx1, hz1);                                  // leste
            CWall(p, hx0, hz0, hx0, K.z - 1.5f); CWall(p, hx0, K.z + 1.5f, hx0, hz1); // oeste, com a passagem da escada
            CWall(p, vx0, hz1, vx0, vz1); CWall(p, vx1, hz1, vx1, vz1); CWall(p, vx0, vz1, vx1, vz1); // braço norte
            CWall(p, vx0, vz0, vx0, hz0); CWall(p, vx1, vz0, vx1, hz0); CWall(p, vx0, vz0, vx1, vz0); // braço sul
            // Teto (acima do nível do chão; por fora parece uma cripta saindo da terra).
            Box("Teto_H", p, new Vector3(K.x, ChamberTop + 0.25f, K.z), new Vector3(15.5f, 0.5f, 5.5f), mossStone);
            Box("Teto_V", p, new Vector3(K.x, ChamberTop + 0.25f, K.z), new Vector3(5.5f, 0.5f, 15.5f), mossStone);

            RoomLights(p, new Vector3(K.x, 0f, K.z), new Vector3(-4f, 0f, 0f), new Vector3(5f, 0f, 0f), new Vector3(0f, 0f, 5f));

            // Chave 2 no fundo do braço leste, num pedestal.
            Vector3 keyPos = new Vector3(hx1 - 1.5f, ChamberY, K.z);
            Box("Pedestal", p, keyPos + Vector3.up * 0.5f, new Vector3(1f, 1f, 1f), stone);
            var keyGo = Inst(keyPrefab, keyPos + Vector3.up * 1.6f);
            keyGo.name = "Key_chave_rubi";
            keyGo.transform.SetParent(p, true);
            var so = new SerializedObject(keyGo.GetComponent<Pickup>());
            so.FindProperty("itemId").stringValue = "chave_rubi";
            so.FindProperty("displayName").stringValue = "Chave Rubi";
            so.ApplyModifiedPropertiesWithoutUndo();
            var r = keyGo.GetComponent<Renderer>();
            r.sharedMaterial = Mat("M_chave_rubi", new Color(1f, 0.2f, 0.2f));
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);

            Banner(p, new Vector3(K.x, ChamberY, K.z), 15f / 1.7f, "Câmara Subterrânea", "Ruínas do Monólito", 4.5f);
            Checkpoint(p, new Vector3(StairEndX + 1.5f, ChamberY + 1f, K.z), new Vector3(2f, 2f, 3f));
        }

        static void CWall(Transform p, float xa, float za, float xb, float zb)
        {
            var a = new Vector3(xa, 0f, za);
            var b = new Vector3(xb, 0f, zb);
            Vector3 d = b - a;
            var w = Box("ParedeCamara", p, (a + b) / 2f + Vector3.up * ((ChamberY + ChamberTop) / 2f),
                new Vector3(0.5f, ChamberTop - ChamberY, d.magnitude + 0.5f), darkStone);
            w.transform.rotation = Quaternion.LookRotation(d.normalized);
        }
    }
}
