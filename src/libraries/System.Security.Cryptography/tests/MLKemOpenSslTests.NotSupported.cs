// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    public sealed class MLKemOpenSslTests
    {
        [Fact]
        public static void MLKemOpenSsl_NotSupportedOnNonUnixPlatforms()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new MLKemOpenSsl(new SafeEvpPKeyHandle()));
        }
    }
}
