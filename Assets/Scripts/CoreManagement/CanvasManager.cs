using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    [SerializeField] private float gunScaleCoefficient;
    [SerializeField] private TextMeshProUGUI gunAmmo;
    [SerializeField] private Image gunImage;
    [SerializeField] private TextMeshProUGUI middleText;
    [SerializeField] private Image healthBar;
    [SerializeField] private Sprite[] healthBarSprites;

    private const float ChestTextTime = 3f;
    private float chestTimer;

    private void Start()
    {
        GameManager.ONHealthChanged.AddListener(ChangeHealth);
        GameManager.ONGameOver.AddListener(GameOver);
        GameManager.ONChestOpened.AddListener(ChestOpened);
    }

    private void Update()
    {
        if (chestTimer > 0)
        {
            chestTimer -= Time.deltaTime;
            if (chestTimer <= 0) middleText.enabled = false;
        }
    }

    private void ChangeHealth(int health)
    {
        healthBar.sprite = healthBarSprites[Math.Abs(health - 6)]; //just fancy sprite indexing based on current health
    }

    public void Shot(GunSo gunSo, int bulletsFired)
    {
        gunAmmo.text = (gunSo.reloadBulletNum - bulletsFired) + " - " + gunSo.reloadBulletNum;
    }

    public void ChangedGun(GunSo newGun, int bulletsFired)
    {
        gunImage.sprite = newGun.gunSprite;
        var scale = /*(0f / newGun.gunSprite.bounds.extents.x) * */gunScaleCoefficient;
        gunImage.transform.localScale = new Vector3(scale, scale, 10);
        gunAmmo.text = (newGun.reloadBulletNum - bulletsFired) + " - " + newGun.reloadBulletNum;
    }

    private void GameOver()
    {
        middleText.text = "Game Over";
        middleText.enabled = true;
    }

    private void ChestOpened(string weaponName)
    {
        middleText.text = weaponName + " obtained!";
        middleText.enabled = true;
        chestTimer = ChestTextTime;
    }
}