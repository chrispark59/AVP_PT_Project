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
    [SerializeField] private int countdownSeconds = 5;

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
        // Show countdown text first (before hiding parent)
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            // Ensure it's visible even if parent is hidden by moving it to panel root temporarily
            if (countdownText.transform.parent != startPanelRoot.transform)
            {
                countdownText.transform.SetParent(startPanelRoot.transform, true);
            }
        }

        // Hide start content (button) but keep countdown visible
        if (startContentRoot != null)
        {
            startContentRoot.SetActive(false);
        }

        int t = countdownSeconds;

        while (t > 0)
        {
            if (countdownText != null)
            {
                countdownText.text = t.ToString();
            }
            yield return new WaitForSeconds(1f);
            t--;
        }

        if (countdownText != null)
        {
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
