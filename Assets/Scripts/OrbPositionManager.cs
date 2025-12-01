using UnityEngine;

/*
script in charge of positioning the orb array  in front of the user at arm's reach distance 
*/
public class OrbPositionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform positionArray; // The PositionArray empty object containing orbs
    [SerializeField] private Transform cameraTransform; // Main camera (head) transform
    
    [Header("Positioning Settings")]
    [Tooltip("Distance in front of user to place the orb array (arm's reach distance in meters). Typical arm's reach is 0.5-0.7m. WARNING: This value is in METERS, not scaled units!")]
    [SerializeField] private float reachDistance = 0.6f;
    
    [Tooltip("Clamp reachDistance to reasonable values (0.1m to 2m) to prevent accidental huge distances")]
    [SerializeField] private bool clampDistance = true;
    
    [Tooltip("Height offset from camera (negative = below eye level)")]
    [SerializeField] private float heightOffset = -0.2f;
    
    [Tooltip("Right offset from camera in meters (positive = shift entire array to the right, negative = shift to the left). Use 0.1-0.3 for noticeable shift.")]
    [SerializeField] private float rightOffset = 0.15f;
    
    [Tooltip("Should the array face the user?")]
    [SerializeField] private bool faceUser = true;
    
    [Tooltip("Update position every frame (for dynamic positioning)")]
    [SerializeField] private bool updateContinuously = false;

    private void Start()
    {
        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            // Try Camera.main first
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                Debug.Log($"OrbPositionManager: Auto-found camera: {cameraTransform.name}");
            }
            else
            {
                // Try to find any camera in the scene
                Camera foundCamera = FindFirstObjectByType<Camera>();
                if (foundCamera != null)
                {
                    cameraTransform = foundCamera.transform;
                    Debug.Log($"OrbPositionManager: Found camera: {cameraTransform.name}");
                }
                else
                {
                    Debug.LogError("OrbPositionManager: No camera found! Please assign cameraTransform in inspector.", this);
                    return;
                }
            }
        }
        
        // Auto-find PositionArray if not assigned
        if (positionArray == null)
        {
            positionArray = transform;
            Debug.Log($"OrbPositionManager: Using self as PositionArray: {positionArray.name}");
        }
        
        // Position the array
        PositionArray();
    }

    private void Update()
    {
        if (updateContinuously)
        {
            PositionArray();
        }
    }

    /// <summary>
    /// Called when values change in the inspector (including during play mode).
    /// This allows real-time updates when adjusting reachDistance.
    /// </summary>
    private void OnValidate()
    {
        // Only update if we're in play mode and references are set
        if (Application.isPlaying && cameraTransform != null && positionArray != null)
        {
            PositionArray();
        }
    }

    /// <summary>
    /// Positions the array in front of the user at arm's reach distance.
    /// </summary>
    public void PositionArray()
    {
        if (cameraTransform == null || positionArray == null)
        {
            Debug.LogWarning($"OrbPositionManager: Missing references! Camera: {cameraTransform != null}, Array: {positionArray != null}", this);
            return;
        }

        // Clamp reachDistance to prevent accidental huge values
        float effectiveDistance = reachDistance;
        if (clampDistance)
        {
            effectiveDistance = Mathf.Clamp(reachDistance, 0.1f, 2.0f);
            if (effectiveDistance != reachDistance)
            {
                Debug.LogWarning($"OrbPositionManager: reachDistance ({reachDistance}m) was clamped to {effectiveDistance}m. Consider setting a value between 0.5-0.7m for arm's reach.");
            }
        }

        // Check for scale issues on camera and PositionArray
        Vector3 cameraScale = cameraTransform.lossyScale;
        Vector3 arrayParentScale = positionArray.parent != null ? positionArray.parent.lossyScale : Vector3.one;
        
        if (Mathf.Abs(cameraScale.x - 1f) > 0.01f || Mathf.Abs(cameraScale.y - 1f) > 0.01f || Mathf.Abs(cameraScale.z - 1f) > 0.01f)
        {
            Debug.LogWarning($"OrbPositionManager: Camera has scale {cameraScale}! This will affect distance calculations. Consider using a scale of 1,1,1.");
        }

        // Get camera position and forward direction in world space
        Vector3 cameraPos = cameraTransform.position;
        Vector3 forward = cameraTransform.forward;
        
        // Flatten forward to horizontal plane (keep Y at 0 relative to camera)
        forward.y = 0f;
        
        // If forward is too small (looking straight up/down), use a default forward
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = cameraTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward; // Fallback
            }
        }
        
        forward.Normalize();
        
        // Get camera's right direction for horizontal offset (full array shift)
        Vector3 right = cameraTransform.right;
        right.y = 0f; // Keep it horizontal
        if (right.sqrMagnitude < 0.01f)
        {
            // Fallback if right vector is invalid
            right = Vector3.Cross(forward, Vector3.up).normalized;
        }
        else
        {
            right.Normalize();
        }
        
        // Calculate target position in front of camera
        // Use the effective distance (clamped if needed)
        Vector3 targetPosition = cameraPos + forward * effectiveDistance;
        
        // Apply horizontal (right) offset to shift the entire array
        if (Mathf.Abs(rightOffset) > 0.001f)
        {
            targetPosition += right * rightOffset;
        }
        
        // Adjust height relative to camera (not absolute world Y)
        targetPosition.y = cameraPos.y + heightOffset;
        
        // Check if PositionArray has a parent with scale that might affect positioning
        if (positionArray.parent != null)
        {
            if (Mathf.Abs(arrayParentScale.x - 1f) > 0.01f || Mathf.Abs(arrayParentScale.y - 1f) > 0.01f || Mathf.Abs(arrayParentScale.z - 1f) > 0.01f)
            {
                Debug.LogWarning($"OrbPositionManager: PositionArray has a parent with scale {arrayParentScale}. Compensating by dividing effectiveDistance by average scale.");
                // Compensate for parent scale
                float avgScale = (arrayParentScale.x + arrayParentScale.y + arrayParentScale.z) / 3f;
                targetPosition = cameraPos + forward * (effectiveDistance / avgScale);
                targetPosition.y = cameraPos.y + heightOffset;
            }
        }
        
        // Set the position in world space
        positionArray.position = targetPosition;
        
        // Only update rotation if faceUser is enabled AND we're not updating continuously
        // (continuous rotation updates can cause jittery effects)
        if (faceUser && !updateContinuously)
        {
            Vector3 directionToUser = cameraPos - targetPosition;
            directionToUser.y = 0f; // Keep rotation horizontal
            if (directionToUser.sqrMagnitude > 0.01f)
            {
                positionArray.rotation = Quaternion.LookRotation(directionToUser);
            }
            else
            {
                // Fallback: face opposite of forward direction
                positionArray.rotation = Quaternion.LookRotation(-forward);
            }
        }
        // If faceUser is disabled or updateContinuously is on, keep rotation stable
        // (Don't reset to identity if it was already set, just don't update it)
        
        // Calculate actual distance for debugging
        float actualDistance = Vector3.Distance(cameraPos, positionArray.position);
        float horizontalDistance = Vector3.Distance(
            new Vector3(cameraPos.x, 0, cameraPos.z), 
            new Vector3(positionArray.position.x, 0, positionArray.position.z)
        );
        
        Debug.Log($"OrbPositionManager: Positioned array | reachDistance setting: {reachDistance}m | Effective distance used: {effectiveDistance}m | Actual 3D distance: {actualDistance:F3}m | Horizontal distance: {horizontalDistance:F3}m | Camera scale: {cameraScale}");
        
        // Warn if reachDistance is too large (common mistake)
        if (reachDistance > 1.0f)
        {
            Debug.LogWarning($"OrbPositionManager: reachDistance is set to {reachDistance}m which is very far! For arm's reach, use 0.5-0.7m. Current setting will place orbs {reachDistance}m away.");
        }
        
        // Warn if actual distance is very different from effective distance
        float distanceDifference = Mathf.Abs(actualDistance - effectiveDistance);
        if (distanceDifference > 0.1f)
        {
            float ratio = actualDistance / effectiveDistance;
            Debug.LogWarning($"OrbPositionManager: Actual distance ({actualDistance:F3}m) differs from effective distance ({effectiveDistance}m) by {distanceDifference:F3}m (ratio: {ratio:F2}x). " +
                           $"Camera scale: {cameraScale}, PositionArray parent scale: {arrayParentScale}");
        }
    }

    /// <summary>
    /// Call this to manually reposition the array (e.g., when exercise starts).
    /// </summary>
    public void RepositionArray()
    {
        PositionArray();
    }
}

