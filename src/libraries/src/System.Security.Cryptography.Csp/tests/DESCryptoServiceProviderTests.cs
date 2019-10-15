// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Csp.Tests;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    public class DESCryptoServiceProviderTests
    {
        private static readonly byte[] KnownGoodKey = "87FF0737F868378F".HexToByteArray();

        [Fact]
        public static void TestShimProperties()
        {
            // Test the Unix shims; but also run on Windows to ensure behavior is consistent.
            using (var alg = new DESCryptoServiceProvider())
            {
                ShimHelpers.TestSymmetricAlgorithmProperties(alg, blockSize: 64, keySize: 64, key:KnownGoodKey);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Only Unix has _impl shim pattern
        public static void TestShimOverloads_Unix()
        {
            ShimHelpers.VerifyAllBaseMembersOverloaded(typeof(DESCryptoServiceProvider));
        }
    }
}
