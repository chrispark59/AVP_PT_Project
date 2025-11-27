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
        // e.g. start spawning targets
        if (objectSpawner != null)
            objectSpawner.BeginSpawning();

        // you could also start timers, enable other systems, etc.
    }
}
