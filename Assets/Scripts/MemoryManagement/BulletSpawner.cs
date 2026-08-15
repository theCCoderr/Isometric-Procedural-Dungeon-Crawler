using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Amr
{
    public class BulletSpawner : MonoBehaviour
    {
        public GunSo gunSo;
        private static List<GunSo> guns = new List<GunSo>();
        private float aimingAngle;
        private SpriteRenderer gunSpriteRend;
        private PlayerAim playerAim;
        private int gunIndex;
        private bool canShoot = true;
        private Transform aimOrigin;
        
        [SerializeField] private SpriteRenderer reloadingSr;
        //[SerializeField] private Animator reloadingAnimator;

        [SerializeField] private float holdMinPeriod = 5f;
        [SerializeField] private float holdTimer;
        
        //private bool firstShot;
        private bool bursting;
        
        private static int[] bulletsFired;
        private static float[] ReloadTimer;
        private bool reloading;

        private CanvasManager canvasManager;


        public void Start()
        {
            gunSpriteRend = gameObject.GetComponentInParent<SpriteRenderer>();
            aimOrigin = GetComponentInParent<Transform>();
            playerAim = aimOrigin.GetComponentInParent<PlayerAim>();
            canvasManager = FindObjectOfType<CanvasManager>();
            RefreshObtainedGuns();
            RefreshGun(gunSo);
            GameManager.ONGameOver.AddListener(UnObtainWeapons);
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (gunIndex < guns.Count - 1)
                {
                    gunIndex++;
                    RefreshGun(guns[gunIndex]);
                }
                else
                {
                    gunIndex = 0;
                    RefreshGun(guns[0]);
                }
            }

            if (ReloadTimer[gunIndex] > 0) ReloadTimer[gunIndex] -= Time.fixedDeltaTime;
            if (ReloadTimer[gunIndex] <= 0 && reloading)
            {
                bulletsFired[gunIndex] = 0;
                reloading = false;
                reloadingSr.enabled = false;
                canvasManager.Shot(gunSo, bulletsFired[gunIndex]); //fake shot to update the ui once reloaded
            }

            if (Input.GetMouseButton(0) && canShoot)
            {
                if (!gunSo.reload || bulletsFired[gunIndex] < gunSo.reloadBulletNum)
                {
                    if (gunSo.burst && holdTimer == 0 && !bursting)
                    {
                        StartCoroutine(BurstShoot());
                    }
                    else if (!gunSo.burst)
                    {
                        StartCoroutine(Shoot(holdTimer > holdMinPeriod));
                    }

                    //firstShot = false;
                }
                else
                {
                    if (ReloadTimer[gunIndex] <= 0)
                    {
                        ReloadTimer[gunIndex] = gunSo.reloadWaitTime;
                        reloading = true;
                        reloadingSr.enabled = true;
                        //reloadingAnimator.StopPlayback();
                        //reloadingAnimator.Play("Reloading meter");
                    }
                }
            }

            if (Input.GetMouseButton(0)) holdTimer += Time.fixedDeltaTime;
            else
            {
                holdTimer = 0;
                //firstShot = true;
            }
        }


        private IEnumerator BurstShoot()
        {
            bursting = true;
            for (var j = 0; j < gunSo.burstNum; j++)
            {
                bulletsFired[gunIndex]++;
                canShoot = false;
                
                /*aimingAngle = aimOrigin.rotation.eulerAngles.z;
                var angleStep = gunSo.bulletSpreadAngle / gunSo.bulletNum;
                var centeringOffset =
                    gunSo.bulletSpreadAngle / 2 -
                    angleStep / 2; //offsets every projectile so the spread is                                                                                                                         //centered on the mouse cursor
                for (var bulletIndex = 0; bulletIndex < gunSo.bulletNum; bulletIndex++)
                {
                    var currentBulletAngle = angleStep * bulletIndex;
                    var bulletRotation = Quaternion.Euler(new Vector3(0, 0, aimingAngle + currentBulletAngle - centeringOffset));
                    var obj = ObjectPool.GetPooledObject("PlayerBullet");
                    obj.transform.position = transform.position;
                    obj.SetActive(true);
                    var bullet = obj.GetComponent<Bullet>();
                    bullet.Initialize(gunSo.bulletSprite, gunSo.fadeOutTime, gunSo.spriteScale, gunSo.damage);
                    obj.transform.rotation = bulletRotation;
                    MCamera.ShakeCamera(gunSo.shakeIntensity, gunSo.randomShakeIntensity);
                    bullet.StartMoving(obj.transform.right, gunSo.bulletSpeed, gunSo.bulletSpreadFactor);
                }*/
                
                
                var newRot = aimOrigin.rotation;

                for (int i = 0; i < gunSo.bulletNum; i++)
                {
                    float addedOffset = i - (gunSo.bulletNum / 2f) * gunSo.bulletSpreadAngle;

                    // Then add "addedOffset" to whatever rotation axis the player must rotate on
                    var lEA = aimOrigin.transform.eulerAngles;
                    newRot = Quaternion.Euler(lEA.x, lEA.y, lEA.z + addedOffset);

                    var obj = ObjectPool.GetPooledObject("PlayerBullet");
                    var position = transform.position;
                    obj.transform.position = position;
                    obj.transform.rotation = newRot;
                    obj.SetActive(true);
                    var bullet = obj.GetComponent<Bullet>();
                    bullet.Initialize(gunSo.bulletSprite, gunSo.fadeOutTime, gunSo.spriteScale, gunSo.damage);
                    var randomSpread = Random.Range(-gunSo.bulletSpreadFactor, gunSo.bulletSpreadFactor);
                    var right = obj.transform.right;
                    bullet.rb.AddForce(new Vector3(right.x, right.y, right.z + randomSpread) * gunSo.bulletSpeed, ForceMode2D.Impulse);
                }


                yield return Helper.GetWait(gunSo.shootingDelayUnHeld);;

                canShoot = true;
            }

            bursting = false;
        }

        private IEnumerator Shoot(bool mouseHeld)
        {
            canShoot = false;
            var rotations = Helper.CalculateRotations(gunSo.bulletNum, gunSo.bulletSpreadAngle, aimOrigin);
            bulletsFired[gunIndex]++; //Shotgun bullets are considered one ammo bullet
            foreach (var rotation in rotations)
            {
                var obj = ObjectPool.GetPooledObject("PlayerBullet");
                obj.transform.position = transform.position;
                obj.SetActive(true);
                var bullet = obj.GetComponent<Bullet>();
                bullet.Initialize(gunSo.bulletSprite, gunSo.fadeOutTime, gunSo.spriteScale, gunSo.damage);
                obj.transform.rotation = rotation;
                var direction =  obj.transform.right;
                direction.x += Random.Range(-gunSo.bulletSpreadFactor, gunSo.bulletSpreadFactor);
                direction.y += Random.Range(-gunSo.bulletSpreadFactor, gunSo.bulletSpreadFactor);
                bullet.rb.linearVelocity = direction * gunSo.bulletSpeed;
                
                MCamera.ShakeCamera(gunSo.shakeIntensity, gunSo.randomShakeIntensity);
            }
            canvasManager.Shot(gunSo, bulletsFired[gunIndex]);

            yield return Helper.GetWait(mouseHeld ? gunSo.shootingDelay : gunSo.shootingDelayUnHeld);
            canShoot = true;
        }

        public static void RefreshObtainedGuns()
        {
            var allGuns = GameManager.gunSOs;

            foreach (var t in allGuns)
                if (t.obtainedByPlayer && !guns.Contains(t))
                    guns.Add(t);

            ReloadTimer = new float[guns.Count];
            bulletsFired = new int[guns.Count];
        }

        private void RefreshGun(GunSo nextGunSo)
        {
            gunSo = nextGunSo;
            gunSpriteRend.sprite = nextGunSo.gunSprite;
            transform.localPosition = nextGunSo.firingPosition;
            playerAim.handlePosition = nextGunSo.handlePosition;
            canvasManager.ChangedGun(nextGunSo, bulletsFired[gunIndex]);
        }

        private void UnObtainWeapons()
        {
            foreach (var v in guns)
            {
                if (v.obtainedByPlayer && !v.startingWeapon)
                {
                    v.obtainedByPlayer = false;
                }
            }
        }
    }
}