using CommandLine;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController.Commands
{
    internal class ListPortsCommand : ICommand<ListPortsCommand.Options>
    {
        [Verb("list", HelpText = "List COM ports.")]
        public class Options { }

        public async Task<int> RunAsync(Options options)
        {
            Console.WriteLine("Port names:");
            foreach (var port in SerialPort.GetPortNames())
            {
                Console.WriteLine($" - {port}");
            }
            Console.WriteLine("End of list.");
            return 0;
        }
    }
}
