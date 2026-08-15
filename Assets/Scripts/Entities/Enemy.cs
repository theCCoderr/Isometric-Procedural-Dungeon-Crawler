using System;
using System.Collections;
using System.Collections.Generic;
using Amr;
using Pathfinding;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using Random = UnityEngine.Random;

public enum EnemyState
{
    circling = 0,
    lightMovementTo = 1,
    lightMovementAway = 2,
    fastMovementTo = 3,
}


public class Enemy : MonoBehaviour
{
    public EnemySo enemySo;
    public int health;
    public Room room;
    public float angle;
    public Vector3 des;
    public SpriteRenderer gunSpriteRenderer;
    public Transform gunTrans;
    [SerializeField] private Sprite spawnIndicator;
    [SerializeField] private Transform aimTrans;
    [SerializeField] private Transform bulletSpawner;
    [SerializeField] private EnemyState enemyState;

    private float indicatorTimer = 1.5f;

    private int bulletsFired;
    private float reloadTimer;

    private AIPath aiPath;
    private Transform playerTrans;
    private float randCourageous; // A random modifier that affects how the enemy reacts to the player's position
    private float circlingAngle;
    [SerializeField] private float circlingDirection = 1;
    [SerializeField] private float stateTimer;
    private List<Vector2> lastTwoFramesPos;
    private float stuckThreshold = 0.1f;

    private bool hasDied; // A logic gate to insure that the enemy dies once
    private float randomNum;
    private SpriteRenderer spriteRenderer;
    private Sprite[] sprites;
    private float startTimer;
    private bool canShoot = true;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        GameManager.ONGameOver.AddListener(OnGameOver);
        var rand = new System.Random();
        randomNum = rand.Next(-1, 2);
        randCourageous = rand.Next(-1, 2);
        lastTwoFramesPos = new List<Vector2>();
    }

    public virtual void Initialize(EnemySo enemySoPar, Room roomPar)
    {
        room = roomPar;
        enemySo = enemySoPar;
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
                startTimer = Random.Range(0.5f, 2f);
                canShoot = true;
                GetComponent<AIDestinationSetter>().target = playerTrans;
                aiPath.maxSpeed = enemySo.maxSpeed;
                health = enemySo.health;
                sprites = enemySo.enemySprites;
                gunSpriteRenderer.sprite = enemySo.gunSo.gunSprite;
                GetComponent<CapsuleCollider2D>().isTrigger = false;
                spriteRenderer.sortingOrder = 1;
            }
        }

        if (indicatorTimer <= 0)
        {
            //ai system to randomly pick between 3 states
            // ** static heavy shooting
            // ** light shooting with movement away/to the player
            // ** movement away/to the player
            // the Ai based on parameters in the enemy's So chooses what state will it be in plus a random modifier will affect the parameters

            lastTwoFramesPos.Add(transform.position);
            var dist = Vector2.Distance(playerTrans.position, transform.position);
            if (dist > enemySo.farEnd) // very far
            {
                enemyState = EnemyState.fastMovementTo;
            }
            else if (dist < enemySo.farEnd + randCourageous && dist > enemySo.nearEnd + randCourageous && 
                     (stateTimer < 0.2f || enemyState != EnemyState.circling && enemyState != EnemyState.lightMovementTo)) // mid
            {
                var random = Random.value;
                if (random >= 0.5f)
                {
                    enemyState = EnemyState.lightMovementTo;
                }
                else if (random < 0.5f)
                {
                    enemyState = EnemyState.circling;
                }
                    
                stateTimer = 3f;
            }

            else if (dist < enemySo.nearEnd + randCourageous)
            {
                enemyState = EnemyState.lightMovementAway;
            }

            if (enemyState == EnemyState.lightMovementTo)
            {
                if (stateTimer >= 0) stateTimer -= Time.deltaTime;
                aiPath.enabled = true;
                aiPath.maxSpeed = enemySo.minSpeed;
            }
            else if (enemyState == EnemyState.circling)
            {
                aiPath.enabled = false;
                if (stateTimer >= 0) stateTimer -= Time.deltaTime;
                transform.rotation = Quaternion.identity;
                transform.RotateAround(playerTrans.transform.position, new Vector3(0, 0, circlingDirection)
                    , (float) Math.Pow(enemySo.maxSpeed, 4) * Time.deltaTime);
            }

            else if (enemyState == EnemyState.lightMovementAway)
            {
                aiPath.enabled = false;
                transform.position = Vector3.MoveTowards(transform.position, playerTrans.position, 
                    -enemySo.minSpeed * Time.deltaTime);


            }
            else if (enemyState == EnemyState.fastMovementTo)
            {
                aiPath.enabled = true;
                aiPath.maxSpeed = enemySo.maxSpeed;
            }

            GunAimingAndShooting();
            if (Vector2.Distance(transform.position, lastTwoFramesPos[0]) <= stuckThreshold)
            {
                circlingDirection *= -1;
            }
        }

        lastTwoFramesPos.Add(transform.position);
        lastTwoFramesPos.RemoveAt(0);
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

        UpdateEnemySprite();
        UpdateGunPosition();
        if (startTimer >= 0) startTimer -= Time.fixedDeltaTime;
        if (startTimer <= 0.1 && canShoot)
        {
            if (!enemySo.gunSo.reload || bulletsFired < enemySo.gunSo.reloadBulletNum)
            {
                StartCoroutine(Shoot());
                bulletsFired++;
            }
            else
            {
                if (reloadTimer <= 0) reloadTimer = enemySo.gunSo.reloadWaitTime + randomNum;
                reloadTimer -= Time.fixedDeltaTime;
                if (reloadTimer <= 0) bulletsFired = 0;
            }
        }

    }

    private IEnumerator Shoot()
    {
        canShoot = false;
        var rotations = Helper.CalculateRotations(enemySo.gunSo.bulletNum, enemySo.gunSo.bulletSpreadAngle, aimTrans);
        foreach (var rotation in rotations)
        {
            var obj = ObjectPool.GetPooledObject("EnemyBullet");
            obj.transform.position = bulletSpawner.position;
            obj.SetActive(true);
            var bullet = obj.GetComponent<Bullet>();
            bullet.Initialize(enemySo.gunSo.bulletSprite, enemySo.gunSo.fadeOutTime, enemySo.gunSo.spriteScale, enemySo.gunSo.damage);
            obj.transform.rotation = rotation;
            
            var direction =  Vector2.right;
            direction.x += Random.Range(-enemySo.gunSo.bulletSpreadFactor, enemySo.gunSo.bulletSpreadFactor);
            direction.y += Random.Range(-enemySo.gunSo.bulletSpreadFactor, enemySo.gunSo.bulletSpreadFactor);
            bullet.rb.linearVelocity = direction * enemySo.gunSo.bulletSpeed;
            
            obj.GetComponent<Rigidbody2D>().linearVelocity = obj.transform.right * enemySo.gunSo.bulletSpeed;
        }

        yield return Helper.GetWait(enemySo.gunSo.shootingDelay);
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

    public void EnemyHit(int damage)
    {
        health -= damage;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        if (health <= 0 && !hasDied) Died();
    }

    protected virtual void Died()
    {
        hasDied = true;
        indicatorTimer = 1f;
        spriteRenderer.sprite = spawnIndicator;
        gunSpriteRenderer.sprite = null;
        aiPath = GetComponent<AIPath>();
        aiPath.canMove = false;
        GetComponent<CapsuleCollider2D>().isTrigger = true;
        ObjectPool.ReturnPooledObject("Enemy", gameObject);
        room.EnemyDied();
    }

    private void OnGameOver()
    {
        ObjectPool.ReturnPooledObject("Enemy", gameObject);
    }
}