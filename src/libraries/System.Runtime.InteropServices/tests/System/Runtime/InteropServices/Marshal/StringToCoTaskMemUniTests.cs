// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class StringToCoTaskMemUniTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("pizza")]
        [InlineData("pepperoni")]
        [InlineData("password")]
        [InlineData("P4ssw0rdAa1")]
        [InlineData("\u1234")]
        [InlineData("\uD800")]
        [InlineData("\uD800\uDC00")]
        [InlineData("\0")]
        [InlineData("abc\0def")]
        public void StringToCoTaskMemUni_PtrToStringUni_Roundtrips(string s)
        {
            int nullIndex = s.IndexOf('\0');
            string expected = nullIndex == -1 ? s : s.Substring(0, nullIndex);

            IntPtr ptr = Marshal.StringToCoTaskMemUni(s);
            try
            {
                Assert.NotEqual(IntPtr.Zero, ptr);

                // Make sure the native memory is correctly laid out.
                for (int i = 0; i < s.Length; i++)
                {
                    Assert.Equal(s[i], (char)Marshal.ReadInt16(IntPtr.Add(ptr, i << 1)));
                }

                // Make sure the native memory roundtrips.
                Assert.Equal(expected, Marshal.PtrToStringUni(ptr));
                Assert.Equal(s, Marshal.PtrToStringUni(ptr, s.Length));
            }
            finally
            {
                Marshal.ZeroFreeCoTaskMemUnicode(ptr);
            }
        }

        [Fact]
        public void StringToCoTaskMemUni_NullString_ReturnsZero()
        {
            Assert.Equal(IntPtr.Zero, Marshal.StringToCoTaskMemUni(null));
        }
    }
}
