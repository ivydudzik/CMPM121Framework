using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class EnemySpawner : MonoBehaviour
{
    public Image level_selector;
    public GameObject button;
    public GameObject enemy;
    public SpawnPoint[] SpawnPoints;

    // A dictionary derived from the given list of spawn points sorted by kind (color)
    private Dictionary<SpawnPoint.SpawnName, List<SpawnPoint>> SpawnPointDict = new();

    private string levelName;

    Dictionary<Spawn, bool> SpawnsCompleted;

    // --- RESOURCE PARSING ---
    public TextAsset EnemyDataFile;
    private List<Enemy> EnemyDataList;
    private Dictionary<string, Enemy> EnemyDict = new Dictionary<string, Enemy>();

    public TextAsset LevelDataFile;
    private List<Level> LevelDataList;
    private Dictionary<string, Level> LevelDict = new Dictionary<string, Level>();

    void Start()
    {
        // Deserialize enemy data
        EnemyDataList = JsonConvert.DeserializeObject<List<Enemy>>(EnemyDataFile.ToString());
        // Create dictionary to access enemy data
        foreach (Enemy enemy in EnemyDataList)
        {
            EnemyDict.Add(enemy.name, enemy);
        }

        // Deserialize level difficulty data
        LevelDataList = JsonConvert.DeserializeObject<List<Level>>(LevelDataFile.ToString());
        // Create dictionary to access level difficulty data
        foreach (Level level in LevelDataList)
        {
            LevelDict.Add(level.name, level);
        }

        // TEMP: HARD-CODED SET LEVEL TO "Easy"
        GameManager.Instance.currentLevelName = "Easy";

        // Set level for spawns to match the selected level (should probably be moved later when the above is moved)
        levelName = GameManager.Instance.currentLevelName;

        // Set wave count to 1
        GameManager.Instance.currentWave = 1;

        // Sort spawn points by kind
        CreateSpawnPointDictionaryByKind();

        // Create level start button
        GameObject selector = Instantiate(button, level_selector.transform);
        selector.transform.localPosition = new Vector3(0, 130);
        selector.GetComponent<MenuSelectorController>().spawner = this;
        selector.GetComponent<MenuSelectorController>().SetLevel("Start");


    }

    private void CreateSpawnPointDictionaryByKind()
    {
        foreach (SpawnPoint spawnpoint in SpawnPoints)
        {
            if (!SpawnPointDict.ContainsKey(spawnpoint.kind)) { SpawnPointDict[spawnpoint.kind] = new List<SpawnPoint>(); }

            SpawnPointDict[spawnpoint.kind].Append(spawnpoint);
        }
    }

    public void StartLevel(string levelname)
    {
        level_selector.gameObject.SetActive(false);
        // this is not nice: we should not have to be required to tell the player directly that the level is starting
        GameManager.Instance.player.GetComponent<PlayerController>().StartLevel();
        StartCoroutine(SpawnWave());
    }

    public void NextWave()
    {
        StartCoroutine(SpawnWave());
    }


    IEnumerator SpawnWave()
    {
        // COUNTDOWN BEFORE WAVE //
        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        GameManager.Instance.countdown = 3; 
        // Wait 'countdown' seconds
        for (int i = GameManager.Instance.countdown; i > 0; i--)
        {
            yield return new WaitForSeconds(1);
            // Update game manager countdown tracking variable every second
            GameManager.Instance.countdown--;
        }

        // SPAWN ENEMIES DURING WAVE //
        GameManager.Instance.state = GameManager.GameState.INWAVE;

        // Get a new spawning coroutine for each enemy type
        SpawnsCompleted = new();
        foreach (Spawn enemySpawnData in LevelDict[levelName].spawns)
        {
            StartCoroutine(SpawnWaveOfEnemy(enemySpawnData));
            SpawnsCompleted[enemySpawnData] = false;
        }

        // Wait for no enemies to be spawning
        foreach (Spawn enemySpawnData in LevelDict[levelName].spawns)
        {
            yield return new WaitWhile(() => SpawnsCompleted[enemySpawnData] == false);
        }

        // Wait for no enemies to be left alive
        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);
        // END WAVE //
        GameManager.Instance.state = GameManager.GameState.WAVEEND;
        GameManager.Instance.currentWave++;
        // Start next wave
        NextWave();
    }

    IEnumerator SpawnWaveOfEnemy(Spawn SpawnData)
    {
        // Access Enemy Spawn Data
        int enemySpawnCount = RPNEvaluator.Evaluate(SpawnData.count, new Dictionary<string, int>() { { "base", 1 } });
        List<int> enemySpawnSequence = SpawnData.sequence;
        // Spawn Enemies
        int sequencePointer = 0;
        for (int enemiesSpawnedInWave = 0; enemiesSpawnedInWave < enemySpawnCount; )
        {
            // Spawn a sub-wave of enemies
            for (int enemiesSpawnedInSequence = 0; enemiesSpawnedInSequence < enemySpawnSequence[sequencePointer]; enemiesSpawnedInSequence++)
            {
                SpawnEnemy(SpawnData);
                enemiesSpawnedInWave++;
            }

            // Loop through the sub-wave spawn counts sequence 
            if (enemySpawnSequence.Count >= (sequencePointer + 1))
            {
                sequencePointer = 0;
            } else
            {
                sequencePointer++;
            }

            // Wait the delay (before spawning next sub-wave of enemies)
            int spawnDelay = SpawnData.delay;
            yield return new WaitForSeconds(spawnDelay);

        }
    }

    void SpawnEnemy(Spawn spawnData)
    {
        // Access Enemy Spawn Data
        string enemyName = spawnData.enemy;

        // Pick a random spawn point and a random offset
        SpawnPoint spawn_point = SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
        Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.8f;
        
        // Instantiate enemy at spawn_point + offset
        Vector3 initial_position = spawn_point.transform.position + new Vector3(offset.x, offset.y, 0);
        GameObject new_enemy = Instantiate(enemy, initial_position, Quaternion.identity);

        // Set sprite by indexing the spritemananager
        new_enemy.GetComponent<SpriteRenderer>().sprite = GameManager.Instance.enemySpriteManager.Get(EnemyDict[enemyName].sprite);

        // Create controller script
        EnemyController en = new_enemy.GetComponent<EnemyController>();

        // Set HP, Speed, and Damage on controller script (Set "base" & "wave" variable contextually for evaluator function)
        int enemyHP = RPNEvaluator.Evaluate(spawnData.hp, new Dictionary<string, int>() { { "base", EnemyDict[enemyName].hp }, {"wave", GameManager.Instance.currentWave } });
        en.hp = new Hittable(enemyHP, Hittable.Team.MONSTERS, new_enemy);
        int enemySpeed = RPNEvaluator.Evaluate(spawnData.speed, new Dictionary<string, int>() { { "base", EnemyDict[enemyName].speed }, { "wave", GameManager.Instance.currentWave } });
        en.speed = enemySpeed;
        int enemyDamage = RPNEvaluator.Evaluate(spawnData.damage, new Dictionary<string, int>() { { "base", EnemyDict[enemyName].damage }, { "wave", GameManager.Instance.currentWave } });
        en.damage = enemyDamage;

        // Add enemy to gamemanager
        GameManager.Instance.AddEnemy(new_enemy);
    }
}
