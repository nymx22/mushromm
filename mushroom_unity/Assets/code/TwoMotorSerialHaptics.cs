#if !UNITY_WEBGL
using System;
using System.Collections;
using System.IO;
#if UNITY_STANDALONE || UNITY_EDITOR
using System.IO.Ports;
#endif
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
#endif

/// <summary>
/// Controls first 2 motors via USB Serial connection with strength range 30-60.
/// More reliable than WiFi - no packet loss, low latency.
/// Attach this component to the same GameObject as the VideoPlayer.
/// 
/// REQUIREMENTS:
/// - Unity must be set to .NET 4.x API Compatibility Level
/// - Edit → Project Settings → Player → Other Settings → Api Compatibility Level → .NET Framework
/// </summary>
#if !UNITY_WEBGL && (UNITY_STANDALONE || UNITY_EDITOR)
public class TwoMotorSerialHaptics : MonoBehaviour
{
    [Header("Video / Timeline")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string timelineFileName = "mushroom_timeline.json";
    [SerializeField, Tooltip("Send cues slightly early to offset latency (ms).")]
    private float preSendOffsetMs = 5f; // Much lower than WiFi - serial is fast!

    [Header("Serial Port Settings")]
    [SerializeField, Tooltip("Serial port name (e.g., COM3 on Windows, /dev/cu.usbserial-xxxx on Mac)")]
    private string portName = "/dev/cu.usbserial-10";
    [SerializeField] private int baudRate = 115200;
    [SerializeField] private bool logPackets = true;

    [Header("Motor Strength Settings")]
    [SerializeField, Range(0, 255)] private int minStrength = 30;
    [SerializeField, Range(0, 255)] private int maxStrength = 60;
    [SerializeField, Tooltip("Which motors to control (0 and 1)")]
    private int[] motorIndices = new int[] { 0, 1 };

    private TimelineFile timeline;
    private int nextIndex;
    private bool ready;
    private SerialPort serialPort;

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
        Debug.Log("=== TwoMotorSerialHaptics Starting ===");
        
        if (!videoPlayer)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (!videoPlayer)
        {
            Debug.LogError("TwoMotorSerialHaptics: Missing VideoPlayer reference.");
            yield break;
        }

        yield return StartCoroutine(LoadTimelineAsync());

        if (timeline?.entries == null || timeline.entries.Length == 0)
        {
            Debug.LogError("TwoMotorSerialHaptics: Timeline file is empty or invalid.");
            yield break;
        }

        // Setup serial port - no try-catch around yield statements
        bool portOpened = false;
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;
            serialPort.NewLine = "\n";
            serialPort.Open();
            
            Debug.Log($"✓ Serial port opened: {portName} @ {baudRate} baud");
            portOpened = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to open serial port: {ex.Message}");
            Debug.LogError("Make sure:");
            Debug.LogError("1. ESP32 is connected via USB");
            Debug.LogError("2. Arduino Serial Monitor is CLOSED");
            Debug.LogError("3. Port name is correct (check Arduino IDE → Tools → Port)");
            Debug.LogError($"4. On Mac, try: ls /dev/cu.* in Terminal to find port name");
            yield break;
        }
        
        if (!portOpened)
        {
            yield break;
        }
        
        // Wait for ESP32 to initialize
        yield return new WaitForSeconds(2f);
        
        // Send test PING
        Debug.Log("Sending test PING...");
        try
        {
            serialPort.WriteLine("PING");
            
            // Try to read response
            string response = serialPort.ReadLine();
            Debug.Log($"ESP32 responded: {response}");
        }
        catch (TimeoutException)
        {
            Debug.LogWarning("No response from ESP32 (timeout). Check Serial Monitor is closed.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Communication error: {ex.Message}");
        }
        
        ready = true;
        nextIndex = 0;

        Debug.Log($"Loaded {timeline.entries.Length} entries.");
        Debug.Log($"Motor strength range: {minStrength}-{maxStrength}");
        Debug.Log($"Controlling motors: {string.Join(", ", motorIndices)}");
        Debug.Log("=== Setup Complete ===");
    }

    private void Update()
    {
        if (!ready || serialPort == null || !serialPort.IsOpen)
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
                Debug.LogError($"TwoMotorSerialHaptics: Failed to load timeline - {request.error}");
                yield break;
            }
            timeline = JsonUtility.FromJson<TimelineFile>(request.downloadHandler.text);
        }
#else
        if (!File.Exists(path))
        {
            Debug.LogError($"TwoMotorSerialHaptics: Timeline file not found at {path}");
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
    /// Sends motor-specific command to ESP32 via Serial.
    /// Format: "Mx:duty" where x is motor index and duty is 0-255.
    /// Example: "M0:45" sets motor 0 to duty 45.
    /// </summary>
    private void SendMotorCommand(int motorIndex, int duty)
    {
        if (serialPort == null || !serialPort.IsOpen)
        {
            Debug.LogError("Serial port is not open!");
            return;
        }
        
        duty = Mathf.Clamp(duty, 0, 255);
        string command = $"M{motorIndex}:{duty}";
        
        try
        {
            serialPort.WriteLine(command);
            
            if (logPackets)
            {
                Debug.Log($"Sent: {command}");
            }
        }
        catch (TimeoutException)
        {
            Debug.LogWarning($"Serial write timeout for command: {command}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Serial send failed: {ex.Message}");
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
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                foreach (int motorIndex in motorIndices)
                {
                    serialPort.WriteLine($"M{motorIndex}:0");
                }
            }
            catch { }
            
            serialPort.Close();
        }
        
        serialPort = null;
    }

    private void OnApplicationQuit()
    {
        OnDisable();
    }

    /// <summary>
    /// Helper to list available serial ports - call from Unity Console or Inspector button
    /// </summary>
    [ContextMenu("List Available Serial Ports")]
    private void ListAvailablePorts()
    {
        string[] ports = SerialPort.GetPortNames();
        Debug.Log($"Available serial ports ({ports.Length}):");
        foreach (string port in ports)
        {
            Debug.Log($"  - {port}");
        }
        
        if (ports.Length == 0)
        {
            Debug.LogWarning("No serial ports found. Is ESP32 connected?");
        }
    }
}
#else
// Dummy class for WebGL or unsupported platforms
public class TwoMotorSerialHaptics : MonoBehaviour
{
    private void Start()
    {
        Debug.LogError("TwoMotorSerialHaptics requires .NET 4.x API Compatibility Level!");
        Debug.LogError("Go to: Edit → Project Settings → Player → Other Settings → Api Compatibility Level → .NET Framework");
        enabled = false;
    }
}
#endif
