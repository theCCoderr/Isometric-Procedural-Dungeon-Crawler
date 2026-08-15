using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Amr;
using Pathfinding;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using Random = UnityEngine.Random;


[Serializable]
public class Vector6
{
    [SerializeField] [Description("1st is the number of bullets.  " + "2nd is the angle spread.    "
            + "3rd is how many times the shot is repeated.  " + "4th is the waiting duration for each shot.   " + 
            "5th is damage per bullet.   6th is bullet speed.")] public float[] v;
    public Sprite bulletSprite;

    public Vector6(float one, float two, float three, float four, float five, float six, Sprite sprite)
    {
        v = new[] {one, two, three, four, five, six};
        bulletSprite = sprite;
    }
}


public enum BossState
{
    still = 0,
    lightMovementTo = 1,
    lightMovementAway = 2,
    shielding = 3
}

public class FirstBoss : MonoBehaviour
{
    
    [Tooltip("1st is the number of bullets. " + "2nd is the angle spread.    " + "3rd is how many times the shot is repeated.  "
            + "4th is the waiting duration for each shot.   " + "5th is damage per bullet.   6th is bullet speed.")]
    [SerializeField] private List<Vector6> stillWaves;
    [SerializeField] private List<Vector6> lightMovementWaves;
    [SerializeField] private float shieldingDuration;

    [SerializeField] private int health;
    [SerializeField] private float maxSpeed;
    [SerializeField] private Vector3 des;
    [SerializeField] private float nearEnd;

    [SerializeField] private SpriteRenderer gunSpriteRenderer;
    [SerializeField] private Transform gunTrans;
    [SerializeField] private Sprite spawnIndicator;
    [SerializeField] private Transform bulletSpawner;
    [SerializeField] private BossState enemyState;

    private float angle;
    private float stateTimer;
    private bool finishedState; // to know if it's time to move to the next state
    private Room room;
    private Transform playerTrans;
    private AIPath aiPath;
    private float indicatorTimer = 2f;
    private float spawningStartTimer;
    private int bulletsFired;
    private float reloadTimer;

    private bool hasDied; // A logic gate to insure that the enemy dies once
    private bool canShoot = true;
    private SpriteRenderer spriteRenderer;
    private Sprite[] sprites;

    private int shotsInAState; //to check if the boss has done all the shots before it switches the state
    private int shotsRepeated; //to check if the shots was repeated correctly and not shot only once

    public void Initialize(Room roomPar)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        room = roomPar;
        spriteRenderer.sprite = spawnIndicator;
        spriteRenderer.sortingOrder = 0;
        aiPath = GetComponent<AIPath>();
        aiPath.canMove = false;
        hasDied = false;
        GetComponent<CapsuleCollider2D>().isTrigger = true;
        playerTrans = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (indicatorTimer > 0)
        {
            indicatorTimer -= Time.deltaTime;
            if (indicatorTimer <= 0)
            {
                aiPath.canMove = true;
                spawningStartTimer = Random.Range(0.5f, 2f);
                canShoot = true;
                GetComponent<AIDestinationSetter>().target = playerTrans;
                aiPath.maxSpeed = maxSpeed;

                //gunSpriteRenderer.sprite = enemySo.gunSo.gunSprite;

                GetComponent<CapsuleCollider2D>().isTrigger = false;
                spriteRenderer.sortingOrder = 1;
                enemyState = BossState.still;
            }
        }

        if (indicatorTimer <= 0)
        {
            //linearly walk through these 3 states
            // ** static heavy shooting
            // ** light shooting with movement from/to the player
            // ** shielding with no shooting

            if (stateTimer > 0) stateTimer -= Time.deltaTime;

            var dist = Vector2.Distance(playerTrans.position, transform.position);

            if (enemyState == BossState.still && finishedState)
            {
                if (dist > nearEnd) // mid
                {
                    enemyState = BossState.lightMovementTo;
                }

                else if (dist < nearEnd)
                {
                    enemyState = BossState.lightMovementAway;
                }

                finishedState = false;
            }
            else if ((enemyState == BossState.lightMovementAway || enemyState == BossState.lightMovementTo) &&
                     !finishedState)
                //for adjustments inside the state
            {
                if (dist > nearEnd) // mid
                {
                    enemyState = BossState.lightMovementTo;
                }

                else if (dist < nearEnd)
                {
                    enemyState = BossState.lightMovementAway;
                }
            }

            else if ((enemyState == BossState.lightMovementAway || enemyState == BossState.lightMovementTo) && finishedState)
            {
                //enemyState = BossState.shielding;
                enemyState = BossState.still;
                stateTimer = shieldingDuration;
                finishedState = false;
            }

            //else if (enemyState == BossState.shielding && stateTimer < 0.2)
            //{
              //  enemyState = BossState.still;
               // finishedState = false;
            //}


            if (enemyState == BossState.still)
            {
                aiPath.enabled = false;
                GunAimingAndShooting();
            }
            else if (enemyState == BossState.lightMovementTo)
            {
                aiPath.enabled = true;
                aiPath.maxSpeed = maxSpeed;
                GunAimingAndShooting();
            }

            else if (enemyState == BossState.lightMovementAway)
            {
                aiPath.enabled = false;
                transform.position = Vector3.MoveTowards(transform.position, playerTrans.position,
                    -maxSpeed * Time.deltaTime);
                GunAimingAndShooting();
            }
            //else if (enemyState == BossState.shielding)
            //{
            //   aiPath.enabled = true;
            //}
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, aiPath.destination);
    }

    private void GunAimingAndShooting()
    {
        des = playerTrans.position - transform.position;
        angle = Mathf.Atan2(des.y, des.x) * Mathf.Rad2Deg;
        if (angle < 90 && angle > -90)
        {
            gunTrans.localScale = new Vector3(1, 1, gunTrans.localScale.z);
            gunTrans.eulerAngles = new Vector3(0, 0, angle);
        }
        else
        {
            gunTrans.localScale = new Vector3(1, -1, gunTrans.localScale.z);
            gunTrans.eulerAngles = new Vector3(0, 0, angle);
        }

        //todo: ye.....
        //UpdateEnemySprite();
        UpdateGunPosition();

        if (spawningStartTimer >= 0) spawningStartTimer -= Time.fixedDeltaTime;
        if (spawningStartTimer <= 0.1)
        {
            //if (enemyState != BossState.shielding)
            //{
                foreach (var n in enemyState == BossState.still ? stillWaves : lightMovementWaves)
                {
                    for (var i = 0; i < n.v[2]; i++)
                    {
                        if (canShoot)
                        {
                            StartCoroutine(Shoot(n));
                            shotsRepeated++;
                        }
                    }

                    if (shotsRepeated == (int) n.v[2])
                    {
                        shotsInAState++;
                        shotsRepeated = 0;
                    }
                }

                if (shotsInAState == (enemyState == BossState.still ? stillWaves.Count : lightMovementWaves.Count))
                {
                    finishedState = true;
                    shotsInAState = 0;
                }
            //}
        }
    }

    private IEnumerator Shoot(Vector6 n)
    {
        canShoot = false;

        /*var newRot = gunTrans.rotation;

        for (var i = 0; i < n.v[0]; i++)
        {
            var addedOffset = i - (n.v[0] / 2f) * n.v[1];

            // Then add "addedOffset" to whatever rotation axis the player must rotate on
            var eA = gunTrans.transform.eulerAngles;
            newRot = Quaternion.Euler(eA.x, eA.y, eA.z + addedOffset);

            var obj = ObjectPool.GetPooledObject("EnemyBullet");
            var position = transform.position;
            obj.transform.position = position;
            obj.transform.rotation = newRot;
            obj.SetActive(true);
            var bullet = obj.GetComponent<Bullet>();
            bullet.Initialize(n.bulletSprite, 20, 1, (int) n.v[4]);
            var right = obj.transform.right;
            bullet.rb.AddForce(new Vector3(right.x, right.y, right.z) * n.v[5], ForceMode2D.Impulse);
        }*/

        var rotations = Helper.CalculateRotations((int) n.v[0], n.v[1], gunTrans);

        foreach (var rotation in rotations)
        {
            var obj = ObjectPool.GetPooledObject("EnemyBullet");
            obj.transform.position = bulletSpawner.position;
            obj.SetActive(true);
            var bullet = obj.GetComponent<Bullet>();
            bullet.Initialize(n.bulletSprite, 20, 1, (int) n.v[4]);
            obj.transform.rotation = rotation;
            bullet.rb.linearVelocity = obj.transform.right * n.v[5];
        }

        yield return Helper.GetWait(n.v[3]);
        canShoot = true;
    }

    

    private void UpdateGunPosition()
    {
        var position = transform.position;
        var aimDirection = des.normalized;
        var x = position.x + aimDirection.x * 0.5f;
        var y = position.y + aimDirection.y * 0.5f;
        gunTrans.position = new Vector3(x, y, 0);
    }

    private void UpdateEnemySprite()
    {

        if (angle < 90f && angle > 0f)
        {
            spriteRenderer.sprite = sprites[0];
            gunSpriteRenderer.sortingLayerName = "Foreground";
        }
        else if (angle < 180f && angle > 90f)
        {
            spriteRenderer.sprite = sprites[1];
            gunSpriteRenderer.sortingLayerName = "Foreground";
        }
        else if (angle < -90f && angle > -180f)
        {
            spriteRenderer.sprite = sprites[2];
            gunSpriteRenderer.sortingLayerName = "Above";
        }
        else if (angle < 0f && angle > -90f)
        {
            spriteRenderer.sprite = sprites[3];
            gunSpriteRenderer.sortingLayerName = "Above";
        }
    }

    public void BossHit(int damage)
    {
        health -= damage;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        if (health <= 0 && !hasDied) Died();
    }

    private void Died()
    {
        hasDied = true;
        room.BossDied();
        Destroy(gameObject);
    }
}