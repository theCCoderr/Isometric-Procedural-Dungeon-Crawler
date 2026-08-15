using Unity.Mathematics;
using UnityEngine;

public class PlayerAim : MonoBehaviour
{
    [SerializeField] public static float angle;
    [SerializeField] private float debugAngle;
    private Transform aimTrans;
    private Vector3 mousePos;
    public Vector3 aimDirection;
    public Vector2 handlePosition;

    private Animator animator;
    private bool isRunning;
    private float x;
    private float y;
    private static Camera mCamera;
    private SpriteRenderer gunSpriteRenderer;
    private Transform playerTrans;
    private static readonly int Speed = Animator.StringToHash("Speed");

    private void OnEnable()
    {
        mCamera = Camera.main;
    }

    private void Start()
    {
        handlePosition = new Vector2();
        playerTrans = GetComponent<Transform>();
        mCamera = Camera.main;
        aimTrans = transform.Find("Aim");
        gunSpriteRenderer = aimTrans.GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        debugAngle = angle;
        mousePos = GetWorldMousePos();
        aimDirection = (mousePos - transform.position).normalized;
        angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        if (angle < 90 && angle > -90)
        {
            aimTrans.localScale = new Vector3(1, 1, aimTrans.localScale.z);
            aimTrans.eulerAngles = new Vector3(0, 0, angle);
        }
        else
        {
            aimTrans.localScale = new Vector3(1, -1, aimTrans.localScale.z);
            aimTrans.eulerAngles = new Vector3(0, 0, angle);
        }

        UpdatePlayerState();
        UpdateGunPosition();
    }
    
    private void UpdateGunPosition()
    {
        var position = playerTrans.position;
        var x = angle < 90 && angle > -90
            ? position.x + aimDirection.x * 0.3f + handlePosition.x
            : position.x + aimDirection.x * 0.3f - handlePosition.x;
        var y = position.y + aimDirection.y * 0.2f + handlePosition.y;
        aimTrans.position = new Vector3(x, y, 0);
    }

    private void PlayAnimation(string direction, bool reversed)
    {
        isRunning = PlayerController.isRunning;
        var clip = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        x = Input.GetAxisRaw("Horizontal") * 40f;
        y = Input.GetAxisRaw("Vertical") * 40f;
        var run = "Player running " + direction;
        var idle = "Player idle " + direction;
        if (clip != run && isRunning)
        {
            animator.Play(run);
            animator.SetFloat(Speed, reversed ? -1 : 1);
        }
        else if (clip != idle && !isRunning)
        {
            animator.Play(idle);
        }
    }

    private void UpdatePlayerState()
    {
        isRunning = PlayerController.isRunning;
        x = Input.GetAxisRaw("Horizontal") * 40f;
        y = Input.GetAxisRaw("Vertical") * 40f;

        /*if (x > 0 && y > 0) // NE
        {
            /*if (angle < 90f && angle > 0f)
            {
                PlayAnimation("NE", false);
            }

            else if (angle < 0f && angle > -90f)
            {
                PlayAnimation("E", true);
            }
            if (angle < -90f && angle > -180f)
            {
                PlayAnimation("SW", false);
            }
            /*if (angle < 180f && angle > 90f)
            {
                PlayAnimation("N", false);
            }
            else
            {

                PlayAnimation("NE", false);
            }
        }
        else if (x < 0 && y > 0) //NW
        {

            /*if (angle < 90f && angle > 0f)
            {
                PlayAnimation("N", false);
            }
            if (angle < 0f && angle > -90f)
            {
                PlayAnimation("SE", true);
            }
            /*else if (angle < -90f && angle > -180f)
            {
                PlayAnimation("W", false);
            }
            else if (angle < 180f && angle > 90f)
            {
                PlayAnimation("NW", false);
            }
            else
            {
                PlayAnimation("NW", false);
            }
        }
        else if (x > 0 && y < 0)//SE
        {
            /*if (angle < 90f && angle > 0f)
            {
                PlayAnimation("E", false);
            }
            else if (angle < 0f && angle > -90f)
            {
                PlayAnimation("SE", false);
            }
            else if (angle < -90f && angle > -180f)
            {
                PlayAnimation("S", false);
            }
            if (angle < 180f && angle > 90f)
            {
                PlayAnimation("NW", true);
            }
            else
            {
                PlayAnimation("SE", false);
            }
        }
        else if (x < 0 && y < 0)//SW
        {
            if (angle < 90f && angle > 0f)
            {
                PlayAnimation("NE", true);
            }
            /*else if (angle < 0f && angle > -90f)
            {
                PlayAnimation("S", false);
            }
            else if (angle < -90f && angle > -180f)
            {
                PlayAnimation("SW", false);
            }
            else if (angle < 180f && angle > 90f)
            {
                PlayAnimation("W", false);
            }
            else
            {
                PlayAnimation("SW", false);
            }
        }


        else if (x > 0 && y == 0) //E
        {
            if (angle < 45f && angle > -45f)
            {
                PlayAnimation("E", false);
            }

            else if (angle > -135F && angle < -45f)
            {
                PlayAnimation("SE", false);
            }
            else if (angle > 180f && angle < 135f || angle < 180f && angle > 135f)
            {
                PlayAnimation("W", true);
            }
            else if (angle < 135f && angle > 45f)
            {
                PlayAnimation("NE", false);
            }
        }
        else if (x == 0 && y > 0) //N
        {
            if (angle < 45f && angle > -45f)
            {
                PlayAnimation("NE", false);
            }

            else if (angle > -135F && angle < -45f)
            {
                PlayAnimation("S", true);
            }
            else if (angle > 180f && angle < 135f || angle < 180f && angle > 135f)
            {
                PlayAnimation("NW", false);
            }
            else if (angle < 135f && angle > 45f)
            {
                PlayAnimation("N", false);
            }
        }
        else if (x < 0 && y == 0) //W
        {
            if (angle < 45f && angle > -45f)
            {
                PlayAnimation("E", true);
            }

            else if (angle > -135F && angle < -45f)
            {
                PlayAnimation("SW", false);
            }
            else if (angle > 180f && angle < 135f || angle < 180f && angle > 135f)
            {
                PlayAnimation("W", false);
            }
            else if (angle < 135f && angle > 45f)
            {
                PlayAnimation("NW", false);
            }
        }
        else if (x == 0 && y < 0) //S
        {
            if (angle < 45f && angle > -45f)
            {
                PlayAnimation("SE", false);
            }

            else if (angle > -135F && angle < -45f)
            {
                PlayAnimation("S", false);
            }
            else if (angle > 180f && angle < 135f || angle < 180f && angle > 135f)
            {
                PlayAnimation("SW", false);
            }
            else if (angle < 135f && angle > 45f)
            {
                PlayAnimation("N", true);
            }
        }*/


        //if (x == 0 && y == 0)
            if (angle < 22.5 && angle > -22.5f)
                PlayAnimation("E", false);
            else if (angle < 67.5f && angle > 22.5f)
                PlayAnimation("NE", false);
            else if (angle < 112.5f && angle > 67.5f)
                PlayAnimation("N", false);
            else if (angle < 157.5f && angle > 112.5f)
                PlayAnimation("NW", false);
            else if (angle < -157.5 && angle > -180f || angle < 180 && angle > 157.5f)
                PlayAnimation("W", false);
            else if (angle < -112.5f && angle > -157.5f)
                PlayAnimation("SW", false);
            else if (angle < -67.5 && angle > -112.5f)
                PlayAnimation("S", false);
            else if (angle < -22.5f && angle > -67.5f)
                PlayAnimation("SE", false);
    }

    //hi how are you
    public static Vector3 GetWorldMousePos()
    {
        var vec = new Vector3(0, 0, 0);
        if (mCamera != null) vec = mCamera.ScreenToWorldPoint(Input.mousePosition);

        vec.z = 0;
        return vec;
    }
}