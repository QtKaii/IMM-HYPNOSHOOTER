using UnityEngine;

public class BulletScript : MonoBehaviour
{
    [SerializeField] protected float speed = 15f;
    [SerializeField] protected float lifetime = 2f;
    [SerializeField] protected int damage = 1;
    
    protected Rigidbody rb;
    protected Collider physicsCollider;
    protected Collider triggerCollider;
    protected GameObject owner;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        physicsCollider = GetComponent<Collider>();
        
        // Ensure the bullet has a Rigidbody
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
        }
        
        // Add a separate trigger collider if not already present
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null || !triggerCollider.isTrigger)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
        }
    }

    protected virtual void Start()
    {
        // Set initial velocity based on the bullet direction
        rb.linearVelocity = transform.forward * speed;
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // Null checks to prevent NullReferenceException
        if (other == null || other.gameObject == null)
            return;

        // Ignore collision with owner
        if (owner != null && other.gameObject == owner)
            return;

        // Ignore friendly fire
        if ((owner != null && other.CompareTag("Enemy")) || (owner == null && other.CompareTag("Player")))
            return;

        // Handle collision with valid targets
        if ((owner == null && other.CompareTag("Enemy")) || (owner != null && other.CompareTag("Player")))
        {
            // Apply damage to the hit object
            PlayerHandler playerHandler = other.GetComponent<PlayerHandler>();
            if (playerHandler != null && owner != other.gameObject)
            {
                playerHandler.TakeDamage(damage);
                Debug.Log("Player hit by bullet.");
            }

            Destroy(gameObject);
        }
    }

    protected virtual void OnBecameInvisible()
    {
        Destroy(gameObject);
    }

    public virtual int GetDamage()
    {
        return damage;
    }

    public void SetOwner(GameObject ownerObject)
    {
        owner = ownerObject;
    }

    public GameObject GetOwner()
    {
        return owner;
    }

    private bool IsOffScreen()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(transform.position);
        return viewportPosition.x < 0f || viewportPosition.x > 1f || 
               viewportPosition.y < 0f || viewportPosition.y > 1f;
    }
}