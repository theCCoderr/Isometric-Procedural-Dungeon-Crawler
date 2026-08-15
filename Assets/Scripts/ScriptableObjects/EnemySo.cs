using UnityEngine;

[CreateAssetMenu(menuName = "enemy")]
public class EnemySo : ScriptableObject
{
    public string enemyName;
    public Sprite[] enemySprites;
    public int health;
    public float maxSpeed;
    public float minSpeed;
    public float farEnd;
    public float nearEnd;
    public bool hasGun;
    public Vector2 colliderSize;
    public Vector2 colliderOffset;
    public GunSo gunSo;
}