#include <Arduino.h>

// Motor pin definitions (matching esp32_udp_haptics.ino)
const int EN[6]  = {33, 12, 21, 32,  4,  5};
const int IN1[6] = {25, 14, 22, 34, 18, 19};
const int IN2[6] = {26, 27, 23, 35,  2, 15};
const int CH[6]  = {0, 1, 2, 3, 4, 5};

const uint32_t PWM_FREQ = 1000;
const uint8_t  PWM_RES  = 8;
const uint8_t  BOOST_DUTY = 128;  // Start with 50% duty (128/255)
const unsigned long VIBRATION_ON_MS = 100;
const unsigned long VIBRATION_OFF_MS = 50;

static inline void motorDirForward(int i) {
  digitalWrite(IN1[i], HIGH);
  digitalWrite(IN2[i], LOW);
}

static inline void motorOn(int i, uint8_t duty) {
  motorDirForward(i);
  ledcWrite(CH[i], duty);
}

static inline void motorOff(int i) {
  ledcWrite(CH[i], 0);
}

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  Serial.println("Vibrating Motors 0 and 1");
  
  // Setup motors first (exactly like working UDP code)
  for (int i = 0; i < 6; ++i) {
    pinMode(IN1[i], OUTPUT);
    pinMode(IN2[i], OUTPUT);
    motorDirForward(i);
  }
  for (int i = 0; i < 6; ++i) {
    ledcAttachChannel(EN[i], PWM_FREQ, PWM_RES, CH[i]);
    ledcWrite(CH[i], 0);
  }
  
  Serial.println("Starting vibration...");
}

void loop() {
  // Turn motors 0 and 1 on
  motorOn(0, BOOST_DUTY);
  motorOn(1, BOOST_DUTY);
  delay(VIBRATION_ON_MS);
  
  // Turn motors 0 and 1 off
  motorOff(0);
  motorOff(1);
  delay(VIBRATION_OFF_MS);
}
