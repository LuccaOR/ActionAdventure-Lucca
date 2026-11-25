using System.Collections.Generic;
using SlimeHunter.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static SlimeHunter.EditorTools.PlayerSetup;

namespace SlimeHunter.EditorTools
{
    /// <summary>
    /// Gera a cena do mapa principal (Floresta.unity) seguindo o mapa do Notion.
    /// Menu: Slime Hunter > Construir Mapa. Rodar de novo recria a cena do zero.
    ///
    /// Fase 1 (atual): cena, iluminação, pisos de cada área e caminhos entre elas (greybox).
    /// Escala: 1 quadrado do desenho = 5 m. Numeração das áreas segue a imagem do Notion:
    ///   1-3 tutorial (caverna) · 4 hub · 5 bosque · 6 ruínas · 7 lago · 8 portão · 9 sala do Rei
    /// </summary>
    public static class MapBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Floresta.unity";
        const string MeshDir = "Assets/_Project/Meshes";
        const string HexMeshPath = MeshDir + "/HexPrism.asset";

        /// <summary>Posições (centro, altura do piso) e tamanhos de cada área. Usado pelas próximas fases.</summary>
        public static class Layout
        {
            // Tutorial: caverna isolada, ligada ao hub pelo portal azul.
            public static readonly Vector3 Clareira = new Vector3(200f, 0f, 236f);   // 1, hex r=16 (cabe o riacho de 9 m)
            public static readonly Vector3 Camara = new Vector3(200f, 0f, 200f);     // 2, hex r=12.5
            public static readonly Vector3 Arena = new Vector3(200f, 0f, 166f);      // 3, hex r=15
            public const float ClareiraR = 16f, CamaraR = 12.5f, ArenaR = 15f;

            // Floresta semiaberta.
            public static readonly Vector3 Hub = Vector3.zero;                        // 4, hex r=15
            public static readonly Vector3 Bosque = new Vector3(-55f, 4f, 45f);       // 5, 35x35, 4 m acima
            public static readonly Vector3 Ruinas = new Vector3(55f, 0f, -5f);        // 6, sala 15x15
            public static readonly Vector3 RuinasCamara = new Vector3(72f, -4f, -5f); // 6, câmara em cruz, 4 m abaixo
            public static readonly Vector3 Lago = new Vector3(-5f, -3f, -60f);        // 7, 55x55, 3 m abaixo
            public static readonly Vector3 Portao = new Vector3(-22f, 0f, -22f);      // 8, plataforma do portão (14x14)
            public const float PortaoSize = 14f;
            /// <summary>Landmark: árvore gigante (templo do Rei) logo atrás do portão, visível de todo o mapa.</summary>
            public static readonly Vector3 ArvoreGigante = new Vector3(-34f, 0f, -34f);
            public const float HubR = 15f, BosqueSize = 35f, RuinasSize = 15f, LagoSize = 55f;

            // Sala do Rei: isolada, ligada ao portão pelo portal roxo.
            public static readonly Vector3 SalaDoRei = new Vector3(-200f, 0f, -200f); // 9, hex r=20
            public const float SalaDoReiR = 20f;

            public static readonly Vector3 PlayerSpawn = Clareira + new Vector3(0f, 0.1f, 11f);
            /// <summary>Onde o portal azul do tutorial deixa o jogador no hub.</summary>
            public static readonly Vector3 HubArrival = new Vector3(6f, 0.1f, 3f);
        }

        static Material caveMat, hubMat, forestMat, ruinsMat, lakeMat, gateMat, kingMat, pathMat;

        [MenuItem("Slime Hunter/Construir Mapa")]
        public static void Build()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    Debug.LogError("[Slime Hunter] Salve a cena aberta antes de construir o mapa.");
                    return;
                }
            }

            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Player.prefab");
            if (player == null) { BuildPrefabs(); player = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Player.prefab"); }

            CreateMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var defaultCam = GameObject.FindWithTag("MainCamera");
            if (defaultCam != null) Object.DestroyImmediate(defaultCam);
            SetupLighting();

            // Ordem: primeiro os destinos dos portais (sala do Rei, hub), depois quem aponta para eles.
            var map = new GameObject("Mapa").transform;
            TutorialBuilder.InitKit(kingMat);
            Transform kingArrival = HubBuilder.BuildKingRoom(Group(map, "Sala do Rei"));
            TutorialBuilder.InitKit(caveMat);
            Transform hubArrival = BuildForest(Group(map, "Floresta"), kingArrival);
            TutorialBuilder.Build(Group(map, "Tutorial (caverna)"), caveMat, hubArrival);

            var p = Inst(player, Layout.PlayerSpawn);
            p.name = "Player";
            p.transform.rotation = Quaternion.LookRotation(Vector3.back);

            EditorSceneManager.SaveScene(scene, ScenePath);
            PutFirstInBuild(ScenePath);
            Debug.Log("[Slime Hunter] Mapa (fases 1-6) salvo em " + ScenePath);
        }

        // ---------- Áreas ----------

        /// <summary>Monta a floresta e devolve o ponto de chegada do portal azul no hub.</summary>
        static Transform BuildForest(Transform root, Transform kingArrival)
        {
            var hub = Group(root, "4 Hub Central");
            HexFloor(hub, Layout.Hub, Layout.HubR, hubMat, null);

            // Portal azul de chegada (só visual: a ida é feita pelo portal da arena do tutorial).
            var arrival = new GameObject("ChegadaDoTutorial").transform;
            arrival.SetParent(hub, false);
            arrival.position = Layout.HubArrival;
            arrival.rotation = Quaternion.LookRotation(new Vector3(-1f, 0f, -0.3f));
            var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disk.name = "PortalAzul (chegada)";
            Object.DestroyImmediate(disk.GetComponent<Collider>());
            disk.transform.SetParent(hub, false);
            disk.transform.position = Layout.HubArrival + new Vector3(1.5f, 1.6f, 0.45f);
            disk.transform.rotation = Quaternion.LookRotation(new Vector3(-1f, 0f, -0.3f)) * Quaternion.Euler(90f, 0f, 0f);
            disk.transform.localScale = new Vector3(2.6f, 0.08f, 2.6f);
            disk.GetComponent<MeshRenderer>().sharedMaterial = Mat("M_PortalAzul", new Color(0.25f, 0.45f, 1f));
            var bosque = Group(root, "5 Bosque das Raizes Retorcidas");
            SquareFloor(bosque, Layout.Bosque, Layout.BosqueSize, Layout.BosqueSize, forestMat, null);
            BosqueBuilder.Build(bosque);

            var ruinas = Group(root, "6 Ruinas do Monolito");
            SquareFloor(ruinas, Layout.Ruinas, Layout.RuinasSize, Layout.RuinasSize, ruinsMat, null);
            // Câmara subterrânea em cruz (a escadaria é revelada pela placa gigante).
            SquareFloor(ruinas, Layout.RuinasCamara, 15f, 5f, ruinsMat, null);
            SquareFloor(ruinas, Layout.RuinasCamara, 5f, 15f, ruinsMat, null);
            RuinasBuilder.Build(ruinas);

            var lago = Group(root, "7 Lago do Vento");
            SquareFloor(lago, Layout.Lago, Layout.LagoSize, Layout.LagoSize, lakeMat, null);
            LagoBuilder.Build(lago);
            var portao = Group(root, "8 Portao das 3 Chaves");
            SquareFloor(portao, Layout.Portao, Layout.PortaoSize, Layout.PortaoSize, gateMat, null);

            // Caminhos a partir do hub (rampas onde há desnível: bosque sobe 4 m, lago desce 3 m),
            // com corrimão invisível dos lados para não cair da ponte.
            var paths = Group(root, "Caminhos");
            PathWithRails(paths, "Caminho Hub-Bosque (sobe)", HubExits.Bosque, new Vector3(-40f, Layout.Bosque.y, 30f), 5f);
            PathWithRails(paths, "Caminho Hub-Ruinas", HubExits.Ruinas, new Vector3(Layout.Ruinas.x - 6f, 0f, Layout.Ruinas.z), 5f);
            PathWithRails(paths, "Caminho Hub-Lago (desce)", HubExits.Lago, new Vector3(-2f, Layout.Lago.y + 1.5f, -34f), 5f); // chega no píer
            PathWithRails(paths, "Caminho Hub-Portao", HubExits.Portao, new Vector3(Layout.Portao.x + 2f, 0f, Layout.Portao.z + 2f), 4f);

            HubBuilder.BuildHub(hub);
            HubBuilder.BuildGate(portao, kingArrival);

            // Rede de segurança: cair da floresta volta ao último checkpoint (com um pouco de dano), sem perder vida.
            var net = new GameObject("RedeDeSeguranca");
            net.transform.SetParent(root, false);
            net.transform.position = new Vector3(0f, -12f, -15f);
            var netCol = net.AddComponent<BoxCollider>();
            netCol.isTrigger = true;
            netCol.size = new Vector3(240f, 4f, 220f);
            var hazard = net.AddComponent<HazardZone>();
            var hso = new SerializedObject(hazard);
            hso.FindProperty("returnToCheckpoint").boolValue = true;
            hso.FindProperty("damage").floatValue = 10f;
            hso.FindProperty("message").stringValue = "Você caiu! De volta ao último ponto seguro.";
            hso.ApplyModifiedPropertiesWithoutUndo();

            // Nome de cada área ao entrar.
            TutorialBuilder.Banner(hub, Layout.Hub, Layout.HubR, "Hub Central", "Floresta Semiaberta");
            TutorialBuilder.Banner(root, Layout.Bosque, Layout.BosqueSize / 1.7f, "Bosque das Raízes Retorcidas", "Chave 1: Esmeralda");
            TutorialBuilder.Banner(ruinas, Layout.Ruinas, Layout.RuinasSize / 1.7f, "Ruínas do Monólito", "Chave 2: Rubi");
            TutorialBuilder.Banner(root, Layout.Lago, Layout.LagoSize / 1.7f, "Lago do Vento", "Chave 3: Safira");
            TutorialBuilder.Banner(portao, Layout.Portao, Layout.PortaoSize / 1.7f, "Portão das 3 Chaves", "Arena do Rei Slime");
            return arrival;
        }

        /// <summary>Pontos onde cada caminho sai do hub (dentro do piso). Usados também pelos vãos da borda do hub.</summary>
        internal static class HubExits
        {
            public static readonly Vector3 Bosque = new Vector3(-6f, 0f, 11f);
            public static readonly Vector3 Ruinas = new Vector3(13f, 0f, -2f);
            public static readonly Vector3 Lago = new Vector3(2f, 0f, -12f);
            public static readonly Vector3 Portao = new Vector3(-8f, 0f, -9f);
            public static readonly Vector3[] All = { Bosque, Ruinas, Lago, Portao };
        }

        static void PathWithRails(Transform p, string name, Vector3 from, Vector3 to, float width)
        {
            Path(p, name, from, to, width, pathMat);
            Vector3 dir = to - from;
            Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            Vector3 mid = (from + to) * 0.5f, right = rot * Vector3.right, up = rot * Vector3.up;
            // Corrimão desde a ponta no hub (fecha o vão entre a borda do hub e o caminho).
            float len = dir.magnitude;
            Vector3 railMid = mid;
            foreach (float side in new[] { -1f, 1f })
            {
                var rail = new GameObject(name + " Corrimao");
                rail.transform.SetParent(p, false);
                rail.transform.rotation = rot;
                rail.transform.position = railMid + right * side * (width / 2f + 0.25f) + up * 1.5f;
                var col = rail.AddComponent<BoxCollider>();
                col.size = new Vector3(0.5f, 3f, len);
            }
        }

        // ---------- Peças ----------

        internal static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        /// <summary>Piso hexagonal (lados retos ao norte/sul) com o topo na altura de center.y.</summary>
        internal static void HexFloor(Transform parent, Vector3 center, float radius, Material mat, string label)
        {
            var go = new GameObject("Piso");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(radius, 1f, radius);
            go.AddComponent<MeshFilter>().sharedMesh = HexMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshCollider>().sharedMesh = HexMesh();
            go.isStatic = true;
            if (label != null) Label(parent, label, center + Vector3.up * 7f);
        }

        internal static void SquareFloor(Transform parent, Vector3 center, float sizeX, float sizeZ, Material mat, string label)
        {
            var floor = Box("Piso", parent, center + Vector3.down * 0.5f, new Vector3(sizeX, 1f, sizeZ), mat);
            floor.isStatic = true;
            if (label != null) Label(parent, label, center + Vector3.up * 7f);
        }

        /// <summary>Caminho reto (vira rampa se as pontas tiverem alturas diferentes); o topo liga os dois pontos.</summary>
        internal static void Path(Transform parent, string name, Vector3 from, Vector3 to, float width, Material mat)
        {
            const float thickness = 0.5f;
            Vector3 dir = to - from;
            var go = Box(name, parent, Vector3.zero, new Vector3(width, thickness, dir.magnitude), mat);
            go.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            go.transform.position = (from + to) * 0.5f - go.transform.up * (thickness * 0.5f);
            go.isStatic = true;
        }

        // Pontos nas faces retas (norte/sul) de um hexágono, avançando 'inset' para dentro do piso.
        internal static float Apothem(float r) => r * 0.8660254f;
        internal static Vector3 EdgeNorth(Vector3 c, float r, float inset) => c + new Vector3(0f, 0f, Apothem(r) - inset);
        internal static Vector3 EdgeSouth(Vector3 c, float r, float inset) => c - new Vector3(0f, 0f, Apothem(r) - inset);

        internal static void Label(Transform parent, string text, Vector3 pos)
        {
            var go = new GameObject("Rotulo");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 1f, 1f, 0.9f);
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            go.AddComponent<Billboard>();
        }

        // ---------- Malha hexagonal ----------

        /// <summary>Prisma hexagonal de raio 1, topo em y=0 e base em y=-1 (escalar em X/Z pelo raio).</summary>
        internal static Mesh HexMesh()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(HexMeshPath);
            if (existing != null) return existing;

            var verts = new List<Vector3>();
            var tris = new List<int>();
            Vector3 Corner(int i, float y)
            {
                float a = Mathf.Deg2Rad * 60f * i;
                return new Vector3(Mathf.Cos(a), y, Mathf.Sin(a));
            }
            void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                // Frente = cross(b-a, c-a) apontando para fora (sentido horário visto de fora, como o Unity usa).
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) { var t = b; b = c; c = t; }
                int s = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
            }

            for (int i = 0; i < 6; i++)
            {
                int j = (i + 1) % 6;
                Vector3 t0 = Corner(i, 0f), t1 = Corner(j, 0f), b0 = Corner(i, -1f), b1 = Corner(j, -1f);
                Tri(new Vector3(0f, 0f, 0f), t0, t1, Vector3.up);         // topo
                Tri(new Vector3(0f, -1f, 0f), b0, b1, Vector3.down);      // base
                Vector3 side = ((t0 + t1) * 0.5f); side.y = 0f;           // lateral
                Tri(t0, t1, b1, side);
                Tri(t0, b1, b0, side);
            }

            var mesh = new Mesh { name = "HexPrism" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            var uvs = new List<Vector2>();
            foreach (var v in verts) uvs.Add(new Vector2(v.x, v.z));
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (!AssetDatabase.IsValidFolder(MeshDir)) AssetDatabase.CreateFolder("Assets/_Project", "Meshes");
            AssetDatabase.CreateAsset(mesh, HexMeshPath);
            return mesh;
        }

        // ---------- Cena ----------

        static void CreateMaterials()
        {
            // Paleta por bioma (greybox: cores só para diferenciar as áreas).
            caveMat = Mat("M_Map_Caverna", new Color(0.42f, 0.38f, 0.36f));
            hubMat = Mat("M_Map_Hub", new Color(0.55f, 0.72f, 0.42f));
            forestMat = Mat("M_Map_Bosque", new Color(0.3f, 0.55f, 0.3f));
            ruinsMat = Mat("M_Map_Ruinas", new Color(0.7f, 0.65f, 0.55f));
            lakeMat = Mat("M_Map_Lago", new Color(0.35f, 0.6f, 0.85f));
            gateMat = Mat("M_Map_Portao", new Color(0.45f, 0.35f, 0.6f));
            kingMat = Mat("M_Map_SalaDoRei", new Color(0.55f, 0.3f, 0.55f));
            pathMat = Mat("M_Map_Caminho", new Color(0.62f, 0.52f, 0.38f));
        }

        static void SetupLighting()
        {
            var sun = Object.FindAnyObjectByType<Light>();
            if (sun != null)
            {
                sun.name = "Sol";
                sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                sun.color = new Color(1f, 0.94f, 0.82f);
                sun.intensity = 1.25f;
                sun.shadows = LightShadows.Soft;
            }

            // Névoa colorida de distância (direção de arte) e ambiente em degradê.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.84f, 0.82f);
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 220f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.62f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.28f, 0.25f);
        }

        static void PutFirstInBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
