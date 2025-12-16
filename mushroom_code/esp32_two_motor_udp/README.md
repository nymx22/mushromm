# Two Motor Timeline Haptics System

Controls motors 0 and 1 with strength range 30-60 based on mushroom timeline.

## Components

### 1. Unity Script: `TwoMotorTimelineHaptics.cs`
- Location: `mushroom_unity/Assets/code/TwoMotorTimelineHaptics.cs`
- Reads `mushroom_timeline.json` from StreamingAssets
- Syncs with VideoPlayer
- Maps duty values from 0-255 to 30-60 range
- Sends UDP commands to ESP32 in format: `M0:duty` and `M1:duty`

### 2. ESP32 Script: `esp32_two_motor_udp.ino`
- Location: `mushroom_code/esp32_two_motor_udp/esp32_two_motor_udp.ino`
- Receives motor-specific UDP commands
- Controls motors 0 and 1 independently
- Safety timeout: stops motors after 500ms of no updates

## Motor Configuration

**Motor 0:**
- EN = 33
- IN1 = 25
- IN2 = 26
- Channel = 0

**Motor 1:**
- EN = 12
- IN1 = 14
- IN2 = 27
- Channel = 1

## Setup Instructions

### ESP32 Setup:
1. Open `esp32_two_motor_udp.ino` in Arduino IDE
2. Update WiFi credentials if needed (lines 5-6)
3. Upload to ESP32
4. Note the IP address shown in Serial Monitor

### Unity Setup:
1. Add `TwoMotorTimelineHaptics.cs` to your VideoPlayer GameObject
2. Set ESP32 IP address in Inspector
3. Ensure `mushroom_timeline.json` is in StreamingAssets folder
4. Adjust strength range in Inspector (default: 30-60)

## Configuration

### Unity Inspector Settings:
- **Timeline File Name**: `mushroom_timeline.json`
- **Pre Send Offset Ms**: `30` (compensate for network latency)
- **ESP32 Address**: Your ESP32 IP (e.g., `192.168.1.50`)
- **ESP32 Port**: `12345`
- **Min Strength**: `30` (minimum motor duty)
- **Max Strength**: `60` (maximum motor duty)
- **Motor Indices**: `[0, 1]` (which motors to control)
- **Log Packets**: Enable for debugging

### How Strength Mapping Works:
- Timeline duty value of 0 → motor off (0)
- Timeline duty value of 1-255 → mapped to 30-60 range
- Example: duty 128 (50%) → approximately 45 (midpoint of 30-60)

## UDP Command Format

The Unity script sends commands in this format:
```
M0:45  // Set motor 0 to duty 45
M1:60  // Set motor 1 to duty 60
```

## Testing

1. Upload ESP32 code and verify WiFi connection
2. Run Unity scene with VideoPlayer
3. Check Serial Monitor for received commands
4. Verify motors respond to video timeline

## Troubleshooting

- **Motors not responding**: Check WiFi connection and IP address
- **Too weak/strong**: Adjust minStrength/maxStrength in Unity Inspector
- **Motors timeout**: Increase SAFETY_TIMEOUT_MS in ESP32 code
- **Latency**: Adjust preSendOffsetMs in Unity Inspector
