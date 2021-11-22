// See https://aka.ms/new-console-template for more information
using CommandLine;
using System.Globalization;
using System.IO.Ports;
using System.Text;

namespace TestController
{
    public class Program
    {

        static Font font, font2, fontWeather;

        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<ListOptions, ClockOptions, PandaOptions, ClearOptions, TestOptions>(args)
                .MapResult(
                    async (ListOptions opts) => await List(opts),
                    async (ClockOptions opts) => await StartClock(opts),
                    async (PandaOptions opts) => await StartPandas(opts),
                    async (ClearOptions opts) => await StartClear(opts),
                    async (TestOptions opts) => await StartTest(opts),
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
        public class ClockOptions : RunOptionsBase {
            [Option('w', "weather")]
            public bool Weather { get; set; }
        }


        [Verb("panda", HelpText = "Pandas!")]

        public class PandaOptions : RunOptionsBase { }

        [Verb("clear", HelpText = "Clear display.")]
        public class ClearOptions : RunOptionsBase { }

        [Verb("test", HelpText = "Test display.")]
        public class TestOptions : RunOptionsBase { }

        public static async Task<int> List(ListOptions options)
        {
            Console.WriteLine("Port names:");
            foreach (var port in SerialPort.GetPortNames())
            {
                Console.WriteLine($" - {port}");
            }
            Console.WriteLine("End of list.");
            return 0;
        }


        public static async Task<int> StartPandas(PandaOptions options)
        {
            using (var display = new DisplayClient(options.Com))
            {
                display.Connect();

                display.SendSetBanks(new byte[]
                {
                    136, 4, 4, 4, 136, 208, 232, 232, 252, 3, 252, 255, 243, 97, 104, 240, 19, 12, 3, 15, 28, 184, 178, 112, 1, 98, 146, 106, 245, 244, 244, 245, 128, 76, 82, 109, 222, 95, 223, 223, 200, 48, 192, 240, 56, 29, 142, 14, 63, 192, 63, 255, 207, 134, 70, 15, 17, 32, 32, 32, 17, 11, 11, 23, 232, 232, 232, 208, 160, 64, 128, 0, 249, 15, 15, 159, 151, 14, 249, 6, 121, 127, 127, 191, 94, 47, 17, 14, 233, 209, 161, 64, 128, 0, 0, 0, 175, 151, 139, 5, 2, 1, 0, 0, 158, 254, 254, 253, 122, 228, 152, 96, 159, 240, 240, 249, 233, 240, 31, 224, 23, 23, 23, 11, 5, 2, 1, 0,
                    112, 136, 4, 4, 132, 200, 232, 232, 252, 3, 252, 255, 255, 127, 99, 253, 227, 28, 3, 15, 28, 56, 180, 112, 0, 1, 98, 146, 106, 245, 244, 245, 0, 128, 140, 146, 45, 94, 223, 223, 209, 32, 192, 240, 57, 29, 46, 14, 191, 64, 63, 255, 207, 134, 22, 15, 8, 16, 16, 16, 9, 11, 11, 23, 232, 232, 232, 208, 160, 64, 128, 0, 255, 15, 15, 159, 151, 14, 249, 6, 121, 127, 127, 191, 94, 47, 17, 14, 245, 233, 209, 160, 64, 128, 0, 0, 223, 175, 151, 11, 5, 2, 1, 0, 158, 254, 254, 125, 250, 228, 152, 96, 144, 240, 249, 233, 240, 255, 31, 224, 23, 23, 23, 11, 5, 2, 1, 0,
                }, fromIndex: 0);

                display.SendSetFrames(new[]
                {
                    new Frame(Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(), TimeSpan.FromMilliseconds(500)),
                    new Frame(Enumerable.Range(16, 16).Select(i => (byte)i).ToArray(), TimeSpan.FromMilliseconds(100))
                });

                Console.WriteLine("Press enter to exit...");

                Console.ReadLine();

                display.SendClear();
            }
            return 0;
        }

        public static async Task<int> StartClear(ClearOptions options)
        {
            using (var display = new DisplayClient(options.Com))
            {
                display.Connect();
                display.SendClear();
                Console.WriteLine("Cleared.");
            }
            return 0;
        }

        public static async Task<int> StartTest(TestOptions options)
        {
            using (var display = new DisplayClient(options.Com))
            {
                display.Connect();
                display.SendSetBanks(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0);
                display.SendSetBanks(new byte[] { 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55 }, 1);
                display.SendSetBanks(new byte[] { 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA }, 2);
                display.SendSetBanks(new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA }, 3);
                display.SendSetBanks(new byte[] { 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55 }, 4);
                display.SendSetBanks(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, 5);
                display.SendSetFrames(new Frame[]
                {
                    new Frame(Enumerable.Repeat((byte)5, 16).ToArray(), TimeSpan.FromSeconds(2)),
                    new Frame(Enumerable.Repeat((byte)1, 16).ToArray(), TimeSpan.FromSeconds(2)),
                    new Frame(Enumerable.Repeat((byte)2, 16).ToArray(), TimeSpan.FromSeconds(2)),
                    new Frame(Enumerable.Repeat((byte)3, 16).ToArray(), TimeSpan.FromSeconds(2)),
                    new Frame(Enumerable.Repeat((byte)4, 16).ToArray(), TimeSpan.FromSeconds(2)),
                    new Frame(Enumerable.Repeat((byte)0, 16).ToArray(), TimeSpan.FromSeconds(2)),
                });

                Console.WriteLine("Infinite display test started. Press enter to clear and exit.");
                Console.ReadLine();
                display.SendClear();

            }
            return 0;
        }

        public static async Task<int> StartClock(ClockOptions options)
        {
            font = FontLoader.Load(@"fonts/CP850-8x8.font.txt");
            font2 = FontLoader.Load(@"fonts/ISO88591-8x16.font.txt").AddMargin(2);
            fontWeather = FontLoader.Load(@"fonts/weather.font.txt");

            using (var display = new DisplayClient(options.Com))
            {
                display.Connect();
                Clock(display, options);
            }
            return 0;
        }


        static void Clock(DisplayClient display, ClockOptions options)
        {
            int offsetFontWeather = 200;
            Dictionary<int, char> weatherIcons = new int[]
            {
                1, 2, 3, 4, 9, 10, 11, 13, 50, 99
            }.Select((val, index) => (val, index))
            .ToDictionary(v => v.val, v => (char)(offsetFontWeather + v.index));

            font = font.ReplaceChars(weatherIcons.Select(ic => new KeyValuePair<char, byte[]>(ic.Value, fontWeather.Chars[(char)ic.Key])));

            var charMapping = new char[]
            {
                ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', 'C', '.', ',', '-', (char)248/*degrees*/
            }.Concat(weatherIcons.Values).Select((ch, index) => (Char: ch, Index1: index * 2, Index2: index * 2 + 1))
            .Select(mapping =>
            {
                var data = font.Chars[mapping.Char];
                display.SendSetBanks(data.AsSpan(0, 8), mapping.Index1);
                display.SendSetBanks(data.AsSpan(8, 8), mapping.Index2);
                return mapping;
            }).ToDictionary(mapping => mapping.Char, mapping => (mapping.Index1, mapping.Index2));


            var loader = new WeatherLoader();
            string temp = "";
            char tempIcon = (char)99; // unknown

            var cancel = new CancellationTokenSource();

            Task taskWeather = Task.CompletedTask;

            if (options.Weather)
            {
                taskWeather = Task.Run(async () =>
                {
                    while (!cancel.Token.IsCancellationRequested)
                    {
                        var result = await loader.RefreshAsync();
                        temp = result.Temp.ToString("0", CultureInfo.InvariantCulture);
                        tempIcon = (char)(int.Parse(result.Icon.Substring(0, 2))); // TODO validate if valid code 

                        try
                        { 
                            await Task.Delay(TimeSpan.FromMinutes(15), cancel.Token);
                        }
                        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                });
            }

            byte[] bytesFromText2Rows(string text) =>
                text.PadRight(8).Substring(0, 8).Select(ch => (byte)charMapping[ch].Index1)
                .Concat(text.Select(ch => (byte)charMapping[ch].Index2))
                .ToArray();

            byte[] bytesFromText1Row(string text) =>
                text.PadRight(8).Substring(0, 8).Select(ch => (byte)charMapping[ch].Index1)
                .ToArray();

            var taskRefresh = Task.Run(async () =>
            {
                while (!cancel.Token.IsCancellationRequested)
                {
                    string text1 = DateTime.Now.ToString("HH:mm:ss");
                    Console.WriteLine($"Send time {text1}");


                    display.SendSetFrames(new Frame[] {
                        new Frame(bytesFromText1Row(text1).Concat(bytesFromText1Row($"{weatherIcons[tempIcon]} {temp}{(char)248}C")).ToArray(), TimeSpan.FromMilliseconds(2000)),
                        new Frame(bytesFromText1Row("  :  :  ").Concat(bytesFromText1Row("")).ToArray(), TimeSpan.FromMilliseconds(2000))
                    });

                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(1000), cancel.Token);
                    }
                    catch (OperationCanceledException) when (cancel.IsCancellationRequested)
                    {
                        return;
                    }
                }
            });

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
            cancel.Cancel();
            Task.WaitAll(taskRefresh, taskWeather);
            Console.WriteLine("Exiting.");
            display.SendClear();
        }
    }
}