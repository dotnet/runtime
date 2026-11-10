// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    public sealed class X25519DiffieHellmanOpenSslTests
    {
        [Fact]
        public static void X25519DiffieHellmanOpenSsl_NotSupportedOnNonUnixPlatforms()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new X25519DiffieHellmanOpenSsl(new SafeEvpPKeyHandle()));
        }
    }
}
