// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public class SlhDsaPlatformTests : SlhDsaTestsBase
    {
        [Fact]
        public static void IsSupportedOnPlatform()
        {
            Assert.Equal(SupportedOnPlatform, SlhDsa.IsSupported);
        }
    }
}
