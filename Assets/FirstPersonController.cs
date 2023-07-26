using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    //TODO: Fix Camera Tilt
    public bool CanMove { get; private set; } = true;
    public bool IsSprinting => canSprint && Input.GetKey(sprintKey);
    public bool ShouldJump => charCon.isGrounded && Input.GetKeyDown(jumpKey);
    public bool ShouldCrouch => !duringCrouchAnimation && Input.GetKeyDown(crouchKey);
    [Header("Movement Controls")]

    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canHeadBob = true;
    [SerializeField] private bool willSlideOnSlope = true;
    [SerializeField] private bool canCameraTilt;
    // [SerializeField] private bool canJumpOnSliding = true;

    [Header("KeyBind")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Space()]

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float slopeSpeed = 8f;
    [SerializeField] private float gravity = 30f;


    [Space()]

    [Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2f;
    [SerializeField, Range(1, 180)] private float upperLookLimit = 80f;
    [SerializeField, Range(1, 180)] private float lowerLookLimit = 80f;


    [Space()]

    [Header("Jump Parameter")]
    [SerializeField] private float jumpForce = 8f;


    [Space()]

    [Header("Crouch Parameters")]
    [SerializeField] float crouchHeight = 0.5f;
    [SerializeField] float standingHeight = 2f;
    [SerializeField] float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = Vector3.up * 0.5f;
    [SerializeField] private Vector3 standingCenter = Vector3.zero;
    private bool isCrouching;
    private bool duringCrouchAnimation;

    [Space()]
    [Header("HeadBob Parameters")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.1f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = 0.025f;

    [Space()]
    [Header("Camera Tilt")]

    [Range(0, 5)]
    [SerializeField] private float maxTiltRotation = 3;
    [SerializeField] private float tiltRotationSpeed = 10f;
    [SerializeField] private bool invertTilt = false;
    private Quaternion defaultRotation;

    [Space()]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform TP_Player;

    Vector3 slopeDirection;
    //Sliding Parameters;
    private Vector3 hitPointNormal;
    private bool isSliding
    {
        get
        {
            if (charCon.isGrounded && Physics.SphereCast(transform.position, charCon.radius, Vector3.down, out RaycastHit slopeHit, 2f))
            {


                slopeDirection = Vector3.ProjectOnPlane(Vector3.down, slopeHit.normal);

                hitPointNormal = slopeHit.normal;

                return Vector3.Angle(hitPointNormal, Vector3.up) > charCon.slopeLimit;
            }
            else
            {
                return false;
            }
        }
    }
    private float headBobtimer;
    private float defaultPosY = 0f;

    private CharacterController charCon;


    private Vector3 moveDirection;
    private Vector2 inputVector;
    private float xRotation = 0f;



    private void Awake()
    {

        #region Cursor Setting
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        #endregion

        charCon = GetComponent<CharacterController>();
        defaultPosY = playerCamera.transform.localPosition.y;
        defaultRotation = Quaternion.identity;

    }

    private void Update()
    {

        if (CanMove)
        {
            HandleMovementInput();
            HandleCameraInput();

            if (canJump) HandleJump();
            if (canCrouch) HandleCrouch();
            if (canHeadBob) HandleHeadBob();
            if (canCameraTilt) HandleCameraTilt();
            ApplyFinalMovement();
        }
    }

    private void HandleHeadBob()
    {
        if (!charCon.isGrounded) return;
        if (Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            headBobtimer += Time.deltaTime * (isCrouching ? crouchBobSpeed : IsSprinting ? sprintBobSpeed : walkBobSpeed);
            playerCamera.transform.localPosition = new Vector3(
                playerCamera.transform.localPosition.x,
                defaultPosY + Mathf.Sin(headBobtimer) * (isCrouching ? crouchBobAmount : IsSprinting ? sprintBobAmount : walkBobAmount),
                playerCamera.transform.localPosition.z
            );
        }
    }

    float smoothVelocity;
    Quaternion targetRotation;
    private void HandleCameraTilt()
    {
        if (!charCon.isGrounded)
        {
            // Reset the camera tilt if the character is not grounded.
            targetRotation = Quaternion.Euler(
            playerCamera.transform.localEulerAngles.x,
            playerCamera.transform.localEulerAngles.y,
            0f);

            playerCamera.transform.localRotation = Quaternion.RotateTowards(
          playerCamera.transform.localRotation,
          targetRotation,
          Time.deltaTime * tiltRotationSpeed);

            return;
        }

        float xInput = Input.GetAxisRaw("Horizontal");

        targetRotation = Quaternion.Euler(
            playerCamera.transform.localEulerAngles.x,
            playerCamera.transform.localEulerAngles.y,
        xInput * maxTiltRotation * (invertTilt ? -1 : 1));


        // Apply the tilt rotation to the camera smoothly.
        playerCamera.transform.localRotation = Quaternion.RotateTowards(
            playerCamera.transform.localRotation,
            targetRotation,
            Time.deltaTime * tiltRotationSpeed);
    }

    private void HandleMovementInput()
    {

        inputVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        float moveDirectionY = moveDirection.y;
        moveDirection = transform.TransformDirection(Vector3.forward) * inputVector.y + transform.TransformDirection(Vector3.right) * inputVector.x;
        moveDirection = moveDirection.normalized * inputVector.magnitude * (isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed);

        moveDirection.y = moveDirectionY;
    }
    private void HandleCameraInput()
    {
        xRotation -= Input.GetAxis("Mouse Y") * lookSpeedY;
        xRotation = Mathf.Clamp(xRotation, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(
            xRotation,
            playerCamera.transform.localEulerAngles.y,
            playerCamera.transform.localEulerAngles.z);

        transform.rotation *= Quaternion.Euler(0f, Input.GetAxis("Mouse X") * lookSpeedX, 0f);

    }
    private void HandleJump()
    {


        if (ShouldJump)
        {
            if (isCrouching)
            {
                StartCoroutine(CrouchStand());
            }

            moveDirection.y = jumpForce;

        }
    }

    private void HandleCrouch()
    {
        if (!charCon.isGrounded) return;
        if (ShouldCrouch)
        {

            StartCoroutine(CrouchStand());
        }
    }

    private IEnumerator CrouchStand()
    {
        if (isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f))
        {

            yield break;
        }


        duringCrouchAnimation = true;
        float timeElapsed = 0;

        float targetHeight = isCrouching ? standingHeight : crouchHeight;
        float currentHeight = charCon.height;

        Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = charCon.center;

        while (timeElapsed < timeToCrouch)
        {
            charCon.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
            charCon.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        charCon.height = targetHeight;
        charCon.center = targetCenter;

        isCrouching = !isCrouching;
        duringCrouchAnimation = false;
    }

    private void ApplyFinalMovement()
    {
        if (!charCon.isGrounded)
        {

            moveDirection.y -= gravity * Time.deltaTime;
        }
        else
        {
            if (!ShouldJump)
                moveDirection.y = -0.1f;
        }

        if (willSlideOnSlope && isSliding)
        {
            // adding downward Momentum
            moveDirection += slopeDirection * slopeSpeed;
        }
        charCon.Move(moveDirection * Time.deltaTime);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3.down * 2f));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + (slopeDirection * 2f));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position - (hitPointNormal * 2f));
    }
}
