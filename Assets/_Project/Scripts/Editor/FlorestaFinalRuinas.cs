using System.Linq;
using SlimeHunter.Interaction;
using SlimeHunter.Puzzles;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;
using static SlimeHunter.EditorTools.FlorestaFinalHub;
using static SlimeHunter.EditorTools.PlayerSetup;
using static SlimeHunter.EditorTools.TutorialBuilder;

namespace SlimeHunter.EditorTools
{
    /// <summary>
    /// "floresta final" (ProBuilder) — 6 Ruínas do Monólito (Notion, Parte 2).
    /// No fim do corredor nordeste do hub (x 17, z 40). Entrada pelo muro sul.
    ///   • Sala em ruínas 20×20 com 3 slimes melee e o monólito.
    ///   • Placa de peso GIGANTE que só afunda no tamanho MÁXIMO (5): derrotar e absorver os slimes.
    ///     Ela abre a laje do muro leste e revela a escadaria.
    ///   • Escadaria desce 4 m até a câmara subterrânea em cruz; Chave 2 (Rubi) no fundo do braço leste.
    /// Menu: Slime Hunter > Floresta Final > Construir Ruínas.
    /// </summary>
    public static class FlorestaFinalRuinas
    {
        const string ScenePath = "Assets/_Project/Scenes/floresta final.unity";
        const string RootName = "Ruinas (ProBuilder)";
        public static readonly Vector3 R = new Vector3(17f, 0f, 51f);      // centro da sala: x 7..27, z 41..61
        const float RoomHalf = 10f;
        const float WallH = 5f;                                            // acima do pulo duplo
        const float EntranceHalf = 3f;                                     // boca do corredor (x 14..20)
        public static readonly Vector3 K = new Vector3(36.5f, -4f, 51f);   // centro da cruz (piso)
        const float ChamberTop = 1.5f;                                     // teto acima do chão: cabe o slime 5 na escada
        const float StairEndX = 34.5f;                                     // escada: x 27 (y 0) → 34.5 (y −4), ~28°
        const float PlateSize = 4f;

        static Material stone, dark, moss, rune, floorM;

        [MenuItem("Slime Hunter/Floresta Final/Construir Ruínas")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError("[Slime Hunter] Abra a cena 'floresta final' antes de construir as Ruínas.");
                return;
            }
            foreach (var go in scene.GetRootGameObjects().Where(g => g.name == RootName)) Object.DestroyImmediate(go);

            stone = Mat("M_FF_Ruina", new Color(0.66f, 0.61f, 0.52f));
            dark = Mat("M_FF_RuinaEscura", new Color(0.4f, 0.37f, 0.34f));
            moss = Mat("M_FF_RuinaMusgo", new Color(0.45f, 0.56f, 0.38f));
            rune = Mat("M_FF_Runa", new Color(0.4f, 0.85f, 1f));
            floorM = Mat("M_FF_PisoRuina", new Color(0.55f, 0.52f, 0.46f));
            var melee = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/MeleeSlime.prefab");
            var gooOrb = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/GooOrb.prefab");
            var key = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Key_Dourada.prefab");

            var root = new GameObject(RootName).transform;
            var room = Group(root, "Sala do Monolito");
            // Termina em x 27 (topo da escada): passar disso viraria um degrau que o slime pequeno não sobe.
            Cube(room, "Piso_Sala", new Vector3(R.x - 0.5f, -0.25f, R.z), new Vector3(RoomHalf * 2f + 1f, 0.5f, RoomHalf * 2f + 2f), floorM);
            FloorTiles(room);
            RoomWalls(room);
            Monolith(room);

            var enemies = Group(room, "Slimes da Ruina");
            foreach (var l in new[] { new Vector3(-4f, 0.5f, -4f), new Vector3(-4f, 0.5f, 5f), new Vector3(2f, 0.5f, -6f) })
                Inst(melee, R + l).transform.SetParent(enemies, true);

            // Gosma que reaparece: se a luta der errado, ainda dá para chegar ao tamanho 5 (sem travar o jogo).
            var spring = Inst(gooOrb, R + new Vector3(7.5f, 0.6f, -7.5f));
            spring.name = "GosmaQueReaparece";
            spring.transform.SetParent(room, true);
            SetFloat(spring.GetComponent<Pickup>(), "respawnSeconds", 10f);
            Hint(room, R + new Vector3(7.5f, 1.5f, -7.5f), new Vector3(3f, 3f, 3f), "Esta gosma volta a cada 10 s: use-a se a luta te deixar pequeno.");

            // Laje que esconde a escadaria (abertura de 3 m no muro leste) + placa gigante que abre a laje.
            var slab = Cube(room, "LajeDaEscadaria", new Vector3(R.x + RoomHalf + 0.5f, WallH / 2f, R.z), new Vector3(1f, WallH, 3f), dark);
            slab.gameObject.isStatic = false;
            var door = slab.gameObject.AddComponent<MechanismDoor>();
            // Afunda até abaixo da escada (senão o topo da laje vira degrau e o slime grande bate no teto).
            SetVector3(door, "openOffset", new Vector3(0f, -(WallH + 1.6f), 0f));
            var plate = GiantPlate(room, R + new Vector3(2f, 0f, 0f), 5f);
            UnityEventTools.AddPersistentListener(plate.OnActivated, door.Open);
            var reveal = plate.gameObject.AddComponent<World.EventHint>();
            SetString(reveal, "text", "A placa afundou! Uma escadaria se abriu no muro leste.");
            UnityEventTools.AddPersistentListener(plate.OnActivated, reveal.Show);
            EditorUtility.SetDirty(plate);

            Banner(room, R, RoomHalf * 2f / 1.7f, "Ruínas do Monólito", "Chave 2 · Rubi", 6f);
            Hint(room, R + new Vector3(0f, 1.5f, -8.5f), new Vector3(6f, 3f, 3f),
                "Placa gigante: só afunda no tamanho MÁXIMO (5). Derrote e absorva os slimes da ruína!");
            Checkpoint(room, R + new Vector3(0f, 1f, -9f), new Vector3(5f, 2f, 2f));

            Chamber(Group(root, "Camara Subterranea"), key);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Slime Hunter] Ruínas (ProBuilder) construídas em " + ScenePath);
        }

        // ---------- Sala ----------

        static void FloorTiles(Transform p)
        {
            // Lajotas soltas e rachadas (só visuais, 2 cm acima do piso).
            for (int i = 0; i < 14; i++)
            {
                float x = ((i * 37) % 17) - 8.5f, z = ((i * 53) % 17) - 8.5f;
                var t = Cube(p, "Lajota", R + new Vector3(x, 0.02f, z), new Vector3(1.6f, 0.04f, 1.6f), i % 3 == 0 ? moss : stone, false);
                t.transform.rotation = Quaternion.Euler(0f, i * 23f, 0f);
            }
        }

        static void RoomWalls(Transform p)
        {
            float x0 = R.x - RoomHalf, x1 = R.x + RoomHalf, z0 = R.z - RoomHalf, z1 = R.z + RoomHalf;
            // Norte e oeste inteiros; sul com a entrada do corredor; leste com a abertura de 3 m da escadaria (laje).
            RuinWall(p, new Vector3(x0 - 1f, 0f, z1 + 0.5f), new Vector3(x1 + 1f, 0f, z1 + 0.5f));
            RuinWall(p, new Vector3(x0 - 0.5f, 0f, z0 - 1f), new Vector3(x0 - 0.5f, 0f, z1 + 1f));
            RuinWall(p, new Vector3(x0 - 1f, 0f, z0 - 0.5f), new Vector3(R.x - EntranceHalf, 0f, z0 - 0.5f));
            RuinWall(p, new Vector3(R.x + EntranceHalf, 0f, z0 - 0.5f), new Vector3(x1 + 1f, 0f, z0 - 0.5f));
            RuinWall(p, new Vector3(x1 + 0.5f, 0f, z0 - 1f), new Vector3(x1 + 0.5f, 0f, R.z - 1.5f));
            RuinWall(p, new Vector3(x1 + 0.5f, 0f, R.z + 1.5f), new Vector3(x1 + 0.5f, 0f, z1 + 1f));

            // Arco quebrado na entrada sul e moldura da escadaria no leste.
            var arch = ShapeGenerator.GenerateArch(PivotLocation.Center, 180f, EntranceHalf + 0.6f, 0.9f, 1.2f, 10, true, true, true, true, true);
            Setup(arch, p, "Arco da Entrada (ProBuilder)", new Vector3(R.x, WallH - 0.2f, z0 - 0.5f), Quaternion.identity, stone, false);
            foreach (float s in new[] { -1f, 1f })
                Column(p, new Vector3(R.x + s * (EntranceHalf + 0.6f), 0f, z0 - 0.5f), WallH - 0.2f, 0.5f, stone, true);
            Cube(p, "Lintel", new Vector3(x1 + 0.5f, WallH + 0.4f, R.z), new Vector3(1.4f, 0.8f, 4.4f), dark);
            for (int i = 0; i < 3; i++)
                Cube(p, "Runa da Porta", new Vector3(x1 - 0.02f, 1.2f + i * 1.2f, R.z - 2.1f), new Vector3(0.04f, 0.5f, 0.5f), rune, false);
        }

        /// <summary>Muro em ruínas entre a e b: 5 m de base (não dá para pular) com ameias quebradas por cima.</summary>
        static void RuinWall(Transform p, Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            Quaternion rot = Quaternion.LookRotation(d.normalized);
            var wall = Cube(p, "Muro", (a + b) / 2f + Vector3.up * (WallH / 2f), new Vector3(1f, WallH, len), stone);
            wall.transform.rotation = rot;
            int n = Mathf.Max(1, Mathf.FloorToInt(len / 2.5f));
            for (int i = 0; i < n; i++)
            {
                if ((i * 7) % 3 == 0) continue; // falhas nas ameias
                float t = (i + 0.5f) / n;
                float h = 0.6f + ((i * 5) % 4) * 0.35f;
                var merlon = Cube(p, "Ameia", Vector3.Lerp(a, b, t) + Vector3.up * (WallH + h / 2f), new Vector3(1f, h, 1.4f), i % 2 == 0 ? stone : moss);
                merlon.transform.rotation = rot;
            }
            // Pedras caídas na base, do lado de dentro (só visuais).
            for (int i = 1; i < n; i += 3)
            {
                var rubble = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 0.45f, 0);
                Vector3 inward = Vector3.Cross(Vector3.up, d.normalized);
                if (Vector3.Dot(inward, R - a) < 0f) inward = -inward;
                Setup(rubble, p, "Entulho (ProBuilder)", Vector3.Lerp(a, b, (i + 0.5f) / n) + inward * 1.1f + Vector3.up * 0.25f, Quaternion.Euler(0f, i * 40f, 0f), stone, false);
            }
        }

        static void Monolith(Transform p)
        {
            var m = Cube(p, "Monolito", R + new Vector3(6.5f, 5f, 6.5f), new Vector3(2f, 10f, 1.3f), dark);
            m.transform.rotation = Quaternion.Euler(0f, 20f, 3f);
            for (int i = 0; i < 4; i++)
                Cube(p, "Runa do Monolito", R + new Vector3(6.5f, 2f + i * 2f, 6.5f) + m.transform.rotation * new Vector3(0f, 0f, -0.67f),
                     new Vector3(0.6f, 0.6f, 0.04f), rune, false).transform.rotation = m.transform.rotation;
            var glow = new GameObject("LuzDoMonolito").AddComponent<Light>();
            glow.transform.SetParent(p, false);
            glow.transform.position = R + new Vector3(5.5f, 3f, 5f);
            glow.type = LightType.Point;
            glow.range = 10f;
            glow.intensity = 2f;
            glow.color = new Color(0.45f, 0.85f, 1f);

            Column(p, R + new Vector3(-6f, 0f, 6.5f), 2.6f, 0.55f, stone, true);
            Column(p, R + new Vector3(-6.5f, 0f, -7f), 1.8f, 0.55f, moss, true);
            Column(p, R + new Vector3(7.5f, 0f, 3.5f), 0.9f, 0.55f, stone, true);
            var fallen = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 10, 0.5f, 4f, 0);
            Setup(fallen, p, "Coluna Caida (ProBuilder)", R + new Vector3(-1f, 0.5f, 7.5f), Quaternion.Euler(0f, 35f, 90f), stone, true);
        }

        static void Column(Transform p, Vector3 basePos, float h, float r, Material mat, bool collider)
        {
            var c = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 10, r, h, 0);
            Setup(c, p, "Coluna (ProBuilder)", basePos + Vector3.up * (h / 2f), Quaternion.identity, mat, collider);
            Cube(p, "Base da Coluna", basePos + Vector3.up * 0.15f, new Vector3(r * 2.6f, 0.3f, r * 2.6f), dark, false);
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
            var visual = Cube(root.transform, "Placa", pos + new Vector3(0f, 0.1f, 0f), new Vector3(PlateSize, 0.2f, PlateSize), Mat("M_Plate", Color.white));
            visual.gameObject.isStatic = false;
            Cube(root.transform, "Moldura", pos + new Vector3(0f, 0.04f, 0f), new Vector3(PlateSize + 0.8f, 0.08f, PlateSize + 0.8f), dark, false);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f * Mathf.Deg2Rad;
                Cube(root.transform, "Runa", pos + new Vector3(Mathf.Cos(a) * (PlateSize / 2f + 0.9f), 0.03f, Mathf.Sin(a) * (PlateSize / 2f + 0.9f)), new Vector3(0.5f, 0.05f, 0.5f), rune, false);
            }
            var plate = root.AddComponent<WeightPlate>();
            SetRef(plate, "plate", visual.transform);
            SetFloat(plate, "requiredSize", required);
            return plate;
        }

        // ---------- Escadaria + câmara em cruz ----------

        static void Chamber(Transform p, GameObject keyPrefab)
        {
            float sx0 = R.x + RoomHalf;   // 27: topo da escada (porta)
            Stairs(p, new Vector3(StairEndX, K.y, K.z), new Vector3(sx0, 0f, K.z), 3f, dark);
            // Poço da escada entre a sala e a câmara.
            float wellEnd = K.x - 7.5f;   // 29
            foreach (float s in new[] { -1f, 1f })
                Cube(p, "Poco", new Vector3((sx0 + wellEnd) / 2f + 0.5f, (K.y + ChamberTop) / 2f, K.z + s * 1.75f), new Vector3(wellEnd - sx0 + 1f, ChamberTop - K.y, 0.5f), dark);

            // Piso da cruz: braço horizontal x 29–44 (z ±2.5), vertical x 34–39 (z ±7.5).
            Cube(p, "Piso_H", K + new Vector3(0f, -0.25f, 0f), new Vector3(15.5f, 0.5f, 5.5f), floorM);
            Cube(p, "Piso_V", K + new Vector3(0f, -0.25f, 0f), new Vector3(5.5f, 0.5f, 15.5f), floorM);

            float hx0 = K.x - 7.5f, hx1 = K.x + 7.5f, vx0 = K.x - 2.5f, vx1 = K.x + 2.5f;
            float hz0 = K.z - 2.5f, hz1 = K.z + 2.5f, vz0 = K.z - 7.5f, vz1 = K.z + 7.5f;
            CWall(p, hx0, hz1, vx0, hz1); CWall(p, vx1, hz1, hx1, hz1);
            CWall(p, hx0, hz0, vx0, hz0); CWall(p, vx1, hz0, hx1, hz0);
            CWall(p, hx1, hz0, hx1, hz1);
            CWall(p, hx0, hz0, hx0, K.z - 1.5f); CWall(p, hx0, K.z + 1.5f, hx0, hz1);
            CWall(p, vx0, hz1, vx0, vz1); CWall(p, vx1, hz1, vx1, vz1); CWall(p, vx0, vz1, vx1, vz1);
            CWall(p, vx0, vz0, vx0, hz0); CWall(p, vx1, vz0, vx1, hz0); CWall(p, vx0, vz0, vx1, vz0);
            // Teto (acima do nível do chão; por fora parece uma cripta saindo da terra).
            Cube(p, "Teto_H", new Vector3(K.x, ChamberTop + 0.25f, K.z), new Vector3(15.5f, 0.5f, 5.5f), moss);
            Cube(p, "Teto_V", new Vector3(K.x, ChamberTop + 0.25f, K.z), new Vector3(5.5f, 0.5f, 15.5f), moss);

            // Tochas azuladas nas pontas da cruz.
            foreach (var o in new[] { new Vector3(-4f, 3.5f, 0f), new Vector3(5f, 3.5f, 0f), new Vector3(0f, 3.5f, 5f), new Vector3(0f, 3.5f, -5f) })
            {
                var l = new GameObject("Tocha").AddComponent<Light>();
                l.transform.SetParent(p, false);
                l.transform.position = K + o;
                l.type = LightType.Point;
                l.range = 9f;
                l.intensity = 2.2f;
                l.color = new Color(0.55f, 0.8f, 1f);
            }
            // Sarcófagos nos braços norte e sul (ambiente).
            Cube(p, "Sarcofago", K + new Vector3(0f, 0.45f, 5.5f), new Vector3(1.4f, 0.9f, 2.4f), stone);
            Cube(p, "Sarcofago", K + new Vector3(0f, 0.45f, -5.5f), new Vector3(1.4f, 0.9f, 2.4f), stone);

            // Chave 2 no fundo do braço leste, num pedestal.
            Vector3 keyPos = new Vector3(hx1 - 1.5f, K.y, K.z);
            Cube(p, "Pedestal", keyPos + Vector3.up * 0.5f, new Vector3(1f, 1f, 1f), stone);
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

            Banner(p, K, 15f / 1.7f, "Câmara Subterrânea", "Ruínas do Monólito", 4.5f);
            Checkpoint(p, new Vector3(StairEndX + 1.5f, K.y + 1f, K.z), new Vector3(2f, 2f, 3f));
        }

        static void CWall(Transform p, float xa, float za, float xb, float zb)
        {
            var a = new Vector3(xa, 0f, za);
            var b = new Vector3(xb, 0f, zb);
            Vector3 d = b - a;
            var w = Cube(p, "ParedeCamara", (a + b) / 2f + Vector3.up * ((K.y + ChamberTop) / 2f),
                new Vector3(0.5f, ChamberTop - K.y, d.magnitude + 0.5f), dark);
            w.transform.rotation = Quaternion.LookRotation(d.normalized);
        }
    }
}
