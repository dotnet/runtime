// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class SecureStringToCoTaskMemUnicodeTests
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
        public void SecureStringToCoTaskMemUnicode_PtrToStringUni_Roundtrips(string s)
        {
            int nullIndex = s.IndexOf('\0');
            string expected = nullIndex == -1 ? s : s.Substring(0, nullIndex);

            using (SecureString secureString = ToSecureString(s))
            {
                IntPtr ptr = Marshal.SecureStringToCoTaskMemUnicode(secureString);
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
        }

        [Fact]
        public void SecureStringToCoTaskMemUnicode_NullString_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => Marshal.SecureStringToCoTaskMemUnicode(null));
        }

        [Fact]
        public void SecureStringToCoTaskMemUnicode_DisposedString_ThrowsObjectDisposedException()
        {
            var secureString = new SecureString();
            secureString.Dispose();

            Assert.Throws<ObjectDisposedException>(() => Marshal.SecureStringToCoTaskMemUnicode(secureString));
        }

        private static SecureString ToSecureString(string data)
        {
            var str = new SecureString();
            foreach (char c in data)
            {
                str.AppendChar(c);
            }
            str.MakeReadOnly();
            return str;
        }
    }
}
