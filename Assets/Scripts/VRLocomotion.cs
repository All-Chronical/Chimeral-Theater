
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChimeralTheater.Locomotion
{
    public enum LocomotionDirectionMode
    {
        HeadRelative
    }

    /// <summary>
    /// Moves and rotates a player body within the XR Origin. Input actions
    /// are looked up by name in a shared asset, keeping bindings out of code.
    /// </summary>
    public class VRLocomotion : MonoBehaviour
    {
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Transform playerBody;

        [Tooltip("Asset holding all Chimeral Theater actions (e.g. ChimeralInput.inputactions).")]
        [SerializeField] private InputActionAsset inputActions;


        [SerializeField] private LocomotionDirectionMode directionMode = LocomotionDirectionMode.HeadRelative;

        [SerializeField] private float speed = 2f;
        [SerializeField] private float rotationSpeed = 90f;

        private float speedMultiplier = 1f;
        private float rotationSpeedMultiplier = 1f;

        private const string MoveMapName = "Chimeral Theater";
        private const string MoveActionName = "Move";
        private const string RotateActionName = "Rotate";

        private InputAction moveAction;
        private InputAction rotateAction;

        private void Awake()
        {
            InputActionMap map = inputActions.FindActionMap(MoveMapName, throwIfNotFound: true);
            moveAction = map.FindAction(MoveActionName, throwIfNotFound: true);
            rotateAction = map.FindAction(RotateActionName, throwIfNotFound: true);
        }

        private void OnEnable()
        {
            moveAction.Enable();
            rotateAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            rotateAction.Disable();
        }

        private void Update()
        {
            Vector2 moveStick = moveAction.ReadValue<Vector2>();
            Vector3 displacement = GetDirection(moveStick) * (speed * speedMultiplier * Time.deltaTime);
            playerBody.position += displacement;

            float turnInput = rotateAction.ReadValue<Vector2>().x;
            playerBody.Rotate(0f, turnInput * rotationSpeed * rotationSpeedMultiplier * Time.deltaTime, 0f, Space.Self);
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = multiplier;
        }

        public void SetRotationSpeedMultiplier(float multiplier)
        {
            rotationSpeedMultiplier = multiplier;
        }

        private Vector3 GetDirection(Vector2 stick)
        {
            switch (directionMode)
            {
                case LocomotionDirectionMode.HeadRelative:
                    return GetHeadRelativeDirection(stick);
                default:
                    return Vector3.zero;
            }
        }

        private Vector3 GetHeadRelativeDirection(Vector2 stick)
        {
            Transform head = xrOrigin.Camera.transform;

            Vector3 forward = head.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = head.right;
            right.y = 0f;
            right.Normalize();

            return forward * stick.y + right * stick.x;
        }
    }
}
