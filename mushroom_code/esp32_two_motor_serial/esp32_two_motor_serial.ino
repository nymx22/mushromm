#include <Arduino.h>

// Motor 0 and Motor 1 pin definitions
const int EN[2]  = {33, 12};   // Enable/PWM pins
const int IN1[2] = {25, 14};   // Direction control 1
const int IN2[2] = {26, 27};   // Direction control 2

const uint32_t PWM_FREQ = 1000;
const uint8_t  PWM_RES  = 8;

unsigned long lastUpdateMs[2] = {0, 0};  // Track last update for each motor
int currentDuty[2] = {0, 0};              // Current duty for each motor
constexpr unsigned long SAFETY_TIMEOUT_MS = 500;

String inputBuffer = "";

static inline void motorDirForward(int i) {
  digitalWrite(IN1[i], HIGH);
  digitalWrite(IN2[i], LOW);
}

static inline void motorOn(int i, uint8_t duty) {
  motorDirForward(i);
  ledcWrite(EN[i], duty);
}

static inline void motorOff(int i) {
  ledcWrite(EN[i], 0);
}

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  Serial.println("=== Two Motor USB Serial Control ===");
  
  // Setup motors
  for (int i = 0; i < 2; ++i) {
    pinMode(IN1[i], OUTPUT);
    pinMode(IN2[i], OUTPUT);
    motorDirForward(i);
  }
  
  for (int i = 0; i < 2; ++i) {
    ledcAttachChannel(EN[i], PWM_FREQ, PWM_RES, i);
    ledcWrite(EN[i], 0);
  }
  
  Serial.printf("Motor 0: EN=%d, IN1=%d, IN2=%d\n", EN[0], IN1[0], IN2[0]);
  Serial.printf("Motor 1: EN=%d, IN1=%d, IN2=%d\n", EN[1], IN1[1], IN2[1]);
  
  Serial.println("\n=== Commands ===");
  Serial.println("PING       - Test connection");
  Serial.println("STATUS     - Get motor status");
  Serial.println("M0:duty    - Set motor 0 (0-255)");
  Serial.println("M1:duty    - Set motor 1 (0-255)");
  Serial.println("Example: M0:45");
  Serial.println("\n✓ Ready for commands");
  Serial.println("================================");
}

void loop() {
  // Read serial commands
  while (Serial.available()) {
    char c = Serial.read();
    
    if (c == '\n' || c == '\r') {
      if (inputBuffer.length() > 0) {
        processCommand(inputBuffer);
        inputBuffer = "";
      }
    } else {
      inputBuffer += c;
    }
  }
  
  // Safety timeout for each motor
  for (int i = 0; i < 2; ++i) {
    if (millis() - lastUpdateMs[i] > SAFETY_TIMEOUT_MS && currentDuty[i] != 0) {
      motorOff(i);
      currentDuty[i] = 0;
      Serial.printf("Motor %d safety timeout\n", i);
    }
  }
}

void processCommand(String command) {
  command.trim();
  
  // Handle PING command
  if (command == "PING") {
    Serial.println("PONG");
    return;
  }
  
  // Handle STATUS command
  if (command == "STATUS") {
    Serial.printf("M0:%d M1:%d OK\n", currentDuty[0], currentDuty[1]);
    return;
  }
  
  // Parse motor command format: "Mx:duty"
  if (command.length() >= 4 && command.charAt(0) == 'M' && command.charAt(2) == ':') {
    int motorIndex = command.charAt(1) - '0';
    
    if (motorIndex >= 0 && motorIndex < 2) {
      // Parse duty value
      int colonIndex = command.indexOf(':');
      String dutyStr = command.substring(colonIndex + 1);
      int duty = dutyStr.toInt();
      duty = constrain(duty, 0, 255);
      
      // Apply to specified motor
      motorOn(motorIndex, duty);
      currentDuty[motorIndex] = duty;
      lastUpdateMs[motorIndex] = millis();
      
      // Send ACK
      Serial.printf("ACK:M%d:%d\n", motorIndex, duty);
    } else {
      Serial.println("ERROR: Invalid motor index");
    }
  } else {
    Serial.println("ERROR: Unknown command");
  }
}
