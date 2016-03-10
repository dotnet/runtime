// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.ProtectedDataTests
{
    public static class ProtectedDataTests
    {
        [Fact]
        public static void RoundTrip()
        {
            RoundTrip(null);
            RoundTrip(new byte[] { 4, 5, 6 });
        }

        private static void RoundTrip(byte[] entropy)
        {
            foreach (DataProtectionScope scope in new DataProtectionScope[] { DataProtectionScope.CurrentUser, DataProtectionScope.LocalMachine })
            {
                byte[] plain = { 1, 2, 3 };
                byte[] encrypted = ProtectedData.Protect(plain, entropy, scope);
                Assert.NotEqual<byte>(plain, encrypted);
                byte[] recovered = ProtectedData.Unprotect(encrypted, entropy, scope);
                Assert.Equal<byte>(plain, recovered);
            }
        }

        [Fact]
        public static void NullEntropyEquivalence()
        {
            // Passing a zero-length array as entropy is equivalent to passing null as entropy.
            byte[] plain = { 1, 2, 3 };
            byte[] nullEntropy = { };
            byte[] encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            byte[] recovered = ProtectedData.Unprotect(encrypted, nullEntropy, DataProtectionScope.CurrentUser);
            Assert.Equal<byte>(plain, recovered);
        }

        [Fact]
        public static void NullEntropyEquivalence2()
        {
            // Passing a zero-length array as entropy is equivalent to passing null as entropy.
            byte[] plain = { 1, 2, 3 };
            byte[] nullEntropy = { };
            byte[] encrypted = ProtectedData.Protect(plain, nullEntropy, DataProtectionScope.CurrentUser);
            byte[] recovered = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            Assert.Equal<byte>(plain, recovered);
        }

        [Fact]
        public static void WrongEntropy()
        {
            // Passing a zero-length array as entropy is equivalent to passing null as entropy.
            byte[] entropy1 = { 4, 5, 6 };
            byte[] entropy2 = { 4, 5, 7 };
            WrongEntropy(null, entropy1);
            WrongEntropy(entropy1, null);
            WrongEntropy(entropy1, entropy2);
        }

        private static void WrongEntropy(byte[] entropy1, byte[] entropy2)
        {
            foreach (DataProtectionScope scope in new DataProtectionScope[] { DataProtectionScope.CurrentUser, DataProtectionScope.LocalMachine })
            {
                byte[] plain = { 1, 2, 3 };
                byte[] encrypted = ProtectedData.Protect(plain, entropy1, scope);
                Assert.ThrowsAny<CryptographicException>(() => ProtectedData.Unprotect(encrypted, entropy2, scope));
            }
        }
    }
}
