using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Handles the round-based orb activation logic for Exercise 2.
/// Uses existing orbs from the PositionArray instead of spawning new objects.
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    [Header("Orb Array Setup")]
    [Tooltip("Reference to the PositionArray Transform that contains all the orb GameObjects")]
    [SerializeField] private Transform positionArray;
    
    [Tooltip("Reference to OrbPositionManager to position the array when workout starts")]
    [SerializeField] private OrbPositionManager orbPositionManager;
    
    [Header("Round Settings")]
    [Tooltip("How many orbs to activate each round (Round 1, Round 2, ...).")]
    [SerializeField] private int[] objectsPerRound = { 5, 8, 11 };
    
    [Tooltip("Delay between activating each orb (seconds)")]
    [SerializeField] private float orbActivationIntervalSeconds = 0.75f;
    
    [Tooltip("Delay between rounds (seconds)")]
    [SerializeField] private float delayBetweenRoundsSeconds = 5f;

    [Header("HUD References")]
    [SerializeField] private TMP_Text roundsLabel;
    [SerializeField] private TMP_Text pointsLabel;

    private int _currentRoundIndex = -1;
    private int _orbsActivatedThisRound;
    private int _points;
    private Coroutine _roundRoutine;
    private OrbBehavior _currentTargetOrb; // Track which orb is currently the target (red)
    private List<OrbBehavior> _availableOrbs = new List<OrbBehavior>(); // All orbs from the array
    private int _nextOrbIndex = 0; // Index for cycling through orbs

    private void Awake()
    {
        // Collect all orbs from the PositionArray
        CollectOrbsFromArray();
    }

    /// <summary>
    /// Collects all OrbBehavior components from the PositionArray's children recursively.
    /// Handles 2D array structure (rows containing orbs).
    /// </summary>
    private void CollectOrbsFromArray()
    {
        _availableOrbs.Clear();
        
        if (positionArray == null)
        {
            Debug.LogWarning("ObjectSpawner: PositionArray is not assigned!", this);
            return;
        }

        // Recursively search through all children to find orbs
        CollectOrbsRecursive(positionArray);

        Debug.Log($"ObjectSpawner: Found {_availableOrbs.Count} orbs in PositionArray (searched recursively)");
    }

    /// <summary>
    /// Recursively searches through a Transform and its children to find OrbBehavior components.
    /// </summary>
    private void CollectOrbsRecursive(Transform parent)
    {
        // Check if this GameObject itself has an OrbBehavior
        OrbBehavior orbBehavior = parent.GetComponent<OrbBehavior>();
        if (orbBehavior != null)
        {
            _availableOrbs.Add(orbBehavior);
            // Initially disable/hide all orbs
            orbBehavior.gameObject.SetActive(false);
            orbBehavior.SetTarget(false);
        }

        // Recursively check all children
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            CollectOrbsRecursive(child);
        }
    }

    /// <summary>
    /// Starts the orb activation behavior.
    /// </summary>
    public void BeginSpawning()
    {
        Debug.Log("ObjectSpawner: BeginSpawning() called");
        
        if (_roundRoutine != null)
        {
            Debug.LogWarning("ObjectSpawner: Round routine already running!");
            return; 
        }

        if (positionArray == null)
        {
            Debug.LogError("ObjectSpawner: PositionArray is not assigned!", this);
            return;
        }

        // Re-collect orbs in case they weren't found in Awake (e.g., if PositionArray was assigned later)
        if (_availableOrbs.Count == 0)
        {
            Debug.LogWarning("ObjectSpawner: No orbs found, attempting to re-collect...");
            CollectOrbsFromArray();
        }

        if (_availableOrbs.Count == 0)
        {
            Debug.LogError($"ObjectSpawner: No orbs found in PositionArray! PositionArray has {positionArray.childCount} children. Make sure orbs have OrbBehavior component.", this);
            return;
        }

        if (objectsPerRound == null || objectsPerRound.Length == 0)
        {
            Debug.LogError("ObjectSpawner needs at least one round configured.", this);
            return;
        }

        Debug.Log($"ObjectSpawner: Starting game with {_availableOrbs.Count} orbs, {objectsPerRound.Length} rounds");

        // Position the orb array in front of the user when workout starts
        if (orbPositionManager != null)
        {
            orbPositionManager.RepositionArray();
        }

        // Reset all orbs
        ResetAllOrbs();

        // Clear any existing target orb
        if (_currentTargetOrb != null)
        {
            _currentTargetOrb.SetTarget(false);
            _currentTargetOrb.WasPressed -= OnOrbPressed;
            _currentTargetOrb = null;
        }

        _points = 0;
        _nextOrbIndex = 0;
        UpdatePointsLabel();
        
        Debug.Log("ObjectSpawner: Starting RunRounds coroutine");
        _roundRoutine = StartCoroutine(RunRounds());
    }

    /// <summary>
    /// Resets all orbs to inactive state.
    /// </summary>
    private void ResetAllOrbs()
    {
        foreach (var orb in _availableOrbs)
        {
            if (orb != null)
            {
                orb.gameObject.SetActive(false);
                orb.SetTarget(false);
                orb.WasPressed -= OnOrbPressed;
            }
        }
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
        Debug.Log("ObjectSpawner: RunRounds coroutine started");
        
        for (int i = 0; i < objectsPerRound.Length; i++)
        {
            _currentRoundIndex = i;
            _orbsActivatedThisRound = 0;
            
            Debug.Log($"ObjectSpawner: Starting Round {i + 1}, will activate {objectsPerRound[i]} orbs");
            UpdateRoundLabel();

            int orbsToActivate = Mathf.Max(0, objectsPerRound[i]);
            while (_orbsActivatedThisRound < orbsToActivate)
            {
                // Check if we have enough orbs available
                if (_nextOrbIndex >= _availableOrbs.Count)
                {
                    Debug.LogWarning($"Not enough orbs! Need {orbsToActivate} but only have {_availableOrbs.Count}. Resetting orb index.");
                    _nextOrbIndex = 0; // Cycle back to start if we run out
                }

                ActivateNextOrb();
                _orbsActivatedThisRound++;
                
                // Wait for user to interact with the orb before activating the next one
                yield return new WaitUntil(() => _currentTargetOrb == null || !_currentTargetOrb.IsTarget);
                
                // Small delay before activating next orb
                yield return new WaitForSeconds(orbActivationIntervalSeconds);
            }

            // Clear any remaining target orb before next round
            if (_currentTargetOrb != null)
            {
                _currentTargetOrb.SetTarget(false);
                _currentTargetOrb.WasPressed -= OnOrbPressed;
                _currentTargetOrb = null;
            }

            yield return new WaitForSeconds(delayBetweenRoundsSeconds);
        }

        _roundRoutine = null;
        _currentRoundIndex = -1;
    }

    /// <summary>
    /// Activates the next orb from the array and makes it the target (red).
    /// </summary>
    private void ActivateNextOrb()
    {
        if (_availableOrbs.Count == 0 || _nextOrbIndex >= _availableOrbs.Count)
        {
            Debug.LogWarning($"No more orbs available to activate! Count: {_availableOrbs.Count}, Index: {_nextOrbIndex}");
            return;
        }

        // Turn off the previous target orb if one exists
        if (_currentTargetOrb != null)
        {
            _currentTargetOrb.SetTarget(false);
            _currentTargetOrb.WasPressed -= OnOrbPressed;
        }

        // Get the next orb
        OrbBehavior nextOrb = _availableOrbs[_nextOrbIndex];
        
        if (nextOrb != null)
        {
            Debug.Log($"ObjectSpawner: Activating orb {_nextOrbIndex + 1}/{_availableOrbs.Count}: {nextOrb.gameObject.name}");
            
            // Activate the orb GameObject
            nextOrb.gameObject.SetActive(true);
            
            // Set it as the target (red)
            nextOrb.SetTarget(true);
            _currentTargetOrb = nextOrb;
            
            Debug.Log($"ObjectSpawner: Orb activated and set as target. IsTarget: {nextOrb.IsTarget}");
            
            // Subscribe to the orb's WasPressed event
            nextOrb.WasPressed += OnOrbPressed;
            
            // Move to next orb index (cycle if needed)
            _nextOrbIndex++;
            if (_nextOrbIndex >= _availableOrbs.Count)
            {
                _nextOrbIndex = 0; // Cycle back to start
            }
        }
        else
        {
            Debug.LogError($"ObjectSpawner: Orb at index {_nextOrbIndex} is null!");
        }
    }
    
    /// <summary>
    /// Called when an orb is pressed/selected by the user.
    /// </summary>
    private void OnOrbPressed(OrbBehavior pressedOrb)
    {
        Debug.Log($"ObjectSpawner: OnOrbPressed called for {pressedOrb?.gameObject.name}, Current target: {_currentTargetOrb?.gameObject.name}");
        
        if (pressedOrb != null && pressedOrb == _currentTargetOrb)
        {
            Debug.Log($"ObjectSpawner: Valid target pressed! Awarding point.");
            
            // Turn off the pressed orb (already done in Press(), but ensure it's off)
            pressedOrb.SetTarget(false);
            
            // Award a point for successfully hitting the target
            _points++;
            UpdatePointsLabel();
            
            // Clear the current target reference
            _currentTargetOrb = null;
            
            // Unsubscribe from the event
            pressedOrb.WasPressed -= OnOrbPressed;
            
            Debug.Log($"ObjectSpawner: Point awarded. Total points: {_points}");
        }
        else
        {
            Debug.LogWarning($"ObjectSpawner: Pressed orb doesn't match current target! Pressed: {pressedOrb?.gameObject.name}, Current: {_currentTargetOrb?.gameObject.name}");
        }
    }

    //basic updating round label UI logic 
    private void UpdateRoundLabel()
    {
        if (roundsLabel == null)
        {
            Debug.LogWarning("ObjectSpawner: roundsLabel is not assigned!");
            return;
        }

        int roundNumber = _currentRoundIndex + 1;
        roundsLabel.text = $"Round {roundNumber}/{objectsPerRound.Length}";
        Debug.Log($"ObjectSpawner: Updated round label to: {roundsLabel.text}");
    }

    //basic updating points label UI logic 
    private void UpdatePointsLabel()
    {
        if (pointsLabel == null)
        {
            Debug.LogWarning("ObjectSpawner: pointsLabel is not assigned!");
            return;
        }

        pointsLabel.text = _points.ToString();
        Debug.Log($"ObjectSpawner: Updated points label to: {pointsLabel.text}");
    }

    //stops spawning objects
    private void OnDisable()
    {
        StopSpawning();
    }
}
