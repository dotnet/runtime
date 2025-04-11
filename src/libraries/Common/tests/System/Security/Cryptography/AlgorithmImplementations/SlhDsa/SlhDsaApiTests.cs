// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public sealed class SlhDsaApiTests : SlhDsaInstanceTestsBase
    {
        public static IEnumerable<object[]> ApiWithDestinationSpanTestData =>
            from algorithm in SlhDsaTestData.AlgorithmsRaw
            from destinationLargerThanRequired in new[] { true, false }
            select new object[] { algorithm, destinationLargerThanRequired };

        private const int PaddingSize = 10;

        [Theory]
        [MemberData(nameof(ApiWithDestinationSpanTestData))]
        public static void CallsExportSlhDsaPublicKeyCore(SlhDsaAlgorithm algorithm, bool destinationLargerThanRequired)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int publicKeySize = algorithm.PublicKeySizeInBytes;
            byte[] publicKey = new byte[publicKeySize + 2 * PaddingSize];
            publicKey.AsSpan().Fill(42);

            slhDsa.ExportSlhDsaPublicKeyCoreHook = (Span<byte> destination) =>
            {
                Assert.Equal(publicKeySize, destination.Length);
                destination.Fill(1);
            };

            // Extra bytes in destination buffer should not be touched
            int extraBytes = destinationLargerThanRequired ? PaddingSize / 2 : 0;
            slhDsa.ExportSlhDsaPublicKey(publicKey.AsSpan(PaddingSize, publicKeySize + extraBytes));
            AssertExpectedFill(publicKey, fillElement: 1, paddingElement: 42, PaddingSize, publicKeySize);
        }

        [Theory]
        [MemberData(nameof(ApiWithDestinationSpanTestData))]
        public static void CallsExportSlhDsaSecretKeyCore(SlhDsaAlgorithm algorithm, bool destinationLargerThanRequired)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int secretKeySize = algorithm.SecretKeySizeInBytes;
            byte[] secretKey = new byte[secretKeySize + 2 * PaddingSize];
            secretKey.AsSpan().Fill(42);

            slhDsa.ExportSlhDsaSecretKeyCoreHook = (Span<byte> destination) =>
            {
                Assert.Equal(secretKeySize, destination.Length);
                destination.Fill(1);
            };

            // Extra bytes in destination buffer should not be touched
            int extraBytes = destinationLargerThanRequired ? PaddingSize / 2 : 0;
            slhDsa.ExportSlhDsaSecretKey(secretKey.AsSpan(PaddingSize, secretKeySize + extraBytes));
            AssertExpectedFill(secretKey, fillElement: 1, paddingElement: 42, PaddingSize, secretKeySize);
        }

        [Theory]
        [MemberData(nameof(ApiWithDestinationSpanTestData))]
        public static void CallsSignDataCore(SlhDsaAlgorithm algorithm, bool destinationLargerThanRequired)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] signature = new byte[signatureSize + 2 * PaddingSize];
            signature.AsSpan().Fill(42);
            byte[] testData = [2];
            byte[] testContext = [3];

            slhDsa.SignDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            {
                AssertExtensions.SequenceEqual(testData, data);
                AssertExtensions.SequenceEqual(testContext, context);

                Assert.Equal(destination.Length, signatureSize);
                destination.Fill(1);
            };

            // Extra bytes in destination buffer should not be touched
            int extraBytes = destinationLargerThanRequired ? PaddingSize / 2 : 0;
            slhDsa.SignData(testData, signature.AsSpan(PaddingSize, signatureSize + extraBytes), testContext);
            AssertExpectedFill(signature, fillElement: 1, paddingElement: 42, PaddingSize, signatureSize);
        }

        private static void AssertExpectedFill(ReadOnlySpan<byte> source, byte fillElement, byte paddingElement, int startIndex, int length)
        {
            // Ensure that the data was filled correctly
            AssertExtensions.FilledWith(fillElement, source.Slice(startIndex, length));

            // And that the padding was not touched
            AssertExtensions.FilledWith(paddingElement, source.Slice(0, startIndex));
            AssertExtensions.FilledWith(paddingElement, source.Slice(startIndex + length));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void CallsVerifyDataCore(SlhDsaAlgorithm algorithm)
        {
            using SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.CreateOverriddenCoreMethodsFail(algorithm);

            int signatureSize = algorithm.SignatureSizeInBytes;
            byte[] testSignature = new byte[signatureSize + 1];
            testSignature.AsSpan().Fill(42);
            byte[] testData = [2];
            byte[] testContext = [3];
            bool returnValue = false;

            slhDsa.VerifyDataCoreHook = (ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            {
                AssertExtensions.SequenceEqual(testData, data);
                AssertExtensions.SequenceEqual(testContext, context);
                AssertExtensions.SequenceEqual(testSignature.AsSpan(0, signatureSize), signature);

                return returnValue;
            };

            // Since `returnValue` is true, this shows the Core method doesn't get called for the wrong sized signature.
            returnValue = true;
            AssertExtensions.FalseExpression(slhDsa.VerifyData(testData, testSignature.AsSpan(0, signatureSize - 1), testContext));
            AssertExtensions.FalseExpression(slhDsa.VerifyData(testData, testSignature.AsSpan(0, signatureSize + 1), testContext));

            // But does for the right one.
            AssertExtensions.TrueExpression(slhDsa.VerifyData(testData, testSignature.AsSpan(0, signatureSize), testContext));

            // And just to prove that the Core method controls the answer...
            returnValue = false;
            AssertExtensions.FalseExpression(slhDsa.VerifyData(testData, testSignature.AsSpan(0, signatureSize), testContext));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public static void CallsVirtualDispose(SlhDsaAlgorithm algorithm)
        {
            SlhDsaMockImplementation slhDsa = SlhDsaMockImplementation.CreateOverriddenCoreMethodsFail(algorithm);
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
            slhDsa.Dispose();
        }

        protected override SlhDsa GenerateKey(SlhDsaAlgorithm algorithm) =>
            SlhDsaMockImplementation.CreateOverriddenCoreMethodsFail(algorithm);

        protected override SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            SlhDsaMockImplementation.CreateOverriddenCoreMethodsFail(algorithm);

        protected override SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            SlhDsaMockImplementation.CreateOverriddenCoreMethodsFail(algorithm);
    }
}
