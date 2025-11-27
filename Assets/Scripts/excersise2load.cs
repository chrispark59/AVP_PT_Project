using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class excersise2load : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject startPanelRoot;
    [SerializeField] private GameObject startContentRoot;  // Assign: StartContentRoot
    [SerializeField] private TMP_Text countdownText; 
    [SerializeField] private GameObject workoutHudPanel;    // your rounds / points UI

    [Header("Countdown Settings")]
    [SerializeField] private int countdownSeconds = 6;

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

        // First, ensure countdown text is moved to panel root if it's a child of startContentRoot
        // This must happen BEFORE hiding startContentRoot
        if (countdownText != null && startPanelRoot != null)
        {
            // Check if countdown text is a child of startContentRoot
            bool isChildOfContentRoot = false;
            if (startContentRoot != null && !areSameObject)
            {
                Transform currentParent = countdownText.transform.parent;
                while (currentParent != null)
                {
                    if (currentParent == startContentRoot.transform)
                    {
                        isChildOfContentRoot = true;
                        break;
                    }
                    currentParent = currentParent.parent;
                }
            }

            // If countdown text is a child of startContentRoot, move it to panel root
            if (isChildOfContentRoot)
            {
                countdownText.transform.SetParent(startPanelRoot.transform, true);
            }
            
            // Ensure countdown text is active and visible
            countdownText.gameObject.SetActive(true);
        }

        // Wait one frame to ensure parent change and activation have taken effect
        yield return null;

        // Ensure startPanelRoot is active (needed for countdown text to be visible)
        if (startPanelRoot != null && !startPanelRoot.activeSelf)
        {
            startPanelRoot.SetActive(true);
        }

        // If they're the same object, we can't hide it yet - just hide the button content
        // Otherwise, hide start content (button) - countdown should still be visible
        if (startContentRoot != null && !areSameObject)
        {
            startContentRoot.SetActive(false);
        }

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
        OnWorkoutStarted?.Invoke();
    }

}
