import processing.video.*;
import java.util.*;

String VIDEO_FILE = "../scene2.mp4";
String TIMELINE_CSV = "../timeline.csv";

// Toggle later to use Arduino serial (after mock works)
boolean USE_SERIAL = false;     // <-- set true when you're ready
// import processing.serial.*;
// Serial sp;
// int BAUD = 115200;
// String PORT_HINT = "";          // e.g. "usbmodem" or "COM3"

// VR Control settings
boolean USE_VR_CONTROL = true;  // Enable VR control mode
String VR_COMMAND_PREFIX = "VR:";  // Prefix for VR commands

// 360 Video First-Person Experience
float cameraYaw = 0;     // Left/Right rotation
float cameraPitch = 0;   // Up/Down rotation
boolean isDragging = false;
int lastMouseX, lastMouseY;

Movie movie;

class Event {
  long t; String cmd,a1,a2,a3; long lead; String note; boolean fired=false;
}
ArrayList<Event> evts = new ArrayList<Event>();
int nextIdx = 0;
long manualOffsetMs = 0;

boolean vLight = false;   // mock "device" state
float   vMotor = 0;       // 0..255 mock PWM

void settings(){ size(1280,720, P3D); }

void setup(){
  surface.setTitle("Video → Mock Control");
  if (USE_SERIAL) connectSerial();

  loadTimeline(TIMELINE_CSV);

  movie = new Movie(this, VIDEO_FILE);
  movie.loop(); // continuous; you can use .play() if you prefer one-shot
}

void connectSerial(){
  // Serial functionality commented out for now
  println("Serial functionality disabled - running in mock mode");
  USE_SERIAL = false;
}

void loadTimeline(String path){
  String[] lines = loadStrings(path);
  if(lines==null){ println("Missing timeline.csv"); exit(); }
  for(String raw: lines){
    String s=raw.trim(); if(s.length()==0 || s.startsWith("#")) continue;
    String[] c = splitTokens(s,",");
    Event e = new Event();
    e.t = (long)int(c[0]); e.cmd=c[1].trim();
    e.a1 = c.length>2?c[2].trim():"-";
    e.a2 = c.length>3?c[3].trim():"-";
    e.a3 = c.length>4?c[4].trim():"-";
    e.lead = c.length>5?(long)int(c[5]):0;
    e.note = c.length>6?c[6].trim():"";
    evts.add(e);
  }
  evts.sort((x,y)->Long.compare(x.t,y.t));
  println("Loaded events: "+evts.size());
}

void movieEvent(Movie m){ m.read(); }

void draw(){
  background(0);
  
  if(movie!=null) {
    // Draw 360 video as a sphere with you inside
    draw360VideoSphere();
  }

  long tNow = (long)(movie.time()*1000.0) + manualOffsetMs;
  fireDue(tNow);

  // Simple HUD + mock "actuators" (drawn in 2D overlay)
  drawHUD(tNow);
  drawMockDevices();
}

void draw360VideoSphere() {
  // Simple approach: display 360 video as a large flat image
  // This gives you the 360 video experience without complex 3D sphere
  
  pushMatrix();
  translate(width/2, height/2);
  
  // Scale the video to fill the screen
  float scale = 2.0; // Make it larger
  scale(scale);
  
  // Calculate the portion of the 360 video to show based on camera rotation
  float videoAspect = (float)movie.width / (float)movie.height;
  float displayWidth = width;
  float displayHeight = height;
  
  // Pan: move left/right in the 360 video
  float panOffset = (cameraYaw / TWO_PI) * movie.width;
  float sourceX = panOffset - displayWidth/2;
  
  // Tilt: move up/down in the 360 video  
  float tiltOffset = (cameraPitch / PI) * movie.height;
  float sourceY = tiltOffset - displayHeight/2;
  
  // Keep source coordinates within video bounds
  sourceX = constrain(sourceX, 0, movie.width - displayWidth);
  sourceY = constrain(sourceY, 0, movie.height - displayHeight);
  
  // Draw the cropped portion of the video
  image(movie, -displayWidth/2, -displayHeight/2, displayWidth, displayHeight, 
        (int)sourceX, (int)sourceY, (int)(sourceX + displayWidth), (int)(sourceY + displayHeight));
  
  popMatrix();
}

void fireDue(long tNow){
  while(nextIdx<evts.size()){
    Event e = evts.get(nextIdx);
    long fireAt = e.t - e.lead;
    if(!e.fired && tNow >= fireAt){
      handleEvent(e);
      e.fired = true;
      nextIdx++;
    } else if (tNow < fireAt) break;
    else nextIdx++;
  }
}

void handleEvent(Event e){
  String line;
  switch(e.cmd){
    case "RESET":
      vLight = false; vMotor = 0;
      line = "RESET";
      break;
    case "SET":
      if ("virtualLight".equals(e.a1)) vLight = "1".equals(e.a2);
      line = "SET,"+e.a1+","+e.a2;
      break;
    case "PWM":
      if ("virtualMotor".equals(e.a1)) vMotor = constrain(parseFloat(e.a2),0,255);
      line = "PWM,"+e.a1+","+e.a2+","+ (e.a3.equals("-")?"0":e.a3);
      break;
    case "VR_EFFECT":
      // Handle VR-specific effects
      line = "VR_EFFECT,"+e.a1+","+e.a2+","+e.a3;
      break;
    default:
      line = "UNKNOWN";
  }
  
  // Output for VR control
  String vrCommand = VR_COMMAND_PREFIX + line;
  println("[" + e.t + "ms " + e.cmd + (e.note.length()>0?(" \""+e.note+"\""):"")+ "] -> " + line);
  println("VR Command: " + vrCommand);
  
  // Send to VR system (you'll need to implement this based on your VR setup)
  if(USE_VR_CONTROL) {
    sendVRCommand(vrCommand);
  }
  
  // if(USE_SERIAL && sp!=null) sp.write(line+"\n");
}

void keyPressed(){
  if(key==' ') { if(movie.isPlaying()) movie.pause(); else movie.play(); }
  if(key=='[') manualOffsetMs -= 10;
  if(key==']') manualOffsetMs += 10;
  if(key=='r'||key=='R'){ for(Event e:evts) e.fired=false; nextIdx=0; movie.jump(0); }
  
  // 360 Video First-Person Controls
  if(key=='w' || key=='W') cameraPitch -= 0.1;  // Look up
  if(key=='s' || key=='S') cameraPitch += 0.1;  // Look down
  if(key=='a' || key=='A') cameraYaw -= 0.1;    // Look left
  if(key=='d' || key=='D') cameraYaw += 0.1;    // Look right
  if(key=='z' || key=='Z') { cameraYaw = 0; cameraPitch = 0; } // Reset view
}

void mousePressed() {
  isDragging = true;
  lastMouseX = mouseX;
  lastMouseY = mouseY;
}

void mouseDragged() {
  if (isDragging) {
    // First-person look control with mouse
    float deltaX = mouseX - lastMouseX;
    float deltaY = mouseY - lastMouseY;
    
    cameraYaw += deltaX * 0.01;
    cameraPitch += deltaY * 0.01;
    
    // Constrain pitch to avoid flipping
    cameraPitch = constrain(cameraPitch, -PI/2, PI/2);
    
    lastMouseX = mouseX;
    lastMouseY = mouseY;
  }
}

void mouseReleased() {
  isDragging = false;
}

void drawHUD(long tNow){
  // Draw HUD in 2D overlay mode
  hint(DISABLE_DEPTH_TEST);
  fill(0,160); rect(0,0,width,100);
  fill(255);
  text("t="+tNow+"ms  video="+nf(movie.time(),1,3)+"s  events="+countFired()+"/"+evts.size()+"  offset="+manualOffsetMs+"ms",12,20);
  text("Space: play/pause  R: restart  [ / ]: time nudge  (Serial "+(USE_SERIAL?"ON":"OFF")+")",12,40);
  text("VR Control: "+(USE_VR_CONTROL?"ON":"OFF")+"  |  360 Video First-Person Experience",12,60);
  text("360 Controls: WASD=look around  Mouse=drag to look  Z=reset view",12,80);
  hint(ENABLE_DEPTH_TEST);
}

int countFired(){ int n=0; for(Event e:evts) if(e.fired) n++; return n; }

void sendVRCommand(String command) {
  // This function will send commands to your VR system
  // Implementation depends on your VR setup:
  
  // Option 1: OSC (Open Sound Control) - works with many VR apps
  // Option 2: WebSocket - for web-based VR
  // Option 3: UDP/TCP - for custom VR applications
  // Option 4: File output - for simple integration
  
  // For now, we'll use file output as a simple method
  String[] lines = {command, str(millis())};
  saveStrings("vr_commands.txt", lines);
  
  // You can also print to console for debugging
  println("VR Command sent: " + command);
}

void drawMockDevices(){
  // Draw mock devices in 2D overlay mode
  hint(DISABLE_DEPTH_TEST);
  
  // Virtual light: big translucent overlay when ON
  if(vLight){
    noStroke(); fill(255,255,255,60); rect(0,0,width,height);
  }
  
  // Virtual motor: bar meter
  noFill(); stroke(255); rect(width-40,20,20,200);
  noStroke(); fill(255); float h = map(vMotor,0,255,0,200);
  rect(width-40, 220-h, 20, h);
  
  hint(ENABLE_DEPTH_TEST);
}
