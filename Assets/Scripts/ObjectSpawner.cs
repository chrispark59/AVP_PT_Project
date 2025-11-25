using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 2f;

    private bool _spawning;
    private float _timer;

    public void BeginSpawning()
    {
        _spawning = true;
        _timer = 0f;
    }

    private void Update()
    {
        if (!_spawning || prefab == null || spawnPoints.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Instantiate(prefab, point.position, point.rotation);
        }
    }
}
