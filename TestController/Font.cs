using System.Collections.Immutable;

namespace TestController
{
    public record Font
    {
        public ImmutableDictionary<char, byte[]> Chars { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Font AddMargin(int y) => this with
        {
            Chars = Chars.Select(d => (Key: d.Key, Value: AddMargin(d.Value, y))).ToImmutableDictionary(d => d.Key, d => d.Value)
        };

        private byte[] AddMargin(byte[] value, int y)
        {
            var newValue = new byte[value.Length];
            Array.Copy(value, 0, newValue, y, value.Length - y);
            return newValue;
        }

        public Font ReplaceChars(IEnumerable<KeyValuePair<char, byte[]>> chars) => this with {
            Chars = Chars.RemoveRange(chars.Select(d => d.Key)).AddRange(chars)
        };
    }
}