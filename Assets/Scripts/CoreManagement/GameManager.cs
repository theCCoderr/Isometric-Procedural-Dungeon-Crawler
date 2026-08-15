using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Amr;
using UnityEngine;
using UnityEngine.Events;
using static Amr.DungeonGenerator;

public class GameManager : MonoBehaviour
{
    [Description("false : close doors" + "\n" + "true : open doors")]
    public static readonly UnityEvent<bool> ONChangeDoorsState = new UnityEvent<bool>();

    public static readonly UnityEvent<int> ONHealthChanged = new UnityEvent<int>();

    public static readonly UnityEvent ONGameOver = new UnityEvent();

    public static readonly UnityEvent<string> ONChestOpened = new UnityEvent<string>();
    public static List<GunSo> gunSOs = new List<GunSo>();
    public static List<SavingManager.RoomData> roomData = new List<SavingManager.RoomData>();
    public static List<SavingManager.RoomData> specialRoomData = new List<SavingManager.RoomData>();

    [SerializeField] private GameObject doorFolder;
    public Transform playerTrans;
    public List<GunSo> refGunsSOs = new List<GunSo>();
    private bool decreaseI;
    private float i = 3f;
    private bool waitedToOpenDoors;

    private void Awake()
    {
        var allRoomData = SavingManager.LoadAllRoomData();
        foreach (var v in allRoomData)
            roomData.Add(v);

        var allSpecialRoomData = SavingManager.LoadSpecialRoomData();
        foreach (var v in allSpecialRoomData)
            specialRoomData.Add(v);

        gunSOs = refGunsSOs;
    }

    private void Start()
    {
        ONHealthChanged.AddListener(HealthChanged);
        ONTilesGenerated.AddListener(ProcessPlayerSpawn);
        ONTilesGenerated.AddListener(ProcessAStarScan);
        ONChangeDoorsState.AddListener(OnDoorsChange);
    }

    private void Update()
    {
        if (decreaseI && i > 0) i -= Time.deltaTime;
        if (i < 0.1)
        {
            var hello = RoomGenerator.Rooms.Count;
            i = 1;
            decreaseI = false;
            var graph = AstarPath.active.data.graphs[0];
            //TODO
            //var middlePointX = (RoomGenerator.xMax + RoomGenerator.xMin) / 2;
            //var middlePointY = (RoomGenerator.yMax + RoomGenerator.yMin) / 2;
            var width = (RoomGenerator.xMax - RoomGenerator.xMin) * 3f + 4;
            var height = (RoomGenerator.yMax - RoomGenerator.yMin) * 3f + 4;
            AstarPath.active.data.gridGraph.center = new Vector3(0, 0, 0);
            AstarPath.active.data.gridGraph.SetDimensions((int)width, (int)height, 0.35f);
            AstarPath.active.Scan(graph);
            
            var nodes = AstarPath.active.data.gridGraph;
            EnemySpawner.Make2DArray(nodes);
        }
    }

    private void OnDoorsChange(bool openDoor)
    {
        waitedToOpenDoors = false;
        StartCoroutine(EnableDoorCol(openDoor));
    }

    private IEnumerator EnableDoorCol(bool openDoor)
    {
        if (!waitedToOpenDoors)
        {
            waitedToOpenDoors = true;
            yield return new WaitForSecondsRealtime(0.4f);
        }
        doorFolder.gameObject.SetActive(!openDoor);
    }

    private void ProcessAStarScan()
    {
        decreaseI = true;
    }

    private void ProcessPlayerSpawn()
    {
        Room startRoom = null;
        foreach (var r in RoomGenerator.Rooms)
            if (r.isStartRoom)
                startRoom = r;

        if (startRoom != null)
        {
            startRoom.hasFinished = true;
            playerTrans.position = new Vector3(startRoom.transform.position.x, startRoom.transform.position.y, 0);
            playerTrans.gameObject.SetActive(true);
        }
    }

    private static void HealthChanged(int health)
    {
        if (health <= 0) ONGameOver.Invoke();
    }
}