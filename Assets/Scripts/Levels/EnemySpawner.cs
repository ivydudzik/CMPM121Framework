using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    public Image level_selector;
    public GameObject button;
    public GameObject enemy;
    public SpawnPoint[] SpawnPoints;

    private string levelName;

    // --- RESOURCE PARSING ---
    public TextAsset EnemyDataFile;
    private List<Enemy> EnemyDataList;
    private Dictionary<string, Enemy> EnemyDict = new Dictionary<string, Enemy>();

    public TextAsset LevelDataFile;
    private List<Level> LevelDataList;
    private Dictionary<string, Level> LevelDict = new Dictionary<string, Level>();


    // --- SPAWNING DEFAULTS ---
    const int sequenceDefaultLength = 1;
    const int sequenceDefaultValue = 1;
    const int delayDefault = 2;
    const string locationDefault = "random";
    const string hpDefault = "base";
    const string speedDefault = "base";
    const string damageDefault = "base";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

        Debug.Log(LevelDict.Count);
        Debug.Log(LevelDict["Easy"].spawns[0].sequence.Count);

        // TEMP: HARD-CODED SET LEVEL TO "Easy"
        GameManager.Instance.currentLevelName = "Easy";

        // Set level for spawns to match the selected level (should probably be moved later when the above is moved)
        levelName = GameManager.Instance.currentLevelName;

        // Create level start button
        GameObject selector = Instantiate(button, level_selector.transform);
        selector.transform.localPosition = new Vector3(0, 130);
        selector.GetComponent<MenuSelectorController>().spawner = this;
        selector.GetComponent<MenuSelectorController>().SetLevel("Start");


    }

    // Update is called once per frame
    void Update()
    {
        
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
        // COUNTDOWN BEFORE WAVE
        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;
        GameManager.Instance.countdown = 3; 
        // Wait 'countdown' seconds
        for (int i = GameManager.Instance.countdown; i > 0; i--) // Why not use the countdown variable in gamemanager? 3 is hardcoded twice
        {
            yield return new WaitForSeconds(1);
            // Update game manager countdown tracking variable every second
            GameManager.Instance.countdown--;
        }
        // SPAWN ENEMIES DURING WAVE
        GameManager.Instance.state = GameManager.GameState.INWAVE;
        foreach (Spawn enemySpawnData in LevelDict[levelName].spawns)
        {
            // Access Enemy Spawn Data
            int enemySpawnCount = RPNEvaluator.Evaluate(enemySpawnData.count, new Dictionary<string, int>() { { "base", 1 } });
            // Spawn Enemies
            for (int i = enemySpawnCount; i < 10; ++i)
            {
                yield return SpawnEnemy(enemySpawnData);
            }
        }
        
        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);
        // END WAVE
        GameManager.Instance.state = GameManager.GameState.WAVEEND;
    }

    IEnumerator SpawnEnemy(Spawn spawnData)
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

        // Create controller script, set hp and speed on controller script
        EnemyController en = new_enemy.GetComponent<EnemyController>();
        // Set "base" variable contextually for evaluator function
        int enemyHP = RPNEvaluator.Evaluate(spawnData.hp, new Dictionary<string, int>() { { "base", EnemyDict[enemyName].hp } });
        en.hp = new Hittable(enemyHP, Hittable.Team.MONSTERS, new_enemy);
        int enemySpeed = RPNEvaluator.Evaluate(spawnData.speed, new Dictionary<string, int>() { { "base", EnemyDict[enemyName].speed } });
        en.speed = enemySpeed;

        // Add enemy to gamemanager
        GameManager.Instance.AddEnemy(new_enemy);

        // Wait (before spawning next zombie)
        yield return new WaitForSeconds(0.5f);
    }
}
