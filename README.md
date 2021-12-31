# DIY Led Matrix Display

## RaspberryPi

```
screen -S led-screen -dm bash -c 'cd ~/led-screen; ./TestController clock /dev/ttyUSB0 -w'
screen -list # list processes
screen -r led-screen # reattach to process
screen -S led-screen -X quit # quit process
```

### Systemd Installation

Edit `sudo nano /lib/systemd/system/led-clock.service`:
```
[Unit]
Description=LED Screen Clock Service
After=multi-user.target

[Service]
User=pi
Environment="DOTNET_ROOT=/home/pi/.dotnet"
Type=simple
ExecStart=bash -c "cd ~/led-screen; ./TestController clock /dev/ttyUSB0 -w -c --dontexit"

[Install]
WantedBy=multi-user.target
```

Then enable on boot:
```
sudo chmod 644 /lib/systemd/system/led-clock.service
sudo systemctl daemon-reload
sudo systemctl enable led-clock.service
```

Test with:
```
sudo systemctl start led-clock.service
sudo systemctl status led-clock.service
journalctl -u led-clock.service -f # show live logs
```

## Protocol

Binary custom protocol between host device and display (Arduino) over COM port to control the display.

Response is:

* `4B` ('K') for status OK (success)
* or `45` ('E') for status ERROR (failure)
* or `2E` ('.') as info that receive buffer is empty and device is ready for next data segment - see buffer limitations section
* all other responses are debug responses and can be ignored; lower-case letters are debug symbols

After each status response (OK or ERROR) client can start with transmitting new command.

Request message limit is 256 bytes.

### Buffer limitations

Due to limited RX buffer you need to split message up to 16 bytes segments. After sending each segment you need to wait for receive buffer empty message.

### Command `SET BANKS`

Sets value of up to 64 banks. Each bank represents preset of 8&times;8 LED segment as 8 bytes. Setting bank stops current animation as it is expected to be followed by `SET FRAMES` commands.

On startup all banks are zeroes and display is empty.

```
Host sends command:

0A 42 XX XX { XX[8] }
++ ++ ++ ++ +-|-----+
|  |  |  |  | |
|  |  |  |  | Bank 8 byte value
|  |  |  |  |
|  |  |  |  Bank values (each 8 bytes)
|  |  |  |
|  |  |  End bank index (0-63)
|  |  |
|  |  Start bank index (0-63)
|  |
|  Command `SET BANKS` constant 0x42 (char B)
|
All commands starts with 0A (\n)

Display reply:

4B
++
|
OK constance ('K')
```

Example - set banks 3-4. Set `bank 3` to `0xFFFFFFFFFFFFFFFF` and `bank 4` to `0xAAAAAAAAAAAAAAAA`.

```
Host sends command:

0A 42 03 04 /* header */
FF FF FF FF FF FF FF FF /* bank 3 */
AA AA AA AA AA AA AA AA /* bank 4 */

Display reply:

4B
```

### Command `SET FRAMES`

Defines frame or animation frames (up to 16) built from references to specific banks and starts with rendering it on display.

```
Host sends command:

0A 46 XX { XX[16] XX }
++ ++ ++ +-|------|--+
|  |  |  | |      |
|  |  |  | |      Delay before next frame (0 = never, 1-255 = delay; see remark)
|  |  |  | |
|  |  |  | Bank indices for each of 16 segments (16 bytes)
|  |  |  |
|  |  |  Frames data (each 17 bytes)
|  |  |
|  |  Frames count (0-16; 0 = clear display)
|  |
|  Command `SET BANKS` constant 0x46 (char F)
|
All commands starts with 0A (\n)

Display reply:

4B
++
|
OK constance ('K')
```

Remark: Delay is 10ms-2550ms (value is ×10ms).

Example - set 2 frames: set all segments to bank `0x05`, wait 70ms, set all segments to bank `0x06` and wait 70ms and repeat.

```
Host sends command:

0A 46 02 /* header */
05 05 05 05 05 05 05 05 05 05 05 05 05 05 05 05 07 /* frame 1 */
06 06 06 06 06 06 06 06 06 06 06 06 06 06 06 06 07 /* frame 2 */

Display reply:

4B
```