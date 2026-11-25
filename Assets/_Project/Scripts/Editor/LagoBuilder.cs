using SlimeHunter.Interaction;
using SlimeHunter.Puzzles;
using SlimeHunter.World;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using static SlimeHunter.EditorTools.MapBuilder;
using static SlimeHunter.EditorTools.PlayerSetup;
using static SlimeHunter.EditorTools.TutorialBuilder;

namespace SlimeHunter.EditorTools
{
    /// <summary>
    /// Fase 6 do mapa: 7 Lago do Vento (Notion, Parte 2).
    ///   • Cair na água volta ao último checkpoint (com dano).
    ///   • Píer de entrada + pedra das runas (ordem: azul → laranja → verde → rosa).
    ///   • Duas cadeias de plataformas (oeste/leste) subindo até correntes de vento que levam a
    ///     plataformas altas, onde ficam slimes ranged.
    ///   • 4 cristais nos cantos do lago; é preciso chegar perto (o tiro cai com a distância).
    ///   • Resolver a sequência ergue uma ponte da água até a ilha central com a Chave 3 (Safira).
    /// Coordenadas locais: centro do lago, y = 0 na superfície da água, +Z norte (lado do hub).
    /// </summary>
    static class LagoBuilder
    {
        const float Half = Layout.LagoSize / 2f;   // 27.5
        const float WaterDepth = 0.7f;             // volume da água (perigo) acima do fundo
        const float PierTop = 1.5f;

        // Cores dos cristais (as mesmas das runas).
        static readonly Color Blue = new Color(0.25f, 0.5f, 1f);
        static readonly Color Orange = new Color(1f, 0.55f, 0.1f);
        static readonly Color Green = new Color(0.2f, 1f, 0.35f);
        static readonly Color Pink = new Color(1f, 0.45f, 0.8f);

        static Material platMat, stoneMat, windMat;
        static Vector3 L => Layout.Lago;
        static Vector3 W(float x, float y, float z) => L + new Vector3(x, y, z);

        public static void Build(Transform p)
        {
            platMat = Mat("M_PlataformaLago", new Color(0.5f, 0.48f, 0.58f));
            stoneMat = Mat("M_Templo", new Color(0.55f, 0.5f, 0.62f));
            windMat = Mat("M_Vento", new Color(0.75f, 0.95f, 1f));
            var ranged = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/RangedSlime.prefab");
            var key = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Key_Dourada.prefab");

            Water(p);
            Bounds(p);

            // Píer de entrada (o caminho do hub chega em z ≈ 26) e pedra das runas a oeste dele.
            var entry = Group(p, "Entrada");
            Plat(entry, "Pier", 3f, PierTop, 24f, 10f, 5f);
            Plat(entry, "PedraDasRunas", -5f, 3f, 23f, 6f, 6f);
            Path(entry, "RampaDasRunas", W(1.5f, PierTop, 23f), W(-2f, 3f, 23f), 2.5f, stoneMat);
            Color[] order = { Blue, Orange, Green, Pink };
            string[] names = { "Azul", "Laranja", "Verde", "Rosa" };
            // Quem sobe a rampa olha para o oeste (−X): a direita dele é +Z. As runas vão de z 21.5 (esquerda)
            // a 24.5 (direita) para serem lidas da esquerda para a direita, com o número em cima de cada uma.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < 4; i++)
            {
                Vector3 pos = W(-6f, 3.03f, 21.5f + i);
                var rune = Box("Runa" + (i + 1) + "_" + names[i], entry, pos, new Vector3(0.8f, 0.06f, 0.8f), Mat("M_RunaLago_" + names[i], order[i]));
                Object.DestroyImmediate(rune.GetComponent<Collider>());

                var num = new GameObject("Numero" + (i + 1));
                num.transform.SetParent(entry, false);
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
            Checkpoint(entry, W(3f, PierTop + 1f, 24f), new Vector3(8f, 2f, 4f));
            Hint(entry, W(3f, PierTop + 1.5f, 24.5f), new Vector3(9f, 3f, 3f),
                "Acerte os 4 cristais dos cantos na ordem das runas (na pedra ao lado). As correntes de vento te levam para o alto.");

            // Cadeias de plataformas: sobem 1 m por plataforma, vãos de ~2.5 m.
            var west = Group(p, "Cadeia Oeste");
            Plat(west, "W1", -12.5f, 2.5f, 18f);
            Plat(west, "W2", -18f, 3.5f, 11.5f);
            Plat(west, "W3", -22f, 4.5f, 5f);
            Plat(west, "W4", -22f, 5.5f, -1.5f);
            Wind(west, "VentoOeste", -22f, -6.5f);
            // Alinhada com a coluna de vento (x −22) e a 1 m dela: sair do vento "em frente" cai nela.
            Plat(west, "W6 (alta)", -21.5f, 10f, -11f);
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

            // Gosma para recuperar tamanho: aqui não há slimes para absorver no caminho e cada tiro gasta tamanho.
            var goo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/GooOrb.prefab");
            foreach (var g in new[] { new Vector3(6.5f, PierTop + 0.6f, 25.5f), new Vector3(-22f, 5.1f, 5f), new Vector3(22f, 5.1f, 5f),
                                      new Vector3(-18f, 4.1f, 11.5f), new Vector3(18f, 4.1f, 11.5f) })
            {
                var orb = Inst(goo, W(g.x, g.y, g.z));
                orb.transform.SetParent(p, true);
                SetFloat(orb.GetComponent<Pickup>(), "respawnSeconds", 15f); // cada tiro gasta tamanho: nunca falta munição
            }

            // Sentinela no meio do lado sul, num pilar alto: cobre as duas plataformas altas.
            Plat(p, "PilarSentinela", 0f, 7f, -20f, 3f, 3f);
            Spawn(p, ranged, 0f, 7.5f, -20f);

            // Cristais nos cantos. A ordem certa é azul (SO) → laranja (SE) → verde (NE) → rosa (NO).
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

            // Ponte submersa (sobe da água ao resolver) do píer até a ilha central com a chave.
            var island = Group(p, "Ilha da Chave 3");
            Plat(island, "Ilha", 3f, PierTop, -6f, 6f, 6f);
            float bz0 = 24f - 2.5f, bz1 = -6f + 3f;
            var bridge = Box("PonteSubmersa", island, W(3f, -2.25f, (bz0 + bz1) / 2f), new Vector3(3f, 0.5f, bz0 - bz1 + 0.5f), stoneMat);
            bridge.isStatic = false;
            var door = bridge.AddComponent<MechanismDoor>();
            SetVector3(door, "openOffset", new Vector3(0f, PierTop + 2f, 0f));
            SetFloat(door, "speed", 1.5f);
            UnityEventTools.AddPersistentListener(puzzle.OnSolved, door.Open);
            var msg = puzzleGo.AddComponent<EventHint>();
            SetString(msg, "text", "Sequência certa! Uma ponte surgiu da água até a ilha central.");
            UnityEventTools.AddPersistentListener(puzzle.OnSolved, msg.Show);
            EditorUtility.SetDirty(puzzle);

            Box("Pedestal", island, W(3f, PierTop + 0.5f, -6f), new Vector3(1f, 1f, 1f), stoneMat);
            var keyGo = Inst(key, W(3f, PierTop + 1.6f, -6f));
            keyGo.name = "Key_chave_safira";
            keyGo.transform.SetParent(island, true);
            var kso = new SerializedObject(keyGo.GetComponent<Pickup>());
            kso.FindProperty("itemId").stringValue = "chave_safira";
            kso.FindProperty("displayName").stringValue = "Chave Safira";
            kso.ApplyModifiedPropertiesWithoutUndo();
            var kr = keyGo.GetComponent<Renderer>();
            kr.sharedMaterial = Mat("M_chave_safira", new Color(0.25f, 0.5f, 1f));
            PrefabUtility.RecordPrefabInstancePropertyModifications(kr);
        }

        // ---------- Peças ----------

        /// <summary>Plataforma quadrada com o topo em y (local).</summary>
        static GameObject Plat(Transform p, string name, float x, float top, float z, float sx = 4f, float sz = 4f)
        {
            // Plataformas "flutuam": 1 m de espessura (o pilar até a água é só visual, sem colisão).
            var go = Box(name, p, W(x, top - 0.5f, z), new Vector3(sx, 1f, sz), platMat);
            if (top > 1.5f)
            {
                var pillar = Box(name + "_Pilar", p, W(x, (top - 1f) / 2f, z), new Vector3(sx * 0.35f, top - 1f, sz * 0.35f), stoneMat);
                Object.DestroyImmediate(pillar.GetComponent<Collider>());
            }
            return go;
        }

        static void Water(Transform p)
        {
            var go = new GameObject("Agua (perigo)");
            go.transform.SetParent(p, false);
            go.transform.position = W(0f, WaterDepth / 2f, 0f);
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(Layout.LagoSize, WaterDepth, Layout.LagoSize);
            var hazard = go.AddComponent<HazardZone>();
            var so = new SerializedObject(hazard);
            so.FindProperty("returnToCheckpoint").boolValue = true;
            so.FindProperty("damage").floatValue = 10f;
            so.FindProperty("message").stringValue = "Splash! O vento não te segurou... de volta ao último ponto seguro.";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Paredes invisíveis em volta do lago (aberto só no norte, onde chega o caminho).</summary>
        static void Bounds(Transform p)
        {
            const float h = 24f;
            void Inv(string n, Vector3 local, Vector3 size)
            {
                var go = new GameObject(n);
                go.transform.SetParent(p, false);
                go.transform.position = L + local;
                go.AddComponent<BoxCollider>().size = size;
            }
            Inv("Limite_S", new Vector3(0f, h / 2f, -Half - 0.5f), new Vector3(Layout.LagoSize + 2f, h, 1f));
            Inv("Limite_O", new Vector3(-Half - 0.5f, h / 2f, 0f), new Vector3(1f, h, Layout.LagoSize + 2f));
            Inv("Limite_L", new Vector3(Half + 0.5f, h / 2f, 0f), new Vector3(1f, h, Layout.LagoSize + 2f));
            // Norte: dois trechos, deixando o vão do caminho (x 0.5 a 5.5).
            Inv("Limite_N_O", new Vector3((-Half + 0.5f) / 2f, h / 2f, Half + 0.5f), new Vector3(Half + 0.5f, h, 1f));
            Inv("Limite_N_L", new Vector3((Half + 5.5f) / 2f, h / 2f, Half + 0.5f), new Vector3(Half - 5.5f, h, 1f));
        }

        /// <summary>Corrente de vento: coluna 3×3 da água até 13 m, com anéis visuais.</summary>
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
                var r = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                r.name = "AnelDeVento";
                Object.DestroyImmediate(r.GetComponent<Collider>());
                r.transform.SetParent(go.transform, false);
                r.transform.position = W(x, i * top / rings.Length, z);
                r.transform.localScale = new Vector3(2.6f, 0.04f, 2.6f);
                r.GetComponent<MeshRenderer>().sharedMaterial = windMat;
                rings[i] = r.transform;
            }
            var wind = go.AddComponent<WindCurrent>();
            var so = new SerializedObject(wind);
            var arr = so.FindProperty("rings");
            arr.arraySize = rings.Length;
            for (int i = 0; i < rings.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = rings[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            Hint(p, W(x, 7f, z + 4.5f), new Vector3(4f, 3f, 3f), "Entre na corrente de vento e espere subir!");
        }

        static ShootableCrystal Crystal(Transform p, string name, float x, float z, Color color)
        {
            const float pillarTop = 6f;
            Box(name + "_Pilar", p, W(x, pillarTop / 2f, z), new Vector3(1.2f, pillarTop, 1.2f), stoneMat);
            // Flutua 1.2 m acima do pilar e tem área de acerto maior que o visual: o tiro cai um pouco com a
            // distância e, encostado no pilar, era o pilar que levava o tiro (achado no teste completo).
            var c = Box(name, p, W(x, pillarTop + 1.2f, z), new Vector3(0.8f, 0.8f, 0.8f), Mat("M_Crystal", Color.white));
            c.isStatic = false;
            c.GetComponent<BoxCollider>().size = Vector3.one * 1.8f;
            c.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
            var crystal = c.AddComponent<ShootableCrystal>();
            SetColor(crystal, "color", color);
            return crystal;
        }

        static void Spawn(Transform p, GameObject prefab, float x, float y, float z)
        {
            Inst(prefab, W(x, y, z)).transform.SetParent(p, true);
        }
    }
}
