// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;

namespace Test.Cryptography
{
    internal static class ByteUtils
    {
        internal static byte[] AsciiBytes(string s)
        {
            byte[] bytes = new byte[s.Length];

            for (int i = 0; i < s.Length; i++)
            {
                bytes[i] = (byte)s[i];
            }

            return bytes;
        }

        internal static byte[] HexToByteArray(this string hexString)
        {
            byte[] bytes = new byte[hexString.Length / 2];

            for (int i = 0; i < hexString.Length; i += 2)
            {
                string s = hexString.Substring(i, 2);
                bytes[i / 2] = byte.Parse(s, NumberStyles.HexNumber, null);
            }

            return bytes;
        }

        internal static string ByteArrayToHex(this byte[] bytes)
        {
            return ByteArrayToHex((ReadOnlySpan<byte>)bytes);
        }

        internal static string ByteArrayToHex(this Span<byte> bytes)
        {
            return ByteArrayToHex((ReadOnlySpan<byte>)bytes);
        }

        internal static string ByteArrayToHex(this ReadOnlyMemory<byte> bytes)
        {
            return ByteArrayToHex(bytes.Span);
        }

        internal static string ByteArrayToHex(this ReadOnlySpan<byte> bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append($"{bytes[i]:X2}");
            }

            return builder.ToString();
        }

        internal static byte[] RepeatByte(byte b, int count)
        {
            byte[] value = new byte[count];

            for (int i = 0; i < count; i++)
            {
                value[i] = b;
            }

            return value;
        }
    }
}
