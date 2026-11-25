using System.IO;
using SlimeHunter.Combat;
using SlimeHunter.Core;
using SlimeHunter.Enemies;
using SlimeHunter.Interaction;
using SlimeHunter.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SlimeHunter.EditorTools
{
    /// <summary>
    /// Gera os prefabs do protagonista e uma cena de teste com todas as mecânicas.
    /// Menu: Slime Hunter > Gerar Player + Cena de Teste
    /// </summary>
    public static class PlayerSetup
    {
        const string Root = "Assets/_Project";
        const string MatDir = Root + "/Materials";
        internal const string PrefabDir = Root + "/Prefabs";
        const string SceneDir = Root + "/Scenes";
        const string ScenePath = SceneDir + "/PlayerTestbed.unity";

        const string PlayerLayer = "Player";
        const string EnemyLayer = "Enemy";
        const string BarrierLayer = "DashBarrier";

        [MenuItem("Slime Hunter/Gerar Player + Cena de Teste")]
        public static void BuildAll()
        {
            BuildPrefabs();
            BuildTestScene();
        }

        [MenuItem("Slime Hunter/Gerar Apenas Prefabs")]
        public static void BuildPrefabs()
        {
            EnsureFolder(MatDir);
            EnsureFolder(PrefabDir);

            var playerMat = Mat("M_PlayerSlime", new Color(0.45f, 0.85f, 1f));
            var gooMat = Mat("M_SlimeGoo", new Color(0.35f, 0.95f, 0.5f));
            var enemyMat = Mat("M_EnemySlime", new Color(0.7f, 0.95f, 0.3f));
            var keyMat = Mat("M_Key", new Color(1f, 0.82f, 0.2f));

            EnsureLayer(PlayerLayer);
            EnsureLayer(EnemyLayer);
            EnsureLayer(BarrierLayer);

            var projectile = CreateProjectilePrefab(gooMat);
            CreatePlayerPrefab(projectile, playerMat);
            CreateTrainingSlimePrefab(enemyMat);
            CreateKeyPrefab(keyMat);
            CreateGooOrbPrefab(gooMat);
            CreateMeleeSlimePrefab(Mat("M_MeleeSlime", new Color(1f, 0.55f, 0.2f)));
            var enemyGoo = CreateEnemyProjectilePrefab(Mat("M_EnemyGoo", new Color(0.75f, 1f, 0.2f)));
            CreateRangedSlimePrefab(Mat("M_RangedSlime", new Color(0.25f, 0.75f, 0.7f)), enemyGoo);
            AssetDatabase.SaveAssets();
            Debug.Log("[Slime Hunter] Prefabs gerados em " + PrefabDir);
        }

        // ---------- Prefabs ----------

        static SlimeProjectile CreateProjectilePrefab(Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SlimeProjectile";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.3f;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<SlimeProjectile>();
            return Save(go, "SlimeProjectile").GetComponent<SlimeProjectile>();
        }

        static GameObject CreatePlayerPrefab(SlimeProjectile projectile, Material mat)
        {
            var root = new GameObject("Player");
            root.tag = "Player";
            root.layer = LayerMask.NameToLayer(PlayerLayer);

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.0f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.5f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.04f;

            // Corpo: só projeta sombra (primeira pessoa, a câmera fica dentro dele).
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            body.transform.localScale = new Vector3(0.85f, 0.75f, 0.85f);
            var br = body.GetComponent<MeshRenderer>();
            br.sharedMaterial = mat;
            br.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(root.transform, false);
            pivot.localPosition = new Vector3(0f, 0.75f, 0f);

            var camGo = new GameObject("PlayerCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 80f;
            camGo.AddComponent<AudioListener>();

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(camGo.transform, false);
            muzzle.localPosition = new Vector3(0.18f, -0.15f, 0.5f);

            root.AddComponent<PlayerInputReader>();
            root.AddComponent<PlayerSize>();
            var motor = root.AddComponent<PlayerMotor>();
            SetInt(motor, "dashPassThroughMask", LayerMask.GetMask(EnemyLayer, BarrierLayer));
            var look = root.AddComponent<PlayerLook>();
            SetRef(look, "cameraPivot", pivot);
            SetRef(look, "playerCamera", cam);
            var shooter = root.AddComponent<PlayerShooter>();
            SetRef(shooter, "projectilePrefab", projectile);
            SetRef(shooter, "muzzle", muzzle);
            SetRef(shooter, "aimCamera", cam);
            root.AddComponent<PlayerAbsorb>();
            SetRef(root.AddComponent<PlayerInteractor>(), "aimCamera", cam);
            root.AddComponent<PlayerInventory>();
            root.AddComponent<PlayerController>();
            root.AddComponent<PlayerHUD>();

            return Save(root, "Player");
        }

        static GameObject CreateTrainingSlimePrefab(Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "TrainingSlime";
            go.transform.localScale = new Vector3(1.2f, 0.9f, 1.2f);
            go.layer = LayerMask.NameToLayer(EnemyLayer);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            SetFloat(go.AddComponent<Health>(), "maxHealth", 30f);
            go.AddComponent<AbsorbableSlime>();
            return Save(go, "TrainingSlime");
        }

        /// <summary>Corpo comum dos slimes inimigos: collider no root, corpo visual com olhos, Rigidbody, Health e poça.</summary>
        static (GameObject root, Transform body) CreateEnemySlimeBase(string name, Material mat, Vector3 bodyScale, float maxHealth)
        {
            int enemyLayer = LayerMask.NameToLayer(EnemyLayer);

            var root = new GameObject(name);
            root.layer = enemyLayer;
            var col = root.AddComponent<SphereCollider>();
            col.radius = 0.45f;

            // Corpo visual separado do collider: o squash & stretch não mexe na física.
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.layer = enemyLayer;
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = bodyScale;
            body.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Olhinhos para dar direção/identidade.
            var eyeMat = Mat("M_SlimeEye", new Color(0.08f, 0.05f, 0.05f));
            foreach (float x in new[] { -0.18f, 0.18f })
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye";
                eye.layer = enemyLayer;
                Object.DestroyImmediate(eye.GetComponent<Collider>());
                eye.transform.SetParent(body.transform, false);
                eye.transform.localPosition = new Vector3(x, 0.12f, 0.45f);
                eye.transform.localScale = new Vector3(0.12f, 0.18f, 0.08f);
                eye.GetComponent<MeshRenderer>().sharedMaterial = eyeMat;
            }

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            SetFloat(root.AddComponent<Health>(), "maxHealth", maxHealth);
            return (root, body.transform);
        }

        /// <summary>Liga a IA ao corpo: referência ao corpo, visão e desligar a IA quando vira poça.</summary>
        static void FinishEnemySlime(GameObject root, Transform body, SlimeEnemy ai, float sizeReward)
        {
            SetRef(ai, "body", body);
            SetInt(ai, "sightMask", ~LayerMask.GetMask(EnemyLayer));

            var absorb = root.AddComponent<AbsorbableSlime>();
            var so = new SerializedObject(absorb);
            var list = so.FindProperty("disableOnDeath");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = ai;
            so.FindProperty("sizeReward").floatValue = sizeReward;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject CreateMeleeSlimePrefab(Material mat)
        {
            var (root, body) = CreateEnemySlimeBase("MeleeSlime", mat, new Vector3(1.05f, 0.85f, 1.05f), 40f);
            var melee = root.AddComponent<MeleeSlime>();
            FinishEnemySlime(root, body, melee, 1.5f);
            return Save(root, "MeleeSlime");
        }

        static SlimeProjectile CreateEnemyProjectilePrefab(Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "EnemyGooProjectile";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * 0.4f;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            var p = go.AddComponent<SlimeProjectile>();
            // Gosma inimiga atravessa outros slimes (sem fogo amigo).
            SetInt(p, "hitMask", ~LayerMask.GetMask(EnemyLayer));
            return Save(go, "EnemyGooProjectile").GetComponent<SlimeProjectile>();
        }

        static GameObject CreateRangedSlimePrefab(Material mat, SlimeProjectile projectile)
        {
            var (root, body) = CreateEnemySlimeBase("RangedSlime", mat, new Vector3(0.95f, 0.95f, 0.95f), 25f);

            // Boca: de onde sai a gosma.
            var mouth = new GameObject("Mouth").transform;
            mouth.SetParent(root.transform, false);
            mouth.localPosition = new Vector3(0f, 0.05f, 0.6f);

            var ranged = root.AddComponent<RangedSlime>();
            SetRef(ranged, "projectilePrefab", projectile);
            SetRef(ranged, "mouth", mouth);
            var so = new SerializedObject(ranged);
            so.FindProperty("windupTime").floatValue = 0.9f;      // incha devagar: dá pra ver chegando
            so.FindProperty("attackCooldown").floatValue = 2.2f;
            so.FindProperty("recoverTime").floatValue = 0.5f;
            so.FindProperty("detectRange").floatValue = 16f;
            so.FindProperty("telegraphScale").vector3Value = new Vector3(1.35f, 1.4f, 1.35f); // infla
            so.FindProperty("telegraphColor").colorValue = new Color(0.8f, 1f, 0.25f);
            so.ApplyModifiedPropertiesWithoutUndo();

            FinishEnemySlime(root, body, ranged, 1f);
            return Save(root, "RangedSlime");
        }

        static GameObject CreateGooOrbPrefab(Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "GooOrb";
            go.transform.localScale = Vector3.one * 0.5f;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<SphereCollider>().isTrigger = true;
            var pickup = go.AddComponent<Pickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("kind").enumValueIndex = (int)Pickup.Kind.Size;
            so.FindProperty("itemId").stringValue = "gosma";
            so.FindProperty("displayName").stringValue = "Gosma";
            so.ApplyModifiedPropertiesWithoutUndo();
            return Save(go, "GooOrb");
        }

        static GameObject CreateKeyPrefab(Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Key_Dourada";
            go.transform.localScale = new Vector3(0.25f, 0.5f, 0.1f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 3f; // relativo à escala pequena (≈1.5 m: o slime pequeno também alcança a chave no pedestal)
            go.AddComponent<Pickup>();
            return Save(go, "Key_Dourada");
        }

        // ---------- Cena de teste ----------

        [MenuItem("Slime Hunter/Gerar Apenas Cena de Teste")]
        public static void BuildTestScene()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Player.prefab");
            if (player == null) { BuildPrefabs(); player = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Player.prefab"); }
            var slime = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/TrainingSlime.prefab");
            var key = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Key_Dourada.prefab");

            EnsureFolder(SceneDir);
            var groundMat = Mat("M_Ground", new Color(0.42f, 0.62f, 0.35f));
            var platMat = Mat("M_Platform", new Color(0.62f, 0.52f, 0.42f));
            var doorMat = Mat("M_Door", new Color(0.55f, 0.3f, 0.2f));
            var checkMat = Mat("M_Checkpoint", new Color(0.4f, 0.8f, 1f));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var defaultCam = GameObject.FindWithTag("MainCamera");
            if (defaultCam != null) Object.DestroyImmediate(defaultCam);

            var env = new GameObject("Environment").transform;

            // Chão
            Box("Ground", env, new Vector3(0, -0.5f, 0), new Vector3(80, 1, 80), groundMat);

            // Escada de plataformas (pulo / pulo duplo / mapa vertical)
            Box("Plat_1", env, new Vector3(6, 0.75f, 6), new Vector3(3, 1.5f, 3), platMat);
            Box("Plat_2", env, new Vector3(6, 2.0f, 11), new Vector3(3, 0.5f, 3), platMat);   // pulo simples
            Box("Plat_3", env, new Vector3(6, 4.5f, 16), new Vector3(3, 0.5f, 3), platMat);   // precisa de pulo duplo
            Box("Plat_4", env, new Vector3(0, 6.5f, 20), new Vector3(3, 0.5f, 3), platMat);
            Box("Tower", env, new Vector3(-7, 4f, 20), new Vector3(4, 8f, 4), platMat);       // topo para testar impacto

            // Gap para testar dash
            Box("DashGap_A", env, new Vector3(-14, 1.5f, 6), new Vector3(4, 3f, 4), platMat);
            Box("DashGap_B", env, new Vector3(-23, 1.5f, 6), new Vector3(4, 3f, 4), platMat);
            Box("Ramp", env, new Vector3(-14, 1.4f, 0.5f), new Vector3(3, 0.3f, 8), platMat).transform.rotation = Quaternion.Euler(-25f, 0, 0);

            // Sala trancada com porta
            Box("Room_Back", env, new Vector3(0, 2, -18), new Vector3(10, 4, 0.5f), platMat);
            Box("Room_Left", env, new Vector3(-5, 2, -14), new Vector3(0.5f, 4, 8), platMat);
            Box("Room_Right", env, new Vector3(5, 2, -14), new Vector3(0.5f, 4, 8), platMat);
            Box("Room_FrontL", env, new Vector3(-3.25f, 2, -10), new Vector3(3.5f, 4, 0.5f), platMat);
            Box("Room_FrontR", env, new Vector3(3.25f, 2, -10), new Vector3(3.5f, 4, 0.5f), platMat);
            Box("Room_Roof", env, new Vector3(0, 4.25f, -14), new Vector3(10.5f, 0.5f, 8.5f), platMat);
            var door = Box("Door", env, new Vector3(0, 1.5f, -10), new Vector3(3, 3, 0.4f), doorMat);
            door.AddComponent<KeyLockedDoor>();

            // Chave em cima da plataforma alta
            Inst(key, new Vector3(0, 7.5f, 20));

            // Jaula com barreira de gosma: só entra atravessando com o dash
            var barrierMat = Mat("M_DashBarrier", new Color(0.75f, 0.35f, 1f));
            Box("Cage_Back", env, new Vector3(14, 2, -9), new Vector3(10, 4, 0.5f), platMat);
            Box("Cage_Left", env, new Vector3(9, 2, -6.5f), new Vector3(0.5f, 4, 5), platMat);
            Box("Cage_Right", env, new Vector3(19, 2, -6.5f), new Vector3(0.5f, 4, 5), platMat);
            Box("Cage_FrontL", env, new Vector3(11, 2, -4), new Vector3(4, 4, 0.5f), platMat);
            Box("Cage_FrontR", env, new Vector3(17, 2, -4), new Vector3(4, 4, 0.5f), platMat);
            Box("Cage_Roof", env, new Vector3(14, 4.25f, -6.5f), new Vector3(10.5f, 0.5f, 5.5f), platMat);
            var barrier = Box("DashBarrier", env, new Vector3(14, 1.5f, -4), new Vector3(2, 3, 0.4f), barrierMat);
            barrier.layer = LayerMask.NameToLayer(BarrierLayer);
            var gooOrb = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/GooOrb.prefab");
            Inst(gooOrb, new Vector3(14, 0.6f, -7));
            Inst(gooOrb, new Vector3(16, 0.6f, -7));
            Inst(gooOrb, new Vector3(-4, 0.6f, 3));

            // Slimes de treino
            var enemies = new GameObject("TrainingSlimes").transform;
            Vector3[] spots = { new Vector3(-4, 0.45f, 8), new Vector3(-2, 0.45f, 10), new Vector3(-6, 0.45f, 11), new Vector3(10, 0.45f, -1), new Vector3(0, 0.45f, -15), new Vector3(11.5f, 0.45f, -7) };
            foreach (var s in spots) Inst(slime, s).transform.SetParent(enemies, true);

            // Slimes melee numa clareira a nordeste (fora do alcance de visão do spawn)
            var meleePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/MeleeSlime.prefab");
            var melees = new GameObject("MeleeSlimes").transform;
            Vector3[] meleeSpots = { new Vector3(18, 0.5f, 14), new Vector3(22, 0.5f, 18), new Vector3(16, 0.5f, 20) };
            foreach (var s in meleeSpots) Inst(meleePrefab, s).transform.SetParent(melees, true);

            // Slimes ranged a oeste: um em cima do pilar do dash (atira do alto) e dois no chão
            var rangedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/RangedSlime.prefab");
            var rangeds = new GameObject("RangedSlimes").transform;
            Vector3[] rangedSpots = { new Vector3(-23, 3.5f, 6), new Vector3(-20, 0.5f, 18), new Vector3(-28, 0.5f, 13) };
            foreach (var s in rangedSpots) Inst(rangedPrefab, s).transform.SetParent(rangeds, true);

            // Checkpoint
            var cp = Box("Checkpoint", env, new Vector3(0, 0.05f, 0), new Vector3(2, 0.1f, 2), checkMat);
            cp.GetComponent<Collider>().isTrigger = true;
            cp.AddComponent<Checkpoint>();

            var p = Inst(player, new Vector3(0, 0.1f, 0));
            p.name = "Player";

            PuzzleSetup.BuildPuzzleLane();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Debug.Log("[Slime Hunter] Cena de teste salva em " + ScenePath);
        }

        // ---------- Helpers ----------

        internal static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        internal static GameObject Inst(GameObject prefab, Vector3 pos)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = pos;
            return go;
        }

        static GameObject Save(GameObject go, string name)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        internal static Material Mat(string name, Color color)
        {
            EnsureFolder(MatDir);
            string path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.6f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        internal static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Cria a camada (User Layer) se ainda não existir.</summary>
        static int EnsureLayer(string name)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var p = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(p.stringValue)) continue;
                p.stringValue = name;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
            throw new System.InvalidOperationException("Sem espaço para criar a camada " + name);
        }

        internal static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
