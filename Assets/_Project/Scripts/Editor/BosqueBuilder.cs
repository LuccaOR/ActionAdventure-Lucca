using SlimeHunter.Interaction;
using SlimeHunter.Puzzles;
using UnityEditor;
using UnityEngine;
using static SlimeHunter.EditorTools.MapBuilder;
using static SlimeHunter.EditorTools.PlayerSetup;
using static SlimeHunter.EditorTools.TutorialBuilder;

namespace SlimeHunter.EditorTools
{
    /// <summary>
    /// Fase 4 do mapa: 5 Bosque das Raízes Retorcidas (Notion, Parte 2).
    /// Três clareiras (faixas sul → norte) separadas por paredes de raízes gigantes. Em cada parede há
    /// uma bifurcação:
    ///   • túnel baixo de raízes (1.4 × 1.0 m) no LESTE: encolher atirando e passar escondido;
    ///   • vão largo no OESTE: atravessar as clareiras e enfrentar os slimes (e absorvê-los).
    /// No fundo (canto NO, oposto à entrada) fica o santuário com a Chave 1 (Esmeralda).
    /// Coordenadas locais: centro do bosque, +X leste, +Z norte, y = 0 no piso (4 m acima do hub).
    /// </summary>
    static class BosqueBuilder
    {
        const float Half = Layout.BosqueSize / 2f;   // 17.5
        const float WallH = 5f;                      // acima do pulo duplo (≈3.2 m + corpo)
        const float TunnelW = 1.4f, TunnelH = 1.0f;  // mesma fenda do tutorial: tamanho ≤ 1.4
        const float GapW = 4f;

        // Paredes de raízes (z local) e aberturas (x local)
        const float Wall1Z = -6f, Wall2Z = 6f, WallT = 3f;
        const float Tunnel1X = 10f, Gap1X = -13f;
        const float Tunnel2X = 12f, Gap2X = -5f;

        static Material rootMat, barkMat, stoneMat;
        static Vector3 C => Layout.Bosque;

        struct Opening
        {
            public float X, W, H;
            public Opening(float x, float w, float h) { X = x; W = w; H = h; }
        }

        public static void Build(Transform p)
        {
            rootMat = Mat("M_RaizGigante", new Color(0.38f, 0.26f, 0.17f));
            barkMat = Mat("M_Madeira", new Color(0.45f, 0.3f, 0.18f));
            stoneMat = Mat("M_Templo", new Color(0.55f, 0.5f, 0.62f));
            var gooOrb = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/GooOrb.prefab");
            var melee = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/MeleeSlime.prefab");
            var ranged = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/RangedSlime.prefab");
            var key = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Key_Dourada.prefab");

            Perimeter(Group(p, "Borda"));

            // Paredes de raízes com a bifurcação (túnel baixo a leste, vão largo a oeste).
            var walls = Group(p, "Paredes de Raizes");
            RootWall(walls, Wall1Z, new Opening(Gap1X, GapW, WallH), new Opening(Tunnel1X, TunnelW, TunnelH));
            RootWall(walls, Wall2Z, new Opening(Gap2X, GapW, WallH), new Opening(Tunnel2X, TunnelW, TunnelH));

            // Patrulhas nas clareiras (oeste), longe dos túneis (leste): 2 + 2 + 1 = 5 slimes.
            var enemies = Group(p, "Patrulha de Slimes");
            Spawn(enemies, melee, new Vector3(-9f, 0.5f, -12f));
            Spawn(enemies, melee, new Vector3(-5f, 0.5f, -15f));
            Spawn(enemies, melee, new Vector3(-8f, 0.5f, 0f));
            Spawn(enemies, ranged, new Vector3(-14f, 0.5f, 2f));

            // Gosma depois de cada túnel: dá para crescer um pouco sem lutar.
            var goo = Group(p, "Gosma");
            Spawn(goo, gooOrb, new Vector3(13f, 0.6f, -2f));
            Spawn(goo, gooOrb, new Vector3(14.5f, 0.6f, 10f));

            Sanctuary(Group(p, "Santuario da Chave 1"), key, ranged);

            // Dicas e checkpoints.
            Vector3 entrance = new Vector3(13f, 0f, -13f);
            Hint(p, W(entrance + Vector3.up * 1.5f), new Vector3(7f, 3f, 7f),
                "Raízes formam túneis baixos: encolha (atire) para passar escondido, ou vá pelo caminho largo e enfrente os slimes.");
            Checkpoint(p, W(entrance + Vector3.up * 1f), new Vector3(5f, 2f, 5f));
            SizeHint(p, new Vector3(Tunnel1X, 1f, Wall1Z - WallT / 2f - 1.5f));
            SizeHint(p, new Vector3(Tunnel2X, 1f, Wall2Z - WallT / 2f - 1.5f));
            Checkpoint(p, W(new Vector3(12f, 1f, -1f)), new Vector3(4f, 2f, 4f));
            Checkpoint(p, W(new Vector3(5f, 1f, 10f)), new Vector3(6f, 2f, 4f));
        }

        static Vector3 W(Vector3 local) => C + local;

        // ---------- Borda ----------

        static void Perimeter(Transform p)
        {
            const float t = 1f;
            // Oeste e norte inteiras; sul inteira; leste aberta no canto SE (z −17.5 → −11) por onde chega o caminho.
            Box("Borda_O", p, W(new Vector3(-Half - t / 2f, WallH / 2f, 0f)), new Vector3(t, WallH, Half * 2f + 2f), rootMat);
            Box("Borda_N", p, W(new Vector3(0f, WallH / 2f, Half + t / 2f)), new Vector3(Half * 2f + 2f, WallH, t), rootMat);
            Box("Borda_S", p, W(new Vector3(0f, WallH / 2f, -Half - t / 2f)), new Vector3(Half * 2f + 2f, WallH, t), rootMat);
            const float openTo = -11f;
            float len = Half + 1f - openTo;
            Box("Borda_L", p, W(new Vector3(Half + t / 2f, WallH / 2f, openTo + len / 2f)), new Vector3(t, WallH, len), rootMat);
            // Raízes decorativas por cima das bordas.
            for (float z = -Half; z <= Half; z += 7f) RootLog(p, W(new Vector3(-Half, WallH, z)), 0f);
            for (float x = -Half; x <= Half; x += 7f) RootLog(p, W(new Vector3(x, WallH, Half)), 90f);
        }

        // ---------- Paredes de raízes ----------

        /// <summary>Parede de raízes atravessando o bosque (eixo X) em z, com aberturas.</summary>
        static void RootWall(Transform p, float z, params Opening[] openings)
        {
            System.Array.Sort(openings, (a, b) => a.X.CompareTo(b.X));
            float x = -Half;
            foreach (var o in openings)
            {
                float end = o.X - o.W / 2f;
                if (end - x > 0.01f) Box("Raiz", p, W(new Vector3((x + end) / 2f, WallH / 2f, z)), new Vector3(end - x, WallH, WallT), rootMat);
                if (o.H < WallH)
                {
                    Box("Raiz_Tunel", p, W(new Vector3(o.X, o.H + (WallH - o.H) / 2f, z)), new Vector3(o.W, WallH - o.H, WallT), rootMat);
                    // Arco de raiz marcando a entrada do túnel.
                    RootLog(p, W(new Vector3(o.X, o.H + 0.4f, z - WallT / 2f)), 90f, 0.5f);
                }
                x = o.X + o.W / 2f;
            }
            if (Half - x > 0.01f) Box("Raiz", p, W(new Vector3((x + Half) / 2f, WallH / 2f, z)), new Vector3(Half - x, WallH, WallT), rootMat);

            for (float rx = -Half + 3f; rx < Half; rx += 8f) RootLog(p, W(new Vector3(rx, WallH + 0.2f, z)), 20f);
        }

        static void RootLog(Transform p, Vector3 pos, float yaw, float radius = 0.7f)
        {
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "RaizDecorativa";
            Object.DestroyImmediate(log.GetComponent<Collider>());
            log.transform.SetParent(p, false);
            log.transform.position = pos;
            log.transform.rotation = Quaternion.Euler(0f, yaw, 90f);
            log.transform.localScale = new Vector3(radius * 2f, 3.5f, radius * 2f);
            log.GetComponent<MeshRenderer>().sharedMaterial = barkMat;
            log.isStatic = true;
        }

        // ---------- Santuário ----------

        static void Sanctuary(Transform p, GameObject keyPrefab, GameObject ranged)
        {
            // Plataforma de 1.5 m no canto NO com rampa vindo do leste; pedestal com a chave.
            Vector3 altar = new Vector3(-12f, 0f, 13f);
            Box("Plataforma", p, W(altar + new Vector3(0f, 0.75f, 0f)), new Vector3(7f, 1.5f, 7f), stoneMat);
            Path(p, "Rampa do Santuario", W(new Vector3(-3f, 0f, 13f)), W(new Vector3(-8.5f, 1.5f, 13f)), 2.5f, stoneMat);
            Box("Pedestal", p, W(altar + new Vector3(0f, 2f, 0f)), new Vector3(1.2f, 1f, 1.2f), stoneMat);
            for (int i = 0; i < 4; i++)
            {
                var s = Box("Coluna", p, W(altar + new Vector3(i % 2 == 0 ? -3f : 3f, 3f, i < 2 ? -3f : 3f)), new Vector3(0.6f, 3f, 0.6f), stoneMat);
                s.name = "Coluna";
            }

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
            Box("TocoDoGuarda", p, W(new Vector3(0.5f, 1.5f, 16f)), new Vector3(2.5f, 3f, 2.5f), rootMat);
            Spawn(p, ranged, new Vector3(0.5f, 3.5f, 16f));
        }

        // ---------- Utilidades ----------

        static void Spawn(Transform p, GameObject prefab, Vector3 local)
        {
            Inst(prefab, W(local)).transform.SetParent(p, true);
        }

        static void SizeHint(Transform p, Vector3 local)
        {
            var go = Trigger(p, "DicaDoTunel", W(local), new Vector3(5f, 2f, 3f));
            SetFloat(go.AddComponent<SizeHintZone>(), "size", 1.4f);
        }
    }
}
