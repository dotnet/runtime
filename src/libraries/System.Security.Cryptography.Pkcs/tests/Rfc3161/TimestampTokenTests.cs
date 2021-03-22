// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static class TimestampTokenTests
    {
        [Theory]
        [InlineData(nameof(TimestampTokenTestData.FreeTsaDotOrg1))]
        [InlineData(nameof(TimestampTokenTestData.Symantec1))]
        [InlineData(nameof(TimestampTokenTestData.DigiCert1))]
        public static void ParseDocument(string testDataName)
        {
            TimestampTokenTestData testData = TimestampTokenTestData.GetTestData(testDataName);

            TestParseDocument(testData.FullTokenBytes, testData, testData.FullTokenBytes.Length);
        }

        [Theory]
        [InlineData(nameof(TimestampTokenTestData.FreeTsaDotOrg1))]
        [InlineData(nameof(TimestampTokenTestData.Symantec1))]
        [InlineData(nameof(TimestampTokenTestData.DigiCert1))]
        public static void ParseDocument_ExcessData(string testDataName)
        {
            TimestampTokenTestData testData = TimestampTokenTestData.GetTestData(testDataName);

            int baseLen = testData.FullTokenBytes.Length;
            byte[] tooMuchData = new byte[baseLen + 30];
            testData.FullTokenBytes.CopyTo(tooMuchData);

            // Look like an octet string of the remainder of the payload.  Should be ignored.
            tooMuchData[baseLen] = 0x04;
            tooMuchData[baseLen + 1] = 28;

            TestParseDocument(tooMuchData, testData, baseLen);
        }

        private static void TestParseDocument(
            ReadOnlyMemory<byte> tokenBytes,
            TimestampTokenTestData testData,
            int? expectedBytesRead)
        {
            int bytesRead;
            Rfc3161TimestampToken token;

            Assert.True(
                Rfc3161TimestampToken.TryDecode(tokenBytes, out token, out bytesRead),
                "Rfc3161TimestampToken.TryDecode");

            if (expectedBytesRead != null)
            {
                Assert.Equal(expectedBytesRead.Value, bytesRead);
            }

            Assert.NotNull(token);
            TimestampTokenInfoTests.AssertEqual(testData, token.TokenInfo);

            SignedCms signedCms = token.AsSignedCms();
            Assert.NotNull(signedCms);
            Assert.Equal(Oids.TstInfo, signedCms.ContentInfo.ContentType.Value);

            Assert.Equal(
                testData.TokenInfoBytes.ByteArrayToHex(),
                signedCms.ContentInfo.Content.ByteArrayToHex());

            if (testData.EmbeddedSigningCertificate != null)
            {
                Assert.NotNull(signedCms.SignerInfos[0].Certificate);

                Assert.Equal(
                    testData.EmbeddedSigningCertificate.Value.ByteArrayToHex(),
                    signedCms.SignerInfos[0].Certificate.RawData.ByteArrayToHex());

                // Assert.NoThrow
                signedCms.CheckSignature(true);
            }
            else
            {
                Assert.Null(signedCms.SignerInfos[0].Certificate);

                using (var signerCert = new X509Certificate2(testData.ExternalCertificateBytes))
                {
                    // Assert.NoThrow
                    signedCms.CheckSignature(
                        new X509Certificate2Collection(signerCert),
                        true);
                }
            }

            X509Certificate2 returnedCert;
            ReadOnlySpan<byte> messageContentSpan = testData.MessageContent.Span;
            X509Certificate2Collection candidates = null;

            if (testData.EmbeddedSigningCertificate != null)
            {
                Assert.True(
                    token.VerifySignatureForData(messageContentSpan, out returnedCert),
                    "token.VerifySignatureForData(correct)");

                Assert.NotNull(returnedCert);
                Assert.Equal(signedCms.SignerInfos[0].Certificate, returnedCert);
            }
            else
            {
                candidates = new X509Certificate2Collection
                {
                    new X509Certificate2(testData.ExternalCertificateBytes),
                };

                Assert.False(
                    token.VerifySignatureForData(messageContentSpan, out returnedCert),
                    "token.VerifySignatureForData(correct, no cert)");

                Assert.Null(returnedCert);

                Assert.True(
                    token.VerifySignatureForData(messageContentSpan, out returnedCert, candidates),
                    "token.VerifySignatureForData(correct, certs)");

                Assert.NotNull(returnedCert);
                Assert.Equal(candidates[0], returnedCert);
            }

            X509Certificate2 previousCert = returnedCert;

            Assert.False(
                token.VerifySignatureForData(messageContentSpan.Slice(1), out returnedCert, candidates),
                "token.VerifySignatureForData(incorrect)");

            Assert.Null(returnedCert);

            byte[] messageHash = testData.HashBytes.ToArray();

            Assert.False(
                token.VerifySignatureForHash(messageHash, HashAlgorithmName.MD5, out returnedCert, candidates),
                "token.VerifyHash(correct, MD5)");
            Assert.Null(returnedCert);

            Assert.False(
                token.VerifySignatureForHash(messageHash, new Oid(Oids.Md5), out returnedCert, candidates),
                "token.VerifyHash(correct, Oid(MD5))");
            Assert.Null(returnedCert);

            Assert.True(
                token.VerifySignatureForHash(messageHash, new Oid(testData.HashAlgorithmId), out returnedCert, candidates),
                "token.VerifyHash(correct, Oid(algId))");
            Assert.NotNull(returnedCert);
            Assert.Equal(previousCert, returnedCert);

            messageHash[0] ^= 0xFF;
            Assert.False(
                token.VerifySignatureForHash(messageHash, new Oid(testData.HashAlgorithmId), out returnedCert, candidates),
                "token.VerifyHash(incorrect, Oid(algId))");
            Assert.Null(returnedCert);
        }

        [Fact]
        public static void TryDecode_Fails_SignedCmsOfData()
        {
            Assert.False(
                Rfc3161TimestampToken.TryDecode(
                    SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber,
                    out Rfc3161TimestampToken token,
                    out int bytesRead),
                "Rfc3161TimestampToken.TryDecode");

            Assert.Equal(0, bytesRead);
            Assert.Null(token);
        }

        [Fact]
        public static void TryDecode_Fails_Empty()
        {
            Assert.False(
                Rfc3161TimestampToken.TryDecode(
                    ReadOnlyMemory<byte>.Empty,
                    out Rfc3161TimestampToken token,
                    out int bytesRead),
                "Rfc3161TimestampToken.TryDecode");

            Assert.Equal(0, bytesRead);
            Assert.Null(token);
        }

        [Fact]
        public static void TryDecode_Fails_EnvelopedCms()
        {
            byte[] encodedMessage =
            ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
             + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004818013"
             + "dc0eb2984a445d04a1f6246b8fe41f1d24507548d449d454d5bb5e0638d75ed101bf78c0155a5d208eb746755fbccbc86923"
             + "8443760a9ae94770d6373e0197be23a6a891f0c522ca96b3e8008bf23547474b7e24e7f32e8134df3862d84f4dea2470548e"
             + "c774dd74f149a56cdd966e141122900d00ad9d10ea1848541294a1302b06092a864886f70d010701301406082a864886f70d"
             + "030704089c8119f6cf6b174c8008bcea3a10d0737eb9").HexToByteArray();

            Assert.False(
                Rfc3161TimestampToken.TryDecode(
                    encodedMessage,
                    out Rfc3161TimestampToken token,
                    out int bytesRead),
                "Rfc3161TimestampToken.TryDecode");

            Assert.Equal(0, bytesRead);
            Assert.Null(token);
        }

        [Fact]
        public static void TryDecode_Fails_MalformedToken()
        {
            ContentInfo contentInfo = new ContentInfo(
                new Oid(Oids.TstInfo, Oids.TstInfo),
                new byte[] { 1 });

            SignedCms cms = new SignedCms(contentInfo);

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                cms.ComputeSignature(new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert));
            }

            Assert.False(
                Rfc3161TimestampToken.TryDecode(
                    cms.Encode(),
                    out Rfc3161TimestampToken token,
                    out int bytesRead),
                "Rfc3161TimestampToken.TryDecode");

            Assert.Equal(0, bytesRead);
            Assert.Null(token);
        }

        [Theory]
        [InlineData(X509IncludeOption.WholeChain, SigningCertificateOption.ValidHashNoName)]
        [InlineData(X509IncludeOption.None, SigningCertificateOption.ValidHashNoName)]
        [InlineData(X509IncludeOption.WholeChain, SigningCertificateOption.ValidHashWithName)]
        [InlineData(X509IncludeOption.None, SigningCertificateOption.ValidHashWithName)]
        public static void MatchV1(X509IncludeOption includeOption, SigningCertificateOption v1Option)
        {
            CustomBuild_CertMatch(
                Certificates.ValidLookingTsaCert,
                new DateTimeOffset(2018, 1, 10, 17, 21, 11, 802, TimeSpan.Zero),
                v1Option,
                SigningCertificateOption.Omit,
                includeOption: includeOption);
        }

        [Theory]
        [InlineData(X509IncludeOption.WholeChain)]
        [InlineData(X509IncludeOption.None)]
        public static void CertHashMismatchV1(X509IncludeOption includeOption)
        {
            CustomBuild_CertMismatch(
                Certificates.ValidLookingTsaCert,
                new DateTimeOffset(2018, 1, 10, 17, 21, 11, 802, TimeSpan.Zero),
                SigningCertificateOption.InvalidHashNoName,
                SigningCertificateOption.Omit,
                includeOption: includeOption);
        }

        [Theory]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithInvalidName,
            SubjectIdentifierType.SubjectKeyIdentifier)]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithInvalidName,
            SubjectIdentifierType.SubjectKeyIdentifier)]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithInvalidName,
            SubjectIdentifierType.IssuerAndSerialNumber)]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithInvalidName,
            SubjectIdentifierType.IssuerAndSerialNumber)]
        public static void CertMismatchIssuerAndSerialV1(
            X509IncludeOption includeOption,
            SigningCertificateOption v1Option,
            SubjectIdentifierType identifierType)
        {
            CustomBuild_CertMismatch(
                Certificates.ValidLookingTsaCert,
                new DateTimeOffset(2018, 1, 10, 17, 21, 11, 802, TimeSpan.Zero),
                v1Option,
                SigningCertificateOption.Omit,
                includeOption: includeOption,
                identifierType: identifierType);
        }

        [Theory]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashNoName,
            null)]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashNoName,
            null)]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithName,
            "MD5")]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithName,
            "MD5")]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithName,
            "SHA1")]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithName,
            "SHA1")]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithName,
            "SHA384")]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithName,
            "SHA384")]
        public static void MatchV2(
            X509IncludeOption includeOption,
            SigningCertificateOption v2Option,
            string hashAlgName)
        {
            CustomBuild_CertMatch(
                Certificates.ValidLookingTsaCert,
                new DateTimeOffset(2018, 1, 10, 17, 21, 11, 802, TimeSpan.Zero),
                SigningCertificateOption.Omit,
                v2Option,
                hashAlgName == null ? default(HashAlgorithmName) : new HashAlgorithmName(hashAlgName),
                includeOption);
        }

        [Theory]
        [InlineData(X509IncludeOption.WholeChain, null)]
        [InlineData(X509IncludeOption.None, null)]
        [InlineData(X509IncludeOption.WholeChain, "MD5")]
        [InlineData(X509IncludeOption.None, "MD5")]
        [InlineData(X509IncludeOption.WholeChain, "SHA1")]
        [InlineData(X509IncludeOption.None, "SHA1")]
        [InlineData(X509IncludeOption.WholeChain, "SHA384")]
        [InlineData(X509IncludeOption.None, "SHA384")]
        public static void CertHashMismatchV2(X509IncludeOption includeOption, string hashAlgName)
        {
            CustomBuild_CertMismatch(
                Certificates.ValidLookingTsaCert,
                new DateTimeOffset(2018, 1, 10, 17, 21, 11, 802, TimeSpan.Zero),
                SigningCertificateOption.Omit,
                SigningCertificateOption.InvalidHashNoName,
                hashAlgName == null ? default(HashAlgorithmName) : new HashAlgorithmName(hashAlgName),
                includeOption: includeOption);
        }

        [Theory]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithInvalidName,
            SubjectIdentifierType.SubjectKeyIdentifier,
            null)]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithInvalidName,
            SubjectIdentifierType.SubjectKeyIdentifier,
            null)]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithInvalidName,
            SubjectIdentifierType.SubjectKeyIdentifier,
            "SHA384")]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithInvalidName,
            SubjectIdentifierType.SubjectKeyIdentifier,
            "SHA384")]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithInvalidName,
            SubjectIdentifierType.IssuerAndSerialNumber,
            null)]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithInvalidName,
            SubjectIdentifierType.IssuerAndSerialNumber,
            null)]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithInvalidName,
            SubjectIdentifierType.IssuerAndSerialNumber,
            "SHA384")]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithInvalidName,
            SubjectIdentifierType.IssuerAndSerialNumber,
            "SHA384")]
        public static void CertMismatchIssuerAndSerialV2(
            X509IncludeOption includeOption,
            SigningCertificateOption v2Option,
            SubjectIdentifierType identifierType,
            string hashAlgName)
        {
            CustomBuild_CertMismatch(
                Certificates.ValidLookingTsaCert,
                new DateTimeOffset(2018, 1, 10, 17, 21, 11, 802, TimeSpan.Zero),
                SigningCertificateOption.Omit,
                v2Option,
                hashAlgName == null ? default(HashAlgorithmName) : new HashAlgorithmName(hashAlgName),
                includeOption: includeOption,
                identifierType: identifierType);
        }

        [Theory]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashNoName,
            SigningCertificateOption.ValidHashNoName,
            null)]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashNoName,
            SigningCertificateOption.ValidHashNoName,
            "SHA512")]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashNoName,
            SigningCertificateOption.ValidHashNoName,
            null)]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashNoName,
            SigningCertificateOption.ValidHashNoName,
            "SHA512")]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashNoName,
            SigningCertificateOption.ValidHashWithName,
            null)]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashNoName,
            SigningCertificateOption.ValidHashWithName,
            "SHA384")]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashNoName,
            SigningCertificateOption.ValidHashWithName,
            null)]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashNoName,
            SigningCertificateOption.ValidHashWithName,
            "SHA384")]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithName,
            SigningCertificateOption.ValidHashNoName,
            null)]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithName,
            SigningCertificateOption.ValidHashNoName,
            "SHA512")]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithName,
            SigningCertificateOption.ValidHashNoName,
            null)]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithName,
            SigningCertificateOption.ValidHashNoName,
            "SHA512")]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithName,
            SigningCertificateOption.ValidHashWithName,
            null)]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithName,
            SigningCertificateOption.ValidHashWithName,
            "SHA384")]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithName,
            SigningCertificateOption.ValidHashWithName,
            null)]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithName,
            SigningCertificateOption.ValidHashWithName,
            "SHA384")]
        public static void CertMatchV1AndV2(
            X509IncludeOption includeOption,
            SigningCertificateOption v1Option,
            SigningCertificateOption v2Option,
            string hashAlgName)
        {
            CustomBuild_CertMatch(
                Certificates.ValidLookingTsaCert,
                new DateTimeOffset(2018, 1, 10, 17, 21, 11, 802, TimeSpan.Zero),
                v1Option,
                v2Option,
                hashAlgName == null ? default(HashAlgorithmName) : new HashAlgorithmName(hashAlgName),
                includeOption);
        }

        [Theory]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.InvalidHashNoName,
            SigningCertificateOption.ValidHashWithName,
            SubjectIdentifierType.IssuerAndSerialNumber,
            null)]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithInvalidSerial,
            SigningCertificateOption.ValidHashWithName,
            SubjectIdentifierType.IssuerAndSerialNumber,
            "SHA384")]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.ValidHashWithInvalidName,
            SigningCertificateOption.InvalidHashNoName,
            SubjectIdentifierType.SubjectKeyIdentifier,
            null)]
        [InlineData(
            X509IncludeOption.None,
            SigningCertificateOption.ValidHashWithName,
            SigningCertificateOption.InvalidHashNoName,
            SubjectIdentifierType.SubjectKeyIdentifier,
            "SHA512")]
        [InlineData(
            X509IncludeOption.WholeChain,
            SigningCertificateOption.InvalidHashWithInvalidSerial,
            SigningCertificateOption.ValidHashNoName,
            SubjectIdentifierType.IssuerAndSerialNumber,
            null)]
        public static void CertMismatchV1OrV2(
            X509IncludeOption includeOption,
            SigningCertificateOption v1Option,
            SigningCertificateOption v2Option,
            SubjectIdentifierType identifierType,
            string hashAlgName)
        {
            CustomBuild_CertMismatch(
                Certificates.ValidLookingTsaCert,
                new DateTimeOffset(2018, 1, 10, 17, 21, 11, 802, TimeSpan.Zero),
                v1Option,
                v2Option,
                hashAlgName == null ? default(HashAlgorithmName) : new HashAlgorithmName(hashAlgName),
                includeOption: includeOption,
                identifierType: identifierType);
        }

        [Theory]
        [InlineData(X509IncludeOption.WholeChain)]
        [InlineData(X509IncludeOption.None)]
        public static void TimestampTooOld(X509IncludeOption includeOption)
        {
            CertLoader loader = Certificates.ValidLookingTsaCert;
            DateTimeOffset referenceTime;

            using (X509Certificate2 cert = loader.GetCertificate())
            {
                referenceTime = cert.NotBefore.AddSeconds(-1);
            }

            CustomBuild_CertMismatch(
                loader,
                referenceTime,
                SigningCertificateOption.ValidHashNoName,
                SigningCertificateOption.Omit,
                includeOption: includeOption);
        }

        [Theory]
        [InlineData(X509IncludeOption.WholeChain)]
        [InlineData(X509IncludeOption.None)]
        public static void TimestampTooNew(X509IncludeOption includeOption)
        {
            CertLoader loader = Certificates.ValidLookingTsaCert;
            DateTimeOffset referenceTime;

            using (X509Certificate2 cert = loader.GetCertificate())
            {
                referenceTime = cert.NotAfter.AddSeconds(1);
            }

            CustomBuild_CertMismatch(
                loader,
                referenceTime,
                SigningCertificateOption.ValidHashNoName,
                SigningCertificateOption.Omit,
                includeOption: includeOption);
        }

        [Theory]
        [InlineData(X509IncludeOption.WholeChain)]
        [InlineData(X509IncludeOption.None)]
        public static void NoEkuExtension(X509IncludeOption includeOption)
        {
            CertLoader loader = Certificates.RSA2048SignatureOnly;
            DateTimeOffset referenceTime;

            using (X509Certificate2 cert = loader.GetCertificate())
            {
                referenceTime = cert.NotAfter.AddDays(-1);

                Assert.Equal(0, cert.Extensions.OfType<X509EnhancedKeyUsageExtension>().Count());
            }

            CustomBuild_CertMismatch(
                loader,
                referenceTime,
                SigningCertificateOption.ValidHashNoName,
                SigningCertificateOption.Omit,
                includeOption: includeOption);
        }

        [Theory]
        [InlineData(X509IncludeOption.WholeChain)]
        [InlineData(X509IncludeOption.None)]
        public static void TwoEkuExtensions(X509IncludeOption includeOption)
        {
            CertLoader loader = Certificates.TwoEkuTsaCert;
            DateTimeOffset referenceTime;

            using (X509Certificate2 cert = loader.GetCertificate())
            {
                referenceTime = cert.NotAfter.AddDays(-1);

                var ekuExts = cert.Extensions.OfType<X509EnhancedKeyUsageExtension>().ToList();

                Assert.Equal(2, ekuExts.Count);

                // Make sure we're validating that "early success" doesn't happen.
                Assert.Contains(
                    Oids.TimeStampingPurpose,
                    ekuExts[0].EnhancedKeyUsages.OfType<Oid>().Select(o => o.Value));
            }

            CustomBuild_CertMismatch(
                loader,
                referenceTime,
                SigningCertificateOption.ValidHashNoName,
                SigningCertificateOption.Omit,
                includeOption: includeOption);
        }

        [Theory]
        [InlineData(X509IncludeOption.WholeChain)]
        [InlineData(X509IncludeOption.None)]
        public static void NonCriticalEkuExtension(X509IncludeOption includeOption)
        {
            CertLoader loader = Certificates.NonCriticalTsaEku;
            DateTimeOffset referenceTime;

            using (X509Certificate2 cert = loader.GetCertificate())
            {
                referenceTime = cert.NotAfter.AddDays(-1);

                var ekuExts = cert.Extensions.OfType<X509EnhancedKeyUsageExtension>().ToList();

                Assert.Equal(1, ekuExts.Count);
                Assert.False(ekuExts[0].Critical, "ekuExts[0].Critical");
            }

            CustomBuild_CertMismatch(
                loader,
                referenceTime,
                SigningCertificateOption.ValidHashNoName,
                SigningCertificateOption.Omit,
                includeOption: includeOption);
        }

        [Theory]
        [InlineData(X509IncludeOption.WholeChain)]
        [InlineData(X509IncludeOption.None)]
        public static void NoTsaEku(X509IncludeOption includeOption)
        {
            CertLoader loader = Certificates.TlsClientServerCert;
            DateTimeOffset referenceTime;

            using (X509Certificate2 cert = loader.GetCertificate())
            {
                referenceTime = cert.NotAfter.AddDays(-1);
            }

            CustomBuild_CertMismatch(
                loader,
                referenceTime,
                SigningCertificateOption.ValidHashNoName,
                SigningCertificateOption.Omit,
                includeOption: includeOption);
        }

        private static void CustomBuild_CertMatch(
            CertLoader loader,
            DateTimeOffset referenceTime,
            SigningCertificateOption v1Option,
            SigningCertificateOption v2Option,
            HashAlgorithmName v2AlgorithmName = default,
            X509IncludeOption includeOption = default,
            SubjectIdentifierType identifierType = SubjectIdentifierType.IssuerAndSerialNumber)
        {
            byte[] tokenBytes = BuildCustomToken(
                loader,
                referenceTime,
                v1Option,
                v2Option,
                v2AlgorithmName,
                includeOption,
                identifierType);

            Rfc3161TimestampToken token;
            Assert.True(Rfc3161TimestampToken.TryDecode(tokenBytes, out token, out int bytesRead));

            Assert.Equal(tokenBytes.Length, bytesRead);
            Assert.NotNull(token);

            Assert.Equal(referenceTime, token.TokenInfo.Timestamp);

            using (X509Certificate2 cert = Certificates.ValidLookingTsaCert.GetCertificate())
            {
                Assert.True(
                    token.VerifySignatureForHash(
                        token.TokenInfo.GetMessageHash().Span,
                        token.TokenInfo.HashAlgorithmId,
                        out X509Certificate2 signer,
                        new X509Certificate2Collection(cert)));

                Assert.Equal(cert, signer);
            }
        }

        private static void CustomBuild_CertMismatch(
            CertLoader loader,
            DateTimeOffset referenceTime,
            SigningCertificateOption v1Option,
            SigningCertificateOption v2Option,
            HashAlgorithmName v2AlgorithmName = default,
            X509IncludeOption includeOption = default,
            SubjectIdentifierType identifierType = SubjectIdentifierType.IssuerAndSerialNumber)
        {
            byte[] tokenBytes = BuildCustomToken(
                loader,
                referenceTime,
                v1Option,
                v2Option,
                v2AlgorithmName,
                includeOption,
                identifierType);

            Rfc3161TimestampToken token;

            bool willParse = includeOption == X509IncludeOption.None;

            if (willParse && identifierType == SubjectIdentifierType.IssuerAndSerialNumber)
            {
                // Because IASN matches against the ESSCertId(V2) directly it will reject the token.

                switch (v1Option)
                {
                    case SigningCertificateOption.ValidHashWithInvalidName:
                    case SigningCertificateOption.ValidHashWithInvalidSerial:
                    case SigningCertificateOption.InvalidHashWithInvalidName:
                    case SigningCertificateOption.InvalidHashWithInvalidSerial:
                        willParse = false;
                        break;
                }

                switch (v2Option)
                {
                    case SigningCertificateOption.ValidHashWithInvalidName:
                    case SigningCertificateOption.ValidHashWithInvalidSerial:
                    case SigningCertificateOption.InvalidHashWithInvalidName:
                    case SigningCertificateOption.InvalidHashWithInvalidSerial:
                        willParse = false;
                        break;
                }
            }

            if (willParse)
            {
                Assert.True(Rfc3161TimestampToken.TryDecode(tokenBytes, out token, out int bytesRead));
                Assert.NotNull(token);
                Assert.Equal(tokenBytes.Length, bytesRead);

                using (X509Certificate2 cert = loader.GetCertificate())
                {
                    Assert.False(
                        token.VerifySignatureForHash(
                            token.TokenInfo.GetMessageHash().Span,
                            token.TokenInfo.HashAlgorithmId,
                            out X509Certificate2 signer,
                            new X509Certificate2Collection(cert)));

                    Assert.Null(signer);
                }
            }
            else
            {
                Assert.False(Rfc3161TimestampToken.TryDecode(tokenBytes, out token, out int bytesRead));

                Assert.Null(token);
                Assert.Equal(0, bytesRead);
            }
        }

        private static byte[] BuildCustomToken(
            CertLoader cert,
            DateTimeOffset timestamp,
            SigningCertificateOption v1Option,
            SigningCertificateOption v2Option,
            HashAlgorithmName v2DigestAlg=default,
            X509IncludeOption includeOption=X509IncludeOption.ExcludeRoot,
            SubjectIdentifierType identifierType=SubjectIdentifierType.IssuerAndSerialNumber)
        {
            long accuracyMicroSeconds = (long)(TimeSpan.FromMinutes(1).TotalMilliseconds * 1000);

            byte[] serialNumber = BitConverter.GetBytes(DateTimeOffset.UtcNow.Ticks);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(serialNumber);
            }

            Rfc3161TimestampTokenInfo info = new Rfc3161TimestampTokenInfo(
                new Oid("0.0", "0.0"),
                new Oid(Oids.Sha384),
                new byte[384 / 8],
                serialNumber,
                timestamp,
                accuracyMicroSeconds,
                isOrdering: true);

            ContentInfo contentInfo = new ContentInfo(new Oid(Oids.TstInfo, Oids.TstInfo), info.Encode());
            SignedCms cms = new SignedCms(contentInfo);

            using (X509Certificate2 tsaCert = cert.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(identifierType, tsaCert)
                {
                    IncludeOption = includeOption
                };

                if (v1Option != SigningCertificateOption.Omit)
                {
                    ExpandOption(v1Option, out bool validHash, out bool skipIssuerSerial, out bool validName, out bool validSerial);

                    // simple SigningCertificate
                    byte[] signingCertificateV1Bytes =
                        "301A3018301604140000000000000000000000000000000000000000".HexToByteArray();

                    if (validHash)
                    {
                        byte[] hash = SHA1.HashData(tsaCert.RawData);

                        Buffer.BlockCopy(
                            hash,
                            0,
                            signingCertificateV1Bytes,
                            signingCertificateV1Bytes.Length - hash.Length,
                            hash.Length);
                    }

                    if (!skipIssuerSerial)
                    {
                        byte[] footer = BuildIssuerAndSerialNumber(tsaCert, validName, validSerial);

                        signingCertificateV1Bytes[1] += (byte)footer.Length;
                        signingCertificateV1Bytes[3] += (byte)footer.Length;
                        signingCertificateV1Bytes[5] += (byte)footer.Length;

                        Assert.InRange(signingCertificateV1Bytes[1], 0, 127);

                        signingCertificateV1Bytes = signingCertificateV1Bytes.Concat(footer).ToArray();
                    }

                    signer.SignedAttributes.Add(
                        new AsnEncodedData("1.2.840.113549.1.9.16.2.12", signingCertificateV1Bytes));
                }

                if (v2Option != SigningCertificateOption.Omit)
                {
                    byte[] attrBytes;
                    byte[] algBytes = Array.Empty<byte>();
                    byte[] hashBytes;
                    byte[] issuerNameBytes = Array.Empty<byte>();

                    if (v2DigestAlg != default)
                    {
                        switch (v2DigestAlg.Name)
                        {
                            case "MD5":
                                algBytes = "300C06082A864886F70D02050500".HexToByteArray();
                                break;
                            case "SHA1":
                                algBytes = "300906052B0E03021A0500".HexToByteArray();
                                break;
                            case "SHA256":
                                // Invalid under DER, because it's the default.
                                algBytes = "300D06096086480165030402010500".HexToByteArray();
                                break;
                            case "SHA384":
                                algBytes = "300D06096086480165030402020500".HexToByteArray();
                                break;
                            case "SHA512":
                                algBytes = "300D06096086480165030402030500".HexToByteArray();
                                break;
                            default:
                                throw new NotSupportedException(v2DigestAlg.Name);
                        }
                    }
                    else
                    {
                        v2DigestAlg = HashAlgorithmName.SHA256;
                    }

                    hashBytes = tsaCert.GetCertHash(v2DigestAlg);

                    ExpandOption(v2Option, out bool validHash, out bool skipIssuerSerial, out bool validName, out bool validSerial);

                    if (!validHash)
                    {
                        hashBytes[0] ^= 0xFF;
                    }

                    if (!skipIssuerSerial)
                    {
                        issuerNameBytes = BuildIssuerAndSerialNumber(tsaCert, validName, validSerial);
                    }

                    // hashBytes hasn't been wrapped in an OCTET STRING yet, so add 2 more.
                    int payloadSize = algBytes.Length + hashBytes.Length + issuerNameBytes.Length + 2;
                    Assert.InRange(payloadSize, 0, 123);

                    attrBytes = new byte[payloadSize + 6];

                    int index = 0;

                    // SEQUENCE (SigningCertificateV2)
                    attrBytes[index++] = 0x30;
                    attrBytes[index++] = (byte)(payloadSize + 4);

                    // SEQUENCE OF => certs
                    attrBytes[index++] = 0x30;
                    attrBytes[index++] = (byte)(payloadSize + 2);

                    // SEQUENCE (ESSCertIdV2)
                    attrBytes[index++] = 0x30;
                    attrBytes[index++] = (byte)payloadSize;

                    Buffer.BlockCopy(algBytes, 0, attrBytes, index, algBytes.Length);
                    index += algBytes.Length;

                    // OCTET STRING (Hash)
                    attrBytes[index++] = 0x04;
                    attrBytes[index++] = (byte)hashBytes.Length;
                    Buffer.BlockCopy(hashBytes, 0, attrBytes, index, hashBytes.Length);
                    index += hashBytes.Length;

                    Buffer.BlockCopy(issuerNameBytes, 0, attrBytes, index, issuerNameBytes.Length);

                    signer.SignedAttributes.Add(
                        new AsnEncodedData("1.2.840.113549.1.9.16.2.47", attrBytes));
                }

                cms.ComputeSignature(signer);
            }

            return cms.Encode();
        }

        private static byte[] BuildIssuerAndSerialNumber(X509Certificate2 tsaCert, bool validName, bool validSerial)
        {
            byte[] issuerNameBytes;

            if (validName)
            {
                issuerNameBytes = tsaCert.IssuerName.RawData;
            }
            else
            {
                issuerNameBytes = new X500DistinguishedName("CN=No Match").RawData;
            }

            byte[] serialBytes = tsaCert.GetSerialNumber();

            if (validSerial)
            {
                Array.Reverse(serialBytes);
            }
            else
            {
                // If the byte sequence was a palindrome it's still a match,
                // so flip some bits.
                serialBytes[0] ^= 0x7F;
            }

            if (issuerNameBytes.Length + serialBytes.Length > 80)
            {
                throw new NotSupportedException(
                    "Issuer name and serial length are bigger than this code can handle");
            }

            // SEQUENCE
            //   SEQUENCE
            //     CONTEXT-SPECIFIC 4
            //       [IssuerName]
            //   INTEGER
            //     [SerialNumber, big endian]

            byte[] issuerAndSerialNumber = new byte[issuerNameBytes.Length + serialBytes.Length + 8];
            issuerAndSerialNumber[0] = 0x30;
            issuerAndSerialNumber[1] = (byte)(issuerAndSerialNumber.Length - 2);

            issuerAndSerialNumber[2] = 0x30;
            issuerAndSerialNumber[3] = (byte)(issuerNameBytes.Length + 2);

            issuerAndSerialNumber[4] = 0xA4;
            issuerAndSerialNumber[5] = (byte)(issuerNameBytes.Length);
            Buffer.BlockCopy(issuerNameBytes, 0, issuerAndSerialNumber, 6, issuerNameBytes.Length);

            issuerAndSerialNumber[issuerNameBytes.Length + 6] = 0x02;
            issuerAndSerialNumber[issuerNameBytes.Length + 7] = (byte)serialBytes.Length;
            Buffer.BlockCopy(serialBytes, 0, issuerAndSerialNumber, issuerNameBytes.Length + 8, serialBytes.Length);

            return issuerAndSerialNumber;
        }

        private static void ExpandOption(
            SigningCertificateOption option,
            out bool validHash,
            out bool skipIssuerSerial,
            out bool validName,
            out bool validSerial)
        {
            Assert.NotEqual(SigningCertificateOption.Omit, option);

            validHash = option < SigningCertificateOption.InvalidHashNoName;

            skipIssuerSerial =
                option == SigningCertificateOption.ValidHashNoName ||
                option == SigningCertificateOption.InvalidHashNoName;

            if (skipIssuerSerial)
            {
                validName = validSerial = false;
            }
            else
            {
                validName =
                    option == SigningCertificateOption.ValidHashWithName ||
                    option == SigningCertificateOption.InvalidHashWithName ||
                    option == SigningCertificateOption.ValidHashWithInvalidSerial ||
                    option == SigningCertificateOption.InvalidHashWithInvalidSerial;

                validSerial =
                    option == SigningCertificateOption.ValidHashWithName ||
                    option == SigningCertificateOption.InvalidHashWithName ||
                    option == SigningCertificateOption.ValidHashWithInvalidName ||
                    option == SigningCertificateOption.InvalidHashWithInvalidName;
            }
        }

        public enum SigningCertificateOption
        {
            Omit,
            ValidHashNoName,
            ValidHashWithName,
            ValidHashWithInvalidName,
            ValidHashWithInvalidSerial,
            InvalidHashNoName,
            InvalidHashWithName,
            InvalidHashWithInvalidName,
            InvalidHashWithInvalidSerial,
        }
    }
}
