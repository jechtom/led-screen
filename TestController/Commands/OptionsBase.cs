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
        [Value(index: 0, Required = true, HelpText = "Com port name.")]
        public string Com { get; set; }

        public DisplayClient CreateClient() => new DisplayClient(Com);
    }
}
