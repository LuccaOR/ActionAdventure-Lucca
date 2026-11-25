using SlimeHunter.Interaction;
using SlimeHunter.Puzzles;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using static SlimeHunter.EditorTools.PlayerSetup;

namespace SlimeHunter.EditorTools
{
    /// <summary>
    /// Monta a "trilha de puzzles" da cena de teste (ao sul), uma estação por puzzle do GDD:
    ///   A Passagem estreita → Chave Esmeralda   B Placa de peso → Chave Rubi
    ///   C Cristais em sequência + elevador → Chave Safira   D Portão das 3 Chaves   E Grade atravessável
    /// Chamado por PlayerSetup.BuildTestScene().
    /// </summary>
    static class PuzzleSetup
    {
        const float Z = -30f;        // linha das paredes frontais
        const float Half = 5f;       // meia largura de cada estação
        const float RoomDepth = 6f;

        static Material wallMat;

        static readonly Color Green = new Color(0.2f, 1f, 0.35f);
        static readonly Color Red = new Color(1f, 0.2f, 0.2f);
        static readonly Color Blue = new Color(0.25f, 0.5f, 1f);
        static readonly Color Yellow = new Color(1f, 0.85f, 0.1f);

        public static void BuildPuzzleLane()
        {
            var root = new GameObject("PuzzleLane").transform;
            wallMat = Mat("M_PuzzleWall", new Color(0.5f, 0.47f, 0.55f));
            var gooOrb = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/GooOrb.prefab");
            var key = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Key_Dourada.prefab");

            NarrowPassage(Station(root, "A_PassagemEstreita"), -28f, gooOrb, key);
            WeightPlateStation(Station(root, "B_PlacaDePeso"), -14f, gooOrb, key);
            CrystalStation(Station(root, "C_CristaisEmSequencia"), 0f, key);
            KeyGateStation(Station(root, "D_PortaoDas3Chaves"), 14f);
            GrateStation(Station(root, "E_GradeAtravessavel"), 28f, gooOrb);
        }

        static Transform Station(Transform root, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            return t;
        }

        // ---------- A: Passagem estreita ----------

        static void NarrowPassage(Transform p, float x, GameObject gooOrb, GameObject key)
        {
            // Túnel de 1 m de altura e 3 m de profundidade: só passa encolhido (tamanho ~1.4 ou menos).
            const float depth = 3f;
            WallWithOpening(p, x, 1.4f, 1.0f, depth, 4f);
            RoomBehind(p, x, depth, 4f);
            Sign(p, "PASSAGEM ESTREITA\nfique pequeno para passar", new Vector3(x, 3.2f, Z + depth / 2f + 0.05f));

            var hint = new GameObject("SizeHint");
            hint.transform.SetParent(p, false);
            hint.transform.position = new Vector3(x, 1f, Z + depth / 2f + 1.5f);
            var trigger = hint.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(4f, 2f, 3f);
            var zone = hint.AddComponent<SizeHintZone>();
            SetFloat(zone, "size", 1.4f);

            // Do outro lado: gosma para crescer de volta e a primeira chave.
            Inst(gooOrb, new Vector3(x - 2f, 0.6f, Z - 4f)).transform.SetParent(p, true);
            Inst(gooOrb, new Vector3(x + 2f, 0.6f, Z - 4f)).transform.SetParent(p, true);
            Key(p, key, new Vector3(x, 0.9f, Z - 6f), "chave_esmeralda", "Chave Esmeralda", Green);
        }

        // ---------- B: Placa de peso ----------

        static void WeightPlateStation(Transform p, float x, GameObject gooOrb, GameObject key)
        {
            const float depth = 1f;
            WallWithOpening(p, x, 3f, 3.6f, depth, 4f);
            RoomBehind(p, x, depth, 4f);
            Sign(p, "PLACA DE PESO\ncresça para ficar pesado", new Vector3(x, 3.75f, Z + depth / 2f + 0.05f));

            var door = Box("PortaDaPlaca", p, new Vector3(x, 1.8f, Z), new Vector3(3f, 3.6f, 0.8f), Mat("M_MechanismDoor", new Color(0.45f, 0.3f, 0.25f)));
            door.isStatic = false;
            var mechanism = door.AddComponent<MechanismDoor>();

            var plateRoot = new GameObject("PlacaDePeso");
            plateRoot.transform.SetParent(p, false);
            plateRoot.transform.position = new Vector3(x, 0f, Z + 5f);
            var zone = plateRoot.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 0.75f, 0f);
            zone.size = new Vector3(2.4f, 1.5f, 2.4f);
            var plateVisual = Box("Placa", plateRoot.transform, Vector3.zero, new Vector3(2.4f, 0.15f, 2.4f), Mat("M_Plate", Color.white));
            plateVisual.transform.localPosition = new Vector3(0f, 0.075f, 0f);
            plateVisual.isStatic = false;

            var plate = plateRoot.AddComponent<WeightPlate>();
            SetRef(plate, "plate", plateVisual.transform);
            SetFloat(plate, "requiredSize", 4f);
            UnityEventTools.AddPersistentListener(plate.OnActivated, mechanism.Open);
            EditorUtility.SetDirty(plate);

            // Gosma por perto para crescer até 4 (começa com 3).
            Inst(gooOrb, new Vector3(x - 3.5f, 0.6f, Z + 8f)).transform.SetParent(p, true);
            Inst(gooOrb, new Vector3(x + 3.5f, 0.6f, Z + 8f)).transform.SetParent(p, true);
            Key(p, key, new Vector3(x, 0.9f, Z - 4f), "chave_rubi", "Chave Rubi", Red);
        }

        // ---------- C: Cristais em sequência + elevador ----------

        static void CrystalStation(Transform p, float x, GameObject key)
        {
            Box("Fundo", p, new Vector3(x, 2f, Z), new Vector3(Half * 2f, 4f, 1f), wallMat);
            Sign(p, "TIRO EM SEQUENCIA\nsiga as runas no chao", new Vector3(x - 1.5f, 3.2f, Z + 0.55f));

            // Cristais da esquerda para a direita: vermelho, amarelo, azul, verde.
            var crystalMat = Mat("M_Crystal", Color.white);
            var pillarMat = Mat("M_Pillar", new Color(0.4f, 0.38f, 0.45f));
            float[] xs = { -4.5f, -2.5f, -0.5f, 1.5f };
            float[] heights = { 1.0f, 2.0f, 1.4f, 2.4f };
            Color[] colors = { Red, Yellow, Blue, Green };
            var crystals = new ShootableCrystal[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 basePos = new Vector3(x + xs[i], 0f, Z + 2.5f);
                Box($"Pilar{i}", p, basePos + Vector3.up * (heights[i] / 2f), new Vector3(0.4f, heights[i], 0.4f), pillarMat);
                var c = Box($"Cristal{i}", p, basePos + Vector3.up * (heights[i] + 0.45f), new Vector3(0.45f, 0.45f, 0.45f), crystalMat);
                c.isStatic = false;
                c.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
                crystals[i] = c.AddComponent<ShootableCrystal>();
                var so = new SerializedObject(crystals[i]);
                so.FindProperty("color").colorValue = colors[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Ordem certa: azul, vermelho, verde, amarelo — mostrada pelas runas no chão.
            int[] solution = { 2, 0, 3, 1 };
            for (int i = 0; i < solution.Length; i++)
            {
                var rune = Box($"Runa{i + 1}", p, new Vector3(x - 2.25f + i * 1.5f, 0.03f, Z + 7.5f), new Vector3(0.9f, 0.06f, 0.9f),
                    Mat("M_Rune_" + solution[i], colors[solution[i]]));
                Object.DestroyImmediate(rune.GetComponent<Collider>());
            }

            var puzzleGo = new GameObject("CristaisPuzzle");
            puzzleGo.transform.SetParent(p, false);
            var puzzle = puzzleGo.AddComponent<CrystalSequencePuzzle>();
            var pso = new SerializedObject(puzzle);
            var arr = pso.FindProperty("order");
            arr.arraySize = solution.Length;
            for (int i = 0; i < solution.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = crystals[solution[i]];
            pso.ApplyModifiedPropertiesWithoutUndo();

            // Resolver liga o elevador, que leva até a plataforma alta com a chave.
            var elevatorGo = Box("Elevador", p, new Vector3(x + 3.5f, 0.25f, Z + 4.5f), new Vector3(2.5f, 0.5f, 2.5f), Mat("M_Elevator", new Color(0.3f, 0.6f, 0.65f)));
            elevatorGo.isStatic = false;
            var elevator = elevatorGo.AddComponent<Elevator>();
            Box("PlataformaAlta", p, new Vector3(x + 3.5f, 5.75f, Z + 1.5f), new Vector3(3f, 0.5f, 3f), wallMat);
            UnityEventTools.AddPersistentListener(puzzle.OnSolved, elevator.Activate);
            EditorUtility.SetDirty(puzzle);

            Key(p, key, new Vector3(x + 3.5f, 6.7f, Z + 1.5f), "chave_safira", "Chave Safira", Blue);
        }

        // ---------- D: Portão das 3 Chaves ----------

        static void KeyGateStation(Transform p, float x)
        {
            const float depth = 1f, height = 6f;
            WallWithOpening(p, x, 4f, 4.4f, depth, height);
            RoomBehind(p, x, depth, height);
            // (Placas TextMesh aparecem através das paredes: só usamos nas faces externas.)
            Sign(p, "PORTAO DAS 3 CHAVES\nArena do Rei Slime", new Vector3(x, 5.2f, Z + depth / 2f + 0.05f));

            var gate = Box("Portao", p, new Vector3(x, 2.2f, Z), new Vector3(4f, 4.4f, 0.6f), Mat("M_KeyGate", new Color(0.35f, 0.3f, 0.45f)));
            gate.isStatic = false;
            var keyGate = gate.AddComponent<KeyGate>();

            var socketMat = Mat("M_Socket", new Color(0.15f, 0.15f, 0.18f));
            (string id, string name, Color color)[] keys =
            {
                ("chave_esmeralda", "Chave Esmeralda", Green),
                ("chave_rubi", "Chave Rubi", Red),
                ("chave_safira", "Chave Safira", Blue),
            };
            var so = new SerializedObject(keyGate);
            var slots = so.FindProperty("slots");
            slots.arraySize = keys.Length;
            for (int i = 0; i < keys.Length; i++)
            {
                var socket = Box($"Encaixe{i}", p, new Vector3(x - 1.2f + i * 1.2f, 3.4f, Z + 0.36f), new Vector3(0.45f, 0.45f, 0.12f), socketMat);
                socket.isStatic = false;
                Object.DestroyImmediate(socket.GetComponent<Collider>());
                socket.transform.SetParent(gate.transform, true);

                var slot = slots.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("itemId").stringValue = keys[i].id;
                slot.FindPropertyRelative("displayName").stringValue = keys[i].name;
                slot.FindPropertyRelative("socket").objectReferenceValue = socket.GetComponent<Renderer>();
                slot.FindPropertyRelative("litColor").colorValue = keys[i].color;
            }
            so.FindProperty("openOffset").vector3Value = new Vector3(0f, -4.5f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- E: Grade atravessável ----------

        static void GrateStation(Transform p, float x, GameObject gooOrb)
        {
            const float depth = 4f, openW = 2.4f, openH = 2.5f;
            WallWithOpening(p, x, openW, openH, depth, 4f);
            RoomBehind(p, x, depth, 4f);
            Sign(p, "GRADE ATRAVESSAVEL\nDash quando abrir!", new Vector3(x, 3.25f, Z + depth / 2f + 0.05f));

            var grateGo = new GameObject("GradeAtravessavel");
            grateGo.transform.SetParent(p, false);
            grateGo.transform.position = new Vector3(x, 0f, Z);
            var blocker = grateGo.AddComponent<BoxCollider>();
            blocker.center = new Vector3(0f, openH / 2f, 0f);
            blocker.size = new Vector3(openW, openH, depth);

            var barMat = Mat("M_GrateBar", new Color(0.35f, 0.35f, 0.4f));
            var bars = new System.Collections.Generic.List<Transform>();
            foreach (float zEdge in new[] { depth / 2f, -depth / 2f })
            {
                for (int i = 0; i < 5; i++)
                {
                    var bar = Box("Barra", grateGo.transform, Vector3.zero, new Vector3(0.12f, openH, 0.12f), barMat);
                    bar.isStatic = false;
                    Object.DestroyImmediate(bar.GetComponent<Collider>());
                    bar.transform.localPosition = new Vector3(-1f + i * 0.5f, openH / 2f, zEdge);
                    bars.Add(bar.transform);
                }
            }
            var grate = grateGo.AddComponent<TimedGrate>();
            var so = new SerializedObject(grate);
            var arr = so.FindProperty("bars");
            arr.arraySize = bars.Count;
            for (int i = 0; i < bars.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = bars[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            for (int i = -1; i <= 1; i++)
                Inst(gooOrb, new Vector3(x + i * 2.5f, 0.6f, Z - depth / 2f - 3f)).transform.SetParent(p, true);
        }

        // ---------- Peças ----------

        /// <summary>Parede frontal da estação com uma abertura (porta/túnel) no meio.</summary>
        static void WallWithOpening(Transform p, float x, float openW, float openH, float depth, float height)
        {
            float sideW = Half - openW / 2f;
            Box("Parede_E", p, new Vector3(x - openW / 2f - sideW / 2f, height / 2f, Z), new Vector3(sideW, height, depth), wallMat);
            Box("Parede_D", p, new Vector3(x + openW / 2f + sideW / 2f, height / 2f, Z), new Vector3(sideW, height, depth), wallMat);
            if (openH < height)
                Box("Parede_Topo", p, new Vector3(x, openH + (height - openH) / 2f, Z), new Vector3(openW, height - openH, depth), wallMat);
        }

        /// <summary>Sala fechada atrás da parede (onde fica a recompensa).</summary>
        static void RoomBehind(Transform p, float x, float frontDepth, float height)
        {
            float zFront = Z - frontDepth / 2f;
            Box("Sala_E", p, new Vector3(x - Half + 0.25f, height / 2f, zFront - RoomDepth / 2f), new Vector3(0.5f, height, RoomDepth), wallMat);
            Box("Sala_D", p, new Vector3(x + Half - 0.25f, height / 2f, zFront - RoomDepth / 2f), new Vector3(0.5f, height, RoomDepth), wallMat);
            Box("Sala_Fundo", p, new Vector3(x, height / 2f, zFront - RoomDepth + 0.25f), new Vector3(Half * 2f, height, 0.5f), wallMat);
        }

        static void Key(Transform p, GameObject keyPrefab, Vector3 pos, string id, string name, Color color)
        {
            var go = Inst(keyPrefab, pos);
            go.name = "Key_" + id;
            go.transform.SetParent(p, true);
            var so = new SerializedObject(go.GetComponent<Pickup>());
            so.FindProperty("itemId").stringValue = id;
            so.FindProperty("displayName").stringValue = name;
            so.ApplyModifiedPropertiesWithoutUndo();
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Mat("M_" + id, color);
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }

        /// <summary>Placa de texto legível de quem chega pelo norte.</summary>
        static void Sign(Transform p, string text, Vector3 pos)
        {
            var go = new GameObject("Placa_" + text.Split('\n')[0]);
            go.transform.SetParent(p, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = 0.07f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
    }
}
