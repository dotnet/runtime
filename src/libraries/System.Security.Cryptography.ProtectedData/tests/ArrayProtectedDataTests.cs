// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.ProtectedDataTests
{
    public class ArrayProtectedDataTests : ProtectedDataTests
    {
        [Fact]
        public void NullEntropyEquivalence()
        {
            // Passing a zero-length array as entropy is equivalent to passing null as entropy.
            byte[] plain = [1, 2, 3];
            byte[] emptyEntropy = [];
            byte[] encrypted = Protect(plain, null, DataProtectionScope.CurrentUser);
            byte[] recovered = Unprotect(encrypted, emptyEntropy, DataProtectionScope.CurrentUser);
            Assert.Equal(plain, recovered);
        }

        [Fact]
        public void NullEntropyEquivalence2()
        {
            // Passing a zero-length array as entropy is equivalent to passing null as entropy.
            byte[] plain = [1, 2, 3];
            byte[] emptyEntropy = [];
            byte[] encrypted = Protect(plain, emptyEntropy, DataProtectionScope.CurrentUser);
            byte[] recovered = Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            Assert.Equal(plain, recovered);
        }

        protected override byte[] Protect(byte[] plain, byte[]? entropy, DataProtectionScope scope) => ProtectedData.Protect(plain, entropy, scope);
        protected override byte[] Unprotect(byte[] encrypted, byte[]? entropy, DataProtectionScope scope) => ProtectedData.Unprotect(encrypted, entropy, scope);
    }
}
