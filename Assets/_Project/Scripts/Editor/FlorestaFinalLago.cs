using System.Linq;
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
    /// "floresta final" (ProBuilder) — 7 Lago do Vento (Notion, Parte 2).
    /// Ao pé da escada do corredor sul do hub (patamar em y −3 = topo do píer).
    ///   • Cair na água volta ao último checkpoint (com dano).
    ///   • Píer + pedra das runas (ordem: azul → laranja → verde → rosa).
    ///   • Duas cadeias de plataformas subindo até correntes de vento que levam às plataformas altas (slimes ranged).
    ///   • 4 cristais nos cantos; resolver a sequência ergue a ponte até a ilha com a Chave 3 (Safira).
    /// Coordenadas locais: L = centro do lago na superfície da água, +Z norte (lado do hub). Mesmas posições
    /// do LagoBuilder antigo (testadas), só o píer vai até a borda norte para encostar no patamar.
    /// Menu: Slime Hunter > Floresta Final > Construir Lago.
    /// </summary>
    public static class FlorestaFinalLago
    {
        const string ScenePath = "Assets/_Project/Scenes/floresta final.unity";
        const string RootName = "Lago (ProBuilder)";
        public static readonly Vector3 L = new Vector3(-10f, -4.5f, -81.5f);   // x −37.5..17.5, z −109..−54
        const float Size = 55f, Half = 27.5f;
        const float WaterDepth = 0.7f;
        const float PierTop = 1.5f;                                            // = y −3 no mundo (patamar do corredor)

        static readonly Color Blue = new Color(0.25f, 0.5f, 1f);
        static readonly Color Orange = new Color(1f, 0.55f, 0.1f);
        static readonly Color Green = new Color(0.2f, 1f, 0.35f);
        static readonly Color Pink = new Color(1f, 0.45f, 0.8f);

        static Material plat, stoneM, wind, water, bed, rockM, mossM, reed, lily, wood;
        static Vector3 W(float x, float y, float z) => L + new Vector3(x, y, z);

        [MenuItem("Slime Hunter/Floresta Final/Construir Lago")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError("[Slime Hunter] Abra a cena 'floresta final' antes de construir o Lago.");
                return;
            }
            foreach (var go in scene.GetRootGameObjects().Where(g => g.name == RootName)) Object.DestroyImmediate(go);

            plat = Mat("M_FF_PlataformaLago", new Color(0.5f, 0.48f, 0.58f));
            stoneM = Mat("M_FF_Templo", new Color(0.58f, 0.54f, 0.64f));
            wind = Mat("M_FF_Vento", new Color(0.75f, 0.95f, 1f));
            water = Mat("M_FF_Agua", new Color(0.2f, 0.45f, 0.75f));
            bed = Mat("M_FF_FundoLago", new Color(0.18f, 0.25f, 0.3f));
            rockM = Mat("M_FF_Rocha", new Color(0.44f, 0.42f, 0.4f));
            mossM = Mat("M_FF_Musgo", new Color(0.36f, 0.55f, 0.25f));
            reed = Mat("M_FF_Junco", new Color(0.45f, 0.6f, 0.3f));
            lily = Mat("M_FF_Vitoria", new Color(0.3f, 0.6f, 0.3f));
            wood = Mat("M_FF_Madeira", new Color(0.42f, 0.28f, 0.18f));
            var ranged = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/RangedSlime.prefab");
            var key = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Key_Dourada.prefab");
            var goo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/GooOrb.prefab");

            var p = new GameObject(RootName).transform;
            Water(Group(p, "Agua"));
            Shore(Group(p, "Margens"));

            // Píer (de madeira, até a borda norte) e pedra das runas a oeste dele.
            var entry = Group(p, "Entrada");
            Pier(entry);
            Plat(entry, "PedraDasRunas", -5f, 3f, 23f, 6f, 6f, stoneM);
            Stairs(entry, W(1.5f, PierTop, 23f), W(-2f, 3f, 23f), 2.5f, stoneM);
            Runes(entry);
            Checkpoint(entry, W(3f, PierTop + 1f, 24f), new Vector3(8f, 2f, 4f));
            Hint(entry, W(3f, PierTop + 1.5f, 24.5f), new Vector3(9f, 3f, 3f),
                "Acerte os 4 cristais dos cantos na ordem das runas (na pedra ao lado). As correntes de vento te levam para o alto.");
            Banner(entry, L, Half / 1.7f * 2f, "Lago do Vento", "Chave 3 · Safira", 16f);

            // Cadeias: sobem 1 m por plataforma, vãos de ~2.5 m.
            var west = Group(p, "Cadeia Oeste");
            Plat(west, "W1", -12.5f, 2.5f, 18f);
            Plat(west, "W2", -18f, 3.5f, 11.5f);
            Plat(west, "W3", -22f, 4.5f, 5f);
            Plat(west, "W4", -22f, 5.5f, -1.5f);
            Wind(west, "VentoOeste", -22f, -6.5f);
            Plat(west, "W6 (alta)", -21.5f, 10f, -11f);   // alinhada com a coluna de vento: sair "em frente" cai nela
            Checkpoint(west, W(-18f, 4.5f, 11.5f), new Vector3(3f, 2f, 3f));
            Checkpoint(west, W(-21.5f, 11f, -11f), new Vector3(3f, 2f, 3f));
            Spawn(west, ranged, -21.5f, 10.5f, -12f);

            var east = Group(p, "Cadeia Leste");
            Plat(east, "E1", 12.5f, 2.5f, 18f);
            Plat(east, "E2", 18f, 3.5f, 11.5f);
            Plat(east, "E3", 22f, 4.5f, 5f);
            Plat(east, "E4", 22f, 5.5f, -1.5f);
            Wind(east, "VentoLeste", 22f, -6.5f);
            Plat(east, "E5 (alta)", 21.5f, 10f, -11f);
            Checkpoint(east, W(18f, 4.5f, 11.5f), new Vector3(3f, 2f, 3f));
            Checkpoint(east, W(21.5f, 11f, -11f), new Vector3(3f, 2f, 3f));
            Spawn(east, ranged, 21.5f, 10.5f, -12f);

            // Gosma que reaparece (cada tiro gasta tamanho: nunca falta munição).
            foreach (var g in new[] { new Vector3(6.5f, PierTop + 0.6f, 25.5f), new Vector3(-22f, 5.1f, 5f), new Vector3(22f, 5.1f, 5f),
                                      new Vector3(-18f, 4.1f, 11.5f), new Vector3(18f, 4.1f, 11.5f) })
            {
                var orb = Inst(goo, W(g.x, g.y, g.z));
                orb.transform.SetParent(p, true);
                SetFloat(orb.GetComponent<Pickup>(), "respawnSeconds", 15f);
            }

            // Sentinela no meio do lado sul, num pilar alto.
            Plat(p, "PilarSentinela", 0f, 7f, -20f, 3f, 3f);
            Spawn(p, ranged, 0f, 7.5f, -20f);

            // Cristais: azul (SO) → laranja (SE) → verde (NE) → rosa (NO).
            var crystals = Group(p, "Cristais");
            var blue = Crystal(crystals, "CristalAzul", -24f, -24f, Blue);
            var orange = Crystal(crystals, "CristalLaranja", 24f, -24f, Orange);
            var green = Crystal(crystals, "CristalVerde", 24f, 24f, Green);
            var pink = Crystal(crystals, "CristalRosa", -24f, 24f, Pink);
            var puzzleGo = new GameObject("SequenciaDoLago");
            puzzleGo.transform.SetParent(crystals, false);
            var puzzle = puzzleGo.AddComponent<CrystalSequencePuzzle>();
            var so = new SerializedObject(puzzle);
            var arr = so.FindProperty("order");
            arr.arraySize = 4;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = blue;
            arr.GetArrayElementAtIndex(1).objectReferenceValue = orange;
            arr.GetArrayElementAtIndex(2).objectReferenceValue = green;
            arr.GetArrayElementAtIndex(3).objectReferenceValue = pink;
            so.ApplyModifiedPropertiesWithoutUndo();

            Island(Group(p, "Ilha da Chave 3"), puzzle, puzzleGo, key);
            Decor(Group(p, "Decoracao"));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Slime Hunter] Lago (ProBuilder) construído em " + ScenePath);
        }

        // ---------- Água e margens ----------

        static void Water(Transform p)
        {
            // Superfície (só visual) + fundo com colisão (para nada cair para sempre) + volume de perigo.
            Cube(p, "Superficie (ProBuilder)", W(0f, 0.3f, 0f), new Vector3(Size, 0.05f, Size), water, false);
            Cube(p, "Fundo", W(0f, -1.25f, 0f), new Vector3(Size + 2f, 0.5f, Size + 2f), bed);
            var go = new GameObject("Agua (perigo)");
            go.transform.SetParent(p, false);
            go.transform.position = W(0f, WaterDepth / 2f, 0f);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(Size, WaterDepth, Size);
            var hazard = go.AddComponent<HazardZone>();
            var hso = new SerializedObject(hazard);
            hso.FindProperty("returnToCheckpoint").boolValue = true;
            hso.FindProperty("damage").floatValue = 10f;
            hso.FindProperty("message").stringValue = "Splash! O vento não te segurou... de volta ao último ponto seguro.";
            hso.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Margem de pedra em volta (visual) + paredes invisíveis altas (o vento sobe até 13 m). Aberta no norte para o píer.</summary>
        static void Shore(Transform p)
        {
            const float h = 24f;
            void Inv(string n, Vector3 local, Vector3 size)
            {
                var go = new GameObject(n);
                go.transform.SetParent(p, false);
                go.transform.position = L + local;
                go.AddComponent<BoxCollider>().size = size;
            }
            Inv("Limite_S", new Vector3(0f, h / 2f, -Half - 0.5f), new Vector3(Size + 2f, h, 1f));
            Inv("Limite_O", new Vector3(-Half - 0.5f, h / 2f, 0f), new Vector3(1f, h, Size + 2f));
            Inv("Limite_L", new Vector3(Half + 0.5f, h / 2f, 0f), new Vector3(1f, h, Size + 2f));
            // Norte: dois trechos deixando a boca do corredor (x local 0..6).
            Inv("Limite_N_O", new Vector3((-Half + 0f) / 2f, h / 2f, Half + 0.5f), new Vector3(Half, h, 1f));
            Inv("Limite_N_L", new Vector3((Half + 6f) / 2f, h / 2f, Half + 0.5f), new Vector3(Half - 6f, h, 1f));

            // Paredão de rocha em volta (sul, leste, oeste) e barranco no norte, dos dois lados da boca.
            void Cliff(Vector3 a, Vector3 b, float height)
            {
                Vector3 d = b - a;
                int n = Mathf.Max(1, Mathf.CeilToInt(d.magnitude / 6f));
                for (int i = 0; i < n; i++)
                {
                    float t = (i + 0.5f) / n;
                    float hh = height + ((i * 37) % 5) * 0.8f;
                    var c = Cube(p, "Rochedo", L + Vector3.Lerp(a, b, t) + Vector3.up * (hh / 2f - 1.5f), new Vector3(3f + (i % 3) * 0.5f, hh, d.magnitude / n + 0.6f), rockM);
                    c.transform.rotation = Quaternion.LookRotation(d.normalized);
                }
            }
            Cliff(new Vector3(-Half - 2f, 0f, -Half - 2f), new Vector3(Half + 2f, 0f, -Half - 2f), 9f);
            Cliff(new Vector3(-Half - 2f, 0f, -Half - 2f), new Vector3(-Half - 2f, 0f, Half + 2f), 9f);
            Cliff(new Vector3(Half + 2f, 0f, -Half - 2f), new Vector3(Half + 2f, 0f, Half + 2f), 9f);
            Cliff(new Vector3(-Half - 2f, 0f, Half + 2f), new Vector3(-2.5f, 0f, Half + 2f), 7f);
            Cliff(new Vector3(8.5f, 0f, Half + 2f), new Vector3(Half + 2f, 0f, Half + 2f), 7f);
        }

        // ---------- Entrada ----------

        static void Pier(Transform p)
        {
            // Deck de madeira de 10×6 (z local 21.5..27.5) encostado no patamar do corredor, com estacas.
            Cube(p, "Pier", W(3f, PierTop - 0.25f, 24.5f), new Vector3(10f, 0.5f, 6f), wood);
            for (int i = 0; i < 5; i++)
                Cube(p, "Tabua", W(-1.5f + i * 2.25f, PierTop + 0.01f, 24.5f), new Vector3(0.08f, 0.02f, 6f), Mat("M_FF_MadeiraEscura", new Color(0.3f, 0.2f, 0.13f)), false);
            foreach (var x in new[] { -1.6f, 7.6f })
                foreach (var z in new[] { 21.9f, 24.5f, 27.1f })
                {
                    var post = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, 0.22f, 3.2f, 0);
                    Setup(post, p, "Estaca (ProBuilder)", W(x, PierTop - 0.6f, z), Quaternion.identity, wood, false);
                }
        }

        static void Runes(Transform p)
        {
            Color[] order = { Blue, Orange, Green, Pink };
            string[] names = { "Azul", "Laranja", "Verde", "Rosa" };
            // Quem sobe a escada olha para o oeste (−X): a direita é +Z. Runas de z 21.5 (esq.) a 24.5 (dir.).
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < 4; i++)
            {
                Vector3 pos = W(-6f, 3.03f, 21.5f + i);
                Cube(p, "Runa" + (i + 1) + "_" + names[i], pos, new Vector3(0.8f, 0.06f, 0.8f), Mat("M_RunaLago_" + names[i], order[i]), false);
                var num = new GameObject("Numero" + (i + 1));
                num.transform.SetParent(p, false);
                num.transform.position = pos + Vector3.up * 0.9f;
                var tm = num.AddComponent<TextMesh>();
                tm.font = font;
                tm.text = (i + 1).ToString();
                tm.fontSize = 64;
                tm.characterSize = 0.08f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = order[i];
                num.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                num.AddComponent<Billboard>();
            }
            // Pedra das runas com cara de altar: pilarzinhos nos cantos.
            foreach (var c in new[] { new Vector2(-7.6f, 20.4f), new Vector2(-7.6f, 25.6f), new Vector2(-2.4f, 20.4f) })
            {
                var col = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, 0.3f, 1.4f, 0);
                Setup(col, p, "Pilarzinho (ProBuilder)", W(c.x, 3.7f, c.y), Quaternion.identity, stoneM, true);
            }
        }

        // ---------- Peças ----------

        /// <summary>Plataforma com o topo em y local; 1 m de espessura; pilar até a água só visual; musgo por cima.</summary>
        static ProBuilderMesh Plat(Transform p, string name, float x, float top, float z, float sx = 4f, float sz = 4f, Material mat = null)
        {
            var go = Cube(p, name, W(x, top - 0.5f, z), new Vector3(sx, 1f, sz), mat != null ? mat : plat);
            Cube(p, name + "_Musgo", W(x, top + 0.01f, z), new Vector3(sx * 0.7f, 0.02f, sz * 0.7f), mossM, false);
            if (top > 1.5f)
            {
                var pillar = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, Mathf.Min(sx, sz) * 0.22f, top - 0.5f, 0);
                Setup(pillar, p, name + "_Pilar (ProBuilder)", W(x, (top - 1f) / 2f - 0.25f, z), Quaternion.identity, stoneM, false);
            }
            return go;
        }

        /// <summary>Corrente de vento: coluna 3×3 da água até 13 m, com anéis (torus do ProBuilder) que sobem.</summary>
        static void Wind(Transform p, string name, float x, float z)
        {
            const float top = 13f;
            var go = new GameObject(name);
            go.transform.SetParent(p, false);
            go.transform.position = W(x, top / 2f, z);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(3f, top, 3f);
            var rings = new Transform[5];
            for (int i = 0; i < rings.Length; i++)
            {
                // PB 6.1: 'innerRadius' = raio total do anel, 'outerRadius' = espessura do tubo.
                var torus = ShapeGenerator.GenerateTorus(PivotLocation.Center, 6, 20, 1.3f, 0.06f, true, 360f, 360f);
                Setup(torus, go.transform, "AnelDeVento (ProBuilder)", W(x, i * top / rings.Length, z), Quaternion.identity, wind, false);
                torus.gameObject.isStatic = false;
                Bounds b = torus.GetComponent<MeshFilter>().sharedMesh.bounds;
                // Deita o anel: o eixo "fino" da malha vira o eixo vertical.
                Vector3 thin = b.size.x < b.size.y && b.size.x < b.size.z ? Vector3.right : (b.size.y < b.size.z ? Vector3.up : Vector3.forward);
                torus.transform.rotation = Quaternion.FromToRotation(thin, Vector3.up);
                rings[i] = torus.transform;
            }
            // Redemoinho na água marcando a base.
            var swirl = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 16, 1.6f, 0.04f, 0);
            Setup(swirl, p, "Redemoinho (ProBuilder)", W(x, 0.34f, z), Quaternion.identity, wind, false);
            var wc = go.AddComponent<WindCurrent>();
            var so = new SerializedObject(wc);
            var arr = so.FindProperty("rings");
            arr.arraySize = rings.Length;
            for (int i = 0; i < rings.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = rings[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            Hint(p, W(x, 7f, z + 4.5f), new Vector3(4f, 3f, 3f), "Entre na corrente de vento e espere subir!");
        }

        static ShootableCrystal Crystal(Transform p, string name, float x, float z, Color color)
        {
            const float pillarTop = 6f;
            var pillar = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, 0.65f, pillarTop + 1.5f, 0);
            Setup(pillar, p, name + "_Pilar (ProBuilder)", W(x, pillarTop / 2f - 0.75f, z), Quaternion.identity, stoneM, true);
            Cube(p, name + "_Capitel", W(x, pillarTop - 0.15f, z), new Vector3(1.6f, 0.3f, 1.6f), stoneM, false); // sem colisão: não rouba o tiro
            // Flutua 1.2 m acima do pilar; área de acerto maior que o visual (o tiro cai com a distância).
            var c = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 0.55f, 0);
            Setup(c, p, name, W(x, pillarTop + 1.2f, z), Quaternion.Euler(20f, 30f, 0f), Mat("M_Crystal", Color.white), false);
            c.gameObject.isStatic = false;
            c.transform.localScale = new Vector3(0.8f, 1.3f, 0.8f);
            var box = c.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(1.8f / 0.8f, 1.8f / 1.3f, 1.8f / 0.8f);
            var crystal = c.gameObject.AddComponent<ShootableCrystal>();
            SetColor(crystal, "color", color);
            return crystal;
        }

        static void Island(Transform p, CrystalSequencePuzzle puzzle, GameObject puzzleGo, GameObject key)
        {
            Plat(p, "Ilha", 3f, PierTop, -6f, 6f, 6f, mossM);
            // Ponte submersa (sobe da água ao resolver), do píer até a ilha.
            float bz0 = 24f - 2.5f, bz1 = -6f + 3f;
            var bridge = Cube(p, "PonteSubmersa", W(3f, -2.25f, (bz0 + bz1) / 2f), new Vector3(3f, 0.5f, bz0 - bz1 + 0.5f), stoneM);
            bridge.gameObject.isStatic = false;
            var door = bridge.gameObject.AddComponent<MechanismDoor>();
            SetVector3(door, "openOffset", new Vector3(0f, PierTop + 2f, 0f));
            SetFloat(door, "speed", 1.5f);
            UnityEventTools.AddPersistentListener(puzzle.OnSolved, door.Open);
            var msg = puzzleGo.AddComponent<EventHint>();
            SetString(msg, "text", "Sequência certa! Uma ponte surgiu da água até a ilha central.");
            UnityEventTools.AddPersistentListener(puzzle.OnSolved, msg.Show);
            EditorUtility.SetDirty(puzzle);

            // Santuário pequeno: pedestal + arco + arvorezinha.
            Cube(p, "Pedestal", W(3f, PierTop + 0.5f, -6f), new Vector3(1f, 1f, 1f), stoneM);
            var arch = ShapeGenerator.GenerateArch(PivotLocation.Center, 180f, 1.8f, 0.35f, 0.5f, 10, true, true, true, true, true);
            Setup(arch, p, "Arco da Ilha (ProBuilder)", W(3f, PierTop + 2.6f, -8.2f), Quaternion.identity, stoneM, false);
            foreach (float s in new[] { -1f, 1f })
            {
                var col = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, 0.22f, 2.6f, 0);
                Setup(col, p, "Coluna do Arco (ProBuilder)", W(3f + s * 1.8f, PierTop + 1.3f, -8.2f), Quaternion.identity, stoneM, true);
            }
            var keyGo = Inst(key, W(3f, PierTop + 1.6f, -6f));
            keyGo.name = "Key_chave_safira";
            keyGo.transform.SetParent(p, true);
            var kso = new SerializedObject(keyGo.GetComponent<Pickup>());
            kso.FindProperty("itemId").stringValue = "chave_safira";
            kso.FindProperty("displayName").stringValue = "Chave Safira";
            kso.ApplyModifiedPropertiesWithoutUndo();
            var kr = keyGo.GetComponent<Renderer>();
            kr.sharedMaterial = Mat("M_chave_safira", new Color(0.25f, 0.5f, 1f));
            PrefabUtility.RecordPrefabInstancePropertyModifications(kr);
            var light = new GameObject("LuzDaChave").AddComponent<Light>();
            light.transform.SetParent(p, false);
            light.transform.position = W(3f, PierTop + 3f, -6f);
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = 2f;
            light.color = new Color(0.4f, 0.6f, 1f);
        }

        static void Decor(Transform p)
        {
            // Juncos nas bordas e vitórias-régias na água (sem colisão).
            for (int i = 0; i < 28; i++)
            {
                float a = i * 37f * Mathf.Deg2Rad;
                float r = Half - 1.2f - (i % 3) * 0.6f;
                Vector3 pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (pos.z > 20f && pos.x > -3f && pos.x < 9f) continue; // não esconde o píer
                var stem = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 5, 0.06f, 1.6f + (i % 4) * 0.3f, 0);
                Setup(stem, p, "Junco (ProBuilder)", W(pos.x, 0.8f, pos.z), Quaternion.Euler((i % 5) * 4f, 0f, (i % 3) * 5f), reed, false);
            }
            for (int i = 0; i < 16; i++)
            {
                float x = ((i * 53) % 44) - 22f, z = ((i * 31) % 40) - 20f;
                if (Mathf.Abs(x - 3f) < 3f) continue; // fora do caminho da ponte
                var pad = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 10, 0.5f + (i % 3) * 0.2f, 0.03f, 0);
                Setup(pad, p, "Vitoria-Regia (ProBuilder)", W(x, 0.35f, z), Quaternion.identity, lily, false);
            }
            Sign(p, "Lago do Vento", W(3f, PierTop + 3f, 27f));
        }

        static void Spawn(Transform p, GameObject prefab, float x, float y, float z)
        {
            // Slimes do lago ficam parados: passeando, cairiam da plataforma.
            var go = Inst(prefab, W(x, y, z));
            go.transform.SetParent(p, true);
            SetFloat(go.GetComponent<SlimeHunter.Enemies.SlimeEnemy>(), "wanderRadius", 0f);
        }
    }
}
