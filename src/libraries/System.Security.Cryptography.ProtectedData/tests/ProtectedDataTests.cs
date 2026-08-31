// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.ProtectedDataTests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public abstract class ProtectedDataTests
    {
        protected abstract byte[] Protect(byte[] plain, byte[]? entropy, DataProtectionScope scope);
        protected abstract byte[] Unprotect(byte[] encrypted, byte[]? entropy, DataProtectionScope scope);

        [Theory]
        [InlineData(null)]
        [InlineData(new byte[] { 4, 5, 6 })]
        public void RoundTrip(byte[]? entropy)
        {
            foreach (DataProtectionScope scope in new[] { DataProtectionScope.CurrentUser, DataProtectionScope.LocalMachine })
            {
                byte[] plain = [1, 2, 3];
                byte[] encrypted = Protect(plain, entropy, scope);
                Assert.NotEqual(plain, encrypted);
                byte[] recovered = Unprotect(encrypted, entropy, scope);
                Assert.Equal(plain, recovered);
            }
        }

        [Theory]
        [InlineData(DataProtectionScope.CurrentUser, false)]
        [InlineData(DataProtectionScope.CurrentUser, true)]
        [InlineData(DataProtectionScope.LocalMachine, false)]
        [InlineData(DataProtectionScope.LocalMachine, true)]
        public void ProtectEmptyData(DataProtectionScope scope, bool useEntropy)
        {
            // Use new byte[0] instead of Array.Empty<byte> to prove the implementation
            // isn't using reference equality
            byte[] data = [];
            byte[] entropy = useEntropy ? [68, 65, 72, 72, 75] : null;
            byte[] encrypted = Protect(data, entropy, scope);

            Assert.NotEqual(data, encrypted);
            byte[] recovered = Unprotect(encrypted, entropy, scope);
            Assert.Equal(data, recovered);
        }

        [Theory]
        // Passing a zero-length array as entropy is equivalent to passing null as entropy.
        [InlineData(null, new byte[] { 4, 5, 6 })]
        [InlineData(new byte[] { 4, 5, 6 }, null)]
        [InlineData(new byte[] { 4, 5, 6 }, new byte[] { 4, 5, 7 })]
        public void WrongEntropy(byte[]? entropy1, byte[]? entropy2)
        {
            foreach (DataProtectionScope scope in new[] { DataProtectionScope.CurrentUser, DataProtectionScope.LocalMachine })
            {
                byte[] plain = [1, 2, 3];
                byte[] encrypted = Protect(plain, entropy1, scope);
                Assert.ThrowsAny<CryptographicException>(() => Unprotect(encrypted, entropy2, scope));
            }
        }
    }
}
