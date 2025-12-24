#include <Arduino.h>
// Single motor pin configuration (matching your working code structure)
const int EN_PIN = 12;   // Enable/PWM pin
const int IN1_PIN = 14;    // Direction control 1
const int IN2_PIN = 27;    // Direction control 2
const int PWM_CHANNEL = 3; // PWM channel

const uint32_t PWM_FREQ = 1000;  // Match your working code
const uint8_t PWM_RES = 8;       // 8-bit resolution (0-255)
const uint8_t VIBE_DUTY = 50;    // Match your working code (you can adjust this)

static inline void motorDirForward() {
  digitalWrite(IN1_PIN, HIGH);
  digitalWrite(IN2_PIN, LOW);
}

static inline void motorStop() {
  ledcWrite(EN_PIN, 0);
}

static inline void motorOn(uint8_t duty) {
  motorDirForward();
  ledcWrite(EN_PIN, duty);
}

void setup() {
  Serial.begin(115200);
  delay(1000);


  
  // Setup direction pins
  pinMode(IN1_PIN, OUTPUT);
  pinMode(IN2_PIN, OUTPUT);
  motorDirForward();
  
  // Setup PWM channel (exactly like your working code)
  ledcAttachChannel(EN_PIN, PWM_FREQ, PWM_RES, PWM_CHANNEL);
  ledcWrite(EN_PIN, 0);
  
  Serial.println("Motor setup complete. Starting beep pattern...");
}

void loop() {
  // Beep pattern: on for 100ms, off for 500ms
  motorOn(VIBE_DUTY);
  delay(100);
  motorStop();
  delay(500);
}
