using System.Collections.Generic;
using Amr;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace editor
{
    public class RoomSoTilemapBinder : EditorWindow
    {
        [SerializeField] private string roomName;
        [SerializeField] private GameObject prefab;
        [SerializeField] private int difficulty;
        [SerializeField] private bool isSpecial;

        public void OnGUI()
        {
            if (GUILayout.Button("Bind RoomSo and room Tilemap")) Bind();

            roomName = EditorGUILayout.TextField("Name", roomName);
            prefab = (GameObject)EditorGUILayout.ObjectField("Tilemap", prefab, typeof(GameObject), false);
            difficulty = EditorGUILayout.IntField("Difficulty", difficulty);
            isSpecial = EditorGUILayout.Toggle("Is special", isSpecial);
        }

        [MenuItem("Mon Tools/BindRoomSoAndTilemap")]
        public static void ShowWindow()
        {
            GetWindow(typeof(RoomSoTilemapBinder));
        }

        private void Bind()
        {
            var dG = FindObjectOfType<DungeonGenerator>();
            var tilemap = prefab.GetComponentInChildren<Tilemap>();
            tilemap.ResizeBounds();
            var oneD = GetTiles(tilemap);
            var res = new int[oneD.Length];
            var size = new Vector2Int(tilemap.size.x, tilemap.size.y);
            for (var i = 0; i < oneD.Length; i++)
                if (oneD[i] != null)
                {
                    if (oneD[i].name == "DebugLitWall")
                    {
                        res[i] = (int)TT.LitWall;
                        continue;
                    }

                    // the plus 7 is for ensuring that if we wanted to have a tile in the tilemap that's a wall of lit wall of nothing
                    // we won't get a floor tile represented in that index
                    int j;
                    for (j = 0; j < dG.floorTiles.Count; j++)
                        if (dG.floorTiles[j].name == oneD[i].name)
                            break;

                    res[i] = j + 7;
                }
                else
                {
                    res[i] = 0;
                }

            SavingManager.SaveRoomData(roomName, res, size, difficulty, roomName, isSpecial);
            Debug.Log("the tilemap (" + tilemap.name + ") and" + " roomSo (" + roomName + ") have been binded");
        }


        private static TileBase[] GetTiles(Tilemap tilemap)
        {
            var tiles = new List<TileBase>();

            for (var y = tilemap.origin.y; y < tilemap.origin.y + tilemap.size.y; y++)
            for (var x = tilemap.origin.x; x < tilemap.origin.x + tilemap.size.x; x++)
            {
                var tile = tilemap.GetTile(new Vector3Int(x, y, 0));
                tiles.Add(tile);
            }

            return tiles.ToArray();
        }
    }
}