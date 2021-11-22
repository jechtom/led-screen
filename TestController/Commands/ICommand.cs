using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController.Commands
{
    internal interface ICommand<TOptions>
    {
        Task<int> RunAsync(TOptions options);
    }
}
