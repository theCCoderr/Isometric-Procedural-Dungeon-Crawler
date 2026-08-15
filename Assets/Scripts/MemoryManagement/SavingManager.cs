using System.IO;
using UnityEngine;

namespace Amr
{
    public static class SavingManager
    {
        private static readonly string DefaultPath = Application.dataPath + "/Resources/Saves/";
        private static readonly string SpecialPath = Application.dataPath + "/Resources/SpecialSaves/";

        public static void Init()
        {
            if (!Directory.Exists(DefaultPath))
                Directory.CreateDirectory(DefaultPath);
        }

        public static void SaveRoomData(string name, int[] array, Vector2 size, int difficulty, string fileName,
            bool isSpecial)
        {
            var tilemapData = new RoomData
            {
                name = name,
                size = size,
                tiles = array,
                difficulty = difficulty
            };

            if (!isSpecial)
                File.WriteAllText(DefaultPath + fileName + ".txt", JsonUtility.ToJson(tilemapData));
            else File.WriteAllText(SpecialPath + fileName + ".txt", JsonUtility.ToJson(tilemapData));
        }

        public static RoomData[] LoadAllRoomData()
        {
            var roomData = Resources.LoadAll("Saves");
            var res = new RoomData[roomData.Length];
            for (var i = 0; i < roomData.Length; i++)
                res[i] = JsonUtility.FromJson<RoomData>(File.ReadAllText(DefaultPath + roomData[i].name + ".txt"));

            return res;
        }

        public static RoomData[] LoadSpecialRoomData()
        {
            var roomData = Resources.LoadAll("SpecialSaves");
            var res = new RoomData[roomData.Length];
            for (var i = 0; i < roomData.Length; i++)
                res[i] = JsonUtility.FromJson<RoomData>(File.ReadAllText(SpecialPath + roomData[i].name + ".txt"));

            return res;
        }

        public class RoomData
        {
            public int difficulty;
            public string name;
            public Vector2 size;
            public int[] tiles;
        }
    }
}