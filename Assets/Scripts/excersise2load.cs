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

    // Called by SpatialUIButton → Press End()
    public void StartWorkoutSequence()
    {
        if (_isRunning) return; // prevents double tapping
        _isRunning = true;
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        startContentRoot.SetActive(false);  // hide start screen
        countdownText.gameObject.SetActive(true);

        int t = countdownSeconds;

        while (t > 0)
        {
            countdownText.text = t.ToString();
            yield return new WaitForSeconds(1f);
            t--;
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);

        // Hide whole menu
        startPanelRoot.SetActive(false);

        // Show workout HUD
        workoutHudPanel.SetActive(true);
        OnWorkoutStarted?.Invoke();
    }

}
