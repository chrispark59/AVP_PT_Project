using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/*
Estimating ROM based off of distance.
We:
- Estimate a shoulder position (either from a provided Transform or from head offsets)
- Shoot a "virtual ray" from the shoulder in the camera's forward direction,
  perpendicular to the camera view plane, with length = arm length
- Use the END of that ray as the full-reach reference point
- Measure the distance between the hand and that endpoint
*/

namespace PolySpatial.Samples
{
    public class PinchDistanceDisplay : MonoBehaviour
    {
        [Header("Input Actions (from your Input Action asset)")]
        [SerializeField]
        private InputActionReference m_TouchZeroValue;   // SpatialPointerState

        [SerializeField]
        private InputActionReference m_TouchZeroPhase;   // TouchPhase

        [Header("Camera / Head Reference")]
        [SerializeField]
        private Transform m_HeadTransform;               // Usually the Main Camera in the PolySpatial rig

        [Header("UI")]
        [SerializeField]
        private TMP_Text reps_Text;                 // TextMeshPro label to show the distance

        [SerializeField]
        private TMP_Text QualityText;

        [Tooltip("Optional: DistanceColorLerp component to update color based on distance")]
        [SerializeField]
        private DistanceColorLerp m_DistanceColorLerp;

        [Header("Shoulder / Arm Estimation")]
        [Tooltip("Optional: if assigned, this is used as the shoulder position. If null, we estimate from head.")]
        [SerializeField]
        private Transform m_ShoulderTransform;

        [Tooltip("Is this the right arm? If false, we mirror offsets for the left arm.")]
        [SerializeField]
        private bool m_IsRightArm = true;

        [Tooltip("Sideways offset from head to shoulder in meters.")]
        [SerializeField]
        private float m_ShoulderSideOffset = 0.18f;

        [Tooltip("Downward offset from head to shoulder in meters.")]
        [SerializeField]
        private float m_ShoulderDownOffset = 0.15f;

        [Tooltip("Backward offset from head to shoulder in meters.")]
        [SerializeField]
        private float m_ShoulderBackOffset = 0.05f;

        [Tooltip("Approximate arm length in meters for forward reach.")]
        [SerializeField]
        private float m_ArmLength = 1.2f;

        [Header("Formatting")]
        [SerializeField]
        private string m_DistanceFormat = "0.00";        // e.g. "1.23"

        [Header("Circle Tracking")]
        [Tooltip("Enable angle-based circle tracking for arm circles")]
        [SerializeField]
        private bool m_EnableCircleTracking = true;

        [Tooltip("Minimum distance from shoulder to start tracking circles (prevents noise when arm is close)")]
        [SerializeField]
        private float m_MinCircleTrackingDistance = 0.3f;

        [Tooltip("Minimum angle change per frame to count (filters out noise)")]
        [SerializeField]
        private float m_MinAngleDelta = 0.5f;

        [Tooltip("Enable debug logging for angle tracking")]
        [SerializeField]
        private bool m_DebugAngleTracking = false;

        // Circle tracking state
        private float m_AccumulatedAngle = 0f;
        private int m_CircleReps = 0;
        private Vector3 m_LastHandDirection = Vector3.zero;
        private bool m_IsTrackingCircle = false;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();

            if (m_TouchZeroValue != null)
                m_TouchZeroValue.action.Enable();

            if (m_TouchZeroPhase != null)
                m_TouchZeroPhase.action.Enable();

            // temp fallback if not assigned
            if (m_HeadTransform == null && Camera.main != null)
                m_HeadTransform = Camera.main.transform;
        }

        private void OnDisable()
        {
            if (m_TouchZeroValue != null)
                m_TouchZeroValue.action.Disable();

            if (m_TouchZeroPhase != null)
                m_TouchZeroPhase.action.Disable();
        }

        // Store last known values for display when not tracking
        private float _lastGapToFullReach = 0f;
        private bool _hasTrackedOnce = false;

        private void Update()
        {
            if (m_HeadTransform == null || reps_Text == null)
            {
                return;
            }

            // Always show reps text if circle tracking is enabled
            if (m_EnableCircleTracking && reps_Text != null)
            {
                reps_Text.text = $"Reps: {m_CircleReps}";
            }

            // Always show quality/ROM text
            if (QualityText != null)
            {
                if (m_EnableCircleTracking)
                {
                    // Show ROM distance in quality text when tracking circles
                    if (_hasTrackedOnce)
                    {
                        QualityText.text = $"ROM: {_lastGapToFullReach.ToString(m_DistanceFormat)} m";
                    }
                    else
                    {
                        QualityText.text = "ROM: --";
                    }
                }
                else
                {
                    if (_hasTrackedOnce)
                    {
                        // Simple ROM quality based on how close to "full reach" they are
                        if (_lastGapToFullReach < 0.3f)
                            QualityText.text = "ROM: Excellent";
                        else if (_lastGapToFullReach < 0.6f)
                            QualityText.text = "ROM: Good";
                        else
                            QualityText.text = "ROM: Needs Improvement";
                    }
                    else
                    {
                        QualityText.text = "ROM: --";
                    }
                }
            }

            if (m_TouchZeroValue == null || m_TouchZeroPhase == null)
            {
                return;
            }

            // Read the current pointer state + phase
            var touchState = m_TouchZeroValue.action.ReadValue<SpatialPointerState>();
            var touchPhase = m_TouchZeroPhase.action.ReadValue<TouchPhase>();

            if (touchPhase == TouchPhase.Began ||
                touchPhase == TouchPhase.Moved ||
                touchPhase == TouchPhase.Stationary)
            {
                // World-space position of the pinch / interaction (hand)
                Vector3 handPosition = touchState.interactionPosition;

                // 1. Estimate shoulder position
                Vector3 shoulderPos = GetEstimatedShoulderPosition();

                // 2. Compute a full-length "arm ray" endpoint in front of the person
                Vector3 fullReachEndPoint = GetArmFullReachEndPoint(shoulderPos);

                // 3. Distances
                float shoulderToHandDistance = Vector3.Distance(shoulderPos, handPosition);        // raw arm length right now
                float gapToFullReach = Vector3.Distance(fullReachEndPoint, handPosition) - 0.15f;   // how far from ideal full extension

                // Store for display when not tracking
                _lastGapToFullReach = gapToFullReach;
                _hasTrackedOnce = true;

                // 4. Circle tracking (angle-based)
                if (m_EnableCircleTracking)
                {
                    UpdateCircleTracking(shoulderPos, handPosition, shoulderToHandDistance);
                    // Update reps text immediately after tracking (in case a circle just completed)
                    reps_Text.text = $"Reps: {m_CircleReps}";
                }
                else
                {
                    reps_Text.text = $"ROM: {gapToFullReach.ToString(m_DistanceFormat)} m";
                }

                // Update quality text with current values
                if (QualityText != null)
                {
                    if (m_EnableCircleTracking)
                    {
                        // Show ROM distance in quality text when tracking circles
                        QualityText.text = $"ROM: {gapToFullReach.ToString(m_DistanceFormat)} m";
                    }
                    else
                    {
                        // Simple ROM quality based on how close to "full reach" they are
                        if (gapToFullReach < 0.3f)
                            QualityText.text = "ROM: Excellent";
                        else if (gapToFullReach < 0.6f)
                            QualityText.text = "ROM: Good";
                        else
                            QualityText.text = "ROM: Needs Improvement";
                    }
                }

                // Update color lerp based on distance (0.6 is optimal)
                if (m_DistanceColorLerp != null)
                {
                    m_DistanceColorLerp.SetDistance(gapToFullReach);
                }
            }
            else
            {
                // Don't reset circle tracking when not pinching - allow continuous tracking
                // Only reset if you want to require pinching for tracking
                // if (m_EnableCircleTracking)
                // {
                //     m_IsTrackingCircle = false;
                //     m_LastHandDirection = Vector3.zero;
                // }
            }
        }

        /// <summary>
        /// Returns an estimated shoulder world position.
        /// If a shoulder Transform is assigned, use that.
        /// Otherwise, estimate from head using local offsets.
        /// </summary>
        private Vector3 GetEstimatedShoulderPosition()
        {
            if (m_ShoulderTransform != null)
                return m_ShoulderTransform.position;

            if (m_HeadTransform == null)
                return Vector3.zero;

            float side = m_IsRightArm ? 1f : -1f;

            Vector3 localOffset = new Vector3(
                side * m_ShoulderSideOffset,
                -m_ShoulderDownOffset,
                -m_ShoulderBackOffset
            );

            return m_HeadTransform.TransformPoint(localOffset);
        }

        /// <summary>
        /// Computes the endpoint of a virtual arm-length ray starting at the shoulder,
        /// aligned with the user's viewing direction (perpendicular to the view plane).
        /// No physics hit-shortening: always length = m_ArmLength.
        /// </summary>
        private Vector3 GetArmFullReachEndPoint(Vector3 shoulderPos)
        {
            if (m_HeadTransform == null)
                return shoulderPos;

            // Take camera forward, flatten Y so it's horizontal, so the line is perpendicular to the view plane
            Vector3 forward = m_HeadTransform.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
            {
                // Fallback if user's looking straight up/down
                forward = m_HeadTransform.forward;
            }

            forward.Normalize();

            Vector3 endPoint = shoulderPos + forward * m_ArmLength;

            // Visualize the virtual arm in the Scene view
            Debug.DrawLine(shoulderPos, endPoint, Color.yellow);

            return endPoint;
        }

        /// <summary>
        /// Updates circle tracking by computing angle changes in the horizontal plane.
        /// Accumulates angle until a full 360° circle is completed, then increments reps.
        /// </summary>
        private void UpdateCircleTracking(Vector3 shoulderPos, Vector3 handPosition, float shoulderToHandDistance)
        {
            // Only track if hand is far enough from shoulder (prevents noise)
            if (shoulderToHandDistance < m_MinCircleTrackingDistance)
            {
                if (m_IsTrackingCircle)
                {
                    // Debug: log when tracking stops due to distance
                    // Debug.Log($"Circle tracking paused: hand too close ({shoulderToHandDistance:F2}m < {m_MinCircleTrackingDistance:F2}m)");
                }
                m_IsTrackingCircle = false;
                m_LastHandDirection = Vector3.zero;
                return;
            }

            // Compute vector from shoulder to hand
            Vector3 shoulderToHand = handPosition - shoulderPos;

            // Project onto horizontal plane (remove Y component)
            Vector3 horizontalDirection = new Vector3(shoulderToHand.x, 0f, shoulderToHand.z);

            // Normalize to get direction
            if (horizontalDirection.sqrMagnitude < 0.0001f)
            {
                // Hand is directly above/below shoulder, can't track angle
                m_IsTrackingCircle = false;
                m_LastHandDirection = Vector3.zero;
                return;
            }

            horizontalDirection.Normalize();

            // If we have a previous direction, compute the angle change
            if (m_IsTrackingCircle && m_LastHandDirection.sqrMagnitude > 0.0001f)
            {
                // Compute signed angle between last direction and current direction
                // Using atan2 to get signed angle in the horizontal plane
                float lastAngle = Mathf.Atan2(m_LastHandDirection.x, m_LastHandDirection.z) * Mathf.Rad2Deg;
                float currentAngle = Mathf.Atan2(horizontalDirection.x, horizontalDirection.z) * Mathf.Rad2Deg;

                // Compute angle difference (handling wrap-around)
                float angleDelta = Mathf.DeltaAngle(lastAngle, currentAngle);

                // Filter out very small angle changes (noise)
                if (Mathf.Abs(angleDelta) < m_MinAngleDelta)
                {
                    angleDelta = 0f;
                }

                // Accumulate the angle change (use absolute value to track total rotation)
                // Track both clockwise and counterclockwise as positive accumulation
                m_AccumulatedAngle += Mathf.Abs(angleDelta);

                // Debug logging - always show accumulated angle
                Debug.Log($"Accumulated Angle: {m_AccumulatedAngle:F2}° / 360° (Reps: {m_CircleReps})");
                
                // Detailed debug logging (if enabled)
                if (m_DebugAngleTracking)
                {
                    Debug.Log($"Angle: last={lastAngle:F1}°, current={currentAngle:F1}°, delta={angleDelta:F2}°, accumulated={m_AccumulatedAngle:F2}°, reps={m_CircleReps}");
                }

                // Check if we've completed a full circle (360°)
                if (m_AccumulatedAngle >= 360f)
                {
                    m_CircleReps++;
                    // Reset accumulated angle, keeping any overflow
                    m_AccumulatedAngle = m_AccumulatedAngle - 360f;
                    
                    Debug.Log($"✅ Circle completed! Total reps: {m_CircleReps}, Remaining angle: {m_AccumulatedAngle:F2}°");
                }
            }
            else
            {
                // Start tracking - initialize
                if (!m_IsTrackingCircle)
                {
                    Debug.Log($"Circle tracking started. Hand distance: {shoulderToHandDistance:F2}m");
                }
                m_IsTrackingCircle = true;
            }

            // Store current direction for next frame
            m_LastHandDirection = horizontalDirection;
        }

        /// <summary>
        /// Resets the circle rep counter. Call this when starting a new exercise set.
        /// </summary>
        public void ResetCircleReps()
        {
            m_CircleReps = 0;
            m_AccumulatedAngle = 0f;
            m_IsTrackingCircle = false;
            m_LastHandDirection = Vector3.zero;
            Debug.Log("Circle reps reset to 0");
        }

        /// <summary>
        /// Gets the current number of completed circles.
        /// </summary>
        public int GetCircleReps()
        {
            return m_CircleReps;
        }
    }
}
