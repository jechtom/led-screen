// See https://aka.ms/new-console-template for more information
using System.IO.Ports;
using TestController;

Console.WriteLine("Hello, World!");

var font = FontLoader.Load(@"fonts\ISO88591-8x16.font.txt").AddMargin(2);

using var port = new SerialPort("COM5", 9600);
port.DataReceived += DataReceived;
port.Open();

//Clock();
//DrawText("Honk !!!");
DrawPanda();

void DrawText(string text)
{
    text = text.PadRight(8);
    var charMapping = text.Distinct().ToArray()
        .Select((ch, index) => (Char: ch, Index1: index * 2, Index2: index * 2 + 1))
        .Select(mapping =>
        {
            var data = font.Chars[mapping.Char];
            SendSetBanks(data.AsSpan(0, 8), mapping.Index1);
            SendSetBanks(data.AsSpan(8, 8), mapping.Index2);
            return mapping;
        }).ToDictionary(mapping => mapping.Char, mapping => (mapping.Index1, mapping.Index2));

    byte[] bytesFromText(string text) =>
        text.Select(ch => (byte)charMapping[ch].Index1)
        .Concat(text.Select(ch => (byte)charMapping[ch].Index2))
        .ToArray();

    SendSetFrames(new Frame[] {
        new Frame(bytesFromText(text), TimeSpan.Zero)
    });

    Console.WriteLine($"Send text: {text}");
}

void Clock()
{
    var charMapping = new char[]
    {
        ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':'
    }.Select((ch, index) => (Char: ch, Index1: index * 2, Index2: index * 2 + 1))
    .Select(mapping =>
    {
        var data = font.Chars[mapping.Char];
        SendSetBanks(data.AsSpan(0, 8), mapping.Index1);
        SendSetBanks(data.AsSpan(8, 8), mapping.Index2);
        return mapping;
    }).ToDictionary(mapping => mapping.Char, mapping => (mapping.Index1, mapping.Index2));

    while (true)
    {
        string text1 = DateTime.Now.ToString("HH:mm:ss").Substring(0, 8);
        string text2 = text1.Replace(':', ' ');

        byte[] bytesFromText(string text) =>
            text.Select(ch => (byte)charMapping[ch].Index1)
            .Concat(text.Select(ch => (byte)charMapping[ch].Index2))
            .ToArray();

        SendSetFrames(new Frame[] {
            new Frame(bytesFromText(text1), TimeSpan.FromMilliseconds(0))
        });

        Thread.Sleep(1000);
        Console.WriteLine($"Send time {text1}");
    }
}


void DrawPanda()
{
    SendSetBanks(new byte[]
    {
    136, 4, 4, 4, 136, 208, 232, 232, 252, 3, 252, 255, 243, 97, 104, 240, 19, 12, 3, 15, 28, 184, 178, 112, 1, 98, 146, 106, 245, 244, 244, 245, 128, 76, 82, 109, 222, 95, 223, 223, 200, 48, 192, 240, 56, 29, 142, 14, 63, 192, 63, 255, 207, 134, 70, 15, 17, 32, 32, 32, 17, 11, 11, 23, 232, 232, 232, 208, 160, 64, 128, 0, 249, 15, 15, 159, 151, 14, 249, 6, 121, 127, 127, 191, 94, 47, 17, 14, 233, 209, 161, 64, 128, 0, 0, 0, 175, 151, 139, 5, 2, 1, 0, 0, 158, 254, 254, 253, 122, 228, 152, 96, 159, 240, 240, 249, 233, 240, 31, 224, 23, 23, 23, 11, 5, 2, 1, 0,
    112, 136, 4, 4, 132, 200, 232, 232, 252, 3, 252, 255, 255, 127, 99, 253, 227, 28, 3, 15, 28, 56, 180, 112, 0, 1, 98, 146, 106, 245, 244, 245, 0, 128, 140, 146, 45, 94, 223, 223, 209, 32, 192, 240, 57, 29, 46, 14, 191, 64, 63, 255, 207, 134, 22, 15, 8, 16, 16, 16, 9, 11, 11, 23, 232, 232, 232, 208, 160, 64, 128, 0, 255, 15, 15, 159, 151, 14, 249, 6, 121, 127, 127, 191, 94, 47, 17, 14, 245, 233, 209, 160, 64, 128, 0, 0, 223, 175, 151, 11, 5, 2, 1, 0, 158, 254, 254, 125, 250, 228, 152, 96, 144, 240, 249, 233, 240, 255, 31, 224, 23, 23, 23, 11, 5, 2, 1, 0,
    }, fromIndex: 0);

    SendSetFrames(new[]
    {
        new Frame(Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(), TimeSpan.FromMilliseconds(900)),
        new Frame(Enumerable.Range(16, 16).Select(i => (byte)i).ToArray(), TimeSpan.FromMilliseconds(400))
    });
}

Console.ReadLine();

SendClear();

Console.WriteLine("Press enter to exit...");
Console.ReadLine();

port.Close();

const int BanksCount = 64;
const int BanksBatchLimit = 31; // 31 * 8 + 1 < 256 (message limit)

void SendSetBanks(Span<byte> banks, int fromIndex)
{
    if (banks.Length == 0) throw new ArgumentException("Zero length array is invalid.");
    if (banks.Length % 8 != 0) throw new ArgumentException("Invalid array length. Length % 8 != 0.");
    int banksCount = banks.Length / 8;
    if (fromIndex + banksCount > BanksCount) throw new ArgumentException("Bank Ids is out of capacity.");
    
    // split to multiple requests if needed
    if(banksCount > BanksBatchLimit)
    {
        SendSetBanks(banks.Slice(0, BanksBatchLimit * 8), fromIndex);
        SendSetBanks(banks.Slice(BanksBatchLimit * 8), fromIndex + BanksBatchLimit);
        return;
    }

    port.Write(new byte[] { 0x0A, 0x42, (byte)fromIndex, (byte)(fromIndex + banksCount - 1) }, 0, 4);
    port.Write(banks.ToArray(), 0, banks.Length);
}

void SendClear()
{
    SendSetBanks(Enumerable.Repeat((byte)0, 8).ToArray(), 0);
    SendSetFrames(new Frame[] { new Frame(Enumerable.Repeat((byte)0, 16).ToArray(), TimeSpan.Zero)});
}

void SendSetFrames(Frame[] frames)
{
    if (frames.Length > 16) throw new ArgumentException("Too much frames.");
    if (frames.Any(f => f.BankIds.Length != 16)) throw new ArgumentException("At least one of frames got invalid length of bank Ids array.");
    if (frames.Any(f => f.Duration < TimeSpan.Zero || f.Duration.TotalMilliseconds > 255 * 10)) throw new ArgumentException("At least one of frames got invalid duration.");

    port.Write(new byte[] { 0x0A, 0x46, (byte)frames.Length }, 0, 3);

    foreach (var frame in frames)
    {
        byte duration = (byte)(frame.Duration.TotalMilliseconds / 10);
        if (duration == 0 && frame.Duration > TimeSpan.Zero) duration = 1; // lowest non-zero duration if rounded to zero but not zero
        port.Write(frame.BankIds, 0, 16);
        port.Write(new byte[] { duration }, 0, 1);
    }
}

void DataReceived(object sender, SerialDataReceivedEventArgs e)
{
    while (port.BytesToRead > 0)
    {
        int ch = port.ReadChar();
        Console.WriteLine($"Received: {(char)ch} / {ch}");
    }
}