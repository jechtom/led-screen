using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController
{
    public class DisplayClient : IDisposable
    {
        SerialPort _port;
        AutoResetEvent _successEvent = new AutoResetEvent(initialState: false);
        AutoResetEvent _bufferEmptyEvent = new AutoResetEvent(initialState: false);

        public DisplayClient(string comPortName)
        {
            _port = new SerialPort(comPortName, 9600, Parity.None, 8, StopBits.One);
        }

        public void Connect()
        {
            _port.Open();
            WaitForInit();
        }

        private void WaitForInit()
        {
            Console.WriteLine("Waiting for device to be ready...");

            var initResponseEvent = new ManualResetEventSlim();

            void InitDataReceived(object sender, SerialDataReceivedEventArgs e)
            {
                while (_port.BytesToRead > 0)
                {
                    int ch = _port.ReadChar();
                    if (ch == 'K' /*ok*/ || ch == 'E' /*error*/ || ch == '?' /*unknown-should be 'E' instead */) initResponseEvent.Set();
                }
            }

            _port.DataReceived += InitDataReceived;

            do
            {
                _port.Write(">");
            } while (!initResponseEvent.Wait(TimeSpan.FromSeconds(0.5)));

            _port.DataReceived -= InitDataReceived;
            _port.DataReceived += this.DataReceived;

            Console.WriteLine("Device is ready.");
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (_port.BytesToRead > 0)
            {
                int ch = _port.ReadChar();
                //Console.WriteLine($"Received: {(char)ch} / {ch}");
                if (ch == 'K') _successEvent.Set();
                if (ch == '.') _bufferEmptyEvent.Set();
            }
        }

        const int BanksCount = 64;
        const int BanksBatchLimit = 31; // 31 * 8 + 1 < 256 (message limit)

        public void SendSetBanks(Span<byte> banks, int fromIndex)
        {
            if (banks.Length == 0) throw new ArgumentException("Zero length array is invalid.");
            if (banks.Length % 8 != 0) throw new ArgumentException("Invalid array length. Length % 8 != 0.");
            int banksCount = banks.Length / 8;
            if (fromIndex + banksCount > BanksCount) throw new ArgumentException("Bank Ids is out of capacity.");

            // split to multiple requests if needed
            if (banksCount > BanksBatchLimit)
            {
                SendSetBanks(banks.Slice(0, BanksBatchLimit * 8), fromIndex);
                SendSetBanks(banks.Slice(BanksBatchLimit * 8), fromIndex + BanksBatchLimit);
                return;
            }

            var ms = new MemoryStream();
            ms.Write(new byte[] { 0x0A, 0x42, (byte)fromIndex, (byte)(fromIndex + banksCount - 1) });
            ms.Write(banks);
            SendMessage(ms.ToArray());
        }


        public void SendSetFrames(Frame[] frames)
        {
            if (frames.Length > 14) throw new ArgumentException("Too much frames.");
            if (frames.Any(f => f.BankIds.Length != 16)) throw new ArgumentException("At least one of frames got invalid length of bank Ids array.");
            if (frames.Any(f => f.Duration < TimeSpan.Zero || f.Duration.TotalMilliseconds > 255 * 10)) throw new ArgumentException("At least one of frames got invalid duration.");

            var ms = new MemoryStream();
            ms.Write(new byte[] { 0x0A, 0x46, (byte)frames.Length });

            foreach (var frame in frames)
            {
                byte duration = (byte)(frame.Duration.TotalMilliseconds / 10);
                if (duration == 0 && frame.Duration > TimeSpan.Zero) duration = 1; // lowest non-zero duration if rounded to zero but not zero
                ms.Write(frame.BankIds);
                ms.WriteByte(duration);
            }

            SendMessage(ms.ToArray());
        }

        void SendMessage(byte[] data)
        {
            _successEvent.Reset();

            const int segmentLength = 64;

            for (int position = 0; position < data.Length; position += segmentLength)
            {
                var len = Math.Min(data.Length - position, segmentLength);
                Console.WriteLine($" > sending data segment ({len}B): {ByteArrayToString(data.AsSpan(position, len))}");
                _bufferEmptyEvent.Reset();
                _port.Write(data, position, len);
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
            throw new InvalidOperationException("Did not received success response in time.");
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
                if(_port.IsOpen)
                {
                    _port.Close();
                }
                _port.Dispose();
                _port = null;
            }

        }
    }
}
