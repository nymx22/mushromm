#include <Arduino.h>

// Test 4: Simple PWM Test
// Purpose: Test PWM functionality on EN pin
// Uses proper PWM channel initialization

const int EN_PIN = 12;
const int IN1_PIN = 14;
const int IN2_PIN = 27;
const int PWM_CHANNEL = 1; // Motor 1 uses channel 1

const uint32_t PWM_FREQ = 1000;
const uint8_t PWM_RES = 8;

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  Serial.println("=== Simple PWM Test ===");
  Serial.println("Motor 1: EN=12, IN1=14, IN2=27, Channel=1");
  Serial.println();
  
  // Setup direction pins
  pinMode(IN1_PIN, OUTPUT);
  pinMode(IN2_PIN, OUTPUT);
  digitalWrite(IN1_PIN, HIGH); // Forward
  digitalWrite(IN2_PIN, LOW);
  
  Serial.println("✓ Direction pins set (forward)");
  
  // Setup PWM with clean initialization
  ledcAttachChannel(EN_PIN, PWM_FREQ, PWM_RES, PWM_CHANNEL);
  ledcWrite(EN_PIN, 0);
  
  Serial.println("✓ PWM configured");
  Serial.printf("  Frequency: %d Hz\n", PWM_FREQ);
  Serial.printf("  Resolution: %d bits (0-255)\n", PWM_RES);
  Serial.printf("  Channel: %d\n", PWM_CHANNEL);
  Serial.println();
  
  Serial.println("Test sequence:");
  Serial.println("- Ramp from 0 to 255 (0% to 100%)");
  Serial.println("- Hold at full power");
  Serial.println("- Ramp down to 0");
  Serial.println("- Repeat...");
  Serial.println();
  
  delay(2000);
}

void loop() {
  // Ramp up
  Serial.println("Ramping UP (0 -> 255)");
  for (int duty = 0; duty <= 255; duty += 5) {
    ledcWrite(EN_PIN, duty);
    Serial.printf("Duty: %d\n", duty);
    delay(100);
  }
  
  // Hold at full
  Serial.println("Holding at FULL POWER (255)");
  ledcWrite(EN_PIN, 255);
  delay(2000);
  
  // Ramp down
  Serial.println("Ramping DOWN (255 -> 0)");
  for (int duty = 255; duty >= 0; duty -= 5) {
    ledcWrite(EN_PIN, duty);
    Serial.printf("Duty: %d\n", duty);
    delay(100);
  }
  
  // Off
  Serial.println("Motor OFF");
  ledcWrite(EN_PIN, 0);
  delay(2000);
  
  Serial.println("\n--- Cycle complete, repeating... ---\n");
  delay(1000);
}
