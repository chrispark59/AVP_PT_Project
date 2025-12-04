using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class excersise2load : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject startPanelRoot;
    [SerializeField] private GameObject startContentRoot;  // Assign: StartContentRoot (button container)
    [SerializeField] private GameObject startButton;  // Optional: specific button to hide (if null, will hide startContentRoot)
    [SerializeField] private TMP_Text startButtonText;  // Optional: specific text component to hide (button label like "Start Workout")
    [SerializeField] private TMP_Text countdownText; 
    [SerializeField] private GameObject workoutHudPanel;    // your rounds / points UI

    [Header("Countdown Settings")]
    [SerializeField] private int countdownSeconds = 6;

    [Header("Visualization References")]
    [SerializeField] private PolySpatial.Samples.PinchDistanceDisplay estimationBehavior;  // Reference to estimation behavior to activate visualization

    public event Action OnWorkoutStarted;   // <-- scene manager / spawner can subscribe to this

    private bool _isRunning;

    private void Start()
    {
        // Ensure countdown text is initially hidden
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        
        // Ensure workout HUD is initially hidden
        if (workoutHudPanel != null)
        {
            workoutHudPanel.SetActive(false);
        }
    }

    // Called by SpatialUIButton → Press End()
    public void StartWorkoutSequence()
    {
        if (_isRunning) return; // prevents double tapping
        _isRunning = true;
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        // Check if startPanelRoot and startContentRoot are the same (would cause issues)
        bool areSameObject = (startPanelRoot != null && startContentRoot != null && startPanelRoot == startContentRoot);

        // Helper function to check if a transform is a child of another
        bool IsChildOf(Transform child, Transform parent)
        {
            if (child == null || parent == null) return false;
            Transform current = child.parent;
            while (current != null)
            {
                if (current == parent) return true;
                current = current.parent;
            }
            return false;
        }

        // Keep the background visible - startContentRoot likely contains the background
        // Ensure startPanelRoot is active (parent container)
        if (startPanelRoot != null && !startPanelRoot.activeSelf)
        {
            startPanelRoot.SetActive(true);
        }

        // Ensure startContentRoot is active (contains the background - we want to keep this visible)
        if (startContentRoot != null && !areSameObject && !startContentRoot.activeSelf)
        {
            startContentRoot.SetActive(true);
        }

        // Hide only the button and its text, not the background container
        if (startButton != null)
        {
            // Hide the specific button GameObject
            startButton.SetActive(false);
        }
        
        // Hide the button text if assigned
        if (startButtonText != null)
        {
            startButtonText.gameObject.SetActive(false);
        }
        else if (startContentRoot != null && !areSameObject && startButton == null)
        {
            // If startButtonText is not assigned but startButton is also not assigned,
            // try to find and hide any TMP_Text components in startContentRoot that aren't the countdown text
            TMP_Text[] allTexts = startContentRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in allTexts)
            {
                if (text != countdownText)
                {
                    text.gameObject.SetActive(false);
                }
            }
        }

        // Set up countdown text - keep it in the same container as the background
        // Prefer startContentRoot if it exists (has the background), otherwise use startPanelRoot
        GameObject backgroundContainer = (startContentRoot != null && !areSameObject) ? startContentRoot : startPanelRoot;
        
        if (countdownText != null && backgroundContainer != null)
        {
            // Move countdown text to the background container if it's not already there
            if (!IsChildOf(countdownText.transform, backgroundContainer.transform))
            {
                countdownText.transform.SetParent(backgroundContainer.transform, true);
            }
            
            // Set the text immediately to avoid showing default "text" value
            countdownText.text = countdownSeconds.ToString();
            
            // Ensure countdown text is active and visible
            countdownText.gameObject.SetActive(true);
        }

        // Wait one frame to ensure parent changes and activation have taken effect
        yield return null;

        // Ensure we're still active (coroutine won't run if GameObject is inactive)
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("excersise2load GameObject is inactive! Coroutine will not run.");
            yield break;
        }

        int t = countdownSeconds;

        // Show initial countdown number immediately
        if (countdownText != null)
        {
            // Force countdown text to be active
            if (!countdownText.gameObject.activeSelf)
            {
                countdownText.gameObject.SetActive(true);
            }
            // Ensure parent is still active
            if (countdownText.transform.parent != null && !countdownText.transform.parent.gameObject.activeSelf)
            {
                countdownText.transform.parent.gameObject.SetActive(true);
            }
            countdownText.text = t.ToString();
            Debug.Log($"Countdown: {t}");
        }

        // Countdown loop - wait, then decrement and show new number
        while (t > 0)
        {
            yield return new WaitForSeconds(1f);
            t--;
            
            if (t > 0 && countdownText != null)
            {
                // Force countdown text to be active
                if (!countdownText.gameObject.activeSelf)
                {
                    countdownText.gameObject.SetActive(true);
                }
                // Ensure parent is still active
                if (countdownText.transform.parent != null && !countdownText.transform.parent.gameObject.activeSelf)
                {
                    countdownText.transform.parent.gameObject.SetActive(true);
                }
                countdownText.text = t.ToString();
                Debug.Log($"Countdown: {t}");
            }
        }
        
        // When countdown reaches 0, activate visualization objects (after loop exits)
        if (estimationBehavior != null)
        {
            estimationBehavior.ActivateVisualizationObjects();
            Debug.Log("Visualization objects (circle and ring) activated at countdown 0");
        }
        else
        {
            Debug.LogWarning("estimationBehavior is null! Cannot activate visualization objects.");
        }

        // Show "GO!" message
        if (countdownText != null)
        {
            if (!countdownText.gameObject.activeSelf)
            {
                countdownText.gameObject.SetActive(true);
            }
            countdownText.text = "GO!";
        }
        yield return new WaitForSeconds(0.5f);

        // Hide countdown text and whole menu
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        if (startPanelRoot != null)
        {
            startPanelRoot.SetActive(false);
        }

        // Show workout HUD
        if (workoutHudPanel != null)
        {
            workoutHudPanel.SetActive(true);
        }
        
        // Invoke workout started event - this will trigger target spawning
        // Small delay to ensure UI is fully hidden before targets appear
        yield return new WaitForSeconds(0.1f);
        OnWorkoutStarted?.Invoke();
    }

}
