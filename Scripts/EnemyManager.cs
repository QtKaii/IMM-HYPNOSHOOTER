using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private int maxEnemies = 10;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float baseSpawnInterval = 2f;
    [SerializeField] private int baseMaxEnemies = 10;
    [SerializeField] private float enemySpeedMultiplier = 1f;
    
    private float lastSpawnTime;
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Start()
    {
        // Create default enemy if no prefabs are assigned
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            enemyPrefabs = new GameObject[1];
            enemyPrefabs[0] = CreateDefaultEnemy();
        }
    }

    private void Update()
    {
        // Clean up destroyed enemies from the list
        activeEnemies.RemoveAll(enemy => enemy == null);

        // Spawn new enemies if needed
        if (Time.time >= lastSpawnTime + spawnInterval && activeEnemies.Count < maxEnemies)
        {
            SpawnEnemy();
            lastSpawnTime = Time.time;
        }
    }

    private void SpawnEnemy()
    {
        // Get a random position on the screen edges
        Vector3 spawnPosition = GetRandomSpawnPosition();
        
        // Randomly select an enemy prefab
        GameObject selectedPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        GameObject enemy = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        enemy.SetActive(true); 
        
        // Set the speed multiplier after instantiation
        var enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            enemyComponent.SetSpeedMultiplier(enemySpeedMultiplier);
        }
        
        activeEnemies.Add(enemy);
        GameManager.Instance.enemies.Add(enemyComponent); // ADD: Add to GameManager's enemy list
        Debug.Log("Enemy spawned and added to GameManager's list.");
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Camera mainCamera = Camera.main;
        float padding = 1f; // Distance outside the screen to spawn
        
        // Get screen bounds in world coordinates
        float screenHeight = mainCamera.orthographicSize * 2;
        float screenWidth = screenHeight * mainCamera.aspect;
        float leftBound = mainCamera.transform.position.x - screenWidth/2 - padding;
        float rightBound = mainCamera.transform.position.x + screenWidth/2 + padding;
        float topBound = mainCamera.transform.position.z + screenHeight/2 + padding;
        float bottomBound = mainCamera.transform.position.z - screenHeight/2 - padding;

        // Randomly choose which edge to spawn on
        int edge = Random.Range(0, 4);
        switch (edge)
        {
            case 0: // top
                return new Vector3(Random.Range(leftBound, rightBound), 0, topBound);
            case 1: // right
                return new Vector3(rightBound, 0, Random.Range(bottomBound, topBound));
            case 2: // bottom
                return new Vector3(Random.Range(leftBound, rightBound), 0, bottomBound);
            default: // left
                return new Vector3(leftBound, 0, Random.Range(bottomBound, topBound));
        }
    }

    private GameObject CreateDefaultEnemy()
    {
        GameObject defaultEnemy = new GameObject("DefaultEnemy");
        
        // Add visual representation
        var renderer = defaultEnemy.AddComponent<MeshRenderer>();
        var filter = defaultEnemy.AddComponent<MeshFilter>();
        filter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = Color.red;
        
        // Add physics components
        var rb = defaultEnemy.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        var collider = defaultEnemy.AddComponent<BoxCollider>();
        collider.size = Vector3.one * 0.8f;
        
        // Set proper scale
        defaultEnemy.transform.localScale = Vector3.one * 0.8f;
        
        // Add Enemy component and configure it
        var enemyComponent = defaultEnemy.AddComponent<Enemy>();
        
        // Make it a prefab by deactivating it
        defaultEnemy.SetActive(false);
        Debug.Log("Default Enemy Prefab created.");
        
        return defaultEnemy;
    }

    public void ClearAllEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
                GameManager.Instance.enemies.Remove(enemy.GetComponent<Enemy>()); 
                Debug.Log("Enemy destroyed and removed from GameManager's list.");
            }
        }
        activeEnemies.Clear();
        Debug.Log("All enemies cleared.");
    }

    public void SetDifficulty(int round, float multiplier)
    {
        // spawn rate and max enemies based on round
        float difficulty = Mathf.Pow(multiplier, round - 1);
        spawnInterval = baseSpawnInterval / difficulty;
        maxEnemies = Mathf.RoundToInt(baseMaxEnemies * difficulty);
        enemySpeedMultiplier = difficulty;
        
        // Reset spawn timer to start spawning immediately
        lastSpawnTime = Time.time - spawnInterval;

        Debug.Log($"Difficulty set to {difficulty}. Spawn Interval: {spawnInterval}, Max Enemies: {maxEnemies}, Enemy Speed Multiplier: {enemySpeedMultiplier}");
    }
}
