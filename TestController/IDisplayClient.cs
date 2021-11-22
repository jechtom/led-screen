
namespace TestController
{
    public interface IDisplayClient : IDisposable  
    {
        void Connect();
        void SendClear();
        void SendSetBanks(Span<byte> banks, int fromIndex);
        void SendSetFrames(Frame[] frames);
    }
}