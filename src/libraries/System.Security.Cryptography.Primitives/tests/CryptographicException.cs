// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.Encryption.Tests.Asymmetric
{
    public static class CryptographicExceptionTests
    {
        [Fact]
        public static void Ctor()
        {
            string message = "Some Message";
            var inner = new FormatException(message);

            Assert.NotNull(new CryptographicException().Message);
            Assert.Equal(message, new CryptographicException(message).Message);
            Assert.Equal(message + " 12345", new CryptographicException(message + " {0}", "12345").Message);
            if (PlatformDetection.IsFullFramework)
            {
                Assert.Equal(unchecked((int)0x80070005), new CryptographicException(5).HResult);
            }
            else
            {
                Assert.Equal(5, new CryptographicException(5).HResult);
            }

            Assert.Same(inner, new CryptographicException(message, inner).InnerException);
            Assert.Equal(message, new CryptographicException(message, inner).Message);
        }
    }
}
