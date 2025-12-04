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
        private TMP_Text m_DistanceText;                 // TextMeshPro label to show the distance

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

        private void Update()
        {
            if (m_HeadTransform == null || m_DistanceText == null ||
                m_TouchZeroValue == null || m_TouchZeroPhase == null)
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
                float gapToFullReach = Vector3.Distance(fullReachEndPoint, handPosition)-0.15f;   // how far from ideal full extension

                // Update UI text: show gap to full reach as ROM distance
                /*
                m_DistanceText.text =
                    $"ROM Gap (hand → full reach): {gapToFullReach.ToString(m_DistanceFormat)} m\n";
                */
                if (QualityText != null)
                {
                    // Simple ROM quality based on how close to "full reach" they are
                    if (gapToFullReach < 0.6f)
                        QualityText.text = "ROM: Excellent";
                    else if (gapToFullReach < 0.3f)
                        QualityText.text = "ROM: Good";
                    else
                        QualityText.text = "ROM: Needs Improvement";
                }

                // Update color lerp based on distance (0.6 is optimal)
                if (m_DistanceColorLerp != null)
                {
                    m_DistanceColorLerp.SetDistance(gapToFullReach);
                }
            }
            else
            {
                // Optional: clear text when not pinching
                // m_DistanceText.text = "ROM: --";
                // if (QualityText != null) QualityText.text = "";
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
    }
}
