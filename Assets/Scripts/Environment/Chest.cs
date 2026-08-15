using UnityEngine;
using Random = UnityEngine.Random;

namespace Amr
{
    public class Chest : MonoBehaviour
    {
        [SerializeField] private int health;
        private bool opened;
        private SpriteRenderer sR;

        private void Awake()
        {
            sR = GetComponent<SpriteRenderer>();
        }

        public void ChestHit(int damage)
        {
            health -= damage;
            if (health <= 0 && !opened) ChestOpened();
        }

        private void ChestOpened()
        {
            opened = true;
            int num;
            while (true)
            {
                num = Random.Range(0, GameManager.gunSOs.Count);
                if (!GameManager.gunSOs[num].obtainedByPlayer)
                {
                    GameManager.gunSOs[num].obtainedByPlayer = true;
                    break;
                }
            }
            BulletSpawner.RefreshObtainedGuns();
            GameManager.ONChestOpened.Invoke(GameManager.gunSOs[num].name);
            gameObject.SetActive(false);
        }
    }
}