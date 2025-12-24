#include <Arduino.h>

// Test 1: Simple LED Blink
// Purpose: Verify ESP32 is working and code uploads correctly
// If this doesn't work, problem is ESP32/power/upload issue

const int LED_PIN = 2; // Built-in LED on most ESP32 boards

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  pinMode(LED_PIN, OUTPUT);
  
  Serial.println("=== LED Blink Test ===");
  Serial.println("Built-in LED should blink every second");
  Serial.println("If LED blinks but Serial prints don't show:");
  Serial.println("  - Wrong baud rate (should be 115200)");
  Serial.println("If nothing happens:");
  Serial.println("  - ESP32 not powered properly");
  Serial.println("  - Code didn't upload");
  Serial.println("  - Wrong board selected");
}

void loop() {
  digitalWrite(LED_PIN, HIGH);
  Serial.println("LED ON");
  delay(1000);
  
  digitalWrite(LED_PIN, LOW);
  Serial.println("LED OFF");
  delay(1000);
}
