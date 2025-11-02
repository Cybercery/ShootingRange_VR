using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Target : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private int bodyHitPoints = 10;
    [SerializeField] private int weakpointHitPoints = 50;
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private float destroyDelay = 0.1f;

    [Header("Weak Point")]
    [SerializeField] private GameObject weakPoint;
    [SerializeField] private Color weakPointColor = new Color(1f, 0.9f, 0.2f); // Bright yellow
    [SerializeField] private bool pulseWeakPoint = true;
    [SerializeField] private float pulseSpeed = 3f;

    [Header("Visual Feedback")]
    [SerializeField] private Color bodyHitColor = Color.red;
    [SerializeField] private Color weakPointHitColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private GameObject bodyHitEffect; // Add this line
    [SerializeField] private GameObject destroyEffect;
    [SerializeField] private GameObject weakPointHitEffect;

    [Header("Enhanced Visuals")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private Color glowColor = new Color(0.2f, 0.6f, 1f); // Blue glow
    [SerializeField] private float glowIntensity = 1f;
    [SerializeField] private bool floatAnimation = true;
    [SerializeField] private float floatHeight = 0.2f;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private bool rotateTarget = true;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 30, 0);

    [Header("Movement")]
    [SerializeField] private bool moveTarget = false;
    [SerializeField] private Vector3 moveDirection = Vector3.right;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float moveDistance = 5f;

    [Header("Events")]
    public UnityEvent<int, bool> onTargetHit;

    private Renderer bodyRenderer;
    private Renderer weakPointRenderer;
    private Color originalBodyColor;
    private Material bodyMaterial;
    private Material weakPointMaterial;
    private bool isHit = false;
    private Vector3 startPosition;
    private float moveTimer = 0f;
    private Collider weakPointCollider;
    private float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
        bodyRenderer = GetComponent<Renderer>();

        if (bodyRenderer != null)
        {
            // Create material instance
            bodyMaterial = new Material(bodyRenderer.material);
            bodyRenderer.material = bodyMaterial;
            originalBodyColor = bodyMaterial.color;

            // Enable emission for glow effect
            if (enableGlow && bodyMaterial.HasProperty("_EmissionColor"))
            {
                bodyMaterial.EnableKeyword("_EMISSION");
                bodyMaterial.SetColor("_EmissionColor", glowColor * glowIntensity);
            }
        }

        // Setup weak point with enhanced visuals
        if (weakPoint != null)
        {
            weakPointRenderer = weakPoint.GetComponent<Renderer>();
            weakPointCollider = weakPoint.GetComponent<Collider>();

            if (weakPointRenderer != null)
            {
                weakPointMaterial = new Material(weakPointRenderer.material);
                weakPointRenderer.material = weakPointMaterial;
                weakPointMaterial.color = weakPointColor;

                if (weakPointMaterial.HasProperty("_EmissionColor"))
                {
                    weakPointMaterial.EnableKeyword("_EMISSION");
                    weakPointMaterial.SetColor("_EmissionColor", weakPointColor * 2f);
                }
            }
        }

        startPosition = transform.position;

        // Spawn animation
        StartCoroutine(SpawnAnimation());
    }

    IEnumerator SpawnAnimation()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = Vector3.zero;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Elastic ease out
            float scale = Mathf.Sin(t * Mathf.PI * 0.5f);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    void Update()
    {
        if (isHit) return;

        // Floating animation
        if (floatAnimation)
        {
            float floatOffset = Mathf.Sin((Time.time - spawnTime) * floatSpeed) * floatHeight;
            transform.position = new Vector3(
                transform.position.x,
                startPosition.y + floatOffset,
                transform.position.z
            );
        }

        // Rotation animation
        if (rotateTarget)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }

        // Target movement
        if (moveTarget)
        {
            moveTimer += Time.deltaTime * moveSpeed;
            float offset = Mathf.PingPong(moveTimer, moveDistance) - (moveDistance / 2f);
            Vector3 targetPos = startPosition + moveDirection.normalized * offset;
            targetPos.y = startPosition.y; // Preserve floating Y

            transform.position = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        }

        // Pulse weak point
        if (pulseWeakPoint && weakPointMaterial != null)
        {
            if (weakPointMaterial.HasProperty("_EmissionColor"))
            {
                float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
                float intensity = Mathf.Lerp(1.5f, 3f, pulse);
                weakPointMaterial.SetColor("_EmissionColor", weakPointColor * intensity);

                // Scale pulse
                float scalePulse = 1f + (pulse * 0.15f);
                weakPoint.transform.localScale = Vector3.one * scalePulse;
            }
        }

        // Body glow pulse
        if (enableGlow && bodyMaterial != null && bodyMaterial.HasProperty("_EmissionColor"))
        {
            float bodyPulse = (Mathf.Sin(Time.time * 2f) + 1f) / 2f;
            float intensity = Mathf.Lerp(glowIntensity * 0.5f, glowIntensity * 1.5f, bodyPulse);
            bodyMaterial.SetColor("_EmissionColor", glowColor * intensity);
        }
    }

    public void OnHit(Vector3 hitPoint)
    {
        if (isHit) return;

        bool hitWeakPoint = false;
        int points = bodyHitPoints;

        if (weakPoint != null && weakPointCollider != null)
        {
            Vector3 closestPoint = weakPointCollider.ClosestPoint(hitPoint);
            float distance = Vector3.Distance(hitPoint, closestPoint);

            if (distance < 0.1f)
            {
                hitWeakPoint = true;
                points = weakpointHitPoints;
            }
        }

        ProcessHit(hitPoint, hitWeakPoint, points);
    }

    void ProcessHit(Vector3 hitPoint, bool hitWeakPoint, int points)
    {
        isHit = true;

        // Award points
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(points, hitWeakPoint);
        }

        onTargetHit?.Invoke(points, hitWeakPoint);

        // Hit animation
        StartCoroutine(HitAnimation(hitWeakPoint, hitPoint));

        if (destroyOnHit)
        {
            if (destroyEffect != null)
            {
                Instantiate(destroyEffect, transform.position, Quaternion.identity);
            }
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            StartCoroutine(ResetTarget());
        }
    }

    IEnumerator HitAnimation(bool hitWeakPoint, Vector3 hitPoint)
    {
        // Flash effect
        Color hitColor = hitWeakPoint ? weakPointHitColor : bodyHitColor;

        if (bodyMaterial != null)
        {
            bodyMaterial.color = hitColor;
            if (bodyMaterial.HasProperty("_EmissionColor"))
            {
                bodyMaterial.SetColor("_EmissionColor", hitColor * 3f);
            }
        }

        if (hitWeakPoint)
        {
            if (weakPointMaterial != null)
            {
                weakPointMaterial.color = weakPointHitColor;
                if (weakPointMaterial.HasProperty("_EmissionColor"))
                {
                    weakPointMaterial.SetColor("_EmissionColor", weakPointHitColor * 5f);
                }
            }

            if (weakPointHitEffect != null)
            {
                Instantiate(weakPointHitEffect, hitPoint, Quaternion.identity);
            }
        }
        else
        {
            // Spawn body hit effect for regular hits
            if (bodyHitEffect != null)
            {
                Instantiate(bodyHitEffect, hitPoint, Quaternion.identity);
            }
        }

        // Recoil animation
        Vector3 originalPos = transform.position;
        Vector3 recoilDir = (transform.position - hitPoint).normalized;

        float elapsed = 0f;
        float duration = 0.15f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float recoil = Mathf.Sin(t * Mathf.PI) * 0.2f;
            transform.position = originalPos + (recoilDir * recoil);
            yield return null;
        }

        transform.position = originalPos;
    }

    IEnumerator ResetTarget()
    {
        yield return new WaitForSeconds(1f);

        if (bodyMaterial != null)
        {
            bodyMaterial.color = originalBodyColor;
            if (bodyMaterial.HasProperty("_EmissionColor"))
            {
                bodyMaterial.SetColor("_EmissionColor", glowColor * glowIntensity);
            }
        }

        if (weakPointMaterial != null)
        {
            weakPointMaterial.color = weakPointColor;
            if (weakPointMaterial.HasProperty("_EmissionColor"))
            {
                weakPointMaterial.SetColor("_EmissionColor", weakPointColor * 2f);
            }
        }

        isHit = false;
    }

    public void SetMovement(bool enabled, Vector3 direction, float speed, float distance)
    {
        moveTarget = enabled;
        moveDirection = direction;
        moveSpeed = speed;
        moveDistance = distance;
        startPosition = transform.position;
    }

    void OnDestroy()
    {
        // Cleanup materials
        if (bodyMaterial != null) Destroy(bodyMaterial);
        if (weakPointMaterial != null) Destroy(weakPointMaterial);
    }
}