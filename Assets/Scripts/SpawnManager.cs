using System.Collections;
using UnityEngine;
using Unity.VisualScripting;

public class SpawnManager : MonoBehaviour
{
    private static SpawnManager _instance;
    public static SpawnManager Instance
    {
        get { return _instance; }
    }
    [SerializeField] private GameObject[] _enemyPrefabs;
    [SerializeField] private WaveEnemySpawns[] _enemySpawns;
    private float[] _enemySpawnPercentage;
    [SerializeField] private Transform _enemyContainer;
    [SerializeField] private GameObject[] _powerupPrefabs;
    [SerializeField] private int[] _powerupWeights;
    private float[] _powerupSpawnPercent;
    [SerializeField] private Transform _powerupContainer;
    private GameObject[] _commonPowerups, _uncommonPowerups, _rarePowerups;
    private float[] _categoryPercent;
    [SerializeField] private Vector2 _spawnXRange;
    [SerializeField] private float _topSpawnArea;
    private bool _canSpawn = true;
    [SerializeField] private GameManager _gameManager;
    [Header("Wave Info")]
    private int _waveCount = 0;
    private int _enemiesInWave = 10;
    private int _currentEnemies = 0;
    private int _spawnedEnemies = 0;
    [SerializeField] private int _finalWave = 10;
    [SerializeField] GameObject _bossPrefab;
    [SerializeField] Vector3 _bossSpawnLocation;
    WaitForSeconds _shortWait;
    WaitForSeconds _midWait;
    [SerializeField] float _shortWaitTime = 2;
    [SerializeField] float _midWaitTime = 5;
    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else if (_instance != this)
            Destroy(this);
    }

    private void Start()
    {
        PowerupPercentCalculation();
        _shortWait = new WaitForSeconds(_shortWaitTime);
        _midWait = new WaitForSeconds(_midWaitTime);
    }

    public void StartSpawning()
    {
        StartCoroutine(EnemySpawnRoutine());
        StartCoroutine(PowerupSpawnRoutine());
    }

    private void PowerupPercentCalculation()
    {
        int totals = 0;
        foreach (int i in _powerupWeights)
            totals += i;
        _powerupSpawnPercent = new float[_powerupWeights.Length];
        for (int j = 0; j < _powerupWeights.Length; j++)
            _powerupSpawnPercent[j] = (float)_powerupWeights[j] / (float)totals;
    }

    IEnumerator EnemySpawnRoutine()
    {
        while ( _canSpawn && _waveCount < _finalWave)
        {
            _waveCount++;
            _enemiesInWave = _waveCount * 10;
            UIManager.Instance.UpdateWaveBanner(_waveCount);
            _enemySpawnPercentage = _enemySpawns[_waveCount - 1].SpawnRates();
            yield return _midWait;
            _currentEnemies = 0;
            _spawnedEnemies = 0;
            while (_spawnedEnemies < _enemiesInWave && _canSpawn)
            {
                float randomX = Random.Range(_spawnXRange.x, _spawnXRange.y);
                int randomEnemy = EnemyPicker();
                Instantiate(_enemyPrefabs[randomEnemy], new Vector3(randomX, _topSpawnArea, 0), Quaternion.identity, _enemyContainer);
                _spawnedEnemies++;
                _currentEnemies++;
                yield return _shortWait;
            }
            while (_currentEnemies > 0)
                yield return null;
        }
        if (_waveCount == _finalWave)
            Instantiate(_bossPrefab);
    }

    IEnumerator PowerupSpawnRoutine()
    {
        while (_canSpawn)
        {
            yield return _midWait;
            while (_currentEnemies > 0)
            {
                float randomX = Random.Range(_spawnXRange.x, _spawnXRange.y);
                int randomPowerup = PowerupPicker();
                Instantiate(_powerupPrefabs[randomPowerup], new Vector3(randomX, _topSpawnArea, 0), Quaternion.identity, _powerupContainer);
                yield return _shortWait;
            }
            yield return null;
        }
    }

    public void OnPlayerDeath()
    {
        _canSpawn = false;
        UIManager.Instance.GameOver();
        _gameManager.OnPlayerDeath();
        Enemy[] enemies = GameObject.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach(Enemy enemy in enemies)
        {
            enemy.OnPlayerDeath();
        }
    }

    public void OnEnemyDeath()
    {
        _currentEnemies--;
    }

    private int PowerupPicker()
    {
        int item = 0;
        float RNG = Random.value;
        float runningTotal = 0;
        for (int i = 0; i < _powerupSpawnPercent.Length; i++)
        {
            runningTotal += _powerupSpawnPercent[i];
            if (RNG <= runningTotal)
                return i;
        }
        return item;
    }

    private int EnemyPicker()
    {
        int enemy = 0;
        float RNG = Random.value;
        float runningTotal = 0;
        for (int i = 0; i < _enemySpawnPercentage.Length; i++)
        {
            runningTotal += _enemySpawnPercentage[i];
            if (RNG <= runningTotal)
            {
                return i;
            }
        }
        return enemy;
    }

    private GameObject PowerupCategoryPick()
    {
        GameObject powerUp = _commonPowerups[0];
        int catID = 0;
        float RNG = Random.value;
        float runningTotal = 0;
        for (int i = 0; i < _categoryPercent.Length; i++)
        {
            runningTotal += _categoryPercent[i];
            if (RNG <= runningTotal)
                catID = 1;
        }
        int randomPowerup;
        switch (catID)
        {
            case 0:
                randomPowerup = Random.Range(0, _commonPowerups.Length);
                powerUp = _commonPowerups[randomPowerup];
                break;
            case 1:
                randomPowerup = Random.Range(0, _uncommonPowerups.Length);
                powerUp = _uncommonPowerups[randomPowerup];
                break;
            case 2:
                randomPowerup = Random.Range(0, _rarePowerups.Length);
                powerUp = _rarePowerups[randomPowerup];
                break;
            default:
                break;

        }
        return powerUp;
    }
}
