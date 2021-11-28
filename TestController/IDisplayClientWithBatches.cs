
namespace TestController
{
    public interface IDisplayClientWithBatches : IDisplayClient
    {
        IDisposable SendSetBanksBatch();
    }
}