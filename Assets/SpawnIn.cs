using Dummiesman;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SpawnIn : MonoBehaviour 
{
    private Vector3 spawnPosition = new Vector3(0f, 0f, 0f);
    private float spawnDistance = 0.5f;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor;
    private bool isSpawning = false; // Prevent multiple simultaneous spawns
    private float spawnCooldown = 2f; // Time between spawns
    private float lastSpawnTime = 0f;

    void Awake()
    {
        interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor>();
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch) && 
            !isSpawning && 
            Time.time - lastSpawnTime >= spawnCooldown)
        {
            StartCoroutine(LoadFromURL());
        }
    }

    void OnDestroy()
    {
        // No longer needed
    }

    IEnumerator LoadFromURL()
    {
        if (isSpawning) yield break;
        isSpawning = true;
        lastSpawnTime = Time.time;

        var cameraRig = GetComponentInParent<OVRCameraRig>();
        if (cameraRig == null)
        {
            Debug.LogError("Could not find OVRCameraRig!");
            isSpawning = false;
            yield break;
        }

        var centerEye = cameraRig.centerEyeAnchor;
        spawnPosition = centerEye.position + (centerEye.forward * spawnDistance);

        string url = "https://people.sc.fsu.edu/~jburkardt/data/obj/lamp.obj";
        Debug.Log("Started loading object...");
        
        UnityWebRequest www = UnityWebRequest.Get(url);
        var operation = www.SendWebRequest();
        
        yield return operation;

        try
        {
            if (www.result == UnityWebRequest.Result.Success)
            {
                using (var textStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(www.downloadHandler.text)))
                {
                    GameObject loadedObj = new OBJLoader().Load(textStream);
                    if (loadedObj != null)
                    {
                        var grabInteractable = loadedObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                        var rigidbody = loadedObj.AddComponent<Rigidbody>();
                        rigidbody.useGravity = true;
                        
                        loadedObj.transform.position = spawnPosition;
                        loadedObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // Scale down the object
                        loadedObj.transform.rotation = Quaternion.identity;
                        loadedObj.SetActive(true);
                        Debug.Log($"Object spawned successfully at: {loadedObj.transform.position}");
                    }
                    else
                    {
                        Debug.LogError("Failed to create object from OBJ file");
                    }
                }
            }
            else
            {
                Debug.LogError("Failed to download file: " + www.error);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in spawn process: {e.Message}");
        }
        finally
        {
            www.Dispose();
            isSpawning = false;
        }

        yield return new WaitForSeconds(0.1f);
    }
}
