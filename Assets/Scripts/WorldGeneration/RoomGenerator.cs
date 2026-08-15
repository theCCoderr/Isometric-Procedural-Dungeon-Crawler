using System;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class WaveList
{
    [SerializeField] public List<EnemySo> waveList;
}

[Serializable]
public class DifficultyWaveList
{
    [SerializeField] public List<WaveList> difficultyWaveList;
}

public class RoomGenerator : MonoBehaviour
{
    public delegate void RoomsGeneratedHandler();

    public static float xMin;
    public static float xMax;
    public static float yMin;
    public static float yMax;

    private static RoomTracer[] V;

    [SerializeField] private GameObject roomsContainer;

    [SerializeField] public List<DifficultyWaveList> difficultyWaves;

    private readonly Dictionary<Room, int> connectionCounter = new Dictionary<Room, int>();
    private readonly List<Vector2> points = new List<Vector2>();

    private bool calculatedBounds;

    private int chestRoomCount;
    private List<LineSegment> delaunayTriangulation;
    private GameObject linesContainer;
    private int radius;

    private Dictionary<Vector2, Room> mainRooms;

    private float roomConnectionFrequency = 0.15f;
    private List<LineSegment> spanningTree;
    public static List<Room> Rooms { get; private set; }

    public static event RoomsGeneratedHandler OnRoomsGenerated;

    public void Generate(int roomCount, int radius, float connectionFrequency, int chestRoomCount)
    {
        linesContainer = new GameObject("Lines");
        roomConnectionFrequency = connectionFrequency;
        this.chestRoomCount = chestRoomCount;
        this.radius = radius;

        Rooms = new List<Room>();
        V = new RoomTracer[roomCount];

        //Initialize our rooms
        for (var i = 0; i < roomCount; i++)
        {
            var room = Instantiate(Resources.Load("GameObjects/Room") as GameObject).GetComponent<Room>();
            room.transform.parent = roomsContainer.transform;
            room.roomData = GameManager.roomData[Random.Range(0, GameManager.roomData.Count)];
            room.waves = difficultyWaves[room.roomData.difficulty - 1].difficultyWaveList;
            Vector3 position = GetRandomPositionInCircle(radius, room.BottomRight, room.TopLeft);

            var tilemapData = room.roomData;
            var width = tilemapData.size.x - 2;
            var height = tilemapData.size.y - 2;

            var isoPos = Helper.Iso2(position);

            //Start rooms at ID 10
            room.Init(isoPos, (int)width, (int)height); //do the tile-ing
            Rooms.Add(room);
            V[i] = new RoomTracer(i, isoPos);
        }

        RandomizedQuickSort(0, V.Length - 1, V, true, true);
        Calculate();
    }

    private Vector2 GetRandomPositionInCircle(float radius, Vector2 bottomRight, Vector2 topLeft)
    {
        var angle = Random.Range(0f, 1f) * Mathf.PI * 2f;
        var rad = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;
        var localPosition = transform.localPosition;
        var x = localPosition.x + rad * Mathf.Cos(angle);
        var y = localPosition.y + rad * Mathf.Sin(angle);
        return Snap(new Vector2(x, y), bottomRight, topLeft);
    }

    private static Vector3 Snap(Vector2 pos, Vector2 bottomRight, Vector2 topLeft)
    {
        pos = Mathf.Abs(bottomRight.x - topLeft.x) % 2 != 0
            ? new Vector2(Mathf.Round(pos.x) + .5f, pos.y)
            : new Vector2(Mathf.Round(pos.x), pos.y);
        pos = Mathf.Abs(bottomRight.y - topLeft.y) % 2 != 0
            ? new Vector2(pos.x, Mathf.Round(pos.y) + .5f)
            : new Vector2(pos.x, Mathf.Round(pos.y));

        return pos;
    }

    private static ClosestP BruteForce(int l, int h, RoomTracer[] arr, bool xAxis)
    {
        var cP = new ClosestP();
        for (var i = l; i <= h; i++)
        for (var j = l; j <= h; j++)
        {
            if (xAxis)
            {
                var dis = Math.Abs(arr[i].pos.x - arr[j].pos.x);
                if (i != j && dis <= cP.XDistance || !cP.instantiated && i != j) cP = new ClosestP(arr[i], arr[j]);
            }
            else
            {
                var dis = Math.Abs(arr[i].pos.y - arr[j].pos.y);
                if (i != j && dis <= cP.YDistance || !cP.instantiated && i != j) cP = new ClosestP(arr[i], arr[j]);
            }
        }

        return cP;
    }

    private ClosestP FindClosestPoints(int l, int h, bool xAxis)
    {
        if (h - l <= 2)
            return BruteForce(l, h, V, xAxis);
        var m = l + (h - l) / 2;
        // ReSharper disable once UnusedVariable
        var lM = V[l + (h + 1 - l) / 2].pos.x;
        var a = FindClosestPoints(l, m, xAxis);
        var b = FindClosestPoints(m + 1, h, xAxis);
        ClosestP cP1;
        if (xAxis)
        {
            cP1 = (a.XDistance >= b.XDistance) ? b : a;
        }
        else
        {
            cP1 = (a.YDistance >= b.YDistance) ? b : a;
        }

        var size = 0;
        RoomTracer[] smallV;
        if (xAxis)
        {
            for (var i = l; i <= h; i++)
                if (Inside(cP1.XDistance, lM, V[i].pos))
                    size++;
            smallV = new RoomTracer[size];
            var smallVi = 0; //TODO fix this mf :)
            for (var i = l; i <= h; i++)
                if (Inside(cP1.XDistance, lM, V[i].pos))
                    smallV[smallVi++] = V[i];
        }
        else
        {
            for (var i = l; i <= h; i++)
                if (Inside(cP1.YDistance, lM, V[i].pos))
                    size++;
            smallV = new RoomTracer[size];
            var smallVi = 0; //TODO fix this mf :)
            for (var i = l; i <= h; i++)
                if (Inside(cP1.YDistance, lM, V[i].pos))
                    smallV[smallVi++] = V[i];
        }

        RandomizedQuickSort(0, smallV.Length - 1, smallV, true, false);
        var cP2 = cP1;
        for (var i = 0; i < size; i++)
        for (var j = i + 1; j < Math.Min(size, 15); j++)
        {
            if (xAxis)
            {
                if (Math.Abs(smallV[i].pos.x - smallV[j].pos.x) < cP2.XDistance &&
                    smallV[j].pos.x - smallV[i].pos.x < cP2.XDistance && i != j)
                    cP2 = new ClosestP(smallV[i], smallV[j]);
            }
            else if (!xAxis)
            {
                if (Math.Abs(smallV[i].pos.y - smallV[j].pos.y) < cP2.YDistance &&
                    smallV[j].pos.y - smallV[i].pos.y < cP2.YDistance && i != j)
                    cP2 = new ClosestP(smallV[i], smallV[j]);
                
            }
        }

        if (xAxis)
        {
            return cP1.XDistance >= cP2.XDistance ? cP1 : cP2;
        }

        return cP1.YDistance >= cP2.YDistance ? cP1 : cP2;
    }

    // ReSharper disable once UnusedMember.Local
    private static bool Inside(double d, double lM, Vector2 p)
    {
        return Math.Abs(p.x - lM) <= d;
    }


    private static int[] Partition3(int l, int h, RoomTracer[] arr, bool inc, bool x)
    {
        int lt = l, i = l, gt = h;
        var pivot = x ? arr[l].pos.x : arr[l].pos.y;

        while (i <= gt)
        {
            var cur = x ? arr[i].pos.x : arr[i].pos.y;
            if (cur < pivot && inc || cur > pivot && !inc)
            {
                Swap(lt, i, arr);
                lt++;
                i++;
            }
            else if (cur > pivot && inc || cur < pivot && !inc)
            {
                Swap(gt, i, arr);
                gt--;
            }
            else
            {
                i++;
            }
        }

        return new[] { lt, gt };
    }

    private static void RandomizedQuickSort(int l, int h, RoomTracer[] arr, bool increasing, bool x)
    {
        if (l >= h) return;
        var k = l + Random.Range(0, int.MaxValue - 1) % (h - l + 1);
        Swap(l, k, arr);
        var m = Partition3(l, h, arr, increasing, x);

        RandomizedQuickSort(l, m[0] - 1, arr, increasing, x);
        RandomizedQuickSort(m[1] + 1, h, arr, increasing, x);
    }

    /// <summary>
    ///     TODO the thing that you should do is check whats changing the pos of the rooms ps: I have changed the room num to
    ///     be 5
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="arr"></param>
    private static void Swap(int a, int b, RoomTracer[] arr)
    {
        var c = arr[a];
        var z = arr[b];
        arr[a] = z;
        arr[b] = c;
    }

    //Create the smallest path possible between all our main rooms
    private System.Collections.IEnumerator GenerateSpanningTree(bool drawLines)
    {
        foreach (var t in spanningTree)
        {
            if (drawLines)
            {
                var line = Instantiate(Resources.Load("GameObjects/Line") as GameObject, linesContainer.transform, true);
                if (t.p0 != null) line.GetComponent<LineRenderer>().SetPosition(0, t.p0.Value);
                if (t.p1 != null) line.GetComponent<LineRenderer>().SetPosition(1, t.p1.Value);
                line.GetComponent<LineRenderer>().sortingOrder = 100;
                line.GetComponent<LineRenderer>().startWidth = 2.5f; // Adjust this if the line is too thick/thin
                line.GetComponent<LineRenderer>().endWidth = 2.5f;

                // THIS IS THE MAGIC LINE! It pauses the code for 0.05 seconds before drawing the next line.
                yield return new WaitForSeconds(0.05f);
            }

            //Create an index so we can keep track of the actual connections count of each main room
            Debug.Assert(t.p0 != null, "t.p0 != null");
            if (t.p0 != null && !connectionCounter.ContainsKey(mainRooms[t.p0.Value]))
                connectionCounter.Add(mainRooms[t.p0.Value], 0);
            Debug.Assert(t.p1 != null, "t.p1 != null");
            if (t.p1 != null && !connectionCounter.ContainsKey(mainRooms[t.p1.Value]))
                connectionCounter.Add(mainRooms[t.p1.Value], 0);

            //increment the counter
            if (t.p0 != null && t.p1 != null)
            {
                connectionCounter[mainRooms[t.p0.Value]]++;
                connectionCounter[mainRooms[t.p1.Value]]++;
                //Add the room connection to the Room object
                mainRooms[t.p0.Value].AddRoomConnection(CreateRoomConnection(t.p0.Value, t.p1.Value));
            }
        }

        StartCoroutine(AddExtraConnections(drawLines));
    }


    //In order for our dungeon to look interesting, we will add more connections to our minimum spanning tree
    //In order for our dungeon to look interesting, we will add more connections to our minimum spanning tree
    private System.Collections.IEnumerator AddExtraConnections(bool drawLines)
    {
        var range = new List<int>();
        for (var n = 0; n < delaunayTriangulation.Count; n++) range.Add(n);

        for (var n = 0; n < (int)(delaunayTriangulation.Count * roomConnectionFrequency); n++)
        {
            var idx = Random.Range(0, range.Count);
            var value = range[idx];
            range.RemoveAt(idx);

            if (drawLines)
            {
                var line = Instantiate(Resources.Load("GameObjects/Line") as GameObject,
                    linesContainer.transform, true);

                var lr = line.GetComponent<LineRenderer>();

                var vector2 = delaunayTriangulation[value].p0;
                if (vector2 != null)
                    lr.SetPosition(0, vector2.Value);
                var p1 = delaunayTriangulation[value].p1;
                if (p1 != null)
                    lr.SetPosition(1, p1.Value);

                lr.sortingOrder = 99;

                // Set the color to Magenta so you can tell the difference between main paths and extra loops!
                lr.startColor = Color.magenta;
                lr.endColor = Color.magenta;
                lr.startWidth = 1.5f; // Adjust this if the line is too thick/thin
                lr.endWidth = 1.5f;

                // The Magic Pause for the extra connections!
                yield return new WaitForSeconds(0.05f);
            }

            //Create an index so we can keep track of the actual connections count of each main room
            var p0 = delaunayTriangulation[value].p0;
            if (p0 != null && !connectionCounter.ContainsKey(mainRooms[p0.Value]))
            {
                var vector2 = delaunayTriangulation[value].p0;
                if (vector2 != null)
                    connectionCounter.Add(mainRooms[vector2.Value], 0);
            }

            var vector3 = delaunayTriangulation[value].p1;
            if (vector3 != null && !connectionCounter.ContainsKey(mainRooms[vector3.Value]))
            {
                var vector2 = delaunayTriangulation[value].p1;
                if (vector2 != null)
                    connectionCounter.Add(mainRooms[vector2.Value], 0);
            }

            //increment the counter
            var o = delaunayTriangulation[value].p0;
            if (o != null)
            {
                connectionCounter[mainRooms[o.Value]]++;
                var vector2 = delaunayTriangulation[value].p1;
                if (vector2 != null)
                {
                    connectionCounter[mainRooms[vector2.Value]]++;


                    mainRooms[o.Value].AddRoomConnection(
                        CreateRoomConnection(o.Value,
                            vector2.Value));
                }
            }

        }
        ProcessRoomConnections();
        SetStartAndEndAndChestRooms();

        foreach (var t in Rooms)
        {
            xMin = Mathf.Min(xMin, t.TopLeft.x);
            xMax = Mathf.Max(xMax, t.BottomRight.x);
            yMin = Mathf.Min(yMin, t.BottomRight.y);
            yMax = Mathf.Max(yMax, t.TopLeft.y);
        }
        OnRoomsGenerated?.Invoke();

    }

    private RoomConnection CreateRoomConnection(Vector2 p0, Vector2 p1)
    {
        //Create the room connection
        var room = mainRooms[p1];

        //Determine the direction of the connection
        ConnectionType direction;

        var xDiff = Mathf.Abs(p0.x - p1.x);
        var yDiff = Mathf.Abs(p0.y - p1.y);

        if (xDiff > yDiff)
            direction = p0.x > p1.x ? ConnectionType.Left : ConnectionType.Right;
        else
            direction = p0.y > p1.y ? ConnectionType.Down : ConnectionType.Up;

        return new RoomConnection(room, direction);
    }

    //Create the lines between main rooms and find secondary rooms
    private static void ProcessRoomConnections()
    {
        foreach (var t in Rooms)
        foreach (var t1 in t.Connections)
        {
            var connectingRoom = t1.Room;
            var direction = t1.Direction;

            //Get line points
            Vector2 p0 = t.Center;
            Vector2 p1 = connectingRoom.Center;
            var p2 = Vector2.zero;
            var p3 = Vector2.zero;

            if (direction == ConnectionType.Up)
            {
                p2 = new Vector2(p0.x, p1.y);
                p3 = p2;
                //Hallways are off by 3 pixels in this direction only.  Not sure why.
                //Adjust by 3 units
                if (p0.x > p1.x) p3 = new Vector2(p0.x, p1.y + 3);
            }
            else if (direction == ConnectionType.Right)
            {
                p2 = new Vector2(p1.x, p0.y);
                p3 = p2;
            }
            else if (direction == ConnectionType.Down)
            {
                p2 = new Vector2(p0.x, p1.y);
                p3 = p2;
            }
            else if (direction == ConnectionType.Left)
            {
                p2 = new Vector2(p1.x, p0.y);
                p3 = p2;
            }

            //Store lines
            t1.line1 = new LineSegment(p0, p3);
            t1.line2 = new LineSegment(p2, p1);
        }
    }

    private void SetStartAndEndAndChestRooms()
    {
        var roomsWithOneConnection = new List<Room>();
        var roomsWithTwoConnection = new List<Room>();

        //check connection counters
        foreach (var kvp in connectionCounter)
        {
            if (kvp.Value == 1) roomsWithOneConnection.Add(kvp.Key);

            if (kvp.Value == 2) roomsWithTwoConnection.Add(kvp.Key);
        }

        Room start = null;
        Room end = null;
        var chestRooms = new Room[chestRoomCount];
        float distance = 0;

        //attempt to grab start room
        if (roomsWithOneConnection.Count >= 1)
        {
            start = roomsWithOneConnection[0];
            roomsWithOneConnection.RemoveAt(0);
        }
        else if (roomsWithTwoConnection.Count > 1)
        {
            start = roomsWithTwoConnection[0];
            roomsWithTwoConnection.RemoveAt(0);
        }

        for (var i = 0; i < chestRoomCount; i++)
            if (roomsWithOneConnection.Count >= 1)
            {
                chestRooms[i] = roomsWithOneConnection[0];
                roomsWithOneConnection.RemoveAt(0);
            }

            else if (roomsWithTwoConnection.Count >= 1)
            {
                chestRooms[i] = roomsWithTwoConnection[0];
                roomsWithTwoConnection.RemoveAt(0);
            }


        //attempt to grab end room
        if (start != null)
        {
            foreach (var t in roomsWithOneConnection)
            {
                var d = (t.Center - start.Center).magnitude;
                if (d > distance)
                {
                    distance = d;
                    end = t;
                }
            }

            foreach (var t in roomsWithTwoConnection)
            {
                var d = (t.Center - start.Center).magnitude;
                if (d > distance)
                {
                    distance = d;
                    end = t;
                }
            }
        }

        //if both start and end are found, set them
        if (start != null && end != null)
        {
            start.SetStartRoom();
            end.SetEndRoom();
        }

        //making ordinary rooms chest rooms
        foreach (var r in chestRooms)
            if (r != null)
            {
                r.SetChestRoom();
                r.roomData = GameManager.specialRoomData.Find(data => data.name == "chest");
                var tilemapData = r.roomData;
                var width = tilemapData.size.x;
                var height = tilemapData.size.y;
                r.Init(r.Center, (int)width, (int)height);
            }

        if (end != null)
        {
            end.roomData = GameManager.specialRoomData.Find(data => data.name == "firstBoss");
            var tilemapData = end.roomData;
            var width = tilemapData.size.x;
            var height = tilemapData.size.y;
            end.Init(end.Center, (int) width, (int) height);
        }
    }
   
    //TODO: do a double cP one for xs, the other for ys
    private void Calculate()
    {
        var random = new System.Random();
        while (true)
        {
            if (!calculatedBounds)
                foreach (var t in Rooms)
                {
                    xMin = Mathf.Min(xMin, t.TopLeft.x);
                    xMax = Mathf.Max(xMax, t.BottomRight.x);
                    yMin = Mathf.Min(yMin, t.BottomRight.y);
                    yMax = Mathf.Max(yMax, t.TopLeft.y);
                }

            calculatedBounds = true;
            RandomizedQuickSort(0, V.Length - 1, V, true, true);
            var cP1 = BruteForce(0, Rooms.Count - 1, V, true);
            var xDis1 = cP1.XDistance;
            var yDis1 = cP1.YDistance;
            var angle1 = Math.Abs(Vector3.SignedAngle(Rooms[cP1.r1.index].Center, Rooms[cP1.r2.index].Center, transform.up) - 90.0f);
            double newMinX1 = (Math.Abs(Rooms[cP1.r1.index].TopLeft.x - Rooms[cP1.r1.index].BottomRight.x) +
                               Math.Abs(Rooms[cP1.r2.index].TopLeft.x - Rooms[cP1.r2.index].BottomRight.x)) / 10;
            var cos1 = Math.Cos(angle1 * (Math.PI / 180.0));
            var sin1 = Math.Sin(angle1 * (Math.PI / 180.0));
            var mul1 = Math.Abs(cos1 + sin1);
            var newMaxX1 = mul1 * (Math.Abs(Rooms[cP1.r1.index].TopLeft.x - Rooms[cP1.r1.index].BottomRight.x)
                                   + Math.Abs(Rooms[cP1.r2.index].TopLeft.x - Rooms[cP1.r2.index].BottomRight.x)) / 2;
            var newMaxY1 = mul1 * (Math.Abs(Rooms[cP1.r1.index].TopLeft.y - Rooms[cP1.r1.index].BottomRight.y)
                                   + Math.Abs(Rooms[cP1.r2.index].TopLeft.y - Rooms[cP1.r2.index].BottomRight.y)) / 2;

            RandomizedQuickSort(0, V.Length - 1, V, true, false);
            var cP = BruteForce(0, Rooms.Count - 1, V, false);
            var xDis = cP.XDistance;
            var yDis = cP.YDistance;
            var angle = Math.Abs(Vector3.SignedAngle(Rooms[cP.r1.index].Center, Rooms[cP.r2.index].Center, transform.up) - 90.0f);
            double newMinY = (Math.Abs(Rooms[cP.r1.index].TopLeft.y - Rooms[cP.r1.index].BottomRight.y) +
                              Math.Abs(Rooms[cP.r2.index].TopLeft.y - Rooms[cP.r2.index].BottomRight.y)) / 10;
            var cos = Math.Cos(angle * (Math.PI / 180.0));
            var sin = Math.Sin(angle * (Math.PI / 180.0));
            var mul = Math.Abs(cos + sin);
            var newMaxX = mul * (Math.Abs(Rooms[cP.r1.index].TopLeft.x - Rooms[cP.r1.index].BottomRight.x)
                                 + Math.Abs(Rooms[cP.r2.index].TopLeft.x - Rooms[cP.r2.index].BottomRight.x)) / 2;
            var newMaxY = mul * (Math.Abs(Rooms[cP.r1.index].TopLeft.y - Rooms[cP.r1.index].BottomRight.y)
                                 + Math.Abs(Rooms[cP.r2.index].TopLeft.y - Rooms[cP.r2.index].BottomRight.y)) / 2;

            if (!((xDis1 < newMinX1 && xDis1 < newMaxX1 && yDis1 > newMaxY1) || (xDis1 > newMaxX1 && yDis1 > newMaxY1)))
            {
                var newX = random.Next(-radius, radius);
                var newY = random.Next(-radius, radius);
                var room = Rooms[cP1.r1.index];
                Rooms[cP1.r1.index].transform.position = Snap(new Vector2(newX, newY),
                    room.BottomRight, room.TopLeft);
                for (var n = 0; n < V.Length; n++)
                    if (V[n].index == cP1.r1.index)
                    {
                        V[n].pos = Rooms[cP1.r1.index].transform.position;
                        break;
                    }
            }

            else if (!((yDis < newMinY && yDis < newMaxY && xDis > newMaxX) || (yDis > newMaxY && xDis > newMaxX)))
            {
                var newX = random.Next(-radius, radius);
                var newY = random.Next(-radius, radius);
                var room = Rooms[cP.r1.index];
                Rooms[cP.r1.index].transform.position = Snap(new Vector2(newX, newY),
                    room.BottomRight, room.TopLeft);
                for (var n = 0; n < V.Length; n++)
                    if (V[n].index == cP.r1.index)
                    {
                        V[n].pos = Rooms[cP.r1.index].transform.position;
                        break;
                    }
            }
            else
            {
                var metReq = true;
                for (var i = 0; i < Rooms.Count; i++)
                {
                    for (var j = i; j < Rooms.Count; j++)
                    {
                        var r1 = Rooms[i];
                        var r2 = Rooms[j];
                        if (r1 != r2)
                        {
                            var xDis2 = Math.Abs(r1.Center.x - r2.Center.x);
                            var yDis2 = Math.Abs(r1.Center.y - r2.Center.y);
                            var angle2 = Math.Abs(Vector3.SignedAngle(r1.Center, r2.Center, transform.up) - 90.0f);
                            var newMinX2 = (Math.Abs(r1.TopLeft.x - r1.BottomRight.x) + Math.Abs(r2.TopLeft.x - r2.BottomRight.x)) / 10;
                            var newMinY2 = (Math.Abs(r1.TopLeft.y - r1.BottomRight.y) + Math.Abs(r2.TopLeft.y - r2.BottomRight.y)) / 10;
                            var cos2 = Math.Cos(angle2 * (Math.PI / 180.0));
                            var sin2 = Math.Sin(angle2 * (Math.PI / 180.0));
                            var mul2 = Math.Abs(cos2 + sin2);
                            var newMaxX2 = mul2 * (Math.Abs(r1.TopLeft.x - r1.BottomRight.x) +
                                                   Math.Abs(r2.TopLeft.x - r2.BottomRight.x)) / 2;
                            var newMaxY2 = mul2 * (Math.Abs(r1.TopLeft.y - r1.BottomRight.y) +
                                                   Math.Abs(r2.TopLeft.y - r2.BottomRight.y)) / 2;

                            if (!((xDis2 < newMinX2 && xDis2 < newMaxX2 && yDis2 > newMaxY2) ||
                                  (xDis2 > newMaxX2 && yDis2 > newMaxY2) ||
                                  (yDis2 < newMinY2 && yDis2 < newMaxY2 && xDis2 > newMaxX2)))
                            {
                                metReq = false;
                                var newX = random.Next(-radius, radius);
                                var newY = random.Next(-radius, radius);
                                var room = r1;
                                r1.transform.position = Snap(new Vector2(newX, newY),
                                    room.BottomRight, room.TopLeft);
                                for (var n = 0; n < V.Length; n++)
                                    if (Rooms[V[n].index] == r1)
                                        V[n].pos = r1.transform.position;
                            }
                        }
                    }
                }
                if (metReq) break;
            }
        }
        if (!calculatedBounds)
            foreach (var t in Rooms)
            {
                xMin = Mathf.Min(xMin, t.TopLeft.x);
                xMax = Mathf.Max(xMax, t.BottomRight.x);
                yMin = Mathf.Min(yMin, t.BottomRight.y);
                yMax = Mathf.Max(yMax, t.TopLeft.y);
            }

        var colors = new List<uint>();
        mainRooms = new Dictionary<Vector2, Room>();

        //Get a point list of all our main rooms
        foreach (var t in Rooms)
        {
            points.Add(t.Center);
            colors.Add(0);
            mainRooms.Add(t.Center, t);
        }

        var voronoi = new Voronoi(points, colors, new Rect(0, 0, 50, 50));
        spanningTree = voronoi.SpanningTree();
        delaunayTriangulation = voronoi.DelaunayTriangulation();
        StartCoroutine(GenerateSpanningTree(true));
    }

    private struct ClosestP
    {
        /*double res;
                if (r1.pos.x >= r2.pos.x)
                {
                    if (r1.pos.y >= r2.pos.y) //if r1 is above and to the right of r2
                    {
                        res = CalcDis(r1.pos - r1.size, r2.pos + r2.size);
                    }
                    else //if r1 is below and to the right of r2
                    {
                        res = CalcDis(new Vector2(r1.pos.x - r1.size.x, r1.pos.y + r1.size.y),
                            new Vector2(r2.pos.x + r2.size.x, r2.pos.y - r2.size.y));
                    }
                }
                else
                {
                    if (r1.pos.y >= r2.pos.y) //if r1 is above and to the left of r2
                    {
                        res = CalcDis(new Vector2(r1.pos.x + r1.size.x, r1.pos.y - r1.size.y),
                            new Vector2(r2.pos.x - r2.size.x, r2.pos.y + r2.size.y));
                    }
                    else //if r1 is below and to the left of r2
                    {
                        res = CalcDis(r1.pos + r1.size, r2.pos - r2.size);
                    }
                }

                return res;*/
        public double XDistance => Mathf.Abs(r2.pos.x - r1.pos.x);

        /*if (r1.pos.x >= r2.pos.x)
                    return Mathf.Abs(r2.pos.x + r2.size.x - (r1.pos.x - r1.pos.x));
                return Mathf.Abs(r2.pos.x - r2.size.x - (r1.pos.x + r1.pos.x));*/
        public double YDistance => Mathf.Abs(r2.pos.y - r1.pos.y);

        /*if (r1.pos.y >= r2.pos.y)
                    return Mathf.Abs(r2.pos.y + r2.size.y - (r1.pos.y - r1.pos.y));
                return Mathf.Abs(r2.pos.y - r2.size.y - (r1.pos.y + r1.pos.y));*/
        public readonly RoomTracer r1;
        public readonly RoomTracer r2;
        public readonly bool instantiated;

        public ClosestP(RoomTracer r1, RoomTracer r2)
        {
            this.r1 = r1;
            this.r2 = r2;
            instantiated = true;
        }
    }

    private struct RoomTracer
    {
        public int index;
        public Vector2 pos;

        public RoomTracer(int index, Vector2 pos)
        {
            this.index = index;
            this.pos = pos;
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Rooms == null) return;

        foreach (var room in Rooms)
        {
            if (room == null) continue;

            // Highlight special rooms in Red (Start, End, Chest rooms)
            Color roomColor;
            if ((room.isStartRoom || room.isEndRoom))
            {
                roomColor = Color.red;
            }
            else if (room.isChestRoom) roomColor = Color.yellow;
            else
            {
                roomColor = Color.white;
            }

            UnityEditor.Handles.color = roomColor;

            // Calculate the 4 corners of the room based on your existing TopLeft/BottomRight data
            Vector3 topLeft = new Vector3(room.TopLeft.x, room.TopLeft.y, 0);
            Vector3 topRight = new Vector3(room.BottomRight.x, room.TopLeft.y, 0);
            Vector3 bottomRight = new Vector3(room.BottomRight.x, room.BottomRight.y, 0);
            Vector3 bottomLeft = new Vector3(room.TopLeft.x, room.BottomRight.y, 0);

            // Draw a thick Anti-Aliased line connecting the 4 corners and looping back to the start
            // THE FIRST NUMBER (5f) IS THE THICKNESS! Crank it up to 8f or 10f if you want it super bold.
            UnityEditor.Handles.DrawAAPolyLine(13f, topLeft, topRight, bottomRight, bottomLeft, topLeft);
        }
    }
#endif
}

