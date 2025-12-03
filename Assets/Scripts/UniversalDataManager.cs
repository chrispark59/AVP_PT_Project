using System;
using UnityEngine;

public class UniversalDataManager : MonoBehaviour
{
    public static UniversalDataManager Instance { get; private set; }

    // public so other scripts can read it on scene load
    public bool ShoulderActive { get; private set; }

    public event Action<bool> ShoulderActiveChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);   // survives scene changes
    }

    public void SetShoulderActive(bool isActive)
    {
        Debug.Log($"UniversalDataManager: SetShoulderActive called with {isActive}. Current state: {ShoulderActive}");
        
        if (ShoulderActive == isActive) 
        {
            Debug.Log($"UniversalDataManager: State unchanged, skipping update.");
            return;
        }

        ShoulderActive = isActive;
        Debug.Log($"UniversalDataManager: ShoulderActive set to {ShoulderActive}. Invoking event with {ShoulderActiveChanged?.GetInvocationList().Length ?? 0} subscribers.");
        ShoulderActiveChanged?.Invoke(isActive);
    }
}

