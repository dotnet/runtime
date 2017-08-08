// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public static partial class EncodingHelpers
    {
        static partial void GetByteCount_NetCoreApp(Encoding encoding, string chars, int index, int count, int expected)
        {
            // Use GetByteCount(string, int, int)
            Assert.Equal(expected, encoding.GetByteCount(chars, index, count));

            // Use GetByteCount(ReadOnlySpan<char> chars)
            Assert.Equal(expected, encoding.GetByteCount(chars.AsReadOnlySpan().Slice(index, count)));
        }

        static partial void GetBytes_NetCoreApp(Encoding encoding, string chars, int index, int count, byte[] expected)
        {
            // Use GetBytes(string, int, int)
            byte[] stringResultAdvanced = encoding.GetBytes(chars, index, count);
            VerifyGetBytes(stringResultAdvanced, 0, stringResultAdvanced.Length, new byte[expected.Length], expected);

            // Use GetBytes(ReadOnlySpan<char>, Span<byte>)
            Array.Clear(stringResultAdvanced, 0, stringResultAdvanced.Length);
            Assert.Equal(expected.Length, encoding.GetBytes(chars.AsReadOnlySpan().Slice(index, count), (Span<byte>)stringResultAdvanced));
            VerifyGetBytes(stringResultAdvanced, 0, stringResultAdvanced.Length, new byte[expected.Length], expected);
        }

        static partial void GetCharCount_NetCoreApp(Encoding encoding, byte[] bytes, int index, int count, int expected)
        {
            // Use GetCharCount(ReadOnlySpan<byte>)
            Assert.Equal(expected, encoding.GetCharCount(new ReadOnlySpan<byte>(bytes, index, count)));
        }

        static partial void VerifyGetChars_NetCoreApp(Encoding encoding, byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, char[] expectedChars)
        {
            // Use GetChars(ReadOnlySpan<byte>, Span<char>)
            char[] byteChars = (char[])chars.Clone();
            int charCount = encoding.GetChars(new ReadOnlySpan<byte>(bytes, byteIndex, byteCount), new Span<char>(byteChars).Slice(charIndex));
            VerifyGetChars(byteChars, charIndex, charCount, (char[])chars.Clone(), expectedChars);
            Assert.Equal(expectedChars.Length, charCount);
        }

        static partial void GetString_NetCoreApp(Encoding encoding, byte[] bytes, int index, int count, string expected)
        {
            // Use GetString(ReadOnlySpan<byte>)
            Assert.Equal(expected, encoding.GetString(new ReadOnlySpan<byte>(bytes, index, count)));
        }
    }
}
