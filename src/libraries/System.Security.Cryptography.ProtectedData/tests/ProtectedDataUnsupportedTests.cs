// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

using Xunit;

namespace System.Security.Cryptography.ProtectedDataTests
{
    [PlatformSpecific(~TestPlatforms.Windows)]
    public static class ProtectedUnsupportedDataTests
    {
        [Theory]
        [InlineData(DataProtectionScope.LocalMachine)]
        [InlineData(DataProtectionScope.CurrentUser)]
        public static void Protect_PlatformNotSupported(DataProtectionScope scope)
        {
            Assert.Throws<PlatformNotSupportedException>(() => ProtectedData.Protect(null, null, scope));
        }

        [Theory]
        [InlineData(DataProtectionScope.LocalMachine)]
        [InlineData(DataProtectionScope.CurrentUser)]
        public static void Unprotect_PlatformNotSupported(DataProtectionScope scope)
        {
            Assert.Throws<PlatformNotSupportedException>(() => ProtectedData.Unprotect(null, null, scope));
        }
    }
}
