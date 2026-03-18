// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.XUnitExtensions;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLDsa), nameof(MLDsa.IsSupported))]
    public abstract class MLDsaTestsBase
    {
        protected abstract MLDsa GenerateKey(MLDsaAlgorithm algorithm);
        protected abstract MLDsa ImportPrivateSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> seed);
        protected abstract MLDsa ImportPrivateKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        protected abstract MLDsa ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void AlgorithmIsAssigned(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            Assert.Same(algorithm, mldsa.Algorithm);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyNoContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature = mldsa.SignData(data);
            ExerciseSuccessfulVerify(mldsa, data, signature, []);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyPreHashNoContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] data = [1, 2, 3, 4, 5];
            byte[] hash = HashInfo.Sha512.GetHash(data);
            byte[] signature = mldsa.SignPreHash(hash, HashInfo.Sha512.Oid, []);
            ExerciseSuccessfulVerifyPreHash(mldsa, HashInfo.Sha512.Oid, hash, signature, []);
        }

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.ExternalMuIsSupported))]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyExternalMuNoContext(MLDsaAlgorithm algorithm)
        {
            byte[] data = [1, 2, 3, 4, 5];
            using MLDsa mldsa = GenerateKey(algorithm);
            SignAndVerifyExternalMu(mldsa, data, []);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyWithContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature = mldsa.SignData(data, context);

            ExerciseSuccessfulVerify(mldsa, data, signature, context);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyPreHashWithContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            byte[] data = [1, 2, 3, 4, 5];
            byte[] hash = HashInfo.Sha512.GetHash(data);
            byte[] signature = mldsa.SignPreHash(hash, HashInfo.Sha512.Oid, context);

            ExerciseSuccessfulVerifyPreHash(mldsa, HashInfo.Sha512.Oid, hash, signature, context);
        }

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.ExternalMuIsSupported))]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyExternalMuWithContext(MLDsaAlgorithm algorithm)
        {
            byte[] data = [1, 2, 3, 4, 5];
            byte[] context = [1, 1, 3, 5, 6];
            using MLDsa mldsa = GenerateKey(algorithm);
            SignAndVerifyExternalMu(mldsa, data, context);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyEmptyMessageNoContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] signature = mldsa.SignData([]);
            ExerciseSuccessfulVerify(mldsa, [], signature, []);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyEmptyMessageWithContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            byte[] signature = mldsa.SignData([], context);
            ExerciseSuccessfulVerify(mldsa, [], signature, context);
        }

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.ExternalMuIsSupported))]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyEmptyMessageExternalMuNoContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            SignAndVerifyExternalMu(mldsa, [], []);
        }

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.ExternalMuIsSupported))]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignVerifyEmptyMessageExternalMuWithContext(MLDsaAlgorithm algorithm)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            SignAndVerifyExternalMu(mldsa, [], context);
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateSignExportPublicVerifyWithPublicOnly(MLDsaAlgorithm algorithm)
        {
            byte[] publicKey;
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature;
            byte[] hash = HashInfo.Sha512.GetHash(data);
            byte[] signaturePreHash;
            byte[]? mu = null;
            byte[] muSignature = null;

            using (MLDsa mldsa = GenerateKey(algorithm))
            {
                signature = mldsa.SignData(data);
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature));

                signaturePreHash = mldsa.SignPreHash(hash, HashInfo.Sha512.Oid);
                AssertExtensions.TrueExpression(mldsa.VerifyPreHash(hash, signaturePreHash, HashInfo.Sha512.Oid));

                publicKey = mldsa.ExportMLDsaPublicKey();

                mu = CalculateMu(mldsa, data);

                if (mu is not null)
                {
                    muSignature = mldsa.SignMu(mu);
                }
            }

            using (MLDsa mldsaPub = ImportPublicKey(algorithm, publicKey))
            {
                ExerciseSuccessfulVerify(mldsaPub, data, signature, [], mu);
                ExerciseSuccessfulVerifyPreHash(mldsaPub, HashInfo.Sha512.Oid, hash, signaturePreHash, []);
                AssertExtensions.FalseExpression(mldsaPub.VerifyPreHash(hash, signature, HashInfo.Sha512.Oid));

                if (muSignature is not null)
                {
                    ExerciseSuccessfulVerify(mldsaPub, data, muSignature, [], mu);
                }
            }
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateExportPrivateKeySignAndVerify(MLDsaAlgorithm algorithm)
        {
            byte[] privateKey;
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature;

            using (MLDsa mldsaTmp = GenerateKey(algorithm))
            {
                signature = mldsaTmp.SignData(data);
                privateKey = mldsaTmp.ExportMLDsaPrivateKey();
            }

            using (MLDsa mldsa = ImportPrivateKey(algorithm, privateKey))
            {
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature));

                Span<byte> signatureSpan = signature.AsSpan();
                signatureSpan.Fill(0);
                mldsa.SignData(data, signatureSpan);

                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature));
                data[0] ^= 1;
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature));
            }
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void GenerateExportPrivateSeedSignAndVerify(MLDsaAlgorithm algorithm)
        {
            byte[] privateSeed;
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature;

            using (MLDsa mldsaTmp = GenerateKey(algorithm))
            {
                signature = mldsaTmp.SignData(data);
                privateSeed = mldsaTmp.ExportMLDsaPrivateSeed();
            }

            using (MLDsa mldsa = ImportPrivateSeed(algorithm, privateSeed))
            {
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature));

                Span<byte> signatureSpan = signature.AsSpan();
                signatureSpan.Fill(0);
                mldsa.SignData(data, signatureSpan);

                ExerciseSuccessfulVerify(mldsa, data, signature, []);
            }
        }

        [ConditionalFact(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.SupportsExportingPrivateKeyPkcs8))]
        public void ImportPrivateKey_CannotReconstructSeed()
        {
            byte[] privateKey;
            using (MLDsa mldsaOriginal = GenerateKey(MLDsaAlgorithm.MLDsa44))
            {
                privateKey = mldsaOriginal.ExportMLDsaPrivateKey();
            }

            using (MLDsa mldsa = ImportPrivateKey(MLDsaAlgorithm.MLDsa44, privateKey))
            {
                Assert.Throws<CryptographicException>(() => mldsa.ExportMLDsaPrivateSeed(new byte[MLDsaAlgorithm.MLDsa44.PrivateSeedSizeInBytes]));
            }
        }

        [Fact]
        public void ImportSeed_CanReconstructPrivateKey()
        {
            byte[] privateKey;
            byte[] seed;
            using (MLDsa mldsaOriginal = GenerateKey(MLDsaAlgorithm.MLDsa44))
            {
                privateKey = mldsaOriginal.ExportMLDsaPrivateKey();
                seed = mldsaOriginal.ExportMLDsaPrivateSeed();
            }

            using (MLDsa mldsa = ImportPrivateSeed(MLDsaAlgorithm.MLDsa44, seed))
            {
                byte[] privateKey2 = mldsa.ExportMLDsaPrivateKey();
                byte[] seed2 = mldsa.ExportMLDsaPrivateSeed();

                AssertExtensions.SequenceEqual(privateKey, privateKey2);
                AssertExtensions.SequenceEqual(seed, seed2);
            }
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllPureMLDsaNistTestCases), MemberType = typeof(MLDsaTestsData))]
        public void NistImportPublicKeyVerify(MLDsaNistTestCase testCase)
        {
            using MLDsa mldsa = ImportPublicKey(testCase.Algorithm, testCase.PublicKey);
            Assert.Equal(testCase.ShouldPass, mldsa.VerifyData(testCase.Message, testCase.Signature, testCase.Context));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.AllPreHashMLDsaNistTestCases), MemberType = typeof(MLDsaTestsData))]
        public void NistImportPublicKeyVerifyPreHash(MLDsaNistTestCase testCase)
        {
            if (!HashInfo.KnownHashAlgorithmOids.Contains(testCase.HashAlgOid))
            {
                // This test case is not supported by the current platform.
                return;
            }

            byte[] hash = HashInfo.HashData(testCase.HashAlgOid, testCase.Message);
            using MLDsa mldsa = ImportPublicKey(testCase.Algorithm, testCase.PublicKey);
            Assert.Equal(testCase.ShouldPass, mldsa.VerifyPreHash(hash, testCase.Signature, testCase.HashAlgOid, testCase.Context));
        }

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.ExternalMuIsSupported))]
        [MemberData(nameof(MLDsaTestsData.AllExternalMuMLDsaNistTestCases), MemberType = typeof(MLDsaTestsData))]
        public void NistImportPublicKeyVerifyExternalMu(MLDsaNistTestCase testCase)
        {
            using MLDsa mldsa = ImportPublicKey(testCase.Algorithm, testCase.PublicKey);
            Assert.Equal(testCase.ShouldPass, mldsa.VerifyMu(testCase.Mu, testCase.Signature));
        }

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.SupportsExportingPrivateKeyPkcs8))]
        [MemberData(nameof(MLDsaTestsData.AllPureMLDsaNistTestCases), MemberType = typeof(MLDsaTestsData))]
        public void NistImportPrivateKeyVerifyExportsAndSignature(MLDsaNistTestCase testCase)
        {
            using MLDsa mldsa = ImportPrivateKey(testCase.Algorithm, testCase.PrivateKey);

            byte[] pubKey = mldsa.ExportMLDsaPublicKey();
            AssertExtensions.SequenceEqual(testCase.PublicKey, pubKey);

            byte[] privateKey = mldsa.ExportMLDsaPrivateKey();
            AssertExtensions.SequenceEqual(testCase.PrivateKey, privateKey);
            Assert.Throws<CryptographicException>(() => mldsa.ExportMLDsaPrivateSeed());

            Assert.Equal(testCase.ShouldPass, mldsa.VerifyData(testCase.Message, testCase.Signature, testCase.Context));
        }

        protected virtual void AssertExportPkcs8FromPublicKey(Action export) =>
            Assert.Throws<CryptographicException>(export);

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void ImportPublicKey_Export(MLDsaKeyInfo info)
        {
            using MLDsa mldsa = ImportPublicKey(info.Algorithm, info.PublicKey);

            MLDsaTestHelpers.AssertExportMLDsaPublicKey(
                export => AssertExtensions.SequenceEqual(info.PublicKey, export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaPrivateKey(
                export => Assert.Throws<CryptographicException>(() => export(mldsa)),
                export => AssertExportPkcs8FromPublicKey(() => export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaPrivateSeed(
                export => Assert.Throws<CryptographicException>(() => export(mldsa)),
                export => AssertExportPkcs8FromPublicKey(() => export(mldsa)));
        }

        [ConditionalTheory(typeof(MLDsaTestHelpers), nameof(MLDsaTestHelpers.SupportsExportingPrivateKeyPkcs8))]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void ImportPrivateKey_Export(MLDsaKeyInfo info)
        {
            using MLDsa mldsa = ImportPrivateKey(info.Algorithm, info.PrivateKey);

            MLDsaTestHelpers.AssertExportMLDsaPublicKey(export =>
                AssertExtensions.SequenceEqual(info.PublicKey, export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaPrivateKey(export =>
                AssertExtensions.SequenceEqual(info.PrivateKey, export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaPrivateSeed(
                export => Assert.Throws<CryptographicException>(() => export(mldsa)),
                // Seed is is not available in PKCS#8
                export => Assert.Null(export(mldsa)));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void ImportPrivateSeed_Export(MLDsaKeyInfo info)
        {
            using MLDsa mldsa = ImportPrivateSeed(info.Algorithm, info.PrivateSeed);

            MLDsaTestHelpers.AssertExportMLDsaPublicKey(export =>
                AssertExtensions.SequenceEqual(info.PublicKey, export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaPrivateKey(
                export => AssertExtensions.SequenceEqual(info.PrivateKey, export(mldsa)),
                // Seed is preferred in PKCS#8, so private key won't be available
                export => Assert.Null(export(mldsa)));

            MLDsaTestHelpers.AssertExportMLDsaPrivateSeed(export =>
                AssertExtensions.SequenceEqual(info.PrivateSeed, export(mldsa)));
        }

        [Theory]
        [MemberData(nameof(MLDsaTestsData.IetfMLDsaAlgorithms), MemberType = typeof(MLDsaTestsData))]
        public void SignData_PublicKeyOnlyThrows(MLDsaKeyInfo info)
        {
            using MLDsa mldsa = ImportPublicKey(info.Algorithm, info.PublicKey);
            byte[] destination = new byte[info.Algorithm.SignatureSizeInBytes];
            CryptographicException ce =
                Assert.ThrowsAny<CryptographicException>(() => mldsa.SignData("hello"u8, destination));

            Assert.DoesNotContain("unknown", ce.Message, StringComparison.OrdinalIgnoreCase);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [MemberData(nameof(UnsupportedWindowsPreHashCombinations))]
        public void SignPreHash_ThrowsForUnsupportedAlgorithmCombinations(MLDsaAlgorithm algorithm, HashInfo hashInfo)
        {
            using MLDsa mldsa = GenerateKey(algorithm);
            byte[] hash = new byte[hashInfo.OutputSize];

            CryptographicException ce = Assert.Throws<CryptographicException>(() => mldsa.SignPreHash(hash, hashInfo.Oid));
            Assert.Contains(algorithm.Name, ce.Message);
            Assert.Contains(hashInfo.Name.Name, ce.Message);
        }

        protected static byte[]? CalculateMu(MLDsa mldsa, byte[] data, byte[]? context = null)
        {
#if NET
            if (MLDsaTestHelpers.ExternalMuIsSupported)
            {
                byte[] mu = new byte[mldsa.Algorithm.MuSizeInBytes];
                Span<byte> trSpan = mu.AsSpan(0, 64);

                using (Shake256 shake = new Shake256())
                {
                    shake.AppendData(mldsa.ExportMLDsaPublicKey());
                    shake.GetHashAndReset(trSpan);

                    shake.AppendData(trSpan);

                    Span<byte> delimOrContextLength = [ 0 ];
                    shake.AppendData(delimOrContextLength);

                    delimOrContextLength[0] = checked((byte)(context?.Length ?? 0));
                    shake.AppendData(delimOrContextLength);

                    if (context is not null)
                    {
                        shake.AppendData(context);
                    }

                    if (data is not null)
                    {
                        shake.AppendData(data);
                    }

                    shake.GetHashAndReset(mu);
                }

                return mu;
            }
#endif

            return null;
        }

        protected static void SignAndVerifyExternalMu(MLDsa mldsa, byte[] data, byte[] context)
        {
            byte[]? mu = CalculateMu(mldsa, data, context);
            byte[] signature;

            if (mu is not null)
            {
                signature = mldsa.SignMu(mu);
                ExerciseSuccessfulVerify(mldsa, data, signature, context, mu);
            }

            signature = mldsa.SignData(data, context);
            ExerciseSuccessfulVerify(mldsa, data, signature, context, mu);
        }

        protected static void ExerciseSuccessfulVerify(MLDsa mldsa, byte[] data, byte[] signature, byte[]? context, byte[]? mu = null)
        {
            ReadOnlySpan<byte> buffer = [0, 1, 2, 3];

            AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature, context));

            if (data.Length > 0)
            {
                AssertExtensions.FalseExpression(mldsa.VerifyData(Array.Empty<byte>(), signature, context));
                AssertExtensions.FalseExpression(mldsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, context));

                data[0] ^= 1;
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, context));
                data[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(mldsa.VerifyData(Array.Empty<byte>(), signature, context));
                AssertExtensions.TrueExpression(mldsa.VerifyData(ReadOnlySpan<byte>.Empty, signature, context));

                AssertExtensions.FalseExpression(mldsa.VerifyData(buffer.Slice(0, 1), signature, context));
                AssertExtensions.FalseExpression(mldsa.VerifyData(buffer.Slice(1, 3), signature, context));
            }

            if (mu is not null)
            {
                AssertExtensions.TrueExpression(mldsa.VerifyMu(mu, signature));
            }

            signature[0] ^= 1;
            {
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, context));

                if (mu is not null)
                {
                    AssertExtensions.FalseExpression(mldsa.VerifyMu(mu, signature));
                }
            }
            signature[0] ^= 1;

            if (context?.Length > 0)
            {
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, Array.Empty<byte>()));
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, ReadOnlySpan<byte>.Empty));

                context[0] ^= 1;
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, context));
                context[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature, Array.Empty<byte>()));
                AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature, ReadOnlySpan<byte>.Empty));

                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, buffer.Slice(0, 1)));
                AssertExtensions.FalseExpression(mldsa.VerifyData(data, signature, buffer.Slice(1, 3)));
            }

            AssertExtensions.TrueExpression(mldsa.VerifyData(data, signature, context));

            if (mu is not null)
            {
                AssertExtensions.TrueExpression(mldsa.VerifyMu(mu, signature));
            }
        }

        protected static void ExerciseSuccessfulVerifyPreHash(MLDsa mldsa, string hashAlgorithmOid, byte[] hash, byte[] signature, byte[] context)
        {
            ReadOnlySpan<byte> buffer = [0, 1, 2, 3];

            AssertExtensions.TrueExpression(mldsa.VerifyPreHash(hash, signature, hashAlgorithmOid, context));

            if (hash.Length > 0)
            {
                Assert.Throws<CryptographicException>(() => mldsa.VerifyPreHash(Array.Empty<byte>(), signature, hashAlgorithmOid, context));
                Assert.Throws<CryptographicException>(() => mldsa.VerifyPreHash(ReadOnlySpan<byte>.Empty, signature, hashAlgorithmOid, context));

                hash[0] ^= 1;
                AssertExtensions.FalseExpression(mldsa.VerifyPreHash(hash, signature, hashAlgorithmOid, context));
                hash[0] ^= 1;
            }
            else
            {
                Assert.Fail("Empty hash is not supported.");
            }

            signature[0] ^= 1;
            AssertExtensions.FalseExpression(mldsa.VerifyPreHash(hash, signature, hashAlgorithmOid, context));
            signature[0] ^= 1;

            if (context.Length > 0)
            {
                AssertExtensions.FalseExpression(mldsa.VerifyPreHash(hash, signature, hashAlgorithmOid, Array.Empty<byte>()));
                AssertExtensions.FalseExpression(mldsa.VerifyPreHash(hash, signature, hashAlgorithmOid, ReadOnlySpan<byte>.Empty));

                context[0] ^= 1;
                AssertExtensions.FalseExpression(mldsa.VerifyPreHash(hash, signature, hashAlgorithmOid, context));
                context[0] ^= 1;
            }
            else
            {
                AssertExtensions.TrueExpression(mldsa.VerifyPreHash(hash, signature, hashAlgorithmOid, Array.Empty<byte>()));
                AssertExtensions.TrueExpression(mldsa.VerifyPreHash(hash, signature, hashAlgorithmOid, ReadOnlySpan<byte>.Empty));

                AssertExtensions.FalseExpression(mldsa.VerifyPreHash(hash, signature, hashAlgorithmOid, buffer.Slice(0, 1)));
                AssertExtensions.FalseExpression(mldsa.VerifyPreHash(hash, signature, hashAlgorithmOid, buffer.Slice(1, 3)));
            }

            AssertExtensions.FalseExpression(mldsa.VerifyPreHash(hash, signature, "1." + hashAlgorithmOid, context));

            AssertExtensions.TrueExpression(mldsa.VerifyPreHash(hash, signature, hashAlgorithmOid, context));
        }

        public static IEnumerable<object[]> UnsupportedWindowsPreHashCombinations()
        {
            yield return new object[] { MLDsaAlgorithm.MLDsa44, HashInfo.Md5 };
            yield return new object[] { MLDsaAlgorithm.MLDsa44, HashInfo.Sha1 };

            yield return new object[] { MLDsaAlgorithm.MLDsa65, HashInfo.Md5 };
            yield return new object[] { MLDsaAlgorithm.MLDsa65, HashInfo.Sha1 };
            yield return new object[] { MLDsaAlgorithm.MLDsa65, HashInfo.Sha256 };
            yield return new object[] { MLDsaAlgorithm.MLDsa65, HashInfo.Sha3_256 };
            yield return new object[] { MLDsaAlgorithm.MLDsa65, HashInfo.Shake128 };

            yield return new object[] { MLDsaAlgorithm.MLDsa87, HashInfo.Md5 };
            yield return new object[] { MLDsaAlgorithm.MLDsa87, HashInfo.Sha1 };
            yield return new object[] { MLDsaAlgorithm.MLDsa87, HashInfo.Sha256 };
            yield return new object[] { MLDsaAlgorithm.MLDsa87, HashInfo.Sha3_256 };
            yield return new object[] { MLDsaAlgorithm.MLDsa87, HashInfo.Sha384 };
            yield return new object[] { MLDsaAlgorithm.MLDsa87, HashInfo.Sha3_384 };
            yield return new object[] { MLDsaAlgorithm.MLDsa87, HashInfo.Shake128 };
        }
    }
}
