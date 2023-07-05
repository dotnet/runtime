// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.Cryptography.Rsa.Tests;
using Test.IO.Streams;
using Xunit;

namespace System.Security.Cryptography.Csp.Tests
{
    public class RSACryptoServiceProviderBackCompat
    {
        private static readonly byte[] s_dataToSign = { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };

        [Theory]
        [MemberData(nameof(AlgorithmIdentifiers))]
        public static void AlgorithmLookups(string primaryId, object halg)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(TestData.RSA2048Params);

                byte[] primary = rsa.SignData(s_dataToSign, primaryId);
                byte[] lookup = rsa.SignData(s_dataToSign, halg);

                Assert.Equal(primary, lookup);
            }
        }

        [Fact]
        public static void ApiInterop_OldToNew()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                byte[] oldSignature = rsa.SignData(s_dataToSign, "SHA384");
                Assert.True(rsa.VerifyData(s_dataToSign, oldSignature, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1));
            }
        }

        [Fact]
        public static void ApiInterop_OldToNew_Positional()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                byte[] oldSignature = rsa.SignData(s_dataToSign, 3, 2, "SHA384");
                Assert.True(rsa.VerifyData(s_dataToSign, 3, 2, oldSignature, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1));
            }
        }

        [Fact]
        public static void ApiInterop_OldToNew_Stream()
        {
            const int TotalCount = 10;

            using (var rsa = new RSACryptoServiceProvider())
            using (PositionValueStream stream1 = new PositionValueStream(TotalCount))
            using (PositionValueStream stream2 = new PositionValueStream(TotalCount))
            {
                byte[] oldSignature = rsa.SignData(stream1, "SHA384");
                Assert.True(rsa.VerifyData(stream2, oldSignature, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1));
            }
        }

        [Fact]
        public static void ApiInterop_NewToOld()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                byte[] newSignature = rsa.SignData(s_dataToSign, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1);
                Assert.True(rsa.VerifyData(s_dataToSign, "SHA384", newSignature));
            }
        }

        [Fact]
        public static void SignHash_NullArray()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                Assert.Throws<ArgumentNullException>(() => rsa.SignHash(null, "SHA384"));
            }
        }

        [Fact]
        public static void SignHash_BadAlgorithm()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                Assert.Throws<CryptographicException>(() => rsa.SignHash(Array.Empty<byte>(), "SHA384-9000"));
            }
        }

        [Fact]
        public static void SignHash_WrongSizeHash()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                Assert.ThrowsAny<CryptographicException>(() => rsa.SignHash(Array.Empty<byte>(), "SHA384"));
            }
        }

        [Fact]
        public static void SignHash_PublicKey()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                RSAParameters publicParams = new RSAParameters
                {
                    Modulus = TestData.RSA1024Params.Modulus,
                    Exponent = TestData.RSA1024Params.Exponent,
                };

                rsa.ImportParameters(publicParams);

                Assert.Throws<CryptographicException>(() => rsa.SignHash(Array.Empty<byte>(), "SHA384"));
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        // (false, false) is not required, that would be equivalent to the RSA AlgorithmImplementation suite.
        public static void VerifyLegacySignVerifyHash(bool useLegacySign, bool useLegacyVerify)
        {
            byte[] dataHash, signature;

            dataHash = SHA256.HashData(TestData.HelloBytes);

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(TestData.RSA2048Params);

                signature = useLegacySign ?
                    rsa.SignHash(dataHash, "SHA256") :
                    rsa.SignHash(dataHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            bool verified;

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(
                    new RSAParameters
                    {
                        Modulus = TestData.RSA2048Params.Modulus,
                        Exponent = TestData.RSA2048Params.Exponent,
                    });

                verified = useLegacyVerify ?
                    rsa.VerifyHash(dataHash, "SHA256", signature) :
                    rsa.VerifyHash(dataHash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            Assert.True(verified);
        }

        public static IEnumerable<object[]> AlgorithmIdentifiers()
        {
            yield return new object[] { "MD5", MD5.Create() };
            yield return new object[] { "MD5", typeof(MD5) };
            yield return new object[] { "MD5", "1.2.840.113549.2.5" };

            if (RSAFactory.SupportsSha1Signatures)
            {
                yield return new object[] { "SHA1", SHA1.Create() };
                yield return new object[] { "SHA1", typeof(SHA1) };
                yield return new object[] { "SHA1", "1.3.14.3.2.26" };
            }

            yield return new object[] { "SHA256", SHA256.Create() };
            yield return new object[] { "SHA256", typeof(SHA256) };
            yield return new object[] { "SHA256", "2.16.840.1.101.3.4.2.1" };
            yield return new object[] { "SHA384", SHA384.Create() };
            yield return new object[] { "SHA384", typeof(SHA384) };
            yield return new object[] { "SHA384", "2.16.840.1.101.3.4.2.2" };
            yield return new object[] { "SHA512", SHA512.Create() };
            yield return new object[] { "SHA512", typeof(SHA512) };
            yield return new object[] { "SHA512", "2.16.840.1.101.3.4.2.3" };
        }
    }
}
