using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class shoulderHighlight : MonoBehaviour
{
    [Header("Colors")]
    [ColorUsage(false, true)]
    public Color restColor = new Color(0f, 0.5f, 1f, 1f); // blue
    [ColorUsage(false, true)]
    public Color activeColor = Color.red;

    SkinnedMeshRenderer skinned;
    MaterialPropertyBlock block;
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        skinned = GetComponent<SkinnedMeshRenderer>();
        block = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        // Subscribe to global state when this scene is active
        if (UniversalDataManager.Instance != null)
        {
            UniversalDataManager.Instance.ShoulderActiveChanged += OnShoulderStateChanged;
            // Initialize to current state
            OnShoulderStateChanged(UniversalDataManager.Instance.ShoulderActive);
        }
        else
        {
            SetToRest();
        }
    }

    void OnDisable()
    {
        if (UniversalDataManager.Instance != null)
            UniversalDataManager.Instance.ShoulderActiveChanged -= OnShoulderStateChanged;
    }

    void OnShoulderStateChanged(bool active)
    {
        if (active) SetActive();
        else        SetToRest();
    }

    public void SetToRest()  => SetColor(restColor);
    public void SetActive()  => SetColor(activeColor);

    void SetColor(Color c)
    {
        skinned.GetPropertyBlock(block);
        block.SetColor(BaseColorID, c);
        skinned.SetPropertyBlock(block);
    }
}

