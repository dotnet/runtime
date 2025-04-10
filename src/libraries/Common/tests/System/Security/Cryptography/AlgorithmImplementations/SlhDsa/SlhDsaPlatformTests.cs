// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public class SlhDsaPlatformTests
    {
        [Fact]
        public static void IsSupported_AgreesWithPlatform()
        {
            Assert.Equal(PlatformSupportsSlhDsa(), SlhDsa.IsSupported);
        }

        private static bool PlatformSupportsSlhDsa()
        {
            if (PlatformDetection.IsOpenSsl3_5)
            {
                return true;
            }

            return false;
        }
    }
}
