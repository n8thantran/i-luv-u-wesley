using Dummiesman;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

using UnityEngine.InputSystem;

public class SpawnIn : MonoBehaviour 
{
    private Vector3 spawnPosition = new Vector3(0f, 0f, 0f);
    private InputAction activateAction;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor;

    void Awake()
    {
        // Get the Interactor component instead of XRBaseController
        interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor>();
        
        // Create and configure the input action
        activateAction = new InputAction(type: InputActionType.Button);
        activateAction.AddBinding("<XRController>/primaryButton");
        activateAction.performed += ctx => StartCoroutine(LoadFromURL());
        activateAction.Enable();
    }

    void OnDestroy()
    {
        activateAction.Disable();
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
                            // Add XR Grab Interactable component to make object interactive
                            var grabInteractable = loadedObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                            
                            loadedObj.transform.position = spawnPosition;
                            loadedObj.transform.localScale = new Vector3(1f, 1f, 1f);
                            loadedObj.transform.rotation = Quaternion.identity;
                            loadedObj.SetActive(true);
                            Debug.Log("Object spawned at: " + spawnPosition);
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
