// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class StringToCoTaskMemUTF8Tests
    {
        [Theory]
        [InlineData("FooBA\u0400R")]
        [InlineData("\u00C0nima\u0300l")]
        [InlineData("Test\uD803\uDD75Test")]
        [InlineData("\u0130")]
        [InlineData("\uD803\uDD75\uD803\uDD75\uD803\uDD75")]
        [InlineData("za\u0306\u01FD\u03B2\uD8FF\uDCFF")]
        [InlineData("za\u0306\u01FD\u03B2\uD8FF\uDCFF")]
        [InlineData("\u0023\u0025\u03a0\u03a3")]
        [InlineData("\u00C5")]
        [InlineData("\u0065\u0065\u00E1\u0065\u0065\u8000\u00E1\u0065\uD800\uDC00\u8000\u00E1\u0065\u0065\u0065")]
        [InlineData("\u00A4\u00D0aR|{AnGe\u00A3\u00A4")]
        [InlineData("\uD800\uDC00\uD800\uDC00\uD800\uDC00")]
        [InlineData("\uD800\uDC00\u0065\uD800\uDC00")]
        [InlineData("")]
        public void StringToCoTaskMemUTF8_PtrToStringUTF8_Roundtrips(string s)
        {
            int nullIndex = s.IndexOf('\0');
            byte[] expected = Encoding.UTF8.GetBytes(nullIndex == -1 ? s : s.Substring(0, nullIndex));

            IntPtr ptr = Marshal.StringToCoTaskMemUTF8(s);
            try
            {
                Assert.NotEqual(IntPtr.Zero, ptr);

                // Make sure the native memory is correctly laid out.
                for (int i = 0; i < s.Length; i++)
                {
                    Assert.Equal(expected[i], Marshal.ReadByte(IntPtr.Add(ptr, i)));
                }

                // Make sure the native memory roundtrips.
                Assert.Equal(s, Marshal.PtrToStringUTF8(ptr));
            }
            finally
            {
                Marshal.ZeroFreeCoTaskMemUTF8(ptr);
            }
        }

        [Fact]
        public void StringToCoTaskMemUni_NullString_ReturnsZero()
        {
            Assert.Equal(IntPtr.Zero, Marshal.StringToCoTaskMemUTF8(null));
        }
    }
}
