using UnityEngine;
using UnityEngine.InputSystem;

namespace Tema
{
    [RequireComponent(typeof(CharacterController))]
    public class TemaPlayerController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private TemaFollowCamera followCamera;
        [SerializeField] private TemaTerrain terrain;
        [SerializeField] private RuntimeAnimatorController movementController;

        [SerializeField] private float walkSpeed = 2.5f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float rotationLerp = 14f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpHeight = 1.5f;

        [SerializeField] private string vertParam = "Vert";
        [SerializeField] private string horParam = "Hor";
        [SerializeField] private string stateParam = "State";
        [SerializeField] private string jumpParam = "IsJump";
        [SerializeField] private float animDamp = 0.12f;

        [SerializeField] private float digRadius = 2.5f;
        [SerializeField] private float digStrength = 6f;
        [SerializeField] private float digForwardOffset = 1.2f;

        private CharacterController _cc;
        private float _verticalVelocity;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();

            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null && movementController != null && animator.runtimeAnimatorController == null)
                animator.runtimeAnimatorController = movementController;
            if (animator != null) animator.applyRootMotion = false;

            if (terrain == null) terrain = FindFirstObjectByType<TemaTerrain>();
            if (followCamera == null) followCamera = FindFirstObjectByType<TemaFollowCamera>();
        }

        private void Update()
        {
            Vector2 input = ReadMoveInput();
            bool running = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

            Vector3 forward = followCamera != null ? followCamera.PlanarForward() : Vector3.forward;
            Vector3 right = followCamera != null ? followCamera.PlanarRight() : Vector3.right;

            Vector3 moveDir = forward * input.y + right * input.x;
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
            float moveAmount = moveDir.magnitude;

            if (moveAmount > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                    1f - Mathf.Exp(-rotationLerp * Time.deltaTime));
            }

            bool grounded = _cc.isGrounded;
            if (grounded && _verticalVelocity < 0f) _verticalVelocity = -2f;

            bool jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            if (grounded && jumpPressed)
                _verticalVelocity = Mathf.Sqrt(-2f * gravity * Mathf.Max(0f, jumpHeight));

            _verticalVelocity += gravity * Time.deltaTime;

            float speed = running ? runSpeed : walkSpeed;
            Vector3 displacement = moveDir * speed + Vector3.up * _verticalVelocity;
            _cc.Move(displacement * Time.deltaTime);

            UpdateAnimator(moveAmount, running, _cc.isGrounded);
            HandleDigging();
        }

        private Vector2 ReadMoveInput()
        {
            Keyboard k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }

        private void UpdateAnimator(float moveAmount, bool running, bool grounded)
        {
            if (animator == null) return;
            animator.SetFloat(vertParam, moveAmount, animDamp, Time.deltaTime);
            animator.SetFloat(horParam, 0f, animDamp, Time.deltaTime);
            animator.SetFloat(stateParam, running ? 1f : 0f, animDamp, Time.deltaTime);
            animator.SetBool(jumpParam, !grounded);
        }

        private void HandleDigging()
        {
            if (terrain == null) return;
            if (Keyboard.current == null || !Keyboard.current.lKey.isPressed) return;

            Vector3 p = transform.position + transform.forward * digForwardOffset;
            p.y = terrain.SampleHeight(p.x, p.z);
            terrain.Dig(p, digRadius, digStrength * Time.deltaTime);
        }
    }
}
