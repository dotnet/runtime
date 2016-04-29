// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using Xunit;

using Test.Cryptography;
using System.Security.Cryptography.Pkcs.Tests;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    public static partial class KeyTransRecipientInfoTests
    {
        [Fact]
        public static void TestKeyTransVersion_RoundTrip()
        {
            KeyTransRecipientInfo recipient = EncodeKeyTransl();
            Assert.Equal(0, recipient.Version);
        }

        [Fact]
        public static void TestKeyTransVersion_FixedValue()
        {
            KeyTransRecipientInfo recipient = FixedValueKeyTrans1();
            Assert.Equal(0, recipient.Version);
        }

        [Fact]
        public static void TestKeyTransType_RoundTrip()
        {
            KeyTransRecipientInfo recipient = EncodeKeyTransl();
            Assert.Equal(RecipientInfoType.KeyTransport, recipient.Type);
        }

        [Fact]
        public static void TestKeyTransType_FixedValue()
        {
            KeyTransRecipientInfo recipient = FixedValueKeyTrans1();
            Assert.Equal(RecipientInfoType.KeyTransport, recipient.Type);
        }

        [Fact]
        public static void TestKeyTransRecipientIdType_RoundTrip()
        {
            KeyTransRecipientInfo recipient = EncodeKeyTransl();
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, subjectIdentifier.Type);
        }

        [Fact]
        public static void TestKeyTransRecipientIdType_FixedValue()
        {
            KeyTransRecipientInfo recipient = FixedValueKeyTrans1();
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, subjectIdentifier.Type);
        }

        [Fact]
        public static void TestKeyTransRecipientIdValue_RoundTrip()
        {
            KeyTransRecipientInfo recipient = EncodeKeyTransl();
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            object value = subjectIdentifier.Value;
            Assert.True(value is X509IssuerSerial);
            X509IssuerSerial xis = (X509IssuerSerial)value;
            Assert.Equal("CN=RSAKeyTransfer1", xis.IssuerName);
            Assert.Equal("31D935FB63E8CFAB48A0BF7B397B67C0", xis.SerialNumber);
        }

        [Fact]
        public static void TestKeyTransRecipientIdValue_FixedValue()
        {
            KeyTransRecipientInfo recipient = FixedValueKeyTrans1();
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            object value = subjectIdentifier.Value;
            Assert.True(value is X509IssuerSerial);
            X509IssuerSerial xis = (X509IssuerSerial)value;
            Assert.Equal("CN=RSAKeyTransfer1", xis.IssuerName);
            Assert.Equal("31D935FB63E8CFAB48A0BF7B397B67C0", xis.SerialNumber);
        }

        [Fact]
        public static void TestKeyTransRecipientIdType_Ski_RoundTrip()
        {
            KeyTransRecipientInfo recipient = EncodeKeyTransl(SubjectIdentifierType.SubjectKeyIdentifier);
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            Assert.Equal(SubjectIdentifierType.SubjectKeyIdentifier, subjectIdentifier.Type);
        }

        [Fact]
        public static void TestKeyTransRecipientIdType_Ski_FixedValue()
        {
            KeyTransRecipientInfo recipient = FixedValueKeyTrans1(SubjectIdentifierType.SubjectKeyIdentifier);
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            Assert.Equal(SubjectIdentifierType.SubjectKeyIdentifier, subjectIdentifier.Type);
        }

        [Fact]
        public static void TestKeyTransRecipientIdValue_Ski_RoundTrip()
        {
            KeyTransRecipientInfo recipient = EncodeKeyTransl(SubjectIdentifierType.SubjectKeyIdentifier);
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            object value = subjectIdentifier.Value;
            Assert.True(value is string);
            string ski = (string)value;
            Assert.Equal("F2008AA9FA3742E8370CB1674CE1D1582921DCC3", ski);
        }

        [Fact]
        public static void TestKeyTransRecipientIdValue_Ski_FixedValue()
        {
            KeyTransRecipientInfo recipient = FixedValueKeyTrans1(SubjectIdentifierType.SubjectKeyIdentifier);
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            object value = subjectIdentifier.Value;
            Assert.True(value is string);
            string ski = (string)value;
            Assert.Equal("F2008AA9FA3742E8370CB1674CE1D1582921DCC3", ski);
        }

        [Fact]
        public static void TestKeyTransKeyEncryptionAlgorithm_RoundTrip()
        {
            KeyTransRecipientInfo recipient = EncodeKeyTransl();
            AlgorithmIdentifier a = recipient.KeyEncryptionAlgorithm;
            Assert.Equal(Oids.Rsa, a.Oid.Value);
            Assert.Equal(0, a.KeyLength);
        }

        [Fact]
        public static void TestKeyTransKeyEncryptionAlgorithm_FixedValue()
        {
            KeyTransRecipientInfo recipient = FixedValueKeyTrans1();
            AlgorithmIdentifier a = recipient.KeyEncryptionAlgorithm;
            Assert.Equal(Oids.Rsa, a.Oid.Value);
            Assert.Equal(0, a.KeyLength);
        }

        [Fact]
        public static void TestKeyTransEncryptedKey_RoundTrip()
        {
            KeyTransRecipientInfo recipient = EncodeKeyTransl();
            byte[] encryptedKey = recipient.EncryptedKey;
            Assert.Equal(128, encryptedKey.Length);   // Since the content encryption key is randomly generated each time, we can only test the length.
        }

        [Fact]
        public static void TestKeyTransEncryptedKey_FixedValue()
        {
            byte[] expectedEncryptedKey =
                ("5ebb2d08773594be9ec5d30c0707cf339f2b982a4f0797b74d520a0c973d668a9a6ad9d28066ef36e5b5620fef67f4d79ee5"
                + "0c25eb999f0c656548347d5676ac4b779f8fce2b87e6388fbe483bb0fcf78ab1f1ff29169600401fded7b2803a0bf96cc160"
                + "c496726216e986869eed578bda652855c85604a056201538ee56b6c4").HexToByteArray();

            KeyTransRecipientInfo recipient = FixedValueKeyTrans1();
            byte[] encryptedKey = recipient.EncryptedKey;
            Assert.Equal<byte>(expectedEncryptedKey, encryptedKey);
        }

        private static KeyTransRecipientInfo EncodeKeyTransl(SubjectIdentifierType type = SubjectIdentifierType.IssuerAndSerialNumber)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(type, cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();

            EnvelopedCms ecms2 = new EnvelopedCms();
            ecms2.Decode(encodedMessage);

            RecipientInfoCollection recipients = ecms2.RecipientInfos;
            Assert.Equal(1, recipients.Count);
            RecipientInfo recipientInfo = recipients[0];
            Assert.True(recipientInfo is KeyTransRecipientInfo);
            return (KeyTransRecipientInfo)recipientInfo;
        }

        private static KeyTransRecipientInfo FixedValueKeyTrans1(SubjectIdentifierType type = SubjectIdentifierType.IssuerAndSerialNumber)
        {
            byte[] encodedMessage;
            switch (type)
            {
                case SubjectIdentifierType.IssuerAndSerialNumber:
                    encodedMessage = s_KeyTransEncodedMessage;
                    break;

                case SubjectIdentifierType.SubjectKeyIdentifier:
                    encodedMessage = s_KeyTransEncodedMessage_Ski;
                    break;

                default:
                    throw new Exception("Bad SubjectIdentifierType.");
            }

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            RecipientInfoCollection recipients = ecms.RecipientInfos;
            Assert.Equal(1, recipients.Count);
            RecipientInfo recipientInfo = recipients[0];
            Assert.True(recipientInfo is KeyTransRecipientInfo);
            return (KeyTransRecipientInfo)recipientInfo;
        }

        private static byte[] s_KeyTransEncodedMessage =
            ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
            + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481805e"
            + "bb2d08773594be9ec5d30c0707cf339f2b982a4f0797b74d520a0c973d668a9a6ad9d28066ef36e5b5620fef67f4d79ee50c"
            + "25eb999f0c656548347d5676ac4b779f8fce2b87e6388fbe483bb0fcf78ab1f1ff29169600401fded7b2803a0bf96cc160c4"
            + "96726216e986869eed578bda652855c85604a056201538ee56b6c4302b06092a864886f70d010701301406082a864886f70d"
            + "030704083adadf63cd297a86800835edc437e31d0b70").HexToByteArray();

        private static byte[] s_KeyTransEncodedMessage_Ski =
            ("3081f206092a864886f70d010703a081e43081e10201023181ae3081ab0201028014f2008aa9fa3742e8370cb1674ce1d158"
            + "2921dcc3300d06092a864886f70d01010105000481804336e978bc72ba2f5264cd854867fac438f36f2b3df6004528f2df83"
            + "4fb2113d6f7c07667e7296b029756222d6ced396a8fffed32be838eec7f2e54b9467fa80f85d097f7d1f0fbde57e07ab3d46"
            + "a60b31f37ef9844dcab2a8eef4fec5579fac5ec1e7ee82409898e17d30c3ac1a407fca15d23c9df2904a707294d78d4300ba"
            + "302b06092a864886f70d010701301406082a864886f70d03070408355c596e3e8540608008f1f811e862e51bbd").HexToByteArray();
    }
}


