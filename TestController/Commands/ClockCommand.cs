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
        Font? font, fontWeather;

        [Verb("clock", HelpText = "Run clock.")]
        public class Options : OptionsBase
        {
            [Option('w', "weather")]
            public bool Weather { get; set; }

            [Option('c', "christmas")]
            public bool IsChristmasModeEnabled { get; set; }
        }

        public async Task<int> RunAsync(Options options)
        {
            font = FontLoader.Load(@"fonts/CP850-8x8.font.txt");
            fontWeather = FontLoader.Load(@"fonts/weather.font.txt");

            using (var display = options.CreateClient())
            {
                display.Connect();
                await ClockAsync(display, options);
            }
            return 0;
        }

        async Task ClockAsync(IDisplayClientWithBatches display, Options options)
        {
            int offsetFontWeather = 200;
            Dictionary<int, char> weatherIcons = new int[]
            {
                1, 2, 3, 4, 9, 10, 11, 13, 50, 99, 100
            }.Select((val, index) => (val, index))
            .ToDictionary(v => v.val, v => (char)(offsetFontWeather + v.index));

            font = font.ReplaceChars(weatherIcons.Select(ic => new KeyValuePair<char, byte[]>(ic.Value, fontWeather.Chars[(char)ic.Key])));

            Dictionary<char, (int Index1, int Index2)> charMapping;

            using (display.SendSetBanksBatch())
            {
                charMapping = new char[]
                {
                ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', 'C', '.', ',', '-', 'D', (char)248/*degrees*/
                }.Concat(weatherIcons.Values).Select((ch, index) => (Char: ch, Index1: index, Index2: -1))
                .Select(mapping =>
                {
                    var data = font.Chars[mapping.Char];
                    display.SendSetBanks(data.AsSpan(0, 8), mapping.Index1);
                    //display.SendSetBanks(data.AsSpan(8, 8), mapping.Index2);
                    return mapping;
                }).ToDictionary(mapping => mapping.Char, mapping => (mapping.Index1, mapping.Index2));
            }

            var loader = new WeatherLoader();
            string temp = "";
            char tempIcon = (char)99; // unknown
            char christmasTreeIcon = (char)100; // christmas tree

            var cancel = new CancellationTokenSource();

            Task taskWeather = Task.CompletedTask;

            if (options.Weather)
            {
                taskWeather = Task.Run(async () =>
                {
                    while (!cancel.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var result = await loader.RefreshAsync();
                            temp = result.Temp.ToString("0", CultureInfo.InvariantCulture);
                            tempIcon = (char)(int.Parse(result.Icon.Substring(0, 2))); // TODO validate if valid code 
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Failed to fetch weather data. Will try again. Error:\n{e})");
                        }

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

            byte[] bytesFromText1Row(string text) =>
                text.PadRight(8).Substring(0, 8).Select(ch => (byte)charMapping[ch].Index1)
                .ToArray();

            var taskRefresh = Task.Run(async () =>
            {
                try
                {
                    while (!cancel.Token.IsCancellationRequested)
                    {
                        string text1 = DateTime.Now.ToString("HH:mm:ss");
                        Console.WriteLine($"Send time {text1}");

                        string text2 = true switch
                        {
                            _ when (options.IsChristmasModeEnabled && (Environment.TickCount / 3000) % 2 == 0) =>
                                $"{weatherIcons[christmasTreeIcon]} {Math.Max(0, (int)(new DateTime(DateTime.Now.Year, 12, 24) - DateTime.Today).TotalDays)}D",
                            _ =>
                                $"{weatherIcons[tempIcon]} {temp}{(char)248}C"
                        };

                        display.SendSetFrames(new Frame[] {
                        new Frame(bytesFromText1Row(text1).Concat(bytesFromText1Row(text2)).ToArray(), TimeSpan.FromMilliseconds(2000)),
                        new Frame(bytesFromText1Row("  :  :  ").Concat(bytesFromText1Row("")).ToArray(), TimeSpan.FromMilliseconds(2000))
                    });

                        try
                        {
                            // wait for next second
                            await Task.Delay(TimeSpan.FromMilliseconds(1000 - DateTime.Now.Millisecond), cancel.Token);
                        }
                        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error rendering command:\n{ex}");
                }
                finally
                {
                    cancel.Cancel(); // if main task stops, cancel all tasks
                }
            });

            // create task to register manual exit
            var manualCancelTask = Task.Run(() =>
            {
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
                cancel.Cancel();
            }, cancel.Token);

            await Task.WhenAll(taskRefresh, taskWeather, manualCancelTask);
            Console.WriteLine("Exiting.");
            display.SendClear();
        }
    }
}
