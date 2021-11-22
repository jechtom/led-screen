using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController
{
    internal class DisplayDebugClient : IDisplayClient
    {
        public void Connect()
        {
        }

        public void Dispose()
        {
        }

        public void SendClear()
        {
            throw new NotImplementedException();
        }

        public void SendSetBanks(Span<byte> banks, int fromIndex)
        {
            throw new NotImplementedException();
        }

        public void SendSetFrames(Frame[] frames)
        {
            throw new NotImplementedException();
        }
    }
}
