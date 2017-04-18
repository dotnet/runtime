// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class EncryptDecrypt : FileSystemTest
    {
        [Fact]
        public static void NullArg_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => File.Encrypt(null));
            AssertExtensions.Throws<ArgumentNullException>("path", () => File.Decrypt(null));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [Fact]
        public static void EncryptDecrypt_NotSupported()
        {
            Assert.Throws<PlatformNotSupportedException>(() => File.Encrypt("path"));
            Assert.Throws<PlatformNotSupportedException>(() => File.Decrypt("path"));
        }
    }
}
