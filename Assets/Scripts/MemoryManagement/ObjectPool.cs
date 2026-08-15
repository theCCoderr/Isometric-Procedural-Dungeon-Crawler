using System;
using System.Collections.Generic;
using UnityEngine;

namespace Amr
{


    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] public int playerBulletsPoolSize = 1000;
        [SerializeField] public int enemyBulletsPoolSize = 2000;
        [SerializeField] public int enemyPoolSize = 50;

        private static Dictionary<string, List<GameObject>> Pools;
        private static GameObject PlayerBulletPrefab;
        private static GameObject EnemyBulletPrefab;
        private static GameObject EnemyPrefab;


        private void Awake()
        {
            Pools = new Dictionary<string, List<GameObject>>
            {
                { "PlayerBullet", new List<GameObject>(playerBulletsPoolSize) },
                { "Enemy", new List<GameObject>(enemyPoolSize) },
                { "EnemyBullet", new List<GameObject>(enemyBulletsPoolSize) }
            };

            PlayerBulletPrefab = (GameObject)Resources.Load("GameObjects/PlayerBullet");
            for (var i = 0; i < playerBulletsPoolSize; i++) Pools["PlayerBullet"].Add(GetNewObject("PlayerBullet"));

            EnemyBulletPrefab = (GameObject)Resources.Load("GameObjects/EnemyBullet");
            for (var i = 0; i < enemyBulletsPoolSize; i++) Pools["EnemyBullet"].Add(GetNewObject("EnemyBullet"));

            EnemyPrefab = (GameObject)Resources.Load("GameObjects/Enemy");
            for (var i = 0; i < enemyPoolSize; i++) Pools["Enemy"].Add(GetNewObject("Enemy"));
        }

        private static GameObject GetNewObject(string name)
        {
            var obj = name switch
            {
                "PlayerBullet" => Instantiate(PlayerBulletPrefab),
                "EnemyBullet" => Instantiate(EnemyBulletPrefab),
                "Enemy" => Instantiate(EnemyPrefab),
                _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
            };

            obj.SetActive(false);
            DontDestroyOnLoad(obj);
            return obj;
        }

        public static GameObject GetPooledObject(string name)
        {
            var pool = Pools[name];
            if (pool.Count > 0) GetNewObject(name);
            var g = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
            return g;
        }

        public static void ReturnPooledObject(string name, GameObject obj)
        {
            if (name == "PlayerBullet")
            {
                obj.SetActive(false);
                obj.GetComponent<Bullet>().StopMoving();
                Pools["PlayerBullet"].Add(obj);
            }
            else if (name == "EnemyBullet")
            {
                obj.SetActive(false);
                obj.GetComponent<Bullet>().StopMoving();
                Pools["EnemyBullet"].Add(obj);
            }
            else
            {
                obj.SetActive(false);
                Pools["Enemy"].Add(obj);
            }
        }
    }
}