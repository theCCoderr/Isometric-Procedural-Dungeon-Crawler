using UnityEngine;

[CreateAssetMenu(menuName = "gun")]
public class GunSo : ScriptableObject
{
    [Header("Gun Parameters")]
    public Sprite gunSprite;
    public bool obtainedByPlayer;
    public bool startingWeapon;
    public int damage;
    public float bulletSpeed;
    public float shootingDelay;
    public float shootingDelayUnHeld;
    public int bulletNum;
    public bool reload;
    public int reloadBulletNum;
    public float reloadWaitTime;
    public bool burst;
    public int burstNum;
    public float bulletSpreadFactor;
    public float bulletSpreadAngle;
    public float shakeIntensity;
    public float randomShakeIntensity;
    public Vector2 handlePosition;
    public Vector2 firingPosition;
    
    [Header("Bullet Parameters")]
    public Sprite bulletSprite;
    public float spriteScale;
    public float fadeOutTime;
}