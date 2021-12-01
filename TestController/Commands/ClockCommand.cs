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
            Dictionary<int, char> icons = new int[]
            {
                1, 2, 3, 4, 9, 10, 11, 13, 50, 99 /* unknown */, 100 /* christmas */, 101 /* christmas2 */, 102 /* heart */, 103 /* calendar */
            }.Select((val, index) => (val, index))
            .ToDictionary(v => v.val, v => (char)(offsetFontWeather + v.index));

            font = font.ReplaceChars(icons.Select(ic => new KeyValuePair<char, byte[]>(ic.Value, fontWeather.Chars[(char)ic.Key])));

            Dictionary<char, (int Index1, int Index2)> charMapping;

            using (display.SendSetBanksBatch())
            {
                charMapping = new char[]
                {
                ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', 'C', '.', ',', '-', 'D', (char)248/*degrees*/
                }.Concat(icons.Values).Select((ch, index) => (Char: ch, Index1: index, Index2: -1))
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
            char spaceChar = (char)32;
            char tempIcon = (char)99; // unknown
            char christmasTreeIcon = (char)100; // christmas tree
            char christmasTree2Icon = (char)101; // christmas tree 2
            char heartIcon = (char)102; // 
            char calendarIcon = (char)103; // calendar icon

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

            void UpdateFrameText(Frame frame, string text, int line)
            {
                const int CharsPerLineCount = 8;
                int frameIndex = line * CharsPerLineCount;
                int index = 0;

                // fill text
                for(; index < text.Length; index++)
                {
                    if (index >= CharsPerLineCount) return;
                    char ch = text[index];
                    frame.BankIds[frameIndex++] = (byte)(charMapping[ch].Index1);
                }

                // add spaces
                for(; index < CharsPerLineCount; index++)
                {
                    frame.BankIds[frameIndex++] = (byte)spaceChar;
                }
            }

            var taskRefresh = Task.Run(async () =>
            {
                try
                {
                    var frameWork = new Frame(new byte[16], TimeSpan.FromMilliseconds(2000));
                    var frameEmpty = new Frame(new byte[16], TimeSpan.FromMilliseconds(2000));
                    
                    UpdateFrameText(frameEmpty, "  :  :  ", line: 0);
                    UpdateFrameText(frameEmpty, "        ", line: 1);
                    
                    var frames = new Frame[] {
                        frameWork,
                        frameEmpty
                    };

                    while (!cancel.Token.IsCancellationRequested)
                    {
                        string text1 = DateTime.Now.ToString("HH:mm:ss");
                        //debug: Console.WriteLine($"Send time {text1}");
                        UpdateFrameText(frameWork, text1, line: 0);

                        int tickModulo = (Environment.TickCount / 3000) % 3;
                        bool tickeModulo2 = (Environment.TickCount / 1000) % 2 == 0;
                        int daysUntilChristmas = Math.Min(99, Math.Max(0, (int)(new DateTime(DateTime.Now.Year, 12, 24) - DateTime.Today).TotalDays));

                        int christmasTreeIconX = tickeModulo2 ? christmasTreeIcon : christmasTree2Icon;
                        int christmasTreeIconY = tickeModulo2 ? christmasTree2Icon : christmasTreeIcon;

                        string text2 = true switch
                        {
                            _ when (options.IsChristmasModeEnabled && tickModulo == 2) =>
                                daysUntilChristmas > 0 ?
                                $"{icons[heartIcon]}{icons[christmasTreeIconX]} {daysUntilChristmas:00} {icons[christmasTreeIconY]}{icons[heartIcon]}"
                                : $"{icons[heartIcon]}{icons[christmasTreeIconX]}{icons[heartIcon]}{icons[christmasTreeIconY]}{icons[heartIcon]}{icons[christmasTreeIconX]}{icons[heartIcon]}{icons[christmasTreeIconY]}",
                            _ when (tickModulo == 1) =>
                                $"  {DateTime.Today:d.M.}",
                            _ =>
                                $"{icons[tempIcon]} {temp}{(char)248}C"
                        };

                        UpdateFrameText(frameWork, text2, line: 1);

                        display.SendSetFrames(frames);

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
