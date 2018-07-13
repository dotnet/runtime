// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class StringToCoTaskMemUTF8Tests
    {
        public static IEnumerable<object[]> StringToCoTaskMemUTF8_TestData()
        {
            yield return new object[] { "FooBA\u0400R", new byte[] { 70, 111, 111, 66, 65, 208, 128, 82 } };

            yield return new object[] { "\u00C0nima\u0300l", new byte[] { 195, 128, 110, 105, 109, 97, 204, 128, 108 } };

            yield return new object[] { "Test\uD803\uDD75Test", new byte[] { 84, 101, 115, 116, 240, 144, 181, 181, 84, 101, 115, 116 } };

            yield return new object[] { "\u0130", new byte[] { 196, 176 } };

            yield return new object[] { "\uD803\uDD75\uD803\uDD75\uD803\uDD75", new byte[] { 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 181, 181 } };

            yield return new object[] { "za\u0306\u01FD\u03B2\uD8FF\uDCFF", new byte[] { 122, 97, 204, 134, 199, 189, 206, 178, 241, 143, 179, 191 } };

            yield return new object[] { "za\u0306\u01FD\u03B2\uD8FF\uDCFF", new byte[] { 206, 178, 241, 143, 179, 191 } };

            yield return new object[] { "\u0023\u0025\u03a0\u03a3", new byte[] { 37, 206, 160 } };

            yield return new object[] { "\u00C5", new byte[] { 0xC3, 0x85 } };

            yield return new object[] { "\u0065\u0065\u00E1\u0065\u0065\u8000\u00E1\u0065\uD800\uDC00\u8000\u00E1\u0065\u0065\u0065", new byte[] { 0x65, 0x65, 0xC3, 0xA1, 0x65, 0x65, 0xE8, 0x80, 0x80, 0xC3, 0xA1, 0x65, 0xF0, 0x90, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xC3, 0xA1, 0x65, 0x65, 0x65 } };

            yield return new object[] { "\u00A4\u00D0aR|{AnGe\u00A3\u00A4", new byte[] { 0xC2, 0xA4, 0xC3, 0x90, 0x61, 0x52, 0x7C, 0x7B, 0x41, 0x6E, 0x47, 0x65, 0xC2, 0xA3, 0xC2, 0xA4 } };

            yield return new object[] { "\uD800\uDC00\uD800\uDC00\uD800\uDC00", new byte[] { 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80 } };

            yield return new object[] { "\uD800\uDC00\u0065\uD800\uDC00", new byte[] { 0xF0, 0x90, 0x80, 0x80, 0x65, 0xF0, 0x90, 0x80, 0x80 } };

            yield return new object[] { string.Empty, new byte[] { } };

            yield return new object[] { null, new byte[] {}};
        }

        [Theory]
        [MemberData(nameof(StringToCoTaskMemUTF8_TestData))]
        public void StringToCoTaskMemUTF8_PtrToStringUTF8_Roundtrips(string chars, byte[] expected)
        {
            IntPtr pString = IntPtr.Zero;
            try
            {
                pString = Marshal.StringToCoTaskMemUTF8(chars);
                Assert.Equal(chars, Marshal.PtrToStringUTF8(pString));
            }
            finally
            {
                if (pString != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUTF8(pString);
                }
            }
        }
    }
}
