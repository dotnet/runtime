// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.SLHDsa.Tests
{
    [ConditionalClass(typeof(SlhDsa), nameof(SlhDsa.IsSupported))]
    public abstract class SlhDsaImplementationTestsBase : SlhDsaInstanceTestsBase
    {
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

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void GenerateSignVerifyNoContext(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] data = [1, 2, 3, 4, 5];
            byte[] signature = new byte[slhDsa.Algorithm.SignatureSizeInBytes];
            Assert.Equal(signature.Length, slhDsa.SignData(data, signature));

            ExerciseSuccessfulVerify(slhDsa, data, signature, []);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
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
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void GenerateSignVerifyEmptyMessageNoContext(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] signature = new byte[slhDsa.Algorithm.SignatureSizeInBytes];
            Assert.Equal(signature.Length, slhDsa.SignData([], signature));

            ExerciseSuccessfulVerify(slhDsa, [], signature, []);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
        public void GenerateSignVerifyEmptyMessageWithContext(SlhDsaAlgorithm algorithm)
        {
            using SlhDsa slhDsa = GenerateKey(algorithm);
            byte[] context = [1, 1, 3, 5, 6];
            byte[] signature = new byte[slhDsa.Algorithm.SignatureSizeInBytes];
            Assert.Equal(signature.Length, slhDsa.SignData([], signature, context));

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
        [MemberData(nameof(SlhDsaTestData.AlgorithmsData), MemberType = typeof(SlhDsaTestData))]
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
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportSecretKey_ExportPkcs8PrivateKey_Pem(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);

            string pem = slhDsa.ExportPkcs8PrivateKeyPem();
            AssertPemsEqual(PemEncoding.WriteString("PRIVATE KEY", info.Pkcs8PrivateKey), pem);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportSecretKey_ExportPkcs8PublicKey_Pem(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);

            string pem = slhDsa.ExportSubjectPublicKeyInfoPem();
            AssertPemsEqual(PemEncoding.WriteString("PUBLIC KEY", info.Pkcs8PublicKey), pem);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportPublicKey_ExportPkcs8PublicKey_Pem(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaPublicKey(info.Algorithm, info.PublicKey);

            string pem = slhDsa.ExportSubjectPublicKeyInfoPem();
            AssertPemsEqual(PemEncoding.WriteString("PUBLIC KEY", info.Pkcs8PublicKey), pem);
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportSecretKey_ExportPkcs8PrivateKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);
            AssertExportPkcs8PrivateKey(slhDsa, pkcs8 => AssertExtensions.SequenceEqual(info.Pkcs8PrivateKey, pkcs8));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportSecretKey_ExportPkcs8PublicKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);
            AssertExportSubjectPublicKeyInfo(slhDsa, spki => AssertExtensions.SequenceEqual(info.Pkcs8PublicKey, spki));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ImportPublicKey_ExportPkcs8PublicKey(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaPublicKey(info.Algorithm, info.PublicKey);
            AssertExportSubjectPublicKeyInfo(slhDsa, spki => AssertExtensions.SequenceEqual(info.Pkcs8PublicKey, spki));
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ExportEncryptedPkcs8PrivateKey_PbeParameters(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);
            AssertEncryptedExportPkcs8PrivateKey(slhDsa, info.EncryptionPassword, info.EncryptionParameters, pkcs8 =>
            {
                AssertEncryptedPkcs8PrivateKeyContents(info.EncryptionParameters, pkcs8);
            });
        }

        [Theory]
        [MemberData(nameof(SlhDsaTestData.GeneratedKeyInfosData), MemberType = typeof(SlhDsaTestData))]
        public void ExportKey_DestinationTooSmall(SlhDsaTestData.SlhDsaGeneratedKeyInfo info)
        {
            using SlhDsa slhDsa = ImportSlhDsaSecretKey(info.Algorithm, info.SecretKey);
            byte[] pkcs8PrivateKey = slhDsa.ExportPkcs8PrivateKey();
            byte[] spki = slhDsa.ExportSubjectPublicKeyInfo();
            byte[] encryptedPkcs8 = slhDsa.ExportEncryptedPkcs8PrivateKey(info.EncryptionPassword, info.EncryptionParameters);
            byte[] largeBuffer = new byte[2 * Math.Max(pkcs8PrivateKey.Length, Math.Max(spki.Length, encryptedPkcs8.Length))];

            int bytesWritten = -1;

            // TryExportPkcs8PrivateKey
            AssertExtensions.FalseExpression(slhDsa.TryExportPkcs8PrivateKey(Span<byte>.Empty, out bytesWritten));               // Empty
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportPkcs8PrivateKey(largeBuffer, out bytesWritten));                     // Too large
            AssertExtensions.Equal(pkcs8PrivateKey.Length, bytesWritten);
            AssertExtensions.FalseExpression(slhDsa.TryExportPkcs8PrivateKey(pkcs8PrivateKey.AsSpan(0..^1), out bytesWritten));  // Too small
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportPkcs8PrivateKey(pkcs8PrivateKey, out bytesWritten));                 // Exact size
            AssertExtensions.Equal(pkcs8PrivateKey.Length, bytesWritten);

            // TryExportSubjectPublicKeyInfo
            AssertExtensions.FalseExpression(slhDsa.TryExportSubjectPublicKeyInfo(Span<byte>.Empty, out bytesWritten));          
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportSubjectPublicKeyInfo(largeBuffer, out bytesWritten));
            AssertExtensions.Equal(spki.Length, bytesWritten);
            AssertExtensions.FalseExpression(slhDsa.TryExportSubjectPublicKeyInfo(spki.AsSpan(0..^1), out bytesWritten));
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportSubjectPublicKeyInfo(spki, out bytesWritten));
            AssertExtensions.Equal(spki.Length, bytesWritten);

            // TryExportEncryptedPkcs8PrivateKey (string password)
            AssertExtensions.FalseExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password", info.EncryptionParameters, Span<byte>.Empty, out bytesWritten));
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password", info.EncryptionParameters, largeBuffer, out bytesWritten));
            AssertExtensions.Equal(encryptedPkcs8.Length, bytesWritten);
            AssertExtensions.FalseExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password", info.EncryptionParameters, encryptedPkcs8.AsSpan(0..^1), out bytesWritten));
            AssertExtensions.Equal(0, bytesWritten);
            AssertExtensions.TrueExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password", info.EncryptionParameters, encryptedPkcs8, out bytesWritten));
            AssertExtensions.Equal(encryptedPkcs8.Length, bytesWritten);

            if (info.EncryptionParameters.EncryptionAlgorithm is not PbeEncryptionAlgorithm.TripleDes3KeyPkcs12)
            {
                // TryExportEncryptedPkcs8PrivateKey (byte[] password)
                AssertExtensions.FalseExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password"u8, info.EncryptionParameters, Span<byte>.Empty, out bytesWritten));
                AssertExtensions.Equal(0, bytesWritten);
                AssertExtensions.TrueExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password"u8, info.EncryptionParameters, largeBuffer, out bytesWritten));
                AssertExtensions.Equal(encryptedPkcs8.Length, bytesWritten);
                AssertExtensions.FalseExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password"u8, info.EncryptionParameters, encryptedPkcs8.AsSpan(0..^1), out bytesWritten));
                AssertExtensions.Equal(0, bytesWritten);
                AssertExtensions.TrueExpression(slhDsa.TryExportEncryptedPkcs8PrivateKey("password"u8, info.EncryptionParameters, encryptedPkcs8, out bytesWritten));
                AssertExtensions.Equal(encryptedPkcs8.Length, bytesWritten);
            }
        }

        private static void AssertPemsEqual(string expectedPem, string actualPem)
        {
            (string Label, byte[] Base64Data)[] expectedPemObjects = EnumeratePem(expectedPem).ToArray();
            (string Label, byte[] Base64Data)[] actualPemObjects = EnumeratePem(actualPem).ToArray();

            AssertExtensions.TrueExpression(expectedPemObjects.Length == actualPemObjects.Length);

            for (int i = 0; i < expectedPemObjects.Length; i++)
            {
                Assert.Equal(expectedPemObjects[i].Label, actualPemObjects[i].Label);
                AssertExtensions.SequenceEqual(expectedPemObjects[i].Base64Data, actualPemObjects[i].Base64Data);
            }
        }

        private static IEnumerable<(string Label, byte[] Base64Data)> EnumeratePem(string pem)
        {
            ReadOnlyMemory<char> pemMemory = pem.AsMemory();
            while (PemEncoding.TryFind(pemMemory.Span, out PemFields fields))
            {
                byte[] data = new byte[fields.DecodedDataLength];
                AssertExtensions.TrueExpression(Convert.TryFromBase64Chars(pemMemory.Span[fields.Base64Data], data, out int bytesWritten));
                AssertExtensions.TrueExpression(bytesWritten == fields.DecodedDataLength);
                yield return (pemMemory[fields.Label].ToString(), data.AsSpan(0, bytesWritten).ToArray());
                pemMemory = pemMemory[fields.Location.End..];
            }
        }
    }
}
