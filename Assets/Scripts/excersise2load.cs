using System.Collections;
using UnityEngine;
using TMPro;

public class excersise2load : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject startPanelRoot;     // the whole floating panel with start button + countdown
    [SerializeField] private TMP_Text countdownText; // text object used to display the countdown
    [SerializeField] private GameObject workoutHudPanel;    // your rounds / points UI

    [Header("Countdown Settings")]
    [SerializeField] private int countdownSeconds = 5;

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
        int t = countdownSeconds;

        // Disable all child objects except the countdown text so only the countdown shows
        foreach (Transform child in startPanelRoot.transform)
            if (child.gameObject != countdownText.gameObject)
                child.gameObject.SetActive(false);

        // Show countdown text
        countdownText.gameObject.SetActive(true);

        while (t > 0)
        {
            countdownText.text = t.ToString();
            yield return new WaitForSeconds(1f);
            t--;
        }

        // Optional "Go!" flash
        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);

        // Hide entire start panel
        startPanelRoot.SetActive(false);

        // Show workout HUD
        if (workoutHudPanel != null)
            workoutHudPanel.SetActive(true);

    }
}
