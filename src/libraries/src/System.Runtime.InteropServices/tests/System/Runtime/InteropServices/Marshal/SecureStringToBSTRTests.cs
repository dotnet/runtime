// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class SecureStringToBSTRTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("pizza")]
        [InlineData("pepperoni")]
        [InlineData("password")]
        [InlineData("P4ssw0rdAa1")]
        [InlineData("\uD800")]
        [InlineData("\uD800\uDC00")]
        [InlineData("\u1234")]
        [InlineData("\0")]
        [InlineData("abc\0def")]
        public void SecureStringToBSTR_InvokePtrToStringBSTR_ReturnsExpected(string data)
        {
            using (SecureString secureString = ToSecureString(data))
            {
                IntPtr ptr = Marshal.SecureStringToBSTR(secureString);
                try
                {
                    Assert.NotEqual(IntPtr.Zero, ptr);
                    Assert.Equal(data, Marshal.PtrToStringBSTR(ptr));
                }
                finally
                {
                    Marshal.ZeroFreeBSTR(ptr);
                }
            }
        }

        [Fact]
        public void SecureStringToBSTR_NullString_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => Marshal.SecureStringToBSTR(null));
        }

        [Fact]
        public void SecureStringToBSTR_DisposedString_ThrowsObjectDisposedException()
        {
            var secureString = new SecureString();
            secureString.Dispose();

            Assert.Throws<ObjectDisposedException>(() => Marshal.SecureStringToBSTR(secureString));
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
