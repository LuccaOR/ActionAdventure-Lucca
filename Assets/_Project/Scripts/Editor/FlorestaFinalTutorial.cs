using System.Linq;
using SlimeHunter.Core;
using SlimeHunter.Interaction;
using SlimeHunter.Puzzles;
using SlimeHunter.World;
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
    /// "floresta final" (ProBuilder) — Tutorial na caverna (Notion, Parte 1). Fica fora do mapa (x 200) e
    /// termina num portal azul que leva à chegada do hub. O jogador começa aqui.
    ///   1 Clareira do Despertar: saltinhos, pular troncos, raiz + pulo duplo, riacho de espinhos (pulo duplo + dash).
    ///   2 Câmara de Puzzles: cristal abre a grade, fenda estreita (encolher), gosma + placa de peso abrem a saída.
    ///   3 Arena de Combate: 2 slimes melee, plataformas para o Impacto; limpar a arena liga o portal azul.
    /// Mesmas medidas do TutorialBuilder antigo (distâncias de pulo medidas em teste). +Z = norte (entrada).
    /// Menu: Slime Hunter > Floresta Final > Construir Tutorial.
    /// </summary>
    public static class FlorestaFinalTutorial
    {
        const string ScenePath = "Assets/_Project/Scenes/floresta final.unity";
        const string RootName = "Tutorial (ProBuilder)";
        public const string SpawnName = "SpawnDoTutorial";

        public static readonly Vector3 C1 = new Vector3(200f, 0f, 236f), C2 = new Vector3(200f, 0f, 200f), C3 = new Vector3(200f, 0f, 166f);
        const float R1 = 16f, R2 = 12.5f, R3 = 15f;
        const float RoomH = 9f, DoorW = 4f, DoorH = 4.2f, CorridorH = 4.6f;
        // Sala 1 (z local): terraço 1 de z 4 a 0; riacho de 9.5 m (pulo+duplo ≈ 6.5, pulo+dash ≈ 8.5, pulo+duplo+dash ≈ 10.8).
        const float TerraceH = 3.6f, Terrace1Front = 4f, Terrace1Back = 0f, PitEnd = -9.5f, Terrace2End = -14.5f;

        static Material floorM, rockM, woodM, spikeM, barM, crystalM, mossM, glowM, portalBlue, pitM;

        [MenuItem("Slime Hunter/Floresta Final/Construir Tutorial")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError("[Slime Hunter] Abra a cena 'floresta final' antes de construir o Tutorial.");
                return;
            }
            foreach (var go in scene.GetRootGameObjects().Where(g => g.name == RootName)) Object.DestroyImmediate(go);

            floorM = Mat("M_FF_ChaoCaverna", new Color(0.36f, 0.33f, 0.3f));
            rockM = Mat("M_FF_RochaCaverna", new Color(0.3f, 0.28f, 0.27f));
            woodM = Mat("M_FF_Madeira", new Color(0.42f, 0.28f, 0.18f));
            spikeM = Mat("M_Espinho", new Color(0.62f, 0.6f, 0.66f));
            barM = Mat("M_GrateBar", new Color(0.35f, 0.35f, 0.4f));
            crystalM = Mat("M_Crystal", Color.white);
            mossM = Mat("M_FF_Musgo", new Color(0.36f, 0.55f, 0.25f));
            glowM = Mat("M_FF_CristalCaverna", new Color(0.45f, 0.85f, 1f));
            portalBlue = Mat("M_FF_PortalAzul", new Color(0.3f, 0.5f, 1f));
            pitM = Mat("M_FF_FundoRiacho", new Color(0.18f, 0.16f, 0.16f));

            var root = new GameObject(RootName).transform;
            Clareira(Group(root, "1 Clareira do Despertar"));
            Camara(Group(root, "2 Camara de Puzzles"));
            Arena(Group(root, "3 Arena de Combate"));

            // Corredor 1→2 desce do terraço 2 (3.6 m) até o chão da câmara; o 2→3 é plano.
            Tunnel(root, "Corredor 1-2", new Vector3(C1.x, TerraceH, C1.z - Apothem(R1) - 1f), C2 + new Vector3(0f, 0f, Apothem(R2) - 2f));
            Tunnel(root, "Corredor 2-3", C2 - new Vector3(0f, 0f, Apothem(R2) - 2f), C3 + new Vector3(0f, 0f, Apothem(R3) - 2f));

            Banner(root, C1, R1, "Clareira do Despertar", "Tutorial · 1/3");
            Banner(root, C2, R2, "Câmara de Puzzles", "Tutorial · 2/3");
            Banner(root, C3, R3, "Arena de Combate", "Tutorial · 3/3");

            var spawn = Marker(root, SpawnName, C1 + new Vector3(0f, 0.1f, 11f), Vector3.back);
            var player = Object.FindAnyObjectByType<Player.PlayerController>();
            if (player != null) player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Slime Hunter] Tutorial (ProBuilder) construído em " + ScenePath);
        }

        static float Apothem(float r) => r * 0.8660254f;

        // ---------- 1 Clareira do Despertar ----------

        static void Clareira(Transform p)
        {
            Vector3 c = C1;
            CaveRoom(p, c, R1, new Door(SouthEdge, TerraceH));
            Lights(p, c, new Color(1f, 0.83f, 0.6f), new Vector3(0f, 7f, 7f), new Vector3(0f, 8f, -11f));
            // Raio de luz vindo de uma fenda no teto: o slime "desperta" ali.
            var beam = new GameObject("Feixe de Luz").AddComponent<Light>();
            beam.transform.SetParent(p, false);
            beam.transform.position = c + new Vector3(0f, RoomH - 0.3f, 11f);
            beam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            beam.type = LightType.Spot; beam.spotAngle = 35f; beam.range = 14f; beam.intensity = 8f;
            beam.color = new Color(1f, 0.95f, 0.75f);
            Hex(p, "Fenda no Teto (ProBuilder)", c + new Vector3(0f, RoomH + 0.02f, 11f), 1.4f, 0.05f, Mat("M_FF_Feixe", new Color(1f, 0.95f, 0.75f)), false);

            // Troncos caídos: aprender a pular (saltinho não passa, pulo passa).
            LogAt(p, c + new Vector3(4f, 0.5f, 8.5f), 30f);
            LogAt(p, c + new Vector3(-9f, 0.5f, 10.5f), -25f);

            // Raiz (1.2 m) e terraço (3.6 m): da raiz até o terraço só com pulo duplo (3.25 m de distância).
            Cube(p, "Raiz", c + new Vector3(-3f, 0.6f, 8.5f), new Vector3(2.5f, 1.2f, 2.5f), woodM);
            for (int i = 0; i < 3; i++)
            {
                var r = Cube(p, "Raiz_Galho", c + new Vector3(-3f + (i - 1) * 1.1f, 0.4f, 8.5f + (i % 2 == 0 ? 1.4f : -1.4f)), new Vector3(0.5f, 0.5f, 1.8f), woodM, false);
                r.transform.rotation = Quaternion.Euler(15f, i * 35f, 0f);
            }
            Cube(p, "Terraco1", c + new Vector3(0f, TerraceH / 2f, (Terrace1Front + Terrace1Back) / 2f), new Vector3(34f, TerraceH, Terrace1Front - Terrace1Back), rockM);
            Cube(p, "Terraco2", c + new Vector3(0f, TerraceH / 2f, (PitEnd + Terrace2End) / 2f), new Vector3(34f, TerraceH, PitEnd - Terrace2End), rockM);
            Cube(p, "Musgo1", c + new Vector3(0f, TerraceH + 0.01f, (Terrace1Front + Terrace1Back) / 2f), new Vector3(20f, 0.02f, 3f), mossM, false);
            Cube(p, "Musgo2", c + new Vector3(0f, TerraceH + 0.01f, (PitEnd + Terrace2End) / 2f - 0.5f), new Vector3(20f, 0.02f, 3f), mossM, false);

            // Riacho de espinhos entre os terraços (cones do ProBuilder, só visuais; o perigo é o volume).
            Cube(p, "Fundo do Riacho", c + new Vector3(0f, 0.02f, (Terrace1Back + PitEnd) / 2f), new Vector3(30f, 0.04f, Terrace1Back - PitEnd), pitM, false);
            for (float x = -12f; x <= 12f; x += 1.5f)
                for (float z = Terrace1Back - 1f; z > PitEnd; z -= 1.5f)
                {
                    var s = ShapeGenerator.GenerateCone(PivotLocation.Center, 0.28f, 0.8f, 6);
                    Setup(s, p, "Espinho (ProBuilder)", c + new Vector3(x + (z % 3f == 0f ? 0.3f : 0f), 0.4f, z), Quaternion.Euler(0f, x * 20f, 0f), spikeM, false);
                }
            var reset = Marker(p, "VoltaDoRiacho", c + new Vector3(0f, TerraceH + 0.1f, Terrace1Front - 1.5f), Vector3.back);
            var hazardGo = Trigger(p, "Riacho (perigo)", c + new Vector3(0f, 1.2f, (Terrace1Back + PitEnd) / 2f), new Vector3(34f, 2.4f, Terrace1Back - PitEnd));
            var hazard = hazardGo.AddComponent<HazardZone>();
            SetRef(hazard, "resetPoint", reset);
            SetString(hazard, "message", "Espinhos! Pule, pule de novo no ar e use o Dash (Shift).");

            Hint(p, c + new Vector3(0f, 1.5f, 11.5f), new Vector3(22f, 3f, 4f), "Você anda aos saltinhos (WASD). Espaço pula: pule os troncos!");
            Hint(p, c + new Vector3(0f, 1.5f, 7f), new Vector3(24f, 3f, 6f), "Suba na raiz e pule DUAS vezes (Espaço de novo no ar) para alcançar o terraço.");
            Hint(p, c + new Vector3(0f, TerraceH + 1.5f, (Terrace1Front + Terrace1Back) / 2f), new Vector3(28f, 3f, 4f), "Riacho de espinhos! Corra, pule, pule de novo e dê Dash (Shift) no ar.");
            Checkpoint(p, c + new Vector3(0f, TerraceH + 1f, (PitEnd + Terrace2End) / 2f), new Vector3(10f, 2f, 3f));

            CaveDecor(p, c, R1, 1);
        }

        // ---------- 2 Câmara de Puzzles ----------

        static void Camara(Transform p)
        {
            Vector3 c = C2;
            CaveRoom(p, c, R2, new Door(NorthEdge), new Door(SouthEdge));
            Lights(p, c, new Color(0.7f, 0.85f, 1f), new Vector3(0f, 7f, 7f), new Vector3(0f, 7f, -6f));

            // Parede da grade (z +4) com a grade no meio, aberta por 1 cristal.
            const float gz = 4f, gateW = 3f, gateH = 3.6f;
            Cube(p, "ParedeGrade_E", c + new Vector3(-(gateW / 2f + 6.25f), RoomH / 2f, gz), new Vector3(12.5f, RoomH, 1f), rockM);
            Cube(p, "ParedeGrade_D", c + new Vector3(gateW / 2f + 6.25f, RoomH / 2f, gz), new Vector3(12.5f, RoomH, 1f), rockM);
            Cube(p, "ParedeGrade_Topo", c + new Vector3(0f, (gateH + RoomH) / 2f, gz), new Vector3(gateW, RoomH - gateH, 1f), rockM);
            var grate = Grate(p, c + new Vector3(0f, 0f, gz), gateW, gateH);

            var crystal = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 0.4f, 0);
            Setup(crystal, p, "Cristal", c + new Vector3(3.5f, 5f, gz + 0.8f), Quaternion.Euler(20f, 30f, 0f), crystalM, false);
            crystal.gameObject.isStatic = false;
            crystal.transform.localScale = new Vector3(0.9f, 1.4f, 0.9f);
            crystal.gameObject.AddComponent<BoxCollider>().size = new Vector3(0.9f, 0.9f, 0.9f);
            Cube(p, "Suporte do Cristal", c + new Vector3(3.5f, 4.3f, gz + 0.75f), new Vector3(0.9f, 0.25f, 0.5f), rockM, false);
            var sc = crystal.gameObject.AddComponent<ShootableCrystal>();
            SetColor(sc, "color", new Color(0.3f, 0.9f, 1f));
            var puzzleGo = new GameObject("CristalDaGrade");
            puzzleGo.transform.SetParent(p, false);
            var puzzle = puzzleGo.AddComponent<CrystalSequencePuzzle>();
            var so = new SerializedObject(puzzle);
            var arr = so.FindProperty("order");
            arr.arraySize = 1;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = sc;
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(puzzle.OnSolved, grate.Open);
            EditorUtility.SetDirty(puzzle);
            Hint(p, c + new Vector3(0f, 1.5f, 7.5f), new Vector3(16f, 3f, 6f), "Atire no cristal (clique). Cada tiro usa um pedaço do seu corpo: você encolhe!");

            // Parede da fenda (z −2 a −4) com um túnel de 1.4 × 1.0 m: só passa encolhido.
            const float fz = -3f, fendaW = 1.4f, fendaH = 1.0f;
            Cube(p, "ParedeFenda_E", c + new Vector3(-(fendaW / 2f + 7f), RoomH / 2f, fz), new Vector3(14f, RoomH, 2f), rockM);
            Cube(p, "ParedeFenda_D", c + new Vector3(fendaW / 2f + 7f, RoomH / 2f, fz), new Vector3(14f, RoomH, 2f), rockM);
            Cube(p, "ParedeFenda_Topo", c + new Vector3(0f, (fendaH + RoomH) / 2f, fz), new Vector3(fendaW, RoomH - fendaH, 2f), rockM);
            var arch = ShapeGenerator.GenerateArch(PivotLocation.Center, 180f, 1.25f, 0.3f, 0.4f, 8, true, true, true, true, true);
            Setup(arch, p, "Moldura da Fenda (ProBuilder)", c + new Vector3(0f, fendaH + 0.1f, fz + 1.05f), Quaternion.identity, glowM, false);
            var sizeHint = Trigger(p, "DicaDaFenda", c + new Vector3(0f, 1f, -0.5f), new Vector3(10f, 2f, 3f));
            SetFloat(sizeHint.AddComponent<SizeHintZone>(), "size", 1.4f);

            // Depois da fenda: gosma para crescer e a placa (tamanho 3) que abre a saída.
            var goo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/GooOrb.prefab");
            Inst(goo, c + new Vector3(-4f, 0.6f, -6f)).transform.SetParent(p, true);
            Inst(goo, c + new Vector3(4f, 0.6f, -6f)).transform.SetParent(p, true);
            var exit = ExitDoor(p, c, R2, SouthEdge, "SaidaDaCamara");
            var plate = Plate(p, c + new Vector3(0f, 0f, -8.3f), 3f);
            UnityEventTools.AddPersistentListener(plate.OnActivated, exit.Open);
            EditorUtility.SetDirty(plate);
            Hint(p, c + new Vector3(0f, 1.5f, -6f), new Vector3(16f, 3f, 3f), "Pegue a gosma para crescer de novo e pise na placa.");
            Checkpoint(p, c + new Vector3(0f, 1f, -6f), new Vector3(8f, 2f, 2f));

            CaveDecor(p, c, R2, 2);
        }

        // ---------- 3 Arena de Combate ----------

        static void Arena(Transform p)
        {
            Vector3 c = C3;
            CaveRoom(p, c, R3, new Door(NorthEdge));
            Lights(p, c, new Color(1f, 0.7f, 0.5f), new Vector3(-6f, 7.5f, 0f), new Vector3(6f, 7.5f, -4f));

            // Plataformas de 3 m: pulo duplo para subir, Impacto para descer em cima dos slimes.
            foreach (float s in new[] { -1f, 1f })
            {
                Cube(p, s < 0 ? "Plataforma_E" : "Plataforma_D", c + new Vector3(8f * s, 1.5f, 2f), new Vector3(5f, 3f, 5f), rockM);
                Cube(p, "Musgo", c + new Vector3(8f * s, 3.01f, 2f), new Vector3(3.6f, 0.02f, 3.6f), mossM, false);
            }

            var melee = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/MeleeSlime.prefab");
            var group = new GameObject("InimigosDaArena").AddComponent<EnemyGroup>();
            group.transform.SetParent(p, false);
            var e1 = Inst(melee, c + new Vector3(-5f, 0.5f, -5f));
            var e2 = Inst(melee, c + new Vector3(5f, 0.5f, -5f));
            e1.transform.SetParent(group.transform, true);
            e2.transform.SetParent(group.transform, true);
            var gso = new SerializedObject(group);
            var list = gso.FindProperty("enemies");
            list.arraySize = 2;
            list.GetArrayElementAtIndex(0).objectReferenceValue = e1.GetComponent<Health>();
            list.GetArrayElementAtIndex(1).objectReferenceValue = e2.GetComponent<Health>();
            gso.ApplyModifiedPropertiesWithoutUndo();

            // Portal azul para o hub (anel do ProBuilder), virado para quem chega do norte; liga com a arena limpa.
            var portalGo = new GameObject("PortalAzul");
            portalGo.transform.SetParent(p, false);
            portalGo.transform.position = c + new Vector3(0f, 0f, -11f);
            var zone = portalGo.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 1.6f, 0f);
            zone.size = new Vector3(3f, 3.2f, 1.5f);
            var ring = Ring(portalGo.transform, "PortalAzul (ProBuilder)", portalGo.transform.position + Vector3.up * 1.8f, Vector3.forward, portalBlue);
            var portal = portalGo.AddComponent<Portal>();
            SetRef(portal, "visual", ring.GetComponent<Renderer>());
            var hubArrival = GameObject.Find("PontoDeChegada");
            if (hubArrival != null) SetRef(portal, "target", hubArrival.transform);
            var pso = new SerializedObject(portal);
            pso.FindProperty("startActive").boolValue = false;
            pso.FindProperty("activeColor").colorValue = new Color(0.25f, 0.45f, 1f);
            pso.FindProperty("activatedMessage").stringValue = "A arena está limpa! Entre no portal azul.";
            pso.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(group.OnCleared, portal.Activate);
            EditorUtility.SetDirty(group);

            Hint(p, c + new Vector3(0f, 1.5f, 10f), new Vector3(16f, 3f, 5f), "Clique = tiro rápido · segure e solte = tiro carregado · no ar, Ctrl/C = Impacto");
            Checkpoint(p, c + new Vector3(0f, 1f, 10f), new Vector3(8f, 2f, 2f));

            CaveDecor(p, c, R3, 3);
        }

        // ---------- Peças da caverna (ProBuilder) ----------

        /// <summary>Sala hexagonal fechada: piso, paredes nas 6 arestas (com portas) e teto. Aresta e vai de 60e° a 60(e+1)°.</summary>
        static void CaveRoom(Transform p, Vector3 c, float r, params Door[] doors)
        {
            Hex(p, "Piso (ProBuilder)", c + Vector3.down * 0.25f, r + 0.6f, 0.5f, floorM, true);
            for (int e = 0; e < 6; e++)
            {
                float a0 = 60f * e * Mathf.Deg2Rad, a1 = 60f * (e + 1) * Mathf.Deg2Rad;
                Vector3 v0 = c + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * r;
                Vector3 v1 = c + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * r;
                Vector3 mid = (v0 + v1) * 0.5f;
                Vector3 outward = mid - c; outward.y = 0f; outward.Normalize();
                Quaternion rot = Quaternion.LookRotation(outward);
                Vector3 basePos = mid + outward * 0.5f;
                float len = r + 1.2f;
                Door? door = null;
                foreach (var d in doors) if (d.Edge == e) door = d;
                if (door == null) { WallPiece(p, basePos, rot, -len / 2f, len / 2f, 0f, RoomH); continue; }
                float bottom = door.Value.Bottom;
                WallPiece(p, basePos, rot, -len / 2f, -DoorW / 2f, 0f, RoomH);
                WallPiece(p, basePos, rot, DoorW / 2f, len / 2f, 0f, RoomH);
                if (bottom > 0f) WallPiece(p, basePos, rot, -DoorW / 2f, DoorW / 2f, 0f, bottom);
                WallPiece(p, basePos, rot, -DoorW / 2f, DoorW / 2f, bottom + DoorH, RoomH);
                // Moldura de pedra na porta (só visual, do lado de dentro).
                var frame = ShapeGenerator.GenerateArch(PivotLocation.Center, 180f, DoorW / 2f + 0.4f, 0.5f, 0.6f, 10, true, true, true, true, true);
                Setup(frame, p, "Moldura da Porta (ProBuilder)", basePos - outward * 0.75f + Vector3.up * (bottom + DoorH - DoorW / 2f), rot, rockM, false);
            }
            Hex(p, "Teto (ProBuilder)", c + Vector3.up * (RoomH + 0.5f), r + 1.5f, 1f, rockM, true);
        }

        static void WallPiece(Transform p, Vector3 basePos, Quaternion rot, float x0, float x1, float y0, float y1)
        {
            var w = Cube(p, "Parede", Vector3.zero, new Vector3(x1 - x0, y1 - y0, 1f), rockM);
            w.transform.rotation = rot;
            w.transform.position = basePos + rot * Vector3.right * ((x0 + x1) / 2f) + Vector3.up * (y0 + (y1 - y0) / 2f);
        }

        /// <summary>Prisma hexagonal (cilindro de 6 lados do ProBuilder) com um vértice no ângulo 0°.</summary>
        static void Hex(Transform p, string name, Vector3 center, float r, float h, Material mat, bool collider)
        {
            var hex = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 6, r, h, 0);
            var v = hex.positions.OrderByDescending(q => q.y).First();
            float a = Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg;   // ângulo de um vértice da malha
            Setup(hex, p, name, center, Quaternion.Euler(0f, a, 0f), mat, collider);  // gira em Y: −a no sentido trigonométrico
        }

        /// <summary>Corredor fechado: piso (escada se tiver desnível), paredes e teto acompanhando a inclinação.</summary>
        static void Tunnel(Transform p, string name, Vector3 from, Vector3 to)
        {
            var g = Group(p, name);
            if (Mathf.Abs(from.y - to.y) > 0.05f)
            {
                Vector3 low = from.y < to.y ? from : to, high = from.y < to.y ? to : from;
                Stairs(g, low, high, DoorW, floorM);
            }
            else
            {
                Vector3 mid0 = (from + to) / 2f;
                Cube(g, "Piso", mid0 + Vector3.down * 0.25f, new Vector3(DoorW, 0.5f, Mathf.Abs(to.z - from.z)), floorM);
            }
            Vector3 dir = to - from;
            Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            Vector3 mid = (from + to) * 0.5f, right = rot * Vector3.right, up = rot * Vector3.up;
            float len = dir.magnitude;
            foreach (float side in new[] { -1f, 1f })
            {
                var w = Cube(g, "Parede", Vector3.zero, new Vector3(0.6f, CorridorH + 1f, len + 0.6f), rockM);
                w.transform.rotation = rot;
                w.transform.position = mid + right * side * (DoorW / 2f + 0.3f) + up * (CorridorH / 2f);
            }
            var ceil = Cube(g, "Teto", Vector3.zero, new Vector3(DoorW + 1.2f, 0.5f, len + 0.6f), rockM);
            ceil.transform.rotation = rot;
            ceil.transform.position = mid + up * (CorridorH + 0.25f);
            var l = new GameObject("Luz").AddComponent<Light>();
            l.transform.SetParent(g, false);
            l.transform.position = mid + Vector3.up * 3.5f;
            l.type = LightType.Point; l.range = 10f; l.intensity = 2f; l.color = new Color(1f, 0.8f, 0.55f);
        }

        static void LogAt(Transform p, Vector3 pos, float yaw)
        {
            var log = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 10, 0.5f, 3f, 0);
            Setup(log, p, "Tronco (ProBuilder)", pos, Quaternion.Euler(0f, yaw, 90f), woodM, true);
        }

        static MechanismDoor Grate(Transform p, Vector3 basePos, float width, float height)
        {
            var root = new GameObject("Grade");
            root.transform.SetParent(p, false);
            root.transform.position = basePos;
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, height / 2f, 0f);
            col.size = new Vector3(width, height, 0.4f);
            for (float x = -width / 2f + 0.3f; x <= width / 2f - 0.29f; x += 0.4f)
            {
                var bar = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 6, 0.06f, height, 0);
                Setup(bar, root.transform, "Barra (ProBuilder)", basePos + new Vector3(x, height / 2f, 0f), Quaternion.identity, barM, false);
                bar.gameObject.isStatic = false;
            }
            var top = Cube(root.transform, "Travessa", basePos + new Vector3(0f, height - 0.1f, 0f), new Vector3(width, 0.15f, 0.15f), barM, false);
            top.gameObject.isStatic = false;
            var door = root.AddComponent<MechanismDoor>();
            SetVector3(door, "openOffset", new Vector3(0f, -(height + 0.1f), 0f));
            return door;
        }

        static MechanismDoor ExitDoor(Transform p, Vector3 c, float r, int edge, string name)
        {
            float a = (60f * edge + 30f) * Mathf.Deg2Rad;
            Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Vector3 pos = c + outward * (Apothem(r) + 0.5f) + Vector3.up * (DoorH / 2f);
            var go = Cube(p, name, pos, new Vector3(DoorW, DoorH, 0.8f), Mat("M_MechanismDoor", new Color(0.45f, 0.3f, 0.25f)));
            go.transform.rotation = Quaternion.LookRotation(outward);
            go.gameObject.isStatic = false;
            // Tábuas e rebites (filhos, descem junto).
            for (int i = 0; i < 3; i++)
            {
                var plank = Cube(go.transform, "Faixa", pos + Vector3.up * (-1.2f + i * 1.2f) - outward * 0.45f, new Vector3(DoorW - 0.2f, 0.2f, 0.1f), barM, false);
                plank.transform.rotation = go.transform.rotation;
                plank.gameObject.isStatic = false;
            }
            var door = go.gameObject.AddComponent<MechanismDoor>();
            SetVector3(door, "openOffset", new Vector3(0f, -(DoorH + 0.1f), 0f));
            return door;
        }

        static WeightPlate Plate(Transform p, Vector3 pos, float required)
        {
            var root = new GameObject("PlacaDePeso");
            root.transform.SetParent(p, false);
            root.transform.position = pos;
            var zone = root.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 0.75f, 0f);
            zone.size = new Vector3(2.4f, 1.5f, 2.4f);
            var visual = Cube(root.transform, "Placa", pos + new Vector3(0f, 0.075f, 0f), new Vector3(2.4f, 0.15f, 2.4f), Mat("M_Plate", Color.white));
            visual.gameObject.isStatic = false;
            Cube(root.transform, "Moldura", pos + new Vector3(0f, 0.03f, 0f), new Vector3(3f, 0.06f, 3f), rockM, false);
            var plate = root.AddComponent<WeightPlate>();
            SetRef(plate, "plate", visual.transform);
            SetFloat(plate, "requiredSize", required);
            return plate;
        }

        static void Lights(Transform p, Vector3 c, Color color, params Vector3[] offsets)
        {
            foreach (var o in offsets)
            {
                var l = new GameObject("Luz").AddComponent<Light>();
                l.transform.SetParent(p, false);
                l.transform.position = c + o;
                l.type = LightType.Point; l.range = 22f; l.intensity = 5f; l.color = color;
            }
        }

        /// <summary>Estalactites no teto e cristais brilhando junto às paredes (sem colisão).</summary>
        static void CaveDecor(Transform p, Vector3 c, float r, int seed)
        {
            for (int i = 0; i < 14; i++)
            {
                float a = (i * 47f + seed * 19f) * Mathf.Deg2Rad;
                float d = (0.35f + ((i * 7 + seed) % 5) * 0.12f) * r;
                float h = 1f + ((i + seed) % 4) * 0.6f;
                var st = ShapeGenerator.GenerateCone(PivotLocation.Center, 0.35f + (i % 3) * 0.15f, h, 6);
                Setup(st, p, "Estalactite (ProBuilder)", c + new Vector3(Mathf.Cos(a) * d, RoomH - h / 2f, Mathf.Sin(a) * d), Quaternion.Euler(180f, i * 30f, 0f), rockM, false);
            }
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f + 15f + seed * 11f) * Mathf.Deg2Rad;
                Vector3 pos = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (r * 0.8f);
                var cr = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 0.35f, 0);
                Setup(cr, p, "Cristal da Caverna (ProBuilder)", pos + Vector3.up * 0.4f, Quaternion.Euler(15f, i * 40f, 10f), glowM, false);
                cr.transform.localScale = new Vector3(0.7f, 1.6f, 0.7f);
            }
        }
    }
}
