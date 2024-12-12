using UnityEngine;
using System.Collections.Generic;

public class BeverageParticleSystem : MonoBehaviour
{
    [System.Serializable]
    public class BeverageParticle
    {
        public Sprite sprite;
        public string name = "Beverage";
        public Color tint = Color.white;
        public Vector2 sizeRange = new Vector2(0.3f, 0.5f);
    }

    [SerializeField] private BeverageParticle[] beverages;
    [SerializeField] private int maxParticles = 50;
    [SerializeField] private float spawnRate = 2f;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private Vector2 defaultSizeRange = new Vector2(0.3f, 0.5f);
    
    private new ParticleSystem particleSystem;
    private ParticleSystemRenderer psRenderer;

    private void Start()
    {
        StartCoroutine(WaitForCameraAndSetup());
    }

    private System.Collections.IEnumerator WaitForCameraAndSetup()
    {
        Debug.Log("Waiting for camera to be available...");
        while (Camera.main == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("Camera found, starting particle system setup");
        SetupParticleSystem();
        Debug.Log($"BeverageParticleSystem Setup Complete - Beverages count: {(beverages != null ? beverages.Length : 0)}");
        ValidateSetup();
    }

    private void ValidateSetup()
    {
        if (particleSystem == null)
        {
            Debug.LogError("Particle System is null after setup!");
            return;
        }

        Debug.Log($"Particle System Status: IsAlive={particleSystem.isPlaying}, IsEmitting={particleSystem.isEmitting}");
        Debug.Log($"Particle Count: {particleSystem.particleCount}");
        
        if (beverages == null || beverages.Length == 0)
        {
            Debug.LogError("No beverage sprites assigned!");
            return;
        }

        foreach (var beverage in beverages)
        {
            if (beverage.sprite == null)
            {
                Debug.LogError($"Null sprite found in beverages array!");
            }
        }

        if (psRenderer != null && psRenderer.material != null)
        {
            Debug.Log($"Renderer Material: {psRenderer.material.name}, Has Texture: {psRenderer.material.mainTexture != null}");
        }
        else
        {
            Debug.LogError("Particle System Renderer or its material is null!");
        }
    }

    private void SetupParticleSystem()
    {
        Debug.Log("Starting SetupParticleSystem");
        
        // Create and configure particle system
        particleSystem = gameObject.GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            Debug.Log("Creating new ParticleSystem component");
            particleSystem = gameObject.AddComponent<ParticleSystem>();
        }
        
        psRenderer = gameObject.GetComponent<ParticleSystemRenderer>();
        if (psRenderer == null)
        {
            Debug.LogError("ParticleSystemRenderer component not found!");
            return;
        }

        // Initialize sprites if available
        if (beverages != null && beverages.Length > 0)
        {
            SetBeverageSprites(beverages);
        }

        var main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(defaultSizeRange.x, defaultSizeRange.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(-90f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 7f);
        main.gravityModifier = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = Color.white;

        var emission = particleSystem.emission;
        emission.rateOverTime = spawnRate;

        // Calculate screen width in world units at spawn height
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Calculate spawn area based on camera viewport
            float spawnHeight = mainCamera.orthographicSize; // Use this for top of screen
            Vector3 topLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 1, 10));
            Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 10));
            float cameraWidth = Vector3.Distance(topLeft, topRight);
            
            Debug.Log($"Spawn Configuration - Width: {cameraWidth}, Height: {spawnHeight}");
            Debug.Log($"Screen Points - TopLeft: {topLeft}, TopRight: {topRight}");
            
            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            shape.scale = new Vector3(cameraWidth, 0.1f, 1f);
            shape.position = new Vector3(0f, topLeft.y, 0f);
            shape.rotation = new Vector3(0f, 0f, 0f);
            shape.randomDirectionAmount = 0.1f;
            shape.randomPositionAmount = 1f;
            
            // Configure initial velocity
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-1f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);
        }

        var rotationOverLifetime = particleSystem.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = rotationSpeed * Mathf.Deg2Rad;

        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.8f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = gradient;

        // Configure renderer
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.sortMode = ParticleSystemSortMode.YoungestInFront;
        psRenderer.material = new Material(Shader.Find("Sprites/Default"));
        psRenderer.material.mainTexture = beverages[0].sprite.texture;
        
        // Setup texture sheet animation
        var textureSheet = particleSystem.textureSheetAnimation;
        textureSheet.enabled = true;
        textureSheet.mode = ParticleSystemAnimationMode.Grid;
        textureSheet.numTilesX = beverages.Length;
        textureSheet.numTilesY = 1;
        textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
        textureSheet.timeMode = ParticleSystemAnimationTimeMode.Lifetime;
        textureSheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f);
        textureSheet.startFrame = new ParticleSystem.MinMaxCurve(0f);
    }

    public void SetBeverageSprites(BeverageParticle[] newBeverages)
    {
        if (newBeverages == null || newBeverages.Length == 0)
            return;

        beverages = newBeverages;
        
        // Update material texture
        if (psRenderer != null && beverages[0].sprite != null)
        {
            psRenderer.material.mainTexture = beverages[0].sprite.texture;
        }

        // Update particle system settings
        if (particleSystem != null)
        {
            var textureSheet = particleSystem.textureSheetAnimation;
            textureSheet.enabled = true;
            textureSheet.mode = ParticleSystemAnimationMode.Grid;
            textureSheet.numTilesX = beverages.Length;
            textureSheet.numTilesY = 1;
            textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
            textureSheet.cycleCount = 1;
            textureSheet.timeMode = ParticleSystemAnimationTimeMode.Lifetime;

            var mainModule = particleSystem.main;
            mainModule.startColor = new ParticleSystem.MinMaxGradient(beverages[0].tint);
            // Use beverage-specific size range if set, otherwise use default
            Vector2 sizeRange = beverages[0].sizeRange != Vector2.zero ? 
                              beverages[0].sizeRange : 
                              defaultSizeRange;
            mainModule.startSize = new ParticleSystem.MinMaxCurve(
                sizeRange.x,
                sizeRange.y
            );
        }
    }
}
