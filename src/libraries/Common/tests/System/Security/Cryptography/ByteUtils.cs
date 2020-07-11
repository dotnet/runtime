// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            return Convert.FromHexString(hexString);
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
            return Convert.ToHexString(bytes);
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
