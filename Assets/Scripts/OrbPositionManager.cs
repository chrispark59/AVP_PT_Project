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
    [Tooltip("Distance in front of user to place the orb array (arm's reach distance in meters). Typical arm's reach is 0.5-0.7m")]
    [SerializeField] private float reachDistance = 0.6f;
    
    [Tooltip("Height offset from camera (negative = below eye level)")]
    [SerializeField] private float heightOffset = -0.2f;
    
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
        
        // Calculate target position in front of camera
        Vector3 targetPosition = cameraPos + forward * reachDistance;
        
        // Adjust height relative to camera (not absolute world Y)
        targetPosition.y = cameraPos.y + heightOffset;
        
        // Check if PositionArray has a parent with scale that might affect positioning
        if (positionArray.parent != null)
        {
            Vector3 parentScale = positionArray.parent.lossyScale;
            if (Mathf.Abs(parentScale.x - 1f) > 0.01f || Mathf.Abs(parentScale.y - 1f) > 0.01f || Mathf.Abs(parentScale.z - 1f) > 0.01f)
            {
                Debug.LogWarning($"OrbPositionManager: PositionArray has a parent with non-uniform scale {parentScale}. This may affect positioning!");
            }
        }
        
        // Set the position in world space
        positionArray.position = targetPosition;
        
        // Make array face the user
        if (faceUser)
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
        
        // Calculate actual distance for debugging
        float actualDistance = Vector3.Distance(cameraPos, positionArray.position);
        float horizontalDistance = Vector3.Distance(
            new Vector3(cameraPos.x, 0, cameraPos.z), 
            new Vector3(positionArray.position.x, 0, positionArray.position.z)
        );
        
        Debug.Log($"OrbPositionManager: Positioned array | reachDistance setting: {reachDistance}m | Actual 3D distance: {actualDistance:F3}m | Horizontal distance: {horizontalDistance:F3}m");
        
        // Warn if actual distance is very different from reachDistance
        if (Mathf.Abs(actualDistance - reachDistance) > 0.1f)
        {
            Debug.LogWarning($"OrbPositionManager: Actual distance ({actualDistance:F3}m) differs significantly from reachDistance setting ({reachDistance}m). Check for parent transforms or scale issues.");
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

