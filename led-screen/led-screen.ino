#include <LedControl.h>

int DIN = 12;
int CS =  11;
int CLK = 10;

int DIN2 = 9;
int CS2 =  8;
int CLK2 = 7;

/* number of 8x8 LED matrices */
const int segments = 16;

const int segmentsWidth = 8;
const int segmentsHeight = 2;

/* COMMANDS */
byte command_buffer[256];
int command_buffer_used = 0;
bool command_receive_start = false;
bool command_receive_op_known = false;
byte command_receive_op = 0;
bool command_receive_length_known = false;
byte command_receive_length = 0;
const byte command_op_start = 0x0A; // line feed (\n) is start byte of every message
const byte command_op_ok = 0x4B; // 'K' success code returned
const byte command_op_err = 0x45; // 'E' error code returned
const byte command_op_set_banks = 0x42; // 'B' command SET BANKS
const byte command_op_set_frames = 0x46; // 'F' command SET FRAMES

/* BANKS */
const int banks_count = 64;
byte banks_data[banks_count * 8];

/* FRAMES */
const int frames_capacity = 16;
byte frames_data[frames_capacity * segments];
int frames_delay_ms[frames_capacity];
int frames_count;
int frames_current = 0;
unsigned long frames_next_ms = 0;
bool frames_next_enabled = false;

LedControl lc = LedControl(DIN, CLK, CS, segmentsWidth);
LedControl lc2 = LedControl(DIN2, CLK2, CS2, segmentsWidth);

byte f[8] = {0x00, 0x66, 0xFF, 0xFF, 0x7E, 0x3C, 0x18, 0x00,};
byte g[8] = {0xFF, 0x99, 0x00, 0x00, 0x81, 0xC3, 0xE7, 0xFF,};
byte h[8] = {0x00, 0x66, 0xFF, 0xFF, 0x7E, 0x3C, 0x18, 0x00,};
byte i[8] = {0xFF, 0x99, 0x00, 0x00, 0x81, 0xC3, 0xE7, 0xFF,};

void setup() {
  Serial.begin(9600);
  initAndClearScreen(true /* do init */);
  //sampleDataInit();
}

/*
byte sample1[128] = {
  136, 4, 4, 4, 136, 208, 232, 232, 252, 3, 252, 255, 243, 97, 104, 240, 19, 12, 3, 15, 28, 184, 178, 112, 1, 98, 146, 106, 245, 244, 244, 245, 128, 76, 82, 109, 222, 95, 223, 223, 200, 48, 192, 240, 56, 29, 142, 14, 63, 192, 63, 255, 207, 134, 70, 15, 17, 32, 32, 32, 17, 11, 11, 23, 232, 232, 232, 208, 160, 64, 128, 0, 249, 15, 15, 159, 151, 14, 249, 6, 121, 127, 127, 191, 94, 47, 17, 14, 233, 209, 161, 64, 128, 0, 0, 0, 175, 151, 139, 5, 2, 1, 0, 0, 158, 254, 254, 253, 122, 228, 152, 96, 159, 240, 240, 249, 233, 240, 31, 224, 23, 23, 23, 11, 5, 2, 1, 0,
};
byte sample2[128] = {
  112, 136, 4, 4, 132, 200, 232, 232, 252, 3, 252, 255, 255, 127, 99, 253, 227, 28, 3, 15, 28, 56, 180, 112, 0, 1, 98, 146, 106, 245, 244, 245, 0, 128, 140, 146, 45, 94, 223, 223, 209, 32, 192, 240, 57, 29, 46, 14, 191, 64, 63, 255, 207, 134, 22, 15, 8, 16, 16, 16, 9, 11, 11, 23, 232, 232, 232, 208, 160, 64, 128, 0, 255, 15, 15, 159, 151, 14, 249, 6, 121, 127, 127, 191, 94, 47, 17, 14, 245, 233, 209, 160, 64, 128, 0, 0, 223, 175, 151, 11, 5, 2, 1, 0, 158, 254, 254, 125, 250, 228, 152, 96, 144, 240, 249, 233, 240, 255, 31, 224, 23, 23, 23, 11, 5, 2, 1, 0,
};


void sampleDataInit() {
  frames_next_enabled = true;
  frames_count = 2;
  frames_delay_ms[0] = 700;
  frames_delay_ms[1] = 250;

  for (int b = 0; b < 16; b++)
  {
    frames_data[b] = b;
    frames_data[b + 16] = b + 16;
  }


  for (int b = 0; b < 128; b++)
  {
    banks_data[b] = sample1[b];
    banks_data[b + 128] = sample2[b];
  }

}
*/

void loop() {
  unsigned long currentMillis = millis();

  if (frames_next_enabled && frames_next_ms <= currentMillis) {
    processNextFrame();
  }
}


void serialEvent() {
  while (Serial.available()) {
    // wait for start byte
    if (!command_receive_start)
    {
      if ((byte)Serial.read() == command_op_start)
      {
        command_receive_start = true;
      }
      continue;
    }

    // read command
    if (!command_receive_op_known)
    {
      // get the new byte:
      switch (command_receive_op = (byte)Serial.read())
      {
        case command_op_set_banks:
        case command_op_set_frames:
          command_receive_op_known = true;
          break;
        default:
          // unknown command
          Serial.write(command_op_err);
          commandReset(false /* success */);
          break;
      }
      continue;
    }

    // too long command
    if (command_buffer_used >= 256)
    {
      commandReset(false);
      continue;
    }

    command_buffer[command_buffer_used++] = (byte)Serial.read();

    // detect length
    if (!command_receive_length_known)
    {
      switch (command_receive_op)
      {
        case command_op_set_banks:
          if (command_buffer_used >= 2)
          {
            // validate command and calculate length
            if (command_buffer[1] < command_buffer[0] || command_buffer[1] >= banks_count) // end must be after start and validate range
            {
              commandReset(false);
              continue;
            }

            command_receive_length = 2 + (1 + (int)command_buffer[1] - (int)command_buffer[0]) * 8; // 2 (header) + 8 * (1 + to_index - from_index)
            command_receive_length_known = true;
          }
          break;
        case command_op_set_frames:
          if (command_buffer_used >= 1)
          {
            // validate command and calculate length
            if (command_buffer[0] >= frames_capacity) // validate range
            {
              commandReset(false);
              continue;
            }

            command_receive_length = 1 + 17 * (int)command_buffer[0]; // 1 (header) + 17 * frames_count
            command_receive_length_known = true;
          }
          break;
      }
    }


    // still unknown length or yet incomplete command
    if (!command_receive_length_known || command_receive_length > command_buffer_used)
    {
      continue;
    }

    // parse command
    switch (command_receive_op)
    {
      case command_op_set_banks:
        parseSetBanks();
        continue;
      case command_op_set_frames:
        parseSetFrames();
        continue;
    }
    commandReset(false /*success*/);
  }
}

void parseSetBanks() {
  // stop animation as we are replacing banks
  frames_next_enabled = false;
  
  int from = (int)command_buffer[0];
  int to = (int)command_buffer[1];

  int dataIndex = 2; // start of the data part
  int currentBankIndex = from * 8; // start of bank array
  for (int i = 0; i < 8 * (to - from + 1); i++) {
    banks_data[currentBankIndex++] = command_buffer[dataIndex++];
  }

  commandReset(true /* success */);
}

void parseSetFrames() {
  frames_count = (int)command_buffer[0];
  frames_current = 0;

  int dataIndex = 1; // start of the data part
  int framesDataIndex = 0; // start in frames array
  for (int frameIndex = 0; frameIndex < frames_count; frameIndex++) {
    for (int j = 0; j < 16; j++) {
      frames_data[framesDataIndex++] = command_buffer[dataIndex++];
    }
    frames_delay_ms[frameIndex] = 10 * (int)(command_buffer[dataIndex++]);
  }

  commandReset(true /*success*/);
  processNextFrame();
}

void commandReset(bool success) {
  command_buffer_used = 0;
  command_receive_start = false;
  command_receive_op_known = false;
  command_receive_op = 0;
  command_receive_length_known = false;
  command_receive_length = 0;

  Serial.write(success ? command_op_ok : command_op_err);
}


void processNextFrame() {
  if (frames_count == 0)
  {
    // clear
    initAndClearScreen(false /* do init */);
  }
  else
  {
    // render current
    printCurrentFrame();
  }

  // next step?
  if (frames_count == 0 || frames_delay_ms[frames_current] == 0)
  {
    // stop
    frames_next_enabled = false;
  }
  else
  {
    // next frame
    frames_next_enabled = true;
    frames_next_ms = millis() + frames_delay_ms[frames_current];
    frames_current++;
    if (frames_current >= frames_count) frames_current = 0;
  }
}

void printCurrentFrame() {
  int row = 0;

  int frameDataIndex = frames_current * segments; // start index
  for (int seg = 0; seg < segmentsWidth; seg++)
  {
    int bank1 = 8 * frames_data[frameDataIndex];
    int bank2 = 8 * frames_data[frameDataIndex + segmentsWidth];
    for (row = 0; row < 8; row++)
    {
      lc.setRow(seg, 7 - row, banks_data[bank1]);
      lc2.setRow(seg, 7 - row, banks_data[bank2]);
      bank1++;
      bank2++;
    }
    frameDataIndex++;
  }
}

void initAndClearScreen(bool doInit) {
  for (int index = 0; index < lc.getDeviceCount(); index++) {
    if (doInit) {
      lc.shutdown(index, false);      //The MAX72XX is in power-saving mode on startup
      lc.setIntensity(index, 1);     // Set the brightness to maximum value
    }
    lc.clearDisplay(index);

    if (doInit) {
      lc2.shutdown(index, false);      //The MAX72XX is in power-saving mode on startup
      lc2.setIntensity(index, 1);     // Set the brightness to maximum value
    }
    lc2.clearDisplay(index);
  }
}
