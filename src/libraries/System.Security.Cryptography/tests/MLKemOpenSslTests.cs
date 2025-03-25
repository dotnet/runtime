// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsOpenSsl3_5))]
    public sealed class MLKemOpenSslTests : MLKemBaseTests
    {
        public override MLKem Generate(MLKemAlgorithm algorithm)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(algorithm.Name);
            return new MLKemOpenSsl(key);
        }

        public override MLKem ImportPrivateSeed(MLKemAlgorithm algorithm, ReadOnlySpan<byte> seed)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(algorithm.Name, seed);
            return new MLKemOpenSsl(key);
        }

        public override MLKem ImportDecapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(algorithm.Name, source, privateKey: true);
            return new MLKemOpenSsl(key);
        }
    }

    public abstract class MLKemBaseTests
    {
        public abstract MLKem Generate(MLKemAlgorithm algorithm);
        public abstract MLKem ImportPrivateSeed(MLKemAlgorithm algorithm, ReadOnlySpan<byte> seed);
        public abstract MLKem ImportDecapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source);

        [Theory]
        [MemberData(nameof(MLKemAlgorithms))]
        public void ExportPrivateSeed_Roundtrip(MLKemAlgorithm algorithm)
        {
            using MLKem kem = Generate(algorithm);
            Assert.Equal(algorithm, kem.Algorithm);

            Span<byte> seed = new byte[algorithm.PrivateSeedSizeInBytes];

            kem.ExportPrivateSeed(seed);
            byte[] allocatedSeed1 = kem.ExportPrivateSeed();
            Assert.True(seed.ContainsAnyExcept((byte)0));
            AssertExtensions.SequenceEqual(seed, allocatedSeed1.AsSpan());

            using MLKem kem2 = ImportPrivateSeed(algorithm, seed);
            Span<byte> seed2 = new byte[algorithm.PrivateSeedSizeInBytes];
            kem2.ExportPrivateSeed(seed2);
            byte[] allocatedSeed2 = kem2.ExportPrivateSeed();
            AssertExtensions.SequenceEqual(seed, seed2);
            AssertExtensions.SequenceEqual(seed2, allocatedSeed2.AsSpan());
        }

        [Fact]
        public static void ExportPrivateSeed_OnlyHasDecapsulationKey()
        {
            using MLKem kem = MLKem.ImportDecapsulationKey(MLKemAlgorithm.MLKem512, MLKemTestData.MLKem512DecapsulationKey);

            Assert.Throws<CryptographicException>(() => kem.ExportPrivateSeed());
            Assert.Throws<CryptographicException>(() => kem.ExportPrivateSeed(
                new byte[vector.Algorithm.PrivateSeedSizeInBytes]));

        }

        public static IEnumerable<object[]> MLKemAlgorithms
        {
            get
            {
                return
                [
                    [ MLKemAlgorithm.MLKem512 ],
                    [ MLKemAlgorithm.MLKem768 ],
                    [ MLKemAlgorithm.MLKem1024 ],
                ];
            }
        }
    }
}
