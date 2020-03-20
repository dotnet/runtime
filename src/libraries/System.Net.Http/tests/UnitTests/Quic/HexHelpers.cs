using System.Globalization;

namespace System.Net.Quic.Tests
{
    static internal class HexHelpers
    {
        public static string ToHexString(byte[] data)
        {
            return BitConverter.ToString(data).ToLower().Replace("-", "");
        }

        public static byte[] FromHexString(string hex)
        {
            var buf = new byte[hex.Length / 2];
            for (var i = 0; i < buf.Length; i++) buf[i] = Byte.Parse(hex.AsSpan(i * 2, 2), NumberStyles.HexNumber);

            return buf;
        }
    }
}