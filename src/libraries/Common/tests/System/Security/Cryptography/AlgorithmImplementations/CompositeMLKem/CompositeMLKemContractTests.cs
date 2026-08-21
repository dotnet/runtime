// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class CompositeMLKemContractTests
    {
        private static readonly PbeParameters s_aes256Sha256Pbe =
            new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 42);

        [Fact]
        public static void ArgumentValidation_Ctor_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => new CompositeMLKemMockImplementation(null));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void Algorithm_MatchesConstructorArgument(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKem kem = CompositeMLKemMockImplementation.Create(algorithm);
            Assert.Same(algorithm, kem.Algorithm);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsAndDisposalTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void NullArgumentValidation(CompositeMLKemAlgorithm algorithm, bool shouldDispose)
        {
            using CompositeMLKem kem = CompositeMLKemMockImplementation.Create(algorithm);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                kem.Dispose();
            }

            AssertExtensions.Throws<ArgumentNullException>("ciphertext", () => kem.Decapsulate(null));

            AssertExtensions.Throws<ArgumentNullException>("password", () => kem.ExportEncryptedPkcs8PrivateKey((string)null, null));
            AssertExtensions.Throws<ArgumentNullException>("password", () => kem.ExportEncryptedPkcs8PrivateKeyPem((string)null, null));
            AssertExtensions.Throws<ArgumentNullException>("password", () => kem.TryExportEncryptedPkcs8PrivateKey((string)null, null, Span<byte>.Empty, out _));

            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.ExportEncryptedPkcs8PrivateKey(string.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.ExportEncryptedPkcs8PrivateKeyPem(string.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null, Span<byte>.Empty, out _));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null, Span<byte>.Empty, out _));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => kem.TryExportEncryptedPkcs8PrivateKey(string.Empty, null, Span<byte>.Empty, out _));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsAndDisposalTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ArgumentValidation_BufferSizes(CompositeMLKemAlgorithm algorithm, bool shouldDispose)
        {
            using CompositeMLKem kem = CompositeMLKemMockImplementation.Create(algorithm);
            int ciphertextSize = algorithm.CiphertextSizeInBytes;
            int sharedSecretSize = algorithm.SharedSecretSizeInBytes;

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                kem.Dispose();
            }

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(new byte[ciphertextSize - 1], new byte[sharedSecretSize]));
            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Encapsulate(new byte[ciphertextSize + 1], new byte[sharedSecretSize]));
            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(new byte[ciphertextSize], new byte[sharedSecretSize - 1]));
            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Encapsulate(new byte[ciphertextSize], new byte[sharedSecretSize + 1]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(new byte[ciphertextSize - 1]));
            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(new byte[ciphertextSize + 1]));

            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(new byte[ciphertextSize - 1].AsSpan(), new byte[sharedSecretSize]));
            AssertExtensions.Throws<ArgumentException>("ciphertext", () => kem.Decapsulate(new byte[ciphertextSize + 1].AsSpan(), new byte[sharedSecretSize]));
            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(new byte[ciphertextSize].AsSpan(), new byte[sharedSecretSize - 1]));
            AssertExtensions.Throws<ArgumentException>("sharedSecret", () => kem.Decapsulate(new byte[ciphertextSize].AsSpan(), new byte[sharedSecretSize + 1]));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsAndDisposalTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ArgumentValidation_OverlappingEncapsulateBuffers(CompositeMLKemAlgorithm algorithm, bool shouldDispose)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            byte[] buffer = new byte[algorithm.CiphertextSizeInBytes];

            if (shouldDispose)
            {
                // Test that overlap validation takes precedence over ObjectDisposedException
                kem.Dispose();
            }

            Assert.Throws<CryptographicException>(() =>
                kem.Encapsulate(buffer.AsSpan(0, algorithm.CiphertextSizeInBytes), buffer.AsSpan(0, algorithm.SharedSecretSizeInBytes)));

            Assert.Equal(0, kem.EncapsulateCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportEncryptedPkcs8PrivateKey_PbeAlgorithmUnknown(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKem kem = CompositeMLKemMockImplementation.Create(algorithm);

            CompositeMLKemTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
                Assert.Throws<CryptographicException>(() =>
                    export(kem, "PLACEHOLDER", new PbeParameters(PbeEncryptionAlgorithm.Unknown, HashAlgorithmName.SHA1, 42))));
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "Password-based encryption requires TripleDES, which is not supported on Browser.")]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportEncryptedPkcs8PrivateKey_Pkcs12HashInvalid(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKem kem = CompositeMLKemMockImplementation.Create(algorithm);

            CompositeMLKemTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
                Assert.Throws<CryptographicException>(() =>
                    export(kem, "PLACEHOLDER", new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA512, 42))));
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "Password-based encryption requires TripleDES, which is not supported on Browser.")]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportEncryptedPkcs8PrivateKey_BytePasswordRejectsPkcs12Kdf(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKem kem = CompositeMLKemMockImplementation.Create(algorithm);

            CompositeMLKemTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
                Assert.Throws<CryptographicException>(() =>
                    export(kem, "PLACEHOLDER", new PbeParameters(PbeEncryptionAlgorithm.TripleDes3KeyPkcs12, HashAlgorithmName.SHA1, 42))),
                CompositeMLKemTestHelpers.EncryptionPasswordType.Byte);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void Encapsulate_Array_CallsCore(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);

            kem.EncapsulateCoreHook = (_, _) => { };
            kem.AddLengthAssertion();
            kem.AddFillDestination(42);

            kem.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret);

            Assert.Equal(1, kem.EncapsulateCoreCallCount);
            Assert.Equal(algorithm.CiphertextSizeInBytes, ciphertext.Length);
            Assert.Equal(algorithm.SharedSecretSizeInBytes, sharedSecret.Length);
            AssertExtensions.FilledWith<byte>(42, ciphertext);
            AssertExtensions.FilledWith<byte>(42, sharedSecret);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void Encapsulate_Span_CallsCore(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);

            byte[] ciphertext = new byte[algorithm.CiphertextSizeInBytes];
            byte[] sharedSecret = new byte[algorithm.SharedSecretSizeInBytes];

            kem.EncapsulateCoreHook = (_, _) => { };
            kem.AddLengthAssertion();
            kem.AddCiphertextBufferIsSameAssertion(ciphertext);
            kem.AddSharedSecretBufferIsSameAssertion(sharedSecret);
            kem.AddFillDestination(7);

            kem.Encapsulate(ciphertext, sharedSecret);

            Assert.Equal(1, kem.EncapsulateCoreCallCount);
            AssertExtensions.FilledWith<byte>(7, ciphertext);
            AssertExtensions.FilledWith<byte>(7, sharedSecret);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void Encapsulate_CoreCryptographicException_Propagates(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            CryptographicException expected = new();

            kem.EncapsulateCoreHook = (_, _) => throw expected;

            Assert.Same(expected, Assert.Throws<CryptographicException>(() => kem.Encapsulate(out _, out _)));
            Assert.Same(
                expected,
                Assert.Throws<CryptographicException>(() =>
                    kem.Encapsulate(new byte[algorithm.CiphertextSizeInBytes], new byte[algorithm.SharedSecretSizeInBytes])));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void Decapsulate_Array_CallsCore(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);

            byte[] ciphertext = new byte[algorithm.CiphertextSizeInBytes];
            ciphertext.AsSpan().Fill(1);

            kem.DecapsulateCoreHook = (_, _) => { };
            kem.AddLengthAssertion();
            kem.AddCiphertextBufferIsSameAssertion(ciphertext);
            kem.AddFillDestination(3);

            byte[] sharedSecret = kem.Decapsulate(ciphertext);

            Assert.Equal(1, kem.DecapsulateCoreCallCount);
            Assert.Equal(algorithm.SharedSecretSizeInBytes, sharedSecret.Length);
            AssertExtensions.FilledWith<byte>(3, sharedSecret);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void Decapsulate_Span_CallsCore(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);

            byte[] ciphertext = new byte[algorithm.CiphertextSizeInBytes];
            byte[] sharedSecret = new byte[algorithm.SharedSecretSizeInBytes];

            kem.DecapsulateCoreHook = (_, _) => { };
            kem.AddLengthAssertion();
            kem.AddCiphertextBufferIsSameAssertion(ciphertext);
            kem.AddSharedSecretBufferIsSameAssertion(sharedSecret);
            kem.AddFillDestination(9);

            kem.Decapsulate(new ReadOnlySpan<byte>(ciphertext), sharedSecret);

            Assert.Equal(1, kem.DecapsulateCoreCallCount);
            AssertExtensions.FilledWith<byte>(9, sharedSecret);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void Decapsulate_CoreCryptographicException_Propagates(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            CryptographicException expected = new();
            byte[] ciphertext = new byte[algorithm.CiphertextSizeInBytes];

            kem.DecapsulateCoreHook = (_, _) => throw expected;

            Assert.Same(expected, Assert.Throws<CryptographicException>(() => kem.Decapsulate(ciphertext)));
            Assert.Same(
                expected,
                Assert.Throws<CryptographicException>(() =>
                    kem.Decapsulate(ciphertext, new byte[algorithm.SharedSecretSizeInBytes])));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void Decapsulate_OverlappingBuffersAreAllowed(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);

            byte[] buffer = new byte[algorithm.CiphertextSizeInBytes];

            kem.DecapsulateCoreHook = (_, _) => { };
            kem.AddLengthAssertion();
            kem.AddFillDestination(5);

            // Unlike encapsulation, decapsulation does not reject overlapping buffers.
            kem.Decapsulate(buffer.AsSpan(), buffer.AsSpan(0, algorithm.SharedSecretSizeInBytes));

            Assert.Equal(1, kem.DecapsulateCoreCallCount);
            AssertExtensions.FilledWith<byte>(5, buffer.AsSpan(0, algorithm.SharedSecretSizeInBytes));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportEncapsulationKey_AllOverloads(CompositeMLKemAlgorithm algorithm)
        {
            byte[] expected = new byte[CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm)];
            expected.AsSpan().Fill(0x5A);

            CompositeMLKemTestHelpers.AssertExportEncapsulationKey(export =>
            {
                using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
                kem.SetNoOpHooks();
                kem.AddFillDestination(expected);

                AssertExtensions.SequenceEqual(expected, export(kem));
            });
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportEncapsulationKey_CoreCryptographicException_Propagates(CompositeMLKemAlgorithm algorithm)
        {
            CompositeMLKemTestHelpers.AssertExportEncapsulationKey(export =>
            {
                using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
                CryptographicException expected = new();

                kem.ExportEncapsulationKeyCoreHook = _ => throw expected;

                Assert.Same(expected, Assert.Throws<CryptographicException>(() => export(kem)));
            });
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportDecapsulationKey_AllOverloads(CompositeMLKemAlgorithm algorithm)
        {
            byte[] expected = new byte[CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(algorithm)];
            expected.AsSpan().Fill(0x3C);

            CompositeMLKemTestHelpers.AssertExportDecapsulationKey(
                export =>
                {
                    using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
                    kem.SetNoOpHooks();
                    kem.AddFillDestination(expected);

                    AssertExtensions.SequenceEqual(expected, export(kem));
                },
                export =>
                {
                    // The PKCS#8 route needs a well-formed PrivateKeyInfo which embeds the raw key.
                    using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
                    kem.SetNoOpHooks();
                    kem.AddFillDestination(CreatePkcs8PrivateKey(algorithm, expected));

                    AssertExtensions.SequenceEqual(expected, export(kem));
                });
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportDecapsulationKey_CoreCryptographicException_Propagates(CompositeMLKemAlgorithm algorithm)
        {
            CompositeMLKemTestHelpers.AssertExportDecapsulationKey(
                export =>
                {
                    using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
                    CryptographicException expected = new();

                    kem.ExportDecapsulationKeyCoreHook = _ => throw expected;

                    Assert.Same(expected, Assert.Throws<CryptographicException>(() => export(kem)));
                },
                export =>
                {
                    using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
                    CryptographicException expected = new();

                    kem.TryExportPkcs8PrivateKeyCoreHook =
                        (Span<byte> _, out int bytesWritten) =>
                        {
                            throw expected;
                        };

                    Assert.Same(expected, Assert.Throws<CryptographicException>(() => export(kem)));
                });
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void TryExportEncapsulationKey_LowerBound(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm);

            // Buffer is too small
            byte[] bytes = new byte[lowerBound - 1];
            AssertExtensions.FalseExpression(kem.TryExportEncapsulationKey(bytes, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(0, kem.ExportEncapsulationKeyCoreCallCount);

            // Buffer meets the lower bound
            bytes = new byte[lowerBound];

            kem.ExportEncapsulationKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);
                return lowerBound;
            };

            AssertExtensions.TrueExpression(kem.TryExportEncapsulationKey(bytes, out bytesWritten));
            Assert.Equal(lowerBound, bytesWritten);
            AssertExtensions.FilledWith<byte>(1, bytes);

            // Buffer meets the lower bound, but returned value is too small
            kem.ExportEncapsulationKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);

                // Writing less than lower bound isn't allowed.
                return lowerBound - 1;
            };

            Assert.Throws<CryptographicException>(() => kem.TryExportEncapsulationKey(bytes, out bytesWritten));
            Assert.Equal(2, kem.ExportEncapsulationKeyCoreCallCount);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void TryExportEncapsulationKey_UpperBound(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int upperBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeUpperBound(algorithm);

            byte[] bytes = new byte[upperBound];

            kem.ExportEncapsulationKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(2);
                return upperBound;
            };

            AssertExtensions.TrueExpression(kem.TryExportEncapsulationKey(bytes, out int bytesWritten));
            Assert.Equal(upperBound, bytesWritten);
            AssertExtensions.FilledWith<byte>(2, bytes);

            // Writing more than the upper bound isn't allowed.
            kem.ExportEncapsulationKeyCoreHook = destination => upperBound + 1;

            Assert.Throws<CryptographicException>(() => kem.TryExportEncapsulationKey(bytes, out bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void TryExportEncapsulationKey_KeyLargerThanDestination(CompositeMLKemAlgorithm algorithm)
        {
            int lowerBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm);
            int upperBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeUpperBound(algorithm);

            if (lowerBound == upperBound)
            {
                // Only variable sized keys can write more than the destination can hold.
                return;
            }

            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);

            kem.ExportEncapsulationKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(4);
                return upperBound;
            };

            byte[] bytes = new byte[lowerBound];
            AssertExtensions.FalseExpression(kem.TryExportEncapsulationKey(bytes, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(1, kem.ExportEncapsulationKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(0, bytes);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportEncapsulationKey_KeyLargerThanDestination(CompositeMLKemAlgorithm algorithm)
        {
            int lowerBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm);
            int upperBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeUpperBound(algorithm);

            if (lowerBound == upperBound)
            {
                return;
            }

            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            kem.ExportEncapsulationKeyCoreHook = _ => upperBound;

            byte[] bytes = new byte[lowerBound];
            Assert.Throws<CryptographicException>(() => kem.ExportEncapsulationKey(bytes));
            Assert.Equal(1, kem.ExportEncapsulationKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(0, bytes);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void TryExportDecapsulationKey_KeyLargerThanDestination(CompositeMLKemAlgorithm algorithm)
        {
            int lowerBound = CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(algorithm);
            int upperBound = CompositeMLKemTestData.ExpectedDecapsulationKeySizeUpperBound(algorithm);

            if (lowerBound == upperBound)
            {
                // Only variable sized keys can write more than the destination can hold.
                return;
            }

            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);

            kem.ExportDecapsulationKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(4);
                return upperBound;
            };

            byte[] bytes = new byte[lowerBound];
            AssertExtensions.FalseExpression(kem.TryExportDecapsulationKey(bytes, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(1, kem.ExportDecapsulationKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(0, bytes);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportDecapsulationKey_KeyLargerThanDestination(CompositeMLKemAlgorithm algorithm)
        {
            int lowerBound = CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(algorithm);
            int upperBound = CompositeMLKemTestData.ExpectedDecapsulationKeySizeUpperBound(algorithm);

            if (lowerBound == upperBound)
            {
                return;
            }

            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            kem.ExportDecapsulationKeyCoreHook = _ => upperBound;

            byte[] bytes = new byte[lowerBound];
            Assert.Throws<CryptographicException>(() => kem.ExportDecapsulationKey(bytes));
            Assert.Equal(1, kem.ExportDecapsulationKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(0, bytes);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportEncapsulationKey_Span(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm);

            byte[] bytes = new byte[lowerBound];

            // Buffer is too small
            Assert.Throws<CryptographicException>(() => kem.ExportEncapsulationKey(bytes.AsSpan(0, lowerBound - 1)));
            Assert.Equal(0, kem.ExportEncapsulationKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(0, bytes);

            kem.ExportEncapsulationKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);
                return lowerBound;
            };

            int bytesWritten = kem.ExportEncapsulationKey(bytes.AsSpan(0, lowerBound));
            Assert.Equal(1, kem.ExportEncapsulationKeyCoreCallCount);
            Assert.Equal(lowerBound, bytesWritten);
            AssertExtensions.FilledWith<byte>(1, bytes);

            // Buffer meets the lower bound, but returned value is too small
            kem.ExportEncapsulationKeyCoreHook = destination =>
            {
                destination.Fill(1);

                // Writing less than lower bound isn't allowed.
                return lowerBound - 1;
            };

            Assert.Throws<CryptographicException>(() => kem.ExportEncapsulationKey(bytes.AsSpan(0, lowerBound)));
            Assert.Equal(2, kem.ExportEncapsulationKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportEncapsulationKey_Array(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm);
            int upperBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeUpperBound(algorithm);

            kem.ExportEncapsulationKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(6);
                return lowerBound;
            };

            byte[] encapsulationKey = kem.ExportEncapsulationKey();
            Assert.Equal(1, kem.ExportEncapsulationKeyCoreCallCount);
            Assert.Equal(lowerBound, encapsulationKey.Length);
            AssertExtensions.FilledWith<byte>(6, encapsulationKey);

            // Writing less than the lower bound isn't allowed.
            kem.ExportEncapsulationKeyCoreHook = destination => lowerBound - 1;

            Assert.Throws<CryptographicException>(() => kem.ExportEncapsulationKey());
            Assert.Equal(2, kem.ExportEncapsulationKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void TryExportDecapsulationKey_LowerBound(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(algorithm);

            // Buffer is too small
            byte[] bytes = new byte[lowerBound - 1];
            AssertExtensions.FalseExpression(kem.TryExportDecapsulationKey(bytes, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(0, kem.ExportDecapsulationKeyCoreCallCount);

            // Buffer meets the lower bound
            bytes = new byte[lowerBound];

            kem.ExportDecapsulationKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);
                return lowerBound;
            };

            AssertExtensions.TrueExpression(kem.TryExportDecapsulationKey(bytes, out bytesWritten));
            Assert.Equal(lowerBound, bytesWritten);
            AssertExtensions.FilledWith<byte>(1, bytes);

            // Buffer meets the lower bound, but returned value is too small
            kem.ExportDecapsulationKeyCoreHook = destination =>
            {
                destination.Fill(1);

                // Writing less than lower bound isn't allowed.
                return lowerBound - 1;
            };

            Assert.Throws<CryptographicException>(() => kem.TryExportDecapsulationKey(bytes, out bytesWritten));
            Assert.Equal(2, kem.ExportDecapsulationKeyCoreCallCount);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void TryExportDecapsulationKey_UpperBound(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int upperBound = CompositeMLKemTestData.ExpectedDecapsulationKeySizeUpperBound(algorithm);

            byte[] bytes = new byte[upperBound];

            kem.ExportDecapsulationKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(2);
                return upperBound;
            };

            AssertExtensions.TrueExpression(kem.TryExportDecapsulationKey(bytes, out int bytesWritten));
            Assert.Equal(upperBound, bytesWritten);
            AssertExtensions.FilledWith<byte>(2, bytes);

            // Writing more than the upper bound isn't allowed.
            kem.ExportDecapsulationKeyCoreHook = destination => upperBound + 1;

            Assert.Throws<CryptographicException>(() => kem.TryExportDecapsulationKey(bytes, out bytesWritten));
            Assert.Equal(0, bytesWritten);

            // The destination is cleared when the core implementation misbehaves.
            AssertExtensions.FilledWith<byte>(0, bytes);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportDecapsulationKey_Span(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(algorithm);

            byte[] bytes = new byte[lowerBound];

            // Buffer is too small
            Assert.Throws<CryptographicException>(() => kem.ExportDecapsulationKey(bytes.AsSpan(0, lowerBound - 1)));
            Assert.Equal(0, kem.ExportDecapsulationKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(0, bytes);

            kem.ExportDecapsulationKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);
                return lowerBound;
            };

            int bytesWritten = kem.ExportDecapsulationKey(bytes.AsSpan(0, lowerBound));
            Assert.Equal(1, kem.ExportDecapsulationKeyCoreCallCount);
            Assert.Equal(lowerBound, bytesWritten);
            AssertExtensions.FilledWith<byte>(1, bytes);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportDecapsulationKey_Array(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(algorithm);
            int upperBound = CompositeMLKemTestData.ExpectedDecapsulationKeySizeUpperBound(algorithm);

            kem.ExportDecapsulationKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(6);
                return lowerBound;
            };

            byte[] decapsulationKey = kem.ExportDecapsulationKey();
            Assert.Equal(1, kem.ExportDecapsulationKeyCoreCallCount);
            Assert.Equal(lowerBound, decapsulationKey.Length);
            AssertExtensions.FilledWith<byte>(6, decapsulationKey);

            // Writing less than the lower bound isn't allowed.
            kem.ExportDecapsulationKeyCoreHook = destination => lowerBound - 1;

            Assert.Throws<CryptographicException>(() => kem.ExportDecapsulationKey());
            Assert.Equal(2, kem.ExportDecapsulationKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void TryExportPkcs8PrivateKey_LowerBound(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(algorithm);

            // A PKCS#8 blob is always larger than the raw key, so anything smaller than the
            // smallest possible key can be rejected without calling the core implementation.
            byte[] bytes = new byte[lowerBound - 1];
            AssertExtensions.FalseExpression(kem.TryExportPkcs8PrivateKey(bytes, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal(0, kem.TryExportPkcs8PrivateKeyCoreCallCount);

            bytes = new byte[lowerBound];
            kem.TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int written) =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);
                written = destination.Length;
                return true;
            };

            AssertExtensions.TrueExpression(kem.TryExportPkcs8PrivateKey(bytes, out bytesWritten));
            Assert.Equal(1, kem.TryExportPkcs8PrivateKeyCoreCallCount);
            Assert.Equal(lowerBound, bytesWritten);
            AssertExtensions.FilledWith<byte>(1, bytes);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportPkcs8PrivateKey_UsesCore(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            byte[] pkcs8 = CreatePkcs8PrivateKey(algorithm);

            kem.SetNoOpHooks();
            kem.AddFillDestination(pkcs8);

            CompositeMLKemTestHelpers.AssertExportPkcs8PrivateKey(kem, exported =>
                AssertExtensions.SequenceEqual(pkcs8, exported));

            AssertExtensions.GreaterThanOrEqualTo(kem.TryExportPkcs8PrivateKeyCoreCallCount, 3);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportPkcs8PrivateKey_GrowsBuffer(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int requiredSize = CompositeMLKemTestData.ExpectedDecapsulationKeySizeUpperBound(algorithm) * 4;

            kem.TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int written) =>
            {
                if (destination.Length < requiredSize)
                {
                    written = 0;
                    return false;
                }

                destination.Slice(0, requiredSize).Fill(8);
                written = requiredSize;
                return true;
            };

            byte[] exported = kem.ExportPkcs8PrivateKey();

            AssertExtensions.GreaterThanOrEqualTo(kem.TryExportPkcs8PrivateKeyCoreCallCount, 2);
            Assert.Equal(requiredSize, exported.Length);
            AssertExtensions.FilledWith<byte>(8, exported);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportPkcs8PrivateKey_CoreReturnsNonsense(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);

            kem.TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int written) =>
            {
                written = destination.Length + 1;
                return true;
            };

            Assert.Throws<CryptographicException>(() => kem.ExportPkcs8PrivateKey());

            kem.TryExportPkcs8PrivateKeyCoreHook = (Span<byte> destination, out int written) =>
            {
                written = -1;
                return true;
            };

            Assert.Throws<CryptographicException>(() => kem.ExportPkcs8PrivateKey());
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportSubjectPublicKeyInfo_Shape(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm);
            byte[] encapsulationKey = new byte[lowerBound];
            encapsulationKey.AsSpan().Fill(0xAA);

            kem.SetNoOpHooks();
            kem.AddFillDestination(encapsulationKey);

            CompositeMLKemTestHelpers.AssertExportSubjectPublicKeyInfo(kem, spki =>
            {
                SubjectPublicKeyInfoAsn decoded = SubjectPublicKeyInfoAsn.Decode(spki, AsnEncodingRules.DER);

                Assert.Equal(CompositeMLKemTestHelpers.AlgorithmToOid(algorithm), decoded.Algorithm.Algorithm);
                AssertExtensions.FalseExpression(decoded.Algorithm.Parameters.HasValue);
                AssertExtensions.SequenceEqual(encapsulationKey, decoded.SubjectPublicKey.ToArray());
            });
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportSubjectPublicKeyInfo_CoreReturnsInvalidSize(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm);

            kem.ExportEncapsulationKeyCoreHook = destination => lowerBound - 1;

            Assert.Throws<CryptographicException>(() => kem.ExportSubjectPublicKeyInfo());
            Assert.Throws<CryptographicException>(() => kem.ExportSubjectPublicKeyInfoPem());
            Assert.Throws<CryptographicException>(() => kem.TryExportSubjectPublicKeyInfo(new byte[4096], out _));
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void TryExportSubjectPublicKeyInfo_DestinationTooSmall(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLKemTestData.ExpectedEncapsulationKeySizeLowerBound(algorithm);
            byte[] encapsulationKey = new byte[lowerBound];

            kem.SetNoOpHooks();
            kem.AddFillDestination(encapsulationKey);

            AssertExtensions.FalseExpression(kem.TryExportSubjectPublicKeyInfo(new byte[lowerBound], out int bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "Password-based encryption requires AES, which is not supported on Browser.")]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportEncryptedPkcs8PrivateKey_ProducesEncryptedPrivateKeyInfo(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            byte[] pkcs8 = CreatePkcs8PrivateKey(algorithm);

            kem.SetNoOpHooks();
            kem.AddFillDestination(pkcs8);

            CompositeMLKemTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
            {
                byte[] encrypted = export(kem, "PLACEHOLDER", s_aes256Sha256Pbe);

                // EncryptedPrivateKeyInfo ::= SEQUENCE { encryptionAlgorithm AlgorithmIdentifier, encryptedData OCTET STRING }
                AsnReader reader = new AsnReader(encrypted, AsnEncodingRules.BER);
                AsnReader encryptedPrivateKeyInfo = reader.ReadSequence();
                AssertExtensions.FalseExpression(reader.HasData);

                encryptedPrivateKeyInfo.ReadSequence(); // encryptionAlgorithm
                AssertExtensions.GreaterThanOrEqualTo(encryptedPrivateKeyInfo.ReadOctetString().Length, pkcs8.Length);
                AssertExtensions.FalseExpression(encryptedPrivateKeyInfo.HasData);
            });
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "Password-based encryption requires AES, which is not supported on Browser.")]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void ExportEncryptedPkcs8PrivateKey_CoreCryptographicException_Propagates(CompositeMLKemAlgorithm algorithm)
        {
            CompositeMLKemTestHelpers.AssertEncryptedExportPkcs8PrivateKey(export =>
            {
                using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
                CryptographicException expected = new();

                kem.TryExportPkcs8PrivateKeyCoreHook =
                    (Span<byte> _, out int bytesWritten) =>
                    {
                        throw expected;
                    };

                Assert.Same(expected, Assert.Throws<CryptographicException>(() => export(kem, "PLACEHOLDER", s_aes256Sha256Pbe)));
            });
        }

        [Theory]
        [SkipOnPlatform(TestPlatforms.Browser, "Password-based encryption requires AES, which is not supported on Browser.")]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void TryExportEncryptedPkcs8PrivateKey_DestinationTooSmall(CompositeMLKemAlgorithm algorithm)
        {
            using CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            byte[] pkcs8 = CreatePkcs8PrivateKey(algorithm);

            kem.SetNoOpHooks();
            kem.AddFillDestination(pkcs8);

            AssertExtensions.FalseExpression(
                kem.TryExportEncryptedPkcs8PrivateKey("PLACEHOLDER", s_aes256Sha256Pbe, Span<byte>.Empty, out int bytesWritten));
            Assert.Equal(0, bytesWritten);

            AssertExtensions.FalseExpression(
                kem.TryExportEncryptedPkcs8PrivateKey("PLACEHOLDER".AsSpan(), s_aes256Sha256Pbe, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);

            AssertExtensions.FalseExpression(
                kem.TryExportEncryptedPkcs8PrivateKey("PLACEHOLDER"u8, s_aes256Sha256Pbe, Span<byte>.Empty, out bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void Dispose_IsIdempotentAndCallsCore(CompositeMLKemAlgorithm algorithm)
        {
            CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            kem.DisposeHook = disposing => AssertExtensions.TrueExpression(disposing);

            Assert.Equal(0, kem.DisposeCallCount);

            kem.Dispose();
            Assert.Equal(1, kem.DisposeCallCount);

            kem.Dispose();
            kem.Dispose();
            Assert.Equal(1, kem.DisposeCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void Dispose_ThrowsObjectDisposed(CompositeMLKemAlgorithm algorithm)
        {
            CompositeMLKemMockImplementation kem = CompositeMLKemMockImplementation.Create(algorithm);
            kem.Dispose();

            CompositeMLKemTestHelpers.VerifyDisposed(kem);

            // No core method should have been called.
            Assert.Equal(0, kem.EncapsulateCoreCallCount);
            Assert.Equal(0, kem.DecapsulateCoreCallCount);
            Assert.Equal(0, kem.ExportEncapsulationKeyCoreCallCount);
            Assert.Equal(0, kem.ExportDecapsulationKeyCoreCallCount);
            Assert.Equal(0, kem.TryExportPkcs8PrivateKeyCoreCallCount);
        }

        private static byte[] CreatePkcs8PrivateKey(CompositeMLKemAlgorithm algorithm) =>
            CreatePkcs8PrivateKey(algorithm, new byte[CompositeMLKemTestData.ExpectedDecapsulationKeySizeLowerBound(algorithm)]);

        private static byte[] CreatePkcs8PrivateKey(CompositeMLKemAlgorithm algorithm, byte[] decapsulationKey)
        {
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = CompositeMLKemTestHelpers.AlgorithmToOid(algorithm),
                    Parameters = default(ReadOnlyMemory<byte>?),
                },
                PrivateKey = decapsulationKey,
            };

            return pkcs8.Encode();
        }
    }
}
