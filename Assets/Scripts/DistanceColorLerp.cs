using UnityEngine;

namespace PolySpatial.Samples
{
    public class DistanceColorLerp : MonoBehaviour
    {
        [Header("Distance Input")]
        [SerializeField] private float m_CurrentDistance = 0.6f;

        [Header("Optimal Distance")]
        [Tooltip("For gap values: 0 = optimal (green), larger = worse (red). For actual distances, set to desired optimal value.")]
        [SerializeField] private float m_OptimalDistance = 0f;

        [Header("Tolerance")]
        [SerializeField] private float m_Tolerance = 0.3f;

        [Header("Colors")]
        [SerializeField] private Color m_OptimalColor = Color.green;
        [SerializeField] private Color m_SubOptimalColor = Color.red;

        [Header("Renderer")]
        [SerializeField] private Renderer m_TargetRenderer;

        private void Awake()
        {
            if (m_TargetRenderer == null)
                m_TargetRenderer = GetComponent<Renderer>();

            if (m_TargetRenderer == null)
                Debug.LogError("DistanceColorLerp: No Renderer found on this GameObject.");
            else
                Debug.Log("DistanceColorLerp: Using shader " + m_TargetRenderer.sharedMaterial.shader.name);
        }

        public void SetDistance(float distance)
        {
            m_CurrentDistance = distance;
            UpdateColor();
        }

        private void UpdateColor()
        {
            if (m_TargetRenderer == null)
                return;

            float distanceFromOptimal = Mathf.Abs(m_CurrentDistance - m_OptimalDistance);
            float lerpValue = Mathf.Clamp01(distanceFromOptimal / m_Tolerance);

            Color c = Color.Lerp(m_OptimalColor, m_SubOptimalColor, lerpValue);

            // 🔥 This hits the standard color property on almost all basic shaders
            m_TargetRenderer.material.color = c;
        }
    }
}
