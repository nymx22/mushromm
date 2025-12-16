using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

/// <summary>
/// Loads a precomputed vibration timeline from StreamingAssets,
/// keeps it in lockstep with a VideoPlayer, and streams duty values
/// to an ESP32 over UDP.
/// Attach this component to the same GameObject as the VideoPlayer.
/// </summary>
public class UnityTimelineHaptics : MonoBehaviour
{
    [Header("Video / Timeline")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string timelineFileName = "mushroom_timeline.json";
    [SerializeField, Tooltip("Send cues slightly early to offset network latency (ms).")]
    private float preSendOffsetMs = 30f;

    [Header("ESP32 UDP Target")]
    [SerializeField] private string esp32Address = "192.168.1.50";
    [SerializeField] private int esp32Port = 12345;
    [SerializeField] private bool logPackets = false;

    [Header("Speaker Audio Output")]
    [SerializeField] private bool enableAudioOutput = true;
    [SerializeField, Range(0f, 50f), Tooltip("Frequency in Hz (10-20Hz = heavy vibration, below hearing range)")]
    private float vibrationFrequency = 15f;
    [SerializeField, Range(0f, 1f), Tooltip("Maximum audio volume")]
    private float maxAudioVolume = 1.0f;
    [SerializeField, Range(1f, 5f), Tooltip("Volume boost multiplier for stronger vibration (higher = stronger)")]
    private float volumeBoost = 3.0f;
    [SerializeField, Tooltip("Smoothing factor for audio transitions (0-1, higher = smoother)")]
    private float audioSmoothing = 0.95f;
    [SerializeField, Range(0f, 1f), Tooltip("Waveform type: 0.0 = square (strongest vibration), 0.5 = triangle, 1.0 = sine (quietest)")]
    private float waveformType = 0.2f;

    private TimelineFile timeline;
    private int nextIndex;
    private bool ready;
    private UdpClient udpClient;
    private AudioSource audioSource;
    private float currentAudioVolume = 0f;
    private float targetAudioVolume = 0f;
    private int currentDuty = 0;
    private double phase = 0.0;
    private double cachedSampleRate = 44100.0; // Default fallback

    [Serializable]
    private class TimelineFile
    {
        public string source;
        public int sample_rate;
        public int chunk_ms;
        public TimelineEntry[] entries;
    }

    [Serializable]
    private class TimelineEntry
    {
        public float time;
        public int duty;
    }

    private IEnumerator Start()
    {
        if (!videoPlayer)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (!videoPlayer)
        {
            Debug.LogError("UnityTimelineHaptics: Missing VideoPlayer reference.");
            yield break;
        }

        // Setup audio source for speaker output
        if (enableAudioOutput)
        {
            // Cache sample rate on main thread (must be done before OnAudioFilterRead is called)
            cachedSampleRate = AudioSettings.outputSampleRate;
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.volume = 1f; // Set to 1, we control volume in OnAudioFilterRead
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.priority = 0; // High priority
            audioSource.bypassEffects = true;
            audioSource.bypassListenerEffects = true;
            audioSource.bypassReverbZones = true;
            
            // Create a dummy AudioClip to ensure AudioSource can play
            // OnAudioFilterRead will override the audio data
            int samples = (int)(cachedSampleRate * 1.0f); // 1 second of audio
            AudioClip dummyClip = AudioClip.Create("HapticsAudioClip", samples, 2, (int)cachedSampleRate, true, OnAudioRead);
            audioSource.clip = dummyClip;
            
            // Start playing so OnAudioFilterRead is called
            audioSource.Play();
        }

        yield return StartCoroutine(LoadTimelineAsync());

        if (timeline?.entries == null || timeline.entries.Length == 0)
        {
            Debug.LogError("UnityTimelineHaptics: Timeline file is empty or invalid.");
            yield break;
        }

        udpClient = new UdpClient();
        ready = true;
        nextIndex = 0;

        if (logPackets)
        {
            Debug.Log($"UnityTimelineHaptics: Loaded {timeline.entries.Length} entries.");
        }
    }

    private void Update()
    {
        if (!ready || udpClient == null)
        {
            return;
        }

        if (!videoPlayer.isPlaying || nextIndex >= timeline.entries.Length)
        {
            return;
        }

        double currentTime = videoPlayer.time;
        double offsetTime = currentTime + (preSendOffsetMs / 1000.0);

        while (nextIndex < timeline.entries.Length &&
               offsetTime >= timeline.entries[nextIndex].time)
        {
            int duty = timeline.entries[nextIndex].duty;
            SendDuty(duty);
            UpdateAudioVolume(duty);
            nextIndex++;
        }

        // Smooth audio transitions
        if (enableAudioOutput)
        {
            currentAudioVolume = Mathf.Lerp(currentAudioVolume, targetAudioVolume, 1f - audioSmoothing);
        }
    }

    private IEnumerator LoadTimelineAsync()
    {
        string path = Path.Combine(Application.streamingAssetsPath, timelineFileName);
#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest request = UnityWebRequest.Get(path))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"UnityTimelineHaptics: Failed to load timeline - {request.error}");
                yield break;
            }
            timeline = JsonUtility.FromJson<TimelineFile>(request.downloadHandler.text);
        }
#else
        if (!File.Exists(path))
        {
            Debug.LogError($"UnityTimelineHaptics: Timeline file not found at {path}");
            yield break;
        }
        string json = File.ReadAllText(path);
        timeline = JsonUtility.FromJson<TimelineFile>(json);
        yield return null;
#endif
    }

    private void SendDuty(int duty)
    {
        duty = Mathf.Clamp(duty, 0, 255);
        currentDuty = duty;
        byte[] payload = Encoding.ASCII.GetBytes(duty.ToString());
        try
        {
            udpClient.Send(payload, payload.Length, esp32Address, esp32Port);
            if (logPackets)
            {
                Debug.Log($"UnityTimelineHaptics: Sent duty {duty}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"UnityTimelineHaptics: UDP send failed - {ex.Message}");
        }
    }

    private void UpdateAudioVolume(int duty)
    {
        if (!enableAudioOutput) return;
        
        // Convert duty (0-255) to audio volume (0-1) with boost multiplier
        float baseVolume = (duty / 255f) * maxAudioVolume;
        targetAudioVolume = Mathf.Clamp01(baseVolume * volumeBoost);
    }
    
    /// <summary>
    /// Callback for dummy AudioClip (OnAudioFilterRead will override the audio)
    /// </summary>
    private void OnAudioRead(float[] data)
    {
        // Fill with silence as fallback (OnAudioFilterRead should override this)
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0f;
        }
    }

    /// <summary>
    /// Generates low-frequency audio for speaker vibration.
    /// This is called automatically by Unity's audio system.
    /// </summary>
    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!enableAudioOutput || !ready) return;

        // Use cached sample rate instead of accessing AudioSettings from audio thread
        double increment = vibrationFrequency / cachedSampleRate;

        for (int i = 0; i < data.Length; i += channels)
        {
            float sample = 0f;
            
            // Generate waveform based on type parameter
            // 0.0 = square wave (strongest vibration, more harmonics = more sound)
            // 0.5 = triangle wave (strong vibration, fewer harmonics)
            // 1.0 = sine wave (smooth, minimal harmonics = quietest)
            
            if (waveformType < 0.5f)
            {
                // Blend between square and triangle
                float squareWave = (phase < 0.5) ? 1f : -1f;
                float triangleWave = 2f * Mathf.Abs(2f * ((float)phase - Mathf.Floor((float)phase + 0.5f))) - 1f;
                float blend = waveformType * 2f; // 0 to 1 when waveformType is 0 to 0.5
                sample = Mathf.Lerp(squareWave, triangleWave, blend);
            }
            else
            {
                // Blend between triangle and sine
                float triangleWave = 2f * Mathf.Abs(2f * ((float)phase - Mathf.Floor((float)phase + 0.5f))) - 1f;
                float sineWave = Mathf.Sin((float)phase * 2f * Mathf.PI);
                float blend = (waveformType - 0.5f) * 2f; // 0 to 1 when waveformType is 0.5 to 1.0
                sample = Mathf.Lerp(triangleWave, sineWave, blend);
            }
            
            // Apply volume
            sample *= currentAudioVolume;
            
            // Apply to all channels
            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample;
            }

            phase += increment;
            if (phase > 1.0) phase -= 1.0;
        }
    }

    public void SeekTo(double timeSeconds)
    {
        if (timeline?.entries == null)
        {
            return;
        }

        // Find first entry at or after the requested time.
        int idx = Array.FindIndex(timeline.entries, entry => entry.time >= timeSeconds);
        nextIndex = idx < 0 ? timeline.entries.Length : idx;
    }

    private void OnDisable()
    {
        udpClient?.Dispose();
        udpClient = null;
        
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}

