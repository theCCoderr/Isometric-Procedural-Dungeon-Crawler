using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
// ReSharper disable InconsistentNaming

namespace Amr
{
    public enum TT
    {
        //add additional entries in the minus area to maintain the correctness of the serialization of the dungeon generator floor tiles
        //with the saves. /also check l 164
        Debug = -100,
        Background = -5,
        FakeFloor = -4,
        OgWall2 = -3,
        OgWall012R = -2,
        HiddenWall = -1,
        Nothing = 0,
        Hallway = 1,
        OgWall012L = 2,
        OgDoor = 3,
        AlreadyPlacedRoomTile = 4,
        LitWall = 6
    }

    public class DungeonGenerator : MonoBehaviour
    {
        public static readonly UnityEvent ONTilesGenerated = new UnityEvent();

        // ReSharper disable once InconsistentNaming
        public static int[][] grid;

        [Description("the delay that ensures that rooms are generated and stopped moving")]
        private static float TimeDelay = 1f;

        private static bool TilesGenerated;

        [SerializeField] [Range(1, 100)] private int roomCount = 10;
        [SerializeField] [Range(1, 1000)] private int radius = 150;
        [SerializeField] [Range(0, 1)] private float roomConnectionFrequency;
        [SerializeField] private int chestRoomCount = 2;

        [SerializeField] private Tilemap floorTilemap;

        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap wall1Tilemap;
        [SerializeField] private Tilemap wall2Tilemap;

        [SerializeField] private Tilemap doorTilemap;
        [SerializeField] private Tilemap door1Tilemap;
        [SerializeField] private Tilemap door2Tilemap;

        [SerializeField] public List<TileBase> floorTiles;
        [SerializeField] private TileBase[] wallSprites;
        [SerializeField] private TileBase[] doorTiles;
        
        [SerializeField] private TileBase debugTile;
        [SerializeField] private TileBase blankTile;

        [SerializeField] private GameObject lightContainer;
        [SerializeField] private GameObject chestContainer;
        [SerializeField] private GameObject roomContainer;

        [SerializeField] private GameObject wallLight;

        [Description("true is for horizontal, false is for vertical")]
        private Dictionary<Vector2, bool> doors;

        private int gridHeight;
        private int gridWidth;

        private List<HallwayRequest> hallwayRequests;

        [Description("coordinates in grid position")]
        private List<RoomTile> roomTiles;
        
        [Description("coordinates in grid position")]
        private List<Vector2> hiddenWalls;

        [Description("coordinates in grid position")]
        private List<Vector2> backgroundTiles;

        [Description("coordinates in world position")]
        private List<Vector2> litWalls;

        private RoomGenerator roomGenerator;
        private List<Room> rooms;

        private void Start()
        {
            hallwayRequests = new List<HallwayRequest>();
            doors = new Dictionary<Vector2, bool>();
            roomTiles = new List<RoomTile>();
            litWalls = new List<Vector2>();
            hiddenWalls = new List<Vector2>();
            backgroundTiles = new List<Vector2>();
            roomGenerator = transform.Find("RoomGenerator").GetComponent<RoomGenerator>();
            RoomGenerator.OnRoomsGenerated += RoomGenerator_OnRoomsGenerated;

            //Generates rooms and room connections
            roomGenerator.Generate(roomCount, radius, roomConnectionFrequency, chestRoomCount);
        }

        /*private void Update()
        {
            TimeDelay -= Time.deltaTime;
            if (TimeDelay <= 0 && !TilesGenerated)
            {
                rooms = RoomGenerator.Rooms;
                InitializeDungeonGeneration();
                TilesGenerated = true;
                ONTilesGenerated.Invoke();
            }
        }*/

        private void RoomGenerator_OnRoomsGenerated()
        {
            rooms = RoomGenerator.Rooms;
            TimeDelay = 0f;
        }

        private void CreateGrid()
        {
            //Add padding for walls
            var gridSizeIncrease = 20; //umm so four for checking the wall sprite and three for original placing? fuck me I don't understand shit
            gridWidth = (int)(RoomGenerator.xMax - RoomGenerator.xMin) + gridSizeIncrease;
            gridHeight = (int)(RoomGenerator.yMax - RoomGenerator.yMin) + gridSizeIncrease;

            // ReSharper disable once HeapView.ObjectAllocation.Evident
            grid = new int[gridWidth][];
            for (var i = 0; i < grid.Length; i++) grid[i] = new int[gridHeight];

            //Create initial grid with room IDs
            foreach (var t in rooms)
            {
                var startX = (int)t.TopLeft.x - (int)RoomGenerator.xMin;
                var startY = (int)t.BottomRight.y - (int)RoomGenerator.yMin;
                var endX = startX + t.roomData.size.x;
                var endY = startY + t.roomData.size.y;

                var tiles = new int[(int)t.roomData.size.x][];
                for (var index2 = 0; index2 < (int)t.roomData.size.x; index2++)
                    tiles[index2] = new int[(int)t.roomData.size.y];

                var index = 0;
                for (var y2 = 0; y2 < t.roomData.size.y; y2++)
                for (var x2 = 0; x2 < t.roomData.size.x; x2++)
                {
                    tiles[x2][y2] = t.roomData.tiles[index];
                    index++;
                }

                for (var x = startX; x < endX; x++)
                for (var y = startY; y < endY; y++)
                    if (gridWidth > x && gridHeight > y)
                    {
                        if (tiles[x - startX][y - startY] == (int)TT.LitWall)
                        {
                            var worldPos = GridToWorld(x, y);
                            litWalls.Add(new Vector2(worldPos.x, worldPos.y));
                        }

                        if (tiles[x - startX][y - startY] == (int)TT.Nothing)
                        {
                            grid[x][y] = (int)TT.HiddenWall;
                            hiddenWalls.Add(new Vector2(x, y));
                        }

                        // make sure that those tiles are actually floor
                        else if (tiles[x - startX][y - startY] > 6)
                        {
                            if (x < grid.Length && y < gridHeight)
                            {
                                grid[x][y] = (int)TT.AlreadyPlacedRoomTile;
                                // the minus 6 is explained in the RoomTilemapManager
                                Debug.Log(t.roomData.name);
                                var tile = floorTiles[tiles[x - startX][y - startY] - 7];
                                roomTiles.Add(new RoomTile(new Vector2(x,y), tile));
                            }
                        }
                    }
            }

            //  HERE IS THE ALL THE VORONOI AND TRIANGULAR SHIT
            //Complete missing sections of map based on LineCasts created in Room Generator
            foreach (var t in rooms)
            foreach (var t1 in t.Connections)
            {
                // ReSharper disable once PossibleInvalidOperationException
                var p0 = t1.line1.p0.Value;
                // ReSharper disable once PossibleInvalidOperationException
                var p1 = t1.line1.p1.Value;
                // ReSharper disable once PossibleInvalidOperationException
                var p2 = t1.line2.p0.Value;
                // ReSharper disable once PossibleInvalidOperationException
                var p3 = t1.line2.p1.Value;

                //flip values if line is going in opposite direction
                if ((int)p0.x > (int)p1.x || (int)p0.y > (int)p1.y)
                {
                    p0 = t1.line1.p1.Value;
                    p1 = t1.line1.p0.Value;
                }

                if ((int)p2.x > (int)p3.x || (int)p2.y > (int)p3.y)
                {
                    p3 = t1.line2.p0.Value;
                    p2 = t1.line2.p1.Value;
                }

                //Adjust lines to grid coordinates
                p0 = new Vector2(p0.x - RoomGenerator.xMin, p0.y - RoomGenerator.yMin);
                p1 = new Vector2(p1.x - RoomGenerator.xMin, p1.y - RoomGenerator.yMin);
                p2 = new Vector2(p2.x - RoomGenerator.xMin, p2.y - RoomGenerator.yMin);
                p3 = new Vector2(p3.x - RoomGenerator.xMin, p3.y - RoomGenerator.yMin);

                //Create the hallways in our grid
                hallwayRequests.Add(new HallwayRequest(p0, p1));
                hallwayRequests.Add(new HallwayRequest(p2, p3));
            }
        }

        private void AddHallwayAndDoors(Vector2 p0, Vector2 p1)
        {
            //Vertical direction
            if ((int)p1.x == (int)p0.x)
                for (var y = (int)p0.y; y < (int)p1.y + 1; y++)
                {
                    //if the tile is a wall then make it a door
                    if (IsW(grid[(int)p1.x][y]))
                        if (grid[(int)p1.x][y + 1] == (int)TT.Nothing &&
                            grid[(int)p1.x][y - 1] == (int)TT.AlreadyPlacedRoomTile
                            || grid[(int)p1.x][y + 1] == (int)TT.AlreadyPlacedRoomTile &&
                            grid[(int)p1.x][y - 1] == (int)TT.Hallway)
                        {
                            //hasPutDoor = true;
                            if (!doors.ContainsKey(new Vector2(p1.x, y)))
                            {
                                doors.Add(new Vector2(p1.x, y), false);
                            }

                            grid[(int)p1.x][y] = (int)TT.Hallway;
                            grid[(int)p1.x + 1][y] = (int)TT.Hallway;
                            grid[(int)p1.x + 2][y] = (int)TT.Hallway;
                            grid[(int)p1.x + 3][y] = (int)TT.Hallway;
                        }

                    //if the tile is nothing then make it a hallway
                    //if (grid[(int)p1.x][y] == (int)TT.Nothing) grid[(int)p1.x][y] = (int)TT.Hallway;

                    //make hallways 4 units wide
                    if ((int)p1.x < gridWidth - 3)
                    {
                        if (grid[(int)p1.x + 0][y] == (int)TT.Nothing || grid[(int)p1.x + 0][y] == (int)TT.FakeFloor)
                        {
                            grid[(int)p1.x][y] = (int)TT.Hallway;
                        }

                        if (grid[(int)p1.x + 1][y] == (int)TT.Nothing || grid[(int)p1.x + 1][y] == (int)TT.FakeFloor)
                            grid[(int)p1.x + 1][y] = (int)TT.Hallway;
                        if (grid[(int)p1.x + 2][y] == (int)TT.Nothing || grid[(int)p1.x + 2][y] == (int)TT.FakeFloor)
                            grid[(int)p1.x + 2][y] = (int)TT.Hallway;
                        if (grid[(int)p1.x + 3][y] == (int)TT.Nothing || grid[(int)p1.x + 3][y] == (int)TT.FakeFloor)
                            grid[(int)p1.x + 3][y] = (int)TT.Hallway;
                    }
                }

            //Horizontal direction
            else if ((int)p1.y == (int)p0.y)
                for (var x = (int)p0.x; x < (int)p1.x; x++)
                {
                    //if the tile is a wall then make it a door
                    if (IsW(grid[x][(int)p1.y]))
                        if (grid[x + 1][(int)p1.y] == (int)TT.Nothing &&
                            grid[x - 1][(int)p1.y] == (int)TT.AlreadyPlacedRoomTile
                            || grid[x + 1][(int)p1.y] == (int)TT.AlreadyPlacedRoomTile &&
                            grid[x - 1][(int)p1.y] == (int)TT.Hallway)
                        {
                            grid[x][(int)p1.y] = (int)TT.Hallway;
                            grid[x][(int)p1.y + 1] = (int)TT.Hallway;
                            grid[x][(int)p1.y + 2] = (int)TT.Hallway;
                            grid[x][(int)p1.y + 3] = (int)TT.Hallway;

                            if (!doors.ContainsKey(new Vector2(x, p1.y))) doors.Add(new Vector2(x, p1.y), true);
                        }

                    //if the tile is nothing then make it a hallway
                    //if (grid[x][(int)p1.y] == (int)TT.Nothing) grid[x][(int)p1.y] = (int)TT.Hallway;
                    if ((int)p1.x < gridWidth - 3)
                    {
                        if (grid[x][(int)p1.y] == (int)TT.Nothing || grid[x][(int)p1.y] == (int)TT.FakeFloor)
                            grid[x][(int)p1.y] = (int)TT.Hallway;
                        if (grid[x][(int)p1.y + 1] == (int)TT.Nothing || grid[x][(int)p1.y + 1] == (int)TT.FakeFloor)
                            grid[x][(int)p1.y + 1] = (int)TT.Hallway;
                        if (grid[x][(int)p1.y + 2] == (int)TT.Nothing || grid[x][(int)p1.y + 2] == (int)TT.FakeFloor)
                            grid[x][(int)p1.y + 2] = (int)TT.Hallway;
                        if (grid[x][(int)p1.y + 3] == (int)TT.Nothing || grid[x][(int)p1.y + 3] == (int)TT.FakeFloor)
                            grid[x][(int)p1.y + 3] = (int)TT.Hallway;
                    }
                }
        }

        private void ProcessHallwayRequests()
        {
            foreach (var h in hallwayRequests)
            {
                AddHallwayAndDoors(h.v1, h.v2);
            }
        }

        private void AddWalls()
        {
            for (var y = 1; y < gridHeight - 1; y++)
            for (var x = 1; x < gridWidth - 1; x++)
            {

                if (grid[x][y] == (int)TT.Nothing || grid[x][y] == (int)TT.HiddenWall)
                {
                    if (IsF(grid[x - 1][y]))
                        grid[x][y] = (int)TT.OgWall012R;

                    else if (IsF(grid[x + 1][y]))
                        grid[x][y] = (int)TT.OgWall012R;

                    else if (IsF(grid[x][y - 1]) && !IsW(grid[x][y + 1]))
                        grid[x][y] = (int)TT.OgWall012L;

                    else if (IsF(grid[x][y + 1]) && !IsW(grid[x][y]))
                        grid[x][y] = (int)TT.OgWall012L;
                }
            }
            ProcessHallwayRequests();

            var gridCopy = new int[gridWidth + 10][];
            for (var i = 0; i < gridCopy.Length; i++) gridCopy[i] = new int[gridHeight + 10];
            
            for (var i = 0; i < grid.Length; i++)
            {
                for (var j = 0; j < grid[i].Length; j++)
                {
                    gridCopy[i + 5][j + 5] = grid[i][j];
                }
            }

            for (var i = 0; i < grid.Length; i++)
            {
                for (var j = 0; j < grid[i].Length; j++)
                {
                    grid[i][j] = gridCopy[i][j];
                }
            }

            foreach (var v in roomTiles)
            {
                SetTile((int)v.v.x + 5, (int)v.v.y + 5, v.t);
            }
            
            //Process
            for (var y = 1; y < gridHeight - 1; y++)
            for (var x = 1; x < gridWidth - 1; x++)
            {

                if (grid[x][y] == (int)TT.Nothing || grid[x][y] == (int)TT.HiddenWall || IsW(grid[x][y]))

                {
                    if (IsF(grid[x - 1][y]))
                        grid[x][y] = (int)TT.OgWall012R;

                    else if (IsF(grid[x - 1][y - 1]))
                        grid[x][y] = (int)TT.OgWall012R;

                    else if (IsF(grid[x - 1][y + 1]))
                        grid[x][y] = (int)TT.OgWall2;

                    else if (IsF(grid[x + 1][y]))
                        grid[x][y] = (int)TT.OgWall2;

                    else if (IsF(grid[x + 1][y + 1]))
                        grid[x][y] = (int)TT.OgWall2;

                    else if (IsF(grid[x + 1][y - 1]))
                        grid[x][y] = (int)TT.OgWall2;

                    else if (IsF(grid[x][y - 1]) && !IsW(grid[x][y + 1]))
                        grid[x][y] = (int)TT.OgWall012L;

                    else if (IsF(grid[x][y + 1]) && !IsW(grid[x][y]))
                        grid[x][y] = (int)TT.OgWall2;
                    else if (IsF(grid[x][y + 1]) && IsW(grid[x][y]) && IsW(grid[x][y - 1]) && IsF(grid[x - 1][y]))
                        grid[x][y] = (int)TT.OgWall012R;
                }
            }
        }

        private static bool IsF(int x) => x == (int)TT.AlreadyPlacedRoomTile || x == (int)TT.Hallway || x == (int)TT.OgDoor || x == (int)TT.FakeFloor;
        

        private static bool IsW(int x)
        {
            return x == (int)TT.OgWall012L || x == (int)TT.OgWall012R || x == (int)TT.OgWall2;
        }


        private TileBase GetWallSprite(int x, int y)
        {
            TileBase res = null;
            //first two in the wallSprites array are middle and then lower wall after those come all the other upper walls in order from left to right
            if (x > 0 && y > 1 && x < grid.Length - 2 && y < grid[0].Length - 2)
            {
                var l = IsW(grid[x - 1][y]);
                var r = IsW(grid[x + 1][y]);
                var u = IsW(grid[x][y + 1]);
                var d = IsW(grid[x][y - 1]);

                //straight upper wall if l r d and under d is floor and no u
                if (((grid[x][y + 1] == (int)TT.Nothing || grid[x][y + 1] == (int)TT.OgWall2) ||
                     grid[x][y + 1] == (int)TT.HiddenWall) && l &&
                    r) res = wallSprites[3];
                //left wall if u d r is floor and no left
                else if (u && d && (grid[x - 1][y] == (int)TT.Nothing ||
                                    grid[x - 1][y] == (int)TT.HiddenWall || IsW(grid[x - 1][y]) && IsF(grid[x - 2][y]))) res = wallSprites[5];
                //right wall if u d l is floor and no right
                else if (u && d && (grid[x + 1][y] == (int)TT.Nothing ||
                                    grid[x + 1][y] == (int)TT.HiddenWall || IsW(grid[x + 1][y]) && IsF(grid[x + 2][y]))) res = wallSprites[6];
                //lower wall if l r u is floor and no down
                else if (IsF(grid[x][y + 1]) && r && l) res = wallSprites[8];
                //SINGLE COLUMN 
                //else if (IsF(grid[x][y + 1]) && IsF(grid[x][y - 1])) res = wallSprites[14];
                //else if (IsF(grid[x + 1][y]) && IsF(grid[x - 1][y])) res = wallSprites[15];
                //OUTWARDS CORNERS //(Big White)
                if (res == null && u && l && IsF(grid[x + 1][y])) res = wallSprites[11];
                if (res == null && u && r && IsF(grid[x - 1][y])) res = wallSprites[10];
                if (res == null && IsF(grid[x][y + 1]) && IsF(grid[x + 1][y]) && d && l) res = wallSprites[13];
                if (res == null && IsF(grid[x][y + 1]) && IsF(grid[x - 1][y]) && d && r) res = wallSprites[12];
                //INWARDS CORNERS //(Small White)
                if (res == null && u && r && !IsF(grid[x][y - 1]) && !IsF(grid[x - 1][y])) res = wallSprites[7];
                if (res == null && r && d && !IsF(grid[x][y + 1]) && !IsF(grid[x - 1][y])) res = wallSprites[2];
                if (res == null && l && d && !IsF(grid[x][y + 1]) && !IsF(grid[x + 1][y])) res = wallSprites[4];
                if (res == null && u && l && !IsF(grid[x][y - 1]) && !IsF(grid[x + 1][y])) res = wallSprites[9];

                if (IsF(grid[x][y + 1]) && IsF(grid[x][y - 1]) && r)
                    res = wallSprites[17];
                else if (IsF(grid[x][y + 1]) && IsF(grid[x][y - 1]) && !r)
                    res = wallSprites[18];
                else if (IsF(grid[x + 1][y]) && IsF(grid[x - 1][y]) && u)
                    res = wallSprites[19];
                else if (IsF(grid[x + 1][y]) && IsF(grid[x - 1][y]) && !u)
                    res = wallSprites[20];
            }

            return res;
        }

        private void AddWallLights()
        {
            foreach (var p in litWalls)
                Instantiate((Object)wallLight, Helper.Iso2(p), Quaternion.identity, lightContainer.transform);
        }


        private void ProcessHiddenWalls()
        {
            foreach (var hW in hiddenWalls)
                if (grid[(int)hW.x][(int)hW.y] == (int)TT.Nothing)
                {
                    var worldPos = GridToWorld((int)hW.x, (int)hW.y);
                    wallTilemap.SetTile(new Vector3Int(worldPos.x, worldPos.y, 0), blankTile);
                }
        }

        private void ProcessDoors()
        {
            foreach (var d in doors)
            {
                var i = (int)d.Key.x;
                var j = (int)d.Key.y;
                if (!d.Value) // left door (horizontal)
                {
                    SetDoorTile(i, j, doorTiles[0], door2Tilemap);
                    SetDoorTile(i + 1, j, doorTiles[1], door2Tilemap);
                    SetDoorTile(i + 2, j, doorTiles[2], door2Tilemap);
                    SetDoorTile(i + 3, j, doorTiles[3], door2Tilemap);

                    SetDoorTile(i, j, doorTiles[8], door1Tilemap);
                    SetDoorTile(i + 1, j, doorTiles[9], door1Tilemap);
                    SetDoorTile(i + 2, j, doorTiles[10], door1Tilemap);
                    SetDoorTile(i + 3, j, doorTiles[11], door1Tilemap);

                    SetDoorTile(i, j, doorTiles[16], doorTilemap);
                    SetDoorTile(i + 1, j, doorTiles[17], doorTilemap);
                    SetDoorTile(i + 2, j, doorTiles[18], doorTilemap);
                    SetDoorTile(i + 3, j, doorTiles[19], doorTilemap);
                }
                else if (d.Value) // right door (vertical)
                {
                    SetDoorTile(i, j, doorTiles[4], door2Tilemap);
                    SetDoorTile(i, j + 1, doorTiles[5], door2Tilemap);
                    SetDoorTile(i, j + 2, doorTiles[6], door2Tilemap);
                    SetDoorTile(i, j + 3, doorTiles[7], door2Tilemap);

                    SetDoorTile(i, j, doorTiles[12], door1Tilemap);
                    SetDoorTile(i, j + 1, doorTiles[13], door1Tilemap);
                    SetDoorTile(i, j + 2, doorTiles[14], door1Tilemap);
                    SetDoorTile(i, j + 3, doorTiles[15], door1Tilemap);

                    SetDoorTile(i, j, doorTiles[20], doorTilemap);
                    SetDoorTile(i, j + 1, doorTiles[21], doorTilemap);
                    SetDoorTile(i, j + 2, doorTiles[22], doorTilemap);
                    SetDoorTile(i, j + 3, doorTiles[23], doorTilemap);
                }

            }
        }


        private void CleanUpHallways()
        {
            for (var i = 1; i < grid.Length - 3; i++)
            for (var j = 3; j < grid[i].Length; j++)
                if (IsW(grid[i][j]) && grid[i - 1][j] == (int)TT.Nothing &&
                    IsW(grid[i + 1][j]) && grid[i + 2][j] == (int)TT.Nothing && IsW(grid[i][j + 1]))
                {
                    grid[i][j] = (int)TT.Nothing;
                    grid[i + 1][j] = (int)TT.Nothing;
                    grid[i][j - 1] = (int)TT.Nothing;
                    grid[i + 1][j - 1] = (int)TT.Nothing;
                    grid[i + 2][j - 1] = (int)TT.Nothing;

                    grid[i + 1][j - 3] = (int)TT.OgWall012L;
                    grid[i][j - 3] = (int)TT.OgWall012L;
                    grid[i + 1][j - 4] = (int)TT.OgWall012L;
                    grid[i][j - 4] = (int)TT.OgWall012L;
                }
            /*for (var i = 1; i < grid.Length - 1; i++)
            for (var j = 6; j < grid[i].Length - 1; j++)
            
                //missing corner pieces
                if (grid[i][j] == (int)TileType.Nothing && IsW(grid[i + 1][j]) &&
                    IsW(grid[i][j - 1]) && grid[i][j + 1] == (int)TileType.Nothing
                    && grid[i - 1][j] == (int)TileType.Nothing &&
                    IsW(grid[i][j - 6])) //to make sure that this corner piece is in a vertical hallway
                    grid[i][j] = (int)TileType.OgWall;*/
        }

        private void AddBackgroundTiles()
        {
            for (var i = grid.Length - 2; i > 0; i--)
            for (var j = grid[i].Length - 2; j > 0; j--)
            {
                if (grid[i][j] == (int)TT.Nothing || (grid[i][j] == (int)TT.AlreadyPlacedRoomTile && (grid[i][j - 1] == (int)TT.HiddenWall ||
                                                      grid[i - 1][j] == (int)TT.HiddenWall)))
                {
                    if (grid[i + 1][j] == (int)TT.OgWall2)
                    {
                        backgroundTiles.Add(new Vector2(i, j));
                    }

                    if (grid[i][j + 1] == (int)TT.OgWall2)
                    {
                        backgroundTiles.Add(new Vector2(i, j));
                    }

                    if (grid[i + 1][j + 1] == (int)TT.OgWall2)
                    {
                        backgroundTiles.Add(new Vector2(i, j));
                    }
                }

                if (grid[i][j] == (int)TT.OgWall012L || grid[i][j] == (int)TT.OgWall012R)
                    if (IsF(grid[i + 1][j]) && IsF(grid[i][j + 1]))
                    {
                        backgroundTiles.Add(new Vector2(i, j));
                    }
            }

            foreach (var v in backgroundTiles)
            {
                if (grid[(int)v.x][(int)v.y] == (int)TT.Nothing)
                {
                    var worldPos = GridToWorld((int)v.x, (int)v.y);
                    wall2Tilemap.SetTile(new Vector3Int(worldPos.x, worldPos.y, 0), wallSprites[16]);
                }
            }
        }

        private void HandleIsometricTransformation()
        {
            roomContainer.transform.rotation = Quaternion.Euler(60f, 0f, 45f);
            roomContainer.transform.localScale = new Vector3(0.7071f, 0.7071f, 0f);
            roomContainer.transform.position = new Vector3(0, 7.75f, 0);
            chestContainer.transform.localScale = new Vector3(0.7071f, 0.7071f, 0f);
            chestContainer.transform.position = new Vector3(0, 7.75f, 0f);
            lightContainer.transform.localScale = new Vector3(0.7071f, 0.7071f, 0f);
            lightContainer.transform.position = new Vector3(0, 8.50f, 0f);
        }

        private void SetDoorTile(int x, int y, TileBase tile, Tilemap tilemap)
        {
            var tmp = GridToWorld(x, y);
            tilemap.SetTile(new Vector3Int(tmp.x, tmp.y, 0), tile);
        }

        private void SetTile(int x, int y, TileBase tile)
        {
            var tmp = GridToWorld(x, y);
            var position = new Vector3Int(tmp.x, tmp.y, 0);

            if (grid[x][y] == (int)TT.OgWall012R)
            {
                wallTilemap.SetTile(position, wallSprites[1]);

                wall1Tilemap.SetTile(new Vector3Int(position.x, position.y, position.x), wallSprites[0]);

                var wallSprite = GetWallSprite(x, y);
                wall2Tilemap.SetTile(new Vector3Int(position.x, position.y, position.x), wallSprite);
            }
            else if (grid[x][y] == (int)TT.OgWall012L)
            {
                wallTilemap.SetTile(position, wallSprites[1]);

                wall1Tilemap.SetTile(new Vector3Int(position.x, position.y, position.x), wallSprites[0]);

                var wallSprite = GetWallSprite(x, y);
                wall2Tilemap.SetTile(new Vector3Int(position.x, position.y, position.x), wallSprite);
            }
            else if (grid[x][y] == (int)TT.OgWall2)
            {
                var wallSprite = GetWallSprite(x, y);
                wall2Tilemap.SetTile(new Vector3Int(position.x, position.y, position.z), wallSprite);
            }

            else if (grid[x][y] == (int)TT.AlreadyPlacedRoomTile)
            {
                if (tile.name !=
                    debugTile.name) //make sure that this method is being called to paint for the first time
                    floorTilemap.SetTile(position, tile);
            }
            else if (grid[x][y] == (int)TT.Hallway)
            {
                floorTilemap.SetTile(position, floorTiles[0]);
            }

            else if (grid[x][y] == (int)TT.HiddenWall)
            {
                wall2Tilemap.SetTile(position, wallSprites[16]);
            }

            else if (grid[x][y] == (int)TT.FakeFloor)
            {
                floorTilemap.SetTile(position, null);
            }
            else if (grid[x][y] == (int)TT.Background)
            {
                wall2Tilemap.SetTile(position, wallSprites[16]);
            }
            else if (grid[x][y] == (int)TT.Debug)
            {
                wall2Tilemap.SetTile(position, debugTile);
            }

            else
            {
                floorTilemap.SetTile(position, tile);
            }
        }

        private static Vector3Int GridToWorld(int x, int y) => new Vector3Int(x + (int)RoomGenerator.xMin, y + (int)RoomGenerator.yMin, 0);
        

        private void InitializeDungeonGeneration()
        {
            CreateGrid();
            AddWalls();
            CleanUpHallways();
            ProcessDoors();
            AddWallLights();
            ProcessHiddenWalls();
            //HandleIsometricTransformation();
            AddBackgroundTiles();

            var width = gridWidth;
            var height = gridHeight;

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                if (grid[x][y] != (int)TT.Nothing &&
                    grid[x][y] != (int)TT.OgDoor &&
                    grid[x][y] != (int)TT.FakeFloor) //not a door because I already set all doors in ProcessDoors
                    SetTile(x, y, debugTile);
        }
    }

    public class RoomTile
    {
        public Vector2 v;
        public TileBase t;

        public RoomTile(Vector2 v, TileBase t)
        {
            this.v = v;
            this.t = t;
        }
    }
    public class HallwayRequest
    {
        public Vector2 v1;
        public Vector2 v2;

        public HallwayRequest(Vector2 v1, Vector2 v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }
    }
}