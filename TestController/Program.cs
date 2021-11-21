// See https://aka.ms/new-console-template for more information
using CommandLine;
using System.IO.Ports;
using System.Text;

namespace TestController
{
    public class Program
    {

        static SerialPort port;
        static Font font;
        static AutoResetEvent successEvent = new AutoResetEvent(initialState: false);
        static AutoResetEvent bufferEmptyEvent = new AutoResetEvent(initialState: false);

        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<ClockOptions, PandaOptions, ListOptions>(args)
                .MapResult(
                    async (ClockOptions opts) => await StartClock(opts),
                    async (PandaOptions opts) => await StartPandas(opts),
                    async (ListOptions opts) => await List(opts),
                    errs => Task.FromResult(-1)
                ); 
        }

        [Verb("list", HelpText = "List COM ports.")]
        public class ListOptions { }
        
        public class RunOptionsBase
        {
            [Value(index: 0, Required = true, HelpText = "Com port name.")]
            public string Com { get; set; }
        }

        [Verb("clock", HelpText = "Run clock.")]
        public class ClockOptions : RunOptionsBase { }


        [Verb("panda", HelpText = "Pandas!")]

        public class PandaOptions : RunOptionsBase { }

        public static async Task<int> List(ListOptions options)
        {
            Console.WriteLine("Port names:");
            foreach(var port in SerialPort.GetPortNames())
            {
                Console.WriteLine($" - {port}");
            }
            Console.WriteLine("End of list.");
            return 0;
        }


        public static async Task<int> StartPandas(PandaOptions options)
        {
            using (port = new SerialPort(options.Com, 9600, Parity.None, 8, StopBits.One))
            {
                port.Open();

                WaitForInit();

                port.DataReceived += DataReceived;

                DrawPanda();

                Console.WriteLine("Press enter to exit...");

                Console.ReadLine();

                SendClear();

                port.Close();
            }
            return 0;
        }


        public static async Task<int> StartClock(ClockOptions options)
        {
            font = FontLoader.Load(@"fonts/ISO88591-8x16.font.txt").AddMargin(2);
            
            using (port = new SerialPort(options.Com, 9600, Parity.None, 8, StopBits.One))
            {
                port.Open();

                WaitForInit();

                port.DataReceived += DataReceived;

                Clock();

                port.Close();
            }
            return 0;
        }

        private static void WaitForInit()
        {
            Console.WriteLine("Waiting for device to be ready...");

            var responseEvent = new ManualResetEventSlim();

            void DataReceived(object sender, SerialDataReceivedEventArgs e)
            {
                while (port.BytesToRead > 0)
                {
                    int ch = port.ReadChar();
                    if (ch == 'K' /*ok*/ || ch == 'E' /*error*/ || ch == '?' /*unknown-should be 'E' instead */) responseEvent.Set();
                }
            }

            port.DataReceived += DataReceived;
            
            do
            {
                port.Write(">");
            } while (!responseEvent.Wait(TimeSpan.FromSeconds(0.5)));

            port.DataReceived -= DataReceived;

            Console.WriteLine("Device is ready.");
        }

        static void Clock()
        {
            var charMapping = new char[]
            {
        ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':'
            }.Select((ch, index) => (Char: ch, Index1: index * 2, Index2: index * 2 + 1))
            .Select(mapping =>
            {
                var data = font.Chars[mapping.Char];
                SendSetBanks(data.AsSpan(0, 8), mapping.Index1);
                SendSetBanks(data.AsSpan(8, 8), mapping.Index2);
                return mapping;
            }).ToDictionary(mapping => mapping.Char, mapping => (mapping.Index1, mapping.Index2));

            while (true)
            {
                string text1 = DateTime.Now.ToString("HH:mm:ss");
                Console.WriteLine($"Send time {text1}");

                byte[] bytesFromText(string text) =>
                    text.Substring(0,8).PadRight(8).Select(ch => (byte)charMapping[ch].Index1)
                    .Concat(text.Select(ch => (byte)charMapping[ch].Index2))
                    .ToArray();

                SendSetFrames(new Frame[] {
                    new Frame(bytesFromText(text1), TimeSpan.FromMilliseconds(2000)),
                    new Frame(bytesFromText("  :  :  "), TimeSpan.FromMilliseconds(2000))
                });

                Thread.Sleep(1000);
            }
        }


        static void DrawPanda()
        {
            SendSetBanks(new byte[]
            {
    136, 4, 4, 4, 136, 208, 232, 232, 252, 3, 252, 255, 243, 97, 104, 240, 19, 12, 3, 15, 28, 184, 178, 112, 1, 98, 146, 106, 245, 244, 244, 245, 128, 76, 82, 109, 222, 95, 223, 223, 200, 48, 192, 240, 56, 29, 142, 14, 63, 192, 63, 255, 207, 134, 70, 15, 17, 32, 32, 32, 17, 11, 11, 23, 232, 232, 232, 208, 160, 64, 128, 0, 249, 15, 15, 159, 151, 14, 249, 6, 121, 127, 127, 191, 94, 47, 17, 14, 233, 209, 161, 64, 128, 0, 0, 0, 175, 151, 139, 5, 2, 1, 0, 0, 158, 254, 254, 253, 122, 228, 152, 96, 159, 240, 240, 249, 233, 240, 31, 224, 23, 23, 23, 11, 5, 2, 1, 0,
    112, 136, 4, 4, 132, 200, 232, 232, 252, 3, 252, 255, 255, 127, 99, 253, 227, 28, 3, 15, 28, 56, 180, 112, 0, 1, 98, 146, 106, 245, 244, 245, 0, 128, 140, 146, 45, 94, 223, 223, 209, 32, 192, 240, 57, 29, 46, 14, 191, 64, 63, 255, 207, 134, 22, 15, 8, 16, 16, 16, 9, 11, 11, 23, 232, 232, 232, 208, 160, 64, 128, 0, 255, 15, 15, 159, 151, 14, 249, 6, 121, 127, 127, 191, 94, 47, 17, 14, 245, 233, 209, 160, 64, 128, 0, 0, 223, 175, 151, 11, 5, 2, 1, 0, 158, 254, 254, 125, 250, 228, 152, 96, 144, 240, 249, 233, 240, 255, 31, 224, 23, 23, 23, 11, 5, 2, 1, 0,
            }, fromIndex: 0);

            SendSetFrames(new[]
            {
        new Frame(Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(), TimeSpan.FromMilliseconds(500)),
        new Frame(Enumerable.Range(16, 16).Select(i => (byte)i).ToArray(), TimeSpan.FromMilliseconds(100))
    });
        }

        const int BanksCount = 64;
        const int BanksBatchLimit = 31; // 31 * 8 + 1 < 256 (message limit)

        static void SendSetBanks(Span<byte> banks, int fromIndex)
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

        static void SendClear()
        {
            SendSetBanks(Enumerable.Repeat((byte)0, 8).ToArray(), 0);
            SendSetFrames(new Frame[] { new Frame(Enumerable.Repeat((byte)0, 16).ToArray(), TimeSpan.Zero) });
        }

        static void SendSetFrames(Frame[] frames)
        {
            if (frames.Length > 14) throw new ArgumentException("Too much frames.");
            if (frames.Any(f => f.BankIds.Length != 16)) throw new ArgumentException("At least one of frames got invalid length of bank Ids array.");
            if (frames.Any(f => f.Duration < TimeSpan.Zero || f.Duration.TotalMilliseconds > 255 * 10)) throw new ArgumentException("At least one of frames got invalid duration.");

            var ms = new MemoryStream();
            ms.Write(new byte [] { 0x0A, 0x46, (byte)frames.Length });

            foreach (var frame in frames)
            {
                byte duration = (byte)(frame.Duration.TotalMilliseconds / 10);
                if (duration == 0 && frame.Duration > TimeSpan.Zero) duration = 1; // lowest non-zero duration if rounded to zero but not zero
                ms.Write(frame.BankIds);
                ms.WriteByte(duration);
            }

            SendMessage(ms.ToArray());
        }

        static void SendMessage(byte[] data)
        {
            successEvent.Reset();

            const int segmentLength = 16;

            for (int position = 0; position < data.Length; position+=segmentLength)
            {
                var len = Math.Min(data.Length - position, segmentLength);
                Console.WriteLine($" > sending data segment ({len}B): {ByteArrayToString(data.AsSpan(position, len))}");
                bufferEmptyEvent.Reset(); 
                port.Write(data, position, len);
                if (!bufferEmptyEvent.WaitOne(TimeSpan.FromSeconds(1)))
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

        static void WaitForSuccessOrFail()
        {
            if (successEvent.WaitOne(TimeSpan.FromSeconds(1))) return; // ok
            throw new InvalidOperationException("Did not received success response in time.");
        }

        static void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (port.BytesToRead > 0)
            {
                int ch = port.ReadChar();
                //Console.WriteLine($"Received: {(char)ch} / {ch}");
                if (ch == 'K') successEvent.Set();
                if (ch == '.') bufferEmptyEvent.Set();
            }
        }
    }
}