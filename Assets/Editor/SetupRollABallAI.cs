using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;

[InitializeOnLoad]
public class SetupRollABallAI
{
    static SetupRollABallAI()
    {
        EditorApplication.delayCall += RunSetupOnce;
    }

    [MenuItem("Tools/Setup AI Tutorial")]
    public static void ManualRunSetup()
    {
        ExecuteSetup(true);
    }

    private static void RunSetupOnce()
    {
        if (!EditorPrefs.GetBool("RollABall_AI_Tutorial_Configured_v1", false))
        {
            ExecuteSetup(false);
            EditorPrefs.SetBool("RollABall_AI_Tutorial_Configured_v1", true);
        }
    }

    private static void ExecuteSetup(bool force)
    {
        Debug.Log("[AI Tutorial] Starting AI, NavMesh, and Obstacles setup...");

        // 1. Ensure Tag "Enemy" exists in TagManager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        bool hasEnemyTag = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals("Enemy"))
            {
                hasEnemyTag = true;
                break;
            }
        }
        if (!hasEnemyTag)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Enemy";
            tagManager.ApplyModifiedProperties();
            Debug.Log("[AI Tutorial] Added 'Enemy' tag.");
        }

        // 2. Load Scene
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.name != "Minigame")
        {
            currentScene = EditorSceneManager.OpenScene("Assets/Minigame.unity");
        }

        // 3. Materials
        Material enemyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Enemy.mat");
        if (enemyMat == null)
        {
            enemyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            enemyMat.color = new Color(0.95f, 0.1f, 0.5f, 1f);
            if (enemyMat.HasProperty("_BaseColor"))
            {
                enemyMat.SetColor("_BaseColor", new Color(0.95f, 0.1f, 0.5f, 1f));
            }
            AssetDatabase.CreateAsset(enemyMat, "Assets/Materials/Enemy.mat");
        }

        Material dynMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Dynamic Obstacle.mat");
        if (dynMat == null)
        {
            dynMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            dynMat.color = new Color(0.95f, 0.55f, 0.1f, 1f);
            if (dynMat.HasProperty("_BaseColor"))
            {
                dynMat.SetColor("_BaseColor", new Color(0.95f, 0.55f, 0.1f, 1f));
            }
            AssetDatabase.CreateAsset(dynMat, "Assets/Materials/Dynamic Obstacle.mat");
        }

        // 4. Ground and NavMeshSurface
        GameObject ground = GameObject.Find("Ground");
        if (ground != null)
        {
            NavMeshSurface surface = ground.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = ground.AddComponent<NavMeshSurface>();
            }
            surface.collectObjects = CollectObjects.Children;

            // 5. Static Obstacles as children of Ground
            Transform obs1 = ground.transform.Find("Obstacle_Cube1");
            if (obs1 == null)
            {
                GameObject cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube1.name = "Obstacle_Cube1";
                cube1.transform.SetParent(ground.transform);
                cube1.transform.localPosition = new Vector3(-4f, 0.5f, 3f);
                cube1.transform.localScale = new Vector3(2f, 1f, 2f);
            }

            Transform obs2 = ground.transform.Find("Obstacle_Cube2");
            if (obs2 == null)
            {
                GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube2.name = "Obstacle_Cube2";
                cube2.transform.SetParent(ground.transform);
                cube2.transform.localPosition = new Vector3(4f, 0.5f, -3f);
                cube2.transform.localScale = new Vector3(1.5f, 1f, 3f);
            }

            Transform obsRamp = ground.transform.Find("Obstacle_Ramp");
            if (obsRamp == null)
            {
                GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ramp.name = "Obstacle_Ramp";
                ramp.transform.SetParent(ground.transform);
                ramp.transform.localPosition = new Vector3(-3f, 0.35f, -4f);
                ramp.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
                ramp.transform.localScale = new Vector3(2.5f, 0.5f, 4f);
            }

            Transform obsBlock = ground.transform.Find("Obstacle_Block");
            if (obsBlock == null)
            {
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = "Obstacle_Block";
                block.transform.SetParent(ground.transform);
                block.transform.localPosition = new Vector3(3f, 0.5f, 4f);
                block.transform.localScale = new Vector3(2f, 1f, 1.5f);
            }

            // Bake NavMesh
            surface.BuildNavMesh();
            Debug.Log("[AI Tutorial] NavMesh baked on Ground.");
        }
        else
        {
            Debug.LogWarning("[AI Tutorial] Ground GameObject not found!");
        }

        // 6. Player and WinText reference
        GameObject player = GameObject.Find("Player");
        GameObject winText = GameObject.Find("WinText");
        if (player != null && winText != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null && pc.winTextObject == null)
            {
                pc.winTextObject = winText;
                EditorUtility.SetDirty(pc);
            }
        }

        // 7. Enemy GameObject
        GameObject enemy = GameObject.Find("Enemy");
        if (enemy == null)
        {
            enemy = new GameObject("Enemy");
            enemy.transform.position = new Vector3(0f, 0f, 6f);

            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.speed = 2.5f;

            EnemyMovement enemyMovement = enemy.AddComponent<EnemyMovement>();
            if (player != null)
            {
                enemyMovement.player = player.transform;
            }

            GameObject enemyBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemyBody.name = "EnemyBody";
            enemyBody.transform.SetParent(enemy.transform);
            enemyBody.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            enemyBody.transform.localScale = new Vector3(0.5f, 1f, 0.5f);

            Renderer bodyRenderer = enemyBody.GetComponent<Renderer>();
            if (bodyRenderer != null && enemyMat != null)
            {
                bodyRenderer.sharedMaterial = enemyMat;
            }

            enemyBody.tag = "Enemy";
            Debug.Log("[AI Tutorial] Enemy created with NavMeshAgent, EnemyMovement, and EnemyBody.");
        }
        else
        {
            // Ensure EnemyMovement has player assigned
            EnemyMovement em = enemy.GetComponent<EnemyMovement>();
            if (em != null && em.player == null && player != null)
            {
                em.player = player.transform;
                EditorUtility.SetDirty(em);
            }
        }

        // 8. Dynamic Obstacle Prefab & Instances
        string prefabPath = "Assets/Prefabs/DynamicBox.prefab";
        GameObject boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (boxPrefab == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            GameObject tempBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tempBox.name = "DynamicBox";
            Renderer r = tempBox.GetComponent<Renderer>();
            if (r != null && dynMat != null)
            {
                r.sharedMaterial = dynMat;
            }
            Rigidbody rb = tempBox.AddComponent<Rigidbody>();
            rb.mass = 0.1f;

            NavMeshObstacle obstacle = tempBox.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;

            boxPrefab = PrefabUtility.SaveAsPrefabAsset(tempBox, prefabPath);
            Object.DestroyImmediate(tempBox);
            Debug.Log("[AI Tutorial] DynamicBox prefab created.");
        }

        // Instances of DynamicBox
        GameObject dynGroup = GameObject.Find("DynamicBoxes");
        if (dynGroup == null)
        {
            dynGroup = new GameObject("DynamicBoxes");
            Vector3[] positions = new Vector3[]
            {
                new Vector3(1.5f, 0.5f, 1f),
                new Vector3(1.5f, 1.5f, 1f),
                new Vector3(-2f, 0.5f, 0f),
                new Vector3(0f, 0.5f, -5f)
            };
            foreach (var pos in positions)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(boxPrefab);
                inst.transform.SetParent(dynGroup.transform);
                inst.transform.position = pos;
            }
            Debug.Log("[AI Tutorial] DynamicBox instances placed in scene.");
        }

        // Save scene & assets
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AI Tutorial] AI Tutorial setup completed successfully!");
    }
}
