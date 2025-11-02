// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public static class SlhDsaOpenSslConstructionTests
    {
        [Fact]
        public static void SlhDsaOpenSsl_NotSupportedOnNonUnixPlatforms()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new SlhDsaOpenSsl(null));
            Assert.Throws<PlatformNotSupportedException>(() => new SlhDsaOpenSsl(new SafeEvpPKeyHandle()));
        }
    }
}
