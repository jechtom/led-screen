namespace TestController
{
    public class Frame
    {
        public Frame(byte[] bankIds, TimeSpan duration)
        {
            if (bankIds.Length != DisplayClient.FRAME_SIZE) throw new ArgumentException("Invalid size of frame.", nameof(bankIds));
            BankIds = bankIds;
            Duration = duration;
        }

        public byte[] BankIds { get; set; }
        public TimeSpan Duration { get; set; }
    }
}