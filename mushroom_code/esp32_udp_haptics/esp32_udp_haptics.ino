#include <Arduino.h>
#include <WiFi.h>
#include <WiFiUdp.h>

const char *WIFI_SSID = "Laincel";
const char *WIFI_PASS = "kickkami";
constexpr uint16_t LOCAL_PORT = 12345;
constexpr unsigned long SAFETY_TIMEOUT_MS = 500;

const int EN[6]  = {33, 12, 21, 32,  4,  5};
const int IN1[6] = {25, 14, 22, 34, 18, 19};
const int IN2[6] = {26, 27, 23, 35,  2, 15};
const int CH[6]  = {0, 1, 2, 3, 4, 5};

const uint32_t PWM_FREQ = 1000;  // Match your preference
const uint8_t  PWM_RES  = 8;      // keep 8-bit resolution
const uint8_t  VIBE_DUTY = 26;   // adjust for desired strength

WiFiUDP udp;
unsigned long lastUpdateMs = 0;
int currentDuty = 0;

static inline void motorDirForward(int i) {
  digitalWrite(IN1[i], HIGH);
  digitalWrite(IN2[i], LOW);
}

static inline void motorOn(int i, uint8_t duty) {
  motorDirForward(i);
  ledcWrite(CH[i], duty);
}

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  // Setup motors first
  for (int i = 0; i < 6; ++i) {
    pinMode(IN1[i], OUTPUT);
    pinMode(IN2[i], OUTPUT);
    motorDirForward(i);
  }
  for (int i = 0; i < 6; ++i) {
    ledcAttachChannel(EN[i], PWM_FREQ, PWM_RES, CH[i]);
    ledcWrite(CH[i], 0);
  }

  // Connect WiFi
  Serial.printf("Connecting to %s...\n", WIFI_SSID);
  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.printf("\nConnected! IP: %s\n", WiFi.localIP().toString().c_str());
  
  // Start UDP
  udp.begin(LOCAL_PORT);
  Serial.printf("UDP listening on port %u\n", LOCAL_PORT);
}

void loop() {
  int packetSize = udp.parsePacket();
  if (packetSize > 0) {
    char buffer[32];
    int len = udp.read(buffer, sizeof(buffer) - 1);
    buffer[len] = '\0';
    
    int duty = 0;
    for (int i = 0; i < len; i++) {
      if (buffer[i] >= '0' && buffer[i] <= '9') {
        duty = duty * 10 + (buffer[i] - '0');
      }
    }
    duty = constrain(duty, 0, 255);
    
    // Apply to all motors
    for (int i = 0; i < 6; ++i) {
      motorOn(i, duty);
    }
    currentDuty = duty;
    lastUpdateMs = millis();
  }
  
  // Safety timeout
  if (millis() - lastUpdateMs > SAFETY_TIMEOUT_MS && currentDuty != 0) {
    for (int i = 0; i < 6; ++i) {
      ledcWrite(CH[i], 0);
    }
    currentDuty = 0;
  }
}
