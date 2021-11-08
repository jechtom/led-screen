// See https://aka.ms/new-console-template for more information
public class Frame
{
    public Frame(byte[] bankIds, TimeSpan duration)
    {
        this.BankIds = bankIds;
        this.Duration = duration;
    }

    public byte[] BankIds { get; set; }
    public TimeSpan Duration { get; set; }
}