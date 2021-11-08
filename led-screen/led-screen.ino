#include <LedControl.h>

int DIN = 12;
int CS =  11;
int CLK = 10;

int DIN2 = 9;
int CS2 =  8;
int CLK2 = 7;

/* number of 8x8 LED matrices */
int SegmentsWidth = 8;
int SegmentsHeight = 2;

unsigned long animationNextFrameMs = 0;

byte command_buffer[256];
int command_buffer_used = 0;
const byte command_op_magic = 0x0A; // line feed (\n) is start byte of every message
const byte command_op_ok = 0x4B; // 'K' success code returned
const byte command_op_set_banks = 0x42; // 'B' command SET BANKS
const byte command_op_set_frames = 0x46; // 'F' command SET FRAMES

LedControl lc = LedControl(DIN,CLK,CS,SegmentsWidth);
LedControl lc2 = LedControl(DIN2,CLK2,CS2,SegmentsWidth);

byte f[8] = {0x00,0x66,0xFF,0xFF,0x7E,0x3C,0x18,0x00,};
byte g[8] = {0xFF,0x99,0x00,0x00,0x81,0xC3,0xE7,0xFF,};
byte h[8] = {0x00,0x66,0xFF,0xFF,0x7E,0x3C,0x18,0x00,};
byte i[8] = {0xFF,0x99,0x00,0x00,0x81,0xC3,0xE7,0xFF,};

byte testScreen[128] = {
136, 4, 4, 4, 136, 208, 232, 232, 252, 3, 252, 255, 243, 97, 104, 240, 19, 12, 3, 15, 28, 184, 178, 112, 1, 98, 146, 106, 245, 244, 244, 245, 128, 76, 82, 109, 222, 95, 223, 223, 200, 48, 192, 240, 56, 29, 142, 14, 63, 192, 63, 255, 207, 134, 70, 15, 17, 32, 32, 32, 17, 11, 11, 23, 232, 232, 232, 208, 160, 64, 128, 0, 249, 15, 15, 159, 151, 14, 249, 6, 121, 127, 127, 191, 94, 47, 17, 14, 233, 209, 161, 64, 128, 0, 0, 0, 175, 151, 139, 5, 2, 1, 0, 0, 158, 254, 254, 253, 122, 228, 152, 96, 159, 240, 240, 249, 233, 240, 31, 224, 23, 23, 23, 11, 5, 2, 1, 0, 
};
byte testScreen2[128] = {
112, 136, 4, 4, 132, 200, 232, 232, 252, 3, 252, 255, 255, 127, 99, 253, 227, 28, 3, 15, 28, 56, 180, 112, 0, 1, 98, 146, 106, 245, 244, 245, 0, 128, 140, 146, 45, 94, 223, 223, 209, 32, 192, 240, 57, 29, 46, 14, 191, 64, 63, 255, 207, 134, 22, 15, 8, 16, 16, 16, 9, 11, 11, 23, 232, 232, 232, 208, 160, 64, 128, 0, 255, 15, 15, 159, 151, 14, 249, 6, 121, 127, 127, 191, 94, 47, 17, 14, 245, 233, 209, 160, 64, 128, 0, 0, 223, 175, 151, 11, 5, 2, 1, 0, 158, 254, 254, 125, 250, 228, 152, 96, 144, 240, 249, 233, 240, 255, 31, 224, 23, 23, 23, 11, 5, 2, 1, 0, 
};


void setup(){
  for(int index=0;index<lc.getDeviceCount();index++) {
        lc.shutdown(index,false);       //The MAX72XX is in power-saving mode on startup
        lc.setIntensity(index,1);      // Set the brightness to maximum value
        lc.clearDisplay(index);    
        lc2.shutdown(index,false);       //The MAX72XX is in power-saving mode on startup
        lc2.setIntensity(index,1);      // Set the brightness to maximum value
        lc2.clearDisplay(index);     
  }
}


void loop(){ 
  unsigned long currentMillis = millis();

  if(animationNextFrameMs <= currentMillis) {
    processNextFrame();
  }
  
    
  /* 
   for(int index=0;index<lc.getDeviceCount();index++) {
    lc.clearDisplay(index);
   }
    
    delay(500);
    */
}


void serialEvent() {
  while (Serial.available()) {
    // get the new byte:
    char inChar = (byte)Serial.read();
    // add it to the inputString:
    inputString += inChar;
    // if the incoming character is a newline, set a flag so the main loop can
    // do something about it:
    if (inChar == '\n') {
      stringComplete = true;
    }
  }
}

void processNextFrame() {
  printBuffer(testScreen);
  animationNextFrameMs = millis() + 500;
}

void printBuffer(byte buffer [])
{
  int segmentOffset = 0;
  int row = 0;
  for(int seg=0;seg<SegmentsWidth;seg++)
  {
    for(row=0;row<8;row++)
    {
      lc.setRow(seg,7-row,buffer[segmentOffset]);
      lc2.setRow(seg,7-row,buffer[segmentOffset + 64]);
      segmentOffset++;
    }
  }
}


void printByte(byte character [], bool off)
{
  int i = 0;
  for(i=0;i<8;i++)
  {
    for(int index=0;index<lc.getDeviceCount();index++) {
      if(index % 2 == (off ? 0 : 1)) {
        lc.setRow(index,7-i,character[i]);
        if(i==0) {
          lc2.clearDisplay(index);
        }
      }
      else {
          lc2.setRow(index,7-i,character[i]);
            
          if(i==0) {
            lc.clearDisplay(index);
          }
        }
      }
    }
}

   
  
