// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    public static class MLKemContractTests
    {
        private static readonly PbeParameters s_aes128Pbe = new(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 2);

        [Fact]
        public static void Constructor_ThrowsForNullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => new MLKemContract(null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Constructor_SetsAlgorithmProperty(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);
            Assert.Equal(algorithm, kem.Algorithm);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        public static void Dispose_OnDisposing(int disposeCalls)
        {
            int count = 0;
            MLKemContract kem = new(MLKemAlgorithm.MLKem512)
            {
                OnDispose = (bool disposing) =>
                {
                    count++;
                    AssertExtensions.TrueExpression(disposing);
                }
            };

            for (int i = 0; i < disposeCalls; i++)
            {
                kem.Dispose();
            }

            Assert.Equal(1, count);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Exact_WrongCiphertextLength(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);

            // Ciphertext too small
            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes - 1],
                new byte[algorithm.SharedSecretSizeInBytes]));

            // Ciphertext too large
            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes + 1],
                new byte[algorithm.SharedSecretSizeInBytes]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Exact_WrongSharedSecretLength(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);

            // Shared secret too small
            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes - 1]));

            // Shared secret too large
            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes + 1]));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Encapsulate_Exact_OverlappingBuffers(bool partial)
        {
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            byte[] buffer = new byte[MLKemAlgorithm.MLKem512.CiphertextSizeInBytes];

            Assert.Throws<CryptographicException>(() =>
            {
                Span<byte> sharedSecret = buffer.AsSpan(partial ? 1 : 0, MLKemAlgorithm.MLKem512.SharedSecretSizeInBytes);
                Span<byte> ciphertext = buffer.AsSpan(0, MLKemAlgorithm.MLKem512.CiphertextSizeInBytes);
                kem.Encapsulate(ciphertext, sharedSecret);
            });
        }

        [Fact]
        public static void Encapsulate_Exact_Disposed()
        {
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.Encapsulate(
                new byte[kem.Algorithm.CiphertextSizeInBytes],
                new byte[kem.Algorithm.SharedSecretSizeInBytes]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Exact_DistinctBuffers_Works(MLKemAlgorithm algorithm)
        {
            byte[] ciphertextBuffer = new byte[algorithm.CiphertextSizeInBytes];
            byte[] sharedSecretBuffer = new byte[algorithm.SharedSecretSizeInBytes];
            using MLKemContract kem = new(algorithm)
            {
                OnEncapsulateCore = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    AssertExtensions.Same(ciphertext, ciphertextBuffer);
                    AssertExtensions.Same(sharedSecret, sharedSecretBuffer);
                }
            };

            kem.Encapsulate(ciphertextBuffer, sharedSecretBuffer);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Exact_SameNonOverlappingBuffer_Works(MLKemAlgorithm algorithm)
        {
            byte[] buffer = new byte[algorithm.CiphertextSizeInBytes + algorithm.SharedSecretSizeInBytes];
            Memory<byte> ciphertextBuffer = buffer.AsMemory(0, algorithm.CiphertextSizeInBytes);
            Memory<byte> sharedSecretBuffer = buffer.AsMemory(algorithm.CiphertextSizeInBytes, algorithm.SharedSecretSizeInBytes);
            using MLKemContract kem = new(algorithm)
            {
                OnEncapsulateCore = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    AssertExtensions.Same(ciphertext, ciphertextBuffer.Span);
                    AssertExtensions.Same(sharedSecret, sharedSecretBuffer.Span);
                }
            };

            kem.Encapsulate(ciphertextBuffer.Span, sharedSecretBuffer.Span);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Encapsulate_Allocated(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm)
            {
                OnEncapsulateCore = (Span<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    ciphertext.Fill(0xCA);
                    sharedSecret.Fill(0xFE);
                }
            };

            kem.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret);

            Assert.Equal(algorithm.CiphertextSizeInBytes, ciphertext.Length);
            Assert.Equal(algorithm.SharedSecretSizeInBytes, sharedSecret.Length);
            AssertExtensions.FilledWith<byte>(0xCA, ciphertext);
            AssertExtensions.FilledWith<byte>(0xFE, sharedSecret);
        }

        [Fact]
        public static void Encapsulate_Allocated_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.Encapsulate(out _, out _));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_Exact_WrongCiphertextLength(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);

            // Ciphertext too small
            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes - 1],
                new byte[algorithm.SharedSecretSizeInBytes]));

            // Ciphertext too large
            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes + 1],
                new byte[algorithm.SharedSecretSizeInBytes]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_Exact_WrongSharedSecretLength(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);

            // Shared secret too small
            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes - 1]));

            // Shared secret too large
            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes],
                new byte[algorithm.SharedSecretSizeInBytes + 1]));
        }

        [Fact]
        public static void Decapsulate_Exact_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.Decapsulate(
                new byte[kem.Algorithm.CiphertextSizeInBytes],
                new byte[kem.Algorithm.SharedSecretSizeInBytes]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_Exact_SeparateBuffers_Works(MLKemAlgorithm algorithm)
        {
            byte[] ciphertextBuffer = new byte[algorithm.CiphertextSizeInBytes];
            byte[] sharedSecretBuffer = new byte[algorithm.SharedSecretSizeInBytes];

            using MLKemContract kem = new(algorithm)
            {
                OnDecapsulateCore = (ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    AssertExtensions.Same(ciphertextBuffer, ciphertext);
                    AssertExtensions.Same(sharedSecretBuffer, sharedSecret);
                }
            };

            kem.Decapsulate(ciphertextBuffer, sharedSecretBuffer);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_Exact_OverlappingBuffers_Works(MLKemAlgorithm algorithm)
        {
            byte[] buffer = new byte[Math.Max(algorithm.CiphertextSizeInBytes, algorithm.SharedSecretSizeInBytes)];
            Memory<byte> ciphertextBuffer = buffer.AsMemory(0, algorithm.CiphertextSizeInBytes);
            Memory<byte> sharedSecretBuffer = buffer.AsMemory(0, algorithm.SharedSecretSizeInBytes);

            using MLKemContract kem = new(algorithm)
            {
                OnDecapsulateCore = (ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    AssertExtensions.Same(ciphertextBuffer.Span, ciphertext);
                    AssertExtensions.Same(sharedSecretBuffer.Span, sharedSecret);
                }
            };

            kem.Decapsulate(ciphertextBuffer.Span, sharedSecretBuffer.Span);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_Allocated_NullCiphertext(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);
            AssertExtensions.Throws<ArgumentNullException>("ciphertext", () => kem.Decapsulate((byte[])null));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_Allocated_CiphertextWrongSize(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes - 1]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(
                new byte[algorithm.CiphertextSizeInBytes + 1]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void Decapsulate_Allocated_Works(MLKemAlgorithm algorithm)
        {
            byte[] ciphertextBuffer = new byte[algorithm.CiphertextSizeInBytes];

            using MLKemContract kem = new(algorithm)
            {
                OnDecapsulateCore = (ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
                {
                    AssertExtensions.Same(ciphertextBuffer, ciphertext);
                    sharedSecret.Fill(0x55);
                }
            };

            byte[] sharedSecret = kem.Decapsulate(ciphertextBuffer);
            Assert.Equal(algorithm.SharedSecretSizeInBytes, sharedSecret.Length);
            AssertExtensions.FilledWith<byte>(0x55, sharedSecret);
        }

        [Fact]
        public static void Decapsulate_Allocated_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem768);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() =>
                kem.Decapsulate(new byte[MLKemAlgorithm.MLKem768.CiphertextSizeInBytes]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportPrivateSeed_Written_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);
            AssertExtensions.Throws<ArgumentException>("destination", () =>
                kem.ExportPrivateSeed(new byte[algorithm.PrivateSeedSizeInBytes - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () =>
                kem.ExportPrivateSeed(new byte[algorithm.PrivateSeedSizeInBytes + 1]));
        }

        [Fact]
        public static void ExportPrivateSeed_Written_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem1024);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() =>
                kem.ExportPrivateSeed(new byte[MLKemAlgorithm.MLKem1024.PrivateSeedSizeInBytes]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportPrivateSeed_Written_Works(MLKemAlgorithm algorithm)
        {
            byte[] privateSeedBuffer = new byte[algorithm.PrivateSeedSizeInBytes];
            using MLKemContract kem = new(algorithm)
            {
                 OnExportPrivateSeedCore = (Span<byte> destination) =>
                 {
                     AssertExtensions.Same(privateSeedBuffer, destination);
                 }
            };

            kem.ExportPrivateSeed(privateSeedBuffer);
        }

        [Fact]
        public static void ExportPrivateSeed_Allocated_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem1024);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.ExportPrivateSeed());
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportPrivateSeed_Allocated_Works(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm)
            {
                 OnExportPrivateSeedCore = (Span<byte> destination) =>
                 {
                    destination.Fill(0x42);
                 }
            };

            byte[] exported = kem.ExportPrivateSeed();
            AssertExtensions.FilledWith<byte>(0x42, exported);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportDecapsulationKey_Written_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);
            AssertExtensions.Throws<ArgumentException>("destination", () =>
                kem.ExportDecapsulationKey(new byte[algorithm.DecapsulationKeySizeInBytes - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () =>
                kem.ExportDecapsulationKey(new byte[algorithm.DecapsulationKeySizeInBytes + 1]));
        }

        [Fact]
        public static void ExportDecapsulationKey_Written_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem1024);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() =>
                kem.ExportDecapsulationKey(new byte[MLKemAlgorithm.MLKem1024.DecapsulationKeySizeInBytes]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportDecapsulationKey_Written_Works(MLKemAlgorithm algorithm)
        {
            byte[] privateSeedBuffer = new byte[algorithm.DecapsulationKeySizeInBytes];
            using MLKemContract kem = new(algorithm)
            {
                 OnExportDecapsulationKeyCore = (Span<byte> destination) =>
                 {
                     AssertExtensions.Same(privateSeedBuffer, destination);
                 }
            };

            kem.ExportDecapsulationKey(privateSeedBuffer);
        }

        [Fact]
        public static void ExportDecapsulationKey_Allocated_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem1024);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.ExportDecapsulationKey());
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportDecapsulationKey_Allocated_Works(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm)
            {
                 OnExportDecapsulationKeyCore = (Span<byte> destination) =>
                 {
                    destination.Fill(0x42);
                 }
            };

            byte[] exported = kem.ExportDecapsulationKey();
            AssertExtensions.FilledWith<byte>(0x42, exported);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportEncapsulationKey_Written_WrongSize(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm);
            AssertExtensions.Throws<ArgumentException>("destination", () =>
                kem.ExportEncapsulationKey(new byte[algorithm.EncapsulationKeySizeInBytes - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () =>
                kem.ExportEncapsulationKey(new byte[algorithm.EncapsulationKeySizeInBytes + 1]));
        }

        [Fact]
        public static void ExportEncapsulationKey_Written_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem1024);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() =>
                kem.ExportEncapsulationKey(new byte[MLKemAlgorithm.MLKem1024.EncapsulationKeySizeInBytes]));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportEncapsulationKey_Written_Works(MLKemAlgorithm algorithm)
        {
            byte[] privateSeedBuffer = new byte[algorithm.EncapsulationKeySizeInBytes];
            using MLKemContract kem = new(algorithm)
            {
                 OnExportEncapsulationKeyCore = (Span<byte> destination) =>
                 {
                     AssertExtensions.Same(privateSeedBuffer, destination);
                 }
            };

            kem.ExportEncapsulationKey(privateSeedBuffer);
        }

        [Fact]
        public static void ExportEncapsulationKey_Allocated_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem1024);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncapsulationKey());
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportEncapsulationKey_Allocated_Works(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm)
            {
                 OnExportEncapsulationKeyCore = (Span<byte> destination) =>
                 {
                    destination.Fill(0x42);
                 }
            };

            byte[] exported = kem.ExportEncapsulationKey();
            AssertExtensions.FilledWith<byte>(0x42, exported);
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void TryExportSubjectPublicKeyInfo_Buffers(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm)
            {
                 OnExportEncapsulationKeyCore = (Span<byte> destination) =>
                 {
                    destination.Fill(0x42);
                    Assert.Equal(algorithm.EncapsulationKeySizeInBytes, destination.Length);
                 }
            };

            byte[] destination = new byte[2048];
            destination.AsSpan().Fill(0xFF);
            AssertExtensions.TrueExpression(kem.TryExportSubjectPublicKeyInfo(destination, out int written));
            ReadSubjectPublicKeyInfo(
                destination.AsMemory(0, written),
                out string oid,
                out ReadOnlyMemory<byte>? parameters,
                out ReadOnlyMemory<byte> publicKey);

            Assert.Equal(oid, MapAlgorithmOid(algorithm));
            AssertExtensions.FalseExpression(parameters.HasValue);
            AssertExtensions.FilledWith<byte>(0x42, publicKey.Span);
            AssertExtensions.FilledWith<byte>(0xFF, destination.AsSpan(written));
        }

        [Fact]
        public static void TryExportSubjectPublicKeyInfo_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.TryExportSubjectPublicKeyInfo([], out _));
        }

        [Theory]
        [MemberData(nameof(MLKemTestData.MLKemAlgorithms), MemberType = typeof(MLKemTestData))]
        public static void ExportSubjectPublicKeyInfo_Allocated(MLKemAlgorithm algorithm)
        {
            using MLKemContract kem = new(algorithm)
            {
                 OnExportEncapsulationKeyCore = (Span<byte> destination) =>
                 {
                    destination.Fill(0x42);
                    Assert.Equal(algorithm.EncapsulationKeySizeInBytes, destination.Length);
                 }
            };

            byte[] spki = kem.ExportSubjectPublicKeyInfo();
            ReadSubjectPublicKeyInfo(
                spki,
                out string oid,
                out ReadOnlyMemory<byte>? parameters,
                out ReadOnlyMemory<byte> publicKey);

            Assert.Equal(oid, MapAlgorithmOid(algorithm));
            AssertExtensions.FalseExpression(parameters.HasValue);
            AssertExtensions.FilledWith<byte>(0x42, publicKey.Span);
        }

        [Fact]
        public static void ExportSubjectPublicKeyInfoPem()
        {
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512)
            {
                 OnExportEncapsulationKeyCore = (Span<byte> destination) =>
                 {
                    destination.Fill(0x42);
                    Assert.Equal(MLKemAlgorithm.MLKem512.EncapsulationKeySizeInBytes, destination.Length);
                 }
            };

            string spkiPem = kem.ExportSubjectPublicKeyInfoPem();
            const string ExpectedPem =
                "-----BEGIN PUBLIC KEY-----\n" +
                "MIIDMjALBglghkgBZQMEBAEDggMhAEJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJCQkJC\n" +
                "QkJCQkJC\n" +
                "-----END PUBLIC KEY-----";
            Assert.Equal(ExpectedPem, spkiPem);
        }

        [Fact]
        public static void ExportSubjectPublicKeyInfo_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.ExportSubjectPublicKeyInfo());
        }

        [Fact]
        public static void TryExportPkcs8PrivateKey_EarlyExitForSmallBuffer()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            byte[] destination = new byte[85];
            AssertExtensions.FalseExpression(kem.TryExportPkcs8PrivateKey(destination, out int written));
            Assert.Equal(0, written);
        }

        [Fact]
        public static void TryExportPkcs8PrivateKey()
        {
            Random random;
#if NET
            random = Random.Shared;
#else
            random = new Random();
#endif
            int bufferSize = random.Next(87, 1024);
            int writtenSize = random.Next(86, bufferSize);
            bool success = (writtenSize & 1) == 1;
            byte[] buffer = new byte[bufferSize];
            MLKemContract kem = new(MLKemAlgorithm.MLKem512)
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    AssertExtensions.Same(buffer, destination);
                    bytesWritten = writtenSize;
                    return success;
                }
            };

            AssertExtensions.TrueExpression(success == kem.TryExportPkcs8PrivateKey(buffer, out int written));
            Assert.Equal(writtenSize, written);
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_OneExportCall()
        {
            int size = -1;
            MLKemContract kem = new(MLKemAlgorithm.MLKem512)
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    destination.Fill(0x88);
                    bytesWritten = destination.Length;
                    size = destination.Length;
                    return true;
                }
            };

            byte[] exported = kem.ExportPkcs8PrivateKey();
            AssertExtensions.FilledWith<byte>(0x88, exported);
            Assert.Equal(size, exported.Length);
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_ExpandAndRetry()
        {
            const int TargetSize = 4567;
            MLKemContract kem = new(MLKemAlgorithm.MLKem512)
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    if (destination.Length < TargetSize)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    destination.Fill(0x88);
                    bytesWritten = TargetSize;
                    return true;
                }
            };

            byte[] exported = kem.ExportPkcs8PrivateKey();
            AssertExtensions.FilledWith<byte>(0x88, exported);
            Assert.Equal(TargetSize, exported.Length);

            // The exact number of calls that made varies depending on the behavior of how the ArrayPool
            // behaves. Though the algorithm is to double the buffer size, the pool may rent more than requested from
            // the doubling. However we know it should be more than one.
            AssertExtensions.GreaterThan(kem.TryExportPkcs8PrivateKeyCoreCount, 1);

            // If the implementation follows a doubling scheme exactly, the ML-KEM 512 decapsulation key size
            // should take no more than 3 calls to reach 4567. The initial size is 1,664 bytes.
            AssertExtensions.LessThan(kem.TryExportPkcs8PrivateKeyCoreCount, 4);
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_MisbehavingBytesWritten_Oversized()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512)
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    // This is not possible and indiciates a derived type is misimplemented.
                    bytesWritten = destination.Length + 1;
                    return true;
                }
            };

            Assert.Throws<CryptographicException>(() => kem.ExportPkcs8PrivateKey());
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_MisbehavingBytesWritten_Negative()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512)
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    bytesWritten = -1;
                    return true;
                }
            };

            Assert.Throws<CryptographicException>(() => kem.ExportPkcs8PrivateKey());
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.ExportPkcs8PrivateKey());
            Assert.Throws<ObjectDisposedException>(() => kem.TryExportPkcs8PrivateKey(new byte[512], out _));
        }

        [Fact]
        public static void ExportEncryptedPkcs8PrivateKey_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.TryExportEncryptedPkcs8PrivateKey(
                    MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(),
                    s_aes128Pbe,
                    new byte[2048],
                    out _));

            Assert.Throws<ObjectDisposedException>(() => kem.TryExportEncryptedPkcs8PrivateKey(
                    MLKemTestData.EncryptedPrivateKeyPassword,
                    s_aes128Pbe,
                    new byte[2048],
                    out _));

            Assert.Throws<ObjectDisposedException>(() =>  kem.TryExportEncryptedPkcs8PrivateKey(
                    MLKemTestData.EncryptedPrivateKeyPasswordBytes,
                    s_aes128Pbe,
                    new byte[2048],
                    out _));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncryptedPkcs8PrivateKey(
                MLKemTestData.EncryptedPrivateKeyPassword,
                s_aes128Pbe));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncryptedPkcs8PrivateKey(
                MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(),
                s_aes128Pbe));

            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncryptedPkcs8PrivateKey(
                MLKemTestData.EncryptedPrivateKeyPasswordBytes,
                s_aes128Pbe));
        }

        [Theory]
        [InlineData(TryExportPkcs8PasswordKind.StringPassword)]
        [InlineData(TryExportPkcs8PasswordKind.SpanOfBytesPassword)]
        [InlineData(TryExportPkcs8PasswordKind.SpanOfCharsPassword)]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser does not support symmetric encryption")]
        public static void TryExportEncryptedPkcs8PrivateKey_ExportsPkcs8(TryExportPkcs8PasswordKind kind)
        {
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512)
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    if (MLKemTestData.IetfMlKem512PrivateKeyExpandedKey.AsSpan().TryCopyTo(destination))
                    {
                        bytesWritten = MLKemTestData.IetfMlKem512PrivateKeyExpandedKey.Length;
                        return true;
                    }

                    Assert.Fail("Initial buffer was not correctly sized.");
                    bytesWritten = 0;
                    return false;
                }
            };

            byte[] buffer = new byte[2048];
            bool success = TryExportEncryptedPkcs8PrivateKeyByKind(kem, kind, buffer, out int written);
            AssertExtensions.TrueExpression(success);
            AssertExtensions.GreaterThan(written, 0);
            Assert.Equal(1, kem.TryExportPkcs8PrivateKeyCoreCount);
        }

        [Theory]
        [InlineData(TryExportPkcs8PasswordKind.StringPassword)]
        [InlineData(TryExportPkcs8PasswordKind.SpanOfBytesPassword)]
        [InlineData(TryExportPkcs8PasswordKind.SpanOfCharsPassword)]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser does not support symmetric encryption")]
        public static void TryExportEncryptedPkcs8PrivateKey_InnerBuffer_LargePkcs8(TryExportPkcs8PasswordKind kind)
        {
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
            {
                if (kem.TryExportPkcs8PrivateKeyCoreCount < 2)
                {
                    bytesWritten = 0;
                    return false;
                }

                if (MLKemTestData.IetfMlKem512PrivateKeyExpandedKey.AsSpan().TryCopyTo(destination))
                {
                    bytesWritten = MLKemTestData.IetfMlKem512PrivateKeyExpandedKey.Length;
                    return true;
                }

                bytesWritten = 0;
                return false;
            };

            byte[] buffer = new byte[2048];
            bool success = TryExportEncryptedPkcs8PrivateKeyByKind(kem, kind, buffer, out int written);
            AssertExtensions.TrueExpression(success);
            AssertExtensions.GreaterThan(written, 0);
            Assert.Equal(2, kem.TryExportPkcs8PrivateKeyCoreCount);
        }

        [Theory]
        [InlineData(TryExportPkcs8PasswordKind.StringPassword)]
        [InlineData(TryExportPkcs8PasswordKind.SpanOfBytesPassword)]
        [InlineData(TryExportPkcs8PasswordKind.SpanOfCharsPassword)]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser does not support symmetric encryption")]
        public static void TryExportEncryptedPkcs8PrivateKey_DestinationTooSmall(TryExportPkcs8PasswordKind kind)
        {
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512)
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    if (MLKemTestData.IetfMlKem512PrivateKeyExpandedKey.AsSpan().TryCopyTo(destination))
                    {
                        bytesWritten = MLKemTestData.IetfMlKem512PrivateKeyExpandedKey.Length;
                        return true;
                    }

                    bytesWritten = 0;
                    return false;
                }
            };

            byte[] buffer = new byte[3];
            bool success = TryExportEncryptedPkcs8PrivateKeyByKind(kem, kind, buffer, out int written);
            AssertExtensions.FalseExpression(success);
            Assert.Equal(0, written);
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_ValidatesPbeParameters_Bad3DESHash()
        {
            byte[] buffer = new byte[2048];
            PbeParameters pbeParameters = new(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA256, 3);
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            Assert.Throws<CryptographicException>(() =>
                kem.TryExportEncryptedPkcs8PrivateKey(
                    MLKemTestData.EncryptedPrivateKeyPassword,
                    pbeParameters,
                    buffer,
                    out _));
            Assert.Throws<CryptographicException>(() =>
                kem.TryExportEncryptedPkcs8PrivateKey(
                    MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(),
                    pbeParameters,
                    buffer,
                    out _));
            Assert.Throws<CryptographicException>(() =>
                kem.TryExportEncryptedPkcs8PrivateKey(
                    MLKemTestData.EncryptedPrivateKeyPasswordBytes,
                    pbeParameters,
                    buffer,
                    out _));
            Assert.Throws<CryptographicException>(() =>
                kem.ExportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPassword, pbeParameters));
            Assert.Throws<CryptographicException>(() =>
                kem.ExportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(), pbeParameters));
            Assert.Throws<CryptographicException>(() =>
                kem.ExportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPasswordBytes, pbeParameters));
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_ValidatesPbeParameters_3DESRequiresChar()
        {
            byte[] buffer = new byte[2048];
            PbeParameters pbeParameters = new(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 3);
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            Assert.Throws<CryptographicException>(() =>
                kem.TryExportEncryptedPkcs8PrivateKey(
                    MLKemTestData.EncryptedPrivateKeyPasswordBytes,
                    pbeParameters,
                    buffer,
                    out _));
            Assert.Throws<CryptographicException>(() =>
                kem.ExportEncryptedPkcs8PrivateKey(MLKemTestData.EncryptedPrivateKeyPasswordBytes, pbeParameters));
            Assert.Throws<CryptographicException>(() => kem.ExportEncryptedPkcs8PrivateKeyPem(
                MLKemTestData.EncryptedPrivateKeyPasswordBytes, pbeParameters));
        }

        [Fact]
        public static void ExportPkcs8PrivateKey_NullArgs()
        {
            byte[] buffer = new byte[2048];
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.TryExportEncryptedPkcs8PrivateKey(
                MLKemTestData.EncryptedPrivateKeyPassword, pbeParameters: null, buffer, out _));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.TryExportEncryptedPkcs8PrivateKey(
                MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(), pbeParameters: null, buffer, out _));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.TryExportEncryptedPkcs8PrivateKey(
                MLKemTestData.EncryptedPrivateKeyPasswordBytes, pbeParameters: null, buffer, out _));
            AssertExtensions.Throws<ArgumentNullException>("password", () => kem.TryExportEncryptedPkcs8PrivateKey(
                (string)null, s_aes128Pbe, buffer, out _));

            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.ExportEncryptedPkcs8PrivateKey(
                MLKemTestData.EncryptedPrivateKeyPassword, pbeParameters: null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.ExportEncryptedPkcs8PrivateKey(
                MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(), pbeParameters: null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.ExportEncryptedPkcs8PrivateKey(
                MLKemTestData.EncryptedPrivateKeyPasswordBytes, pbeParameters: null));

            AssertExtensions.Throws<ArgumentNullException>("password", () => kem.ExportEncryptedPkcs8PrivateKey(
                (string)null, s_aes128Pbe));

            AssertExtensions.Throws<ArgumentNullException>("password", () => kem.ExportEncryptedPkcs8PrivateKeyPem(
                (string)null, s_aes128Pbe));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.ExportEncryptedPkcs8PrivateKeyPem(
                MLKemTestData.EncryptedPrivateKeyPassword, pbeParameters: null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.ExportEncryptedPkcs8PrivateKeyPem(
                MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(), pbeParameters: null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.ExportEncryptedPkcs8PrivateKeyPem(
                MLKemTestData.EncryptedPrivateKeyPasswordBytes, pbeParameters: null));
        }

        [Fact]
        public static void ExportEncryptedPkcs8PrivateKeyPem_Disposed()
        {
            MLKemContract kem = new(MLKemAlgorithm.MLKem512);
            kem.Dispose();
            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncryptedPkcs8PrivateKeyPem(
                MLKemTestData.EncryptedPrivateKeyPassword, s_aes128Pbe));
            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncryptedPkcs8PrivateKeyPem(
                MLKemTestData.EncryptedPrivateKeyPassword, s_aes128Pbe));
            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncryptedPkcs8PrivateKeyPem(
                MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(), s_aes128Pbe));
            Assert.Throws<ObjectDisposedException>(() => kem.ExportEncryptedPkcs8PrivateKeyPem(
                MLKemTestData.EncryptedPrivateKeyPasswordBytes, s_aes128Pbe));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser does not support symmetric encryption")]
        public static void ExportEncryptedPkcs8PrivateKeyPem()
        {
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512)
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    if (MLKemTestData.IetfMlKem512PrivateKeyExpandedKey.AsSpan().TryCopyTo(destination))
                    {
                        bytesWritten = MLKemTestData.IetfMlKem512PrivateKeyExpandedKey.Length;
                        return true;
                    }

                    bytesWritten = 0;
                    return false;
                }
            };

            string pem = kem.ExportEncryptedPkcs8PrivateKeyPem(MLKemTestData.EncryptedPrivateKeyPassword, s_aes128Pbe);
            AssertPem(pem);
            pem = kem.ExportEncryptedPkcs8PrivateKeyPem(MLKemTestData.EncryptedPrivateKeyPasswordBytes, s_aes128Pbe);
            AssertPem(pem);
            pem = kem.ExportEncryptedPkcs8PrivateKeyPem(MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(), s_aes128Pbe);
            AssertPem(pem);

            static void AssertPem(string pem)
            {
                PemFields fields = PemEncoding.Find(pem.AsSpan());
                Assert.Equal(Index.FromStart(0), fields.Location.Start);
                Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
                Assert.Equal("ENCRYPTED PRIVATE KEY", pem.AsSpan()[fields.Label].ToString());
            }
        }

        [Fact]
        public static void ExportPkcs8PrivateKeyPem()
        {
            using MLKemContract kem = new(MLKemAlgorithm.MLKem512)
            {
                OnTryExportPkcs8PrivateKeyCore = (Span<byte> destination, out int bytesWritten) =>
                {
                    if (MLKemTestData.IetfMlKem512PrivateKeyExpandedKey.AsSpan().TryCopyTo(destination))
                    {
                        bytesWritten = MLKemTestData.IetfMlKem512PrivateKeyExpandedKey.Length;
                        return true;
                    }

                    bytesWritten = 0;
                    return false;
                }
            };

            string pem = kem.ExportPkcs8PrivateKeyPem();
            byte[] pkcs8 = kem.ExportPkcs8PrivateKey();
            PemFields fields = PemEncoding.Find(pem.AsSpan());
            Assert.Equal(Index.FromStart(0), fields.Location.Start);
            Assert.Equal(Index.FromStart(pem.Length), fields.Location.End);
            Assert.Equal("PRIVATE KEY", pem.AsSpan()[fields.Label].ToString());
            AssertExtensions.SequenceEqual(pkcs8, Convert.FromBase64String(pem.AsSpan()[fields.Base64Data].ToString()));
        }

        private static string MapAlgorithmOid(MLKemAlgorithm algorithm)
        {
            if (algorithm == MLKemAlgorithm.MLKem512)
            {
                return "2.16.840.1.101.3.4.4.1";
            }
            else if (algorithm == MLKemAlgorithm.MLKem768)
            {
                return "2.16.840.1.101.3.4.4.2";
            }
            else if (algorithm == MLKemAlgorithm.MLKem1024)
            {
                return "2.16.840.1.101.3.4.4.3";
            }

            Assert.Fail($"Unknown algorithm {algorithm.Name}.");
            return null;
        }

        private static void ReadSubjectPublicKeyInfo(
            ReadOnlyMemory<byte> source,
            out string oid,
            out ReadOnlyMemory<byte>? algorithmParameters,
            out ReadOnlyMemory<byte> subjectPublicKey)
        {
            AsnReader outer = new(source, AsnEncodingRules.DER);
            AsnReader reader = outer.ReadSequence();
            outer.ThrowIfNotEmpty();

            AsnReader spkiAlgorithm = reader.ReadSequence();
            oid = spkiAlgorithm.ReadObjectIdentifier();

            if (spkiAlgorithm.HasData)
            {
                algorithmParameters = spkiAlgorithm.ReadEncodedValue();
            }
            else
            {
                algorithmParameters = null;
            }

            spkiAlgorithm.ThrowIfNotEmpty();

            AssertExtensions.TrueExpression(reader.TryReadPrimitiveBitString(out int unusedBits, out subjectPublicKey));
            reader.ThrowIfNotEmpty();
            Assert.Equal(0, unusedBits);
        }

        private static bool TryExportEncryptedPkcs8PrivateKeyByKind(
            MLKem kem,
            TryExportPkcs8PasswordKind kind,
            Span<byte> destination,
            out int bytesWritten)
        {
            switch (kind)
            {
                case TryExportPkcs8PasswordKind.StringPassword:
                    return kem.TryExportEncryptedPkcs8PrivateKey(
                        MLKemTestData.EncryptedPrivateKeyPassword,
                        s_aes128Pbe,
                        destination,
                        out bytesWritten);
                case TryExportPkcs8PasswordKind.SpanOfCharsPassword:
                    return kem.TryExportEncryptedPkcs8PrivateKey(
                        MLKemTestData.EncryptedPrivateKeyPassword.AsSpan(),
                        s_aes128Pbe,
                        destination,
                        out bytesWritten);
                case TryExportPkcs8PasswordKind.SpanOfBytesPassword:
                    return kem.TryExportEncryptedPkcs8PrivateKey(
                        MLKemTestData.EncryptedPrivateKeyPasswordBytes,
                        s_aes128Pbe,
                        destination,
                        out bytesWritten);
                default:
                    throw new XunitException($"Unknown password kind '{kind}'.");
            }
        }

        public enum TryExportPkcs8PasswordKind
        {
            StringPassword,
            SpanOfCharsPassword,
            SpanOfBytesPassword,
        }
    }

    internal sealed class MLKemContract : MLKem
    {
        internal DecapsulateCoreCallback OnDecapsulateCore { get; set; }
        internal EncapsulateCoreCallback OnEncapsulateCore { get; set; }
        internal ExportKeyCoreCallback OnExportPrivateSeedCore { get; set; }
        internal ExportKeyCoreCallback OnExportEncapsulationKeyCore { get; set; }
        internal ExportKeyCoreCallback OnExportDecapsulationKeyCore { get; set; }
        internal TryExportPkcs8PrivateKeyCoreCallback OnTryExportPkcs8PrivateKeyCore { get; set; }
        internal Action<bool> OnDispose { get; set; } = (bool disposing) => { };

        internal int DecapsulateCoreCount { get; set; }
        internal int EncapsulateCoreCount { get; set; }
        internal int ExportPrivateSeedCoreCount { get; set; }
        internal int ExportEncapsulationKeyCoreCount { get; set; }
        internal int ExportDecapsulationKeyCoreCount { get; set; }
        internal int TryExportPkcs8PrivateKeyCoreCount { get; set; }

        private bool _disposed;

        public MLKemContract(MLKemAlgorithm algorithm) : base(algorithm)
        {
        }

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            DecapsulateCoreCount++;
            GetCallback(OnDecapsulateCore)(ciphertext, sharedSecret);
        }

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            EncapsulateCoreCount++;
            GetCallback(OnEncapsulateCore)(ciphertext, sharedSecret);
        }

        protected override void ExportPrivateSeedCore(Span<byte> destination)
        {
            ExportPrivateSeedCoreCount++;
            GetCallback(OnExportPrivateSeedCore)(destination);
        }

        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            ExportDecapsulationKeyCoreCount++;
            GetCallback(OnExportDecapsulationKeyCore)(destination);
        }

        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            ExportEncapsulationKeyCoreCount++;
            GetCallback(OnExportEncapsulationKeyCore)(destination);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            TryExportPkcs8PrivateKeyCoreCount++;
            return GetCallback(OnTryExportPkcs8PrivateKeyCore)(destination, out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            GetCallback(OnDispose)(disposing);
            VerifyCalledOnDispose();
            _disposed = true;
        }

        private void VerifyCalledOnDispose()
        {
            if (OnDecapsulateCore is not null && DecapsulateCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(DecapsulateCore)}.");
            }
            if (OnEncapsulateCore is not null && EncapsulateCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(EncapsulateCore)}.");
            }
            if (OnExportPrivateSeedCore is not null && ExportPrivateSeedCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(ExportPrivateSeedCore)}.");
            }
            if (OnExportDecapsulationKeyCore is not null && ExportDecapsulationKeyCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(ExportDecapsulationKeyCore)}.");
            }
            if (OnExportEncapsulationKeyCore is not null && ExportEncapsulationKeyCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(ExportEncapsulationKeyCore)}.");
            }
            if (OnTryExportPkcs8PrivateKeyCore is not null && TryExportPkcs8PrivateKeyCoreCount == 0)
            {
                Assert.Fail($"Expected call to {nameof(TryExportPkcs8PrivateKeyCore)}.");
            }
        }

        internal delegate void DecapsulateCoreCallback(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret);
        internal delegate void EncapsulateCoreCallback(Span<byte> ciphertext, Span<byte> sharedSecret);
        internal delegate void ExportKeyCoreCallback(Span<byte> destination);
        internal delegate bool TryExportPkcs8PrivateKeyCoreCallback(Span<byte> destination, out int bytesWritten);

        private T GetCallback<T>(T callback, [CallerMemberNameAttribute]string caller = null) where T : Delegate
        {
            if (_disposed)
            {
                Assert.Fail($"Unexpected call to {caller} after Dispose.");
            }

            return callback ?? throw new XunitException($"Unexpected call to {caller}.");
        }
    }
}
