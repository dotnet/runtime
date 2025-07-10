// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    public abstract class SlhDsaTests
    {
        protected abstract SlhDsa GenerateKey(SlhDsaAlgorithm algorithm);
        protected abstract SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        protected abstract SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);

        public static IEnumerable<object[]> NistPureSigVerTestVectorsData =>
            from vector in SlhDsaTestData.NistSigVerTestVectors
            where vector.HashAlgorithm is null
            select new object[] { vector };

        [Theory]
        [MemberData(nameof(NistPureSigVerTestVectorsData))]
        public void NistPureSignatureVerificationTest(SlhDsaTestData.SlhDsaSigVerTestVector vector)
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

        public static IEnumerable<object[]> NistPreHashSigVerTestVectorsData =>
            from vector in SlhDsaTestData.NistSigVerTestVectors
            where vector.HashAlgorithm is not null
            select new object[] { vector };

        [Theory]
        [MemberData(nameof(NistPreHashSigVerTestVectorsData))]
        public void NistPreHashSignatureVerificationTest(SlhDsaTestData.SlhDsaSigVerTestVector vector)
        {
            byte[] msg = vector.Message;
            byte[] ctx = vector.Context;
            byte[] sig = vector.Signature;
            byte[] hash;

#if NET
            if (vector.HashAlgorithm == SlhDsaTestHelpers.Shake128Oid)
            {
                using (Shake128 hasher = new Shake128())
                {
                    hasher.AppendData(msg);
                    hash = hasher.GetHashAndReset(256 / 8);
                }
            }
            else if (vector.HashAlgorithm == SlhDsaTestHelpers.Shake256Oid)
            {
                using (Shake256 hasher = new Shake256())
                {
                    hasher.AppendData(msg);
                    hash = hasher.GetHashAndReset(512 / 8);
                }
            }
            else
#endif
            {
                HashAlgorithmName hashAlgorithmName =
                    vector.HashAlgorithm switch
                    {
                        SlhDsaTestHelpers.Md5Oid => HashAlgorithmName.MD5,
                        SlhDsaTestHelpers.Sha1Oid => HashAlgorithmName.SHA1,
                        SlhDsaTestHelpers.Sha256Oid => HashAlgorithmName.SHA256,
                        SlhDsaTestHelpers.Sha384Oid => HashAlgorithmName.SHA384,
                        SlhDsaTestHelpers.Sha512Oid => HashAlgorithmName.SHA512,
#if NET
                        SlhDsaTestHelpers.Sha3_256Oid => HashAlgorithmName.SHA3_256,
                        SlhDsaTestHelpers.Sha3_384Oid => HashAlgorithmName.SHA3_384,
                        SlhDsaTestHelpers.Sha3_512Oid => HashAlgorithmName.SHA3_512,
#endif
                        _ => throw new XunitException($"Unknown hash algorithm OID: {vector.HashAlgorithm}"),
                    };

                using (IncrementalHash hasher = IncrementalHash.CreateHash(hashAlgorithmName))
                {
                    hasher.AppendData(msg);
                    hash = hasher.GetHashAndReset();
                }
            }

            // Test signature verification with public key
            using SlhDsa publicSlhDsa = ImportSlhDsaPublicKey(vector.Algorithm, vector.PublicKey);
            Assert.Equal(vector.TestPassed, publicSlhDsa.VerifyPreHash(hash, sig, vector.HashAlgorithm, ctx));

            // Test signature verification with secret key
            using SlhDsa secretSlhDsa = ImportSlhDsaSecretKey(vector.Algorithm, vector.SecretKey);
            Assert.Equal(vector.TestPassed, secretSlhDsa.VerifyPreHash(hash, sig, vector.HashAlgorithm, ctx));
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
        ];

        [Theory]
        [MemberData(nameof(AlgorithmsData_Small))]
        public void GenerateSignVerifyNoContext(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature = slhDsa.SignData(data);
            ExerciseSuccessfulVerify(slhDsa, data, signature, []);

            signature.AsSpan().Clear();
            slhDsa.SignData(data, signature, Array.Empty<byte>());
            ExerciseSuccessfulVerify(slhDsa, data, signature, Array.Empty<byte>());
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData_Small))]
        public void GenerateSignVerifyWithContext(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            byte[] data = [1, 2, 3, 4, 5];

            byte[] signature = slhDsa.SignData(data, context);
            ExerciseSuccessfulVerify(slhDsa, data, signature, context);
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData_Small))]
        public void GenerateSignVerifyEmptyMessageNoContext(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] signature = slhDsa.SignData([]);
            ExerciseSuccessfulVerify(slhDsa, [], signature, []);

            signature.AsSpan().Clear();
            slhDsa.SignData(Array.Empty<byte>(), signature, Array.Empty<byte>());
            ExerciseSuccessfulVerify(slhDsa, [], signature, []);
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData_Small))]
        public void GenerateSignVerifyEmptyMessageWithContext(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            byte[] signature = slhDsa.SignData([], context);
            ExerciseSuccessfulVerify(slhDsa, [], signature, context);

            signature.AsSpan().Clear();
            slhDsa.SignData(Array.Empty<byte>(), signature, context);
            ExerciseSuccessfulVerify(slhDsa, [], signature, context);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void GenerateSignExportPublicVerifyWithPublicOnly(SlhDsaAlgorithm algorithm)
        {
            byte[] publicKey;
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature;

            using (SlhDsa slhDsa = GenerateKey(algorithm))
            {
                signature = slhDsa.SignData(data);
                AssertExtensions.TrueExpression(slhDsa.VerifyData(data, signature));

                publicKey = slhDsa.ExportSlhDsaPublicKey();
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
                signature = slhDsa.SignData(data);
                secretKey = slhDsa.ExportSlhDsaSecretKey();
            }

            using (SlhDsa slhDsa = ImportSlhDsaSecretKey(algorithm, secretKey))
            {
                ExerciseSuccessfulVerify(slhDsa, data, signature, []);

                signature.AsSpan().Clear();
                slhDsa.SignData(data, signature, []);

                ExerciseSuccessfulVerify(slhDsa, data, signature, []);
            }
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void GenerateSignPreHashExportPublicVerifyWithPublicOnly(SlhDsaAlgorithm algorithm)
        {
            byte[] publicKey;
            string shake256Oid = SlhDsaTestHelpers.Shake256Oid;
            byte[] data = new byte[512 / 8];
            byte[] signature;

            using (SlhDsa slhDsa = GenerateKey(algorithm))
            {
                signature = slhDsa.SignPreHash(data, shake256Oid);
                AssertExtensions.TrueExpression(slhDsa.VerifyPreHash(data, signature, shake256Oid));

                publicKey = slhDsa.ExportSlhDsaPublicKey();
            }

            using (SlhDsa publicSlhDsa = ImportSlhDsaPublicKey(algorithm, publicKey))
            {
                ExerciseSuccessfulVerifyPreHash(publicSlhDsa, data, signature, shake256Oid, []);
            }
        }

        [Theory]
        [MemberData(nameof(AlgorithmsData_Small))]
        public void GenerateExportSecretKeySignPreHashAndVerify(SlhDsaAlgorithm algorithm)
        {
            byte[] secretKey;
            string shake256Oid = SlhDsaTestHelpers.Shake256Oid;
            byte[] data = new byte[512 / 8];
            byte[] signature;

            using (SlhDsa slhDsa = GenerateKey(algorithm))
            {
                signature = slhDsa.SignPreHash(data, shake256Oid);
                secretKey = slhDsa.ExportSlhDsaSecretKey();
            }

            using (SlhDsa slhDsa = ImportSlhDsaSecretKey(algorithm, secretKey))
            {
                ExerciseSuccessfulVerifyPreHash(slhDsa, data, signature, shake256Oid, []);

                signature.AsSpan().Clear();
                slhDsa.SignPreHash(data, signature, shake256Oid, []);

                ExerciseSuccessfulVerifyPreHash(slhDsa, data, signature, shake256Oid, []);
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
            ReadOnlySpan<byte> buffer = [0, 1, 2, 3];
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

                AssertExtensions.FalseExpression(slhDsa.VerifyData(buffer.Slice(0, 1), signature, context));
                AssertExtensions.FalseExpression(slhDsa.VerifyData(buffer.Slice(1), signature, context));
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

                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, buffer.Slice(0, 1)));
                AssertExtensions.FalseExpression(slhDsa.VerifyData(data, signature, buffer.Slice(1)));
            }

            AssertExtensions.TrueExpression(slhDsa.VerifyData(data, signature, context));
        }

        protected static void ExerciseSuccessfulVerifyPreHash(SlhDsa slhDsa, byte[] data, byte[] signature, string hashAlgorithmOid, byte[] context)
        {
            ReadOnlySpan<byte> buffer = [0, 1, 2, 3];
            AssertExtensions.TrueExpression(slhDsa.VerifyPreHash(data, signature, hashAlgorithmOid, context));

            if (data.Length > 0)
            {
                data[0] ^= 1;
                AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(data, signature, hashAlgorithmOid, context));
                data[0] ^= 1;
            }
            else
            {
                Assert.Fail("Empty hash is not supported.");
            }

            signature[0] ^= 1;
            AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(data, signature, hashAlgorithmOid, context));
            signature[0] ^= 1;

            if (context.Length > 0)
            {
                AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(data, signature, hashAlgorithmOid, Array.Empty<byte>()));
                AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(data, signature, hashAlgorithmOid, ReadOnlySpan<byte>.Empty));

                context[0] ^= 1;
                AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(data, signature, hashAlgorithmOid, context));
                context[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(slhDsa.VerifyPreHash(data, signature, hashAlgorithmOid, Array.Empty<byte>()));
                AssertExtensions.TrueExpression(slhDsa.VerifyPreHash(data, signature, hashAlgorithmOid, ReadOnlySpan<byte>.Empty));

                AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(data, signature, hashAlgorithmOid, buffer.Slice(0, 1)));
                AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(data, signature, hashAlgorithmOid, buffer.Slice(1)));
            }

            string hashAlgorithmOid2 = "1." + hashAlgorithmOid;
            AssertExtensions.FalseExpression(slhDsa.VerifyPreHash(data, signature, hashAlgorithmOid2, context));

            AssertExtensions.TrueExpression(slhDsa.VerifyPreHash(data, signature, hashAlgorithmOid, context));
        }
    }
}
