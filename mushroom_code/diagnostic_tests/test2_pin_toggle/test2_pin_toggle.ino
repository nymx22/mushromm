#include <Arduino.h>

// Test 2: Pin Toggle Test
// Purpose: Verify motor control pins work
// Measure pins with multimeter - should see 3.3V toggling

const int TEST_PINS[] = {12, 14, 27}; // EN, IN1, IN2 for Motor 1
const char* PIN_NAMES[] = {"EN (12)", "IN1 (14)", "IN2 (27)"};
const int NUM_PINS = 3;

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  Serial.println("=== Pin Toggle Test ===");
  Serial.println("Testing motor control pins...");
  Serial.println("Measure each pin with multimeter:");
  Serial.println("  - Should see 3.3V when HIGH");
  Serial.println("  - Should see 0V when LOW");
  Serial.println();
  
  // Setup all pins as outputs
  for (int i = 0; i < NUM_PINS; i++) {
    pinMode(TEST_PINS[i], OUTPUT);
    digitalWrite(TEST_PINS[i], LOW);
    Serial.printf("Pin %s configured\n", PIN_NAMES[i]);
  }
  
  Serial.println("\nStarting toggle test...");
  Serial.println("Each pin will toggle HIGH/LOW every 2 seconds");
}

void loop() {
  // Test each pin individually - LONG delays for easy measurement
  for (int i = 0; i < NUM_PINS; i++) {
    Serial.println("\n========================================");
    Serial.printf(">>> %s -> HIGH <<<\n", PIN_NAMES[i]);
    Serial.println("Measure now! (10 seconds)");
    Serial.println("Should read: 3.3V");
    Serial.println("========================================");
    digitalWrite(TEST_PINS[i], HIGH);
    delay(10000); // 10 seconds - plenty of time!
    
    Serial.println("\n========================================");
    Serial.printf(">>> %s -> LOW <<<\n", PIN_NAMES[i]);
    Serial.println("Measure now! (10 seconds)");
    Serial.println("Should read: 0.0V");
    Serial.println("========================================");
    digitalWrite(TEST_PINS[i], LOW);
    delay(10000); // 10 seconds
  }
  
  Serial.println("\n\n--- Cycle complete, repeating... ---\n");
  delay(2000);
}
