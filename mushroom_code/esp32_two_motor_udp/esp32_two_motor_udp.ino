#include <Arduino.h>
#include <WiFi.h>
#include <WiFiUdp.h>

const char *WIFI_SSID = "Fat_Cat_Fablab";
const char *WIFI_PASS = "m4k3rb0t";
constexpr uint16_t LOCAL_PORT = 12345;
constexpr unsigned long SAFETY_TIMEOUT_MS = 500;

// Motor 0 and Motor 1 pin definitions
const int EN[2]  = {33, 12};   // Enable/PWM pins
const int IN1[2] = {25, 14};   // Direction control 1
const int IN2[2] = {26, 27};   // Direction control 2
const int CH[2]  = {0, 1};     // PWM channels

const uint32_t PWM_FREQ = 1000;
const uint8_t  PWM_RES  = 8;

WiFiUDP udp;
unsigned long lastUpdateMs[2] = {0, 0};  // Track last update for each motor
int currentDuty[2] = {0, 0};              // Current duty for each motor

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
  
  Serial.println("=== Two Motor UDP Control ===");
  
  // Setup motors
  for (int i = 0; i < 2; ++i) {
    pinMode(IN1[i], OUTPUT);
    pinMode(IN2[i], OUTPUT);
    motorDirForward(i);
  }
  
  for (int i = 0; i < 2; ++i) {
    ledcAttachChannel(EN[i], PWM_FREQ, PWM_RES, CH[i]);
    ledcWrite(EN[i], 0);
  }
  
  Serial.printf("Motor 0: EN=%d, IN1=%d, IN2=%d\n", EN[0], IN1[0], IN2[0]);
  Serial.printf("Motor 1: EN=%d, IN1=%d, IN2=%d\n", EN[1], IN1[1], IN2[1]);

  // Connect WiFi
  Serial.printf("Connecting to WiFi: %s\n", WIFI_SSID);
  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  
  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED && attempts < 40) {
    delay(500);
    Serial.print(".");
    attempts++;
    if (attempts % 10 == 0) {
      Serial.printf(" [%d/40]\n", attempts);
    }
  }
  
  if (WiFi.status() == WL_CONNECTED) {
    Serial.printf("\n✓ Connected! IP: %s\n", WiFi.localIP().toString().c_str());
  } else {
    Serial.println("\n✗ WiFi connection FAILED!");
    Serial.println("Check:");
    Serial.println("  1. WiFi name and password");
    Serial.println("  2. Network is 2.4GHz (ESP32 doesn't support 5GHz)");
    Serial.println("  3. Network is in range");
    Serial.println("\nContinuing without WiFi...");
  }
  
  // Start UDP
  if (WiFi.status() == WL_CONNECTED) {
    udp.begin(LOCAL_PORT);
    Serial.printf("✓ UDP listening on port %u\n", LOCAL_PORT);
    Serial.println("\n=== Commands ===");
    Serial.println("PING       - Test connection (responds with PONG)");
    Serial.println("STATUS     - Get motor status");
    Serial.println("M0:duty    - Set motor 0 (0-255)");
    Serial.println("M1:duty    - Set motor 1 (0-255)");
    Serial.println("Example: M0:45 sets motor 0 to duty 45");
    Serial.println("\n✓ Setup complete! Ready to receive commands.");
  } else {
    Serial.println("✗ Cannot start UDP without WiFi connection");
    Serial.println("Motors can still be tested manually in code");
  }
  
  Serial.println("\n================================");
}

void loop() {
  // Check WiFi connection
  if (WiFi.status() != WL_CONNECTED) {
    // No WiFi, just handle safety timeout
    for (int i = 0; i < 2; ++i) {
      if (millis() - lastUpdateMs[i] > SAFETY_TIMEOUT_MS && currentDuty[i] != 0) {
        motorOff(i);
        currentDuty[i] = 0;
      }
    }
    delay(100);
    return;
  }
  
  int packetSize = udp.parsePacket();
  if (packetSize > 0) {
    char buffer[32];
    int len = udp.read(buffer, sizeof(buffer) - 1);
    buffer[len] = '\0';
    
    IPAddress remoteIP = udp.remoteIP();
    uint16_t remotePort = udp.remotePort();
    
    // Handle PING command for connection testing
    if (strcmp(buffer, "PING") == 0) {
      Serial.printf("PING received from %s:%d\n", remoteIP.toString().c_str(), remotePort);
      
      // Send PONG response back
      const char* response = "PONG";
      udp.beginPacket(remoteIP, remotePort);
      udp.write((const uint8_t*)response, strlen(response));
      udp.endPacket();
      
      Serial.println("PONG sent back");
      return;
    }
    
    // Handle STATUS command
    if (strcmp(buffer, "STATUS") == 0) {
      Serial.printf("STATUS request from %s:%d\n", remoteIP.toString().c_str(), remotePort);
      
      char statusMsg[64];
      snprintf(statusMsg, sizeof(statusMsg), "M0:%d M1:%d OK", currentDuty[0], currentDuty[1]);
      udp.beginPacket(remoteIP, remotePort);
      udp.write((const uint8_t*)statusMsg, strlen(statusMsg));
      udp.endPacket();
      
      Serial.printf("Status sent: %s\n", statusMsg);
      return;
    }
    
    // Parse command format: "Mx:duty" where x is motor index (0 or 1)
    if (len >= 4 && buffer[0] == 'M' && buffer[2] == ':') {
      int motorIndex = buffer[1] - '0';  // Convert '0' or '1' to int
      
      if (motorIndex >= 0 && motorIndex < 2) {
        // Parse duty value
        int duty = 0;
        for (int i = 3; i < len; i++) {
          if (buffer[i] >= '0' && buffer[i] <= '9') {
            duty = duty * 10 + (buffer[i] - '0');
          }
        }
        duty = constrain(duty, 0, 255);
        
        // Apply to specified motor
        motorOn(motorIndex, duty);
        currentDuty[motorIndex] = duty;
        lastUpdateMs[motorIndex] = millis();
        
        Serial.printf("Motor %d set to duty %d from %s\n", motorIndex, duty, remoteIP.toString().c_str());
        
        // Send ACK response
        char ack[16];
        snprintf(ack, sizeof(ack), "ACK:M%d:%d", motorIndex, duty);
        udp.beginPacket(remoteIP, remotePort);
        udp.write((const uint8_t*)ack, strlen(ack));
        udp.endPacket();
      }
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
