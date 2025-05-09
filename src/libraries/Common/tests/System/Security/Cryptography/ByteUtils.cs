// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Security.Cryptography;
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
#if NET9_0_OR_GREATER
            return Convert.FromHexString(hexString);
#else
            byte[] bytes = new byte[hexString.Length / 2];

            for (int i = 0; i < hexString.Length; i += 2)
            {
                string s = hexString.Substring(i, 2);
                bytes[i / 2] = byte.Parse(s, NumberStyles.HexNumber, null);
            }

            return bytes;
#endif
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
#if NET9_0_OR_GREATER
            return Convert.ToHexString(bytes);
#else
            StringBuilder builder = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append($"{bytes[i]:X2}");
            }

            return builder.ToString();
#endif
        }

        internal static byte[] RepeatByte(byte b, int count)
        {

            byte[] value = new byte[count];

#if NET
            value.AsSpan().Fill(b);
#else
            for (int i = 0; i < count; i++)
            {
                value[i] = b;
            }
#endif

            return value;
        }

        internal static bool ContainsAnyExcept(this ReadOnlySpan<byte> bytes, byte value)
        {
#if NET9_0_OR_GREATER
            return bytes.ContainsAnyExcept(value);
#else
            foreach (byte b in bytes)
            {
                if (b != value)
                {
                    return true;
                }
            }

            return false;
#endif
        }

        internal static string PemEncode(string label, byte[] data)
        {
#if NET
            return PemEncoding.WriteString(label, data);
#else
            return
                $"-----BEGIN {label}-----\n{Convert.ToBase64String(data, Base64FormattingOptions.InsertLineBreaks)}\n-----END {label}-----";
#endif
        }
    }
}
