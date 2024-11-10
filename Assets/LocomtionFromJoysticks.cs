using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class JoystickLocomotion : MonoBehaviour
{
    public float speed = 2.0f;
    private XROrigin xrOrigin;
    private ActionBasedContinuousMoveProvider moveProvider;
    [SerializeField] private InputActionReference moveInputActionReference;

    void Start()
    {
        // Find XR Origin in the scene
        xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("No XROrigin found in scene!");
            return;
        }

        // Set up move provider if needed
        moveProvider = GetComponent<ActionBasedContinuousMoveProvider>();
        if (moveProvider == null)
        {
            moveProvider = gameObject.AddComponent<ActionBasedContinuousMoveProvider>();
        }
    }

    void Update()
    {
        if (xrOrigin == null || moveInputActionReference == null) return;

        // Read the movement input
        Vector2 input = moveInputActionReference.action.ReadValue<Vector2>();

        // Get the camera's forward and right vectors
        Vector3 forward = xrOrigin.Camera.transform.forward;
        Vector3 right = xrOrigin.Camera.transform.right;

        // Project to horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction
        Vector3 movement = (forward * input.y + right * input.x) * speed * Time.deltaTime;

        // Apply movement to the XR Origin
        xrOrigin.transform.position += movement;
    }

    private void OnEnable()
    {
        if (moveInputActionReference != null)
            moveInputActionReference.action.Enable();
    }

    private void OnDisable()
    {
        if (moveInputActionReference != null)
            moveInputActionReference.action.Disable();
    }
}
