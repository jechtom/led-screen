using CommandLine;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController.Commands
{
    internal class PandaCommand : ICommand<PandaCommand.Options>
    {
        [Verb("panda", HelpText = "Panda love!")]
        public class Options : OptionsBase { }

        public async Task<int> RunAsync(Options options)
        {
            using (var display = options.CreateClient())
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
    }
}
