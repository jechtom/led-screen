// See https://aka.ms/new-console-template for more information
using CommandLine;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using TestController.Commands;

namespace TestController
{
    public class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments<ListPortsCommand.Options, TestCommand.Options, PandaCommand.Options, ClockCommand.Options, ClearCommand.Options>(args)
                .MapResult(
                    async (ListPortsCommand.Options opts) => await new ListPortsCommand().RunAsync(opts),
                    async (TestCommand.Options opts) => await new TestCommand().RunAsync(opts),
                    async (PandaCommand.Options opts) => await new PandaCommand().RunAsync(opts),
                    async (ClockCommand.Options opts) => await new ClockCommand().RunAsync(opts),
                    async (ClearCommand.Options opts) => await new ClearCommand().RunAsync(opts),
                    errs => Task.FromResult(-1)
                );
        }
    }
}