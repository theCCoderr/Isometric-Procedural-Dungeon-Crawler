using UnityEngine;
using Random = UnityEngine.Random;

namespace Amr
{
    public class Bullet : MonoBehaviour
    {
        public Rigidbody2D rb;
        [SerializeField] private Vector2 angle;
        [SerializeField] private float speed;
        private CircleCollider2D circleCollider2d;
        private SpriteRenderer spriteRenderer;
        private float fadeOutTimer;
        private int damage;
        private bool hasHit; // a necessary logic gate to insure the bullet hit twice

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            circleCollider2d = GetComponent<CircleCollider2D>();
        }

        private void Start()
        {
            GameManager.ONGameOver.AddListener(OnGameOver);
        }

        public void Initialize(Sprite sprite, float fadeOutTime, float spriteScale, int damage)
        {
            fadeOutTimer = fadeOutTime;
            var scale = transform.localScale;
            scale.Set(spriteScale, spriteScale, 1);
            transform.localScale = scale;
            spriteRenderer.sprite = sprite;
            this.damage = damage;
            hasHit = false;
        }

        private void Update()
        {
            fadeOutTimer -= Time.deltaTime;
            if (fadeOutTimer <= 0)
            {
                GameObject o;
                ObjectPool.ReturnPooledObject(
                    (o = gameObject).CompareTag("PlayerBullet") ? "PlayerBullet" : "EnemyBullet", o);
            }
        }
        
        public void StopMoving()
        {
            rb.linearVelocity = Vector2.zero;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacles"))
            {
                
                if (gameObject.CompareTag("PlayerBullet")) GetComponent<ParticleSystem>().Play();
                ObjectPool.ReturnPooledObject(gameObject.CompareTag("PlayerBullet") ? "PlayerBullet" : "EnemyBullet",
                    gameObject);
            }


            else if (collision.gameObject.layer == LayerMask.NameToLayer("Enemies")  && !hasHit) // hit a enemy
            {
                if (gameObject.CompareTag("PlayerBullet"))
                {
                    hasHit = true;
                    GetComponent<ParticleSystem>().Play();
                    ObjectPool.ReturnPooledObject("PlayerBullet", gameObject);
                    var enemy = collision.gameObject.GetComponent<Enemy>();
                    enemy.EnemyHit(damage);
                    Debug.Log("halo");
                }
            }

            else if (collision.gameObject.layer == LayerMask.NameToLayer("Player")  && !hasHit) // hit the player
            {
                if (gameObject.CompareTag("EnemyBullet"))
                {
                    hasHit = true;
                    ObjectPool.ReturnPooledObject("EnemyBullet", gameObject);
                    PlayerHealth.ChangeHealth(false, damage);
                }
            }
            else if (collision.gameObject.layer == LayerMask.NameToLayer("Chest")  && !hasHit)
            {
                if (gameObject.CompareTag("PlayerBullet"))
                {
                    hasHit = true;
                    ObjectPool.ReturnPooledObject("PlayerBullet", gameObject);
                    var chest = collision.gameObject.GetComponent<Chest>();
                    chest.ChestHit(damage);
                }
            }
            else if (collision.gameObject.layer == LayerMask.NameToLayer("FirstBoss") && !hasHit)
            {
                if (gameObject.CompareTag("PlayerBullet"))
                {
                    hasHit = true;
                    ObjectPool.ReturnPooledObject("PlayerBullet", gameObject);
                    var boss = collision.gameObject.GetComponent<FirstBoss>();
                    boss.BossHit(damage);
                }
            }

        }

        private void OnGameOver()
        {
            GameObject o;
            ObjectPool.ReturnPooledObject(
                (o = gameObject).CompareTag("PlayerBullet") ? "player bullet" : "enemy bullet", o);
        }
    }
}