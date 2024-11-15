using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float baseSpeed = 3f;
    [SerializeField] private float preferredRange = 5f;
    [SerializeField] private float stoppingDistance = 4f;
    
    [Header("Attack")]
    [SerializeField] private float beamWidth = 0.2f;
    [SerializeField] private float beamMaxLength = 10f;
    [SerializeField] private Color beamColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float beamChargeTime = 3f;
    [SerializeField] private float postAttackDelay = 1f;
    
     [Header("Health")]
    public int health = 10;
    [SerializeField] private float healthBarHeight = 10f;
    [SerializeField] private float healthBarWidth = 100f;
    [SerializeField] private Color healthBarColor = Color.green;
    [SerializeField] private Color healthBarBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    [SerializeField] private GameObject bulletPrefab;

    private float speedMultiplier = 1f;
    private Transform player;
    private Rigidbody rb;
    private LineRenderer beamRenderer;
    private bool isBeamActive;
    private bool isCharging;
    private float nextBeamDamageTime;
    private float beamDamageInterval = 0.1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Configure Rigidbody
        if (rb != null)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        SetupBeamRenderer();
    }

    private void Update()
    {
        if (player == null || isCharging) return;

        Vector3 directionToPlayer = (player.position - transform.position);
        float distanceToPlayer = directionToPlayer.magnitude;
        Vector3 normalizedDirection = directionToPlayer.normalized;

        // movement behavior
        if (distanceToPlayer > preferredRange)
        {
            // Move towards player
            rb.linearVelocity = normalizedDirection * baseSpeed * speedMultiplier;
        }
        else if (distanceToPlayer < stoppingDistance)
        {
            // Back away if too close
            rb.linearVelocity = -normalizedDirection * baseSpeed * speedMultiplier * 0.5f;
        }
        else
        {
            // Stop and strt attack sequence
            rb.linearVelocity = Vector3.zero;
            StartCoroutine(AttackSequence());
        }

        // Update rotation to face player
        if (!isCharging)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
        }
    }

    private IEnumerator AttackSequence()
    {
        if (isCharging) yield break; // Prevent multiple attack sequences

        isCharging = true;
        
        // Stop movement but maintain position
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true; // prevent any weird physics from affecting enemy during attack
        
        // Start beam attack
        FireBeam();
        
        // Keep updating rotation and beam during charge
        float chargeEndTime = Time.time + beamChargeTime;
        while (Time.time < chargeEndTime && player != null)
        {
            // Makes sure enemy is always facing player
            Vector3 directionToPlayer = (player.position - transform.position);
            transform.rotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
            
            // Update beam position
            FireBeam();
            
            yield return null;
        }
        
        // Stop beam and fire bullet
        StopBeam();
        FireBullet();
        
        // Wait post-attack delay
        yield return new WaitForSeconds(postAttackDelay);
        
        // Resume normal physics
        rb.isKinematic = false;
        isCharging = false;
    }

    private void FireBullet()
    {
        if (player == null || bulletPrefab == null) return;

        
        GameObject bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
        bullet.transform.localScale *= 1.5f; //slighty increase size of bullet
        
        // Ensure the bullet is active and facing the right direction
        bullet.SetActive(true);

        // Set the owner of the bullet to this enemy
        BulletScript bulletComponent = bullet.GetComponent<BulletScript>();
        if (bulletComponent != null)
        {
            bulletComponent.SetOwner(gameObject);
            Debug.Log("Bullet instantiated and owner set.");
        }
    }

    private void SetupBeamRenderer()
    {
        beamRenderer = gameObject.AddComponent<LineRenderer>();
        beamRenderer.startWidth = beamWidth;
        beamRenderer.endWidth = beamWidth;
        beamRenderer.material = new Material(Shader.Find("Sprites/Default"));
        beamRenderer.startColor = beamColor;
        beamRenderer.endColor = beamColor;
        beamRenderer.positionCount = 2;
        beamRenderer.enabled = false;
    }

    private void FireBeam()
    {
        if (!isBeamActive)
        {
            isBeamActive = true;
            beamRenderer.enabled = true;
        }

        Vector3 beamStart = transform.position;
        Vector3 beamDirection = transform.forward;
        
        RaycastHit hit;
        if (Physics.Raycast(beamStart, beamDirection, out hit, beamMaxLength))
        {
            Vector3 beamEnd = hit.point;

            beamRenderer.SetPosition(0, beamStart);
            beamRenderer.SetPosition(1, beamEnd);

            // Only apply damage if we hit the player ignore other enemies
            if (hit.collider.CompareTag("Player") && 
                Time.time >= nextBeamDamageTime)
            {
                PlayerHandler playerComponent = hit.collider.GetComponent<PlayerHandler>();
                if (playerComponent != null)
                {
                    playerComponent.TakeDamage(1);
                    nextBeamDamageTime = Time.time + beamDamageInterval;
                }
            }
        }
        else
        {
            Vector3 beamEnd = beamStart + beamDirection * beamMaxLength;
            beamRenderer.SetPosition(0, beamStart);
            beamRenderer.SetPosition(1, beamEnd);
        }
    }

    private void StopBeam()
    {
        if (isBeamActive)
        {
            isBeamActive = false;
            beamRenderer.enabled = false;
        }
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            BulletScript bullet = other.GetComponent<BulletScript>();
            if (bullet != null && bullet.GetOwner() != gameObject)  // Only take damage if not hit by own bullet
            {
                health -= bullet.GetDamage();
                Debug.Log($"Enemy took {bullet.GetDamage()} damage. Current Health: {health}");

                if (health <= 0)
                {
                    // Add score when enemy is defeated
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddScore(10);
                        Debug.Log("Enemy defeated. Score added.");
                    }

                    Destroy(other.gameObject);
                    Destroy(gameObject);
                }
            }
        }
    }

    private void OnGUI()
    {
        // Render health bar
        Vector3 healthBarPosition = Camera.main.WorldToScreenPoint(transform.position);
        healthBarPosition.y = Screen.height - healthBarPosition.y; 

        GUI.color = healthBarBackgroundColor;
        GUI.DrawTexture(new Rect(healthBarPosition.x - healthBarWidth / 2, healthBarPosition.y - healthBarHeight - 10, healthBarWidth, healthBarHeight), Texture2D.whiteTexture);

        GUI.color = healthBarColor;
        GUI.DrawTexture(new Rect(healthBarPosition.x - healthBarWidth / 2, healthBarPosition.y - healthBarHeight - 10, healthBarWidth * ((float)health / 10), healthBarHeight), Texture2D.whiteTexture);
    }

    private void OnDestroy()
    {
        StopBeam();
    }
}