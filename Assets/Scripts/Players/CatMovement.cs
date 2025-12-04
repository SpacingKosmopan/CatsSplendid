using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CatMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Vector2 moveInput;

    private Rigidbody rb;
    private Animator animator;
    private InputSystem_Actions inputActions;
    public Transform cameraTransform;


// ---- SKOK ----
public float[] jumpForces = new float[4];   
public float forwardJumpMultiplier = 2f;     // ile dodatkowej siły do przodu
public float jumpChargeTime = 0.3f;

private float chargeTimer = 0f;
private int jumpChargeLevel = 0;
private bool chargingJump = false;
public bool isGrounded = true;

public float jumpDelay = 1f;        // czas ładowania animacji przed skokiem
private float jumpTimer = 0f;
private bool jumpQueued = false;    // true, gdy chcemy skoczyć po animacji


    // ---- UI ----
    public Image chargeIndicator;
    public Sprite[] chargeSprites;              // 0–4

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Jump.started += ctx => StartCharging();
        inputActions.Player.Jump.canceled += ctx => ReleaseJump();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void FixedUpdate()
    {
        MoveCharacter();
        UpdateRotation();
        UpdateCharge();
    }

    // -------------------------
    //       RUCH
    // -------------------------
private void MoveCharacter()
{
    Vector3 camForward = cameraTransform.forward;
    Vector3 camRight = cameraTransform.right;

    // Zabezpieczenie: tylko płaszczyzna XZ
    camForward.y = 0;
    camRight.y = 0;
    camForward.Normalize();
    camRight.Normalize();

    // Ruch względem kamery
    Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;

    Vector3 move = moveDir * moveSpeed * Time.fixedDeltaTime;
    rb.MovePosition(rb.position + move);

    // Obrót — tylko jeśli coś wciskasz
    if (moveDir != Vector3.zero)
    {
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(moveDir),
            0.15f
        );
    }

    animator.SetBool("isWalking", moveDir != Vector3.zero);
}

private void UpdateRotation()
{
    if (moveInput == Vector2.zero) return;

    Vector3 camForward = cameraTransform.forward;
    Vector3 camRight = cameraTransform.right;

    camForward.y = 0;
    camRight.y = 0;
    camForward.Normalize();
    camRight.Normalize();

    Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;

    transform.rotation = Quaternion.Slerp(
        transform.rotation,
        Quaternion.LookRotation(moveDir),
        5f * Time.deltaTime // teraz obrót wolniejszy i płynny
    );
}


    // -------------------------
//       ŁADOWANIE SKOKU
// -------------------------

private void StartCharging()
{
    if (!isGrounded) return;

    chargingJump = true;
    jumpChargeLevel = 0;
    chargeTimer = 0f;
    jumpTimer = 0f;

    // Uruchamiamy animację skoku
    animator.SetBool("isJumping", true);
}

private void UpdateCharge()
{
    if (!chargingJump || !isGrounded) return;

    // Odliczamy czas skoku (1 sekunda = czas animacji)
    jumpTimer += Time.deltaTime;

    // Zwiększamy poziom siły co jumpChargeTime (opcjonalne)
    chargeTimer += Time.deltaTime;
    if (chargeTimer >= jumpChargeTime && jumpChargeLevel < 4)
    {
        chargeTimer = 0f;
        jumpChargeLevel++;
        UpdateUI();
    }

    // Po 1 sekundzie animacja kończy się → wykonaj fizyczny skok
    if (jumpTimer >= jumpDelay)
    {
        PerformJump();
    }
}


private void PerformJump()
{
    if (jumpChargeLevel == 0) jumpChargeLevel = 1; // minimalny poziom

    float upForce = jumpVerticalForces[jumpChargeLevel - 1];
    float forwardForce = jumpForwardForces[jumpChargeLevel - 1];

    rb.AddForce(Vector3.up * upForce, ForceMode.Impulse);
    rb.AddForce(transform.forward * forwardForce, ForceMode.Impulse);

    chargingJump = false;
    jumpChargeLevel = 0;
    jumpTimer = 0f;
}

public float[] jumpVerticalForces = new float[4];
public float[] jumpForwardForces = new float[4];

private void ReleaseJump()
{
    if (!chargingJump) return;

    chargingJump = false;
    jumpChargeLevel = 0;
    jumpQueued = false;
    jumpTimer = 0f;

    UpdateUI();

    // anuluj animację ładowania
    animator.SetBool("isJumping", false);
}

private void OnCollisionEnter(Collision collision)
{
    if (collision.gameObject.CompareTag("Ground"))
    {
        isGrounded = true;
        animator.SetBool("isJumping", false);
    }
}



    // -------------------------
    //       UI
    // -------------------------

    private void UpdateUI()
    {
        if (chargeIndicator != null && chargeSprites.Length >= 5)
        {
            chargeIndicator.sprite = chargeSprites[jumpChargeLevel];
        }
    }
}