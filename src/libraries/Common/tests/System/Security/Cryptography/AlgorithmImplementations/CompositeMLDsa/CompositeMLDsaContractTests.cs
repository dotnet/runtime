// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Sdk;
using CompositeMLDsaTestVector = System.Security.Cryptography.Tests.CompositeMLDsaTestData.CompositeMLDsaTestVector;

namespace System.Security.Cryptography.Tests
{
    public static class CompositeMLDsaContractTests
    {
        public static IEnumerable<object[]> ArgumentValidationData =>
            from algorithm in CompositeMLDsaTestData.AllAlgorithms
            from shouldDispose in new[] { true, false }
            select new object[] { algorithm, shouldDispose };

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public static void NullArgumentValidation(CompositeMLDsaAlgorithm algorithm, bool shouldDispose)
        {
            using CompositeMLDsa dsa = CompositeMLDsaMockImplementation.Create(algorithm);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                dsa.Dispose();
            }

            AssertExtensions.Throws<ArgumentNullException>("data", () => dsa.SignData(null));
            AssertExtensions.Throws<ArgumentNullException>("data", () => dsa.VerifyData(null, null));

            AssertExtensions.Throws<ArgumentNullException>("signature", () => dsa.VerifyData(Array.Empty<byte>(), null));
        }

        [Fact]
        public static void ArgumentValidation_Ctor_NullAlgorithm()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => new CompositeMLDsaMockImplementation(null));
        }

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public static void ArgumentValidation(CompositeMLDsaAlgorithm algorithm, bool shouldDispose)
        {
            using CompositeMLDsa dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int maxSignatureSize = algorithm.MaxSignatureSizeInBytes;

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                dsa.Dispose();
            }

            // Context length must be less than 256
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => dsa.TrySignData(ReadOnlySpan<byte>.Empty, new byte[maxSignatureSize], out _, new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => dsa.SignData(Array.Empty<byte>(), new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => dsa.VerifyData(ReadOnlySpan<byte>.Empty, new byte[maxSignatureSize], new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => dsa.VerifyData(Array.Empty<byte>(), new byte[maxSignatureSize], new byte[256]));
        }
        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPublicKey_Bounds(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].PublicKeySizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa =>  rsa.KeySizeInBits / 8,
                    ecdsa => 1 + 2 * ((ecdsa.KeySizeInBits + 7) / 8),
                    eddsa => eddsa.KeySizeInBits / 8);

            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPublicKey(new byte[lowerBound - 1], out _));

            dsa.TryExportCompositeMLDsaPublicKeyCoreHook = (destination, out bytesWritten) =>
            {
                AssertExtensions.LessThanOrEqualTo(lowerBound, destination.Length);
                bytesWritten = lowerBound;
                return true;
            };

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPublicKey(new byte[lowerBound], out int writtenBytes));
            Assert.Equal(lowerBound, writtenBytes);

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPublicKey(new byte[lowerBound + 1], out writtenBytes));
            Assert.Equal(lowerBound, writtenBytes);

            dsa.TryExportCompositeMLDsaPublicKeyCoreHook = (destination, out bytesWritten) =>
            {
                // Writing less than lower bound isn't allowed.
                bytesWritten = lowerBound - 1;
                return true;
            };

            Assert.Throws<CryptographicException>(() => dsa.TryExportCompositeMLDsaPublicKey(new byte[lowerBound], out _));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPrivateKey_Bounds(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].PrivateSeedSizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => rsa.KeySizeInBits / 8,
                    ecdsa => 1 + ((ecdsa.KeySizeInBits + 7) / 8),
                    eddsa => eddsa.KeySizeInBits / 8);

            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPrivateKey(new byte[lowerBound - 1], out _));

            dsa.TryExportCompositeMLDsaPrivateKeyCoreHook = (destination, out bytesWritten) =>
            {
                AssertExtensions.LessThanOrEqualTo(lowerBound, destination.Length);
                bytesWritten = lowerBound;
                return true;
            };

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPrivateKey(new byte[lowerBound], out int writtenBytes));
            Assert.Equal(lowerBound, writtenBytes);

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPrivateKey(new byte[lowerBound + 1], out writtenBytes));
            Assert.Equal(lowerBound, writtenBytes);

            dsa.TryExportCompositeMLDsaPrivateKeyCoreHook = (destination, out bytesWritten) =>
            {
                // Writing less than lower bound isn't allowed.
                bytesWritten = lowerBound - 1;
                return true;
            };

            Assert.Throws<CryptographicException>(() => dsa.TryExportCompositeMLDsaPrivateKey(new byte[lowerBound], out _));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TrySignData_Bounds(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int lowerBound = 32 + CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].SignatureSizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => rsa.KeySizeInBits / 8,
                    ecdsa => 0,
                    eddsa => 2 * eddsa.KeySizeInBits / 8);

            AssertExtensions.FalseExpression(dsa.TrySignData(ReadOnlySpan<byte>.Empty, new byte[lowerBound - 1], out _));

            dsa.TrySignDataCoreHook = (data, context, destination, out bytesWritten) =>
            {
                AssertExtensions.LessThanOrEqualTo(lowerBound, destination.Length);
                bytesWritten = lowerBound;
                return true;
            };

            AssertExtensions.TrueExpression(dsa.TrySignData(ReadOnlySpan<byte>.Empty, new byte[lowerBound], out int writtenBytes));
            Assert.Equal(lowerBound, writtenBytes);

            AssertExtensions.TrueExpression(dsa.TrySignData(ReadOnlySpan<byte>.Empty, new byte[lowerBound + 1], out writtenBytes));
            Assert.Equal(lowerBound, writtenBytes);

            AssertExtensions.GreaterThanOrEqualTo(algorithm.MaxSignatureSizeInBytes, lowerBound);

            dsa.TrySignDataCoreHook = (data, context, destination, out bytesWritten) =>
            {
                // Writing less than lower bound isn't allowed.
                bytesWritten = lowerBound - 1;
                return true;
            };

            Assert.Throws<CryptographicException>(() => dsa.TrySignData(ReadOnlySpan<byte>.Empty, new byte[lowerBound], out _));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void VerifyData_Threshold(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int threshold =
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    _ => algorithm.MaxSignatureSizeInBytes,
                    _ => 32 + CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].SignatureSizeInBytes,
                    _ => algorithm.MaxSignatureSizeInBytes);

            AssertExtensions.FalseExpression(dsa.VerifyData(ReadOnlySpan<byte>.Empty, new byte[threshold - 1]));

            dsa.VerifyDataCoreHook = (data, signature, context) => true;

            AssertExtensions.TrueExpression(dsa.VerifyData(ReadOnlySpan<byte>.Empty, new byte[threshold]));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPublicKey_InitialBuffer(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            int initialBufferSize = -1;

            dsa.TryExportCompositeMLDsaPublicKeyCoreHook = (destination, out bytesWritten) =>
            {
                // Buffer is always big enough, but it may bee too big for a valid key, so bound it with an actual key.
                bytesWritten = Math.Min(vector.PublicKey.Length, destination.Length);
                initialBufferSize = destination.Length;
                return true;
            };

            _ = dsa.ExportCompositeMLDsaPublicKey();

            int mldsaKeySize = CompositeMLDsaTestHelpers.MLDsaAlgorithms[vector.Algorithm].PublicKeySizeInBytes;
            int expectedInitialBufferSize = CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                vector.Algorithm,
                // RSA doesn't have an exact size, so it will use pooled buffers. Their sizes are powers of two.
                rsa => RoundUpToPowerOfTwo(mldsaKeySize + (rsa.KeySizeInBits / 8) * 2 + 16),
                ecdsa => mldsaKeySize + 1 + 2 * ((ecdsa.KeySizeInBits + 7) / 8),
                eddsa => mldsaKeySize + eddsa.KeySizeInBits / 8);

            Assert.Equal(expectedInitialBufferSize, initialBufferSize);
            AssertExtensions.Equal(1, dsa.TryExportCompositeMLDsaPublicKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPrivateKey_InitialBuffer(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            int initialBufferSize = -1;

            dsa.TryExportCompositeMLDsaPrivateKeyCoreHook = (destination, out bytesWritten) =>
            {
                // Buffer is always big enough, but it may bee too big for a valid key, so bound it with an actual key.
                bytesWritten = Math.Min(vector.SecretKey.Length, destination.Length);
                initialBufferSize = destination.Length;
                return true;
            };

            _ = dsa.ExportCompositeMLDsaPrivateKey();

            int mldsaKeySize = CompositeMLDsaTestHelpers.MLDsaAlgorithms[vector.Algorithm].PrivateSeedSizeInBytes;
            int expectedInitialBufferSize = CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                vector.Algorithm,
                // RSA and ECDSA don't have an exact size, so it will use pooled buffers. Their sizes are powers of two.
                rsa => RoundUpToPowerOfTwo(mldsaKeySize + (rsa.KeySizeInBits / 8) * 2 + (rsa.KeySizeInBits / 8) / 2 * 5 + 64),
                ecdsa => RoundUpToPowerOfTwo(mldsaKeySize + 1 + ((ecdsa.KeySizeInBits + 7) / 8) + 1 + 2 * ((ecdsa.KeySizeInBits + 7) / 8) + 64),
                eddsa => mldsaKeySize + eddsa.KeySizeInBits / 8);

            Assert.Equal(expectedInitialBufferSize, initialBufferSize);
            AssertExtensions.Equal(1, dsa.TryExportCompositeMLDsaPrivateKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void SignData_InitialBuffer(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            int initialBufferSize = -1;

            dsa.TrySignDataCoreHook = (data, context, destination, out bytesWritten) =>
            {
                // Buffer is always big enough, but it may bee too big for a valid signature, so bound it with an actual signature.
                bytesWritten = Math.Min(vector.Signature.Length, destination.Length);
                initialBufferSize = destination.Length;
                return true;
            };

            _ = dsa.SignData(vector.Message);

            // Pooled buffer sizes are powers of two, but RSA and EdDSA have fixed size signature,
            // so they shouldn't pool their intermediate buffer.
            int expectedInitialBufferSize =
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    vector.Algorithm,
                    _ => vector.Algorithm.MaxSignatureSizeInBytes,
                    _ => RoundUpToPowerOfTwo(vector.Algorithm.MaxSignatureSizeInBytes),
                    _ => vector.Algorithm.MaxSignatureSizeInBytes);

            Assert.Equal(expectedInitialBufferSize, initialBufferSize);
            AssertExtensions.Equal(1, dsa.TrySignDataCoreCallCount);
        }

        private const int PaddingSize = 10;

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPublicKey_CallsCore(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            dsa.TryExportCompositeMLDsaPublicKeyCoreHook = (_, out x) => { x = 42; return true; };
            dsa.AddFillDestination(vector.PublicKey);

            byte[] exported = dsa.ExportCompositeMLDsaPublicKey();
            AssertExtensions.LessThan(0, dsa.TryExportCompositeMLDsaPublicKeyCoreCallCount);
            AssertExtensions.SequenceEqual(exported, vector.PublicKey);

            byte[] publicKey = CreatePaddedFilledArray(vector.PublicKey.Length, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = publicKey.AsMemory(PaddingSize, vector.PublicKey.Length);
            dsa.AddDestinationBufferIsSameAssertion(destination);
            dsa.TryExportCompositeMLDsaPublicKeyCoreCallCount = 0;

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPublicKey(destination.Span, out int bytesWritten));
            Assert.Equal(vector.PublicKey.Length, bytesWritten);
            Assert.Equal(1, dsa.TryExportCompositeMLDsaPublicKeyCoreCallCount);
            AssertExpectedFill(publicKey, vector.PublicKey, PaddingSize, 42);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPrivateKey_CallsCore(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            dsa.TryExportCompositeMLDsaPrivateKeyCoreHook = (_, out x) => { x = 42; return true; };
            dsa.AddFillDestination(vector.SecretKey);

            byte[] exported = dsa.ExportCompositeMLDsaPrivateKey();
            AssertExtensions.LessThan(0, dsa.TryExportCompositeMLDsaPrivateKeyCoreCallCount);
            AssertExtensions.SequenceEqual(exported, vector.SecretKey);

            byte[] secretKey = CreatePaddedFilledArray(vector.SecretKey.Length, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = secretKey.AsMemory(PaddingSize, vector.SecretKey.Length);
            dsa.AddDestinationBufferIsSameAssertion(destination);
            dsa.TryExportCompositeMLDsaPrivateKeyCoreCallCount = 0;

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPrivateKey(destination.Span, out int bytesWritten));
            Assert.Equal(vector.SecretKey.Length, bytesWritten);
            Assert.Equal(1, dsa.TryExportCompositeMLDsaPrivateKeyCoreCallCount);
            AssertExpectedFill(secretKey, vector.SecretKey, PaddingSize, 42);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TrySignData_CallsCore(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            dsa.TrySignDataCoreHook = (_, _, _, out x) => { x = 42; return true; };
            dsa.AddFillDestination(vector.Signature);
            dsa.AddDataBufferIsSameAssertion(vector.Message);
            dsa.AddContextBufferIsSameAssertion(Array.Empty<byte>());

            byte[] exported = dsa.SignData(vector.Message, Array.Empty<byte>());
            AssertExtensions.LessThan(0, dsa.TrySignDataCoreCallCount);
            AssertExtensions.SequenceEqual(exported, vector.Signature);

            byte[] signature = CreatePaddedFilledArray(vector.Signature.Length, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = signature.AsMemory(PaddingSize, vector.Signature.Length);
            dsa.AddDestinationBufferIsSameAssertion(destination);
            dsa.TrySignDataCoreCallCount = 0;

            dsa.TrySignData(vector.Message, destination.Span, out int bytesWritten, Array.Empty<byte>());
            Assert.Equal(vector.Signature.Length, bytesWritten);
            Assert.Equal(1, dsa.TrySignDataCoreCallCount);
            AssertExpectedFill(signature, vector.Signature, PaddingSize, 42);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void VerifyData_CallsCore(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            dsa.VerifyDataCoreHook = (_, _, _) => true;
            dsa.AddDataBufferIsSameAssertion(vector.Message);
            dsa.AddSignatureBufferIsSameAssertion(vector.Signature);
            dsa.AddContextBufferIsSameAssertion(Array.Empty<byte>());

            AssertExtensions.TrueExpression(dsa.VerifyData(vector.Message, vector.Signature, Array.Empty<byte>()));
            AssertExtensions.Equal(1, dsa.VerifyDataCoreCallCount);

            AssertExtensions.TrueExpression(dsa.VerifyData(vector.Message, vector.Signature, Array.Empty<byte>()));
            AssertExtensions.Equal(2, dsa.VerifyDataCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPublicKey_CoreReturnsFals(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            dsa.TryExportCompositeMLDsaPublicKeyCoreHook = (_, out w) => { w = 0; return false; };
            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPublicKey(new byte[vector.PublicKey.Length], out _));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPrivateKey_CoreReturnsFalse(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            dsa.TryExportCompositeMLDsaPrivateKeyCoreHook = (_, out w) => { w = 0; return false; };
            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPrivateKey(new byte[vector.SecretKey.Length], out _));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TrySignData_CoreReturnsFalse(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            dsa.TrySignDataCoreHook = (_, _, _, out w) => { w = 0; return false; };
            AssertExtensions.FalseExpression(dsa.TrySignData(vector.Message, new byte[vector.Signature.Length], out _, Array.Empty<byte>()));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void VerifyData_CoreReturnsFalse(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            dsa.VerifyDataCoreHook = (_, _, _) => false;
            AssertExtensions.FalseExpression(dsa.VerifyData(vector.Message, vector.Signature, Array.Empty<byte>()));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void Sign_ExactSize_CoreError(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);

            int? exactSignatureSize =
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => algorithm.MaxSignatureSizeInBytes,
                    ecdsa => default(int?),
                    eddsa => algorithm.MaxSignatureSizeInBytes);

            if (exactSignatureSize is null)
                return;

            dsa.TrySignDataCoreHook =
                (data, context, destination, out w) =>
                {
                    int expectedSize = exactSignatureSize.Value;
                    Assert.Equal(expectedSize, destination.Length);

                    // Destination is exactly sized, so this should never return false.
                    // Caller should validate and throw.
                    w = 0;
                    return false;
                };

            Assert.Throws<CryptographicException>(() => dsa.SignData([]));

            dsa.TrySignDataCoreHook =
                (data, context, destination, out w) =>
                {
                    int expectedSize = exactSignatureSize.Value;
                    Assert.Equal(expectedSize, destination.Length);

                    // Destination is exactly sized, so written bytes should be the same as the destination length.
                    // Caller should validate and throw.
                    w = expectedSize - 1;
                    return true;
                };

            Assert.Throws<CryptographicException>(() => dsa.SignData([]));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportPublicKey_ExactSize_CoreError(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int? exactPublicKeySize = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].PublicKeySizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => default(int?),
                    ecdsa => 1 + 2 * ((ecdsa.KeySizeInBits + 7) / 8),
                    eddsa => eddsa.KeySizeInBits / 8);

            if (exactPublicKeySize is null)
                return;

            dsa.TryExportCompositeMLDsaPublicKeyCoreHook =
                (destination, out w) =>
                {
                    int expectedSize = exactPublicKeySize.Value;
                    Assert.Equal(expectedSize, destination.Length);

                    // Destination is exactly sized, so this should never return false.
                    // Caller should validate and throw.
                    w = 0;
                    return false;
                };

            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPublicKey());

            dsa.TryExportCompositeMLDsaPublicKeyCoreHook =
                (destination, out w) =>
                {
                    int expectedSize = exactPublicKeySize.Value;
                    Assert.Equal(expectedSize, destination.Length);

                    // Destination is exactly sized, so written bytes should be the same as the destination length.
                    // Caller should validate and throw.
                    w = expectedSize - 1;
                    return true;
                };

            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPublicKey());
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportPrivateKey_ExactSize_CoreError(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int? exactPrivateKeySize = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].PrivateSeedSizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => default(int?),
                    ecdsa => default(int?),
                    eddsa => eddsa.KeySizeInBits / 8);

            if (exactPrivateKeySize is null)
                return;

            dsa.TryExportCompositeMLDsaPrivateKeyCoreHook =
                (destination, out w) =>
                {
                    int expectedSize = exactPrivateKeySize.Value;
                    Assert.Equal(expectedSize, destination.Length);

                    // Destination is exactly sized, so this should never return false.
                    // Caller should validate and throw.
                    w = 0;
                    return false;
                };

            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPrivateKey());

            dsa.TryExportCompositeMLDsaPrivateKeyCoreHook =
                (destination, out w) =>
                {
                    int expectedSize = exactPrivateKeySize.Value;
                    Assert.Equal(expectedSize, destination.Length);

                    // Destination is exactly sized, so written bytes should be the same as the destination length.
                    // Caller should validate and throw.
                    w = expectedSize - 1;
                    return true;
                };

            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPrivateKey());
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void Dispose_CallsVirtual(CompositeMLDsaAlgorithm algorithm)
        {
            CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            bool disposeCalled = false;

            // First Dispose call should invoke overridden Dispose should be called
            dsa.DisposeHook = (bool disposing) =>
            {
                AssertExtensions.TrueExpression(disposing);
                disposeCalled = true;
            };

            dsa.Dispose();
            AssertExtensions.TrueExpression(disposeCalled);

            // Subsequent Dispose calls should be a no-op
            dsa.DisposeHook = _ => Assert.Fail();

            dsa.Dispose();
            dsa.Dispose(); // no throw

            CompositeMLDsaTestHelpers.VerifyDisposed(dsa);
        }

        private static void AssertExpectedFill(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> content, int offset, byte paddingElement)
        {
            // Ensure that the data was filled correctly
            AssertExtensions.SequenceEqual(content, buffer.Slice(offset, content.Length));

            // And that the padding was not touched
            AssertExtensions.FilledWith(paddingElement, buffer.Slice(0, offset));
            AssertExtensions.FilledWith(paddingElement, buffer.Slice(offset + content.Length));
        }

        private static byte[] CreatePaddedFilledArray(int size, byte filling)
        {
            byte[] publicKey = new byte[size + 2 * PaddingSize];
            publicKey.AsSpan().Fill(filling);
            return publicKey;
        }

        private static int RoundUpToPowerOfTwo(int value)
        {
            if (value <= 0)
            {
                throw new XunitException("Value must be positive.");
            }

            int power = 1;

            while (power < value)
            {
                power <<= 1;
            }

            return power;
        }
    }
}
