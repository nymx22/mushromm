#include <Arduino.h>

// Test 5: Robust Initialization
// Purpose: Test with extra initialization steps to prevent intermittent issues
// Includes: cleanup, delays, verification

const int EN_PIN = 12;
const int IN1_PIN = 14;
const int IN2_PIN = 27;
const int PWM_CHANNEL = 1;

const uint32_t PWM_FREQ = 1000;
const uint8_t PWM_RES = 8;
const uint8_t VIBE_DUTY = 100;

void setup() {
  Serial.begin(115200);
  delay(2000); // Longer initial delay
  
  Serial.println("\n\n=== Robust Initialization Test ===");
  Serial.println("This test includes extra initialization steps");
  Serial.println("to prevent intermittent issues.\n");
  
  // Step 1: Initialize pins as outputs
  Serial.println("Step 1: Configuring pins...");
  pinMode(IN1_PIN, OUTPUT);
  pinMode(IN2_PIN, OUTPUT);
  pinMode(EN_PIN, OUTPUT);
  
  // Step 2: Set all pins LOW initially
  Serial.println("Step 2: Setting all pins LOW...");
  digitalWrite(IN1_PIN, LOW);
  digitalWrite(IN2_PIN, LOW);
  digitalWrite(EN_PIN, LOW);
  delay(500);
  
  // Step 3: Set direction
  Serial.println("Step 3: Setting direction (forward)...");
  digitalWrite(IN1_PIN, HIGH);
  digitalWrite(IN2_PIN, LOW);
  delay(100);
  
  // Step 4: Cleanup any existing PWM
  Serial.println("Step 4: Cleaning up PWM channels...");
  ledcWrite(EN_PIN, 0);
  delay(100);
  
  // Step 5: Attach PWM channel
  Serial.println("Step 5: Attaching PWM channel...");
  ledcAttachChannel(EN_PIN, PWM_FREQ, PWM_RES, PWM_CHANNEL);
  ledcWrite(EN_PIN, 0);
  delay(500);
  
  // Step 6: Test motor briefly
  Serial.println("Step 6: Quick motor test (1 second)...");
  ledcWrite(EN_PIN, VIBE_DUTY);
  delay(1000);
  ledcWrite(EN_PIN, 0);
  delay(500);
  
  Serial.println("\n✓ Initialization complete!");
  Serial.println("Starting beep pattern...\n");
}

void loop() {
  // Simple beep pattern
  ledcWrite(EN_PIN, VIBE_DUTY);
  Serial.println("BEEP");
  delay(100);
  
  ledcWrite(EN_PIN, 0);
  delay(500);
}
