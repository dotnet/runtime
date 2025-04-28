// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
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
