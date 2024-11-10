using Dummiesman;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


public class VRLaserPointer : MonoBehaviour
{
    private LineRenderer laserLineRenderer;
    private float defaultLength = 3.0f;
    private Vector3 laserEndPoint;
    private InputAction triggerAction;
    private InputAction spawnAction;
    private GameObject currentlyHighlighted;
    private Material originalMaterial;
    private Material highlightMaterial;
    private bool isHoldingObject = false;
    private Vector3 grabOffset;
    private bool isDragging = false;
    private Vector3 offset;
    private Transform controllerTransform;

    void Start()
    {
        // Setup LineRenderer
        laserLineRenderer = gameObject.AddComponent<LineRenderer>();
        laserLineRenderer.startWidth = 0.01f;
        laserLineRenderer.endWidth = 0.01f;
        laserLineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        laserLineRenderer.material.color = Color.red;
        laserLineRenderer.positionCount = 2;

        // Get controller transform
        controllerTransform = transform;

        // Remove highlight material setup as we'll use Outline instead

        // Setup Input Actions
        triggerAction = new InputAction(type: InputActionType.Button);
        triggerAction.AddBinding("<XRController>{RightHand}/triggerButton");
        triggerAction.performed += ctx => HandleTriggerPress();
        triggerAction.Enable();

        spawnAction = new InputAction(type: InputActionType.Button);
        spawnAction.AddBinding("<XRController>{RightHand}/primaryButton"); // A button
        spawnAction.performed += ctx => StartCoroutine(LoadFromURL());
        spawnAction.Enable();
    }

    void OnDestroy()
    {
        triggerAction.Disable();
        spawnAction.Disable();
    }

    void Update()
    {
        UpdateLaser();
        
        // Handle highlight checking
        CheckForHighlight();

        // Check for trigger release
        if (isDragging && !triggerAction.IsPressed())
        {
            isDragging = false;
            // Allow the object to be rehighlighted
            currentlyHighlighted = null;
        }
    }

    private bool IsPlane(GameObject obj)
    {
        if (obj == null) return false;

        // Check if object has plane in its name
        if (obj.name.ToLower().Contains("plane"))
            return true;
            
        // Check for flat mesh
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            string meshName = meshFilter.sharedMesh.name.ToLower();
            if (meshName.Contains("plane") || meshName.Contains("floor") || meshName.Contains("ground"))
                return true;
        }

        // Check for flat transform scale
        if (Mathf.Approximately(obj.transform.localScale.y, 0.1f) && 
            obj.transform.localScale.x > 1f && 
            obj.transform.localScale.z > 1f)
            return true;
        
        return false;
    }

    private void CheckForHighlight()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Skip if the hit object is a plane
            if (IsPlane(hitObject))
            {
                if (!isDragging)
                {
                    UnhighlightCurrentObject();
                }
                return;
            }
            
            // Highlight any object the laser is pointing at
            if (currentlyHighlighted != hitObject && !isDragging)
            {
                UnhighlightCurrentObject();
                currentlyHighlighted = hitObject;
                HighlightObject(hitObject);
            }
        }
        else if (!isDragging)
        {
            UnhighlightCurrentObject();
        }
    }

    private void HighlightObject(GameObject obj)
    {
        if (obj == null) return;
        
        Outline outline = obj.GetComponent<Outline>();
        if (outline == null)
        {
            outline = obj.AddComponent<Outline>();
        }
        outline.enabled = true;
    }

    private IEnumerator EnableOutlineNextFrame(Outline outline)
    {
        yield return null; // Wait one frame
        if (outline != null)
        {
            outline.OutlineColor = Color.yellow;
            outline.OutlineWidth = 7.0f;
            outline.enabled = true;
        }
    }

    private void UnhighlightCurrentObject()
    {
        if (currentlyHighlighted != null)
        {
            // Only disable the outline if we're not dragging this object
            if (!isDragging)
            {
                Outline outline = currentlyHighlighted.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = false;
                }
                currentlyHighlighted = null;
            }
        }
    }

    private void HandleTriggerPress()
    {
        if (currentlyHighlighted != null)
        {
            if (!isDragging)
            {
                // Start dragging
                isDragging = true;
                offset = currentlyHighlighted.transform.position - controllerTransform.position;
            }
        }
    }

    private void UpdateLaser()
    {
        laserLineRenderer.SetPosition(0, transform.position);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit))
        {
            laserLineRenderer.SetPosition(1, hit.point);
            laserEndPoint = hit.point;

            // Move held object based on controller position
            if (isDragging && currentlyHighlighted != null)
            {
                currentlyHighlighted.transform.position = controllerTransform.position + 
                    Vector3.Scale(offset, controllerTransform.lossyScale);
            }
        }
        else
        {
            laserLineRenderer.SetPosition(1, transform.position + (transform.forward * defaultLength));
            laserEndPoint = transform.position + (transform.forward * defaultLength);
            UnhighlightCurrentObject();
        }
    }

    IEnumerator LoadFromURL()
    {
        string url = "https://people.sc.fsu.edu/~jburkardt/data/obj/lamp.obj";
        Debug.Log("Started loading object...");
        
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    using (var textStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(www.downloadHandler.text)))
                    {
                        GameObject loadedObj = new OBJLoader().Load(textStream);
                        if (loadedObj != null)
                        {
                            // Set the layer to Default (which is typically layer 0)
                            loadedObj.layer = 0;
                            foreach (Transform child in loadedObj.GetComponentsInChildren<Transform>())
                            {
                                child.gameObject.layer = 0;
                            }

                            // Set up materials for all mesh renderers
                            MeshRenderer[] renderers = loadedObj.GetComponentsInChildren<MeshRenderer>();
                            foreach (MeshRenderer renderer in renderers)
                            {
                                Material newMaterial = new Material(Shader.Find("Standard"));
                                newMaterial.color = Color.white;
                                renderer.material = newMaterial;
                            }

                            // Create and configure Physics Material
                            PhysicsMaterial physicsMaterial = new PhysicsMaterial();
                            physicsMaterial.dynamicFriction = 0.6f;
                            physicsMaterial.staticFriction = 0.6f;
                            physicsMaterial.bounciness = 0f;
                            physicsMaterial.frictionCombine = PhysicsMaterialCombine.Average;
                            
                            // Add mesh collider instead of box collider for better precision
                            MeshCollider meshCollider = loadedObj.AddComponent<MeshCollider>();
                            meshCollider.convex = true; // Enable convex for rigidbody interaction
                            meshCollider.material = physicsMaterial;

                            // Add and configure Rigidbody
                            Rigidbody rb = loadedObj.AddComponent<Rigidbody>();
                            rb.mass = 1f;
                            rb.useGravity = true;
                            rb.linearDamping = 1f; // Add linear drag
                            rb.angularDamping = 0.5f; // Add angular drag
                            rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent rotation
                            
                            // Add XR Grab Interactable
                            var grabInteractable = loadedObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                            
                            loadedObj.transform.position = laserEndPoint;
                            loadedObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                            loadedObj.transform.rotation = Quaternion.identity;
                            loadedObj.SetActive(true);
                            Debug.Log("Object spawned at: " + laserEndPoint);
                        }
                        else
                        {
                            Debug.LogError("Failed to create object from OBJ file");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error loading OBJ: " + e.Message);
                }
            }
            else
            {
                Debug.LogError("Failed to download file: " + www.error);
            }
      
    }
    }
}