   #include <Arduino.h>
   void setup() {
     ledcSetup(0, 5000, 8);
     ledcAttachPin(2, 0);
     ledcWrite(0, 128);
   }
   void loop() {}