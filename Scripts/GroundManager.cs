using UnityEngine;
using System.Collections.Generic;

public class GroundManager : MonoBehaviour
{
    [Header("Ground Settings")]
    [Tooltip("The prefab for your ground tile")]
    [SerializeField] private GameObject groundTilePrefab;
    
    public void SetGroundTilePrefab(GameObject prefab)
    {
        groundTilePrefab = prefab;
        if (Application.isPlaying && groundTilePrefab != null)
        {
            InitializeGroundGrid();
        }
    }

    [Tooltip("Size of each ground tile")]
    [SerializeField] private float tileSize = 10f;

    [Tooltip("Number of tiles in each direction (total tiles = gridSize * gridSize)")]
    [SerializeField] private int gridSize = 3;

    [Tooltip("How far the player needs to move (as a fraction of tile size) before tiles reposition")]
    [Range(0.1f, 0.5f)]
    [SerializeField] private float repositionThreshold = 0.25f;

    [Tooltip("The Y position the ground should maintain")]
    [SerializeField] private float groundHeight = 0f;

    private Transform playerTransform;
    private Vector3 lastPlayerPosition;
    private GameObject[,] groundTiles;
    private Vector2Int currentCenterTile;

    private void Start()
    {
        // Wait a frame to ensure GameManager has initialized the player
        StartCoroutine(InitializeAfterGameManager());
    }

    private System.Collections.IEnumerator InitializeAfterGameManager()
    {
        yield return null; // Wait one frame

        if (groundTilePrefab == null)
        {
            Debug.LogError("Ground tile prefab not assigned!");
            enabled = false;
            yield break;
        }

        // Find player
        if (GameManager.Instance != null && GameManager.Instance.player != null)
        {
            playerTransform = GameManager.Instance.player.transform;
            lastPlayerPosition = playerTransform.position;
            InitializeGroundGrid();
        }
        else
        {
            Debug.LogError("Player reference not found!");
            enabled = false;
        }
    }

    private void InitializeGroundGrid()
    {
        Debug.Log($"InitializeGroundGrid called. Prefab: {groundTilePrefab}, GridSize: {gridSize}, TileSize: {tileSize}");
        
        if (groundTilePrefab == null)
        {
            Debug.LogError("Ground tile prefab is null in InitializeGroundGrid!");
            return;
        }

        // Create parent object for tiles
        GameObject tilesParent = new GameObject("Ground Tiles");
        tilesParent.transform.parent = transform;

        groundTiles = new GameObject[gridSize, gridSize];
        float offset = (gridSize - 1) * tileSize * 0.5f;

        // Create initial grid of tiles
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector3 position = new Vector3(
                    x * tileSize - offset,
                    groundHeight,
                    z * tileSize - offset
                );

                GameObject tile = Instantiate(groundTilePrefab, position, Quaternion.identity, tilesParent.transform);
                if (tile == null)
                {
                    Debug.LogError($"Failed to instantiate ground tile at position {position}");
                    continue;
                }
                groundTiles[x, z] = tile;
                groundTiles[x, z].name = $"Ground_Tile_{x}_{z}";
                Debug.Log($"Created tile at {position}");
            }
        }

        currentCenterTile = new Vector2Int(gridSize / 2, gridSize / 2);
    }

    private void Update()
    {
        if (playerTransform == null || groundTiles == null) return;

        Vector3 movement = playerTransform.position - lastPlayerPosition;
        
        // Check if player has moved far enough to warrant repositioning
        if (movement.magnitude > tileSize * repositionThreshold)
        {
            RepositionTiles();
            lastPlayerPosition = playerTransform.position;
        }
    }

    private void RepositionTiles()
    {
        Vector3 playerPos = playerTransform.position;
        float maxDistance = tileSize * (gridSize - 1) * 0.5f;
        Vector3 playerXZPos = new Vector3(playerPos.x, groundHeight, playerPos.z);

        // First, remove all tiles that are too far away
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (groundTiles[x, z] != null)
                {
                    Vector3 tilePos = groundTiles[x, z].transform.position;
                    if (Vector3.Distance(tilePos, playerXZPos) > maxDistance * 1.5f)
                    {
                        Destroy(groundTiles[x, z]);
                        groundTiles[x, z] = null;
                    }
                }
            }
        }

        // Calculate the grid positions needed around the player
        int startX = Mathf.RoundToInt((playerPos.x - maxDistance) / tileSize);
        int endX = Mathf.RoundToInt((playerPos.x + maxDistance) / tileSize);
        int startZ = Mathf.RoundToInt((playerPos.z - maxDistance) / tileSize);
        int endZ = Mathf.RoundToInt((playerPos.z + maxDistance) / tileSize);

        // Create a hashset to track existing tile positions
        HashSet<Vector2Int> existingPositions = new HashSet<Vector2Int>();
        
        // Record positions of existing tiles
        foreach (GameObject tile in groundTiles)
        {
            if (tile != null)
            {
                Vector3 pos = tile.transform.position;
                existingPositions.Add(new Vector2Int(
                    Mathf.RoundToInt(pos.x / tileSize),
                    Mathf.RoundToInt(pos.z / tileSize)
                ));
            }
        }

        // Spawn new tiles where needed
        for (int x = startX; x <= endX; x++)
        {
            for (int z = startZ; z <= endZ; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                Vector3 worldPos = new Vector3(x * tileSize, groundHeight, z * tileSize);

                // Only spawn if within range and position not occupied
                if (Vector3.Distance(worldPos, playerXZPos) <= maxDistance && 
                    !existingPositions.Contains(gridPos) &&
                    !Physics.Raycast(worldPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, LayerMask.GetMask("Default")))
                {
                    // Find an empty slot in the array
                    for (int i = 0; i < gridSize; i++)
                    {
                        for (int j = 0; j < gridSize; j++)
                        {
                            if (groundTiles[i, j] == null)
                            {
                                groundTiles[i, j] = Instantiate(groundTilePrefab, worldPos, 
                                    Quaternion.identity, transform);
                                goto tileStored;
                            }
                        }
                    }
                    tileStored: continue;
                }
            }
        }

        currentCenterTile = new Vector2Int(
            Mathf.RoundToInt(playerPos.x / tileSize),
            Mathf.RoundToInt(playerPos.z / tileSize)
        );
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && groundTilePrefab != null)
        {
            Gizmos.color = Color.green;
            float offset = (gridSize - 1) * tileSize * 0.5f;

            // Draw wire cubes to show the grid in the editor
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 position = new Vector3(
                        x * tileSize - offset,
                        groundHeight,
                        z * tileSize - offset
                    );
                    Gizmos.DrawWireCube(position, new Vector3(tileSize, 0.1f, tileSize));
                }
            }
        }
    }
}
