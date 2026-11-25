using System.Collections.Generic;
using System.Linq;
using SlimeHunter.Puzzles;
using SlimeHunter.World;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;
using static SlimeHunter.EditorTools.PlayerSetup;
using static SlimeHunter.EditorTools.TutorialBuilder;

namespace SlimeHunter.EditorTools
{
    /// <summary>
    /// Mapa novo na cena "floresta final", feito com ProBuilder (malhas editáveis no Editor).
    /// Por enquanto só o HUB, ampliado (×3) a partir do esboço do usuário:
    ///   centro 30×30 · corredor N-oeste → Bosque (sobe) · corredor N-leste → Ruínas
    ///   corredor sul → Lago (desce) · área oeste → Portão das 3 Chaves (árvore gigante atrás).
    /// Menu: Slime Hunter > Floresta Final > Construir Hub. Rodar de novo refaz só o que este script cria.
    /// </summary>
    public static class FlorestaFinalHub
    {
        const string ScenePath = "Assets/_Project/Scenes/floresta final.unity";
        const string RootName = "Hub (ProBuilder)";
        const float Slab = 0.5f;          // espessura dos pisos
        const float CliffH = 6f;          // paredes de pedra do hub (acima do pulo duplo)
        const float CorridorW = 6f;

        // Layout (mundo). Centro do hub = centro do esboço original.
        static readonly Vector3 HubC = new Vector3(5f, 0f, -5f);
        const float HubHalf = 15f;                                           // x −10..20, z −20..10
        static readonly Vector3 GateAreaC = new Vector3(-16f, 0f, -5f);     // 12×12: x −22..−10, z −11..1
        static readonly float BosqueX = -7f, RuinasX = 17f, LagoX = -7f;    // eixos dos corredores
        public static readonly Vector3 Arrival = new Vector3(12.5f, 0.1f, -5f);
        static readonly Vector3 TreeC = new Vector3(-34f, 0f, -5f);

        static Material grass, stone, rock, wood, leaves, temple, portalBlue, portalPurple, gold;

        [MenuItem("Slime Hunter/Floresta Final/Construir Tudo")]
        public static void BuildAll()
        {
            Build();
            FlorestaFinalBosque.Build();
            FlorestaFinalRuinas.Build();
            FlorestaFinalLago.Build();
            FlorestaFinalRei.Build();
            FlorestaFinalTutorial.Build();
        }

        [MenuItem("Slime Hunter/Floresta Final/Construir Hub")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError("[Slime Hunter] Abra a cena 'floresta final' antes de construir o Hub.");
                return;
            }

            Materials();
            RemoveSketchAndOldBuild(scene);

            var root = new GameObject(RootName).transform;
            Floors(Group(root, "Pisos"));
            Cliffs(Group(root, "Paredes de Pedra"));
            Corridors(Group(root, "Corredores"));
            Plaza(Group(root, "Praca Central"));
            Decor(Group(root, "Decoracao"));
            var arrival = ArrivalPortal(Group(root, "Chegada (portal azul)"));
            Gate(Group(root, "Portao das 3 Chaves"));
            Landmark(Group(root, "Arvore Gigante (landmark)"));
            Gameplay(Group(root, "Gameplay"), arrival);
            PlacePlayer(arrival);
            FlorestaFinalRei.LinkPortals(); // se a sala do Rei já existe, religa o portal roxo a ela

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Slime Hunter] Hub (ProBuilder) construído em " + ScenePath);
        }

        // ---------- Limpeza ----------

        static void RemoveSketchAndOldBuild(Scene scene)
        {
            // Esboço do usuário (substituído pelo hub ampliado) e build anterior deste script.
            string[] sketch = { "Hub", "Plane", "Plane (1)", "Plane (2)", "Plane (3)", RootName };
            foreach (var go in scene.GetRootGameObjects())
            {
                if (sketch.Contains(go.name)) Object.DestroyImmediate(go);
                else if (go.name == "Main Camera") go.SetActive(false); // o Player tem a própria câmera
            }
        }

        // ---------- Pisos ----------

        static void Floors(Transform p)
        {
            Cube(p, "Piso_Hub", new Vector3(HubC.x, -Slab / 2f, HubC.z), new Vector3(HubHalf * 2f, Slab, HubHalf * 2f), grass);
            Cube(p, "Piso_AreaDoPortao", new Vector3(GateAreaC.x, -Slab / 2f, GateAreaC.z), new Vector3(12f, Slab, 12f), temple);
            // Corredores: o do bosque termina em escada subindo 4 m; o do lago em escada descendo 3 m.
            Cube(p, "Piso_Corredor_Bosque", new Vector3(BosqueX, -Slab / 2f, 21f), new Vector3(CorridorW, Slab, 22f), stone);
            Cube(p, "Piso_Corredor_Ruinas", new Vector3(RuinasX, -Slab / 2f, 25f), new Vector3(CorridorW, Slab, 30f), stone);
            Cube(p, "Piso_Corredor_Lago", new Vector3(LagoX, -Slab / 2f, -31f), new Vector3(CorridorW, Slab, 22f), stone);
        }

        // ---------- Paredes de pedra (penhascos) com vãos nas saídas ----------

        static void Cliffs(Transform p)
        {
            float x0 = HubC.x - HubHalf, x1 = HubC.x + HubHalf, z0 = HubC.z - HubHalf, z1 = HubC.z + HubHalf; // −10, 20, −20, 10
            // Norte: entre os dois corredores.
            CliffRun(p, new Vector3(-4f, 0f, z1 + 1f), new Vector3(14f, 0f, z1 + 1f));
            // Sul: do corredor do lago até a quina leste.
            CliffRun(p, new Vector3(-4f, 0f, z0 - 1f), new Vector3(x1 + 2f, 0f, z0 - 1f));
            // Leste inteira.
            CliffRun(p, new Vector3(x1 + 1f, 0f, z0 - 2f), new Vector3(x1 + 1f, 0f, z1 + 2f));
            // Oeste: dois trechos deixando a passagem para a área do portão (z −11..1).
            CliffRun(p, new Vector3(x0 - 1f, 0f, 1f), new Vector3(x0 - 1f, 0f, z1 + 2f));
            CliffRun(p, new Vector3(x0 - 1f, 0f, z0 - 2f), new Vector3(x0 - 1f, 0f, -11f));
        }

        /// <summary>Paredão de pedra entre a e b em blocos de alturas variadas (todos ≥ CliffH).</summary>
        static void CliffRun(Transform p, Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 5f));
            Quaternion rot = Quaternion.LookRotation(d.normalized);
            for (int i = 0; i < n; i++)
            {
                float t0 = i / (float)n, t1 = (i + 1) / (float)n;
                Vector3 mid = Vector3.Lerp(a, b, (t0 + t1) / 2f);
                float h = CliffH + ((i * 37) % 5) * 0.6f;
                float thick = 2f + ((i * 13) % 3) * 0.4f;
                var block = Cube(p, "Penhasco", mid + Vector3.up * (h / 2f - 0.5f), new Vector3(thick, h + 1f, len / n + 0.4f), rock);
                block.transform.rotation = rot;
            }
        }

        // ---------- Corredores (paredes, escadas, fim "em construção") ----------

        static void Corridors(Transform p)
        {
            // Bosque: chão até z 32, escada sobe 4 m até z 40, patamar em y 4.
            var bosque = Group(p, "Corredor Bosque");
            CorridorWalls(bosque, BosqueX, 12f, 44f, -1f, 9f);
            Stairs(bosque, new Vector3(BosqueX, 0f, 32f), new Vector3(BosqueX, 4f, 40f));
            Cube(bosque, "Patamar", new Vector3(BosqueX, 4f - Slab / 2f, 42f), new Vector3(CorridorW, Slab, 4f), stone);
            // O patamar abre direto no Bosque (FlorestaFinalBosque), sem fim "em construção".

            // Ruínas: plano até o fim.
            var ruinas = Group(p, "Corredor Ruinas");
            CorridorWalls(ruinas, RuinasX, 12f, 40f, -1f, 6f);
            // O corredor abre direto nas Ruínas (FlorestaFinalRuinas).

            // Lago: chão até z −42, escada desce 3 m até z −50, patamar em y −3.
            var lago = Group(p, "Corredor Lago");
            CorridorWalls(lago, LagoX, -54f, -22f, -4f, 6f);
            Stairs(lago, new Vector3(LagoX, 0f, -42f), new Vector3(LagoX, -3f, -50f));
            Cube(lago, "Patamar", new Vector3(LagoX, -3f - Slab / 2f, -52f), new Vector3(CorridorW, Slab, 4f), stone);
            // O patamar encosta no píer do Lago (FlorestaFinalLago).
        }

        static void CorridorWalls(Transform p, float axisX, float zFrom, float zTo, float yBottom, float yTop)
        {
            float len = zTo - zFrom, h = yTop - yBottom;
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 c = new Vector3(axisX + side * (CorridorW / 2f + 1f), yBottom + h / 2f, (zFrom + zTo) / 2f);
                Cube(p, "ParedeCorredor", c, new Vector3(2f, h, len), rock);
            }
        }

        /// <summary>
        /// Escada de ProBuilder só visual + rampa invisível de colisão (degraus de verdade travam o slime
        /// pequeno, cujo step offset encolhe com o tamanho).
        /// </summary>
        internal static void Stairs(Transform p, Vector3 bottom, Vector3 top, float width = CorridorW, Material mat = null)
        {
            float rise = top.y - bottom.y;
            Vector3 flat = top - bottom; flat.y = 0f;
            float run = flat.magnitude;
            int steps = Mathf.Max(4, Mathf.RoundToInt(Mathf.Abs(rise) / 0.3f));

            var stair = ShapeGenerator.GenerateStair(PivotLocation.Center, new Vector3(width, Mathf.Abs(rise), run), steps, true);
            stair.gameObject.name = "Escada (ProBuilder)";
            stair.transform.SetParent(p, false);
            stair.GetComponent<MeshRenderer>().sharedMaterial = mat != null ? mat : stone;
            // Descobre para que lado a escada gerada sobe (+Z ou −Z) e gira para subir na direção certa.
            var verts = stair.positions;
            float hiZ = verts.Where(v => v.y > 0f).Average(v => v.z), loZ = verts.Where(v => v.y <= 0f).Average(v => v.z);
            Vector3 meshUp = hiZ > loZ ? Vector3.forward : Vector3.back;
            Vector3 wantUp = rise > 0f ? flat.normalized : -flat.normalized;
            // Só gira em volta de Y (FromToRotation com vetores opostos poderia virar a escada de cabeça para baixo).
            float yaw = Vector3.SignedAngle(meshUp, wantUp, Vector3.up);
            stair.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            stair.transform.position = (bottom + top) / 2f + Vector3.down * 0.01f;
            stair.gameObject.isStatic = true;

            // Rampa de colisão: o topo liga bottom a top.
            Vector3 dir = top - bottom;
            var ramp = new GameObject("Rampa (colisao)");
            ramp.transform.SetParent(p, false);
            ramp.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            ramp.transform.position = (bottom + top) / 2f - ramp.transform.up * 0.25f;
            ramp.AddComponent<BoxCollider>().size = new Vector3(width, 0.5f, dir.magnitude);
        }

        static void DeadEnd(Transform p, Vector3 at, string text)
        {
            var wall = new GameObject("Limite (em construcao)");
            wall.transform.SetParent(p, false);
            wall.transform.position = at + Vector3.up * 3f;
            wall.AddComponent<BoxCollider>().size = new Vector3(CorridorW + 2f, 6f, 0.5f);
            // Cerca de madeira só visual + placa.
            for (int i = -1; i <= 1; i++)
                Cube(p, "Cerca", at + new Vector3(i * 2f, 0.6f, 0f), new Vector3(0.25f, 1.2f, 0.25f), wood, false);
            Cube(p, "Cerca_Tabua", at + new Vector3(0f, 0.9f, 0f), new Vector3(CorridorW - 1f, 0.2f, 0.15f), wood, false);
            Sign(p, text, at + Vector3.up * 2.4f);
        }

        // ---------- Praça central ----------

        static void Plaza(Transform p)
        {
            var ring = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 16, 4f, 0.12f, 0);
            Setup(ring, p, "Praca (ProBuilder)", HubC + Vector3.up * 0.06f, Quaternion.identity, temple, true);
            // Estátua de slime (icosaedro achatado) com coroa: dica visual de quem manda na floresta.
            var statue = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 1f, 1);
            Setup(statue, p, "Estatua do Rei (ProBuilder)", HubC + Vector3.up * 1.6f, Quaternion.identity, temple, true);
            statue.transform.localScale = new Vector3(1.4f, 1.1f, 1.4f);
            Cube(p, "Pedestal", HubC + Vector3.up * 0.6f, new Vector3(1.6f, 1f, 1.6f), rock);
            var crown = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 5, 0.55f, 0.35f, 0);
            Setup(crown, p, "Coroa (ProBuilder)", HubC + Vector3.up * 2.85f, Quaternion.identity, gold, false);
        }

        // ---------- Decoração ----------

        static void Decor(Transform p)
        {
            // Árvores encostadas nos paredões, longe das bocas dos corredores.
            Vector3[] trees = { new Vector3(-1f, 0f, 7f), new Vector3(10f, 0f, 7f), new Vector3(17.5f, 0f, 2f),
                                new Vector3(17.5f, 0f, -12f), new Vector3(12f, 0f, -17.5f), new Vector3(1f, 0f, -17.5f) };
            foreach (var t in trees) Tree(p, t, 6f + Mathf.Abs(t.x % 3f), 2.4f);
            // Pedras.
            Vector3[] rocks = { new Vector3(-8f, 0f, -14f), new Vector3(18f, 0f, -18f), new Vector3(4f, 0f, 8f), new Vector3(18.5f, 0f, -2f) };
            for (int i = 0; i < rocks.Length; i++)
            {
                var r = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 1f, 0);
                Setup(r, p, "Pedra (ProBuilder)", rocks[i] + Vector3.up * 0.4f, Quaternion.Euler(0f, i * 47f, 0f), rock, true);
                r.transform.localScale = new Vector3(1.3f + i * 0.2f, 0.8f, 1.1f);
            }
            // Placas nas bocas das saídas.
            Sign(p, "Bosque das Raízes", new Vector3(BosqueX, 3f, 9f));
            Sign(p, "Ruínas do Monólito", new Vector3(RuinasX, 3f, 9f));
            Sign(p, "Lago do Vento", new Vector3(LagoX, 3f, -19f));
            Sign(p, "Portão das 3 Chaves", new Vector3(-9f, 3.5f, -5f));
        }

        static void Tree(Transform p, Vector3 basePos, float height, float canopy)
        {
            var trunk = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, 0.45f, height, 0);
            Setup(trunk, p, "Arvore_Tronco (ProBuilder)", basePos + Vector3.up * (height / 2f), Quaternion.identity, wood, true);
            var crown = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, canopy, 1);
            Setup(crown, p, "Arvore_Copa (ProBuilder)", basePos + Vector3.up * (height + canopy * 0.5f), Quaternion.Euler(0f, basePos.x * 13f, 0f), leaves, false);
        }

        // ---------- Chegada (portal azul do tutorial) ----------

        static Transform ArrivalPortal(Transform p)
        {
            var arrival = new GameObject("PontoDeChegada").transform;
            arrival.SetParent(p, false);
            arrival.position = Arrival;
            arrival.rotation = Quaternion.LookRotation(Vector3.left); // de frente para o portão e a árvore
            var ring = Ring(p, "PortalAzul (ProBuilder)", Arrival + new Vector3(2.5f, 1.7f, 0f), Vector3.left, portalBlue);
            return arrival;
        }

        /// <summary>Anel de portal (torus do ProBuilder) em pé, virado para 'facing'.</summary>
        internal static ProBuilderMesh Ring(Transform p, string name, Vector3 center, Vector3 facing, Material mat)
        {
            // Atenção: no código do ProBuilder 6.1 'innerRadius' é o raio TOTAL do anel e 'outerRadius' é a
            // espessura do tubo (o contrário do que a documentação sugere).
            var torus = ShapeGenerator.GenerateTorus(PivotLocation.Center, 8, 24, 1.85f, 0.25f, true, 360f, 360f);
            Setup(torus, p, name, center, Quaternion.identity, mat, false);
            // Alinha o eixo do anel (o eixo "fino" da malha) com a direção que o portal encara.
            Bounds b = torus.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector3 thin = b.size.x < b.size.y && b.size.x < b.size.z ? Vector3.right : (b.size.y < b.size.z ? Vector3.up : Vector3.forward);
            torus.transform.rotation = Quaternion.FromToRotation(thin, facing.normalized);
            // Disco brilhante no meio.
            var disk = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 20, 1.35f, 0.05f, 0);
            Setup(disk, p, name + " Disco", center, Quaternion.FromToRotation(Vector3.up, facing.normalized), mat, false);
            return torus;
        }

        // ---------- Portão das 3 Chaves ----------

        static void Gate(Transform p)
        {
            const float gateX = -18f;
            // Moldura: pilares + lintel + arco decorativo do ProBuilder, voltados para o hub (+X).
            Cube(p, "Pilar_N", new Vector3(gateX, 3.5f, -1.5f), new Vector3(1.2f, 7f, 1.2f), temple);
            Cube(p, "Pilar_S", new Vector3(gateX, 3.5f, -8.5f), new Vector3(1.2f, 7f, 1.2f), temple);
            Cube(p, "Lintel", new Vector3(gateX, 7.5f, -5f), new Vector3(1.4f, 1f, 8.4f), temple);
            var arch = ShapeGenerator.GenerateArch(PivotLocation.Center, 180f, 4.2f, 0.8f, 1.2f, 12, true, true, true, true, true);
            Setup(arch, p, "Arco (ProBuilder)", new Vector3(gateX, 9.6f, -5f), Quaternion.Euler(0f, 90f, 0f), temple, false);

            // Fecha a área: muros do portão até os lados e recinto atrás (só se entra pelo portão).
            // Do pilar até o muro lateral, sem fresta (o slime pequeno passa em 0.6 m).
            Cube(p, "Muro_N", new Vector3(gateX, 3.5f, 0.05f), new Vector3(1f, 7f, 2.3f), rock);
            Cube(p, "Muro_S", new Vector3(gateX, 3.5f, -10.05f), new Vector3(1f, 7f, 2.3f), rock);
            Cube(p, "Area_N", new Vector3(GateAreaC.x, 3.5f, 2f), new Vector3(12f, 7f, 2f), rock);
            Cube(p, "Area_S", new Vector3(GateAreaC.x, 3.5f, -12f), new Vector3(12f, 7f, 2f), rock);
            Cube(p, "Recinto_Fundo", new Vector3(-23f, 3.5f, -5f), new Vector3(2f, 7f, 14f), rock);

            // Portão com 3 encaixes (KeyGate): desce no chão quando as 3 chaves entram.
            var gate = Cube(p, "Portao (ProBuilder)", new Vector3(gateX, 3f, -5f), new Vector3(0.8f, 6f, 6f), Mat("M_KeyGate", new Color(0.35f, 0.3f, 0.45f)));
            gate.gameObject.isStatic = false;
            var keyGate = gate.gameObject.AddComponent<KeyGate>();
            (string id, string name, Color color)[] keys =
            {
                ("chave_esmeralda", "Chave Esmeralda", new Color(0.2f, 1f, 0.35f)),
                ("chave_rubi", "Chave Rubi", new Color(1f, 0.2f, 0.2f)),
                ("chave_safira", "Chave Safira", new Color(0.25f, 0.5f, 1f)),
            };
            var so = new SerializedObject(keyGate);
            var slots = so.FindProperty("slots");
            slots.arraySize = keys.Length;
            for (int i = 0; i < keys.Length; i++)
            {
                var socket = Cube(p, $"Encaixe{i}", new Vector3(gateX + 0.45f, 4.4f, -6.5f + i * 1.5f), new Vector3(0.15f, 0.6f, 0.6f), Mat("M_Socket", new Color(0.15f, 0.15f, 0.18f)), false);
                socket.gameObject.isStatic = false;
                socket.transform.SetParent(gate.transform, true);
                var slot = slots.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("itemId").stringValue = keys[i].id;
                slot.FindPropertyRelative("displayName").stringValue = keys[i].name;
                slot.FindPropertyRelative("socket").objectReferenceValue = socket.GetComponent<Renderer>();
                slot.FindPropertyRelative("litColor").colorValue = keys[i].color;
            }
            so.FindProperty("openOffset").vector3Value = new Vector3(0f, -6.2f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Portal roxo no recinto: liga quando o portão abre. Sem destino por enquanto (a sala do Rei ainda não existe nesta cena).
            var purple = new GameObject("PortalRoxo");
            purple.transform.SetParent(p, false);
            purple.transform.position = new Vector3(-20.5f, 0f, -5f);
            var zone = purple.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 1.6f, 0f);
            zone.size = new Vector3(1.5f, 3.2f, 3f);
            var ring = Ring(purple.transform, "PortalRoxo (ProBuilder)", purple.transform.position + Vector3.up * 1.7f, Vector3.right, portalPurple);
            var portal = purple.AddComponent<Portal>();
            SetRef(portal, "visual", ring.GetComponent<Renderer>());
            var pso = new SerializedObject(portal);
            pso.FindProperty("startActive").boolValue = false;
            pso.FindProperty("activeColor").colorValue = new Color(0.65f, 0.3f, 1f);
            pso.FindProperty("activatedMessage").stringValue = "O portão se abriu! (A sala do Rei ainda será construída nesta cena.)";
            pso.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(keyGate.OnOpened, portal.Activate);
            EditorUtility.SetDirty(keyGate);
        }

        // ---------- Árvore gigante ----------

        static void Landmark(Transform p)
        {
            var trunk = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 12, 4.5f, 50f, 0);
            Setup(trunk, p, "Tronco (ProBuilder)", TreeC + Vector3.up * 19f, Quaternion.identity, wood, true);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f + 15f;
                var root = Cube(p, "Raiz", TreeC + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 6.5f + Vector3.up * 0.3f, new Vector3(2.4f, 2.6f, 9f), wood, false);
                root.transform.rotation = Quaternion.Euler(18f, a, 0f);
            }
            Vector3[] blobs = { new Vector3(0f, 47f, 0f), new Vector3(10f, 43f, 6f), new Vector3(-9f, 44f, -7f), new Vector3(5f, 53f, -8f), new Vector3(-6f, 52f, 8f) };
            float[] radii = { 17f, 11f, 12f, 9f, 9f };
            for (int i = 0; i < blobs.Length; i++)
            {
                var c = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, radii[i], 1);
                Setup(c, p, "Copa (ProBuilder)", TreeC + blobs[i], Quaternion.Euler(0f, i * 31f, 0f), leaves, false);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                var f = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 1.1f, 0);
                Setup(f, p, "Fruto (ProBuilder)", TreeC + new Vector3(Mathf.Cos(a) * 15f, 41f + (i % 3) * 3f, Mathf.Sin(a) * 15f), Quaternion.identity, gold, false);
            }
            var light = new GameObject("LuzDaArvore").AddComponent<Light>();
            light.transform.SetParent(p, false);
            light.transform.position = TreeC + Vector3.up * 38f;
            light.type = LightType.Point;
            light.range = 45f;
            light.intensity = 3f;
            light.color = new Color(1f, 0.85f, 0.5f);
        }

        // ---------- Gameplay ----------

        static void Gameplay(Transform p, Transform arrival)
        {
            Banner(p, HubC, HubHalf, "Hub Central", "Floresta Semiaberta");
            Banner(p, GateAreaC, 12f / 1.7f, "Portão das 3 Chaves", "Arena do Rei Slime");
            Hint(p, Arrival + new Vector3(-2f, 1.5f, 0f), new Vector3(6f, 3f, 6f),
                "Ache as 3 chaves (Bosque, Ruínas e Lago) para abrir o portão da árvore gigante.");
            Checkpoint(p, Arrival + Vector3.up, new Vector3(4f, 2f, 4f));

            // Rede de segurança: cair volta ao último checkpoint sem perder vida.
            var net = new GameObject("RedeDeSeguranca");
            net.transform.SetParent(p, false);
            net.transform.position = new Vector3(0f, -14f, -5f);
            var col = net.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(200f, 4f, 200f);
            var hz = net.AddComponent<HazardZone>();
            var so = new SerializedObject(hz);
            so.FindProperty("returnToCheckpoint").boolValue = true;
            so.FindProperty("damage").floatValue = 10f;
            so.FindProperty("message").stringValue = "Você caiu! De volta ao último ponto seguro.";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void PlacePlayer(Transform arrival)
        {
            var existing = Object.FindAnyObjectByType<Player.PlayerController>();
            GameObject player = existing != null ? existing.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Player.prefab"));
            player.name = "Player";
            // O jogo começa no tutorial (se ele já existe na cena); senão, na chegada do hub.
            var spawn = GameObject.Find(FlorestaFinalTutorial.SpawnName);
            Transform at = spawn != null ? spawn.transform : arrival;
            player.transform.SetPositionAndRotation(at.position, at.rotation);
        }

        // ---------- ProBuilder: utilidades ----------

        internal static ProBuilderMesh Cube(Transform p, string name, Vector3 center, Vector3 size, Material mat, bool collider = true)
        {
            var pb = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            return Setup(pb, p, name, center, Quaternion.identity, mat, collider);
        }

        internal static ProBuilderMesh Setup(ProBuilderMesh pb, Transform p, string name, Vector3 pos, Quaternion rot, Material mat, bool collider)
        {
            pb.gameObject.name = name;
            pb.transform.SetParent(p, false);
            pb.transform.SetPositionAndRotation(pos, rot);
            pb.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider)
            {
                // (sem '??': no Editor um componente ausente volta como "null falso" do Unity)
                var mc = pb.gameObject.GetComponent<MeshCollider>();
                if (mc == null) mc = pb.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = pb.GetComponent<MeshFilter>().sharedMesh;
            }
            pb.gameObject.isStatic = true;
            return pb;
        }

        internal static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        internal static void Sign(Transform p, string text, Vector3 pos)
        {
            var go = new GameObject("Placa " + text.Split('\n')[0]);
            go.transform.SetParent(p, false);
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = text;
            tm.fontSize = 56;
            tm.characterSize = 0.07f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            go.AddComponent<Billboard>();
        }

        internal static void Materials()
        {
            grass = Mat("M_FF_Grama", new Color(0.45f, 0.68f, 0.38f));
            stone = Mat("M_FF_Caminho", new Color(0.62f, 0.56f, 0.46f));
            rock = Mat("M_FF_Rocha", new Color(0.44f, 0.42f, 0.4f));
            wood = Mat("M_FF_Madeira", new Color(0.42f, 0.28f, 0.18f));
            leaves = Mat("M_FF_Folhagem", new Color(0.32f, 0.62f, 0.35f));
            temple = Mat("M_FF_Templo", new Color(0.58f, 0.54f, 0.64f));
            portalBlue = Mat("M_FF_PortalAzul", new Color(0.3f, 0.5f, 1f));
            portalPurple = Mat("M_FF_PortalRoxo", new Color(0.65f, 0.3f, 1f));
            gold = Mat("M_FF_Dourado", new Color(1f, 0.82f, 0.3f));
        }
    }
}
