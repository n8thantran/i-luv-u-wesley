using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using Dummiesman;
using UnityEngine.Networking;

public class VRLaserPointer : MonoBehaviour
{
    public float laserDistance = 10f;
    public float laserWidth = 0.01f;
    public Material laserMaterial;
    
    private GameObject laserPointer;
    private LineRenderer lineRenderer;
    private Transform rightHandAnchor;
    private GameObject loadedLampPrefab;
    private bool isLoading = false;

    void Start()
    {
        rightHandAnchor = GameObject.Find("OVRCameraRig/TrackingSpace/RightHandAnchor").transform;
        if (rightHandAnchor == null)
        {
            Debug.LogError("Could not find RightHandAnchor!");
        }
        CreateLaser();
    }

    void CreateLaser()
    {
        laserPointer = new GameObject("LaserPointer");
        lineRenderer = laserPointer.AddComponent<LineRenderer>();
        
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        lineRenderer.material = laserMaterial;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = true;
    }

    void Update()
    {
        UpdateLaser();
            
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            Debug.Log("A Button Pressed - Starting lamp download");
            if (!isLoading)
            {
                StartCoroutine(LoadAndSpawnLamp());
            }
        }
    }

    void UpdateLaser()
    {
        Ray ray = new Ray(rightHandAnchor.position, rightHandAnchor.forward);
        RaycastHit hit;

        Vector3 endPosition;
        if (Physics.Raycast(ray, out hit, laserDistance))
        {
            endPosition = hit.point;
        }
        else
        {
            endPosition = ray.origin + ray.direction * laserDistance;
        }

        lineRenderer.SetPosition(0, ray.origin);
        lineRenderer.SetPosition(1, endPosition);
    }

    private IEnumerator LoadAndSpawnLamp()
    {
        isLoading = true;
        Debug.Log("Starting lamp download...");

        string url = "https://people.sc.fsu.edu/~jburkardt/data/obj/lamp.obj";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download lamp: {www.error}");
                isLoading = false;
                yield break;
            }

            Debug.Log("Lamp downloaded successfully, creating object...");
            string objData = www.downloadHandler.text;

            try
            {
                var textStream = new MemoryStream(Encoding.UTF8.GetBytes(objData));
                loadedLampPrefab = new OBJLoader().Load(textStream);
                Debug.Log("Lamp object created successfully");

                Ray ray = new Ray(rightHandAnchor.position, rightHandAnchor.forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, laserDistance))
                {
                    Vector3 spawnPosition = hit.point;
                    Quaternion spawnRotation = Quaternion.identity;
                    
                    GameObject spawnedLamp = Instantiate(loadedLampPrefab, spawnPosition, spawnRotation);
                    Debug.Log($"Lamp spawned at position: {spawnPosition}");

                    // Add a collider to the spawned lamp
                    if (spawnedLamp.GetComponent<Collider>() == null)
                    {
                        spawnedLamp.AddComponent<BoxCollider>();
                    }

                    // Add rigidbody for physics
                    if (spawnedLamp.GetComponent<Rigidbody>() == null)
                    {
                        Rigidbody rb = spawnedLamp.AddComponent<Rigidbody>();
                        rb.useGravity = true;
                    }
                }
                else
                {
                    Debug.LogWarning("No surface detected to spawn lamp");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating lamp object: {e.Message}\n{e.StackTrace}");
            }
        }

        isLoading = false;
        Debug.Log("Lamp spawning process completed");
    }
}