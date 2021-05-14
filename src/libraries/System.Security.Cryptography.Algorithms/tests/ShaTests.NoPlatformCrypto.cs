// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotPlatformCryptoSupported))]
    public class ShaTests_NoPlatformCrypto
    {
        public static IEnumerable<object[]> ShaImpl()
        {
            yield return new object[]{ SHA1.Create() };
            yield return new object[]{ SHA256.Create() };
            yield return new object[]{ SHA384.Create() };
            yield return new object[]{ SHA512.Create() };
        }

        [Theory]
        [MemberData(nameof(ShaImpl))]
        public void Sha_PlatformNotSupportedException(HashAlgorithm hash)
        {
            Assert.Throws<PlatformNotSupportedException>(() => hash.ComputeHash(Array.Empty<byte>()));
        }
    }
}