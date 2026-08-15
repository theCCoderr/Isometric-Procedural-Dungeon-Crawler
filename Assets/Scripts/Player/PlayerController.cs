using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float movementSpeed;
    [Range(0, .3f)] [SerializeField] private float movementSmoothing = .05f; // How much to smooth out the movement
    [SerializeField] private LineRenderer lr;
    [SerializeField] private float grappleSpeed;
    [SerializeField] private float cuttingTime;
    [SerializeField] private int grapplingDamage;
    [SerializeField] private GameObject grappleHook;
    
    
    public static bool isRunning;
    public static float xMovement;
    public static float yMovement;
        
    private float cashedAngle;
    private float cuttingTimer;
    private Vector3 grappleAimAtPoint;
    private Vector3 grappleCurrentPos;
    private Vector3 grappleEndPoint;
    private RaycastHit2D hit2D;
    private GameObject instantiatedGrappleHook;
    private bool isExtending;
    private bool isGrappling;
    private bool mouseDown;
    private Vector3 mVelocity = Vector3.zero;
    private PlayerAim playerAim;
    private Rigidbody2D rb;
    private Vector3 startPosition;
    private float xEveryFrame;
    private float yEveryFrame;

    private void Awake()
    {
        playerAim = GetComponent<PlayerAim>();
        rb = GetComponent<Rigidbody2D>();
        RoomGenerator.OnRoomsGenerated += RoomGenerator_OnRoomsGenerate;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void FixedUpdate()
    {
        xMovement = Input.GetAxisRaw("Horizontal");
        yMovement = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.Mouse1)) mouseDown = true;

        Move(xMovement * Time.deltaTime, yMovement * Time.deltaTime, mouseDown);
        mouseDown = false;
        xMovement = 0;
        yMovement = 0;
    }

    private void RoomGenerator_OnRoomsGenerate()
    {
        gameObject.SetActive(true);
    }


    private void Move(float xMove, float yMove, bool mousePressed)
    {
        if (!mousePressed && !isGrappling && !isExtending)
        {
            isRunning = xMove != 0 || yMove != 0;
            // Move the character by finding the target velocity
            var input = Vector2.ClampMagnitude(new Vector2(xMove, yMove), 1);
            var targetVelocity = new Vector3(input.x * movementSpeed, input.y * movementSpeed, 0f);
            if (input.x != 0) targetVelocity.y /= 2;
            // And then smoothing it out and applying it to the character
            rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref mVelocity, movementSmoothing);
        }

        else if (mousePressed && !isExtending && !isGrappling)
        {
            cuttingTimer = cuttingTime;
            isRunning = false;
            lr.enabled = true;
            isExtending = true;
            cashedAngle = PlayerAim.angle;

            startPosition = transform.position;
            grappleAimAtPoint = playerAim.aimDirection;
            grappleCurrentPos = startPosition;
            rb.linearVelocity = Vector2.zero;
            mVelocity = Vector2.zero;

            hit2D = Physics2D.Raycast(startPosition, grappleAimAtPoint,
                500f, 1 << LayerMask.NameToLayer("Obstacles"));

            grappleEndPoint = hit2D.point;
            xEveryFrame = startPosition.x <= grappleEndPoint.x
                ? Mathf.Abs((grappleEndPoint.x - startPosition.x) * grappleSpeed)
                : -Mathf.Abs((grappleEndPoint.x - startPosition.x) * grappleSpeed);

            yEveryFrame = startPosition.y <= grappleEndPoint.y
                ? Mathf.Abs((grappleEndPoint.y - startPosition.y) * grappleSpeed)
                : -Mathf.Abs((grappleEndPoint.y - startPosition.y) * grappleSpeed);

            lr.positionCount = 2;
            Extend();
        }
        else if (isExtending)
        {
            Extend();
            var xDifference = Mathf.Abs(grappleCurrentPos.x - grappleEndPoint.x);
            var yDifference = Mathf.Abs(grappleCurrentPos.y - grappleEndPoint.y);
            if (xDifference < 0.1 || yDifference < 0.1)
            {
                isExtending = false;
                isGrappling = true;
                lr.enabled = true;
                rb.linearVelocity = Vector2.zero;
                mVelocity = Vector2.zero;
                var q = Quaternion.Euler(new Vector3(0, 0, cashedAngle));
                instantiatedGrappleHook = Instantiate(grappleHook, grappleEndPoint, q);
            }
        }
        else if (isGrappling && !isExtending)
        {
            if (cuttingTimer >= 0.1) cuttingTimer -= Time.deltaTime;
            var targetVelocity = grappleAimAtPoint * 20;
            rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity, targetVelocity, ref mVelocity, movementSmoothing);
            lr.SetPositions(new[] { transform.position, grappleCurrentPos });

            var xDifference = Mathf.Abs(transform.position.x - grappleEndPoint.x);
            var yDifference = Mathf.Abs(transform.position.y - grappleEndPoint.y);
            if (xDifference < 1 && yDifference < 1 || cuttingTimer <= 0.1)
            {
                isGrappling = false;
                lr.enabled = false;
                Destroy(instantiatedGrappleHook);
            }
        }
    }

    public void OnCollisionEnter2D(Collision2D collider)
    {
        if (collider.gameObject.layer == LayerMask.NameToLayer("Enemies"))
        {
            if (isGrappling && !isExtending)
            {
                collider.gameObject.GetComponent<Enemy>().EnemyHit(grapplingDamage);
                var targetVelocity = grappleAimAtPoint * 20;
                rb.linearVelocity =  -targetVelocity;
                isGrappling = false;
                lr.enabled = false;
                Destroy(instantiatedGrappleHook);
            }
        }
    }

    private void Extend()
    {
        grappleCurrentPos += new Vector3(xEveryFrame, yEveryFrame);
        lr.SetPositions(new[] { transform.position, grappleCurrentPos });
    }
}