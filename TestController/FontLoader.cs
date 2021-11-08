using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestController
{
    public class FontLoader
    {
        public static Font Load(string path)
        {
            var data = new Dictionary<char, byte[]>();

            int? width = null;
            int? height = null;
            int? inChar = null;
            BitArray? charData = null;
            int inCharLine = 0;

            var fwRegex = new Regex(@"^@FW (?<val>[\d]+)");
            var fhRegex = new Regex(@"^@FH (?<val>[\d]+)");
            var charRegex = new Regex(@"^@CH (?<val>[\d]+) ;");

            foreach (var line in File.ReadLines(path))
            {
                Match match;

                if(width == null && (match = fwRegex.Match(line)).Success)
                {
                    width = int.Parse(match.Groups["val"].Value);
                } else if (height == null && (match = fhRegex.Match(line)).Success)
                {
                    height = int.Parse(match.Groups["val"].Value);
                } else if (width != null && height != null && (match = charRegex.Match(line)).Success)
                {
                    inChar = int.Parse(match.Groups["val"].Value);
                    charData = new BitArray(width.Value * height.Value);
                    charData.SetAll(false);
                    inCharLine = 0;
                }
                else if(inChar != null)
                {
                    for (int i = 0; i < width.Value; i++)
                    {
                        if (line[i] != ' ') charData.Set(i + inCharLine * width.Value, true);
                    }

                    if(++inCharLine >= height.Value)
                    {
                        data.Add((char)inChar.Value, BitArrayToByteArray(charData));
                        inChar = null;
                    }
                }
            }

            return new Font() { Chars = data.ToImmutableDictionary(), Width = width.Value, Height = height.Value };
        }

        public static byte[] BitArrayToByteArray(BitArray bits)
        {
            byte[] ret = new byte[(bits.Length - 1) / 8 + 1];
            bits.CopyTo(ret, 0);
            return ret;
        }
    }
}
