#include <Arduino.h>

// ====== 6×电机（3×L298N，每块2路）顺序触发示例 ======
const int EN[6]  = {33, 12, 21, 32,  4,  5};
const int IN1[6] = {25, 14, 22, 34, 18, 19};
const int IN2[6] = {26, 27, 23, 35,  2, 15};

const int CH[6] = {0, 1, 2, 3, 4, 5};

const uint32_t PWM_FREQ = 25000; 
const uint8_t  PWM_RES  = 8;    
const uint8_t  VIBE_DUTY = 25;  // 10% 力度 ≈ 255*0.1

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
  for (int i = 0; i < 6; ++i) {
    pinMode(IN1[i], OUTPUT);
    pinMode(IN2[i], OUTPUT);
    motorDirForward(i);
  }
  for (int i = 0; i < 6; ++i) {
    ledcAttachChannel(EN[i], PWM_FREQ, PWM_RES, CH[i]);
    ledcWrite(EN[i], 0);
  }
}

void loop() {
  for (int i = 0; i < 6; ++i) {
    motorOn(i, VIBE_DUTY);
    delay(100);       
    motorStop(i);
    delay(3000);      
  }
}