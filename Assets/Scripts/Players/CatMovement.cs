using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 3f;

    [Header("Charged Jump")]
    public float minJumpForce = 4f;
    public float maxJumpForce = 12f;
    public float chargeTime = 1f;
    public float jumpDelay = 1f;

    [Header("UI Charge Indicator")]
    public Image chargeIndicator;
    public Sprite[] chargeSprites; // 0..4

    private float chargeTimer = 0f;
    private bool charging = false;
    private bool isGrounded = true;
    private bool isJumping = false;
    private bool jumpPending = false;

    private Rigidbody rb;
    private Animator animator;
    private InputSystem_Actions inputActions;
    private Transform cameraTransform;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        cameraTransform = Camera.main.transform;
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Jump.started += ctx => StartCharging();
        inputActions.Player.Jump.canceled += ctx => ReleaseJump();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        HandleMovementAnimation();
        HandleCharging(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        Move();
    }

    // --------------------------
    // Movement
    // --------------------------
    private void Move()
    {
        if (isJumping || jumpPending) return;

        Vector3 f = cameraTransform.forward;
        Vector3 r = cameraTransform.right;
        f.y = 0; r.y = 0;
        f.Normalize(); r.Normalize();

        Vector3 dir = f * moveInput.y + r * moveInput.x;

        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void HandleMovementAnimation()
    {
        bool walking = moveInput.sqrMagnitude > 0.01f && !isJumping;
        animator.SetBool("isWalking", walking);
    }

    // --------------------------
    // Charging Jump
    // --------------------------
    private void StartCharging()
    {
        if (!isGrounded) return;

        charging = true;
        chargeTimer = 0f;

        animator.SetBool("isChargingJump", true);
        UpdateUI();
    }

    private void ReleaseJump()
    {
        if (!charging) return;

        charging = false;

        // natychmiastowa animacja puszczenia spacji
        animator.SetBool("isChargingJump", false);
        animator.SetTrigger("jumpReleased");

        UpdateUI();

        animator.SetTrigger("doJump");

        jumpPending = true;

        // rozpocznij korutynę fizycznego skoku
        StartCoroutine(JumpAfterDelay(jumpDelay));
    }

    private IEnumerator JumpAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PerformJump();
    }

    private void HandleCharging(float dt)
    {
        if (!charging) return;

        chargeTimer += dt;
        chargeTimer = Mathf.Clamp(chargeTimer, 0f, chargeTime);

        UpdateUI();
    }

    private void PerformJump()
    {
        if (!isGrounded) return;

        isGrounded = false;
        isJumping = true;
        jumpPending = false;

        float t = chargeTimer / chargeTime;
        float jumpForce = Mathf.Lerp(minJumpForce, maxJumpForce, t);

        Vector3 forward = transform.forward.normalized;
        Vector3 jumpVector = (Vector3.up + forward).normalized * jumpForce;

        rb.AddForce(jumpVector, ForceMode.Impulse);

        chargeTimer = 0f;
        UpdateUI();
    }

    // --------------------------
    // Landing
    // --------------------------
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
            isJumping = false;
        }
    }

    // --------------------------
    // UI
    // --------------------------
    private void UpdateUI()
    {
        if (chargeIndicator == null || chargeSprites == null || chargeSprites.Length == 0) return;

        int level = 0;
        if (charging)
        {
            float percent = chargeTimer / chargeTime;
            level = Mathf.FloorToInt(percent * (chargeSprites.Length - 1));
            level = Mathf.Clamp(level, 0, chargeSprites.Length - 1);
        }

        chargeIndicator.sprite = chargeSprites[level];
    }
}
