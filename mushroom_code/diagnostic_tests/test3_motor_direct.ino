#include <Arduino.h>

// Test 3: Direct Motor Control (No PWM)
// Purpose: Test motor with simple HIGH/LOW (full power)
// Bypasses PWM complexity to isolate issues

const int EN_PIN = 12;
const int IN1_PIN = 14;
const int IN2_PIN = 27;

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  Serial.println("=== Direct Motor Control Test ===");
  Serial.println("Motor 1: EN=12, IN1=14, IN2=27");
  Serial.println();
  
  // Setup pins
  pinMode(EN_PIN, OUTPUT);
  pinMode(IN1_PIN, OUTPUT);
  pinMode(IN2_PIN, OUTPUT);
  
  // Initialize all LOW
  digitalWrite(EN_PIN, LOW);
  digitalWrite(IN1_PIN, LOW);
  digitalWrite(IN2_PIN, LOW);
  
  Serial.println("✓ Pins initialized");
  Serial.println("\nTest sequence:");
  Serial.println("1. Motor OFF (2 sec)");
  Serial.println("2. Motor FORWARD FULL POWER (2 sec)");
  Serial.println("3. Motor OFF (2 sec)");
  Serial.println("4. Repeat...");
  Serial.println();
  
  delay(2000);
}

void loop() {
  // Motor OFF
  Serial.println("Motor OFF");
  digitalWrite(EN_PIN, LOW);
  digitalWrite(IN1_PIN, LOW);
  digitalWrite(IN2_PIN, LOW);
  delay(2000);
  
  // Motor FORWARD at FULL POWER (no PWM)
  Serial.println("Motor FORWARD (FULL POWER)");
  digitalWrite(IN1_PIN, HIGH);  // Forward direction
  digitalWrite(IN2_PIN, LOW);
  digitalWrite(EN_PIN, HIGH);   // Full power (no PWM)
  delay(2000);
  
  Serial.println();
}
