using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestController
{
    /// <summary>
    /// Implements batches support for some commands.
    /// </summary>
    public class DisplayClientWithBatchesDecorator : IDisplayClientWithBatches
    {
        private bool _isInBatch = false;
        private SetBankBatch _batchSendSetBanks = SetBankBatch.Empty;

        record SetBankBatch(int FromIndex, byte[] Data)
        {
            public static SetBankBatch Empty { get; } = new SetBankBatch(0, new byte[0]);
            public bool IsEmpty => Length == 0;
            public int Length { get; } = Data.Length / DisplayClient.BANK_SIZE;
            public int ToIndex => FromIndex + Length;

            public bool TryAppend(SetBankBatch another, out SetBankBatch result)
            {
                if (IsEmpty)
                {
                    result = another;
                    return true;
                }

                if (another.IsEmpty)
                {
                    result = this;
                    return true;
                }

                if (ToIndex != another.FromIndex)
                {
                    result = Empty;
                    return false;
                }

                // append
                var array1 = Data;
                var array2 = another.Data;
                int array1OriginalLength = array1.Length;
                Array.Resize(ref array1, array1OriginalLength + array2.Length);
                Array.Copy(array2, 0, array1, array1OriginalLength, array2.Length);
                result = new SetBankBatch(FromIndex, array1);
                return true;
            }
        }

        public DisplayClientWithBatchesDecorator(IDisplayClient inner)
        {
            Inner = inner;   
        }

        public IDisplayClient Inner { get; }

        public void Connect() => Inner.Connect();

        public IDisposable SendSetBanksBatch() => new SendBatchDisposable(this);

        class SendBatchDisposable : IDisposable
        {
            public SendBatchDisposable(DisplayClientWithBatchesDecorator displayClient)
            {
                DisplayClient = displayClient;
                DisplayClient.SendSetBanksBatchBegin();
            }

            public DisplayClientWithBatchesDecorator DisplayClient { get; }

            public void Dispose()
            {
                DisplayClient.SendSetBanksBatchEnd();
            }
        }

        private void SendSetBanksBatchBegin()
        {
            if (_isInBatch) throw new InvalidOperationException("Already in batch.");
            _isInBatch = true;
        }

        private void SendSetBanksBatchEnd()
        {
            if (!_isInBatch) new InvalidOperationException("Not in batch.");
            _isInBatch = false;
            
            if (_batchSendSetBanks.IsEmpty) return; // nothing to send?
            Inner.SendSetBanks(_batchSendSetBanks.Data, _batchSendSetBanks.FromIndex);
            _batchSendSetBanks = SetBankBatch.Empty;
        }

        public void SendClear() => Inner.SendClear();
        public void SendSetBanks(Span<byte> banks, int fromIndex)
        {
            // send without batch?
            if (!_isInBatch)
            {
                Inner.SendSetBanks(banks, fromIndex);
                return;
            }

            var newBatchPart = new SetBankBatch(fromIndex, banks.ToArray());

            if(_batchSendSetBanks.TryAppend(newBatchPart, out SetBankBatch newAppendedBatch))
            {
                // appended
                _batchSendSetBanks = newAppendedBatch;
            }
            else
            {
                // send and start new
                Inner.SendSetBanks(_batchSendSetBanks.Data, _batchSendSetBanks.FromIndex);
                _batchSendSetBanks = newBatchPart;
            }
        }

        public void SendSetFrames(Frame[] frames) => Inner.SendSetFrames(frames);
        public void Dispose() => Inner.Dispose();
    }
}
