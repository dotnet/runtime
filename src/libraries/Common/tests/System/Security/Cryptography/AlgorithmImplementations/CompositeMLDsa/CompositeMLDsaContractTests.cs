// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

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
            int lowerBound = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeLowerBound(algorithm);

            // Buffer is too small
            byte[] bytes = new byte[lowerBound - 1];
            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPublicKey(bytes, out int bytesWritten));
            Assert.Equal(0, bytesWritten);

            // Buffer meets the lower bound
            bytes = new byte[lowerBound];

            dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);
                return lowerBound;
            };

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPublicKey(bytes, out bytesWritten));
            Assert.Equal(lowerBound, bytesWritten);
            AssertExtensions.FilledWith<byte>(1, bytes);
            Assert.Equal(lowerBound, bytesWritten);

            // Buffer meets the lower bound, but returned value is too small
            dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);

                // Writing less than lower bound isn't allowed.
                return lowerBound - 1;
            };

            Assert.Throws<CryptographicException>(() => dsa.TryExportCompositeMLDsaPublicKey(bytes, out bytesWritten));
            Assert.Equal(2, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPublicKey_Span_LowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeLowerBound(algorithm);

            byte[] bytes = new byte[lowerBound];

            // Buffer is too small
            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPublicKey(bytes.AsSpan(0, lowerBound - 1)));
            AssertExtensions.FilledWith<byte>(0, bytes);

            // Buffer meets the lower bound
            dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);
                return lowerBound;
            };

            int bytesWritten = dsa.ExportCompositeMLDsaPublicKey(bytes.AsSpan(0, lowerBound));
            Assert.Equal(1, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(1, bytes);
            Assert.Equal(lowerBound, bytesWritten);

            // Buffer meets the lower bound, but returned value is too small
            dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);
                // Writing less than lower bound isn't allowed.
                return lowerBound - 1;
            };

            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPublicKey(bytes.AsSpan(0, lowerBound)));
            Assert.Equal(2, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPublicKey_Array_LowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeLowerBound(algorithm);

            dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);

                // Writing less than lower bound isn't allowed.
                return lowerBound - 1;
            };

            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPublicKey());
            Assert.Equal(1, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPublicKey_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int upperBound = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeUpperBound(algorithm);

            // Buffer is the max size
            byte[] bytes = new byte[upperBound];

            dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(1);

                return upperBound;
            };

            dsa.AddDestinationBufferIsSameAssertion(bytes);

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPublicKey(bytes, out int bytesWritten));
            Assert.Equal(upperBound, bytesWritten);
            Assert.Equal(1, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(1, bytes);

            // Buffer is the max size, but returned value is too big
            dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
            {
                // Writing more than upper bound isn't allowed.
                return upperBound + 1;
            };

            dsa.AddDestinationBufferIsSameAssertion(bytes);

            Assert.Throws<CryptographicException>(() => dsa.TryExportCompositeMLDsaPublicKey(bytes, out bytesWritten));
            Assert.Equal(2, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPublicKey_Span_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int upperBound = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeUpperBound(algorithm);

            byte[] bytes = new byte[upperBound];

            // Buffer is the max size
            dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(1);
                return upperBound;
            };

            dsa.AddDestinationBufferIsSameAssertion(bytes);

            int bytesWritten = dsa.ExportCompositeMLDsaPublicKey(bytes);
            Assert.Equal(1, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(1, bytes);
            Assert.Equal(upperBound, bytesWritten);

            // Buffer is the max size, but returned value is too big
            dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
            {
                // Writing more than upper bound isn't allowed.
                return upperBound + 1;
            };

            dsa.AddDestinationBufferIsSameAssertion(bytes);

            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPublicKey(bytes.AsSpan(0, upperBound)));
            Assert.Equal(2, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPublicKey_Array_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int upperBound = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeUpperBound(algorithm);

            dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
            {
                // Writing more than upper bound isn't allowed.
                return upperBound + 1;
            };

            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPublicKey());
            Assert.Equal(1, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPrivateKey_LowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeLowerBound(algorithm);

            // Buffer is too small
            byte[] bytes = new byte[lowerBound - 1];
            AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPrivateKey(bytes, out int bytesWritten));
            Assert.Equal(0, bytesWritten);

            // Buffer meets the lower bound
            bytes = new byte[lowerBound];

            dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);
                return lowerBound;
            };

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPrivateKey(bytes, out bytesWritten));
            Assert.Equal(1, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(1, bytes);
            Assert.Equal(lowerBound, bytesWritten);

            // Buffer meets the lower bound, but returned value is too small
            dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);

                // Writing less than lower bound isn't allowed.
                return lowerBound - 1;
            };

            bytes.AsSpan().Clear();

            Assert.Throws<CryptographicException>(() => dsa.TryExportCompositeMLDsaPrivateKey(bytes, out bytesWritten));
            Assert.Equal(2, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(0, bytes);
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPrivateKey_Span_LowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeLowerBound(algorithm);

            byte[] bytes = new byte[lowerBound];

            // Buffer is too small
            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPrivateKey(bytes.AsSpan(0, lowerBound - 1)));
            AssertExtensions.FilledWith<byte>(0, bytes);

            // Buffer meets the lower bound
            dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);
                return lowerBound;
            };

            int bytesWritten = dsa.ExportCompositeMLDsaPrivateKey(bytes.AsSpan(0, lowerBound));
            Assert.Equal(1, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(1, bytes);
            Assert.Equal(lowerBound, bytesWritten);

            // Buffer meets the lower bound, but returned value is too small
            dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);

                // Writing less than lower bound isn't allowed.
                return lowerBound - 1;
            };

            bytes.AsSpan().Clear();

            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPrivateKey(bytes.AsSpan(0, lowerBound)));
            Assert.Equal(2, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(0, bytes);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPrivateKey_Array_LowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int lowerBound = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeLowerBound(algorithm);

            dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
                destination.Fill(1);

                // Writing less than lower bound isn't allowed.
                return lowerBound - 1;
            };

            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPrivateKey());
            Assert.Equal(1, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPrivateKey_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int upperBound = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeUpperBound(algorithm);

            // Buffer is the max size
            byte[] bytes = new byte[upperBound];

            dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(1);

                return upperBound;
            };

            dsa.AddDestinationBufferIsSameAssertion(bytes);

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPrivateKey(bytes, out int bytesWritten));
            Assert.Equal(1, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
            Assert.Equal(upperBound, bytesWritten);
            AssertExtensions.FilledWith<byte>(1, bytes);

            // Buffer is the max size, but returned value is too big
            dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(1);

                // Writing more than upper bound isn't allowed.
                return upperBound + 1;
            };

            dsa.AddDestinationBufferIsSameAssertion(bytes);

            bytes.AsSpan().Clear();

            Assert.Throws<CryptographicException>(() => dsa.TryExportCompositeMLDsaPrivateKey(bytes, out bytesWritten));
            Assert.Equal(2, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
            Assert.Equal(0, bytesWritten);
            AssertExtensions.FilledWith<byte>(0, bytes);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPrivateKey_Span_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int upperBound = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeUpperBound(algorithm);

            byte[] bytes = new byte[upperBound];

            // Buffer is the max size
            dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(1);

                return upperBound;
            };

            dsa.AddDestinationBufferIsSameAssertion(bytes);

            int bytesWritten = dsa.ExportCompositeMLDsaPrivateKey(bytes);
            Assert.Equal(1, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(1, bytes);
            Assert.Equal(upperBound, bytesWritten);

            // Buffer is the max size, but returned value is too big
            dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(1);

                // Writing more than upper bound isn't allowed.
                return upperBound + 1;
            };

            dsa.AddDestinationBufferIsSameAssertion(bytes);

            bytes.AsSpan().Clear();

            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPrivateKey(bytes.AsSpan(0, upperBound)));
            Assert.Equal(2, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(0, bytes);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPrivateKey_Array_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int upperBound = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeUpperBound(algorithm);

            dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
            {
                Assert.Equal(upperBound, destination.Length);
                destination.Fill(1);

                // Writing more than upper bound isn't allowed.
                return upperBound + 1;
            };

            Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPrivateKey());
            Assert.Equal(1, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
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
                    ecdsa => 8,
                    eddsa => 2 * eddsa.KeySizeInBits / 8);

            Assert.Throws<ArgumentException>(() => dsa.SignData(ReadOnlySpan<byte>.Empty, new byte[lowerBound - 1]));
            Assert.Throws<ArgumentException>(() => dsa.SignData(ReadOnlySpan<byte>.Empty, new byte[algorithm.MaxSignatureSizeInBytes - 1]));

            dsa.SignDataCoreHook = (data, context, destination) =>
            {
                AssertExtensions.GreaterThanOrEqualTo(destination.Length, lowerBound);
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
                    rsa => algorithm.MaxSignatureSizeInBytes,
                    ecdsa => 32 + CompositeMLDsaTestHelpers.MLDsaAlgorithms[algorithm].SignatureSizeInBytes + 8,
                    eddsa => algorithm.MaxSignatureSizeInBytes);

            AssertExtensions.FalseExpression(dsa.VerifyData(ReadOnlySpan<byte>.Empty, new byte[threshold - 1]));

            dsa.VerifyDataCoreHook = (data, signature, context) => true;

            AssertExtensions.TrueExpression(dsa.VerifyData(ReadOnlySpan<byte>.Empty, new byte[threshold]));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPublicKey_BufferSize(CompositeMLDsaAlgorithm algorithm)
        {
            int maxPublicKeySize = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeUpperBound(algorithm);
            int minPublicKeySize = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeLowerBound(algorithm);

            TestWithMockKeySize(algorithm, minPublicKeySize, minPublicKeySize);
            TestWithMockKeySize(algorithm, minPublicKeySize, (minPublicKeySize + maxPublicKeySize) / 2);
            TestWithMockKeySize(algorithm, minPublicKeySize, maxPublicKeySize);
            TestWithMockKeySize(algorithm, minPublicKeySize, maxPublicKeySize + 1);

            TestWithMockKeySize(algorithm, maxPublicKeySize, maxPublicKeySize);
            TestWithMockKeySize(algorithm, maxPublicKeySize, maxPublicKeySize + 1);

            TestWithMockKeySize(algorithm, (minPublicKeySize + maxPublicKeySize) / 2, (minPublicKeySize + maxPublicKeySize) / 2);
            TestWithMockKeySize(algorithm, (minPublicKeySize + maxPublicKeySize) / 2, maxPublicKeySize);
            TestWithMockKeySize(algorithm, (minPublicKeySize + maxPublicKeySize) / 2, maxPublicKeySize + 1);

            static void TestWithMockKeySize(CompositeMLDsaAlgorithm algorithm, int mockKeySize, int inputBufferSize)
            {
                using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
                dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
                {
                    // Buffer size must always be upper bound for the key length.
                    Assert.Equal(CompositeMLDsaTestHelpers.ExpectedPublicKeySizeUpperBound(algorithm), destination.Length);
                    return mockKeySize;
                };

                byte[] bytes = dsa.ExportCompositeMLDsaPublicKey();
                Assert.Equal(1, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
                Assert.Equal(mockKeySize, bytes.Length);

                Assert.Equal(mockKeySize, dsa.ExportCompositeMLDsaPublicKey(new byte[inputBufferSize]));
                Assert.Equal(2, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);

                AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPublicKey(new byte[inputBufferSize], out int bytesWritten));
                Assert.Equal(3, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
                Assert.Equal(mockKeySize, bytesWritten);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPrivateKey_BufferSize(CompositeMLDsaAlgorithm algorithm)
        {
            int maxPrivateKeySize = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeUpperBound(algorithm);
            int minPrivateKeySize = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeLowerBound(algorithm);

            TestWithMockKeySize(algorithm, minPrivateKeySize, minPrivateKeySize);
            TestWithMockKeySize(algorithm, minPrivateKeySize, (minPrivateKeySize + maxPrivateKeySize) / 2);
            TestWithMockKeySize(algorithm, minPrivateKeySize, maxPrivateKeySize);
            TestWithMockKeySize(algorithm, minPrivateKeySize, maxPrivateKeySize + 1);

            TestWithMockKeySize(algorithm, maxPrivateKeySize, maxPrivateKeySize);
            TestWithMockKeySize(algorithm, maxPrivateKeySize, maxPrivateKeySize + 1);

            TestWithMockKeySize(algorithm, (minPrivateKeySize + maxPrivateKeySize) / 2, (minPrivateKeySize + maxPrivateKeySize) / 2);
            TestWithMockKeySize(algorithm, (minPrivateKeySize + maxPrivateKeySize) / 2, maxPrivateKeySize);
            TestWithMockKeySize(algorithm, (minPrivateKeySize + maxPrivateKeySize) / 2, maxPrivateKeySize + 1);

            static void TestWithMockKeySize(CompositeMLDsaAlgorithm algorithm, int mockKeySize, int inputBufferSize)
            {
                using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
                dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
                {
                    // Buffer size must always be upper bound for the key length.
                    Assert.Equal(CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeUpperBound(algorithm), destination.Length);
                    return mockKeySize;
                };

                byte[] bytes = dsa.ExportCompositeMLDsaPrivateKey();
                Assert.Equal(1, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
                Assert.Equal(mockKeySize, bytes.Length);

                Assert.Equal(mockKeySize, dsa.ExportCompositeMLDsaPrivateKey(new byte[inputBufferSize]));
                Assert.Equal(2, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);

                AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPrivateKey(new byte[inputBufferSize], out int bytesWritten));
                Assert.Equal(3, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
                Assert.Equal(mockKeySize, bytesWritten);
            }
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
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPublicKey_CallsCore(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int maxPublicKeySize = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeUpperBound(algorithm);
            int minPublicKeySize = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeLowerBound(algorithm);

            int keySize = (minPublicKeySize + maxPublicKeySize) / 2;

            dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
            {
                // Filling past the expected size is allowed, but ignored.
                destination.Fill(1);
                return keySize;
            };

            byte[] exported = dsa.ExportCompositeMLDsaPublicKey();
            Assert.Equal(1, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(1, exported);
            Assert.Equal(keySize, exported.Length);

            byte[] publicKey = CreatePaddedFilledArray(keySize, 42);

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPublicKey(publicKey.AsSpan(PaddingSize, keySize), out int bytesWritten));
            Assert.Equal(2, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
            Assert.Equal(keySize, bytesWritten);

            // Padding should not be touched
            AssertExtensions.FilledWith<byte>(42, publicKey.AsSpan(0, PaddingSize));
            AssertExtensions.FilledWith<byte>(1, publicKey.AsSpan(PaddingSize, keySize));
            AssertExtensions.FilledWith<byte>(42, publicKey.AsSpan(PaddingSize + keySize));

            publicKey = CreatePaddedFilledArray(keySize, 42);

            Assert.Equal(keySize, dsa.ExportCompositeMLDsaPublicKey(publicKey.AsSpan(PaddingSize, keySize)));
            Assert.Equal(3, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);

            AssertExtensions.FilledWith<byte>(42, publicKey.AsSpan(0, PaddingSize));
            AssertExtensions.FilledWith<byte>(1, publicKey.AsSpan(PaddingSize, keySize));
            AssertExtensions.FilledWith<byte>(42, publicKey.AsSpan(PaddingSize + keySize));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void TryExportCompositeMLDsaPrivateKey_CallsCore(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int maxPrivateKeySize = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeUpperBound(algorithm);
            int minPrivateKeySize = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeLowerBound(algorithm);

            int keySize = (minPrivateKeySize + maxPrivateKeySize) / 2;

            dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
            {
                // Filling past the expected size is allowed, but ignored.
                destination.Fill(1);
                return keySize;
            };

            byte[] exported = dsa.ExportCompositeMLDsaPrivateKey();
            Assert.Equal(1, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
            AssertExtensions.FilledWith<byte>(1, exported);
            Assert.Equal(keySize, exported.Length);

            byte[] privateKey = CreatePaddedFilledArray(keySize, 42);

            AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPrivateKey(privateKey.AsSpan(PaddingSize, keySize), out int bytesWritten));
            Assert.Equal(2, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
            Assert.Equal(keySize, bytesWritten);

            // Padding should not be touched
            AssertExtensions.FilledWith<byte>(42, privateKey.AsSpan(0, PaddingSize));
            AssertExtensions.FilledWith<byte>(1, privateKey.AsSpan(PaddingSize, keySize));
            AssertExtensions.FilledWith<byte>(42, privateKey.AsSpan(PaddingSize + keySize));

            privateKey = CreatePaddedFilledArray(keySize, 42);

            Assert.Equal(keySize, dsa.ExportCompositeMLDsaPrivateKey(privateKey.AsSpan(PaddingSize, keySize)));
            Assert.Equal(3, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);

            AssertExtensions.FilledWith<byte>(42, privateKey.AsSpan(0, PaddingSize));
            AssertExtensions.FilledWith<byte>(1, privateKey.AsSpan(PaddingSize, keySize));
            AssertExtensions.FilledWith<byte>(42, privateKey.AsSpan(PaddingSize + keySize));
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
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPublicKey_MaxSizeKey_MinSizeDestination(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int maxKeyLength = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeUpperBound(algorithm);
            int minKeyLength = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeLowerBound(algorithm);
            byte[] minSizeKey = new byte[minKeyLength];

            dsa.ExportCompositeMLDsaPublicKeyCoreHook = destination =>
            {
                Assert.Equal(maxKeyLength, destination.Length);
                destination.Fill(1);
                return maxKeyLength;
            };

            if (minKeyLength != maxKeyLength)
            {
                Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPublicKey(minSizeKey));
                Assert.Equal(1, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
                AssertExtensions.FilledWith<byte>(0, minSizeKey);

                AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPublicKey(minSizeKey, out int bytesWritten));
                Assert.Equal(2, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
                Assert.Equal(0, bytesWritten);
                AssertExtensions.FilledWith<byte>(0, minSizeKey);
            }
            else
            {
                dsa.AddDestinationBufferIsSameAssertion(minSizeKey);

                int bytesWritten = dsa.ExportCompositeMLDsaPublicKey(minSizeKey);
                Assert.Equal(1, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
                Assert.Equal(minKeyLength, bytesWritten);
                AssertExtensions.FilledWith<byte>(1, minSizeKey);

                minSizeKey.AsSpan().Clear();

                AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPublicKey(minSizeKey, out bytesWritten));
                Assert.Equal(2, dsa.ExportCompositeMLDsaPublicKeyCoreCallCount);
                Assert.Equal(minKeyLength, bytesWritten);
                AssertExtensions.FilledWith<byte>(1, minSizeKey);
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ExportCompositeMLDsaPrivateKey_MaxSizeKey_MinSizeDestination(CompositeMLDsaAlgorithm algorithm)
        {
            using CompositeMLDsaMockImplementation dsa = CompositeMLDsaMockImplementation.Create(algorithm);
            int maxKeyLength = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeUpperBound(algorithm);
            int minKeyLength = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeLowerBound(algorithm);
            byte[] minSizeKey = new byte[minKeyLength];

            dsa.ExportCompositeMLDsaPrivateKeyCoreHook = destination =>
            {
                Assert.Equal(maxKeyLength, destination.Length);
                destination.Fill(1);
                return maxKeyLength;
            };

            if (minKeyLength != maxKeyLength)
            {
                Assert.Throws<CryptographicException>(() => dsa.ExportCompositeMLDsaPrivateKey(minSizeKey));
                Assert.Equal(1, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
                AssertExtensions.FilledWith<byte>(0, minSizeKey);

                AssertExtensions.FalseExpression(dsa.TryExportCompositeMLDsaPrivateKey(minSizeKey, out int bytesWritten));
                Assert.Equal(2, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
                Assert.Equal(0, bytesWritten);
                AssertExtensions.FilledWith<byte>(0, minSizeKey);
            }
            else
            {
                dsa.AddDestinationBufferIsSameAssertion(minSizeKey);

                int bytesWritten = dsa.ExportCompositeMLDsaPrivateKey(minSizeKey);
                Assert.Equal(1, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
                Assert.Equal(minKeyLength, bytesWritten);
                AssertExtensions.FilledWith<byte>(1, minSizeKey);

                minSizeKey.AsSpan().Clear();

                AssertExtensions.TrueExpression(dsa.TryExportCompositeMLDsaPrivateKey(minSizeKey, out bytesWritten));
                Assert.Equal(2, dsa.ExportCompositeMLDsaPrivateKeyCoreCallCount);
                Assert.Equal(minKeyLength, bytesWritten);
                AssertExtensions.FilledWith<byte>(1, minSizeKey);
            }
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
