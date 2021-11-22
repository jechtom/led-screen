using CommandLine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController.Commands
{
    internal class ClockCommand : ICommand<ClockCommand.Options>
    {
        Font font, font2, fontWeather;

        [Verb("clock", HelpText = "Run clock.")]
        public class Options : OptionsBase
        {
            [Option('w', "weather")]
            public bool Weather { get; set; }
        }

        public async Task<int> RunAsync(Options options)
        {
            font = FontLoader.Load(@"fonts/CP850-8x8.font.txt");
            font2 = FontLoader.Load(@"fonts/ISO88591-8x16.font.txt").AddMargin(2);
            fontWeather = FontLoader.Load(@"fonts/weather.font.txt");

            using (var display = options.CreateClient())
            {
                display.Connect();
                Clock(display, options);
            }
            return 0;
        }

        void Clock(DisplayClient display, Options options)
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
