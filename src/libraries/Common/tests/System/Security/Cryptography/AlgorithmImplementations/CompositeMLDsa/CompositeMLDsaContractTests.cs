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

            AssertExtensions.Throws<ArgumentException>("destination", () => dsa.SignData(ReadOnlySpan<byte>.Empty, new byte[maxSignatureSize - 1], []));

            // Context length must be less than 256
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => dsa.SignData(ReadOnlySpan<byte>.Empty, new byte[maxSignatureSize], new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => dsa.SignData(Array.Empty<byte>(), new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => dsa.VerifyData(ReadOnlySpan<byte>.Empty, new byte[maxSignatureSize], new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => dsa.VerifyData(Array.Empty<byte>(), new byte[maxSignatureSize], new byte[256]));
        }
        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPublicKey_LowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].PublicKeySizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa =>  rsa.KeySizeInBits / 8,
                    ecdsa => 1 + 2 * ((ecdsa.KeySizeInBits + 7) / 8),
                    eddsa => eddsa.KeySizeInBits / 8);

            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPublicKey(new byte[lowerBound - 1], out int bytesWritten));
            Assert.Equal(0, bytesWritten);

            dsa.TryExportCompositeMLDsaPublicKeyCoreHook = (destination, out bytesWritten) =>
            {
                AssertExtensions.LessThanOrEqualTo(lowerBound, destination.Length);
                bytesWritten = lowerBound;
                return true;
            };

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPublicKey(new byte[lowerBound], out bytesWritten));
            Assert.Equal(lowerBound, bytesWritten);

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPublicKey(new byte[lowerBound + 1], out bytesWritten));
            Assert.Equal(lowerBound, bytesWritten);

            dsa.TryExportCompositeMLDsaPublicKeyCoreHook = (destination, out bytesWritten) =>
            {
                // Writing less than lower bound isn't allowed.
                bytesWritten = lowerBound - 1;
                return true;
            };

            Assert.Throws<CryptographicException>(() => dsa.TryExportCompositeMLDsaPublicKey(new byte[lowerBound], out bytesWritten));
            Assert.Equal(0, bytesWritten);

            Assert.Throws<CryptographicException>(dsa.ExportCompositeMLDsaPublicKey);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPublicKey_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int? upperBoundOrNull = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].PublicKeySizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => default(int?),
                    ecdsa => 1 + 2 * ((ecdsa.KeySizeInBits + 7) / 8),
                    eddsa => eddsa.KeySizeInBits / 8);

            if (upperBoundOrNull is null)
            {
                return;
            }

            int upperBound = upperBoundOrNull.Value;

            dsa.TryExportCompositeMLDsaPublicKeyCoreHook = (destination, out bytesWritten) =>
            {
                bytesWritten = upperBound;
                return true;
            };

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPublicKey(new byte[upperBound], out int bytesWritten));
            Assert.Equal(upperBound, bytesWritten);

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPublicKey(new byte[upperBound + 1], out bytesWritten));
            Assert.Equal(upperBound, bytesWritten);

            dsa.TryExportCompositeMLDsaPublicKeyCoreHook = (destination, out bytesWritten) =>
            {
                // Writing more than upper bound isn't allowed.
                bytesWritten = upperBound + 1;
                return true;
            };

            Assert.Throws<CryptographicException>(() => dsa.TryExportCompositeMLDsaPublicKey(new byte[upperBound + 1], out bytesWritten));
            Assert.Equal(0, bytesWritten);

            Assert.Throws<CryptographicException>(dsa.ExportCompositeMLDsaPublicKey);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPrivateKey_LowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].PrivateSeedSizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => rsa.KeySizeInBits / 8,
                    ecdsa => 1 + ((ecdsa.KeySizeInBits + 7) / 8),
                    eddsa => eddsa.KeySizeInBits / 8);

            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPrivateKey(new byte[lowerBound - 1], out int bytesWritten));
            Assert.Equal(0, bytesWritten);

            dsa.TryExportCompositeMLDsaPrivateKeyCoreHook = (destination, out bytesWritten) =>
            {
                AssertExtensions.LessThanOrEqualTo(lowerBound, destination.Length);
                bytesWritten = lowerBound;
                return true;
            };

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPrivateKey(new byte[lowerBound], out bytesWritten));
            Assert.Equal(lowerBound, bytesWritten);

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPrivateKey(new byte[lowerBound + 1], out bytesWritten));
            Assert.Equal(lowerBound, bytesWritten);

            dsa.TryExportCompositeMLDsaPrivateKeyCoreHook = (destination, out bytesWritten) =>
            {
                // Writing less than lower bound isn't allowed.
                bytesWritten = lowerBound - 1;
                return true;
            };

            Assert.Throws<CryptographicException>(() => dsa.TryExportCompositeMLDsaPrivateKey(new byte[lowerBound], out bytesWritten));
            Assert.Equal(0, bytesWritten);

            Assert.Throws<CryptographicException>(dsa.ExportCompositeMLDsaPrivateKey);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPrivateKey_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int? upperBoundOrNull = CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].PrivateSeedSizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => default(int?),
                    ecdsa => default(int?),
                    eddsa => eddsa.KeySizeInBits / 8);

            if (upperBoundOrNull is null)
            {
                return;
            }

            int upperBound = upperBoundOrNull.Value;

            dsa.TryExportCompositeMLDsaPrivateKeyCoreHook = (destination, out bytesWritten) =>
            {
                bytesWritten = upperBound;
                return true;
            };

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPrivateKey(new byte[upperBound], out int bytesWritten));
            Assert.Equal(upperBound, bytesWritten);

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPrivateKey(new byte[upperBound + 1], out bytesWritten));
            Assert.Equal(upperBound, bytesWritten);

            dsa.TryExportCompositeMLDsaPrivateKeyCoreHook = (destination, out bytesWritten) =>
            {
                // Writing more than upper bound isn't allowed.
                bytesWritten = upperBound + 1;
                return true;
            };

            Assert.Throws<CryptographicException>(() => dsa.TryExportCompositeMLDsaPrivateKey(new byte[upperBound + 1], out bytesWritten));
            Assert.Equal(0, bytesWritten);

            Assert.Throws<CryptographicException>(dsa.ExportCompositeMLDsaPrivateKey);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void SignData_LowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int lowerBound = 32 + CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].SignatureSizeInBytes +
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    algorithm,
                    rsa => rsa.KeySizeInBits / 8,
                    ecdsa => 0,
                    eddsa => 2 * eddsa.KeySizeInBits / 8);

            Assert.Throws<ArgumentException>(() => dsa.SignData(ReadOnlySpan<byte>.Empty, new byte[lowerBound - 1]));
            Assert.Throws<ArgumentException>(() => dsa.SignData(ReadOnlySpan<byte>.Empty, new byte[algorithm.MaxSignatureSizeInBytes - 1]));

            dsa.SignDataCoreHook = (data, context, destination) =>
            {
                AssertExtensions.LessThanOrEqualTo(lowerBound, destination.Length);
                return lowerBound;
            };

            Assert.Equal(lowerBound, dsa.SignData(ReadOnlySpan<byte>.Empty, new byte[algorithm.MaxSignatureSizeInBytes]));
            Assert.Equal(lowerBound, dsa.SignData(ReadOnlySpan<byte>.Empty, new byte[algorithm.MaxSignatureSizeInBytes + 1]));

            AssertExtensions.GreaterThanOrEqualTo(algorithm.MaxSignatureSizeInBytes, lowerBound);

            dsa.SignDataCoreHook = (data, context, destination) =>
            {
                // Writing less than lower bound isn't allowed.
                return lowerBound - 1;
            };

            Assert.Throws<CryptographicException>(() => dsa.SignData(ReadOnlySpan<byte>.Empty, new byte[algorithm.MaxSignatureSizeInBytes]));
            Assert.Throws<CryptographicException>(() => dsa.SignData([]));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void SignData_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);

            int upperBound = algorithm.MaxSignatureSizeInBytes;

            dsa.SignDataCoreHook = (data, context, destination) =>
            {
                Assert.Equal(upperBound, destination.Length);
                return upperBound;
            };

            Assert.Equal(upperBound, dsa.SignData(ReadOnlySpan<byte>.Empty, new byte[upperBound]));
            Assert.Equal(upperBound, dsa.SignData(ReadOnlySpan<byte>.Empty, new byte[upperBound + 1]));

            dsa.SignDataCoreHook = (data, context, destination) =>
            {
                // Writing more than upper bound isn't allowed.
                return upperBound + 1;
            };

            Assert.Throws<CryptographicException>(() => dsa.SignData(ReadOnlySpan<byte>.Empty, new byte[upperBound + 1]));
            Assert.Throws<CryptographicException>(() => dsa.SignData([]));
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

            CompositeMLDsaTestHelpers.ExecuteComponentAction(
                vector.Algorithm,
                // RSA doesn't have an exact size, so it will use pooled buffers. Their sizes are powers of two.
                rsa => AssertExtensions.LessThanOrEqualTo(mldsaKeySize + (rsa.KeySizeInBits / 8) * 2 + 16, initialBufferSize),
                ecdsa => Assert.Equal(mldsaKeySize + 1 + 2 * ((ecdsa.KeySizeInBits + 7) / 8), initialBufferSize),
                eddsa => Assert.Equal(mldsaKeySize + eddsa.KeySizeInBits / 8, initialBufferSize));

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
                // Buffer is always big enough, but it may be too big for a valid key, so bound it with an actual key.
                bytesWritten = Math.Min(vector.SecretKey.Length, destination.Length);
                initialBufferSize = destination.Length;
                return true;
            };

            _ = dsa.ExportCompositeMLDsaPrivateKey();

            int mldsaKeySize = CompositeMLDsaTestHelpers.MLDsaAlgorithms[vector.Algorithm].PrivateSeedSizeInBytes;

            CompositeMLDsaTestHelpers.ExecuteComponentAction(
                vector.Algorithm,
                // RSA and ECDSA don't have an exact size, so it will use pooled buffers. Their sizes are powers of two.
                rsa => AssertExtensions.LessThanOrEqualTo(mldsaKeySize + (rsa.KeySizeInBits / 8) * 2 + (rsa.KeySizeInBits / 8) / 2 * 5 + 64, initialBufferSize),
                ecdsa => AssertExtensions.LessThanOrEqualTo(mldsaKeySize + 1 + ((ecdsa.KeySizeInBits + 7) / 8) + 1 + 2 * ((ecdsa.KeySizeInBits + 7) / 8) + 64, initialBufferSize),
                eddsa => Assert.Equal(mldsaKeySize + eddsa.KeySizeInBits / 8, initialBufferSize));

            AssertExtensions.Equal(1, dsa.TryExportCompositeMLDsaPrivateKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void SignData_BufferSize(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            int testSignatureSize = vector.Algorithm.MaxSignatureSizeInBytes -
                // Test returning less than maximum size for non-fixed size signatures.
                CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                    vector.Algorithm,
                    rsa => 0,
                    ecdsa => 1,
                    eddsa => 0);

            dsa.SignDataCoreHook = (data, context, destination) =>
            {
                Assert.Equal(vector.Algorithm.MaxSignatureSizeInBytes, destination.Length);
                return testSignatureSize;
            };

            byte[] signature = dsa.SignData(vector.Message);
            Assert.Equal(testSignatureSize, signature.Length);

            signature = new byte[vector.Algorithm.MaxSignatureSizeInBytes];
            dsa.AddSignatureBufferIsSameAssertion(signature);

            Assert.Equal(testSignatureSize, dsa.SignData(vector.Message, signature, []));

            AssertExtensions.Equal(2, dsa.SignDataCoreCallCount);
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
            dsa.SignDataCoreHook = (_, _, _) => { return -1; };
            dsa.AddFillDestination(vector.Signature);
            dsa.AddDataBufferIsSameAssertion(vector.Message);
            dsa.AddContextBufferIsSameAssertion(Array.Empty<byte>());

            byte[] exported = dsa.SignData(vector.Message, Array.Empty<byte>());
            AssertExtensions.LessThan(0, dsa.SignDataCoreCallCount);
            AssertExtensions.SequenceEqual(exported, vector.Signature);

            byte[] signature = CreatePaddedFilledArray(vector.Signature.Length, 42);

            // Extra bytes in destination buffer should not be touched
            Memory<byte> destination = signature.AsMemory(PaddingSize, vector.Algorithm.MaxSignatureSizeInBytes);
            dsa.AddDestinationBufferIsSameAssertion(destination);
            dsa.SignDataCoreCallCount = 0;

            int bytesWritten = dsa.SignData(vector.Message, destination.Span, Array.Empty<byte>());
            Assert.Equal(vector.Signature.Length, bytesWritten);
            Assert.Equal(1, dsa.SignDataCoreCallCount);
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
            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPublicKey(new byte[vector.PublicKey.Length], out int bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPrivateKey_CoreReturnsFalse(CompositeMLDsaTestVector vector)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(vector.Algorithm);
            dsa.TryExportCompositeMLDsaPrivateKeyCoreHook = (_, out w) => { w = 0; return false; };
            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPrivateKey(new byte[vector.SecretKey.Length], out int bytesWritten));
            Assert.Equal(0, bytesWritten);
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
        public static void TryExportPublicKeyCore_ExactSize_ReturnFalse(CompositeMLDsaAlgorithm algorithm)
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
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportPrivateKeyCore_ExactSize_ReturnFalse(CompositeMLDsaAlgorithm algorithm)
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
    }
}
