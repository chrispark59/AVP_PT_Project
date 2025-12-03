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
        Debug.Log($"shoulderHighlight: OnEnable called on {gameObject.name}");
        
        // Subscribe to global state when this scene is active
        if (UniversalDataManager.Instance != null)
        {
            Debug.Log($"shoulderHighlight: UniversalDataManager found. Current ShoulderActive state: {UniversalDataManager.Instance.ShoulderActive}");
            UniversalDataManager.Instance.ShoulderActiveChanged += OnShoulderStateChanged;
            // Initialize to current state
            OnShoulderStateChanged(UniversalDataManager.Instance.ShoulderActive);
        }
        else
        {
            Debug.LogWarning($"shoulderHighlight: UniversalDataManager.Instance is null! Setting to rest color.");
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
        Debug.Log($"shoulderHighlight: OnShoulderStateChanged called with active={active} on {gameObject.name}");
        if (active) 
        {
            Debug.Log($"shoulderHighlight: Setting to ACTIVE (red)");
            SetActive();
        }
        else        
        {
            Debug.Log($"shoulderHighlight: Setting to REST (blue)");
            SetToRest();
        }
    }

    public void SetToRest()  
    {
        Debug.Log($"shoulderHighlight: SetToRest() called on {gameObject.name}");
        SetColor(restColor);
    }
    
    public void SetActive()  
    {
        Debug.Log($"shoulderHighlight: SetActive() called on {gameObject.name}");
        SetColor(activeColor);
    }

    void SetColor(Color c)
    {
        if (skinned == null)
        {
            Debug.LogError($"shoulderHighlight: SkinnedMeshRenderer is null on {gameObject.name}!");
            return;
        }
        
        Debug.Log($"shoulderHighlight: SetColor called with color {c} (R:{c.r:F2}, G:{c.g:F2}, B:{c.b:F2}, A:{c.a:F2}) on {gameObject.name}");
        
        skinned.GetPropertyBlock(block);
        block.SetColor(BaseColorID, c);
        skinned.SetPropertyBlock(block);
        
        Debug.Log($"shoulderHighlight: Color applied to SkinnedMeshRenderer on {gameObject.name}");
    }
}

