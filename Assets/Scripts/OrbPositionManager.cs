using UnityEngine;

/// <summary>
/// Positions the orb PositionArray in front of the user at arm's reach distance.
/// Should be attached to the PositionArray GameObject or a parent that contains it.
/// </summary>
public class OrbPositionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform positionArray; // The PositionArray empty object containing orbs
    [SerializeField] private Transform cameraTransform; // Main camera (head) transform
    
    [Header("Positioning Settings")]
    [Tooltip("Distance in front of user to place the orb array (arm's reach distance in meters)")]
    [SerializeField] private float reachDistance = 3f;
    
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
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
                Debug.LogWarning("OrbPositionManager: No camera assigned and no Camera.main found!", this);
        }
        
        // Auto-find PositionArray if not assigned
        if (positionArray == null)
        {
            positionArray = transform;
        }
        
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
    /// Positions the array in front of the user at arm's reach distance.
    /// </summary>
    public void PositionArray()
    {
        if (cameraTransform == null || positionArray == null)
            return;

        // Calculate position in front of camera at reach distance
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f; // Keep it horizontal
        forward.Normalize();
        
        Vector3 cameraPos = cameraTransform.position;
        Vector3 targetPosition = cameraPos + forward * reachDistance;
        targetPosition.y += heightOffset; // Adjust height
        
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

