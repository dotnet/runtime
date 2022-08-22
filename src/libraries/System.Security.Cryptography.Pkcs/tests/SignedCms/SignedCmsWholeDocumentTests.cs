// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static class SignedCmsWholeDocumentTests
    {
        [Theory]
#if !NETFRAMEWORK
        [InlineData(true)]
#endif
        [InlineData(false)]
        public static void ReadRsaPssDocument(bool fromSpan)
        {
            SignedCms cms = new SignedCms();
            if (fromSpan)
            {
#if !NETFRAMEWORK
                cms.Decode(SignedDocuments.RsaPssDocument.AsSpan());
#else
                throw new Xunit.Sdk.XunitException(
                    "This test should not evaluate for .NET Framework, the API is missing.");
#endif
            }
            else
            {
                cms.Decode(SignedDocuments.RsaPssDocument);
            }

            Assert.Equal(3, cms.Version);

            ContentInfo contentInfo = cms.ContentInfo;

            Assert.Equal("1.2.840.113549.1.7.1", contentInfo.ContentType.Value);
            Assert.Equal("54686973206973206120746573740D0A", contentInfo.Content.ByteArrayToHex());

            X509Certificate2Collection certs = cms.Certificates;
            Assert.Single(certs);

            X509Certificate2 topLevelCert = certs[0];
            Assert.Equal("localhost", topLevelCert.GetNameInfo(X509NameType.SimpleName, false));

            Assert.Equal(
                new DateTimeOffset(2016, 3, 2, 2, 37, 54, TimeSpan.Zero),
                new DateTimeOffset(topLevelCert.NotBefore));

            Assert.Equal(
                new DateTimeOffset(2017, 3, 2, 2, 37, 54, TimeSpan.Zero),
                new DateTimeOffset(topLevelCert.NotAfter));

            SignerInfoCollection signers = cms.SignerInfos;
            Assert.Single(signers);

            SignerInfo signer = signers[0];
            Assert.Equal(3, signer.Version);
            Assert.Equal(SubjectIdentifierType.SubjectKeyIdentifier, signer.SignerIdentifier.Type);
            Assert.Equal("1063CAB14FB14C47DC211C0E0285F3EE5946BF2D", signer.SignerIdentifier.Value);
            Assert.Equal("2.16.840.1.101.3.4.2.1", signer.DigestAlgorithm.Value);
#if NETCOREAPP
            Assert.Equal("1.2.840.113549.1.1.10", signer.SignatureAlgorithm.Value);
#endif

            CryptographicAttributeObjectCollection signedAttrs = signer.SignedAttributes;
            Assert.Equal(4, signedAttrs.Count);

            Assert.Equal("1.2.840.113549.1.9.3", signedAttrs[0].Oid.Value);
            Assert.Equal("1.2.840.113549.1.9.5", signedAttrs[1].Oid.Value);
            Assert.Equal("1.2.840.113549.1.9.4", signedAttrs[2].Oid.Value);
            Assert.Equal("1.2.840.113549.1.9.15", signedAttrs[3].Oid.Value);

            Assert.Equal(1, signedAttrs[0].Values.Count);
            Assert.Equal(1, signedAttrs[1].Values.Count);
            Assert.Equal(1, signedAttrs[2].Values.Count);
            Assert.Equal(1, signedAttrs[3].Values.Count);

            Pkcs9ContentType contentTypeAttr = (Pkcs9ContentType)signedAttrs[0].Values[0];
            Assert.Equal("1.2.840.113549.1.7.1", contentTypeAttr.ContentType.Value);

            Pkcs9SigningTime signingTimeAttr = (Pkcs9SigningTime)signedAttrs[1].Values[0];
            Assert.Equal(
                new DateTimeOffset(2017, 10, 26, 1, 6, 25, TimeSpan.Zero),
                new DateTimeOffset(signingTimeAttr.SigningTime));

            Pkcs9MessageDigest messageDigestAttr = (Pkcs9MessageDigest)signedAttrs[2].Values[0];
            Assert.Equal(
                "07849DC26FCBB2F3BD5F57BDF214BAE374575F1BD4E6816482324799417CB379",
                messageDigestAttr.MessageDigest.ByteArrayToHex());

            Assert.IsType<Pkcs9AttributeObject>(signedAttrs[3].Values[0]);
#if !NETCOREAPP
            Assert.NotSame(signedAttrs[3].Oid, signedAttrs[3].Values[0].Oid);
#endif
            Assert.Equal(
                "306A300B060960864801650304012A300B0609608648016503040116300B0609" +
                    "608648016503040102300A06082A864886F70D0307300E06082A864886F70D03" +
                    "0202020080300D06082A864886F70D0302020140300706052B0E030207300D06" +
                    "082A864886F70D0302020128",
                signedAttrs[3].Values[0].RawData.ByteArrayToHex());

#if NETCOREAPP
            Assert.Equal(
                "B93E81D141B3C9F159AB0021910635DC72E8E860BE43C28E5D53243D6DC247B7" +
                    "D4F18C20195E80DEDCC75B29C43CE5047AD775B65BFC93589BD748B950C68BAD" +
                    "DF1A4673130302BBDA8667D5DDE5EA91ECCB13A9B4C04F1C4842FEB1697B7669" +
                    "C7692DD3BDAE13B5AA8EE3EB5679F3729D1DC4F2EB9DC89B7E8773F2F8C6108C" +
                    "05",
                signer.GetSignature().ByteArrayToHex());
#endif

            CryptographicAttributeObjectCollection unsignedAttrs = signer.UnsignedAttributes;
            Assert.Empty(unsignedAttrs);

            SignerInfoCollection counterSigners = signer.CounterSignerInfos;
            Assert.Empty(counterSigners);

            X509Certificate2 signerCertificate = signer.Certificate;
            Assert.Equal(
                "CN=localhost, OU=.NET Framework, O=Microsoft Corp., L=Redmond, S=Washington, C=US",
                signerCertificate.SubjectName.Name);

            // CheckHash always throws for certificate-based signers.
            Assert.Throws<CryptographicException>(() => signer.CheckHash());

            // At this time we cannot support the PSS parameters for this document.
            Assert.Throws<CryptographicException>(() => signer.CheckSignature(true));

            // Since there are no NoSignature signers the document CheckHash will succeed.
            // Assert.NotThrows
            cms.CheckHash();

            // Since at least one signer fails, the document signature will fail
            Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void ReadRsaPkcs1SimpleDocument()
        {
            SignedCms cms = new SignedCms();

            cms.Decode(SignedDocuments.RsaPkcs1OneSignerIssuerAndSerialNumber);

            Assert.Equal(1, cms.Version);

            ContentInfo contentInfo = cms.ContentInfo;

            Assert.Equal("1.2.840.113549.1.7.1", contentInfo.ContentType.Value);
            Assert.Equal("4D6963726F736F667420436F72706F726174696F6E", contentInfo.Content.ByteArrayToHex());

            SignerInfoCollection signers = cms.SignerInfos;
            Assert.Single(signers);
            SignerInfo signer = signers[0];

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.GetCertificate())
            {
                X509Certificate2Collection certs = cms.Certificates;

                Assert.Single(certs);

                Assert.Equal(cert, certs[0]);
                Assert.Equal(cert, signer.Certificate);
                Assert.NotSame(certs[0], signer.Certificate);
                Assert.NotSame(cert, signer.Certificate);
                Assert.NotSame(cert, certs[0]);
            }

            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, signer.SignerIdentifier.Type);
            Assert.Equal(Oids.Sha1, signer.DigestAlgorithm.Value);

#if NETCOREAPP
            Assert.Equal(Oids.Rsa, signer.SignatureAlgorithm.Value);

            Assert.Equal(
                "5A1717621D450130B3463662160EEC06F7AE77E017DD95F294E97A0BDD433FE6" +
                    "B2CCB34FAAC33AEA50BFD7D9E78DC7174836284619F744278AE77B8495091E09" +
                    "6EEF682D9CA95F6E81C7DDCEDDA6A12316B453C894B5000701EB09DF57A53B73" +
                    "3A4E80DA27FA710870BD88C86E2FDB9DCA14D18BEB2F0C87E9632ABF02BE2FE3",
                signer.GetSignature().ByteArrayToHex());
#endif

            CryptographicAttributeObjectCollection signedAttrs = signer.SignedAttributes;
            Assert.Empty(signedAttrs);

            CryptographicAttributeObjectCollection unsignedAttrs = signer.UnsignedAttributes;
            Assert.Empty(unsignedAttrs);

            SignerInfoCollection counterSigners = signer.CounterSignerInfos;
            Assert.Empty(counterSigners);

            // Assert.NotThrows
            signer.CheckSignature(true);

            // CheckHash always throws for certificate-based signers.
            Assert.Throws<CryptographicException>(() => signer.CheckHash());

            // Since there are no NoSignature signers the document CheckHash will succeed.
            // Assert.NotThrows
            cms.CheckHash();

            // Since all the signers succeed the document will succeed
            cms.CheckSignature(true);
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void ReadRsaPkcs1CounterSigned()
        {
            SignedCms cms = new SignedCms();

            cms.Decode(SignedDocuments.CounterSignedRsaPkcs1OneSigner);

            Assert.Equal(1, cms.Version);

            ContentInfo contentInfo = cms.ContentInfo;
            Assert.Equal("1.2.840.113549.1.7.1", contentInfo.ContentType.Value);
            Assert.Equal("4D6963726F736F667420436F72706F726174696F6E", contentInfo.Content.ByteArrayToHex());

            SignerInfoCollection signers = cms.SignerInfos;
            Assert.Single(signers);
            SignerInfo signer = signers[0];

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.GetCertificate())
            {
                X509Certificate2Collection certs = cms.Certificates;

                Assert.Single(certs);

                Assert.Equal(cert, certs[0]);
                Assert.Equal(cert, signer.Certificate);
                Assert.NotSame(certs[0], signer.Certificate);
                Assert.NotSame(cert, signer.Certificate);
                Assert.NotSame(cert, certs[0]);
            }

            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, signer.SignerIdentifier.Type);
            Assert.Equal(Oids.Sha1, signer.DigestAlgorithm.Value);

#if NETCOREAPP
            Assert.Equal(Oids.Rsa, signer.SignatureAlgorithm.Value);

            Assert.Equal(
                "5A1717621D450130B3463662160EEC06F7AE77E017DD95F294E97A0BDD433FE6" +
                    "B2CCB34FAAC33AEA50BFD7D9E78DC7174836284619F744278AE77B8495091E09" +
                    "6EEF682D9CA95F6E81C7DDCEDDA6A12316B453C894B5000701EB09DF57A53B73" +
                    "3A4E80DA27FA710870BD88C86E2FDB9DCA14D18BEB2F0C87E9632ABF02BE2FE3",
                signer.GetSignature().ByteArrayToHex());
#endif

            CryptographicAttributeObjectCollection signedAttrs = signer.SignedAttributes;
            Assert.Empty(signedAttrs);

            CryptographicAttributeObjectCollection unsignedAttrs = signer.UnsignedAttributes;
            Assert.Single(unsignedAttrs);

            Assert.Equal(Oids.CounterSigner, unsignedAttrs[0].Oid.Value);

            SignerInfoCollection counterSigners = signer.CounterSignerInfos;
            Assert.Single(counterSigners);
            SignerInfo counterSigner = counterSigners[0];

            Assert.Equal(SubjectIdentifierType.SubjectKeyIdentifier, counterSigner.SignerIdentifier.Type);

            // Not universally true, but is in this case.
            Assert.Equal(signer.Certificate, counterSigner.Certificate);
            Assert.NotSame(signer.Certificate, counterSigner.Certificate);

            // Assert.NotThrows
            signer.CheckSignature(true);

            // Assert.NotThrows
            counterSigner.CheckSignature(true);

            // The document should be validly signed, then.
            // Assert.NotThrows
            cms.CheckSignature(true);

            // If CheckSignature succeeds then CheckHash cannot.
            Assert.Throws<CryptographicException>(() => counterSigner.CheckHash());
            Assert.Throws<CryptographicException>(() => signer.CheckHash());

            // Since there are no NoSignature signers, CheckHash won't throw.
            // Assert.NotThrows
            cms.CheckHash();
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void CheckNoSignatureDocument()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.NoSignatureSignedWithAttributesAndCounterSignature);

            Assert.Equal(1, cms.Version);
            Assert.Equal(Oids.Pkcs7Data, cms.ContentInfo.ContentType.Value);

            Assert.Equal(
                "4D6963726F736F667420436F72706F726174696F6E",
                cms.ContentInfo.Content.ByteArrayToHex());

            X509Certificate2Collection cmsCerts = cms.Certificates;
            Assert.Single(cmsCerts);

            SignerInfoCollection signers = cms.SignerInfos;
            Assert.Single(signers);

            SignerInfo signer = signers[0];

            Assert.Equal(SubjectIdentifierType.NoSignature, signer.SignerIdentifier.Type);
            Assert.Null(signer.SignerIdentifier.Value);
            Assert.Null(signer.Certificate);
            Assert.Equal(Oids.Sha1, signer.DigestAlgorithm.Value);

#if NETCOREAPP
            Assert.Equal("1.3.6.1.5.5.7.6.2", signer.SignatureAlgorithm.Value);

            Assert.Equal(
                "8B70D20D0477A35CD84AB962C10DC52FBA6FAD6B",
                signer.GetSignature().ByteArrayToHex());
#endif

            CryptographicAttributeObjectCollection signedAttrs = signer.SignedAttributes;
            Assert.Equal(3, signedAttrs.Count);

            Assert.Single(signedAttrs[0].Values);
            Assert.Single(signedAttrs[1].Values);
            Assert.Single(signedAttrs[2].Values);

            Pkcs9ContentType contentType = (Pkcs9ContentType)signedAttrs[0].Values[0];
            Pkcs9SigningTime signingTime = (Pkcs9SigningTime)signedAttrs[1].Values[0];
            Pkcs9MessageDigest messageDigest = (Pkcs9MessageDigest)signedAttrs[2].Values[0];

            Assert.Equal(Oids.Pkcs7Data, contentType.ContentType.Value);
            Assert.Equal(
                new DateTimeOffset(2017, 11, 1, 17, 17, 17, TimeSpan.Zero),
                signingTime.SigningTime);

            Assert.Equal(DateTimeKind.Utc, signingTime.SigningTime.Kind);

            using (SHA1 sha1 = SHA1.Create())
            {
                Assert.Equal(
                    sha1.ComputeHash(cms.ContentInfo.Content).ByteArrayToHex(),
                    messageDigest.MessageDigest.ByteArrayToHex());
            }

            CryptographicAttributeObjectCollection unsignedAttrs = signer.UnsignedAttributes;
            Assert.Single(unsignedAttrs);
            // No need to check the contents, it's a CounterSigner (tested next)

            SignerInfoCollection counterSigners = signer.CounterSignerInfos;
            Assert.Single(counterSigners);

            SignerInfo counterSigner = counterSigners[0];
            Assert.Equal(3, counterSigner.Version);
            Assert.Equal(SubjectIdentifierType.SubjectKeyIdentifier, counterSigner.SignerIdentifier.Type);
            Assert.Equal("6B4A6B92FDED07EE0119F3674A96D1A70D2A588D", (string)counterSigner.SignerIdentifier.Value);
            Assert.Equal(Oids.Sha1, counterSigner.DigestAlgorithm.Value);

            CryptographicAttributeObjectCollection csSignedAttrs = counterSigner.SignedAttributes;
            Assert.Equal(2, csSignedAttrs.Count);

            // RFC3369 says that the content-type attribute must not be present for counter-signers, but it is.
            // RFC2630 said that it "is not required".
            Pkcs9ContentType csContentType = (Pkcs9ContentType)csSignedAttrs[0].Values[0];
            Pkcs9MessageDigest csMessageDigest = (Pkcs9MessageDigest)csSignedAttrs[1].Values[0];

            Assert.Equal(Oids.Pkcs7Data, csContentType.ContentType.Value);
            Assert.Equal(
                "833378066BDCCBA7047EF6919843D181A57D6479",
                csMessageDigest.MessageDigest.ByteArrayToHex());

#if NETCOREAPP
            Assert.Equal(Oids.Rsa, counterSigner.SignatureAlgorithm.Value);

            Assert.Equal(
                "2155D226DD744166E582D040E60535210195050EA00F2C179897198521DABD0E" +
                    "6B27750FD8BA5F9AAF58B4863B6226456F38553A22453CAF0A0F106766C7AB6F" +
                    "3D6AFD106753DC50F8A6E4F9E5508426D236C2DBB4BCB8162FA42E995CBA16A3" +
                    "40FD7C793569DF1B71368E68253299BC74E38312B40B8F52EAEDE10DF414A522",
                counterSigner.GetSignature().ByteArrayToHex());
#endif

            using (X509Certificate2 capiCert = Certificates.RSAKeyTransferCapi1.GetCertificate())
            {
                Assert.Equal(capiCert, cmsCerts[0]);
                Assert.Equal(capiCert, counterSigner.Certificate);
            }

            // The counter-signer has a (real) signature, and a certificate, so CheckSignature
            // will pass
            counterSigner.CheckSignature(true);

            // The (primary) signer has a hash-only signature with no certificate,
            // so CheckSignature will fail.
            Assert.Throws<CryptographicException>(() => signer.CheckSignature(true));

            // The (primary) signer is a NoSignature type, so CheckHash will succeed
            signer.CheckHash();

            // The document has a NoSignature signer, so CheckHash will do something (and succeed).
            cms.CheckHash();

            // Since the document's primary signer is NoSignature, CheckSignature will fail
            Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void NonEmbeddedCertificate()
        {
            SignedCms cms = new SignedCms();
            cms.Decode(SignedDocuments.RsaCapiTransfer1_NoEmbeddedCert);

            Assert.Equal(3, cms.Version);
            Assert.Equal(Oids.Pkcs7Data, cms.ContentInfo.ContentType.Value);

            Assert.Equal(
                "4D6963726F736F667420436F72706F726174696F6E",
                cms.ContentInfo.Content.ByteArrayToHex());

            Assert.Empty(cms.Certificates);

            SignerInfoCollection signers = cms.SignerInfos;
            Assert.Single(signers);

            SignerInfo signer = signers[0];
            Assert.Equal(3, signer.Version);
            Assert.Equal(SubjectIdentifierType.SubjectKeyIdentifier, signer.SignerIdentifier.Type);
            Assert.Equal("6B4A6B92FDED07EE0119F3674A96D1A70D2A588D", (string)signer.SignerIdentifier.Value);
            Assert.Equal(Oids.Sha1, signer.DigestAlgorithm.Value);

            CryptographicAttributeObjectCollection signedAttrs = signer.SignedAttributes;
            Assert.Equal(3, signedAttrs.Count);

            Pkcs9ContentType contentType = (Pkcs9ContentType)signedAttrs[0].Values[0];
            Pkcs9SigningTime signingTime = (Pkcs9SigningTime)signedAttrs[1].Values[0];
            Pkcs9MessageDigest messageDigest = (Pkcs9MessageDigest)signedAttrs[2].Values[0];

            Assert.Equal(Oids.Pkcs7Data, contentType.ContentType.Value);
            Assert.Equal(
                new DateTimeOffset(2017, 11, 2, 15, 34, 4, TimeSpan.Zero),
                signingTime.SigningTime);

            Assert.Equal(DateTimeKind.Utc, signingTime.SigningTime.Kind);

            Assert.Empty(signer.UnsignedAttributes);
            Assert.Empty(signer.CounterSignerInfos);
            Assert.Null(signer.Certificate);

#if NETCOREAPP
            Assert.Equal(Oids.Rsa, signer.SignatureAlgorithm.Value);

            Assert.Equal(
                "0EDE3870B8A80B45A21BAEC4681D059B46502E1B1AA6B8920CF50D4D837646A5" +
                    "5559B4C05849126C655D95FF3C6C1B420E07DC42629F294EE69822FEA56F32D4" +
                    "1B824CBB6BF809B7583C27E77B7AC58DFC925B1C60EA4A67AA84D73FC9E9191D" +
                    "33B36645F17FD6748A2D8B12C6C384C3C734D27273386211E4518FE2B4ED0147",
                signer.GetSignature().ByteArrayToHex());
#endif

            using (SHA1 sha1 = SHA1.Create())
            {
                Assert.Equal(
                    sha1.ComputeHash(cms.ContentInfo.Content).ByteArrayToHex(),
                    messageDigest.MessageDigest.ByteArrayToHex());
            }

            // Since it's not NoSignature CheckHash will throw.
            Assert.Throws<CryptographicException>(() => signer.CheckHash());

            // Since there's no matched certificate CheckSignature will throw.
            Assert.Throws<CryptographicException>(() => signer.CheckSignature(true));

            // Since there are no NoSignature signers, SignedCms.CheckHash will succeed
            cms.CheckHash();

            // Since there are no matched certificates SignedCms.CheckSignature will throw.
            Assert.Throws<CryptographicException>(() => cms.CheckSignature(true));

            using (X509Certificate2 wrongCert = Certificates.RSAKeyTransfer1.GetCertificate())
            using (X509Certificate2 rightCert = Certificates.RSAKeyTransferCapi1.GetCertificate())
            {
                X509Certificate2Collection coll = new X509Certificate2Collection(wrongCert);

                // Wrong certificate, still throw.
                Assert.Throws<CryptographicException>(() => signer.CheckSignature(coll, true));
                Assert.Throws<CryptographicException>(() => cms.CheckSignature(coll, true));

                coll = new X509Certificate2Collection(rightCert);

                // Right cert, success
                signer.CheckSignature(coll, true);
                Assert.Null(signer.Certificate);
                cms.CheckSignature(coll, true);
                Assert.Null(cms.SignerInfos[0].Certificate);

                coll.Add(wrongCert);

                // Both right and wrong, success
                signer.CheckSignature(coll, true);
                Assert.Null(signer.Certificate);
                cms.CheckSignature(coll, true);
                Assert.Null(cms.SignerInfos[0].Certificate);

                coll = new X509Certificate2Collection(wrongCert);

                // Just wrong again, no accidental stateful match
                Assert.Throws<CryptographicException>(() => signer.CheckSignature(coll, true));
                Assert.Throws<CryptographicException>(() => cms.CheckSignature(coll, true));

                coll.Add(rightCert);

                // Wrong then right, success
                signer.CheckSignature(coll, true);
                Assert.Null(signer.Certificate);
                cms.CheckSignature(coll, true);
                Assert.Null(cms.SignerInfos[0].Certificate);
            }
        }

        [ConditionalFact(typeof(SignatureSupport), nameof(SignatureSupport.SupportsRsaSha1Signatures))]
        public static void ReadRsaPkcs1DoubleCounterSigned()
        {
            SignedCms cms = new SignedCms();

            cms.Decode(SignedDocuments.OneRsaSignerTwoRsaCounterSigners);

            Assert.Equal(1, cms.Version);

            ContentInfo contentInfo = cms.ContentInfo;
            Assert.Equal("1.2.840.113549.1.7.1", contentInfo.ContentType.Value);
            Assert.Equal("4D6963726F736F667420436F72706F726174696F6E", contentInfo.Content.ByteArrayToHex());

            SignerInfoCollection signers = cms.SignerInfos;
            Assert.Single(signers);
            SignerInfo signer = signers[0];

            using (X509Certificate2 cert = Certificates.RSAKeyTransferCapi1.GetCertificate())
            {
                X509Certificate2Collection certs = cms.Certificates;

                Assert.Equal(2, certs.Count);

                Assert.Equal(cert, certs[1]);
                Assert.Equal(cert, signer.Certificate);
                Assert.NotSame(certs[1], signer.Certificate);
                Assert.NotSame(cert, signer.Certificate);
                Assert.NotSame(cert, certs[1]);
            }

            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, signer.SignerIdentifier.Type);
            Assert.Equal(Oids.Sha1, signer.DigestAlgorithm.Value);

#if NETCOREAPP
            Assert.Equal(Oids.Rsa, signer.SignatureAlgorithm.Value);

            Assert.Equal(
                "5A1717621D450130B3463662160EEC06F7AE77E017DD95F294E97A0BDD433FE6" +
                    "B2CCB34FAAC33AEA50BFD7D9E78DC7174836284619F744278AE77B8495091E09" +
                    "6EEF682D9CA95F6E81C7DDCEDDA6A12316B453C894B5000701EB09DF57A53B73" +
                    "3A4E80DA27FA710870BD88C86E2FDB9DCA14D18BEB2F0C87E9632ABF02BE2FE3",
                signer.GetSignature().ByteArrayToHex());
#endif

            CryptographicAttributeObjectCollection signedAttrs = signer.SignedAttributes;
            Assert.Empty(signedAttrs);

            CryptographicAttributeObjectCollection unsignedAttrs = signer.UnsignedAttributes;
            Assert.Equal(2, unsignedAttrs.Count);

            Assert.Equal(Oids.CounterSigner, unsignedAttrs[0].Oid.Value);

            SignerInfoCollection counterSigners = signer.CounterSignerInfos;
            Assert.Equal(2, counterSigners.Count);
            SignerInfo cs1 = counterSigners[0];
            SignerInfo cs2 = counterSigners[1];

            Assert.Equal(SubjectIdentifierType.SubjectKeyIdentifier, cs1.SignerIdentifier.Type);

            // Not universally true, but is in this case.
            Assert.Equal(signer.Certificate, cs1.Certificate);
            Assert.NotSame(signer.Certificate, cs1.Certificate);

            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, cs2.SignerIdentifier.Type);
            Assert.NotEqual(signer.Certificate, cs2.Certificate);

            // Assert.NotThrows
            signer.CheckSignature(true);

            // Assert.NotThrows
            cs1.CheckSignature(true);

            // Assert.NotThrows
            cs2.CheckSignature(true);

            // The document should be validly signed, then.
            // Assert.NotThrows
            cms.CheckSignature(true);

#if NETCOREAPP
            Assert.Equal(
                "1AA282DBED4D862D7CEA30F803E790BDB0C97EE852778CEEDDCD94BB9304A155" +
                    "2E60A8D36052AC8C2D28755F3B2F473824100AB3A6ABD4C15ABD77E0FFE13D0D" +
                    "F253BCD99C718FA673B6CB0CBBC68CE5A4AC671298C0A07C7223522E0E7FFF15" +
                    "CEDBAB55AAA99588517674671691065EB083FB729D1E9C04B2BF99A9953DAA5E",
                cs2.GetSignature().ByteArrayToHex());
#endif

            // If CheckSignature succeeds then CheckHash cannot.
            Assert.Throws<CryptographicException>(() => cs1.CheckHash());
            Assert.Throws<CryptographicException>(() => cs2.CheckHash());
            Assert.Throws<CryptographicException>(() => signer.CheckHash());

            // Since there are no NoSignature signers, CheckHash won't throw.
            // Assert.NotThrows
            cms.CheckHash();
        }
    }
}
