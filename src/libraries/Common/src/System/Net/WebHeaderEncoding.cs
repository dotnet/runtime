// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System.Net
{
    // we use this static class as a helper class to encode/decode HTTP headers.
    // what we need is a 1-1 correspondence between a char in the range U+0000-U+00FF
    // and a byte in the range 0x00-0xFF (which is the range that can hit the network).
    // The Latin-1 encoding (ISO-88591-1) (GetEncoding(28591)) works for byte[] to string, but is a little slow.
    // It doesn't work for string -> byte[] because of best-fit-mapping problems.
    internal static class WebHeaderEncoding
    {
        internal static string GetString(byte[] bytes, int byteIndex, int byteCount)
        {
            if (byteCount < 1)
            {
                return string.Empty;
            }

            Debug.Assert(bytes != null && (uint)byteIndex <= (uint)bytes.Length && (uint)(byteIndex + byteCount) <= (uint)bytes.Length);

            return string.Create(byteCount, (bytes, byteIndex), static (buffer, state) =>
            {
                ReadOnlySpan<byte> source = state.bytes.AsSpan(state.byteIndex, buffer.Length);
                int lastBlockStart = source.Length - 7;
                int i = 0;

                for (; i < lastBlockStart; i += 8)
                {
                    buffer[i] = (char)source[i];
                    buffer[i + 1] = (char)source[i + 1];
                    buffer[i + 2] = (char)source[i + 2];
                    buffer[i + 3] = (char)source[i + 3];
                    buffer[i + 4] = (char)source[i + 4];
                    buffer[i + 5] = (char)source[i + 5];
                    buffer[i + 6] = (char)source[i + 6];
                    buffer[i + 7] = (char)source[i + 7];
                }

                for (; i < source.Length; i++)
                {
                    buffer[i] = (char)source[i];
                }
            });
        }

        internal static int GetByteCount(string myString) => myString.Length;

        internal static void GetBytes(string myString, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (myString.Length == 0)
            {
                return;
            }

            ReadOnlySpan<char> source = myString.AsSpan(charIndex, charCount);
            Span<byte> destination = bytes.AsSpan(byteIndex, charCount);

            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = (byte)source[i];
            }
        }

        internal static byte[] GetBytes(string myString)
        {
            byte[] bytes = new byte[myString.Length];
            if (myString.Length != 0)
            {
                GetBytes(myString, 0, myString.Length, bytes, 0);
            }
            return bytes;
        }
    }
}
