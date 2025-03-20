// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
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

        [ConditionalFact(nameof(NotSupportedOnPlatform))]
        public static void ThrowIfNotSupportedOnPlatform()
        {
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.GenerateKey(null));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportFromEncryptedPem(ReadOnlySpan<char>.Empty, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportFromEncryptedPem(ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportFromPem(ReadOnlySpan<char>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportPkcs8PrivateKey(ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSlhDsaPrivateSeed(null, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSlhDsaPublicKey(null, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSlhDsaSecretKey(null, ReadOnlySpan<byte>.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => SlhDsa.ImportSubjectPublicKeyInfo(ReadOnlySpan<byte>.Empty));
        }
    }
}
