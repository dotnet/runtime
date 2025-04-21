// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    /// <summary>
    /// Basic constructor validation for <see cref="SlhDsaOpenSsl"/> that is relevant even when OpenSSL doesn't support SLH-DSA.
    /// For more comprehensive tests that rely on OpenSSL support for SLH-DSA, see <see cref="SlhDsaOpenSslTests"/>.
    /// </summary>
    public static class SlhDsaOpenSslConstructionTests
    {
        [Fact]
        public static void SlhDsaOpenSsl_Ctor_ArgValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("pkeyHandle", static () => new SlhDsaOpenSsl(null));
        }

        [Fact]
        public static void SlhDsaOpenSsl_Ctor_InvalidHandle()
        {
            AssertExtensions.Throws<ArgumentException>("pkeyHandle", static () => new SlhDsaOpenSsl(new SafeEvpPKeyHandle()));
        }
    }
}
