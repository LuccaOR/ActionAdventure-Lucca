using SlimeHunter.Combat;
using SlimeHunter.Core;
using SlimeHunter.Interaction;
using SlimeHunter.Puzzles;
using SlimeHunter.World;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using static SlimeHunter.EditorTools.MapBuilder;
using static SlimeHunter.EditorTools.PlayerSetup;

namespace SlimeHunter.EditorTools
{
    /// <summary>
    /// Fase 2 do mapa: o tutorial na caverna (Notion, Parte 1).
    ///   1 Clareira do Despertar: saltinhos, pular tronco, raiz + pulo duplo, riacho de espinhos (pulo duplo + dash).
    ///   2 Câmara de Puzzles: cristal abre a grade, fenda estreita (encolher), gosma + placa de peso abrem a saída.
    ///   3 Arena de Combate: 2 slimes melee, plataformas para o Impacto; limpar a arena liga o portal azul para o hub.
    /// Coordenadas locais de cada sala: +Z = norte (entrada), −Z = sul (saída).
    /// </summary>
    static class TutorialBuilder
    {
        const float RoomH = 9f;          // altura das salas até o teto
        const float DoorW = 4f, DoorH = 4.2f;
        const float CorridorH = 4.6f;
        internal const int NorthEdge = 1, SouthEdge = 4; // índices das arestas retas do hexágono

        // Sala 1: terraços e riacho (z local)
        const float TerraceH = 3.6f;
        const float Terrace1Front = 4f, Terrace1Back = 0f;   // terraço 1: z 4 → 0
        // Riacho de 9.5 m (medido no teste com teclado virtual, saindo da borda):
        // pulo+duplo ≈ 6.5 m (cai), pulo+dash ≈ 8.5 m (cai), pulo+duplo+dash ≈ 10.8 m (passa).
        const float PitEnd = -9.5f;                          // riacho: z 0 → −9.5
        const float Terrace2End = -14.5f;                    // terraço 2: z −9 → parede sul

        static Material floorMat, rock, wood, spikeMat, barMat, crystalMat, portalMat;
        static GameObject gooOrb, meleePrefab;

        internal struct Door
        {
            public int Edge;
            public float Bottom;
            public Door(int edge, float bottom = 0f) { Edge = edge; Bottom = bottom; }
        }

        public static void Build(Transform root, Material caveFloor, Transform hubArrival)
        {
            InitKit(caveFloor);

            Clareira(Group(root, "1 Clareira do Despertar"));
            Camara(Group(root, "2 Camara de Puzzles"));
            Arena(Group(root, "3 Arena de Combate"), hubArrival);

            // Corredor 1→2 desce do terraço 2 (3.6 m) até o chão da câmara; o 2→3 é plano.
            Vector3 c1 = Layout.Clareira, c2 = Layout.Camara, c3 = Layout.Arena;
            Corridor(root, "Corredor 1-2", new Vector3(c1.x, TerraceH, c1.z - Apothem(Layout.ClareiraR) - 1f), EdgeNorth(c2, Layout.CamaraR, 2f));
            Corridor(root, "Corredor 2-3", EdgeSouth(c2, Layout.CamaraR, 2f), EdgeNorth(c3, Layout.ArenaR, 2f));

            // Nome da área ao entrar em cada sala.
            Banner(root, c1, Layout.ClareiraR, "Clareira do Despertar", "Tutorial · 1/3");
            Banner(root, c2, Layout.CamaraR, "Câmara de Puzzles", "Tutorial · 2/3");
            Banner(root, c3, Layout.ArenaR, "Arena de Combate", "Tutorial · 3/3");
        }

        /// <summary>Materiais e prefabs usados pelas peças (salas, portais...). Chamar antes de usá-las fora do tutorial.</summary>
        internal static void InitKit(Material caveFloor)
        {
            floorMat = caveFloor;
            rock = Mat("M_Caverna_Rocha", new Color(0.33f, 0.3f, 0.29f));
            wood = Mat("M_Madeira", new Color(0.45f, 0.3f, 0.18f));
            spikeMat = Mat("M_Espinho", new Color(0.62f, 0.6f, 0.66f));
            barMat = Mat("M_GrateBar", new Color(0.35f, 0.35f, 0.4f));
            crystalMat = Mat("M_Crystal", Color.white);
            portalMat = Mat("M_Portal", Color.white);
            gooOrb = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/GooOrb.prefab");
            meleePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/MeleeSlime.prefab");
        }

        /// <summary>Área (caixa) que mostra o nome do lugar ao entrar. Cobre um hexágono/quadrado de raio r.</summary>
        internal static void Banner(Transform p, Vector3 center, float r, string title, string subtitle, float height = 8f)
        {
            var go = Trigger(p, "Banner " + title, center + Vector3.up * (height / 2f), new Vector3(r * 1.7f, height, r * 1.7f));
            var banner = go.AddComponent<AreaBanner>();
            var so = new SerializedObject(banner);
            so.FindProperty("title").stringValue = title;
            so.FindProperty("subtitle").stringValue = subtitle;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- 1 Clareira do Despertar ----------

        static void Clareira(Transform p)
        {
            Vector3 c = Layout.Clareira;
            CaveRoom(p, c, Layout.ClareiraR, RoomH, new Door(SouthEdge, TerraceH));
            RoomLights(p, c, new Vector3(0f, 7f, 7f), new Vector3(0f, 8f, -11f));

            // Troncos caídos: aprender a pular (saltinho não passa, pulo passa).
            Log(p, c + new Vector3(4f, 0.5f, 8.5f), 30f);
            Log(p, c + new Vector3(-9f, 0.5f, 10.5f), -25f);

            // Raiz (1.2 m) e terraço (3.6 m): da raiz até o terraço só com pulo duplo.
            // 3.25 m de distância: dá tempo de chegar ao topo do pulo duplo antes da parede do terraço.
            Box("Raiz", p, c + new Vector3(-3f, 0.6f, 8.5f), new Vector3(2.5f, 1.2f, 2.5f), wood);
            Box("Terraco1", p, c + new Vector3(0f, TerraceH / 2f, (Terrace1Front + Terrace1Back) / 2f),
                new Vector3(34f, TerraceH, Terrace1Front - Terrace1Back), rock);
            Box("Terraco2", p, c + new Vector3(0f, TerraceH / 2f, (PitEnd + Terrace2End) / 2f),
                new Vector3(34f, TerraceH, PitEnd - Terrace2End), rock);

            // Riacho de espinhos entre os terraços.
            for (float x = -12f; x <= 12f; x += 1.5f)
                for (float z = Terrace1Back - 1f; z > PitEnd; z -= 1.5f)
                {
                    var s = Box("Espinho", p, c + new Vector3(x, 0.3f, z), new Vector3(0.45f, 0.45f, 0.45f), spikeMat);
                    s.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
                    Object.DestroyImmediate(s.GetComponent<Collider>());
                }
            var reset = Marker(p, "VoltaDoRiacho", c + new Vector3(0f, TerraceH + 0.1f, Terrace1Front - 1.5f), Vector3.back);
            var hazardGo = Trigger(p, "Riacho (perigo)", c + new Vector3(0f, 1.2f, (Terrace1Back + PitEnd) / 2f), new Vector3(34f, 2.4f, Terrace1Back - PitEnd));
            var hazard = hazardGo.AddComponent<HazardZone>();
            SetRef(hazard, "resetPoint", reset);
            SetString(hazard, "message", "Espinhos! Pule, pule de novo no ar e use o Dash (Shift).");

            Hint(p, c + new Vector3(0f, 1.5f, 11.5f), new Vector3(22f, 3f, 4f),
                "Você anda aos saltinhos (WASD). Espaço pula: pule os troncos!");
            Hint(p, c + new Vector3(0f, 1.5f, 7f), new Vector3(24f, 3f, 6f),
                "Suba na raiz e pule DUAS vezes (Espaço de novo no ar) para alcançar o terraço.");
            Hint(p, c + new Vector3(0f, TerraceH + 1.5f, (Terrace1Front + Terrace1Back) / 2f), new Vector3(28f, 3f, 4f),
                "Riacho de espinhos! Corra, pule, pule de novo e dê Dash (Shift) no ar.");

            Checkpoint(p, c + new Vector3(0f, TerraceH + 1f, (PitEnd + Terrace2End) / 2f), new Vector3(10f, 2f, 3f));
        }

        // ---------- 2 Câmara de Puzzles ----------

        static void Camara(Transform p)
        {
            Vector3 c = Layout.Camara;
            CaveRoom(p, c, Layout.CamaraR, RoomH, new Door(NorthEdge), new Door(SouthEdge));
            RoomLights(p, c, new Vector3(0f, 7f, 7f), new Vector3(0f, 7f, -6f));

            // Parede da grade (z +4) com a grade no meio, aberta por 1 cristal.
            const float gz = 4f, gateW = 3f, gateH = 3.6f;
            Box("ParedeGrade_E", p, c + new Vector3(-(gateW / 2f + 6.25f), RoomH / 2f, gz), new Vector3(12.5f, RoomH, 1f), rock);
            Box("ParedeGrade_D", p, c + new Vector3(gateW / 2f + 6.25f, RoomH / 2f, gz), new Vector3(12.5f, RoomH, 1f), rock);
            Box("ParedeGrade_Topo", p, c + new Vector3(0f, (gateH + RoomH) / 2f, gz), new Vector3(gateW, RoomH - gateH, 1f), rock);
            var grate = Grate(p, c + new Vector3(0f, 0f, gz), gateW, gateH);

            var crystalGo = Box("Cristal", p, c + new Vector3(3.5f, 5f, gz + 0.8f), new Vector3(0.6f, 0.6f, 0.6f), crystalMat);
            crystalGo.isStatic = false;
            crystalGo.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
            var crystal = crystalGo.AddComponent<ShootableCrystal>();
            SetColor(crystal, "color", new Color(0.3f, 0.9f, 1f));
            var puzzleGo = new GameObject("CristalDaGrade");
            puzzleGo.transform.SetParent(p, false);
            var puzzle = puzzleGo.AddComponent<CrystalSequencePuzzle>();
            var so = new SerializedObject(puzzle);
            var arr = so.FindProperty("order");
            arr.arraySize = 1;
            arr.GetArrayElementAtIndex(0).objectReferenceValue = crystal;
            so.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(puzzle.OnSolved, grate.Open);
            EditorUtility.SetDirty(puzzle);

            Hint(p, c + new Vector3(0f, 1.5f, 7.5f), new Vector3(16f, 3f, 6f),
                "Atire no cristal (clique). Cada tiro usa um pedaço do seu corpo: você encolhe!");

            // Parede da fenda (z −2 a −4) com um túnel de 1.4 x 1.0 m: só passa encolhido.
            const float fz = -3f, fendaW = 1.4f, fendaH = 1.0f;
            Box("ParedeFenda_E", p, c + new Vector3(-(fendaW / 2f + 7f), RoomH / 2f, fz), new Vector3(14f, RoomH, 2f), rock);
            Box("ParedeFenda_D", p, c + new Vector3(fendaW / 2f + 7f, RoomH / 2f, fz), new Vector3(14f, RoomH, 2f), rock);
            Box("ParedeFenda_Topo", p, c + new Vector3(0f, (fendaH + RoomH) / 2f, fz), new Vector3(fendaW, RoomH - fendaH, 2f), rock);
            var sizeHint = Trigger(p, "DicaDaFenda", c + new Vector3(0f, 1f, -0.5f), new Vector3(10f, 2f, 3f));
            SetFloat(sizeHint.AddComponent<SizeHintZone>(), "size", 1.4f);

            // Depois da fenda: gosma para crescer e a placa (tamanho 3) que abre a saída.
            Inst(gooOrb, c + new Vector3(-4f, 0.6f, -6f)).transform.SetParent(p, true);
            Inst(gooOrb, c + new Vector3(4f, 0.6f, -6f)).transform.SetParent(p, true);
            var exit = DoorInEdge(p, c, Layout.CamaraR, SouthEdge, "SaidaDaCamara");
            var plate = WeightPlateAt(p, c + new Vector3(0f, 0f, -8.3f), 3f);
            UnityEventTools.AddPersistentListener(plate.OnActivated, exit.Open);
            EditorUtility.SetDirty(plate);
            Hint(p, c + new Vector3(0f, 1.5f, -6f), new Vector3(16f, 3f, 3f),
                "Pegue a gosma para crescer de novo e pise na placa.");
            Checkpoint(p, c + new Vector3(0f, 1f, -6f), new Vector3(8f, 2f, 2f));
        }

        // ---------- 3 Arena de Combate ----------

        static void Arena(Transform p, Transform hubArrival)
        {
            Vector3 c = Layout.Arena;
            CaveRoom(p, c, Layout.ArenaR, RoomH, new Door(NorthEdge));
            RoomLights(p, c, new Vector3(-6f, 7.5f, 0f), new Vector3(6f, 7.5f, -4f));

            // Plataformas de 3 m: pulo duplo para subir, Impacto para descer em cima dos slimes.
            Box("Plataforma_E", p, c + new Vector3(-8f, 1.5f, 2f), new Vector3(5f, 3f, 5f), rock);
            Box("Plataforma_D", p, c + new Vector3(8f, 1.5f, 2f), new Vector3(5f, 3f, 5f), rock);

            var group = new GameObject("InimigosDaArena").AddComponent<EnemyGroup>();
            group.transform.SetParent(p, false);
            var e1 = Inst(meleePrefab, c + new Vector3(-5f, 0.5f, -5f));
            var e2 = Inst(meleePrefab, c + new Vector3(5f, 0.5f, -5f));
            e1.transform.SetParent(group.transform, true);
            e2.transform.SetParent(group.transform, true);
            var gso = new SerializedObject(group);
            var list = gso.FindProperty("enemies");
            list.arraySize = 2;
            list.GetArrayElementAtIndex(0).objectReferenceValue = e1.GetComponent<Health>();
            list.GetArrayElementAtIndex(1).objectReferenceValue = e2.GetComponent<Health>();
            gso.ApplyModifiedPropertiesWithoutUndo();

            // Portal azul para o hub: liga quando a arena estiver limpa.
            var portal = PortalAt(p, c + new Vector3(0f, 0f, -11f), hubArrival, false);
            UnityEventTools.AddPersistentListener(group.OnCleared, portal.Activate);
            EditorUtility.SetDirty(group);

            Hint(p, c + new Vector3(0f, 1.5f, 10f), new Vector3(16f, 3f, 5f),
                "Clique = tiro rápido · segure e solte = tiro carregado · no ar, Ctrl/C = Impacto");
            Checkpoint(p, c + new Vector3(0f, 1f, 10f), new Vector3(8f, 2f, 2f));
        }

        // ---------- Peças da caverna ----------

        /// <summary>Sala hexagonal fechada: piso, paredes nas 6 arestas (com portas) e teto.</summary>
        internal static void CaveRoom(Transform p, Vector3 c, float r, float height, params Door[] doors)
        {
            // Sem rótulo 3D aqui: TextMesh aparece através das paredes da caverna.
            // O nome da área vai para o banner de área (fase futura).
            HexFloor(p, c, r, floorMat, null);

            for (int e = 0; e < 6; e++)
            {
                float a0 = 60f * e * Mathf.Deg2Rad, a1 = 60f * (e + 1) * Mathf.Deg2Rad;
                Vector3 v0 = c + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * r;
                Vector3 v1 = c + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * r;
                Vector3 mid = (v0 + v1) * 0.5f;
                Vector3 outward = (mid - c); outward.y = 0f; outward.Normalize();
                Quaternion rot = Quaternion.LookRotation(outward);
                Vector3 basePos = mid + outward * 0.5f;
                float len = r + 1.2f; // cobre os cantos

                Door? door = null;
                foreach (var d in doors) if (d.Edge == e) door = d;
                if (door == null)
                {
                    Wall(p, basePos, rot, -len / 2f, len / 2f, 0f, height);
                    continue;
                }
                float bottom = door.Value.Bottom;
                Wall(p, basePos, rot, -len / 2f, -DoorW / 2f, 0f, height);
                Wall(p, basePos, rot, DoorW / 2f, len / 2f, 0f, height);
                if (bottom > 0f) Wall(p, basePos, rot, -DoorW / 2f, DoorW / 2f, 0f, bottom);
                Wall(p, basePos, rot, -DoorW / 2f, DoorW / 2f, bottom + DoorH, height);
            }

            // Teto: prisma hexagonal acima (a face de baixo fica em height).
            HexFloor(p, c + Vector3.up * (height + 1f), r + 1.5f, rock, null);
        }

        internal static void Wall(Transform p, Vector3 basePos, Quaternion rot, float x0, float x1, float y0, float y1)
        {
            var go = Box("Parede", p, Vector3.zero, new Vector3(x1 - x0, y1 - y0, 1f), rock);
            go.transform.rotation = rot;
            go.transform.position = basePos + rot * Vector3.right * ((x0 + x1) / 2f) + Vector3.up * (y0 + (y1 - y0) / 2f);
        }

        /// <summary>Corredor fechado (piso, paredes laterais e teto); inclina se as pontas tiverem alturas diferentes.</summary>
        internal static void Corridor(Transform p, string name, Vector3 from, Vector3 to)
        {
            Path(p, name, from, to, DoorW, floorMat);
            Vector3 dir = to - from;
            Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            Vector3 mid = (from + to) * 0.5f, right = rot * Vector3.right, up = rot * Vector3.up;
            float len = dir.magnitude;
            foreach (float side in new[] { -1f, 1f })
            {
                var w = Box(name + "_Parede", p, Vector3.zero, new Vector3(0.6f, CorridorH, len), rock);
                w.transform.rotation = rot;
                w.transform.position = mid + right * side * (DoorW / 2f + 0.3f) + up * (CorridorH / 2f);
            }
            var ceil = Box(name + "_Teto", p, Vector3.zero, new Vector3(DoorW + 1.2f, 0.5f, len), rock);
            ceil.transform.rotation = rot;
            ceil.transform.position = mid + up * (CorridorH + 0.25f);
        }

        internal static void RoomLights(Transform p, Vector3 c, params Vector3[] offsets)
        {
            foreach (var o in offsets)
            {
                var go = new GameObject("Luz");
                go.transform.SetParent(p, false);
                go.transform.position = c + o;
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 22f;
                l.intensity = 5f;
                l.color = new Color(1f, 0.83f, 0.6f);
            }
        }

        static void Log(Transform p, Vector3 pos, float yaw)
        {
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "Tronco";
            log.transform.SetParent(p, false);
            log.transform.position = pos;
            log.transform.rotation = Quaternion.Euler(0f, yaw, 90f);
            log.transform.localScale = new Vector3(1f, 3f, 1f);
            log.GetComponent<MeshRenderer>().sharedMaterial = wood;
            log.isStatic = true;
        }

        /// <summary>Grade de barras que desce quando Open() é chamado (MechanismDoor).</summary>
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
                var bar = Box("Barra", root.transform, Vector3.zero, new Vector3(0.1f, height, 0.1f), barMat);
                bar.isStatic = false;
                Object.DestroyImmediate(bar.GetComponent<Collider>());
                bar.transform.localPosition = new Vector3(x, height / 2f, 0f);
            }
            var door = root.AddComponent<MechanismDoor>();
            SetVector3(door, "openOffset", new Vector3(0f, -(height + 0.1f), 0f));
            return door;
        }

        /// <summary>Porta maciça no vão de uma aresta (aberta por mecanismo).</summary>
        internal static MechanismDoor DoorInEdge(Transform p, Vector3 c, float r, int edge, string name)
        {
            float a = (60f * edge + 30f) * Mathf.Deg2Rad;
            Vector3 outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Vector3 pos = c + outward * (Apothem(r) + 0.5f) + Vector3.up * (DoorH / 2f);
            var go = Box(name, p, pos, new Vector3(DoorW, DoorH, 0.8f), Mat("M_MechanismDoor", new Color(0.45f, 0.3f, 0.25f)));
            go.transform.rotation = Quaternion.LookRotation(outward);
            go.isStatic = false;
            var door = go.AddComponent<MechanismDoor>();
            SetVector3(door, "openOffset", new Vector3(0f, -(DoorH + 0.1f), 0f));
            return door;
        }

        internal static WeightPlate WeightPlateAt(Transform p, Vector3 pos, float required)
        {
            var root = new GameObject("PlacaDePeso");
            root.transform.SetParent(p, false);
            root.transform.position = pos;
            var zone = root.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 0.75f, 0f);
            zone.size = new Vector3(2.4f, 1.5f, 2.4f);
            var visual = Box("Placa", root.transform, Vector3.zero, new Vector3(2.4f, 0.15f, 2.4f), Mat("M_Plate", Color.white));
            visual.transform.localPosition = new Vector3(0f, 0.075f, 0f);
            visual.isStatic = false;
            var plate = root.AddComponent<WeightPlate>();
            SetRef(plate, "plate", visual.transform);
            SetFloat(plate, "requiredSize", required);
            return plate;
        }

        internal static Portal PortalAt(Transform p, Vector3 pos, Transform target, bool active)
            => PortalAt(p, pos, target, active, "PortalAzul", new Color(0.25f, 0.45f, 1f),
                        "A arena está limpa! Entre no portal azul.");

        /// <summary>Portal com cor e mensagem próprias. A frente do disco é o +Z local de 'p'... ver rotação no chamador.</summary>
        internal static Portal PortalAt(Transform p, Vector3 pos, Transform target, bool active, string name, Color color, string message)
        {
            var root = new GameObject(name);
            root.transform.SetParent(p, false);
            root.transform.position = pos;
            var zone = root.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 1.5f, 0f);
            zone.size = new Vector3(3f, 3f, 1.5f);

            var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disk.name = "Disco";
            Object.DestroyImmediate(disk.GetComponent<Collider>());
            disk.transform.SetParent(root.transform, false);
            disk.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            disk.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disk.transform.localScale = new Vector3(2.6f, 0.08f, 2.6f);
            disk.GetComponent<MeshRenderer>().sharedMaterial = portalMat;

            var portal = root.AddComponent<Portal>();
            SetRef(portal, "target", target);
            SetRef(portal, "visual", disk.GetComponent<Renderer>());
            var so = new SerializedObject(portal);
            so.FindProperty("startActive").boolValue = active;
            so.FindProperty("activatedMessage").stringValue = message;
            so.FindProperty("activeColor").colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
            return portal;
        }

        internal static void Hint(Transform p, Vector3 pos, Vector3 size, string text)
        {
            var go = Trigger(p, "Dica", pos, size);
            SetString(go.AddComponent<HintZone>(), "text", text);
        }

        internal static void Checkpoint(Transform p, Vector3 pos, Vector3 size)
        {
            var go = Trigger(p, "Checkpoint", pos, size);
            go.AddComponent<Checkpoint>();
            go.transform.rotation = Quaternion.LookRotation(Vector3.back);
        }

        internal static GameObject Trigger(Transform p, string name, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(p, false);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            return go;
        }

        internal static Transform Marker(Transform p, string name, Vector3 pos, Vector3 facing)
        {
            var t = new GameObject(name).transform;
            t.SetParent(p, false);
            t.position = pos;
            t.rotation = Quaternion.LookRotation(facing);
            return t;
        }

        internal static void SetString(Object o, string field, string value)
        {
            var so = new SerializedObject(o);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetColor(Object o, string field, Color value)
        {
            var so = new SerializedObject(o);
            so.FindProperty(field).colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetVector3(Object o, string field, Vector3 value)
        {
            var so = new SerializedObject(o);
            so.FindProperty(field).vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
