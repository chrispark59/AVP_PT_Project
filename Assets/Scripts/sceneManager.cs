using UnityEngine;

public class Exercise2SceneManager : MonoBehaviour
{
    [SerializeField] private excersise2load exerciseLoader;
    [SerializeField] private ObjectSpawner objectSpawner;

    private void Awake()
    {
        // Subscribe to the event
        exerciseLoader.OnWorkoutStarted += HandleWorkoutStarted;
    }

    private void OnDestroy()
    {
        exerciseLoader.OnWorkoutStarted -= HandleWorkoutStarted;
    }

    private void HandleWorkoutStarted()
    {
        Debug.Log("Exercise2SceneManager: HandleWorkoutStarted called");
        
        // e.g. start spawning targets
        if (objectSpawner != null)
        {
            Debug.Log("Exercise2SceneManager: Calling objectSpawner.BeginSpawning()");
            objectSpawner.BeginSpawning();
        }
        else
        {
            Debug.LogError("Exercise2SceneManager: objectSpawner is null! Make sure it's assigned in the inspector.");
        }

        // you could also start timers, enable other systems, etc.
    }
}
