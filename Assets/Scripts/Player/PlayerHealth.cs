using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private const float healthCoolDown = 1f;
    private static float healthCoolDownTimer;

    private const float redFlash = 0.1f;
    private static float redFlashTimer;
    
    private static SpriteRenderer sR;
    private const int MAXPlayerHealth = 6;
    private static int CurrentPlayerHealth;

    private void Start()
    {
        sR = GetComponent<SpriteRenderer>();
        CurrentPlayerHealth = MAXPlayerHealth;
        GameManager.ONHealthChanged.Invoke(MAXPlayerHealth); //for starting with the right num
    }

    private void Update()
    {
        if (healthCoolDownTimer > 0)
        {
            healthCoolDownTimer -= Time.deltaTime;
            if (redFlashTimer > 0)
            {
                redFlashTimer -= Time.deltaTime;
                if (redFlashTimer <= 0 && sR.color == Color.red)
                {
                    sR.color = Color.white;
                    redFlashTimer = redFlash;
                }
                else if (redFlashTimer <= 0 && sR.color == Color.white)
                {
                    sR.color = Color.red;
                    redFlashTimer = redFlash;
                }
            }
            else redFlashTimer = redFlash;
        }
    }

    public static void ChangeHealth(bool increase, int amount)
    {
        if (increase)
            CurrentPlayerHealth += amount;
        else if (healthCoolDownTimer <= 0)
        {
            healthCoolDownTimer = healthCoolDown;
            redFlashTimer = redFlash;
            sR.color = Color.red;
            CurrentPlayerHealth -= amount;
        }

        GameManager.ONHealthChanged.Invoke(CurrentPlayerHealth);
    }
}