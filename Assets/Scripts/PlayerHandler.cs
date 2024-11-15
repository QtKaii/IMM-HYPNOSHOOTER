using UnityEngine;
using System.Collections;

public class PlayerHandler : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    [SerializeField] private float dashDistance = 3f;
    [SerializeField] private float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Combat")]
    public int maxAmmo = 5;
    public float reloadTime = 1.5f;
    public float shootCooldown = 0.2f;
    [SerializeField] private int bulletsPerShot = 3;
    [SerializeField] private float spreadAngle = 15f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Health")]
    public int maxHealth = 10;
    [HideInInspector]
    public int currentHealth;

    public float lastDashTime = -999f;
    public bool isReloading;
    public int currentAmmo;
    public float reloadStartTime;

    private float lastShotTime;
    private bool isDashing;
    private Camera mainCamera;
    private Rigidbody rb;

    private void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        currentAmmo = maxAmmo;
        currentHealth = maxHealth;

        // Create default fire point
        if (firePoint == null)
        {
            GameObject defaultFirePoint = new GameObject("DefaultFirePoint");
            defaultFirePoint.transform.parent = transform;
            defaultFirePoint.transform.localPosition = new Vector3(0.5f, 0, 0);
            firePoint = defaultFirePoint.transform;
        }
    }

    private void HandleMovement()
    {
        // Get input for horizontal and vertical movement
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // Create movement vector
        Vector3 movement = new Vector3(horizontalInput, 0, verticalInput).normalized;

        // Move the player
        rb.linearVelocity = movement * moveSpeed;
    }

    private void HandleAiming()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPosition = ray.GetPoint(distance);
            Vector3 aimDirection = (mouseWorldPosition - transform.position).normalized;

            // create a rotation that only changes around the Y-axis
            Quaternion targetRotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
        }
    }

    private void HandleShooting()
    {
        // Reload logic
        if (isReloading)
        {
            if (Time.time >= reloadStartTime + reloadTime)
            {
                isReloading = false;
                currentAmmo = maxAmmo;
                Debug.Log("Reloading completed.");
            }
            return;
        }

        // Shooting logic
        if (Input.GetButton("Fire1") && Time.time >= lastShotTime + shootCooldown && currentAmmo > 0)
        {
            FireMultishot();
            currentAmmo--;
            lastShotTime = Time.time;
            Debug.Log($"Shot fired. Ammo remaining: {currentAmmo}/{maxAmmo}");
        }

        // Reload trigger
        if (Input.GetKeyDown(KeyCode.R) || (Input.GetButton("Fire1") && currentAmmo == 0))
        {
            StartReload();
        }
    }

    private void StartReload()
    {
        if (!isReloading && currentAmmo < maxAmmo)
        {
            isReloading = true;
            reloadStartTime = Time.time;
            Debug.Log("Reloading started.");
        }
    }

    private void FireMultishot()
    {
        float startAngle = -spreadAngle * (bulletsPerShot - 1) / 2f;

        GameObject prefabToUse = bulletPrefab;

        for (int i = 0; i < bulletsPerShot; i++)
        {
            float angle = startAngle + spreadAngle * i;
            Quaternion rotation = firePoint.rotation * Quaternion.Euler(0, angle, 0);
            GameObject bullet = Instantiate(prefabToUse, firePoint.position, rotation);

            // Set the owner to the player
            BulletScript bulletScript = bullet.GetComponent<BulletScript>();
            if (bulletScript != null)
            {
                bulletScript.SetOwner(gameObject);
                Debug.Log("Bullet instantiated and owner set.");
            }

            bullet.SetActive(true);
        }
    }

    private void HandleDash()
    {
        if (!Input.GetKeyDown(KeyCode.Space) || Time.time < lastDashTime + dashCooldown) return;

        Vector3 dashDirection;
        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        if (moveInput.sqrMagnitude > 0.1f)
        {
            dashDirection = moveInput.normalized;
        }
        else
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 mouseWorldPosition = ray.GetPoint(distance);
                dashDirection = (mouseWorldPosition - transform.position).normalized;
            }
            else
            {
                dashDirection = transform.forward;
            }
        }

        StartCoroutine(PerformDash(dashDirection));
        lastDashTime = Time.time;
        Debug.Log("Dash performed.");
    }

    private IEnumerator PerformDash(Vector3 direction)
    {
        isDashing = true;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + direction * dashDistance;
        float elapsed = 0f;

        // Store initial rotation
        Quaternion startRotation = transform.rotation;

        // Calculate tilt rotation (tilting sideways towards dash direction)
        Vector3 tiltAxis = Vector3.Cross(Vector3.up, direction);
        Quaternion tiltRotation = Quaternion.AngleAxis(-15f, tiltAxis) * startRotation;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashDuration;

            // Interpolate position
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, progress));

            // Interpolate rotation for tilt effect
            transform.rotation = Quaternion.Slerp(startRotation, tiltRotation, Mathf.SmoothStep(0f, 1f, progress));

            yield return null;
        }

        // Smoothly return to original rotation
        float returnElapsed = 0f;
        while (returnElapsed < 0.2f) 
        {
            returnElapsed += Time.deltaTime;
            float returnProgress = returnElapsed / 0.2f;
            transform.rotation = Quaternion.Slerp(tiltRotation, startRotation, Mathf.SmoothStep(0f, 1f, returnProgress));
            yield return null;
        }

        isDashing = false;
        Debug.Log("Dash completed.");
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage. Current Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Debug.Log("Player has died. Game Over.");
            GameManager.Instance.GameOver();
        }
    }

    private void Update()
    {
        if (!isDashing)
        {
            HandleMovement();
            HandleAiming();
            HandleShooting();
            HandleDash();
        }
    }

    // Utility methods
    public float GetHealthRatio()
    {
        return (float)currentHealth / maxHealth;
    }

    public float GetLastDashTime()
    {
        return lastDashTime;
    }

    public float GetDashCooldown()
    {
        return dashCooldown;
    }
}