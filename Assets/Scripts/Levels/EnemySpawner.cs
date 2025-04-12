using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System;

public class EnemySpawner : MonoBehaviour
{
    public Image level_selector;
    public GameObject button;
    public GameObject enemy;
    public SpawnPoint[] SpawnPoints;

    public TextAsset EnemyDataFile;
    private List<Enemy> EnemyDataList;
    private Dictionary<string, Enemy> EnemyDict = new Dictionary<string, Enemy>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Deserialize enemy data
        EnemyDataList = JsonConvert.DeserializeObject<List<Enemy>>(EnemyDataFile.ToString());
        // Create dictionary to access enemydata
        foreach (Enemy enemy in EnemyDataList)
        {
            EnemyDict.Add(enemy.name, enemy);
        }

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
        for (int i = 3; i > 0; i--) // Why not use the countdown variable in gamemanager? 3 is hardcoded twice
        {
            yield return new WaitForSeconds(1);
            // Update game manager countdown tracking variable every second
            GameManager.Instance.countdown--;
        }
        // SPAWN ENEMIES DURING WAVE
        GameManager.Instance.state = GameManager.GameState.INWAVE;
        for (int i = 0; i < 10; ++i)
        {
            yield return SpawnEnemy("skeleton");
        }
        yield return new WaitWhile(() => GameManager.Instance.enemy_count > 0);
        // END WAVE
        GameManager.Instance.state = GameManager.GameState.WAVEEND;
    }

    IEnumerator SpawnEnemy(string name)
    {
        // Pick a random spawn point and a random offset
        SpawnPoint spawn_point = SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
        Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.8f;
        
        // Instantiate enemy at spawn_point + offset
        Vector3 initial_position = spawn_point.transform.position + new Vector3(offset.x, offset.y, 0);
        GameObject new_enemy = Instantiate(enemy, initial_position, Quaternion.identity);

        // Set sprite by indexing the spritemananager
        new_enemy.GetComponent<SpriteRenderer>().sprite = GameManager.Instance.enemySpriteManager.Get(EnemyDict[name].sprite);

        // Create controller script, set hp and speed on controller script
        EnemyController en = new_enemy.GetComponent<EnemyController>();
        en.hp = new Hittable(EnemyDict[name].hp, Hittable.Team.MONSTERS, new_enemy);
        en.speed = EnemyDict[name].speed;

        // Add enemy to gamemanager
        GameManager.Instance.AddEnemy(new_enemy);

        // Wait (before spawning next zombie)
        yield return new WaitForSeconds(0.5f);
    }
}
