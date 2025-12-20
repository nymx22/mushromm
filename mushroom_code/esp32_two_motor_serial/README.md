# Two Motor USB Serial Control

Controls motors 0 and 1 via USB Serial connection - more reliable than WiFi!

## Advantages Over WiFi
- ✅ **No packet loss** (wired connection)
- ✅ **Low latency** (~1-5ms vs 50-300ms WiFi)
- ✅ **Simple setup** (no network configuration)
- ✅ **Very reliable** (no interference)
- ✅ **Easy debugging** (can test with Serial Monitor)

## Components

### 1. ESP32 Script: `esp32_two_motor_serial.ino`
Receives commands via USB Serial and controls motors 0 and 1.

### 2. Unity Script: `TwoMotorSerialHaptics.cs`
Reads timeline and sends commands to ESP32 via serial port.

## Motor Configuration

**Motor 0:**
- EN = 33, IN1 = 25, IN2 = 26

**Motor 1:**
- EN = 12, IN1 = 14, IN2 = 27

## Setup Instructions

### ESP32 Setup:

1. **Upload the code:**
   - Open `esp32_two_motor_serial.ino` in Arduino IDE
   - Select your ESP32 board and port
   - Upload

2. **Test with Serial Monitor:**
   - Open Serial Monitor (115200 baud)
   - You should see:
     ```
     === Two Motor USB Serial Control ===
     Motor 0: EN=33, IN1=25, IN2=26
     Motor 1: EN=12, IN1=14, IN2=27
     ✓ Ready for commands
     ```

3. **Test commands:**
   - Type: `PING` → should respond `PONG`
   - Type: `M0:50` → motor 0 should run at duty 50
   - Type: `M1:100` → motor 1 should run at duty 100
   - Type: `STATUS` → should show current motor states

4. **Close Serial Monitor** before using Unity!

### Unity Setup:

1. **Add script to VideoPlayer:**
   - Drag `TwoMotorSerialHaptics.cs` onto your VideoPlayer GameObject

2. **Find the serial port name:**
   
   **On Windows:**
   - Arduino IDE → Tools → Port (e.g., `COM3`, `COM4`)
   - Or Device Manager → Ports (COM & LPT)

   **On Mac:**
   - Open Terminal and run: `ls /dev/cu.*`
   - Look for: `/dev/cu.usbserial-XXXX` or `/dev/cu.wchusbserial-XXXX`
   - Or Arduino IDE → Tools → Port

3. **Configure in Unity Inspector:**
   - **Port Name**: Set to your serial port (e.g., `COM3` or `/dev/cu.usbserial-1420`)
   - **Baud Rate**: `115200` (default)
   - **Min Strength**: `30` (default)
   - **Max Strength**: `60` (default)
   - **Log Packets**: Enable for debugging

4. **Test:**
   - Right-click on the component → "List Available Serial Ports"
   - Should show your ESP32 port in Console
   - Run the scene
   - Check Unity Console for "✓ Serial port opened"

## Usage

### Commands:

The system sends commands in this format:
```
M0:45   // Set motor 0 to duty 45
M1:60   // Set motor 1 to duty 60
```

ESP32 responds with:
```
ACK:M0:45   // Acknowledgment
```

### Timeline Integration:

The timeline duty values (0-255) are automatically mapped to your configured range (30-60 by default).

**Example:**
- Timeline duty 0 → Motor duty 0 (off)
- Timeline duty 128 → Motor duty ~45 (middle of 30-60 range)
- Timeline duty 255 → Motor duty 60 (max)

## Troubleshooting

### "Failed to open serial port"
- **ESP32 not connected**: Plug in USB cable
- **Serial Monitor open**: Close Arduino Serial Monitor
- **Wrong port name**: Check Arduino IDE → Tools → Port
- **Permissions (Mac)**: May need to allow access in System Settings

### "No response from ESP32"
- ESP32 is booting (wait 2 seconds after upload)
- Serial Monitor is still open
- Baud rate mismatch (must be 115200)

### "Motors not responding"
- Check Serial Monitor to see if ESP32 receives commands
- Verify motor wiring
- Check power supply to motors

### Port name keeps changing (Mac)
- Each ESP32 gets a different port name
- Each time you plug/unplug, name might change
- Check with: `ls /dev/cu.*` every time

### Can't upload code
- Close Unity (it's using the serial port)
- Or disable/remove the TwoMotorSerialHaptics component
- Or stop Play mode in Unity

## Testing

### Manual Test from Serial Monitor:
1. Upload ESP32 code
2. Open Serial Monitor (115200 baud)
3. Type commands:
   ```
   PING          → should respond PONG
   STATUS        → shows motor states
   M0:100        → motor 0 runs at duty 100
   M1:50         → motor 1 runs at duty 50
   M0:0          → motor 0 stops
   ```

### Test from Unity:
1. Close Serial Monitor
2. Run Unity scene
3. Check Unity Console for:
   ```
   ✓ Serial port opened: COM3 @ 115200 baud
   ESP32 responded: PONG
   Loaded X entries.
   ```
4. Start video playback
5. Motors should sync with timeline

## Advantages vs WiFi Version

| Feature | WiFi | USB Serial |
|---------|------|------------|
| Latency | 50-300ms | 1-5ms |
| Packet loss | 0-30% | 0% |
| Setup complexity | High | Low |
| Reliability | Poor (in your case) | Excellent |
| Range | ~30m | ~2m (cable) |
| Debugging | Hard | Easy |

## Tips

- **Use USB extension cable** for longer reach (up to 15 feet with active cable)
- **USB hub works** if powered properly
- **Can't use Serial Monitor and Unity simultaneously** - choose one
- **Always close serial port** in Unity when stopping (handled automatically)
- **Test with Serial Monitor first** before using Unity

## Command Reference

### ESP32 → Unity (Responses)
```
PONG                // Response to PING
ACK:M0:45          // Command acknowledged
M0:45 M1:60 OK     // Status response
ERROR: message     // Error message
```

### Unity → ESP32 (Commands)
```
PING               // Test connection
STATUS             // Get motor status
M0:45              // Set motor 0 to duty 45
M1:100             // Set motor 1 to duty 100
```

## Safety

- **Timeout protection**: Motors automatically stop after 500ms of no commands
- **Safe shutdown**: Motors turn off when Unity stops
- **Error handling**: Invalid commands are rejected

## Performance

- **Latency**: ~1-5ms (vs 50-300ms WiFi)
- **Throughput**: ~11,520 bytes/sec @ 115200 baud
- **Commands/sec**: ~1000+ possible (way more than needed)
- **CPU usage**: Very low

Much faster and more reliable than your current WiFi setup!
