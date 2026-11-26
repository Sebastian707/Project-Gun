using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float jumpPower = 7f;
    public float gravity = 14f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private CharacterController characterController;

    private bool canMove = true;

    [Header("Head Bobbing")]
    public float bobSpeed = 12f;      // How fast the head bobs (higher = faster steps)
    public float bobAmount = 0.05f;    // How dramatic the bob is (keep it small for PS1!)
    private float defaultY = 0;        // Stores the original camera height
    private float timer = 0;
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Save the default camera height
        defaultY = playerCamera.transform.localPosition.y;
    }

    void Update()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);


        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.C) && canMove)
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;

        }
        else
        {
            characterController.height = defaultHeight;
            walkSpeed = 4f;
            runSpeed = 8f;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);

            // ================= HEAD BOBBING START =================
            // Check if player is moving on the ground (ignoring vertical movement)
            if (characterController.isGrounded && (Mathf.Abs(curSpeedX) > 0.1f || Mathf.Abs(curSpeedY) > 0.1f))
            {
                // Increase timer based on movement speed (bobs faster when sprinting)
                float currentBobSpeed = isRunning ? bobSpeed * 1.3f : bobSpeed;
                if (Input.GetKey(KeyCode.C)) currentBobSpeed = bobSpeed * 0.7f; // Slower when crouching

                timer += Time.deltaTime * currentBobSpeed;

                // Calculate new Y position using a Sine wave
                float newY = defaultY + Mathf.Sin(timer) * bobAmount;
                playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, newY, playerCamera.transform.localPosition.z);
            }
            else
            {
                // Smoothly return camera to default height when standing still or airborne
                timer = 0;
                float currentY = playerCamera.transform.localPosition.y;
                float targetY = Mathf.MoveTowards(currentY, defaultY, Time.deltaTime * bobSpeed * 0.5f);
                playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, targetY, playerCamera.transform.localPosition.z);
            }
            // ================== HEAD BOBBING END ==================
        }
    }
}

