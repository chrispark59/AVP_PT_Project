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
    [SerializeField] private float delayBetweenRoundsSeconds = 2f;

    [Tooltip("How many objects each round should spawn (Round 1, Round 2, ...).")]
    [SerializeField] private int[] objectsPerRound = { 5, 8, 11 };

    [Header("HUD References")]
    [SerializeField] private TMP_Text roundsLabel;
    [SerializeField] private TMP_Text pointsLabel;

    private int _currentRoundIndex = -1;
    private int _objectsSpawnedThisRound;
    private int _points;
    private Coroutine _roundRoutine;

    /// <summary>Begin spawning through all configured rounds.</summary>
    public void BeginSpawning()
    {
        if (_roundRoutine != null)
            return; // already running

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

        _points = 0;
        UpdatePointsLabel();
        _roundRoutine = StartCoroutine(RunRounds());
    }

    /// <summary>Stops any ongoing spawning activity.</summary>
    public void StopSpawning()
    {
        if (_roundRoutine == null)
            return;

        StopCoroutine(_roundRoutine);
        _roundRoutine = null;
    }

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
                yield return new WaitForSeconds(spawnIntervalSeconds);
            }

            yield return new WaitForSeconds(delayBetweenRoundsSeconds);
        }

        _roundRoutine = null;
        _currentRoundIndex = -1;
    }

    private void SpawnSingleObject()
    {
        if (prefab == null)
            return;

        var spawnPose = ResolveSpawnPose();
        Instantiate(prefab, spawnPose.position, spawnPose.rotation);
    }

    /// <summary>
    /// Determines where the next object should spawn.
    /// Override or edit this if you want custom placement logic.
    /// </summary>
    protected virtual Pose ResolveSpawnPose()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return new Pose(point.position, point.rotation);
        }

        return new Pose(transform.position, transform.rotation);
    }

    private void UpdateRoundLabel()
    {
        if (roundsLabel == null)
            return;

        int roundNumber = _currentRoundIndex + 1;
        roundsLabel.text = $"Round {roundNumber}/{objectsPerRound.Length}";
    }

    private void UpdatePointsLabel()
    {
        if (pointsLabel == null)
            return;

        pointsLabel.text = _points.ToString();
    }

    private void OnDisable()
    {
        StopSpawning();
    }
}
