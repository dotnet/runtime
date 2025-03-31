// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public static class MLKemTests
    {
        [Fact]
        public static void Generate_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => MLKem.GenerateKey(null));
        }

        [Fact]
        public static void ImportPrivateSeed_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportPrivateSeed(null, new byte[MLKemAlgorithm.MLKem512.PrivateSeedSizeInBytes]));

            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportPrivateSeed(null, new ReadOnlySpan<byte>(new byte[MLKemAlgorithm.MLKem512.PrivateSeedSizeInBytes])));
        }

        [Fact]
        public static void ImportPrivateSeed_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportPrivateSeed(MLKemAlgorithm.MLKem512, null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPrivateSeed_WrongSize_Array(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, new byte[algorithm.PrivateSeedSizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, new byte[algorithm.PrivateSeedSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, Array.Empty<byte>()));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPrivateSeed_WrongSize_Span(MLKemAlgorithm algorithm)
        {
            byte[] seed = new byte[algorithm.PrivateSeedSizeInBytes + 1];

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, seed.AsSpan()));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, seed.AsSpan(0, seed.Length - 2)));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportPrivateSeed(algorithm, ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void ImportDecapsulationKey_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportDecapsulationKey(null, new byte[MLKemAlgorithm.MLKem512.DecapsulationKeySizeInBytes]));
        }

        [Fact]
        public static void ImportDecapsulationKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportDecapsulationKey(MLKemAlgorithm.MLKem512, null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportDecapsulationKey_WrongSize_Array(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, new byte[algorithm.DecapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, new byte[algorithm.DecapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, Array.Empty<byte>()));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportDecapsulationKey_WrongSize_Span(MLKemAlgorithm algorithm)
        {
            byte[] destination = new byte[algorithm.DecapsulationKeySizeInBytes + 1];

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, destination.AsSpan()));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, destination.AsSpan(0, destination.Length - 2)));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportDecapsulationKey(algorithm, ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void ImportEncapsulationKey_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () =>
                MLKem.ImportEncapsulationKey(null, new byte[MLKemAlgorithm.MLKem512.EncapsulationKeySizeInBytes]));
        }

        [Fact]
        public static void ImportEncapsulationKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportEncapsulationKey(MLKemAlgorithm.MLKem512, null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportEncapsulationKey_WrongSize_Array(MLKemAlgorithm algorithm)
        {
            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, new byte[algorithm.EncapsulationKeySizeInBytes + 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, new byte[algorithm.EncapsulationKeySizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, []));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportEncapsulationKey_WrongSize_Span(MLKemAlgorithm algorithm)
        {
            byte[] destination = new byte[algorithm.EncapsulationKeySizeInBytes + 1];

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, destination.AsSpan()));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, destination.AsSpan(0, destination.Length - 2)));

            AssertExtensions.Throws<ArgumentException>("source", () =>
                MLKem.ImportEncapsulationKey(algorithm, ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportSubjectPublicKeyInfo((byte[])null));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", static () =>
                MLKem.ImportPkcs8PrivateKey((byte[])null));
        }

        [Theory]
        [MemberData(nameof(Pkcs8PrivateKeySeedTestData))]
        public static void ImportPkcs8PrivateKey_Seed_Array(MLKemAlgorithm algorithm, byte[] pkcs8)
        {
            using MLKem kem = MLKem.ImportPkcs8PrivateKey(pkcs8);
            Assert.Equal(algorithm, kem.Algorithm);
            AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
        }

        [Theory]
        [MemberData(nameof(Pkcs8PrivateKeySeedTestData))]
        public static void ImportPkcs8PrivateKey_Seed_Span(MLKemAlgorithm algorithm, byte[] pkcs8)
        {
            using MLKem kem = MLKem.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(pkcs8));
            Assert.Equal(algorithm, kem.Algorithm);
            AssertExtensions.SequenceEqual(MLKemTestData.IncrementalSeed, kem.ExportPrivateSeed());
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ImportPkcs8PrivateKey_Seed_BadLength(MLKemAlgorithm algorithm)
        {
            byte[] encoded = Pkcs8Encode(algorithm.GetOid(), seed: new byte[algorithm.PrivateSeedSizeInBytes - 1]);
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(encoded));

            encoded = Pkcs8Encode(algorithm.GetOid(), seed: new byte[algorithm.PrivateSeedSizeInBytes + 1]);
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(encoded));
        }

        [Theory]
        [MemberData(nameof(Pkcs8PrivateKeySeedTestData))]
        public static void ImportPkcs8PrivateKey_Seed_Array_TrailingData(MLKemAlgorithm algorithm, byte[] pkcs8)
        {
            _ = algorithm;
            Array.Resize(ref pkcs8, pkcs8.Length + 1);
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(pkcs8));
        }

        [Theory]
        [MemberData(nameof(Pkcs8PrivateKeySeedTestData))]
        public static void ImportPkcs8PrivateKey_Seed_Span_TrailingData(MLKemAlgorithm algorithm, byte[] pkcs8)
        {
            _ = algorithm;
            Array.Resize(ref pkcs8, pkcs8.Length + 1);
            Assert.Throws<CryptographicException>(() => MLKem.ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(pkcs8)));
        }

        public static IEnumerable<object[]> Pkcs8PrivateKeySeedTestData
        {
            get
            {
                yield return [MLKemAlgorithm.MLKem512, MLKemTestData.IetfMlKem512PrivateKeySeed];
                yield return [MLKemAlgorithm.MLKem768, MLKemTestData.IetfMlKem768PrivateKeySeed];
                yield return [MLKemAlgorithm.MLKem1024, MLKemTestData.IetfMlKem1024PrivateKeySeed];
            }
        }

        private static byte[] Pkcs8Encode(
            string oid,
            ReadOnlyMemory<byte>? seed = default,
            ReadOnlyMemory<byte>? expandedKey = default)
        {
            AsnWriter writer = new(AsnEncodingRules.DER);
            using (writer.PushSequence())
            {
                writer.WriteInteger(0); //Version

                using (writer.PushSequence()) // AlgorithmIdentifier
                {
                    writer.WriteObjectIdentifier(oid);
                }

                if (seed.HasValue && expandedKey.HasValue)
                {
                    using (writer.PushSequence())
                    {
                        writer.WriteOctetString(seed.Value.Span);
                        writer.WriteOctetString(expandedKey.Value.Span);
                    }
                }
                else if (seed.HasValue)
                {
                    writer.WriteOctetString(seed.Value.Span, new Asn1Tag(TagClass.ContextSpecific, 0));
                }
                else if (expandedKey.HasValue)
                {
                    writer.WriteOctetString(expandedKey.Value.Span);
                }
                else
                {
                    Assert.Fail("Seed or ExpandedKey must be provided.");
                    return null;
                }
            }

            return writer.Encode();
        }

        private static string GetOid(this MLKemAlgorithm algorithm)
        {
            if (algorithm == MLKemAlgorithm.MLKem512)
            {
                return MLKemTestData.MlKem512Oid;
            }
            if (algorithm == MLKemAlgorithm.MLKem768)
            {
                return MLKemTestData.MlKem768Oid;
            }
            if (algorithm == MLKemAlgorithm.MLKem1024)
            {
                return MLKemTestData.MlKem1024Oid;
            }

            Assert.Fail("$Unexpected algorithm identifier {algorithm.Name}.");
            return null;
        }
    }
}
