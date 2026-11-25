using SlimeHunter.Combat;
using SlimeHunter.Core;
using SlimeHunter.Enemies;
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
    /// Fase 3 do mapa:
    ///   4 Hub: borda (árvores + parede invisível com vãos nas saídas), placas apontando as áreas.
    ///   8 Portão das 3 Chaves: arco na base da árvore gigante (landmark); com as 3 chaves abre e liga o portal roxo.
    ///   9 Sala do Rei: arena fechada com o Rei Slime provisório (sem ataques ainda).
    /// </summary>
    static class HubBuilder
    {
        static readonly Color Green = new Color(0.2f, 1f, 0.35f);
        static readonly Color Red = new Color(1f, 0.2f, 0.2f);
        static readonly Color Blue = new Color(0.25f, 0.5f, 1f);
        static readonly Color Purple = new Color(0.65f, 0.3f, 1f);

        const float KingRoomH = 14f;

        // ---------- 4 Hub ----------

        public static void BuildHub(Transform hub)
        {
            var trunkMat = Mat("M_TroncoArvore", new Color(0.4f, 0.27f, 0.17f));
            var leafMat = Mat("M_Folhagem", new Color(0.3f, 0.62f, 0.32f));

            // Borda: parede invisível num círculo dentro do piso, com vãos onde saem os caminhos.
            const float ringR = 12.5f, gapDeg = 13f;
            var border = Group(hub, "Borda (invisivel)");
            for (float a = 0f; a < 360f; a += 10f)
            {
                float mid = a + 5f;
                if (NearExit(mid, gapDeg)) continue;
                Vector3 p0 = Polar(a, ringR), p1 = Polar(a + 10f, ringR);
                var seg = new GameObject("Borda");
                seg.transform.SetParent(border, false);
                seg.transform.position = (p0 + p1) * 0.5f + Vector3.up * 3f;
                seg.transform.rotation = Quaternion.LookRotation(p1 - p0);
                seg.AddComponent<BoxCollider>().size = new Vector3(0.5f, 6f, (p1 - p0).magnitude + 0.4f);
            }

            // Árvores na borda (fora dos vãos) para a borda parecer natural.
            var trees = Group(hub, "Arvores");
            for (float a = 0f; a < 360f; a += 24f)
            {
                if (NearExit(a, gapDeg + 6f)) continue;
                Tree(trees, Polar(a, 13.6f), 7f + (a % 48f) / 24f * 1.5f, 2.6f, trunkMat, leafMat);
            }

            // Placas nas saídas.
            Sign(hub, "Bosque das Raízes", HubExits.Bosque);
            Sign(hub, "Ruínas do Monólito", HubExits.Ruinas);
            Sign(hub, "Lago do Vento", HubExits.Lago);
            Sign(hub, "Portão das 3 Chaves", HubExits.Portao);

            Hint(hub, Layout.HubArrival + new Vector3(-2f, 1.5f, 0f), new Vector3(7f, 3f, 7f),
                "Ache as 3 chaves (Bosque, Ruínas e Lago) para abrir o portão da árvore gigante.");
        }

        static bool NearExit(float angleDeg, float gapDeg)
        {
            foreach (var e in HubExits.All)
            {
                float exitDeg = Mathf.Atan2(e.z, e.x) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(angleDeg, exitDeg)) < gapDeg) return true;
            }
            return false;
        }

        static Vector3 Polar(float deg, float r)
        {
            float a = deg * Mathf.Deg2Rad;
            return Layout.Hub + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        static void Tree(Transform p, Vector3 basePos, float height, float canopyR, Material trunk, Material leaves)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            t.name = "Arvore";
            t.transform.SetParent(p, false);
            t.transform.position = basePos + Vector3.up * (height / 2f);
            t.transform.localScale = new Vector3(0.8f, height / 2f, 0.8f);
            t.GetComponent<MeshRenderer>().sharedMaterial = trunk;
            t.isStatic = true;

            var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.name = "Copa";
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.transform.SetParent(p, false);
            c.transform.position = basePos + Vector3.up * (height + canopyR * 0.4f);
            c.transform.localScale = Vector3.one * canopyR * 2f;
            c.GetComponent<MeshRenderer>().sharedMaterial = leaves;
            c.isStatic = true;
        }

        static void Sign(Transform p, string text, Vector3 exitPoint)
        {
            var go = new GameObject("Placa " + text);
            go.transform.SetParent(p, false);
            go.transform.position = exitPoint + Vector3.up * 3.2f;
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

        // ---------- 8 Portão das 3 Chaves + árvore gigante ----------

        public static void BuildGate(Transform p, Transform kingArrival)
        {
            var stone = Mat("M_Templo", new Color(0.55f, 0.5f, 0.62f));
            Landmark(p);

            // Pivô do portão: o +Z local aponta para o hub.
            var pivot = new GameObject("Portao (pivo)").transform;
            pivot.SetParent(p, false);
            pivot.position = new Vector3(-24f, 0f, -24f);
            pivot.rotation = Quaternion.LookRotation((Layout.Hub - pivot.position).normalized);

            Local(Box("Pilar_E", pivot, Vector3.zero, new Vector3(1f, 7f, 1f), stone), pivot, new Vector3(-3.5f, 3.5f, 0f));
            Local(Box("Pilar_D", pivot, Vector3.zero, new Vector3(1f, 7f, 1f), stone), pivot, new Vector3(3.5f, 3.5f, 0f));
            Local(Box("Lintel", pivot, Vector3.zero, new Vector3(8f, 1.5f, 1.2f), stone), pivot, new Vector3(0f, 7.75f, 0f));
            // Recinto atrás do portão (só se entra pelo portão).
            Local(Box("Recinto_E", pivot, Vector3.zero, new Vector3(0.6f, 7f, 6f), stone), pivot, new Vector3(-3.5f, 3.5f, -3f));
            Local(Box("Recinto_D", pivot, Vector3.zero, new Vector3(0.6f, 7f, 6f), stone), pivot, new Vector3(3.5f, 3.5f, -3f));
            Local(Box("Recinto_Fundo", pivot, Vector3.zero, new Vector3(7.6f, 7f, 0.6f), stone), pivot, new Vector3(0f, 3.5f, -6f));
            Local(Box("Recinto_Teto", pivot, Vector3.zero, new Vector3(7.6f, 0.6f, 6.6f), stone), pivot, new Vector3(0f, 7.3f, -3f));

            // Portão com 3 encaixes.
            var gate = Box("Portao", pivot, Vector3.zero, new Vector3(6f, 6f, 0.8f), Mat("M_KeyGate", new Color(0.35f, 0.3f, 0.45f)));
            Local(gate, pivot, new Vector3(0f, 3f, 0f));
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
                var socket = Box($"Encaixe{i}", pivot, Vector3.zero, new Vector3(0.6f, 0.6f, 0.15f), socketMat);
                Local(socket, pivot, new Vector3(-1.5f + i * 1.5f, 4.4f, 0.48f));
                socket.isStatic = false;
                Object.DestroyImmediate(socket.GetComponent<Collider>());
                socket.transform.SetParent(gate.transform, true);

                var slot = slots.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("itemId").stringValue = keys[i].id;
                slot.FindPropertyRelative("displayName").stringValue = keys[i].name;
                slot.FindPropertyRelative("socket").objectReferenceValue = socket.GetComponent<Renderer>();
                slot.FindPropertyRelative("litColor").colorValue = keys[i].color;
            }
            so.FindProperty("openOffset").vector3Value = new Vector3(0f, -6.2f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Portal roxo dentro do recinto: liga quando o portão abre.
            var portal = PortalAt(pivot, Vector3.zero, kingArrival, false, "PortalRoxo", Purple,
                "O portão se abriu! Entre no portal roxo para enfrentar o Rei Slime.");
            portal.transform.SetParent(pivot, false);
            portal.transform.localPosition = new Vector3(0f, 0f, -3.5f);
            portal.transform.localRotation = Quaternion.identity;
            UnityEventTools.AddPersistentListener(keyGate.OnOpened, portal.Activate);
            EditorUtility.SetDirty(keyGate);
        }

        static void Local(GameObject go, Transform pivot, Vector3 localPos)
        {
            go.transform.SetParent(pivot, false);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = localPos;
        }

        /// <summary>Árvore gigante (templo do Rei): visível de todo o mapa para guiar o jogador.</summary>
        static void Landmark(Transform p)
        {
            var root = Group(p, "Arvore Gigante (landmark)");
            Vector3 c = Layout.ArvoreGigante;
            var bark = Mat("M_TroncoGigante", new Color(0.36f, 0.24f, 0.18f));
            var crown = Mat("M_CopaGigante", new Color(0.35f, 0.7f, 0.45f));
            var glow = Mat("M_FrutoBrilhante", new Color(1f, 0.85f, 0.35f));

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Tronco";
            trunk.transform.SetParent(root, false);
            trunk.transform.position = c + Vector3.up * 19f;     // de y = −6 até 44
            trunk.transform.localScale = new Vector3(9f, 25f, 9f);
            trunk.GetComponent<MeshRenderer>().sharedMaterial = bark;
            trunk.isStatic = true;

            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f + 15f;
                var rootBox = Box("Raiz", root, Vector3.zero, new Vector3(2.2f, 2.5f, 9f), bark);
                rootBox.transform.rotation = Quaternion.Euler(18f, a, 0f);
                rootBox.transform.position = c + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 6f + Vector3.up * 0.2f;
            }

            Vector3[] blobs = { new Vector3(0f, 50f, 0f), new Vector3(9f, 46f, 5f), new Vector3(-8f, 47f, -6f), new Vector3(4f, 55f, -7f) };
            float[] sizes = { 34f, 22f, 24f, 18f };
            for (int i = 0; i < blobs.Length; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = "Copa";
                Object.DestroyImmediate(s.GetComponent<Collider>());
                s.transform.SetParent(root, false);
                s.transform.position = c + blobs[i];
                s.transform.localScale = Vector3.one * sizes[i];
                s.GetComponent<MeshRenderer>().sharedMaterial = crown;
                s.isStatic = true;
            }
            // Frutos brilhantes: pontos de cor visíveis de longe.
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                f.name = "Fruto";
                Object.DestroyImmediate(f.GetComponent<Collider>());
                f.transform.SetParent(root, false);
                f.transform.position = c + new Vector3(Mathf.Cos(a) * 15f, 44f + (i % 3) * 3f, Mathf.Sin(a) * 15f);
                f.transform.localScale = Vector3.one * 2.2f;
                f.GetComponent<MeshRenderer>().sharedMaterial = glow;
            }
            var light = new GameObject("LuzDaArvore").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.position = c + Vector3.up * 40f;
            light.type = LightType.Point;
            light.range = 45f;
            light.intensity = 3f;
            light.color = new Color(1f, 0.85f, 0.5f);
        }

        // ---------- 9 Sala do Rei ----------

        /// <summary>Monta a sala do Rei e devolve o ponto de chegada do portal roxo.</summary>
        public static Transform BuildKingRoom(Transform root)
        {
            var p = Group(root, "9 Sala do Rei");
            Vector3 c = Layout.SalaDoRei;
            CaveRoom(p, c, Layout.SalaDoReiR, KingRoomH);
            RoomLights(p, c, new Vector3(-8f, 11f, 0f), new Vector3(8f, 11f, 0f), new Vector3(0f, 11f, 10f));

            var arrival = Marker(p, "ChegadaDoPortalRoxo", c + new Vector3(0f, 0.1f, -13f), Vector3.forward);
            var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disk.name = "PortalRoxo (chegada)";
            Object.DestroyImmediate(disk.GetComponent<Collider>());
            disk.transform.SetParent(p, false);
            disk.transform.position = c + new Vector3(0f, 1.6f, -15.5f);
            disk.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            disk.transform.localScale = new Vector3(2.6f, 0.08f, 2.6f);
            disk.GetComponent<MeshRenderer>().sharedMaterial = Mat("M_PortalRoxo", Purple);

            var king = KingSlime(p, c + new Vector3(0f, 0f, 6f));

            var group = new GameObject("Chefe").AddComponent<EnemyGroup>();
            group.transform.SetParent(p, false);
            var gso = new SerializedObject(group);
            var list = gso.FindProperty("enemies");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = king.GetComponent<Health>();
            gso.ApplyModifiedPropertiesWithoutUndo();
            var victory = group.gameObject.AddComponent<EventHint>();
            SetString(victory, "text", "O Rei Slime caiu! Fique em cima do corpo dele e segure E para absorvê-lo.");
            UnityEventTools.AddPersistentListener(group.OnCleared, victory.Show);
            EditorUtility.SetDirty(group);

            Banner(p, c, Layout.SalaDoReiR, "Sala do Rei Slime", "Chefe final");
            Hint(p, c + new Vector3(0f, 1.5f, -12f), new Vector3(10f, 3f, 4f),
                "(Provisório) O Rei ainda não ataca: os 3 ataques do GDD vêm na tarefa do chefe.");
            return arrival;
        }

        static GameObject KingSlime(Transform p, Vector3 pos)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            var root = new GameObject("ReiSlime (provisorio)");
            root.layer = enemyLayer;
            root.transform.SetParent(p, false);
            root.transform.position = pos + Vector3.up * 2.2f;
            var col = root.AddComponent<SphereCollider>();
            col.radius = 2.4f;

            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Corpo";
            body.layer = enemyLayer;
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(5.5f, 4.4f, 5.5f);
            body.GetComponent<MeshRenderer>().sharedMaterial = Mat("M_ReiSlime", new Color(0.35f, 0.8f, 0.35f));

            var gold = Mat("M_Coroa", new Color(1f, 0.8f, 0.2f));
            var crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crown.name = "Coroa";
            Object.DestroyImmediate(crown.GetComponent<Collider>());
            crown.transform.SetParent(body.transform, false);
            crown.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            crown.transform.localScale = new Vector3(0.38f, 0.06f, 0.38f);
            crown.GetComponent<MeshRenderer>().sharedMaterial = gold;
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                var spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spike.name = "Ponta";
                Object.DestroyImmediate(spike.GetComponent<Collider>());
                spike.transform.SetParent(body.transform, false);
                spike.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.15f, 0.58f, Mathf.Sin(a) * 0.15f);
                spike.transform.localRotation = Quaternion.Euler(0f, -i * 72f, 45f);
                spike.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                spike.GetComponent<MeshRenderer>().sharedMaterial = gold;
            }

            SetFloat(root.AddComponent<Health>(), "maxHealth", 400f);
            var placeholder = root.AddComponent<KingSlimePlaceholder>();
            SetRef(placeholder, "body", body.transform);

            var absorb = root.AddComponent<AbsorbableSlime>();
            var so = new SerializedObject(absorb);
            var off = so.FindProperty("disableOnDeath");
            off.arraySize = 1;
            off.GetArrayElementAtIndex(0).objectReferenceValue = placeholder;
            so.FindProperty("sizeReward").floatValue = 2f;
            so.FindProperty("absorbDuration").floatValue = 3f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }
    }
}
