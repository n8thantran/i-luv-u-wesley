using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using Dummiesman;
using UnityEngine.Networking;
using Meta.WitAi;
using Meta.WitAi.Json;
using TMPro;
using Oculus.Voice;

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
    private AppVoiceExperience voiceService;
    private TMP_Text status;

    void Start()
    {
        rightHandAnchor = GameObject.Find("OVRCameraRig/TrackingSpace/RightHandAnchor").transform;
        if (rightHandAnchor == null)
        {
            Debug.LogError("Could not find RightHandAnchor!");
        }

        voiceService = GameObject.Find("AppVoiceExperience").GetComponent<AppVoiceExperience>();
        if (voiceService == null)
        {
            Debug.LogError("Could not find VoiceService!");
        }
        voiceService.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscriptionString);

        status = GameObject.Find("Canvas/Status").GetComponent<TMP_Text>();
        if (status == null)
        {
            Debug.LogError("Could not find TextMesh Pro component!");
        }

        status.text = "Click the A button to spawn an object";

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
                status.text = "Listening for voice command...";
                voiceService.Activate();
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

    void OnFullTranscriptionString(string transcription)
    {
        transcription = transcription.ToLower();
        status.text = "Transcription received";

        string BASE_URL = "https://4f1e-68-65-175-63.ngrok-free.app/generate";
        string url = $"{BASE_URL}/{UnityWebRequest.EscapeURL(transcription)}";
        Debug.Log($"Transcription URL: {url}");
    
        // Start loading object
        StartCoroutine(LoadAndSpawnLamp(url));
    }

    private IEnumerator LoadAndSpawnLamp(string url)
    {
        status.text = "Loading object...";
        isLoading = true;
        Debug.Log("Starting lamp download...");

        // url = "https://dreamscapeassetbucket.s3.us-west-1.amazonaws.com/output/blue_bird/output.obj";
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
        status.text = "Press A to spawn another object";
        isLoading = false;
        Debug.Log("Lamp spawning process completed");
    }
}