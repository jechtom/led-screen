using CommandLine;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController.Commands
{
    internal class ClearCommand : ICommand<ClearCommand.Options>
    {
        [Verb("clear", HelpText = "Clear the display.")]
        public class Options : OptionsBase { }

        public async Task<int> RunAsync(Options options)
        {
            using (var display = options.CreateClient())
            {
                display.Connect();
                display.SendClear();
                Console.WriteLine("Cleared.");
            }
            return 0;
        }
    }
}
