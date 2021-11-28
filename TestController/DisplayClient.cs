using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController
{
    public class DisplayClient : IDisposable, IDisplayClient
    {
        
        SerialPort _port;
        AutoResetEvent _successEvent = new AutoResetEvent(initialState: false);
        AutoResetEvent _bufferEmptyEvent = new AutoResetEvent(initialState: false);
        AutoResetEvent _readyForNextCommand = new AutoResetEvent(initialState: false);
        byte[] _messageBuffer = new byte[256]; // maximum message length
        RetryPolicy _retryPolicy;

        public DisplayClient(string comPortName)
        {
            _port = new SerialPort(comPortName, 9600, Parity.None, 8, StopBits.One);
            _retryPolicy = Policy
              .Handle<DisplayCommunicationException>()
              .WaitAndRetry(sleepDurations: new[]
              {
                TimeSpan.FromSeconds(0.2),
                TimeSpan.FromSeconds(0.5),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(4)
              }, onRetry: (er, _) =>
              {
                  Console.WriteLine($"Retry. Failed attempt to communicate:\n{er}");
                  SendDataUntilDeviceIsReadyForNext(); // device in unknown state
              });
        }

        public void Connect()
        {
            _port.DataReceived += DataReceived;
            _port.Open();
            SendDataUntilDeviceIsReadyForNext();
        }

        private void SendDataUntilDeviceIsReadyForNext()
        {
            Console.WriteLine("Waiting for device to be ready...");

            _readyForNextCommand.Reset();

            // Bombard with data until device will announce error/ok - that mean: ready for next command.
            // This is needed:
            //  1) On init - waiting for device/com port to be ready and inited.
            //  2) On failed transmission device can be in the middle of receiving command data (up to 256B).
            //     To complete it we will just fill it anything so we can start with sending retry.
            do
            {
                _port.Write(">");
            } while (!_readyForNextCommand.WaitOne(TimeSpan.FromSeconds(0.02)));

            Console.WriteLine("Device is ready.");
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (_port.BytesToRead > 0)
            {
                int ch = _port.ReadChar();
                //Console.WriteLine($"Received: {(char)ch} / {ch}");
                if (ch == 'K' /*ok*/ || ch == 'E' /*error*/) _readyForNextCommand.Set();
                if (ch == 'K') _successEvent.Set();
                if (ch == '.') _bufferEmptyEvent.Set();
            }
        }

        public const int BANKS_COUNT = 64; // banks count in device memory
        public const int COMMAND_SET_BANKS_LIMIT_BANKS = 31; // 1 + 31 * 8 < 256 (message limit) - maximum number of banks in single batch
        public const int BANK_SIZE = 8; // size of single bank item
        public const int FRAME_SIZE = 16; // size of single frame
        public const int FRAMES_COUNT = 14; // frames maximum count in device memory
        public const int FRAME_SIZE_MESSAGE = FRAME_SIZE + 1; // size of single frame message (includes delay byte)
        public const int SEND_SEGMENT_SIZE = 64; // maximum number of bytes to send before waiting for confirmation - to prevent device buffer overflow
        
        public void SendSetBanks(Span<byte> banks, int fromIndex)
        {
            if (banks.Length == 0) throw new ArgumentException("Zero length array is invalid.");
            if (banks.Length % BANK_SIZE != 0) throw new ArgumentException($"Invalid array length. Length % {BANK_SIZE} != 0.");
            int banksCount = banks.Length / BANK_SIZE;
            if (fromIndex + banksCount > BANKS_COUNT) throw new ArgumentException($"Bank Ids is out of capacity. Banks count: {BANKS_COUNT}");

            // split to multiple requests if needed
            if (banksCount > COMMAND_SET_BANKS_LIMIT_BANKS)
            {
                SendSetBanks(banks.Slice(0, COMMAND_SET_BANKS_LIMIT_BANKS * BANK_SIZE), fromIndex);
                SendSetBanks(banks.Slice(COMMAND_SET_BANKS_LIMIT_BANKS * BANK_SIZE), fromIndex + COMMAND_SET_BANKS_LIMIT_BANKS);
                return;
            }

            int length = 0;
            _messageBuffer[length++] = 0x0A;
            _messageBuffer[length++] = 0x42;
            _messageBuffer[length++] = (byte)fromIndex;
            _messageBuffer[length++] = (byte)(fromIndex + banksCount - 1);
            banks.CopyTo(new Span<byte>(_messageBuffer, length, banks.Length));
            length += banks.Length;
            SendMessage(length);
        }
        
        public void SendSetFrames(Frame[] frames)
        {
            if (frames.Length > FRAMES_COUNT) throw new ArgumentException("Too much frames.");
            if (frames.Any(f => f.BankIds.Length != FRAME_SIZE)) throw new ArgumentException("At least one of frames got invalid length of bank Ids array.");
            if (frames.Any(f => f.Duration < TimeSpan.Zero || f.Duration.TotalMilliseconds > 255 * 10)) throw new ArgumentException("At least one of frames got invalid duration.");

            int length = 0;
            _messageBuffer[length++] = 0x0A;
            _messageBuffer[length++] = 0x46;
            _messageBuffer[length++] = (byte)frames.Length;

            foreach (var frame in frames)
            {
                byte duration = (byte)(frame.Duration.TotalMilliseconds / 10);
                if (duration == 0 && frame.Duration > TimeSpan.Zero) duration = 1; // lowest non-zero duration if rounded to zero but not zero
                Buffer.BlockCopy(frame.BankIds, 0, _messageBuffer, length, FRAME_SIZE);
                length += FRAME_SIZE;
                _messageBuffer[length++] = duration;
            }

            SendMessage(length);
        }

        void SendMessage(int length)
        {
            // retry command until successful or fails several times
            _retryPolicy.Execute(() => SendMessageImpl(length));
        }

        void SendMessageImpl(int length)
        {
            _successEvent.Reset();

            for (int position = 0; position < length; position += SEND_SEGMENT_SIZE)
            {
                var len = Math.Min(length - position, SEND_SEGMENT_SIZE);
                //debug: Console.WriteLine($" > sending data segment ({len}B): {ByteArrayToString(_messageBuffer.AsSpan(position, len))}");
                _bufferEmptyEvent.Reset();
                _port.Write(_messageBuffer, position, len);
                if (!_bufferEmptyEvent.WaitOne(TimeSpan.FromSeconds(1)))
                {
                    throw new InvalidOperationException("Did not received success segment read response in time.");
                }
            }

            WaitForSuccessOrFail();

            Console.WriteLine("Command sent successfuly.");
        }

        public static string ByteArrayToString(Span<byte> ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        void WaitForSuccessOrFail()
        {
            if (_successEvent.WaitOne(TimeSpan.FromSeconds(1))) return; // ok
            throw new DisplayCommunicationException("Did not received success response in time.");
        }

        public void SendClear()
        {
            SendSetBanks(Enumerable.Repeat((byte)0, 8).ToArray(), 0);
            SendSetFrames(new Frame[] { new Frame(Enumerable.Repeat((byte)0, 16).ToArray(), TimeSpan.Zero) });
        }

        public void Dispose()
        {
            if (_port != null)
            {
                if (_port.IsOpen)
                {
                    _port.Close();
                }
                _port.Dispose();
                _port = null;
            }

        }
    }
}
