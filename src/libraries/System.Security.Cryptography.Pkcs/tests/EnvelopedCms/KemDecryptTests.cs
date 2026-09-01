// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using System.Security.Cryptography.X509Certificates;

using Xunit;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    [PlatformSpecific(~TestPlatforms.Windows)]
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public static class KemDecryptTests
    {
        public static TheoryData<byte[]> AesKeyWrapDocuments { get; } = new TheoryData<byte[]>
        {
            KemTestDocuments.MlKem768Aes128Wrap,
            KemTestDocuments.MlKem768Aes192Wrap,
            KemTestDocuments.MlKem768,
        };

        public static TheoryData<byte[]> HkdfDocuments { get; } = new TheoryData<byte[]>
        {
            KemTestDocuments.MlKem768HkdfSha256,
            KemTestDocuments.MlKem768,
            KemTestDocuments.MlKem768HkdfSha512,
            KemTestDocuments.MlKem768HkdfSha3_256,
            KemTestDocuments.MlKem768HkdfSha3_384,
            KemTestDocuments.MlKem768HkdfSha3_512,
        };

        public static TheoryData<byte[], byte[]> MlKemParameterSetDocuments { get; } = new TheoryData<byte[], byte[]>
        {
            { KemTestDocuments.MlKem512, MLKemTestData.IetfMlKem512PrivateKeySeed },
            { KemTestDocuments.MlKem768, MLKemTestData.IetfMlKem768PrivateKeySeed },
            { KemTestDocuments.MlKem1024, MLKemTestData.IetfMlKem1024PrivateKeySeed },
        };

        public static TheoryData<byte[], byte[]?> UkmDocuments { get; } = new TheoryData<byte[], byte[]?>
        {
            { KemTestDocuments.MlKem768, null },
            { KemTestDocuments.MlKem768EmptyUkm, [] },
            { KemTestDocuments.MlKem768NonEmptyUkm, [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08] },
        };

        [Theory]
        [MemberData(nameof(AesKeyWrapDocuments))]
        public static void DecryptAesKeyWrapAlgorithm(byte[] encodedMessage)
        {
            Decrypt(encodedMessage, MLKemTestData.IetfMlKem768PrivateKeySeed);
        }

        [Theory]
        [MemberData(nameof(HkdfDocuments))]
        public static void DecryptHkdfAlgorithm(byte[] encodedMessage)
        {
            Decrypt(encodedMessage, MLKemTestData.IetfMlKem768PrivateKeySeed);
        }

        [Theory]
        [MemberData(nameof(MlKemParameterSetDocuments))]
        public static void DecryptMlKemParameterSet(byte[] encodedMessage, byte[] privateKey)
        {
            Decrypt(encodedMessage, privateKey);
        }

        [Theory]
        [MemberData(nameof(UkmDocuments))]
        public static void DecryptUserKeyingMaterial(byte[] encodedMessage, byte[]? expectedUkm)
        {
            EnvelopedCms cms = Decrypt(encodedMessage, MLKemTestData.IetfMlKem768PrivateKeySeed);
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));
            ReadOnlyMemory<byte>? actualUkm = recipientInfo.UserKeyingMaterial;

            if (expectedUkm is null)
            {
                Assert.Null(actualUkm);
            }
            else
            {
                Assert.True(actualUkm.HasValue);
                Assert.Equal<byte>(expectedUkm, actualUkm.Value.ToArray());
            }
        }

        [Fact]
        public static void DecryptWithCertificatePrivateKey()
        {
            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem,
                MLKemTestData.IetfMlKem768PrivateKeySeedPem))
            {
                EnvelopedCms cms = new EnvelopedCms();
                cms.Decode(KemTestDocuments.MlKem768);
                cms.Decrypt(new X509Certificate2Collection(certificate));

                Assert.Equal("hello world!"u8.ToArray(), cms.ContentInfo.Content);
            }
        }

        [Fact]
        public static void DecryptCompositeMLKemNotSupported()
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(KemTestDocuments.MlKem768);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (TestCompositeMLKem key = new TestCompositeMLKem(CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048))
            {
                Assert.Throws<PlatformNotSupportedException>(() => cms.Decrypt(recipientInfo, key));
            }
        }

        [Fact]
        public static void DecryptInvalidVersion()
        {
            const string Document = """
                MIIFRgYJKoZIhvcNAQcDoIIFNzCCBTMCAQMxggTupIIE6gYLKoZIhvcNAQkQDQMwggTZAgEBMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDALBglghkgBZQMEAS0EKBN/DHlGK/WvNzQ4kolGSwZMU/mRipHkscRAfKSA/zSY3tJMuCsyUE8wPAYJKoZIhvcNAQcB
                MB0GCWCGSAFlAwQBKgQQRxfJmVCr9TJ5HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptInvalidKemCiphertextLength()
        {
            const string Document = """
                MIIFRgYJKoZIhvcNAQcDoIIFNzCCBTMCAQMxggTupIIE6gYLKoZIhvcNAQkQDQMwggTZAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAwSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDALBglghkgBZQMEAS0EKBN/DHlGK/WvNzQ4kolGSwZMU/mRipHkscRAfKSA/zSY3tJMuCsyUE8wPAYJKoZIhvcNAQcB
                MB0GCWCGSAFlAwQBKgQQRxfJmVCr9TJ5HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem1024);
        }

        [Fact]
        public static void DecryptInvalidAesKeyWrapLength()
        {
            const string Document = """
                MIIFNQYJKoZIhvcNAQcDoIIFJjCCBSICAQMxggTdpIIE2QYLKoZIhvcNAQkQDQMwggTIAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDALBglghkgBZQMEAS0EFwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMDwGCSqGSIb3DQEHATAdBglghkgBZQMEASoEEEcX
                yZlQq/UyeR6qHGlc3UqAEHcofbY5qy0dTNpv0AM2K+8=
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptAesKeyWrapOidDoesNotMatchKekLength()
        {
            const string Document = """
                MIIFRgYJKoZIhvcNAQcDoIIFNzCCBTMCAQMxggTupIIE6gYLKoZIhvcNAQkQDQMwggTZAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDALBglghkgBZQMEAQUEKBN/DHlGK/WvNzQ4kolGSwZMU/mRipHkscRAfKSA/zSY3tJMuCsyUE8wPAYJKoZIhvcNAQcB
                MB0GCWCGSAFlAwQBKgQQRxfJmVCr9TJ5HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptUnknownKdf()
        {
            const string Document = """
                MIIFPgYJKoZIhvcNAQcDoIIFLzCCBSsCAQMxggTmpIIE4gYLKoZIhvcNAQkQDQMwggTRAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTAFBgMqAwQCASAwCwYJYIZIAWUDBAEtBCgTfwx5Riv1rzc0OJKJRksGTFP5kYqR5LHEQHykgP80mN7STLgrMlBPMDwGCSqGSIb3DQEHATAdBglghkgB
                ZQMEASoEEEcXyZlQq/UyeR6qHGlc3UqAEHcofbY5qy0dTNpv0AM2K+8=
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptUnknownKem()
        {
            const string Document = """
                MIIFQAYJKoZIhvcNAQcDoIIFMTCCBS0CAQMxggTopIIE5AYLKoZIhvcNAQkQDQMwggTTAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAUGAyoDBQSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ1hOmItZ4
                LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8bCmLAgnA
                jFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvBqT8mOyQp
                RBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmOoygf/Xly
                xQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4nCb8BAlC
                g+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3x6QHRwQo
                jjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4MRsHgCaAN
                Cw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJeWMI4cYuV
                y+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo2FW56NPJ
                nWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRInueHqOOzD
                b9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZPXFeomCC0
                0O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNBwRx1xp1g
                Y0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dYoTANBgsq
                hkiG9w0BCRADHQIBIDALBglghkgBZQMEAS0EKBN/DHlGK/WvNzQ4kolGSwZMU/mRipHkscRAfKSA/zSY3tJMuCsyUE8wPAYJKoZIhvcNAQcBMB0GCWCG
                SAFlAwQBKgQQRxfJmVCr9TJ5HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptKemAlgorithmParameters()
        {
            const string Document = """
                MIIFSAYJKoZIhvcNAQcDoIIFOTCCBTUCAQMxggTwpIIE7AYLKoZIhvcNAQkQDQMwggTbAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMA0GCWCGSAFlAwQEAgUABIIEQPwlsWLBJ5hc9nmSoo0id+KiF8wGPJQubX6GUiNM32orh2C8ilD6MB2s
                d0nWE6Yi1ngu8YXr8/6jGrf4VJlJpt/AFW7AYxL9iJTLE9YE7RSapZOGuzjaL4gZTSGvnKAeRvoAak+b8h0OLjxO0zU80E+XVAEFNozZMDFvc2fbVrQS
                0HxsKYsCCcCMULvZgHiSn74fBNrInqe66Kb3hWa4+484CkTAhWEMQIilOgXn8z9rvHfSq2ABcLXNwewKJ2TPM756Sq3yVY/OBS+02YMka7aRRlqNk6oZ
                i8GpPyY7JClEGghJQWH4zlgQTYMxbKtFPimO+9QDmXCsd5IN0+JkBnZBo5YTXbidF/6AkbwMbbMmqqZo/hOxw9v5uz6/7TxB/Ro2NnCksboOqxTLTrpY
                yY6jKB/9eXLFAVcnofECyDRygP+28TutH0vROISQ+OSw8fHOhx/FAra0PbaWIy96ZHaA7SpWOkhkCUjOr2In0n6yrmVahpEdayf5svOkGJO6UsTvjXMc
                nLicJvwECUKD4k6uHnRlCmOkdygnhfg10u5AM4Kna60afHFAjjdyr6YsVissf2960KS1G3IojmIOwYkxWR4jNSaH4ge6dVwTWdOaKJMiR5ttUaQYvK/5
                cjfHpAdHBCiOMx6v9uGhPyvXb0Zg0w4BDNCbcXCk0ssHOsiPvQ2bZytree0v9oww9EjcIaWExzjTmvevW1ux9VyCpo2xYmyPY9xQMsUqdjDVzIS9RnXI
                XgxGweAJoA0LD3NhRCPFWd955ddznSGo7MAP9lq7NVQB0FFGlMHURwOcjhFEWW6SHHt9e+jZZRbLS8R2PV8zD5smEVNFAjnhl3Q5L20Tx+bkri05pBO0
                Il5Ywjhxi5XL7z5kWvDVgbO1GCr+6Z/vAE7fMZ0BioSGrduDyWfyOKkwPjBOZByaQYvVHVDF4lJdAI1l48rAgxoYamIpMhmsQcBKR/1dxtO1wvT0UsqM
                fOjYVbno08mdaDVxN0YIdP1UFxPTpV3rObLeqMYJLt+JJmzXuWrhTJz+U9hzH7QTUInjMxwCD36MQJXlxJRn4d5MqmZs86Aveuf2hY4aLS9dwnx20h8x
                Eie54eo47MNv0VukwUNnUBkxsa4KHHFm2NUz2LDTyZz9YgXapr21UiQQpfxlAO4jPx74LUdNrAQVhnVcEGoxPLQF9ilmFPVb9Ql7muBQM0ldcG/kt09a
                Fk9cV6iYILTQ7RuqKhRUeqTMHubRW00eScRqvaSzmnOKC+jUDB312yB17VThmlEIgwBHtEPjytzy9M1I0Xkt+8NKMntVklESmGiDf1mZSFS4yXehxM5d
                00HBHHXGnWBjQ46AFeS7UNEU2SYmb45fiA73Pe957QDiUc8/O37AMbwXyawuz4UY8NadQ/VNZXIctZYhPVVn8Cu8Q/l4zuPRcf0JgiX/hnWdxfrQm247
                51ihMA0GCyqGSIb3DQEJEAMdAgEgMAsGCWCGSAFlAwQBLQQoE38MeUYr9a83NDiSiUZLBkxT+ZGKkeSxxEB8pID/NJje0ky4KzJQTzA8BgkqhkiG9w0B
                BwEwHQYJYIZIAWUDBAEqBBBHF8mZUKv1MnkeqhxpXN1KgBB3KH22OastHUzab9ADNivv
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptKdfAlgorithmParameters()
        {
            const string Document = """
                MIIFSAYJKoZIhvcNAQcDoIIFOTCCBTUCAQMxggTwpIIE7AYLKoZIhvcNAQkQDQMwggTbAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTAPBgsqhkiG9w0BCRADHQUAAgEgMAsGCWCGSAFlAwQBLQQoE38MeUYr9a83NDiSiUZLBkxT+ZGKkeSxxEB8pID/NJje0ky4KzJQTzA8BgkqhkiG9w0B
                BwEwHQYJYIZIAWUDBAEqBBBHF8mZUKv1MnkeqhxpXN1KgBB3KH22OastHUzab9ADNivv
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptAesKeyWrapAlgorithmParameters()
        {
            const string Document = """
                MIIFSAYJKoZIhvcNAQcDoIIFOTCCBTUCAQMxggTwpIIE7AYLKoZIhvcNAQkQDQMwggTbAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDANBglghkgBZQMEAS0FAAQoE38MeUYr9a83NDiSiUZLBkxT+ZGKkeSxxEB8pID/NJje0ky4KzJQTzA8BgkqhkiG9w0B
                BwEwHQYJYIZIAWUDBAEqBBBHF8mZUKv1MnkeqhxpXN1KgBB3KH22OastHUzab9ADNivv
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptUnknownAesKeyWrap()
        {
            const string Document = """
                MIIFQAYJKoZIhvcNAQcDoIIFMTCCBS0CAQMxggTopIIE5AYLKoZIhvcNAQkQDQMwggTTAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDAFBgMqAwYEKBN/DHlGK/WvNzQ4kolGSwZMU/mRipHkscRAfKSA/zSY3tJMuCsyUE8wPAYJKoZIhvcNAQcBMB0GCWCG
                SAFlAwQBKgQQRxfJmVCr9TJ5HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptKemAlgorithmDoesNotMatchPrivateKey()
        {
            AssertInvalidDocument(KemTestDocuments.MlKem768, MLKemAlgorithm.MLKem512);
        }

        private static void AssertInvalidDocument(string document, MLKemAlgorithm algorithm)
        {
            AssertInvalidDocument(Convert.FromBase64String(document), algorithm);
        }

        private static void AssertInvalidDocument(byte[] document, MLKemAlgorithm algorithm)
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(document);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (ValidationMLKem privateKey = new ValidationMLKem(algorithm))
            {
                Assert.Throws<CryptographicException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        private static EnvelopedCms Decrypt(byte[] encodedMessage, byte[] privateKey)
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(encodedMessage);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (MLKem mlKem = MLKem.ImportPkcs8PrivateKey(privateKey))
            {
                cms.Decrypt(recipientInfo, mlKem);
            }

            Assert.Equal("hello world!"u8.ToArray(), cms.ContentInfo.Content);
            return cms;
        }

        private sealed class ValidationMLKem : MLKem
        {
            internal ValidationMLKem(MLKemAlgorithm algorithm)
                : base(algorithm)
            {
            }

            protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
                Assert.Fail("Decapsulation should not be attempted.");

            protected override void Dispose(bool disposing)
            {
            }

            protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret) =>
                throw new NotSupportedException();

            protected override void ExportDecapsulationKeyCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override void ExportEncapsulationKeyCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override void ExportPrivateSeedCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
                throw new NotSupportedException();
        }

        private sealed class TestCompositeMLKem : CompositeMLKem
        {
            internal TestCompositeMLKem(CompositeMLKemAlgorithm algorithm)
                : base(algorithm)
            {
            }

            protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
                throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
            }

            protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret) =>
                throw new NotSupportedException();

            protected override int ExportDecapsulationKeyCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override int ExportEncapsulationKeyCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
                throw new NotSupportedException();
        }
    }
}
