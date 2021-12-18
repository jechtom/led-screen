using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController.Commands
{
    internal class OptionsBase
    {
        const string DebugComName = "debug";

        [Value(index: 0, Required = true, HelpText = $"Com port name. Use '{DebugComName}' for debug device.")]
        public string? Com { get; set; }

        [Option("dontexit", HelpText ="Don't exit process when pressed 'Enter'. Useful for Linux systemd that receive 'Enter' once started.")]
        public bool DontExit { get; set; }

        public IDisplayClientWithBatches CreateClient() =>
            Com switch {
                DebugComName => new DisplayClientWithBatchesDecorator(new DisplayDebugClient()),
                _ => new DisplayClientWithBatchesDecorator(new DisplayClient(Com ?? throw new InvalidOperationException("Null port.")))
            };
    }
}
