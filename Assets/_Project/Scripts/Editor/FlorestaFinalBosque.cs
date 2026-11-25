using System.Linq;
using SlimeHunter.Interaction;
using SlimeHunter.Puzzles;
using UnityEditor;
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
    /// "floresta final" (ProBuilder) — 5 Bosque das Raízes Retorcidas (Notion, Parte 2).
    /// Fica no alto da escada do corredor noroeste do hub (piso em y 4). Entrada pelo canto SE.
    /// Três clareiras (sul → norte) separadas por duas paredes de raízes; cada parede tem uma bifurcação:
    ///   • túnel baixo de raízes (1.4 × 1.0 m) a LESTE: encolher atirando e passar escondido;
    ///   • vão largo a OESTE: atravessar as clareiras e enfrentar os slimes (e absorvê-los).
    /// No canto NO fica o santuário com a Chave 1 (Esmeralda).
    /// Coordenadas locais: C = centro do bosque, +X leste, +Z norte, y = 0 no piso.
    /// Menu: Slime Hunter > Floresta Final > Construir Bosque.
    /// </summary>
    public static class FlorestaFinalBosque
    {
        const string ScenePath = "Assets/_Project/Scenes/floresta final.unity";
        const string RootName = "Bosque (ProBuilder)";
        public static readonly Vector3 C = new Vector3(-20f, 4f, 62f);   // x −38..−2, z 44..80
        const float Half = 18f;
        const float WallH = 6f;                         // acima do pulo duplo
        const float TunnelW = 1.4f, TunnelH = 1.0f;     // fenda do tutorial: só tamanho ≤ 1.4
        const float GapW = 4f;
        const float Wall1Z = -6f, Wall2Z = 6f, WallT = 3f;
        const float Tunnel1X = 10f, Gap1X = -13f;
        const float Tunnel2X = 12f, Gap2X = -5f;
        const float EntranceX0 = 8f, EntranceX1 = 14f;  // boca do corredor do hub (x −10..−4 no mundo)

        static Material floor, root, bark, moss, stoneM, leaves, mushroom, glow;

        struct Opening
        {
            public float X, W, H;
            public Opening(float x, float w, float h) { X = x; W = w; H = h; }
        }

        [MenuItem("Slime Hunter/Floresta Final/Construir Bosque")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError("[Slime Hunter] Abra a cena 'floresta final' antes de construir o Bosque.");
                return;
            }
            foreach (var go in scene.GetRootGameObjects().Where(g => g.name == RootName)) Object.DestroyImmediate(go);

            floor = Mat("M_FF_GramaBosque", new Color(0.3f, 0.5f, 0.28f));
            root = Mat("M_FF_Raiz", new Color(0.36f, 0.24f, 0.16f));
            bark = Mat("M_FF_Madeira", new Color(0.42f, 0.28f, 0.18f));
            moss = Mat("M_FF_Musgo", new Color(0.36f, 0.55f, 0.25f));
            stoneM = Mat("M_FF_Templo", new Color(0.58f, 0.54f, 0.64f));
            leaves = Mat("M_FF_FolhagemEscura", new Color(0.2f, 0.45f, 0.26f));
            mushroom = Mat("M_FF_Cogumelo", new Color(0.85f, 0.35f, 0.3f));
            glow = Mat("M_FF_Vagalume", new Color(0.75f, 1f, 0.5f));

            var gooOrb = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/GooOrb.prefab");
            var melee = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/MeleeSlime.prefab");
            var ranged = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/RangedSlime.prefab");
            var key = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Key_Dourada.prefab");

            var rootT = new GameObject(RootName).transform;

            Cube(Group(rootT, "Piso"), "Piso_Bosque", W(new Vector3(0f, -0.25f, 0f)), new Vector3(Half * 2f + 1f, 0.5f, Half * 2f + 1f), floor);
            Perimeter(Group(rootT, "Borda de Raizes"));

            var walls = Group(rootT, "Paredes de Raizes");
            RootWall(walls, Wall1Z, new Opening(Gap1X, GapW, WallH), new Opening(Tunnel1X, TunnelW, TunnelH));
            RootWall(walls, Wall2Z, new Opening(Gap2X, GapW, WallH), new Opening(Tunnel2X, TunnelW, TunnelH));

            Decor(Group(rootT, "Decoracao"));

            // Patrulhas nas clareiras a oeste, longe dos túneis (leste): passeando 4 m não chegam a 14 m da rota furtiva. 3 melee + 1 ranged + guarda = 5.
            var enemies = Group(rootT, "Patrulha de Slimes");
            Spawn(enemies, melee, new Vector3(-11f, 0.5f, -12f));
            Spawn(enemies, melee, new Vector3(-8f, 0.5f, -15f));
            Spawn(enemies, melee, new Vector3(-10f, 0.5f, 0f));
            Spawn(enemies, ranged, new Vector3(-14f, 0.5f, 2f));

            // Gosma depois de cada túnel: dá para crescer um pouco sem lutar.
            var goo = Group(rootT, "Gosma");
            Spawn(goo, gooOrb, new Vector3(13f, 0.6f, -2f));
            Spawn(goo, gooOrb, new Vector3(14.5f, 0.6f, 10f));

            Sanctuary(Group(rootT, "Santuario da Chave 1"), key, ranged);
            Gameplay(Group(rootT, "Gameplay"));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Slime Hunter] Bosque (ProBuilder) construído em " + ScenePath);
        }

        static Vector3 W(Vector3 local) => C + local;

        // ---------- Borda ----------

        static void Perimeter(Transform p)
        {
            const float t = 1.5f;
            float edge = Half + t / 2f;
            // Sul com a boca do corredor (x local 8..14); oeste, norte e leste inteiras.
            RootSegment(p, -Half - t, EntranceX0, -edge, true);
            RootSegment(p, EntranceX1, Half + t, -edge, true);
            RootSegment(p, -Half - t, Half + t, edge, true);
            RootSegment(p, -Half, Half, -edge, false);
            RootSegment(p, -Half, Half, edge, false);

            // Raízes enormes deitadas por cima da borda + árvores-muralha nos cantos.
            for (float z = -Half + 3f; z < Half; z += 7f) Log(p, W(new Vector3(-Half, WallH + 0.3f, z)), 90f, 0.9f, 7f);
            for (float z = -Half + 6f; z < Half; z += 7f) Log(p, W(new Vector3(Half, WallH + 0.3f, z)), 90f, 0.9f, 7f);
            for (float x = -Half + 3f; x < Half; x += 7f) Log(p, W(new Vector3(x, WallH + 0.3f, Half)), 0f, 0.9f, 7f);
            foreach (var c in new[] { new Vector3(-Half, 0f, -Half), new Vector3(-Half, 0f, Half), new Vector3(Half, 0f, Half), new Vector3(Half, 0f, -Half) })
                BigTree(p, c, 13f, 1.8f, 4.5f, false);
        }

        /// <summary>Muro de raízes de a até b (ao longo de X se alongX, senão de Z) na linha 'at'.</summary>
        static void RootSegment(Transform p, float a, float b, float at, bool alongX)
        {
            float len = b - a, mid = (a + b) / 2f;
            Vector3 center = alongX ? new Vector3(mid, WallH / 2f, at) : new Vector3(at, WallH / 2f, mid);
            Vector3 size = alongX ? new Vector3(len, WallH, 1.5f) : new Vector3(1.5f, WallH, len);
            Cube(p, "Borda", W(center), size, root);
        }

        // ---------- Paredes de raízes com a bifurcação ----------

        static void RootWall(Transform p, float z, params Opening[] openings)
        {
            System.Array.Sort(openings, (a, b) => a.X.CompareTo(b.X));
            float x = -Half;
            int n = 0;
            foreach (var o in openings)
            {
                float end = o.X - o.W / 2f;
                if (end - x > 0.01f) WallPiece(p, x, end, z, n++);
                if (o.H < WallH)
                {
                    // Túnel: raiz por cima da fenda; boca marcada por um arco de raiz (ProBuilder) e musgo.
                    Cube(p, "Raiz_Tunel", W(new Vector3(o.X, o.H + (WallH - o.H) / 2f, z)), new Vector3(o.W, WallH - o.H, WallT), root);
                    foreach (float side in new[] { -1f, 1f })
                    {
                        var arch = ShapeGenerator.GenerateArch(PivotLocation.Center, 180f, 1.3f, 0.35f, 0.5f, 8, true, true, true, true, true);
                        Setup(arch, p, "Arco do Tunel (ProBuilder)", W(new Vector3(o.X, o.H + 0.15f, z + side * (WallT / 2f + 0.05f))), Quaternion.identity, bark, false);
                    }
                    Cube(p, "Musgo", W(new Vector3(o.X, WallH + 0.05f, z)), new Vector3(o.W + 2f, 0.1f, WallT + 0.4f), moss, false);
                }
                else
                {
                    // Vão largo: duas raízes grossas em pé nas laterais.
                    foreach (float side in new[] { -1f, 1f })
                        Pillar(p, W(new Vector3(o.X + side * (o.W / 2f + 0.3f), 0f, z)), 0.9f, WallH + 1.5f, root);
                }
                x = o.X + o.W / 2f;
            }
            if (Half - x > 0.01f) WallPiece(p, x, Half, z, n);

            for (float rx = -Half + 3f; rx < Half; rx += 8f) Log(p, W(new Vector3(rx, WallH + 0.4f, z)), 20f, 0.8f, 5f);
        }

        /// <summary>Trecho de parede de raízes: bloco principal + nós de raiz saltando para os lados.</summary>
        static void WallPiece(Transform p, float x0, float x1, float z, int seed)
        {
            Cube(p, "Raiz", W(new Vector3((x0 + x1) / 2f, WallH / 2f, z)), new Vector3(x1 - x0, WallH, WallT), root);
            for (float x = x0 + 1.5f; x < x1 - 1f; x += 3.5f)
            {
                int i = Mathf.RoundToInt(x * 7f) + seed;
                float side = (i % 2 == 0) ? 1f : -1f;
                float h = 2f + (Mathf.Abs(i) % 3) * 1.2f;
                var knot = Cube(p, "No de Raiz", W(new Vector3(x, h / 2f, z + side * (WallT / 2f + 0.3f))), new Vector3(1.1f, h, 0.8f), bark, false);
                knot.transform.rotation = Quaternion.Euler(side * 12f, (i % 5) * 9f, 0f);
            }
        }

        // ---------- Decoração ----------

        static void Decor(Transform p)
        {
            // Árvores tortas nas clareiras (troncos com colisão), longe dos túneis e dos vãos.
            BigTree(p, new Vector3(-14f, 0f, -14f), 10f, 1f, 4f, true);
            BigTree(p, new Vector3(3f, 0f, -12f), 9f, 0.9f, 3.5f, true);
            BigTree(p, new Vector3(2.5f, 0f, 0f), 10f, 1f, 4f, true);
            BigTree(p, new Vector3(-16.5f, 0f, 3.5f), 9f, 0.9f, 3.5f, true);
            BigTree(p, new Vector3(9f, 0f, 14f), 10f, 1f, 4f, true);
            BigTree(p, new Vector3(16f, 0f, 2f), 8f, 0.8f, 3f, true);

            // Cogumelos e pedras com musgo (sem colisão, só ambiente).
            Vector3[] shrooms = { new Vector3(-16f, 0f, -9f), new Vector3(6f, 0f, -16f), new Vector3(-11f, 0f, 9f), new Vector3(16f, 0f, -16f),
                                  new Vector3(15.5f, 0f, 15f), new Vector3(-3f, 0f, 16f), new Vector3(4f, 0f, 3f), new Vector3(-16.5f, 0f, 16.5f) };
            for (int i = 0; i < shrooms.Length; i++) Mushroom(p, shrooms[i], 0.7f + (i % 3) * 0.35f);

            // Vagalumes: luzes fracas esverdeadas.
            Vector3[] lights = { new Vector3(-8f, 3f, -12f), new Vector3(12f, 2.5f, -2f), new Vector3(-6f, 3f, 12f), new Vector3(12f, 3f, 12f) };
            foreach (var l in lights)
            {
                var lg = new GameObject("Vagalumes").AddComponent<Light>();
                lg.transform.SetParent(p, false);
                lg.transform.position = W(l);
                lg.type = LightType.Point;
                lg.range = 12f;
                lg.intensity = 1.5f;
                lg.color = new Color(0.7f, 1f, 0.5f);
                var dot = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 0.12f, 0);
                Setup(dot, p, "Vagalume (ProBuilder)", W(l), Quaternion.identity, glow, false);
            }

            Sign(p, "Bosque das Raízes\nRetorcidas", W(new Vector3(11f, 3.5f, -16f)));
        }

        static void BigTree(Transform p, Vector3 local, float height, float radius, float canopy, bool collider)
        {
            Vector3 basePos = W(local);
            var trunk = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, radius, height, 0);
            float lean = ((Mathf.RoundToInt(local.x * 3f + local.z) % 3) - 1) * 6f;
            Setup(trunk, p, "Tronco (ProBuilder)", basePos + Vector3.up * (height / 2f), Quaternion.Euler(lean, local.x * 11f, lean * 0.5f), bark, collider);
            // Raízes na base.
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + local.z * 7f;
                var r = Cube(p, "Raiz da Arvore", basePos + Quaternion.Euler(0f, a, 0f) * Vector3.forward * (radius + 0.8f) + Vector3.up * 0.2f,
                             new Vector3(0.6f, 0.7f, 2.2f), bark, false);
                r.transform.rotation = Quaternion.Euler(20f, a, 0f);
            }
            Vector3 top = trunk.transform.TransformPoint(Vector3.up * height / 2f);
            var crown = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, canopy, 1);
            Setup(crown, p, "Copa (ProBuilder)", top + Vector3.up * canopy * 0.4f, Quaternion.Euler(0f, local.x * 17f, 0f), leaves, false);
            crown.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
        }

        static void Pillar(Transform p, Vector3 basePos, float radius, float height, Material mat)
        {
            var c = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, radius, height, 0);
            Setup(c, p, "Raiz em pe (ProBuilder)", basePos + Vector3.up * (height / 2f), Quaternion.identity, mat, true);
        }

        static void Log(Transform p, Vector3 pos, float yaw, float radius, float length)
        {
            var c = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, radius, length, 0);
            Setup(c, p, "Raiz Deitada (ProBuilder)", pos, Quaternion.Euler(0f, yaw, 90f), bark, false);
        }

        static void Mushroom(Transform p, Vector3 local, float s)
        {
            var stem = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 6, 0.15f * s, 0.8f * s, 0);
            Setup(stem, p, "Cogumelo_Pe (ProBuilder)", W(local) + Vector3.up * 0.4f * s, Quaternion.identity, stoneM, false);
            var cap = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 0.45f * s, 1);
            Setup(cap, p, "Cogumelo_Chapeu (ProBuilder)", W(local) + Vector3.up * 0.85f * s, Quaternion.identity, mushroom, false);
            cap.transform.localScale = new Vector3(1f, 0.5f, 1f);
        }

        // ---------- Santuário ----------

        static void Sanctuary(Transform p, GameObject keyPrefab, GameObject ranged)
        {
            // Plataforma de pedra de 1.5 m no canto NO com escada vindo do leste; pedestal com a chave; colunas e telhado.
            Vector3 altar = new Vector3(-12f, 0f, 13f);
            Cube(p, "Plataforma", W(altar + new Vector3(0f, 0.75f, 0f)), new Vector3(7f, 1.5f, 7f), stoneM);
            Stairs(p, W(new Vector3(-3f, 0f, 13f)), W(new Vector3(-8.5f, 1.5f, 13f)), 2.5f, stoneM);
            Cube(p, "Pedestal", W(altar + new Vector3(0f, 2f, 0f)), new Vector3(1.2f, 1f, 1.2f), stoneM);
            for (int i = 0; i < 4; i++)
            {
                var col = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, 0.35f, 3.2f, 0);
                Setup(col, p, "Coluna (ProBuilder)", W(altar + new Vector3(i % 2 == 0 ? -3f : 3f, 3.1f, i < 2 ? -3f : 3f)), Quaternion.identity, stoneM, true);
            }
            // Telhado só visual (não atrapalha o slime grande).
            Cube(p, "Telhado", W(altar + new Vector3(0f, 4.9f, 0f)), new Vector3(7.4f, 0.4f, 7.4f), stoneM, false);
            Cube(p, "Telhado_Topo", W(altar + new Vector3(0f, 5.4f, 0f)), new Vector3(4.5f, 0.6f, 4.5f), moss, false);
            var gem = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 0.5f, 0);
            Setup(gem, p, "Gema (ProBuilder)", W(altar + new Vector3(0f, 6.2f, 0f)), Quaternion.identity, Mat("M_chave_esmeralda", new Color(0.2f, 1f, 0.35f)), false);

            var keyGo = Inst(keyPrefab, W(altar + new Vector3(0f, 2.9f, 0f)));
            keyGo.name = "Key_chave_esmeralda";
            keyGo.transform.SetParent(p, true);
            var so = new SerializedObject(keyGo.GetComponent<Pickup>());
            so.FindProperty("itemId").stringValue = "chave_esmeralda";
            so.FindProperty("displayName").stringValue = "Chave Esmeralda";
            so.ApplyModifiedPropertiesWithoutUndo();
            var r = keyGo.GetComponent<Renderer>();
            r.sharedMaterial = Mat("M_chave_esmeralda", new Color(0.2f, 1f, 0.35f));
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);

            // Guarda: slime ranged num toco de raiz alto, vigiando quem chega.
            var stump = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 10, 1.4f, 3f, 0);
            Setup(stump, p, "Toco do Guarda (ProBuilder)", W(new Vector3(0.5f, 1.5f, 16f)), Quaternion.identity, bark, true);
            // Fica parado no toco (sem passear), senão cai de lá.
            var guard = Inst(ranged, W(new Vector3(0.5f, 3.5f, 16f)));
            guard.name = "Guarda do Santuario";
            guard.transform.SetParent(p, true);
            SetFloat(guard.GetComponent<SlimeHunter.Enemies.SlimeEnemy>(), "wanderRadius", 0f);
        }

        // ---------- Gameplay ----------

        static void Gameplay(Transform p)
        {
            Banner(p, C, Half * 2f / 1.7f, "Bosque das Raízes Retorcidas", "Chave 1 · Esmeralda");
            Vector3 entrance = new Vector3(11f, 0f, -15f);
            Hint(p, W(entrance + Vector3.up * 1.5f), new Vector3(6f, 3f, 5f),
                "Raízes formam túneis baixos: encolha (atire) para passar escondido, ou vá pelo caminho largo e enfrente os slimes.");
            Checkpoint(p, W(entrance + Vector3.up), new Vector3(5f, 2f, 4f));
            SizeHint(p, new Vector3(Tunnel1X, 1f, Wall1Z - WallT / 2f - 1.5f));
            SizeHint(p, new Vector3(Tunnel2X, 1f, Wall2Z - WallT / 2f - 1.5f));
            Checkpoint(p, W(new Vector3(12f, 1f, -1f)), new Vector3(4f, 2f, 4f));
            Checkpoint(p, W(new Vector3(5f, 1f, 10f)), new Vector3(6f, 2f, 4f));
        }

        static void Spawn(Transform p, GameObject prefab, Vector3 local) => Inst(prefab, W(local)).transform.SetParent(p, true);

        static void SizeHint(Transform p, Vector3 local)
        {
            var go = Trigger(p, "DicaDoTunel", W(local), new Vector3(5f, 2f, 3f));
            SetFloat(go.AddComponent<SizeHintZone>(), "size", 1.4f);
        }
    }
}
