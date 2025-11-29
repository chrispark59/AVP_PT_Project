using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Handles the round-based spawning logic for Exercise 2.
/// Object placement can be customised by overriding <see cref="ResolveSpawnPose"/>
/// or editing that method directly – everything else (round pacing, UI updates,
/// bookkeeping) is taken care of here.
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    [Header("Spawn Setup")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnIntervalSeconds = 0.75f;
    [SerializeField] private float delayBetweenRoundsSeconds = 5f;
    
    [Header("Orb Array Positioning")]
    [Tooltip("Optional: Reference to OrbPositionManager to position the array when workout starts")]
    [SerializeField] private OrbPositionManager orbPositionManager;

    [Tooltip("How many objects each round should spawn (Round 1, Round 2, ...).")]
    [SerializeField] private int[] objectsPerRound = { 5, 8, 11 };

    [Header("HUD References")]
    [SerializeField] private TMP_Text roundsLabel;
    [SerializeField] private TMP_Text pointsLabel;

    private int _currentRoundIndex = -1;
    private int _objectsSpawnedThisRound;
    private int _points;
    private Coroutine _roundRoutine;
    private OrbBehavior _currentTargetOrb; // Track which orb is currently the target (red)

    /*
    starts spawning behavior 
    */
    public void BeginSpawning()
    {
        if (_roundRoutine != null)
            return; 

        if (prefab == null)
        {
            Debug.LogError("ObjectSpawner has no prefab assigned.", this);
            return;
        }

        if (objectsPerRound == null || objectsPerRound.Length == 0)
        {
            Debug.LogError("ObjectSpawner needs at least one round configured.", this);
            return;
        }

        // Position the orb array in front of the user when workout starts
        if (orbPositionManager != null)
        {
            orbPositionManager.RepositionArray();
        }

        // Clear any existing target orb
        if (_currentTargetOrb != null)
        {
            _currentTargetOrb.SetTarget(false);
            _currentTargetOrb.WasPressed -= OnOrbPressed;
            _currentTargetOrb = null;
        }

        _points = 0;
        UpdatePointsLabel();
        _roundRoutine = StartCoroutine(RunRounds());
    }

    //stops spawning behavior 
    public void StopSpawning()
    {
        if (_roundRoutine == null)
            return;

        StopCoroutine(_roundRoutine);
        _roundRoutine = null;
        
        // Clear the current target orb
        if (_currentTargetOrb != null)
        {
            _currentTargetOrb.SetTarget(false);
            _currentTargetOrb.WasPressed -= OnOrbPressed;
            _currentTargetOrb = null;
        }
    }

    //runs rounds based off of objectsPerRound array
    private IEnumerator RunRounds()
    {
        for (int i = 0; i < objectsPerRound.Length; i++)
        {
            _currentRoundIndex = i;
            _objectsSpawnedThisRound = 0;
            UpdateRoundLabel();

            int objectsToSpawn = Mathf.Max(0, objectsPerRound[i]);
            while (_objectsSpawnedThisRound < objectsToSpawn)
            {
                SpawnSingleObject();
                _objectsSpawnedThisRound++;
                _points++;
                UpdatePointsLabel();
                /*
                need additional function to check if user has interacted with spawned object before spawning an additional object
                */
                yield return new WaitForSeconds(spawnIntervalSeconds);
            }

            yield return new WaitForSeconds(delayBetweenRoundsSeconds);
        }

        _roundRoutine = null;
        _currentRoundIndex = -1;
    }

    //spawns a single object
    private void SpawnSingleObject()
    {
        if (prefab == null)
            return;

        var spawnPose = ResolveSpawnPose();
        GameObject spawnedObj = Instantiate(prefab, spawnPose.position, spawnPose.rotation);
        
        // Get the OrbBehavior component from the spawned object
        OrbBehavior orbBehavior = spawnedObj.GetComponent<OrbBehavior>();
        if (orbBehavior != null)
        {
            // Turn off the previous target orb if one exists
            if (_currentTargetOrb != null)
            {
                _currentTargetOrb.SetTarget(false);
            }
            
            // Set this new orb as the target (red)
            orbBehavior.SetTarget(true);
            _currentTargetOrb = orbBehavior;
            
            // Subscribe to the orb's WasPressed event to handle when it's selected
            orbBehavior.WasPressed += OnOrbPressed;
        }
    }
    
    // Called when an orb is pressed/selected
    private void OnOrbPressed(OrbBehavior pressedOrb)
    {
        // Turn off the pressed orb
        if (pressedOrb != null)
        {
            pressedOrb.SetTarget(false);
            
            // If this was the current target, clear the reference
            if (_currentTargetOrb == pressedOrb)
            {
                _currentTargetOrb = null;
            }
            
            // Unsubscribe from the event
            pressedOrb.WasPressed -= OnOrbPressed;
        }
    }

    /*
    spawn location should be designed based off a grid system positioned within arm distance of the user 
    */
    protected virtual Pose ResolveSpawnPose()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return new Pose(point.position, point.rotation);
        }

        return new Pose(transform.position, transform.rotation);
    }

    //basic updating round label UI logic 
    private void UpdateRoundLabel()
    {
        if (roundsLabel == null)
            return;

        int roundNumber = _currentRoundIndex + 1;
        roundsLabel.text = $"Round {roundNumber}/{objectsPerRound.Length}";
    }

    //basic updating points label UI logic 
    private void UpdatePointsLabel()
    {
        if (pointsLabel == null)
            return;

        pointsLabel.text = _points.ToString();
    }

    //stops spawning objects
    private void OnDisable()
    {
        StopSpawning();
    }
}
