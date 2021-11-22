using CommandLine;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController.Commands
{
    internal class TestCommand : ICommand<TestCommand.Options>
    {
        [Verb("test", HelpText = "Test display.")]
        public class Options : OptionsBase { }

        public async Task<int> RunAsync(Options options)
        {
            using (var display = options.CreateClient())
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
    }
}
