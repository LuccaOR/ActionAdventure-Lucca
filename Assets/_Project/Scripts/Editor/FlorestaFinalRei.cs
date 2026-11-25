using System.Linq;
using SlimeHunter.Combat;
using SlimeHunter.Core;
using SlimeHunter.Enemies;
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
    /// "floresta final" (ProBuilder) — 9 Sala do Rei Slime (Notion, Parte 2): o "Salão das Raízes",
    /// uma arena redonda escavada 40 m abaixo da árvore gigante. Chega-se pelo portal roxo do Portão das 3 Chaves.
    ///   • Rei Slime PROVISÓRIO (400 de vida, sem ataques: a IA com os 3 ataques do GDD vem depois);
    ///     ao morrer vira poça e o jogador absorve segurando E.
    ///   • Derrotado o Rei, liga um portal de volta ao hub.
    /// Menu: Slime Hunter > Floresta Final > Construir Sala do Rei.
    /// </summary>
    public static class FlorestaFinalRei
    {
        const string ScenePath = "Assets/_Project/Scenes/floresta final.unity";
        const string RootName = "Sala do Rei (ProBuilder)";
        public static readonly Vector3 C = new Vector3(-34f, -40f, -5f);   // embaixo da árvore gigante
        const float Radius = 20f;
        const float Height = 14f;
        const string ArrivalName = "ChegadaDoPortalRoxo";
        static readonly Color Purple = new Color(0.65f, 0.3f, 1f);

        static Material floorM, wallM, root, glow, gold, kingM, throne, portalPurple, portalBlue;

        [MenuItem("Slime Hunter/Floresta Final/Construir Sala do Rei")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError("[Slime Hunter] Abra a cena 'floresta final' antes de construir a Sala do Rei.");
                return;
            }
            foreach (var go in scene.GetRootGameObjects().Where(g => g.name == RootName)) Object.DestroyImmediate(go);

            floorM = Mat("M_FF_PisoRaizes", new Color(0.3f, 0.24f, 0.2f));
            wallM = Mat("M_FF_TerraEscura", new Color(0.25f, 0.2f, 0.18f));
            root = Mat("M_FF_Raiz", new Color(0.36f, 0.24f, 0.16f));
            glow = Mat("M_FF_SeivaDourada", new Color(1f, 0.82f, 0.35f));
            gold = Mat("M_FF_Dourado", new Color(1f, 0.82f, 0.3f));
            kingM = Mat("M_ReiSlime", new Color(0.35f, 0.8f, 0.35f));
            throne = Mat("M_FF_Templo", new Color(0.58f, 0.54f, 0.64f));
            portalPurple = Mat("M_FF_PortalRoxo", Purple);
            portalBlue = Mat("M_FF_PortalAzul", new Color(0.3f, 0.5f, 1f));

            var p = new GameObject(RootName).transform;
            Arena(Group(p, "Arena"));
            Roots(Group(p, "Raizes"));
            Throne(Group(p, "Trono"));
            var arrival = Arrival(Group(p, "Chegada"));
            var king = KingSlime(Group(p, "Chefe"), C + new Vector3(0f, 0f, 6f));
            Gameplay(Group(p, "Gameplay"), king, arrival);

            LinkPortals();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Slime Hunter] Sala do Rei (ProBuilder) construída em " + ScenePath);
        }

        /// <summary>Liga o portal roxo do hub à chegada da sala (e o portal de volta à chegada do hub).</summary>
        public static void LinkPortals()
        {
            var arrival = GameObject.Find(ArrivalName);
            var hubArrival = GameObject.Find("PontoDeChegada");
            // A arena fica em y −40, abaixo do killHeight padrão do jogador (−30). Nesta cena as quedas já são
            // pegas pela rede de segurança (y −14) e pela água do lago, então baixa o limite só na instância da cena.
            var player = Object.FindAnyObjectByType<Player.PlayerController>();
            if (player != null && arrival != null)
            {
                SetFloat(player, "killHeight", C.y - 20f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            }
            foreach (var portal in Object.FindObjectsByType<Portal>(FindObjectsInactive.Include))
            {
                if (portal.name == "PortalRoxo" && arrival != null)
                {
                    SetRef(portal, "target", arrival.transform);
                    SetString(portal, "activatedMessage", "O portão se abriu! Entre no portal roxo para enfrentar o Rei Slime.");
                }
                else if ((portal.name == "PortalDeVolta" || portal.name == "PortalAzul") && hubArrival != null) // volta do Rei e saída do tutorial
                    SetRef(portal, "target", hubArrival.transform);
            }
        }

        // ---------- Arena ----------

        static void Arena(Transform p)
        {
            // Piso redondo (cilindro de 24 lados) + anel de pedra no centro.
            var floor = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 24, Radius + 1f, 1f, 0);
            Setup(floor, p, "Piso (ProBuilder)", C + Vector3.down * 0.5f, Quaternion.identity, floorM, true);
            var ring = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 24, 9f, 0.06f, 0);
            Setup(ring, p, "Circulo (ProBuilder)", C + Vector3.up * 0.03f, Quaternion.identity, throne, false);
            var inner = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 24, 8.4f, 0.08f, 0);
            Setup(inner, p, "Circulo Interno (ProBuilder)", C + Vector3.up * 0.04f, Quaternion.identity, floorM, false);

            // Parede de terra em 24 blocos inclinados para dentro + teto.
            const int n = 24;
            for (int i = 0; i < n; i++)
            {
                float a = (i + 0.5f) * 360f / n;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                float w = 2f * Mathf.PI * (Radius + 1.5f) / n + 0.6f;
                var block = Cube(p, "Parede", C + dir * (Radius + 1.5f) + Vector3.up * (Height / 2f), new Vector3(w, Height + 1f, 3f), wallM);
                block.transform.rotation = Quaternion.LookRotation(-dir) * Quaternion.Euler(-4f - (i % 3) * 2f, 0f, 0f);
            }
            var ceiling = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 24, Radius + 3f, 1f, 0);
            Setup(ceiling, p, "Teto (ProBuilder)", C + Vector3.up * (Height + 0.5f), Quaternion.identity, wallM, true);

            // Luz: seiva dourada escorrendo das raízes (pontos de luz quentes) + luz roxa do portal.
            foreach (var o in new[] { new Vector3(-10f, 10f, 0f), new Vector3(10f, 10f, 0f), new Vector3(0f, 10f, 10f), new Vector3(0f, 10f, -10f), new Vector3(0f, 12f, 0f) })
            {
                var l = new GameObject("Luz").AddComponent<Light>();
                l.transform.SetParent(p, false);
                l.transform.position = C + o;
                l.type = LightType.Point;
                l.range = 22f;
                l.intensity = 2.2f;
                l.color = new Color(1f, 0.8f, 0.5f);
            }
        }

        static void Roots(Transform p)
        {
            // Raiz-mãe descendo do teto no centro (não chega ao chão) + raízes penduradas e outras saindo das paredes.
            var mother = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 12, 3.5f, 4f, 0);
            Setup(mother, p, "Raiz Mae (ProBuilder)", C + Vector3.up * (Height - 1.5f), Quaternion.identity, root, false);
            var tip = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 2.6f, 1);
            Setup(tip, p, "Ponta da Raiz (ProBuilder)", C + Vector3.up * (Height - 3.6f), Quaternion.identity, root, false);
            tip.transform.localScale = new Vector3(1f, 0.7f, 1f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f + 10f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                float r = 6f + (i % 3) * 4f;
                float len = 3f + (i % 4) * 1.2f;
                var hang = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, 0.35f + (i % 2) * 0.2f, len, 0);
                Setup(hang, p, "Raiz Pendurada (ProBuilder)", C + dir * r + Vector3.up * (Height - len / 2f), Quaternion.Euler((i % 3 - 1) * 8f, a, 0f), root, false);
                // Gota de seiva brilhando na ponta.
                var drop = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 0.25f, 0);
                Setup(drop, p, "Seiva (ProBuilder)", C + dir * r + Vector3.up * (Height - len - 0.2f), Quaternion.identity, glow, false);
            }
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f + 5f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                // Sobe do chão encostada na parede e entra nela lá em cima (inclinada para fora).
                float len = 10f + (i % 3) * 2f;
                Vector3 axis = (Vector3.up * 4f + dir).normalized;
                var wallRoot = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 8, 0.6f + (i % 2) * 0.25f, len, 0);
                Setup(wallRoot, p, "Raiz da Parede (ProBuilder)", C + dir * (Radius - 1.6f) + axis * (len / 2f) - Vector3.up * 0.3f,
                      Quaternion.FromToRotation(Vector3.up, axis), root, false);
            }
        }

        static void Throne(Transform p)
        {
            // Trono de pedra atrás do Rei (norte), com degraus e coroa gigante esculpida.
            Vector3 t = C + new Vector3(0f, 0f, 15f);
            Cube(p, "Degrau", t + new Vector3(0f, 0.25f, 0f), new Vector3(10f, 0.5f, 5f), throne);
            Cube(p, "Degrau", t + new Vector3(0f, 0.75f, 1f), new Vector3(8f, 0.5f, 3f), throne);
            Cube(p, "Encosto", t + new Vector3(0f, 4f, 2.2f), new Vector3(7f, 7f, 1f), throne);
            var arch = ShapeGenerator.GenerateArch(PivotLocation.Center, 180f, 3.5f, 0.6f, 1f, 12, true, true, true, true, true);
            Setup(arch, p, "Arco do Trono (ProBuilder)", t + new Vector3(0f, 7.5f, 2.2f), Quaternion.identity, gold, false);
            for (int i = 0; i < 5; i++)
            {
                var spike = Cube(p, "Ponta da Coroa", t + new Vector3(-3f + i * 1.5f, 8.2f + (i == 2 ? 0.6f : 0f), 2.2f), new Vector3(0.5f, 1.4f, 0.5f), gold, false);
                spike.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            }
        }

        static Transform Arrival(Transform p)
        {
            var arrival = Marker(p, ArrivalName, C + new Vector3(0f, 0.1f, -13f), Vector3.forward);
            Ring(p, "PortalRoxo chegada (ProBuilder)", C + new Vector3(0f, 1.8f, -17.5f), Vector3.forward, portalPurple);
            return arrival;
        }

        // ---------- Rei Slime (provisório) ----------

        static GameObject KingSlime(Transform p, Vector3 pos)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            var go = new GameObject("ReiSlime (provisorio)");
            go.layer = enemyLayer;
            go.transform.SetParent(p, false);
            go.transform.position = pos + Vector3.up * 2.2f;
            go.AddComponent<SphereCollider>().radius = 2.4f;

            // Corpo: icosaedro do ProBuilder (raio 0.5) na escala do corpo antigo.
            var body = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 0.5f, 2);
            body.gameObject.name = "Corpo (ProBuilder)";
            body.gameObject.layer = enemyLayer;
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(5.5f, 4.4f, 5.5f);
            body.GetComponent<MeshRenderer>().sharedMaterial = kingM;

            var crown = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 10, 0.19f, 0.12f, 0);
            crown.gameObject.name = "Coroa (ProBuilder)";
            crown.transform.SetParent(body.transform, false);
            crown.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            crown.GetComponent<MeshRenderer>().sharedMaterial = gold;
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                var spike = ShapeGenerator.GenerateCube(PivotLocation.Center, Vector3.one * 0.05f);
                spike.gameObject.name = "Ponta";
                spike.transform.SetParent(body.transform, false);
                spike.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.15f, 0.58f, Mathf.Sin(a) * 0.15f);
                spike.transform.localRotation = Quaternion.Euler(0f, -i * 72f, 45f);
                spike.GetComponent<MeshRenderer>().sharedMaterial = gold;
            }
            // Olhos.
            foreach (float s in new[] { -1f, 1f })
            {
                var eye = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, 0.05f, 1);
                eye.gameObject.name = "Olho";
                eye.transform.SetParent(body.transform, false);
                eye.transform.localPosition = new Vector3(s * 0.14f, 0.12f, 0.46f); // +Z local = frente do Rei
                eye.GetComponent<MeshRenderer>().sharedMaterial = Mat("M_Olho", new Color(0.05f, 0.05f, 0.08f));
            }

            SetFloat(go.AddComponent<Health>(), "maxHealth", 400f);
            var placeholder = go.AddComponent<KingSlimePlaceholder>();
            SetRef(placeholder, "body", body.transform);
            var absorb = go.AddComponent<AbsorbableSlime>();
            var so = new SerializedObject(absorb);
            var off = so.FindProperty("disableOnDeath");
            off.arraySize = 1;
            off.GetArrayElementAtIndex(0).objectReferenceValue = placeholder;
            so.FindProperty("sizeReward").floatValue = 2f;
            so.FindProperty("absorbDuration").floatValue = 3f;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.transform.rotation = Quaternion.LookRotation(Vector3.back); // olha para quem chega
            return go;
        }

        // ---------- Gameplay ----------

        static void Gameplay(Transform p, GameObject king, Transform arrival)
        {
            Banner(p, C, Radius, "Sala do Rei Slime", "Salão das Raízes", Height);
            Checkpoint(p, arrival.position + Vector3.up, new Vector3(4f, 2f, 3f));
            Hint(p, C + new Vector3(0f, 1.5f, -11f), new Vector3(10f, 3f, 4f),
                "(Provisório) O Rei ainda não ataca: os 3 ataques do GDD vêm na tarefa do chefe.");

            // Portal de volta ao hub (azul), ao lado da chegada: liga quando o Rei cai.
            var back = new GameObject("PortalDeVolta");
            back.transform.SetParent(p, false);
            back.transform.position = C + new Vector3(-8f, 0f, -14f);
            var zone = back.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 1.6f, 0f);
            zone.size = new Vector3(3f, 3.2f, 1.5f);
            var ring = Ring(back.transform, "PortalDeVolta (ProBuilder)", back.transform.position + Vector3.up * 1.7f, Vector3.forward, portalBlue);
            var portal = back.AddComponent<Portal>();
            SetRef(portal, "visual", ring.GetComponent<Renderer>());
            var pso = new SerializedObject(portal);
            pso.FindProperty("startActive").boolValue = false;
            pso.FindProperty("activeColor").colorValue = new Color(0.3f, 0.5f, 1f);
            pso.FindProperty("activatedMessage").stringValue = "Um portal de volta ao hub se abriu.";
            pso.ApplyModifiedPropertiesWithoutUndo();

            var group = new GameObject("Chefe (grupo)").AddComponent<EnemyGroup>();
            group.transform.SetParent(p, false);
            var gso = new SerializedObject(group);
            var list = gso.FindProperty("enemies");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = king.GetComponent<Health>();
            gso.ApplyModifiedPropertiesWithoutUndo();
            var victory = group.gameObject.AddComponent<EventHint>();
            SetString(victory, "text", "O Rei Slime caiu! Fique em cima do corpo dele e segure E para absorvê-lo.");
            UnityEventTools.AddPersistentListener(group.OnCleared, victory.Show);
            UnityEventTools.AddPersistentListener(group.OnCleared, portal.Activate);
            EditorUtility.SetDirty(group);
        }
    }
}
