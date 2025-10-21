using UnityEngine;
using UnityEngine.Video;
using UnityOSC;
using System.Collections;
using System.Collections.Generic;

public class VR360Controller : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public string videoFileName = "scene3.mp4";

    [Header("OSC Settings")]
    public int oscPort = 8000;
    private OSCServer oscServer;

    [Header("Effects")]
    public GameObject flashEffect;
    public AudioSource audioSource;

    private bool isLightOn = false;
    private float motorSpeed = 0f;

    void Start()
    {
        Debug.Log("=== VR360Controller Starting ===");
        SetupVideo();
        SetupOSC();
        Debug.Log("=== VR360Controller Started ===");

        // Debug: Check if sphere exists
        GameObject sphere = GameObject.Find("360VideoSphere");
        if (sphere != null)
        {
            Debug.Log("Sphere found: " + sphere.name);
        }
        else
        {
            Debug.Log("Sphere NOT found!");
        }
    }

    void SetupVideo()
    {
        Debug.Log("Setting up video...");

        // Create video player
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = Application.streamingAssetsPath + "/" + videoFileName;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.MaterialOverride;

        Debug.Log("Video URL: " + videoPlayer.url);
        Debug.Log("Video file exists: " + System.IO.File.Exists(Application.streamingAssetsPath + "/" + videoFileName));

        videoPlayer.Play();
        Debug.Log("Video play called");
    }

    void SetupOSC()
    {
        // Start OSC server to receive commands from Processing
        oscServer = new OSCServer(oscPort);
        Debug.Log("OSC Server started on port " + oscPort);
    }

    void Update()
    {
        // Debug video status
        if (videoPlayer != null)
        {
            Debug.Log("Video is playing: " + videoPlayer.isPlaying);
            Debug.Log("Video texture: " + (videoPlayer.texture != null ? "Available" : "Null"));
            if (videoPlayer.texture != null)
            {
                Debug.Log("Texture size: " + videoPlayer.texture.width + "x" + videoPlayer.texture.height);
            }
        }

        // Update video texture on sphere
        if (videoPlayer != null && videoPlayer.texture != null)
        {
            GameObject sphere = GameObject.Find("360VideoSphere");
            if (sphere != null)
            {
                Renderer renderer = sphere.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.SetTexture("_MainTex", videoPlayer.texture);
                    Debug.Log("Applied texture to sphere!");
                }
            }
        }
    }

    void ProcessCommand(string command)
    {
        string[] parts = command.Split(',');

        if (parts[0] == "VR:RESET")
        {
            ResetAll();
        }
        else if (parts[0] == "VR:SET" && parts[1] == "virtualLight")
        {
            SetLight(parts[2] == "1");
        }
        else if (parts[0] == "VR:VR_EFFECT" && parts[1] == "flash")
        {
            FlashScreen(parts[2], int.Parse(parts[3]));
        }
        else if (parts[0] == "VR:PWM" && parts[1] == "virtualMotor")
        {
            SetMotor(float.Parse(parts[2]));
        }
    }

    void ResetAll()
    {
        isLightOn = false;
        motorSpeed = 0f;
        if (flashEffect) flashEffect.SetActive(false);
    }

    void SetLight(bool on)
    {
        isLightOn = on;
        if (flashEffect) flashEffect.SetActive(on);
    }

    void FlashScreen(string color, int duration)
    {
        if (flashEffect)
        {
            flashEffect.SetActive(true);
            Invoke("TurnOffFlash", duration / 1000f);
        }
    }

    void TurnOffFlash()
    {
        if (flashEffect) flashEffect.SetActive(false);
    }

    void SetMotor(float speed)
    {
        motorSpeed = speed;
        // Add haptic feedback here if needed
    }

    void OnDestroy()
    {
        if (oscServer != null)
        {
            oscServer.Close();
        }
    }
}

