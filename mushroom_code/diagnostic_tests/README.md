# Motor Diagnostic Tests

Use these tests to diagnose intermittent motor issues.

## Test Order

Run tests in this order to isolate the problem:

### **Test 1: LED Blink** (`test1_led_blink.ino`)
**Purpose:** Verify ESP32 and upload process work

**What it does:**
- Blinks built-in LED
- Prints to Serial Monitor

**If this fails:**
- ESP32 not powered
- Wrong board selected in Arduino IDE
- Upload failed

**If this works:** → Continue to Test 2

---

### **Test 2: Pin Toggle** (`test2_pin_toggle.ino`)
**Purpose:** Verify motor control pins can output

**What it does:**
- Toggles EN, IN1, IN2 pins HIGH/LOW
- Use multimeter to verify 3.3V when HIGH, 0V when LOW

**If this fails:**
- Pin damaged
- Wrong pin numbers
- Breadboard issue

**If this works:** → Continue to Test 3

---

### **Test 3: Direct Motor Control** (`test3_motor_direct.ino`)
**Purpose:** Test motor with simple ON/OFF (no PWM)

**What it does:**
- Runs motor at full power (digital HIGH)
- Bypasses PWM complexity

**Expected:**
- Motor should run STRONGLY
- No ramping, just ON/OFF

**If this fails:**
- Motor driver issue
- Power supply insufficient
- Motor connections loose
- Ground not connected

**If this works:** → Problem is PWM-related, go to Test 4

---

### **Test 4: Simple PWM** (`test4_pwm_simple.ino`)
**Purpose:** Test PWM functionality

**What it does:**
- Ramps motor from 0% to 100% smoothly
- Shows duty cycle values in Serial

**Expected:**
- Motor should smoothly increase speed
- You should see gradual ramp-up

**If this fails:**
- PWM channel conflict
- Wrong PWM frequency
- Motor driver doesn't support PWM on EN pin

**If this works:** → Problem is initialization, go to Test 5

---

### **Test 5: Robust Initialization** (`test5_robust_init.ino`)
**Purpose:** Test with careful initialization sequence

**What it does:**
- Cleans up PWM channels first
- Adds delays between steps
- Verifies each step
- Then runs beep pattern

**Expected:**
- Should work more reliably than simple test
- Prints each initialization step

**If this works better:** → Use this initialization sequence in your real code

**If still intermittent:** → Hardware problem (see below)

---

## Common Issues & Solutions

### **Works sometimes, fails after reconnecting:**
**Cause:** Loose breadboard connections
**Fix:** 
- Press down firmly on all connections
- Use fresh jumper wires
- Try different breadboard rows
- Solder connections instead

### **Works initially, stops after hours:**
**Cause:** Thermal shutdown or power issues
**Fix:**
- Add heatsink to motor driver
- Check if driver gets hot
- Use external 5V power (not USB)
- Add 1000µF capacitor across power

### **Serial prints but motor doesn't run:**
**Cause:** Power supply issue
**Fix:**
- Motor driver needs separate 5V supply
- Don't power motors from ESP32 pin
- USB can't provide enough current
- Check GND connection between ESP32 and driver

### **ESP32 resets when motor starts:**
**Cause:** Voltage drop from motor current
**Fix:**
- Add large capacitor (1000µF) on motor power
- Use separate power supply for motors
- ESP32 brown-out detector too sensitive

### **Motor runs but very weak:**
**Cause:** 
- Duty cycle too low
- Wrong pin connections (IN1/IN2 swapped)
- Motor driver in brake mode
- Insufficient voltage

**Fix:**
- Increase duty cycle to 100+ 
- Verify IN1=HIGH, IN2=LOW for forward
- Check motor driver enable logic
- Measure voltage at motor terminals

---

## Hardware Checklist

Before blaming software, check:

- [ ] All breadboard connections firmly pressed
- [ ] Motor driver has power LED on
- [ ] GND connected between ESP32 and motor driver
- [ ] Motor driver power supply adequate (5V, >1A)
- [ ] Jumper wires not loose or corroded
- [ ] Motor driver not overheating
- [ ] Motor spins freely by hand
- [ ] Correct motor polarity (reverse if needed)
- [ ] USB cable good quality (data + power)
- [ ] ESP32 not damaged (try different one)

---

## Using the Tests

1. **Upload test** via Arduino IDE
2. **Open Serial Monitor** (115200 baud)
3. **Observe behavior** and Serial output
4. **Compare** with expected results above
5. **Note which test fails** - that tells you where the problem is

---

## Test Results Interpretation

| Test 1 | Test 2 | Test 3 | Test 4 | Test 5 | Problem Area |
|--------|--------|--------|--------|--------|--------------|
| ❌ | - | - | - | - | ESP32/Upload |
| ✅ | ❌ | - | - | - | Pins damaged |
| ✅ | ✅ | ❌ | - | - | Power/Driver/Motor |
| ✅ | ✅ | ✅ | ❌ | - | PWM config |
| ✅ | ✅ | ✅ | ✅ | ❌ | Init sequence |
| ✅ | ✅ | ✅ | ✅ | Sometimes | Hardware intermittent |

---

## Need Help?

If all tests fail intermittently:
- **95% chance:** Loose connections or power issue
- **Try:** Different breadboard, fresh wires, external power
- **Last resort:** Solder everything, use different ESP32/driver

If specific test always fails:
- Look at that test's "If this fails" section
- Check hardware for that specific component
- Try swapping components
