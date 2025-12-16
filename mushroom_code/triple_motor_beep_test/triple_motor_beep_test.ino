#include <Arduino.h>

// Triple motor pin configuration (motors 0, 1, and 2)
const int EN[3]  = {33, 12, 21};   // Enable/PWM pins
const int IN1[3] = {25, 14, 22};   // Direction control 1
const int IN2[3] = {26, 27, 23};  // Direction control 2
const int PWM_CHANNEL[3] = {0, 1, 2}; // PWM channels

const uint32_t PWM_FREQ = 1000;  // Match your working code
const uint8_t PWM_RES = 8;       // 8-bit resolution (0-255)
const uint8_t VIBE_DUTY = 50;    // Adjust for desired strength

static inline void motorDirForward(int i) {
  digitalWrite(IN1[i], HIGH);
  digitalWrite(IN2[i], LOW);
}

static inline void motorStop(int i) {
  ledcWrite(EN[i], 0);
}

static inline void motorOn(int i, uint8_t duty) {
  motorDirForward(i);
  ledcWrite(EN[i], duty);
}

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  // Setup direction pins for all three motors
  for (int i = 0; i < 3; ++i) {
    pinMode(IN1[i], OUTPUT);
    pinMode(IN2[i], OUTPUT);
    motorDirForward(i);
  }
  
  // Setup PWM channels for all three motors
  for (int i = 0; i < 3; ++i) {
    ledcAttachChannel(EN[i], PWM_FREQ, PWM_RES, PWM_CHANNEL[i]);
    ledcWrite(EN[i], 0);
  }
  
  Serial.println("Triple motor setup complete. Starting beep pattern...");
  Serial.printf("Motor 0: EN=%d, IN1=%d, IN2=%d\n", EN[0], IN1[0], IN2[0]);
  Serial.printf("Motor 1: EN=%d, IN1=%d, IN2=%d\n", EN[1], IN1[1], IN2[1]);
  Serial.printf("Motor 2: EN=%d, IN1=%d, IN2=%d\n", EN[2], IN1[2], IN2[2]);
}

void loop() {
  // Beep pattern: all three motors on for 100ms, off for 500ms
  motorOn(0, VIBE_DUTY);
  motorOn(1, VIBE_DUTY);
  motorOn(2, VIBE_DUTY);
  delay(100);
  
  motorStop(0);
  motorStop(1);
  motorStop(2);
  delay(500);
}
