// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs7;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.Tests;
using System.Security.Cryptography.X509Certificates;

using TestOids = System.Security.Cryptography.Pkcs.Tests.Oids;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    internal static class CompositeMLKemCmsTestData
    {
        private const string IdSmimeOriKem = "1.2.840.113549.1.9.16.13.3";

        internal static CompositeMLKemTestVector X25519Vector { get; } =
            GetVector(CompositeMLKemAlgorithm.MLKem768WithX25519);

        internal static CompositeMLKemTestVector P256Vector { get; } =
            GetVector(CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256);

        internal static byte[] BuildEnvelopedData(
            CompositeMLKemTestVector vector,
            HashAlgorithmName kdfAlgorithm = default,
            string? kdfOid = null,
            string? wrapOid = null,
            int kekLength = 32,
            byte[]? ukm = null,
            int version = 0,
            string? kemOid = null,
            byte[]? kemCiphertext = null,
            int? encryptedKeyLength = null,
            bool includeKemParameters = false,
            bool includeKdfParameters = false,
            bool includeWrapParameters = false)
        {
            kdfAlgorithm = kdfAlgorithm == default ? HashAlgorithmName.SHA384 : kdfAlgorithm;
            kdfOid ??= TestOids.HkdfSha384;
            wrapOid ??= TestOids.Aes256Wrap;

            using (X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(vector.Certificate))
            {
                kemOid ??= certificate.GetKeyAlgorithm();
            }

            byte[] contentEncryptionKey =
            [
                0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,
                0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,
            ];
            byte[] iv =
            [
                0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF,
            ];
            byte[] keyEncryptionKey = new byte[kekLength];
            byte[] encryptedKey;
            AlgorithmIdentifierAsn wrap = CreateAlgorithmIdentifier(wrapOid, includeWrapParameters);
            ReadOnlyMemory<byte>? userKeyingMaterial = null;

            if (ukm is not null)
            {
                userKeyingMaterial = ukm;
            }

            CmsOriForKemOtherInfoAsn kdfInfo = new CmsOriForKemOtherInfoAsn
            {
                Wrap = wrap,
                KekLength = kekLength,
                Ukm = userKeyingMaterial,
            };

            HKDF.DeriveKey(
                kdfAlgorithm,
                vector.SharedSecret,
                keyEncryptionKey,
                salt: [],
                Encode(kdfInfo));

            using (Aes aes = Aes.Create())
            {
                aes.SetKey(keyEncryptionKey);
                encryptedKey = aes.EncryptKeyWrap(contentEncryptionKey);
            }

            if (encryptedKeyLength.HasValue)
            {
                encryptedKey = new byte[encryptedKeyLength.Value];
            }

            byte[] encryptedContent;

            using (Aes aes = Aes.Create())
            {
                aes.SetKey(contentEncryptionKey);
                encryptedContent = aes.EncryptCbc("hello world!"u8, iv, PaddingMode.PKCS7);
            }

            KemRecipientInfoAsn kemRecipientInfo = new KemRecipientInfoAsn
            {
                Version = version,
                Rid = new RecipientIdentifierAsn
                {
                    SubjectKeyIdentifier = new byte[] { 1, 2, 3 },
                },
                Kem = CreateAlgorithmIdentifier(kemOid, includeKemParameters),
                Kemct = kemCiphertext ?? vector.Ciphertext.ToArray(),
                Kdf = CreateAlgorithmIdentifier(kdfOid, includeKdfParameters),
                KekLength = kekLength,
                Ukm = userKeyingMaterial,
                Wrap = wrap,
                EncryptedKey = encryptedKey,
            };
            RecipientInfoAsn recipientInfo = new RecipientInfoAsn
            {
                Ori = new OtherRecipientInfoAsn
                {
                    OriType = IdSmimeOriKem,
                    OriValue = Encode(kemRecipientInfo),
                },
            };
            EnvelopedDataAsn envelopedData = new EnvelopedDataAsn
            {
                Version = 3,
                RecipientInfos = [recipientInfo],
                EncryptedContentInfo = new EncryptedContentInfoAsn
                {
                    ContentType = TestOids.Pkcs7Data,
                    ContentEncryptionAlgorithm = new AlgorithmIdentifierAsn
                    {
                        Algorithm = TestOids.Aes256,
                        Parameters = EncodeOctetString(iv),
                    },
                    EncryptedContent = encryptedContent,
                },
            };
            ContentInfoAsn contentInfo = new ContentInfoAsn
            {
                ContentType = TestOids.Pkcs7Enveloped,
                Content = Encode(envelopedData),
            };

            return Encode(contentInfo);
        }

        private static CompositeMLKemTestVector GetVector(CompositeMLKemAlgorithm algorithm) =>
            CompositeMLKemTestData.AllIetfVectors.Single(vector => vector.Algorithm == algorithm);

        private static AlgorithmIdentifierAsn CreateAlgorithmIdentifier(string oid, bool includeNullParameters)
        {
            ReadOnlyMemory<byte>? parameters = null;

            if (includeNullParameters)
            {
                parameters = new byte[] { 0x05, 0x00 };
            }

            return new AlgorithmIdentifierAsn
            {
                Algorithm = oid,
                Parameters = parameters,
            };
        }

        private static byte[] Encode(CmsOriForKemOtherInfoAsn value)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            value.Encode(writer);
            return writer.Encode();
        }

        private static byte[] Encode(ContentInfoAsn value)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            value.Encode(writer);
            return writer.Encode();
        }

        private static byte[] Encode(EnvelopedDataAsn value)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            value.Encode(writer);
            return writer.Encode();
        }

        private static byte[] Encode(KemRecipientInfoAsn value)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            value.Encode(writer);
            return writer.Encode();
        }

        private static byte[] EncodeOctetString(byte[] value)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteOctetString(value);
            return writer.Encode();
        }
    }
}
