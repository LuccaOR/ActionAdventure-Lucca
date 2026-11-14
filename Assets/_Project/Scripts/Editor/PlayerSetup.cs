using System.IO;
using SlimeHunter.Combat;
using SlimeHunter.Core;
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
        const string PrefabDir = Root + "/Prefabs";
        const string SceneDir = Root + "/Scenes";
        const string ScenePath = SceneDir + "/PlayerTestbed.unity";

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

            var projectile = CreateProjectilePrefab(gooMat);
            CreatePlayerPrefab(projectile, playerMat);
            CreateTrainingSlimePrefab(enemyMat);
            CreateKeyPrefab(keyMat);
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
            var health = root.AddComponent<Health>();
            SetFloat(health, "invulnerabilityAfterHit", 0.6f);
            root.AddComponent<SlimeMass>();
            root.AddComponent<PlayerMotor>();
            var look = root.AddComponent<PlayerLook>();
            SetRef(look, "cameraPivot", pivot);
            SetRef(look, "playerCamera", cam);
            var shooter = root.AddComponent<PlayerShooter>();
            SetRef(shooter, "projectilePrefab", projectile);
            SetRef(shooter, "muzzle", muzzle);
            SetRef(shooter, "aimCamera", cam);
            SetRef(root.AddComponent<PlayerAbsorb>(), "aimCamera", cam);
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
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            SetFloat(go.AddComponent<Health>(), "maxHealth", 30f);
            go.AddComponent<AbsorbableSlime>();
            return Save(go, "TrainingSlime");
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
            trigger.radius = 2f; // relativo à escala pequena
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

            // Slimes de treino
            var enemies = new GameObject("TrainingSlimes").transform;
            Vector3[] spots = { new Vector3(-4, 0.45f, 8), new Vector3(-2, 0.45f, 10), new Vector3(-6, 0.45f, 11), new Vector3(10, 0.45f, -2), new Vector3(0, 0.45f, -15) };
            foreach (var s in spots) Inst(slime, s).transform.SetParent(enemies, true);

            // Checkpoint
            var cp = Box("Checkpoint", env, new Vector3(0, 0.05f, 0), new Vector3(2, 0.1f, 2), checkMat);
            cp.GetComponent<Collider>().isTrigger = true;
            cp.AddComponent<Checkpoint>();

            var p = Inst(player, new Vector3(0, 0.1f, 0));
            p.name = "Player";

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Debug.Log("[Slime Hunter] Cena de teste salva em " + ScenePath);
        }

        // ---------- Helpers ----------

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
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

        static GameObject Inst(GameObject prefab, Vector3 pos)
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

        static Material Mat(string name, Color color)
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

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
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
