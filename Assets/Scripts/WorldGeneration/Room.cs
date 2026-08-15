using System.Collections.Generic;
using System.Linq;
using Amr;
using Pathfinding;
using UnityEngine;

public class Room : MonoBehaviour
{
    public BoxCollider2D boxCollider2D;

    public List<WaveList> waves;

    public int enemiesNum;
    public bool hasFinished;
    public bool hasStarted;

    public bool isChestRoom;
    public bool isStartRoom;
    public bool isEndRoom;

    private GameObject chest;
    private GameObject firstBoss;

    private GridGraph grid;
    public SavingManager.RoomData roomData; //the data set in room generator and accessed in dungeonGenerator
    private int waveNum;

    public Vector3 Center => transform.position;

    public Vector3 TopLeft
    {
        get
        {
            if (gameObject.activeSelf)
            {
                var transform1 = transform;
                var position = transform1.position;
                var localScale = transform1.localScale;
                return new Vector3(position.x - localScale.x / 2f, position.y + localScale.y / 2f);
            }

            return new Vector3(0, 0, 0);
        }
    }

    public Vector3 BottomRight
    {
        get
        {
            var transform1 = transform;
            var position = transform1.position;
            var localScale = transform1.localScale;
            return new Vector3(position.x + localScale.x / 2f, position.y - localScale.y / 2f);
        }
    }

    public List<RoomConnection> Connections { get; private set; }

    private void Awake()
    {
        Connections = new List<RoomConnection>();
    }

    private void Start()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        grid = AstarPath.active.data.gridGraph;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            if (!hasFinished && !hasStarted)
            {
                RoomEntered();
                hasStarted = true;
            }
    }

    public void SetStartRoom()
    {
        isStartRoom = true;

        var tilemapData = new SavingManager.RoomData();
        foreach (var v in GameManager.roomData.Where(v => v.name == "normal"))
        {
            tilemapData = v;
            roomData = v;
        }

        var width = (int)tilemapData.size.x;
        var height = (int)tilemapData.size.y;

        //Start rooms at ID 10
        Init(transform.position, width, height); //do the tile-ing
        /*for (var i = 0; i < RoomGenerator.v.Length; i++)
        {
            if (RoomGenerator.v[i].pos == (Vector2) transform.position)
                RoomGenerator.v[i] = new RoomGenerator.RoomTracer(i, transform.position);
        }*/
    }

    public void SetChestRoom()
    {
        chest = (GameObject)Resources.Load("GameObjects/Chest");
        isChestRoom = true;
        Instantiate(chest, Helper.Iso2(Center), Quaternion.identity, GameObject.Find("ChestContainer").transform);
    }

    public void SetEndRoom()
    {
        isEndRoom = true;
    }

    public void Init(Vector2 position, int width, int height)
    {
        var transform1 = transform;
        transform1.position = new Vector3(position.x, position.y);
        transform1.localScale = new Vector2(width + 1, height + 1);
    }

    public void AddRoomConnection(RoomConnection connection)
    {
        if (!Connections.Contains(connection)) Connections.Add(connection);
    }

    private void RoomEntered()
    {
        if (!isChestRoom && !isEndRoom)
        {
            SpawnNextWave(0);
            GameManager.ONChangeDoorsState.Invoke(false);
        }
        else if (isEndRoom)
        {
            //instantiate the first boss instead of spawning enemies
            firstBoss = (GameObject)Resources.Load("GameObjects/FirstBoss");
            firstBoss = Instantiate(firstBoss, Center, Quaternion.identity);
            firstBoss.GetComponent<FirstBoss>().Initialize(this);
            firstBoss.GetComponent<AIDestinationSetter>().target = GameObject.FindGameObjectWithTag("Player").transform;
            GameManager.ONChangeDoorsState.Invoke(false);
            Debug.Log("entered the boss sphere of influence");
        }
    }

    public void EnemyDied()
    {
        enemiesNum--;
        if (enemiesNum == 0)
        {
            waveNum++;
            SpawnNextWave(waveNum);
        }
    }

    public void BossDied()
    {
        Debug.Log("ta da");
    }

    private void SpawnNextWave(int waveNumber)
    {
        if (waveNumber < waves.Count)
        {
            var enemySOs = waves[waveNumber].waveList;
            enemiesNum = enemySOs.Count;
            if (grid != null) EnemySpawner.SpawnEnemies(enemySOs, this, grid);
        }
        else
        {
            hasFinished = true;
            GameManager.ONChangeDoorsState.Invoke(true);
        }
    }
}