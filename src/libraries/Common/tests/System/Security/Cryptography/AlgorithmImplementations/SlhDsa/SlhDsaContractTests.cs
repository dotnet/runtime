// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public static class SlhDsaContractTests
    {
        public static IEnumerable<object[]> ArgumentValidationData =>
            from algorithm in SlhDsaTestData.AlgorithmsRaw
            from shouldDispose in new[] { true, false }
            select new object[] { algorithm, shouldDispose };

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public static void NullArgumentValidation(SlhDsaAlgorithm algorithm, bool shouldDispose)
        {
            using SlhDsa slhDsa = SlhDsaMockImplementation.Create(algorithm);

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                slhDsa.Dispose();
            }

            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char>.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte>.Empty, null, Span<byte>.Empty, out _));
            AssertExtensions.Throws<ArgumentNullException>("pbeParameters", () => slhDsa.TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char>.Empty, null, Span<byte>.Empty, out _));
        }

        [Theory]
        [MemberData(nameof(ArgumentValidationData))]
        public static void ArgumentValidation(SlhDsaAlgorithm algorithm, bool shouldDispose)
        {
            using SlhDsa slhDsa = SlhDsaMockImplementation.Create(algorithm);

            int publicKeySize = algorithm.PublicKeySizeInBytes;
            int secretKeySize = algorithm.SecretKeySizeInBytes;
            int signatureSize = algorithm.SignatureSizeInBytes;

            if (shouldDispose)
            {
                // Test that argument validation exceptions take precedence over ObjectDisposedException
                slhDsa.Dispose();
            }

            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.ExportSlhDsaPublicKey(new byte[publicKeySize - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.ExportSlhDsaSecretKey(new byte[secretKeySize - 1]));
            AssertExtensions.Throws<ArgumentException>("destination", () => slhDsa.SignData(ReadOnlySpan<byte>.Empty, new byte[signatureSize - 1], ReadOnlySpan<byte>.Empty));

            // Context length must be less than 256
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => slhDsa.SignData(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, new byte[256]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("context", () => slhDsa.VerifyData(ReadOnlySpan<byte>.Empty, Span<byte>.Empty, new byte[256]));
        }

        public static IEnumerable<object[]> ApiWithDestinationSpanTestData =>
            from algorithm in SlhDsaTestData.AlgorithmsRaw
            from destinationLargerThanRequired in new[] { true, false }
            select new object[] { algorithm, destinationLargerThanRequired };

        private const int PaddingSize = 10;

        [Theory]
        [MemberData(nameof(ApiWithDestinationSpanTestData))]
        public static void ExportSlhDsaPublicKey_CallsCore(SlhDsaAlgorithm algorithm, bool destinationLargerThanRequired)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

            int publicKeySize = algorithm.PublicKeySizeInBytes;
            byte[] publicKey = CreatePaddedFilledArray(publicKeySize, 42);

            // Extra bytes in destination buffer should not be touched
            int extraBytes = destinationLargerThanRequired ? PaddingSize / 2 : 0;
            Memory<byte> destination = publicKey.AsMemory(PaddingSize, publicKeySize + extraBytes);

            slhDsa.ExportSlhDsaPublicKeyCoreHook = _ => { };
            slhDsa.AddDestinationBufferIsSameAssertion(destination[..publicKeySize]);
            slhDsa.AddFillDestination(1);

            slhDsa.ExportSlhDsaPublicKey(destination.Span);
            Assert.Equal(1, slhDsa.ExportSlhDsaPublicKeyCoreCallCount);
            AssertExpectedFill(publicKey, fillElement: 1, paddingElement: 42, PaddingSize, publicKeySize);
        }

        [Theory]
        [MemberData(nameof(ApiWithDestinationSpanTestData))]
        public static void ExportSlhDsaSecretKey_CallsCore(SlhDsaAlgorithm algorithm, bool destinationLargerThanRequired)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

            int secretKeySize = algorithm.SecretKeySizeInBytes;
            byte[] secretKey = CreatePaddedFilledArray(secretKeySize, 42);

            // Extra bytes in destination buffer should not be touched
            int extraBytes = destinationLargerThanRequired ? PaddingSize / 2 : 0;
            Memory<byte> destination = secretKey.AsMemory(PaddingSize, secretKeySize + extraBytes);

            slhDsa.ExportSlhDsaSecretKeyCoreHook = _ => { };
            slhDsa.AddDestinationBufferIsSameAssertion(destination[..secretKeySize]);
            slhDsa.AddFillDestination(1);

            slhDsa.ExportSlhDsaSecretKey(destination.Span);
            Assert.Equal(1, slhDsa.ExportSlhDsaSecretKeyCoreCallCount);
            AssertExpectedFill(secretKey, fillElement: 1, paddingElement: 42, PaddingSize, secretKeySize);
        }

        [Theory]
        [MemberData(nameof(ApiWithDestinationSpanTestData))]
        public static void SignData_CallsCore(SlhDsaAlgorithm algorithm, bool destinationLargerThanRequired)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] signature = CreatePaddedFilledArray(signatureSize, 42);

            // Extra bytes in destination buffer should not be touched
            int extraBytes = destinationLargerThanRequired ? PaddingSize / 2 : 0;
            Memory<byte> destination = signature.AsMemory(PaddingSize, signatureSize + extraBytes);
            byte[] testData = [2];
            byte[] testContext = [3];

            slhDsa.SignDataCoreHook = (_, _, _) => { };
            slhDsa.AddDataBufferIsSameAssertion(testData);
            slhDsa.AddContextBufferIsSameAssertion(testContext);
            slhDsa.AddDestinationBufferIsSameAssertion(destination[..signatureSize]);
            slhDsa.AddFillDestination(1);

            slhDsa.SignData(testData, signature.AsSpan(PaddingSize, signatureSize + extraBytes), testContext);
            Assert.Equal(1, slhDsa.SignDataCoreCallCount);
            AssertExpectedFill(signature, fillElement: 1, paddingElement: 42, PaddingSize, signatureSize);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void VerifyData_CallsCore(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);

            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] testSignature = CreatePaddedFilledArray(signatureSize, 42);
            byte[] testData = [2];
            byte[] testContext = [3];
            bool returnValue = false;

            slhDsa.VerifyDataCoreHook = (_, _, _) => returnValue;
            slhDsa.AddDataBufferIsSameAssertion(testData);
            slhDsa.AddContextBufferIsSameAssertion(testContext);
            slhDsa.AddSignatureBufferIsSameAssertion(testSignature.AsMemory(PaddingSize, signatureSize));

            // Since `returnValue` is true, this shows the Core method doesn't get called for the wrong sized signature.
            returnValue = true;
            AssertExtensions.FalseExpression(slhDsa.VerifyData(testData, testSignature.AsSpan(PaddingSize, signatureSize - 1), testContext));
            Assert.Equal(0, slhDsa.VerifyDataCoreCallCount);

            AssertExtensions.FalseExpression(slhDsa.VerifyData(testData, testSignature.AsSpan(PaddingSize, signatureSize + 1), testContext));
            Assert.Equal(0, slhDsa.VerifyDataCoreCallCount);

            // But does for the right one.
            AssertExtensions.TrueExpression(slhDsa.VerifyData(testData, testSignature.AsSpan(PaddingSize, signatureSize), testContext));
            Assert.Equal(1, slhDsa.VerifyDataCoreCallCount);

            // And just to prove that the Core method controls the answer...
            returnValue = false;
            AssertExtensions.FalseExpression(slhDsa.VerifyData(testData, testSignature.AsSpan(PaddingSize, signatureSize), testContext));
            Assert.Equal(2, slhDsa.VerifyDataCoreCallCount);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void Dispose_CallsVirtual(SlhDsaAlgorithm algorithm)
        {
            SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.Create(algorithm);
            bool disposeCalled = false;

            // First Dispose call should invoke overridden Dispose should be called
            slhDsa.DisposeHook = (bool disposing) =>
            {
                AssertExtensions.TrueExpression(disposing);
                disposeCalled = true;
            };

            slhDsa.Dispose();
            AssertExtensions.TrueExpression(disposeCalled);

            // Subsequent Dispose calls should be a no-op
            slhDsa.DisposeHook = _ => Assert.Fail();

            slhDsa.Dispose();
            slhDsa.Dispose(); // no throw

            SlhDsaTestHelpers.VerifyDisposed(slhDsa);
        }

        private static void AssertExpectedFill(ReadOnlySpan<byte> source, byte fillElement, byte paddingElement, int startIndex, int length)
        {
            // Ensure that the data was filled correctly
            AssertExtensions.FilledWith(fillElement, source.Slice(startIndex, length));

            // And that the padding was not touched
            AssertExtensions.FilledWith(paddingElement, source.Slice(0, startIndex));
            AssertExtensions.FilledWith(paddingElement, source.Slice(startIndex + length));
        }

        private static byte[] CreatePaddedFilledArray(int size, byte filling)
        {
            byte[] publicKey = new byte[size + 2 * PaddingSize];
            publicKey.AsSpan().Fill(filling);
            return publicKey;
        }
    }
}
