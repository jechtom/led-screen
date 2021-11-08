using System.Collections.Immutable;

namespace TestController
{
    public record Font
    {
        public ImmutableDictionary<char, byte[]> Chars { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}