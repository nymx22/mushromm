using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>
/// Simple UDP test to verify Unity can send packets to ESP32.
/// Attach to any GameObject and check Console.
/// </summary>
public class SimpleUDPTest : MonoBehaviour
{
    [SerializeField] private string esp32Address = "192.168.1.87";
    [SerializeField] private int esp32Port = 12345;
    
    private UdpClient udpClient;
    private float nextSendTime = 0f;
    private int sendCount = 0;

    void Start()
    {
        Debug.Log("=== Simple UDP Test ===");
        
        try
        {
            // Method 1: Simple UdpClient
            udpClient = new UdpClient();
            Debug.Log($"✓ UDP Client created");
            
            // Get local IP
            string localIP = GetLocalIPAddress();
            Debug.Log($"Local IP: {localIP}");
            Debug.Log($"Target: {esp32Address}:{esp32Port}");
            
            // Send initial PING
            SendPing();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create UDP client: {ex.Message}");
        }
    }

    void Update()
    {
        if (udpClient != null && Time.time >= nextSendTime)
        {
            SendPing();
            nextSendTime = Time.time + 2f; // Send every 2 seconds
        }
    }

    void SendPing()
    {
        try
        {
            sendCount++;
            string message = $"PING {sendCount}";
            byte[] data = Encoding.ASCII.GetBytes(message);
            
            // Method 1: Direct send with endpoint
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(esp32Address), esp32Port);
            int bytesSent = udpClient.Send(data, data.Length, endpoint);
            
            Debug.Log($"[{sendCount}] Sent '{message}' ({bytesSent} bytes) to {esp32Address}:{esp32Port}");
            Debug.Log($"Check ESP32 Serial Monitor for 'PING {sendCount} received'");
        }
        catch (SocketException ex)
        {
            Debug.LogError($"Socket error {ex.ErrorCode}: {ex.Message}");
            
            // Provide specific help based on error code
            switch (ex.ErrorCode)
            {
                case 10064: // Host is down
                    Debug.LogError("Host is down - ESP32 unreachable. Check:");
                    Debug.LogError("1. ESP32 is powered on and connected to WiFi");
                    Debug.LogError("2. ESP32 IP is correct: " + esp32Address);
                    Debug.LogError("3. Mac and ESP32 are on same network");
                    Debug.LogError("4. Try: ping " + esp32Address + " in Terminal");
                    break;
                case 10065: // No route to host
                    Debug.LogError("No route to host - Network routing issue");
                    break;
                case 10013: // Permission denied
                    Debug.LogError("Permission denied - Check firewall settings");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send failed: {ex.Message}");
        }
    }

    string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "No IPv4 address found";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    void OnDisable()
    {
        udpClient?.Close();
        udpClient = null;
    }
}
