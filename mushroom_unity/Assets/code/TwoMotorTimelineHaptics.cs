using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using System.Net;

/// <summary>
/// Controls first 2 motors (motors 0 and 1) with strength range 30-60.
/// Loads timeline from StreamingAssets and syncs with VideoPlayer.
/// Attach this component to the same GameObject as the VideoPlayer.
/// </summary>
public class TwoMotorTimelineHaptics : MonoBehaviour
{
    [Header("Video / Timeline")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string timelineFileName = "mushroom_timeline.json";
    [SerializeField, Tooltip("Send cues slightly early to offset network latency (ms).")]
    private float preSendOffsetMs = 30f;

    [Header("ESP32 UDP Target")]
    [SerializeField] private string esp32Address = "192.168.1.87";
    [SerializeField] private int esp32Port = 12345;
    [SerializeField] private bool logPackets = true;  // Enable by default for debugging
    [SerializeField, Tooltip("Send each command multiple times for reliability")]
    private int redundancyCount = 3;  // Send each command 3 times for poor WiFi

    [Header("Motor Strength Settings")]
    [SerializeField, Range(0, 255)] private int minStrength = 30;
    [SerializeField, Range(0, 255)] private int maxStrength = 60;
    [SerializeField, Tooltip("Which motors to control (0 and 1)")]
    private int[] motorIndices = new int[] { 0, 1 };

    private TimelineFile timeline;
    private int nextIndex;
    private bool ready;
    private UdpClient udpClient;
    private int consecutiveErrors = 0;
    private const int MAX_CONSECUTIVE_ERRORS = 10;
    private bool errorWarningShown = false;

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
        Debug.Log("=== TwoMotorTimelineHaptics Starting ===");
        
        if (!videoPlayer)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (!videoPlayer)
        {
            Debug.LogError("TwoMotorTimelineHaptics: Missing VideoPlayer reference.");
            yield break;
        }

        yield return StartCoroutine(LoadTimelineAsync());

        if (timeline?.entries == null || timeline.entries.Length == 0)
        {
            Debug.LogError("TwoMotorTimelineHaptics: Timeline file is empty or invalid.");
            yield break;
        }

        // Create UDP client (don't connect, just send to endpoint each time)
        try
        {
            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            // Set longer timeout for poor WiFi
            udpClient.Client.SendTimeout = 500; // 500ms timeout
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create UDP client: {ex.Message}");
            yield break;
        }
        
        Debug.Log($"UDP Client created successfully");
        Debug.Log($"Target: {esp32Address}:{esp32Port}");
        Debug.Log($"Redundancy: Sending each command {redundancyCount} times for reliability");
        
        // Verify ESP32 address is valid
        System.Net.IPAddress ipAddress;
        if (!System.Net.IPAddress.TryParse(esp32Address, out ipAddress))
        {
            Debug.LogError($"Invalid IP address: {esp32Address}");
            yield break;
        }
        
        // Send test PINGs with redundancy
        Debug.Log("Sending test PINGs...");
        byte[] pingData = Encoding.ASCII.GetBytes("PING");
        for (int i = 0; i < 3; i++)
        {
            try
            {
                udpClient.Send(pingData, pingData.Length, esp32Address, esp32Port);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"PING {i+1} failed: {ex.Message}");
            }
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("PINGs sent! Check ESP32 Serial Monitor.");
        
        ready = true;
        nextIndex = 0;

        Debug.Log($"Loaded {timeline.entries.Length} entries.");
        Debug.Log($"Motor strength range: {minStrength}-{maxStrength}");
        Debug.Log($"Controlling motors: {string.Join(", ", motorIndices)}");
        Debug.LogWarning("WARNING: WiFi connection has high latency. Consider moving ESP32 closer to router.");
        Debug.Log("=== Setup Complete ===");
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
            int rawDuty = timeline.entries[nextIndex].duty;
            // Map duty from 0-255 to minStrength-maxStrength
            int mappedDuty = MapDutyToRange(rawDuty);
            
            // Send to both motors
            foreach (int motorIndex in motorIndices)
            {
                SendMotorCommand(motorIndex, mappedDuty);
            }
            
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
                Debug.LogError($"TwoMotorTimelineHaptics: Failed to load timeline - {request.error}");
                yield break;
            }
            timeline = JsonUtility.FromJson<TimelineFile>(request.downloadHandler.text);
        }
#else
        if (!File.Exists(path))
        {
            Debug.LogError($"TwoMotorTimelineHaptics: Timeline file not found at {path}");
            yield break;
        }
        string json = File.ReadAllText(path);
        timeline = JsonUtility.FromJson<TimelineFile>(json);
        yield return null;
#endif
    }

    /// <summary>
    /// Maps duty value from 0-255 range to minStrength-maxStrength range.
    /// If input is 0, output is 0 (motor off).
    /// </summary>
    private int MapDutyToRange(int duty)
    {
        if (duty == 0)
        {
            return 0; // Keep motor off if duty is 0
        }
        
        // Map non-zero values from 1-255 to minStrength-maxStrength
        float normalized = (float)duty / 255f; // 0.0 to 1.0
        int mapped = Mathf.RoundToInt(Mathf.Lerp(minStrength, maxStrength, normalized));
        return Mathf.Clamp(mapped, minStrength, maxStrength);
    }

    /// <summary>
    /// Sends motor-specific command to ESP32 via UDP.
    /// Format: "Mx:duty" where x is motor index and duty is 0-255.
    /// Example: "M0:45" sets motor 0 to duty 45.
    /// Sends multiple times for reliability on poor WiFi.
    /// </summary>
    private void SendMotorCommand(int motorIndex, int duty)
    {
        if (udpClient == null)
        {
            if (!errorWarningShown)
            {
                Debug.LogError("UDP Client is null! Cannot send command.");
                errorWarningShown = true;
            }
            return;
        }
        
        duty = Mathf.Clamp(duty, 0, 255);
        string command = $"M{motorIndex}:{duty}";
        byte[] payload = Encoding.ASCII.GetBytes(command);
        
        // Send multiple times for reliability (fire-and-forget UDP)
        int successCount = 0;
        bool hadError = false;
        
        for (int i = 0; i < redundancyCount; i++)
        {
            try
            {
                udpClient.Send(payload, payload.Length, esp32Address, esp32Port);
                successCount++;
                consecutiveErrors = 0; // Reset error count on success
            }
            catch (SocketException ex)
            {
                hadError = true;
                consecutiveErrors++;
                
                // Only log first error and every 50th error to avoid spam
                if (consecutiveErrors == 1 || consecutiveErrors % 50 == 0)
                {
                    Debug.LogWarning($"Socket error {ex.ErrorCode}: {ex.Message} (Error #{consecutiveErrors})");
                    
                    if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS && !errorWarningShown)
                    {
                        Debug.LogError("===================================");
                        Debug.LogError("ESP32 UNREACHABLE!");
                        Debug.LogError("Check:");
                        Debug.LogError("1. ESP32 Serial Monitor - is it still connected?");
                        Debug.LogError("2. WiFi signal - Move ESP32 closer to router");
                        Debug.LogError("3. Restart ESP32");
                        Debug.LogError("===================================");
                        errorWarningShown = true;
                    }
                }
                break; // Stop trying if we get an error
            }
            catch (Exception ex)
            {
                hadError = true;
                consecutiveErrors++;
                if (consecutiveErrors == 1 || consecutiveErrors % 50 == 0)
                {
                    Debug.LogWarning($"UDP send failed: {ex.Message} (Error #{consecutiveErrors})");
                }
                break;
            }
        }
        
        if (logPackets && successCount > 0)
        {
            Debug.Log($"Sent {command} ({successCount}/{redundancyCount} attempts succeeded)");
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
        // Turn off motors when disabled
        if (udpClient != null)
        {
            foreach (int motorIndex in motorIndices)
            {
                SendMotorCommand(motorIndex, 0);
            }
        }
        
        udpClient?.Dispose();
        udpClient = null;
    }
}
