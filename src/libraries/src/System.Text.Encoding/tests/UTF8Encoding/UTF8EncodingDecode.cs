// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingDecode
    {
        public static IEnumerable<object[]> Decode_TestData()
        {
            // All ASCII chars
            for (char c = char.MinValue; c <= 0x7F; c++)
            {
                yield return new object[] { new byte[] { (byte)c }, 0, 1, c.ToString() };
                yield return new object[] { new byte[] { 97, (byte)c, 98 }, 1, 1, c.ToString() };
                yield return new object[] { new byte[] { 97, (byte)c, 98 }, 0, 3, "a" + c.ToString() + "b" };
            }

            // Mixture of ASCII and Unicode
            yield return new object[] { new byte[] { 70, 111, 111, 66, 65, 208, 128, 82 }, 0, 8, "FooBA\u0400R" };
            yield return new object[] { new byte[] { 195, 128, 110, 105, 109, 97, 204, 128, 108 }, 0, 9, "\u00C0nima\u0300l" };
            yield return new object[] { new byte[] { 84, 101, 115, 116, 240, 144, 181, 181, 84, 101, 115, 116 }, 0, 12, "Test\uD803\uDD75Test" };
            yield return new object[] { new byte[] { 0, 84, 101, 10, 115, 116, 0, 9, 0, 84, 15, 101, 115, 116, 0 }, 0, 15, "\0Te\nst\0\t\0T\u000Fest\0" };
            yield return new object[] { new byte[] { 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 181, 181 }, 0, 12, "\uD803\uDD75\uD803\uDD75\uD803\uDD75" };
            yield return new object[] { new byte[] { 196, 176 }, 0, 2, "\u0130" };
            yield return new object[] { new byte[] { 0x61, 0xCC, 0x8A }, 0, 3, "\u0061\u030A" };

            yield return new object[] { new byte[] { 0xC2, 0xA4, 0xC3, 0x90, 0x61, 0x52, 0x7C, 0x7B, 0x41, 0x6E, 0x47, 0x65, 0xC2, 0xA3, 0xC2, 0xA4 }, 0, 16, "\u00A4\u00D0aR|{AnGe\u00A3\u00A4" };

            yield return new object[] { new byte[] { 0x00, 0x7F }, 0, 2, "\u0000\u007F" };
            yield return new object[] { new byte[] { 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F, 0x00, 0x7F }, 0, 14, "\u0000\u007F\u0000\u007F\u0000\u007F\u0000\u007F\u0000\u007F\u0000\u007F\u0000\u007F" };

            yield return new object[] { new byte[] { 0xC2, 0x80, 0xDF, 0xBF }, 0, 4, "\u0080\u07FF" };
            yield return new object[] { new byte[] { 0xC2, 0x80, 0xDF, 0xBF, 0xC2, 0x80, 0xDF, 0xBF, 0xC2, 0x80, 0xDF, 0xBF, 0xC2, 0x80, 0xDF, 0xBF }, 0, 16, "\u0080\u07FF\u0080\u07FF\u0080\u07FF\u0080\u07FF" };

            yield return new object[] { new byte[] { 0xE0, 0xA0, 0x80, 0xE0, 0xBF, 0xBF }, 0, 6, "\u0800\u0FFF" };
            yield return new object[] { new byte[] { 0xE0, 0xA0, 0x80, 0xE0, 0xBF, 0xBF, 0xE0, 0xA0, 0x80, 0xE0, 0xBF, 0xBF, 0xE0, 0xA0, 0x80, 0xE0, 0xBF, 0xBF }, 0, 18, "\u0800\u0FFF\u0800\u0FFF\u0800\u0FFF" };

            yield return new object[] { new byte[] { 0xE1, 0x80, 0x80, 0xEC, 0xBF, 0xBF }, 0, 6, "\u1000\uCFFF" };
            yield return new object[] { new byte[] { 0xE1, 0x80, 0x80, 0xEC, 0xBF, 0xBF, 0xE1, 0x80, 0x80, 0xEC, 0xBF, 0xBF, 0xE1, 0x80, 0x80, 0xEC, 0xBF, 0xBF }, 0, 18, "\u1000\uCFFF\u1000\uCFFF\u1000\uCFFF" };

            yield return new object[] { new byte[] { 0xED, 0x80, 0x80, 0xED, 0x9F, 0xBF }, 0, 6, "\uD000\uD7FF" };
            yield return new object[] { new byte[] { 0xED, 0x80, 0x80, 0xED, 0x9F, 0xBF, 0xED, 0x80, 0x80, 0xED, 0x9F, 0xBF, 0xED, 0x80, 0x80, 0xED, 0x9F, 0xBF }, 0, 18, "\uD000\uD7FF\uD000\uD7FF\uD000\uD7FF" };

            yield return new object[] { new byte[] { 0xF0, 0x90, 0x80, 0x80, 0xF0, 0xBF, 0xBF, 0xBF }, 0, 8, "\uD800\uDC00\uD8BF\uDFFF" };
            yield return new object[] { new byte[] { 0xF0, 0x90, 0x80, 0x80, 0xF0, 0xBF, 0xBF, 0xBF, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0xBF, 0xBF, 0xBF }, 0, 16, "\uD800\uDC00\uD8BF\uDFFF\uD800\uDC00\uD8BF\uDFFF" };

            yield return new object[] { new byte[] { 0xF1, 0x80, 0x80, 0x80, 0xF3, 0xBF, 0xBF, 0xBF }, 0, 8, "\uD8C0\uDC00\uDBBF\uDFFF" };
            yield return new object[] { new byte[] { 0xF1, 0x80, 0x80, 0x80, 0xF3, 0xBF, 0xBF, 0xBF, 0xF1, 0x80, 0x80, 0x80, 0xF3, 0xBF, 0xBF, 0xBF }, 0, 16, "\uD8C0\uDC00\uDBBF\uDFFF\uD8C0\uDC00\uDBBF\uDFFF" };

            yield return new object[] { new byte[] { 0xF4, 0x80, 0x80, 0x80, 0xF4, 0x8F, 0xBF, 0xBF }, 0, 8, "\uDBC0\uDC00\uDBFF\uDFFF" };
            yield return new object[] { new byte[] { 0xF4, 0x80, 0x80, 0x80, 0xF4, 0x8F, 0xBF, 0xBF, 0xF4, 0x80, 0x80, 0x80, 0xF4, 0x8F, 0xBF, 0xBF }, 0, 16, "\uDBC0\uDC00\uDBFF\uDFFF\uDBC0\uDC00\uDBFF\uDFFF" };

            // Long ASCII strings
            yield return new object[] { new byte[] { 84, 101, 115, 116, 83, 116, 114, 105, 110, 103 }, 0, 10, "TestString" };
            yield return new object[] { new byte[] { 84, 101, 115, 116, 84, 101, 115, 116 }, 0, 8, "TestTest" };

            // Control codes
            yield return new object[] { new byte[] { 0x1F, 0x10, 0x00, 0x09 }, 0, 4, "\u001F\u0010\u0000\u0009" };
            yield return new object[] { new byte[] { 0x1F, 0x00, 0x10, 0x09 }, 0, 4, "\u001F\u0000\u0010\u0009" };
            yield return new object[] { new byte[] { 0x00, 0x1F, 0x10, 0x09 }, 0, 4, "\u0000\u001F\u0010\u0009" };

            // BOM
            yield return new object[] { new byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, 0, 4, "\uFEFF\u0041" };

            // U+FDD0 - U+FDEF
            yield return new object[] { new byte[] { 0xEF, 0xB7, 0x90, 0xEF, 0xB7, 0xAF }, 0, 6, "\uFDD0\uFDEF" };

            // 2 byte encoding
            yield return new object[] { new byte[] { 0xC3, 0xA1 }, 0, 2, "\u00E1" };
            yield return new object[] { new byte[] { 0xC3, 0x85 }, 0, 2, "\u00C5" };

            // 3 byte encoding
            yield return new object[] { new byte[] { 0xE8, 0x80, 0x80 }, 0, 3, "\u8000" };
            yield return new object[] { new byte[] { 0xE2, 0x84, 0xAB }, 0, 3, "\u212B" };

            // Surrogate pairs
            yield return new object[] { new byte[] { 240, 144, 128, 128 }, 0, 4, "\uD800\uDC00" };
            yield return new object[] { new byte[] { 97, 240, 144, 128, 128, 98 }, 0, 6, "a\uD800\uDC00b" };

            yield return new object[] { new byte[] { 0xF0, 0x90, 0x8F, 0xBF }, 0, 4, "\uD800\uDFFF" };
            yield return new object[] { new byte[] { 0xF4, 0x8F, 0xB0, 0x80 }, 0, 4, "\uDBFF\uDC00" };
            yield return new object[] { new byte[] { 0xF4, 0x8F, 0xBF, 0xBF }, 0, 4, "\uDBFF\uDFFF" };

            yield return new object[] { new byte[] { 0xF3, 0xB0, 0x80, 0x80 }, 0, 4, "\uDB80\uDC00" };

            // High BMP non-chars
            yield return new object[] { new byte[] { 239, 191, 189 }, 0, 3, "\uFFFD" };
            yield return new object[] { new byte[] { 239, 191, 190 }, 0, 3, "\uFFFE" };
            yield return new object[] { new byte[] { 239, 191, 191 }, 0, 3, "\uFFFF" };
            yield return new object[] { new byte[] { 0xEF, 0xBF, 0xAE }, 0, 3, "\uFFEE" };

            yield return new object[] { new byte[] { 0xEE, 0x80, 0x80, 0xEF, 0xBF, 0xBF, 0xEE, 0x80, 0x80, 0xEF, 0xBF, 0xBF, 0xEE, 0x80, 0x80, 0xEF, 0xBF, 0xBF }, 0, 18, "\uE000\uFFFF\uE000\uFFFF\uE000\uFFFF" };

            // Empty strings
            yield return new object[] { new byte[0], 0, 0, string.Empty };
            yield return new object[] { new byte[10], 10, 0, string.Empty };
            yield return new object[] { new byte[10], 0, 0, string.Empty };
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/20525", TargetFrameworkMonikers.UapAot)]
        public void Decode(byte[] bytes, int index, int count, string expected)
        {
            EncodingHelpers.Decode(new UTF8Encoding(true, false), bytes, index, count, expected);
            EncodingHelpers.Decode(new UTF8Encoding(false, false), bytes, index, count, expected);

            EncodingHelpers.Decode(new UTF8Encoding(false, true), bytes, index, count, expected);
            EncodingHelpers.Decode(new UTF8Encoding(true, true), bytes, index, count, expected);
        }
        
        public static IEnumerable<object[]> Decode_InvalidBytes_TestData()
        {
            yield return new object[] { new byte[] { 196, 84, 101, 115, 116, 196, 196, 196, 176, 176, 84, 101, 115, 116, 176 }, 0, 15, "\uFFFDTest\uFFFD\uFFFD\u0130\uFFFDTest\uFFFD" };
            yield return new object[] { new byte[] { 240, 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 240 }, 0, 12, "\uFFFD\uD803\uDD75\uD803\uDD75\uFFFD\uFFFD" };

            // Invalid surrogate bytes
            byte[] validSurrogateBytes = new byte[] { 240, 144, 128, 128 };
            yield return new object[] { validSurrogateBytes, 0, 3, "\uFFFD" };
            yield return new object[] { validSurrogateBytes, 1, 3, "\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { validSurrogateBytes, 0, 2, "\uFFFD" };
            yield return new object[] { validSurrogateBytes, 1, 2, "\uFFFD\uFFFD" };
            yield return new object[] { validSurrogateBytes, 2, 2, "\uFFFD\uFFFD" };
            yield return new object[] { validSurrogateBytes, 2, 1, "\uFFFD" };

            yield return new object[] { new byte[] { 0xED, 0xA0, 0x80 }, 0, 3, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xED, 0xAF, 0xBF }, 0, 3, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xED, 0xB0, 0x80 }, 0, 3, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xED, 0xBF, 0xBF }, 0, 3, "\uFFFD\uFFFD" };

            // Invalid surrogate pair (low/low, high/high, low/high)
            yield return new object[] { new byte[] { 0xED, 0xA0, 0x80, 0xED, 0xAF, 0xBF }, 0, 6, "\uFFFD\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xED, 0xB0, 0x80, 0xED, 0xB0, 0x80 }, 0, 6, "\uFFFD\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xED, 0xA0, 0x80, 0xED, 0xA0, 0x80 }, 0, 6, "\uFFFD\uFFFD\uFFFD\uFFFD" };

            // Too high scalar value in surrogates
            yield return new object[] { new byte[] { 0xED, 0xA0, 0x80, 0xEE, 0x80, 0x80 }, 0, 6, "\uFFFD\uFFFD\uE000" };
            yield return new object[] { new byte[] { 0xF4, 0x90, 0x80, 0x80 }, 0, 4, "\uFFFD\uFFFD\uFFFD" };

            // These are examples of overlong sequences. This can cause security
            // vulnerabilities (e.g. MS00-078) so it is important we parse these as invalid.
            yield return new object[] { new byte[] { 0xC0 }, 0, 1, "\uFFFD" };
            yield return new object[] { new byte[] { 0xC0, 0xAF }, 0, 2, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xE0, 0x80, 0xBF }, 0, 3, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xF0, 0x80, 0x80, 0xBF }, 0, 4, "\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xF8, 0x80, 0x80, 0x80, 0xBF }, 0, 5, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xFC, 0x80, 0x80, 0x80, 0x80, 0xBF }, 0, 6, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0xC0, 0xBF }, 0, 2, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xE0, 0x9C, 0x90 }, 0, 3, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xF0, 0x8F, 0xA4, 0x80 }, 0, 4, "\uFFFD\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0xEF, 0x41 }, 0, 2, "\uFFFD\u0041" };
            yield return new object[] { new byte[] { 0xEF, 0xBF, 0xAE }, 0, 1, "\uFFFD" };
            yield return new object[] { new byte[] { 0xEF, 0xBF, 0x41 }, 0, 3, "\uFFFD\u0041" };
            yield return new object[] { new byte[] { 0xEF, 0xBF, 0x61 }, 0, 3, "\uFFFD\u0061" };
            yield return new object[] { new byte[] { 0xEF, 0xBF, 0xEF, 0xBF, 0xAE }, 0, 5, "\uFFFD\uFFEE" };
            yield return new object[] { new byte[] { 0xEF, 0xBF, 0xC0, 0xBF }, 0, 4, "\uFFFD\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0xF0, 0xC4, 0x80 }, 0, 3, "\uFFFD\u0100" };

            yield return new object[] { new byte[] { 176 }, 0, 1, "\uFFFD" };
            yield return new object[] { new byte[] { 196 }, 0, 1, "\uFFFD" };

            yield return new object[] { new byte[] { 0xA4, 0xD0, 0x61, 0x52, 0x7C, 0x7B, 0x41, 0x6E, 0x47, 0x65, 0xA3, 0xA4 }, 0, 12, "\uFFFD\uFFFD\u0061\u0052\u007C\u007B\u0041\u006E\u0047\u0065\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0xA3 }, 0, 1, "\uFFFD" };
            yield return new object[] { new byte[] { 0xA3, 0xA4 }, 0, 2, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0x65, 0xA3, 0xA4 }, 0, 3, "\u0065\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0x47, 0x65, 0xA3, 0xA4 }, 0, 4, "\u0047\u0065\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xA4, 0xD0, 0x61, 0xA3, 0xA4 }, 0, 5, "\uFFFD\uFFFD\u0061\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xA4, 0xD0, 0x61, 0xA3 }, 0, 4, "\uFFFD\uFFFD\u0061\uFFFD" };
            yield return new object[] { new byte[] { 0xD0, 0x61, 0xA3 }, 0, 3, "\uFFFD\u0061\uFFFD" };
            yield return new object[] { new byte[] { 0xA4, 0x61, 0xA3 }, 0, 3, "\uFFFD\u0061\uFFFD" };
            yield return new object[] { new byte[] { 0xD0, 0x61, 0x52, 0xA3 }, 0, 4, "\uFFFD\u0061\u0052\uFFFD" };
                        
            yield return new object[] { new byte[] { 0xAA }, 0, 1, "\uFFFD" };
            yield return new object[] { new byte[] { 0xAA, 0x41 }, 0, 2, "\uFFFD\u0041" };

            yield return new object[] { new byte[] { 0xEF, 0xFF, 0xEE }, 0, 3, "\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xEF, 0xFF, 0xAE }, 0, 3, "\uFFFD\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0x80, 0x90, 0xA0, 0xB0, 0xC1 }, 0, 5, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x80, 0x90, 0xA0, 0xB0, 0xC1 }, 0, 15, "\u007F\u007F\u007F\u007F\u007F\u007F\u007F\u007F\u007F\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0x80, 0x90, 0xA0, 0xB0, 0xC1, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F, 0x7F }, 0, 15, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\u007F\u007F\u007F\u007F\u007F\u007F\u007F\u007F\u007F" };

            yield return new object[] { new byte[] { 0xC2, 0x7F, 0xC2, 0xC0, 0xDF, 0x7F, 0xDF, 0xC0 }, 0, 8, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xC2, 0xDF }, 0, 2, "\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0x80, 0x80, 0xC1, 0x80, 0xC1, 0xBF }, 0, 6, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xC2, 0x7F, 0xC2, 0xC0, 0x7F, 0x7F, 0x7F, 0x7F, 0xC3, 0xA1, 0xDF, 0x7F, 0xDF, 0xC0 }, 0, 14, "\uFFFD\u007F\uFFFD\uFFFD\u007F\u007F\u007F\u007F\u00E1\uFFFD\u007F\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0xE0, 0xA0, 0x7F, 0xE0, 0xA0, 0xC0, 0xE0, 0xBF, 0x7F, 0xE0, 0xBF, 0xC0 }, 0, 12, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xE0, 0x9F, 0x80, 0xE0, 0xC0, 0x80, 0xE0, 0x9F, 0xBF, 0xE0, 0xC0, 0xBF }, 0, 12, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xE0, 0xA0, 0x7F, 0xE0, 0xA0, 0xC0, 0x7F, 0xE0, 0xBF, 0x7F, 0xC3, 0xA1, 0xE0, 0xBF, 0xC0 }, 0, 15, "\uFFFD\u007F\uFFFD\uFFFD\u007F\uFFFD\u007F\u00E1\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0xE1, 0x80, 0x7F, 0xE1, 0x80, 0xC0, 0xE1, 0xBF, 0x7F, 0xE1, 0xBF, 0xC0, 0xEC, 0x80, 0x7F, 0xEC, 0x80, 0xC0, 0xEC, 0xBF, 0x7F, 0xEC, 0xBF, 0xC0 }, 0, 24, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xE1, 0x7F, 0x80, 0xE1, 0xC0, 0x80, 0xE1, 0x7F, 0xBF, 0xE1, 0xC0, 0xBF, 0xEC, 0x7F, 0x80, 0xEC, 0xC0, 0x80, 0xEC, 0x7F, 0xBF, 0xEC, 0xC0, 0xBF }, 0, 24, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0xED, 0x80, 0x7F, 0xED, 0x80, 0xC0, 0xED, 0x9F, 0x7F, 0xED, 0x9F, 0xC0 }, 0, 12, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xED, 0x7F, 0x80, 0xED, 0xA0, 0x80, 0xED, 0x7F, 0xBF, 0xED, 0xA0, 0xBF }, 0, 12, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xED, 0x7F, 0x80, 0xED, 0xA0, 0x80, 0xE8, 0x80, 0x80, 0xED, 0x7F, 0xBF, 0xED, 0xA0, 0xBF }, 0, 15, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u8000\uFFFD\u007F\uFFFD\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0xEE, 0x80, 0x7F, 0xEE, 0x80, 0xC0, 0xEE, 0xBF, 0x7F, 0xEE, 0xBF, 0xC0, 0xEF, 0x80, 0x7F, 0xEF, 0x80, 0xC0, 0xEF, 0xBF, 0x7F, 0xEF, 0xBF, 0xC0 }, 0, 24, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xEE, 0x7F, 0x80, 0xEE, 0xC0, 0x80, 0xEE, 0x7F, 0xBF, 0xEE, 0xC0, 0xBF, 0xEF, 0x7F, 0x80, 0xEF, 0xC0, 0x80, 0xEF, 0x7F, 0xBF, 0xEF, 0xC0, 0xBF }, 0, 24, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0xF0, 0x90, 0x80, 0x7F, 0xF0, 0x90, 0x80, 0xC0, 0xF0, 0xBF, 0xBF, 0x7F, 0xF0, 0xBF, 0xBF, 0xC0 }, 0, 16, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xF0, 0x90, 0x7F, 0x80, 0xF0, 0x90, 0xC0, 0x80, 0xF0, 0x90, 0x7F, 0xBF, 0xF0, 0x90, 0xC0, 0xBF }, 0, 16, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xF0, 0x8F, 0x80, 0x80, 0xF0, 0xC0, 0x80, 0x80, 0xF0, 0x8F, 0xBF, 0xBF, 0xF0, 0xC0, 0xBF, 0xBF }, 0, 16, "\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0xF1, 0x80, 0x80, 0x7F, 0xF1, 0x80, 0x80, 0xC0, 0xF1, 0xBF, 0xBF, 0x7F, 0xF1, 0xBF, 0xBF, 0xC0, 0xF3, 0x80, 0x80, 0x7F, 0xF3, 0x80, 0x80, 0xC0, 0xF3, 0xBF, 0xBF, 0x7F, 0xF3, 0xBF, 0xBF, 0xC0 }, 0, 32, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xF1, 0x80, 0x7F, 0x80, 0xF1, 0x80, 0xC0, 0x80, 0xF1, 0x80, 0x7F, 0xBF, 0xF1, 0x80, 0xC0, 0xBF, 0xF3, 0x80, 0x7F, 0x80, 0xF3, 0x80, 0xC0, 0x80, 0xF3, 0x80, 0x7F, 0xBF, 0xF3, 0x80, 0xC0, 0xBF }, 0, 32, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xF1, 0x7F, 0x80, 0x80, 0xF1, 0xC0, 0x80, 0x80, 0xF1, 0x7F, 0xBF, 0xBF, 0xF1, 0xC0, 0xBF, 0xBF, 0xF3, 0x7F, 0x80, 0x80, 0xF3, 0xC0, 0x80, 0x80, 0xF3, 0x7F, 0xBF, 0xBF, 0xF3, 0xC0, 0xBF, 0xBF }, 0, 32, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD" };

            yield return new object[] { new byte[] { 0xF4, 0x80, 0x80, 0x7F, 0xF4, 0x80, 0x80, 0xC0, 0xF4, 0x8F, 0xBF, 0x7F, 0xF4, 0x8F, 0xBF, 0xC0 }, 0, 16, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xF4, 0x80, 0x7F, 0x80, 0xF4, 0x80, 0xC0, 0x80, 0xF4, 0x80, 0x7F, 0xBF, 0xF4, 0x80, 0xC0, 0xBF }, 0, 16, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD" };
            yield return new object[] { new byte[] { 0xF4, 0x7F, 0x80, 0x80, 0xF4, 0x90, 0x80, 0x80, 0xF4, 0x7F, 0xBF, 0xBF, 0xF4, 0x90, 0xBF, 0xBF }, 0, 16, "\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD\u007F\uFFFD\uFFFD\uFFFD\uFFFD\uFFFD" };
        }

        [Theory]
        [MemberData(nameof(Decode_InvalidBytes_TestData))]
        public static void Decode_InvalidBytes(byte[] bytes, int index, int count, string expected)
        {
            EncodingHelpers.Decode(new UTF8Encoding(true, false), bytes, index, count, expected);
            EncodingHelpers.Decode(new UTF8Encoding(false, false), bytes, index, count, expected);

            NegativeEncodingTests.Decode_Invalid(new UTF8Encoding(false, true), bytes, index, count);
            NegativeEncodingTests.Decode_Invalid(new UTF8Encoding(true, true), bytes, index, count);
        }
    }
}
