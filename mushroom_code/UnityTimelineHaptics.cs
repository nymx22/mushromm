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

    private TimelineFile timeline;
    private int nextIndex;
    private bool ready;
    private UdpClient udpClient;

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
            SendDuty(timeline.entries[nextIndex].duty);
            nextIndex++;
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
    }
}

