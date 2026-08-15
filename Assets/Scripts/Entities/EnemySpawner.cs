using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Amr
{
    public static class EnemySpawner
    {
        private static GraphNode[][] twoDNodes;

        public static void Make2DArray(GridGraph grid)
        {
            twoDNodes = new GraphNode[grid.width][];
            for (var i = 0; i < twoDNodes.Length; i++)
            {
                twoDNodes[i] = new GraphNode[grid.depth];
            }

            var input = grid.nodes;
            for (var iN = 0; iN < grid.width; iN++)
            {
                for (var j = 0; j < grid.depth; j++)
                {
                    twoDNodes[iN][j] = input[iN * grid.depth + j];
                }
            }
        }

        public static void SpawnEnemies(List<EnemySo> enemySOs, Room room, GridGraph grid)
        {
            foreach (var t in enemySOs)
            {
                var obj = ObjectPool.GetPooledObject("Enemy");
                var bounds = room.boxCollider2D.bounds;
                for (var r = 0; r < 1000000; r++)
                {
                    var randomNodeX = Random.Range((int)bounds.center.x - bounds.extents.x + 3, bounds.center.x + bounds.extents.x - 3);
                    var randomNodeY = Random.Range((int)bounds.center.y - bounds.extents.y + 3, bounds.center.y + bounds.extents.y - 3);
                    var point = new Vector3(randomNodeX, randomNodeY);
                    var node = AstarPath.active.GetNearest(point);
                    if (!node.node.Walkable) continue;
                    if ((Vector2)point != room.boxCollider2D.ClosestPoint(point)) continue;
                    obj.transform.position = new Vector3(randomNodeX, randomNodeY);
                    break;
                }

                obj.SetActive(true);
                var enemy = obj.GetComponent<Enemy>();
                enemy.Initialize(t, room);
                obj.GetComponent<AIDestinationSetter>().target = GameObject.FindGameObjectWithTag("Player").transform;
            }
        }
    }
}