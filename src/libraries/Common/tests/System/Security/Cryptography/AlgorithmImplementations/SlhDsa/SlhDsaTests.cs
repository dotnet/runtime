// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public abstract class SlhDsaTests
    {
        protected abstract SlhDsa GenerateKey(SlhDsaAlgorithm algorithm);
        protected abstract SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        protected abstract SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);

        public static IEnumerable<object[]> NistSigVerTestVectorsData =>
            from vector in SlhDsaTestData.NistSigVerTestVectors
            select new object[] { vector };

        [Theory]
        [MemberData(nameof(NistSigVerTestVectorsData))]
        public void NistSignatureVerificationTest(SlhDsaTestData.SlhDsaSigVerTestVector vector)
        {
            byte[] msg = vector.Message;
            byte[] ctx = vector.Context;
            byte[] sig = vector.Signature;

            // Test signature verification with public key
            using SlhDsa publicSlhDsa = ImportSlhDsaPublicKey(vector.Algorithm, vector.PublicKey);
            Assert.Equal(vector.TestPassed, publicSlhDsa.VerifyData(msg, sig, ctx));

            // Test signature verification with secret key
            using SlhDsa secretSlhDsa = ImportSlhDsaSecretKey(vector.Algorithm, vector.SecretKey);
            Assert.Equal(vector.TestPassed, secretSlhDsa.VerifyData(msg, sig, ctx));
        }

        // Signing takes a relatively long time so we'll just run it on a representative sample of algorithms.
        public static IEnumerable<object[]> AlgorithmsData_Small => AlgorithmsRaw_Small.Select(a => new[] { a });

        public static SlhDsaAlgorithm[] AlgorithmsRaw_Small =
        [
            // Fast algorithms
            SlhDsaAlgorithm.SlhDsaSha2_128f,
            SlhDsaAlgorithm.SlhDsaShake128f,
            SlhDsaAlgorithm.SlhDsaSha2_192f,
            SlhDsaAlgorithm.SlhDsaShake192f,
            SlhDsaAlgorithm.SlhDsaSha2_256f,
            SlhDsaAlgorithm.SlhDsaShake256f,

            // Slow algorithms
            // These tend to be over 10x slower than the fast counterparts. For perf numbers, see
            // section 10 in https://sphincs.org/data/sphincs+-r3.1-specification.pdf (from June 2022).
            SlhDsaAlgorithm.SlhDsaSha2_128s,
            SlhDsaAlgorithm.SlhDsaShake192s,
            SlhDsaAlgorithm.SlhDsaSha2_256s,
        ];

        [Theory]
        [MemberData(nameof(AlgorithmsData_Small))]
        public void GenerateSignVerifyNoContext(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature = new byte[slhDsa.Algorithm.SignatureSizeInBytes];

            Assert.Equal(signature.Length, slhDsa.SignData(data, signature));
            ExerciseSuccessfulVerify(slhDsa, data, signature, []);

            signature.AsSpan().Clear();
            Assert.Equal(signature.Length, slhDsa.SignData(data, signature, Array.Empty<byte>()));
            ExerciseSuccessfulVerify(slhDsa, data, signature, Array.Empty<byte>());
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData_Small))]
        public void GenerateSignVerifyWithContext(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature = new byte[slhDsa.Algorithm.SignatureSizeInBytes];

            Assert.Equal(signature.Length, slhDsa.SignData(data, signature, context));
            ExerciseSuccessfulVerify(slhDsa, data, signature, context);
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData_Small))]
        public void GenerateSignVerifyEmptyMessageNoContext(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] signature = new byte[slhDsa.Algorithm.SignatureSizeInBytes];

            Assert.Equal(signature.Length, slhDsa.SignData([], signature));
            ExerciseSuccessfulVerify(slhDsa, [], signature, []);

            signature.AsSpan().Clear();
            Assert.Equal(signature.Length, slhDsa.SignData(Array.Empty<byte>(), signature, Array.Empty<byte>()));
            ExerciseSuccessfulVerify(slhDsa, [], signature, []);
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData_Small))]
        public void GenerateSignVerifyEmptyMessageWithContext(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            byte[] signature = new byte[slhDsa.Algorithm.SignatureSizeInBytes];

            Assert.Equal(signature.Length, slhDsa.SignData([], signature, context));
            ExerciseSuccessfulVerify(slhDsa, [], signature, context);

            signature.AsSpan().Clear();
            Assert.Equal(signature.Length, slhDsa.SignData(Array.Empty<byte>(), signature, context));
            ExerciseSuccessfulVerify(slhDsa, [], signature, context);
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData_Small))]
        public void GenerateSignExportPublicVerifyWithPublicOnly(SlhDsaAlgorithm algorithm)
        {
            byte[] publicKey;
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature;

            using (SlhDsa slhDsa = GenerateKey(algorithm))
            {
                signature = new byte[algorithm.SignatureSizeInBytes];
                Assert.Equal(signature.Length, slhDsa.SignData(data, signature));
                AssertExtensions.TrueExpression(slhDsa.VerifyData(data, signature));

                publicKey = new byte[algorithm.PublicKeySizeInBytes];
                Assert.Equal(publicKey.Length, slhDsa.ExportSlhDsaPublicKey(publicKey));
            }

            using (SlhDsa publicSlhDsa = ImportSlhDsaPublicKey(algorithm, publicKey))
            {
                ExerciseSuccessfulVerify(publicSlhDsa, data, signature, []);
            }
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData_Small))]
        public void GenerateExportSecretKeySignAndVerify(SlhDsaAlgorithm algorithm)
        {
            byte[] secretKey;
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature;

            using (SlhDsa slhDsa = GenerateKey(algorithm))
            {
                signature = new byte[algorithm.SignatureSizeInBytes];
                Assert.Equal(signature.Length, slhDsa.SignData(data, signature));

                secretKey = new byte[algorithm.SecretKeySizeInBytes];
                Assert.Equal(secretKey.Length, slhDsa.ExportSlhDsaSecretKey(secretKey));
            }

            using (SlhDsa slhDsa = ImportSlhDsaSecretKey(algorithm, secretKey))
            {
                ExerciseSuccessfulVerify(slhDsa, data, signature, []);

                signature.AsSpan().Clear();
                Assert.Equal(signature.Length, slhDsa.SignData(data, signature));

                ExerciseSuccessfulVerify(slhDsa, data, signature, []);
            }
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void UseAfterDispose(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);

            slhDsa.Dispose();
            slhDsa.Dispose(); // no throw

            SlhDsaTestHelpers.VerifyDisposed(slhDsa);
        }

        protected static void ExerciseSuccessfulVerify(SlhDsa slhDsa, byte[] data, byte[] signature, byte[] context)
        {
            AssertExtensions.TrueExpression(slhDsa.VerifyData(data, signature, context));

            if (data.Length > 0)
            {
                AssertExtensions.FalseExpression(slhDsa.VerifyData(Array.Empty<byte>(), signature, context));
                AssertExtensions.FalseExpression(slhDsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, context));

                data[0] ^= 1;
                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, context));
                data[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(slhDsa.VerifyData(Array.Empty<byte>(), signature, context));
                AssertExtensions.TrueExpression(slhDsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, context));

                AssertExtensions.FalseExpression(slhDsa.VerifyData([0], signature, context));
                AssertExtensions.FalseExpression(slhDsa.VerifyData([1, 2, 3], signature, context));
            }

            signature[0] ^= 1;
            AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, context));
            signature[0] ^= 1;

            if (context.Length > 0)
            {
                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, Array.Empty<byte>()));
                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, ReadOnlySpan<byte>.Empty));

                context[0] ^= 1;
                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, context));
                context[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(slhDsa.VerifyData(data, signature, Array.Empty<byte>()));
                AssertExtensions.TrueExpression(slhDsa.VerifyData(data, signature, ReadOnlySpan<byte>.Empty));

                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, [0]));
                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, [1, 2, 3]));
            }

            AssertExtensions.TrueExpression(slhDsa.VerifyData(data, signature, context));
        }
    }
}
