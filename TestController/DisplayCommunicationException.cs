using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController
{
    /// <summary>
    /// Represents inner exception
    /// </summary>
    public class DisplayCommunicationException : Exception
    {
        public DisplayCommunicationException(string? message) : base(message)
        {
        }

        public DisplayCommunicationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
