// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Security;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class SecureStringToGlobalAllocAnsiTests
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
        public void SecureStringToGlobalAllocAnsi_InvokePtrToStringAnsi_Roundtrips(string data)
        {
            string expectedFullString = new string(data.ToCharArray().Select(c => c > 0xFF ? '?' : c).ToArray());
            int nullIndex = expectedFullString.IndexOf('\0');
            string expectedParameterlessString = nullIndex == -1 ? expectedFullString : expectedFullString.Substring(0, nullIndex);

            using (SecureString secureString = ToSecureString(data))
            {
                IntPtr ptr = Marshal.SecureStringToGlobalAllocAnsi(secureString);
                try
                {
                    // Unix is incorrect with unicode chars.
                    bool containsNonAnsiChars = data.Any(c => c > 0xFF);
                    if (!containsNonAnsiChars || PlatformDetection.IsWindows)
                    {
                        Assert.Equal(expectedParameterlessString, Marshal.PtrToStringAnsi(ptr));
                        Assert.Equal(expectedFullString, Marshal.PtrToStringAnsi(ptr, data.Length));
                    }
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocAnsi(ptr);
                }
            }
        }

        [Fact]
        public void SecureStringToGlobalAllocAnsi_NullString_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("s", () => Marshal.SecureStringToGlobalAllocAnsi(null));
        }

        [Fact]
        public void SecureStringToGlobalAllocAnsi_DisposedString_ThrowsObjectDisposedException()
        {
            var secureString = new SecureString();
            secureString.Dispose();

            Assert.Throws<ObjectDisposedException>(() => Marshal.SecureStringToGlobalAllocAnsi(secureString));
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
