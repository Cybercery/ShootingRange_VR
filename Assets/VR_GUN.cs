using UnityEngine;
using UnityEngine.XR;

public class VRGun : MonoBehaviour
{
    [Header("Gun Settings")]
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private LineRenderer laserSight;
    [SerializeField] private AudioSource shootSound;
    [SerializeField] private ParticleSystem hitEffect;

    [Header("Controller")]
    [SerializeField] private XRNode controllerNode = XRNode.RightHand;
    [SerializeField] private OVRInput.Controller ovrController = OVRInput.Controller.RTouch;

    [Header("Debug")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private bool debugLogs = true;

    private InputDevice controller;
    private float nextFireTime;
    private bool triggerPressed;

    void Start()
    {
        Debug.Log("=== VRGun Start - Initializing ===");
        controller = InputDevices.GetDeviceAtXRNode(controllerNode);
        Debug.Log($"Controller valid: {controller.isValid}");
        Debug.Log($"Target Layer Mask Value: {targetLayer.value}");

        if (laserSight != null)
        {
            laserSight.enabled = true;
            laserSight.positionCount = 2;
            Debug.Log("Laser sight configured");
        }
        else
        {
            Debug.LogWarning("Laser sight is NULL!");
        }

        if (muzzleFlash != null)
            muzzleFlash.SetActive(false);

        Debug.Log("=== VRGun initialized successfully ===");
    }

    void Update()
    {
        // Get controller if not found
        if (!controller.isValid)
            controller = InputDevices.GetDeviceAtXRNode(controllerNode);

        // Update laser sight
        UpdateLaserSight();

        // Check for trigger input
        bool triggerValue = GetTriggerPressed();

        // DEBUG: Log every frame to see if Update is running
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("UPDATE IS RUNNING - Space key down detected!");
        }

        if (triggerValue && !triggerPressed && Time.time >= nextFireTime)
        {
            Debug.Log("FIRING!");
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
        triggerPressed = triggerValue;
    }

    bool GetTriggerPressed()
    {
        // ALWAYS check keyboard/mouse first for testing
        if (Input.GetKey(KeyCode.Space))
        {
            Debug.Log(">>> SPACE KEY DETECTED <<<");
            return true;
        }

        if (Input.GetMouseButton(0))
        {
            Debug.Log(">>> MOUSE BUTTON DETECTED <<<");
            return true;
        }

        // Try OVR Input first (Meta XR SDK)
        try
        {
            if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, ovrController))
            {
                Debug.Log(">>> OVR TRIGGER DETECTED <<<");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("OVR Input not available: " + e.Message);
        }

        // Fallback to XR Input
        bool triggerValue;
        if (controller.TryGetFeatureValue(CommonUsages.triggerButton, out triggerValue))
        {
            if (triggerValue)
            {
                Debug.Log(">>> XR TRIGGER DETECTED <<<");
                return triggerValue;
            }
        }

        return false;
    }

    void UpdateLaserSight()
    {
        if (laserSight == null) return;

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        laserSight.SetPosition(0, transform.position);

        // Raycast with layer mask
        if (Physics.Raycast(ray, out hit, range, targetLayer))
        {
            laserSight.SetPosition(1, hit.point);

            // Draw green ray when hitting target layer
            if (showDebugRays)
                Debug.DrawRay(transform.position, hit.point - transform.position, Color.green);
        }
        else
        {
            laserSight.SetPosition(1, transform.position + transform.forward * range);

            // Draw red ray when not hitting
            if (showDebugRays)
                Debug.DrawRay(transform.position, transform.forward * range, Color.red);
        }
    }

    void Shoot()
    {
        Debug.Log("=== SHOOT CALLED ===");

        // Visual feedback
        if (muzzleFlash != null)
            StartCoroutine(ShowMuzzleFlash());

        if (shootSound != null)
            shootSound.Play();

        // Haptic feedback
        OVRInput.SetControllerVibration(0.3f, 0.3f, ovrController);
        controller.SendHapticImpulse(0, 0.3f, 0.1f);

        // Raycast
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        Debug.Log($"Raycast from: {transform.position}, Direction: {transform.forward}");
        Debug.Log($"Layer Mask: {targetLayer.value} (Binary: {System.Convert.ToString(targetLayer.value, 2)})");

        // First try WITHOUT layer mask to see if we hit anything at all
        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.Log($"HIT SOMETHING! Object: {hit.collider.gameObject.name}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            Debug.Log($"Hit point: {hit.point}, Distance: {hit.distance}");
        }
        else
        {
            Debug.Log("Raycast hit NOTHING at all!");
        }

        // Now try WITH layer mask
        if (Physics.Raycast(ray, out hit, range, targetLayer))
        {
            Debug.Log($"HIT TARGET LAYER! Object: {hit.collider.gameObject.name}");

            // Spawn hit effect
            if (hitEffect != null)
            {
                ParticleSystem effect = Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(effect.gameObject, 2f);
            }

            // Check for Target script on hit object OR parent
            Target target = hit.collider.GetComponent<Target>();
            if (target == null)
            {
                // Try getting from parent (in case we hit the weak point child)
                target = hit.collider.GetComponentInParent<Target>();
                Debug.Log("Checked parent for Target script: " + (target != null ? "FOUND" : "NOT FOUND"));
            }

            if (target != null)
            {
                Debug.Log("Target script found! Calling OnHit()");
                target.OnHit(hit.point);
            }
            else
            {
                Debug.LogWarning("Hit object on target layer but has NO Target script!");
            }
        }
        else
        {
            Debug.Log("Raycast with layer mask hit nothing.");
        }
    }

    System.Collections.IEnumerator ShowMuzzleFlash()
    {
        muzzleFlash.SetActive(true);
        yield return new WaitForSeconds(0.05f);
        muzzleFlash.SetActive(false);
    }

    // Visualize gun direction in Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * range);
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}