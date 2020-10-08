using System.Globalization;
using System.Linq;

namespace System.Net.Quic.Tests
{
    internal static class HexHelpers
    {
        public static string ToHexString(ReadOnlySpan<byte> data)
        {
            return BitConverter.ToString(data.ToArray()).ToLower().Replace("-", "");
        }

        public static byte[] FromHexString(string hex)
        {
            // remove spaces and newlines
            hex = new string(hex.Where(c => !char.IsWhiteSpace(c)).ToArray());
            var buf = new byte[hex.Length / 2];

            FromHexStringInternal(hex, buf);

            return buf;
        }

        public static void FromHexString(string hex, Span<byte> target)
        {
            // remove spaces and newlines
            hex = new string(hex.Where(c => !char.IsWhiteSpace(c)).ToArray());
            FromHexStringInternal(hex, target);
        }

        private static void FromHexStringInternal(string hexNoWs, Span<byte> target)
        {
            if (hexNoWs.Length > target.Length * 2) throw new ArgumentException("Buffer too short");
            for (int i = 0; i < hexNoWs.Length / 2; i++)
                target[i] = byte.Parse(hexNoWs.AsSpan(i * 2, 2), NumberStyles.HexNumber);
        }
    }
}
