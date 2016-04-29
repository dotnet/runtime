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
    public static partial class KeyAgreeRecipientInfoTests
    {
        [Fact]
        public static void TestKeyAgreeVersion_RoundTrip()
        {
            KeyAgreeRecipientInfo recipient = EncodeKeyAgreel();
            Assert.Equal(3, recipient.Version);
        }

        [Fact]
        public static void TestKeyAgreeVersion_FixedValue()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1();
            Assert.Equal(3, recipient.Version);
        }

        [Fact]
        public static void TestKeyAgreeType_RoundTrip()
        {
            KeyAgreeRecipientInfo recipient = EncodeKeyAgreel();
            Assert.Equal(RecipientInfoType.KeyAgreement, recipient.Type);
        }

        [Fact]
        public static void TestKeyAgreeType_FixedValue()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1();
            Assert.Equal(RecipientInfoType.KeyAgreement, recipient.Type);
        }

        [Fact]
        public static void TestKeyAgreesRecipientIdType_RoundTrip()
        {
            KeyAgreeRecipientInfo recipient = EncodeKeyAgreel();
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, subjectIdentifier.Type);
        }

        [Fact]
        public static void TestKeyAgreeRecipientIdType_FixedValue()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1();
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            Assert.Equal(SubjectIdentifierType.IssuerAndSerialNumber, subjectIdentifier.Type);
        }

        [Fact]
        public static void TestKeyAgreeRecipientIdValue_RoundTrip()
        {
            KeyAgreeRecipientInfo recipient = EncodeKeyAgreel();
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            object value = subjectIdentifier.Value;
            Assert.True(value is X509IssuerSerial);
            X509IssuerSerial xis = (X509IssuerSerial)value;
            Assert.Equal("CN=\"Managed PKCS#7 Test Root Authority\"", xis.IssuerName);
            Assert.Equal("0AE59B0CB8119F8942EDA74163413A02", xis.SerialNumber);
        }

        [Fact]
        public static void TestKeyAgreeRecipientIdValue_FixedValue()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1();
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            object value = subjectIdentifier.Value;
            Assert.True(value is X509IssuerSerial);
            X509IssuerSerial xis = (X509IssuerSerial)value;
            Assert.Equal("CN=\"Managed PKCS#7 Test Root Authority\"", xis.IssuerName);
            Assert.Equal("0AE59B0CB8119F8942EDA74163413A02", xis.SerialNumber);
        }

        [Fact]
        public static void TestKeyAgreeRecipientIdType_Ski_RoundTrip()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1(SubjectIdentifierType.SubjectKeyIdentifier);
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            Assert.Equal(SubjectIdentifierType.SubjectKeyIdentifier, subjectIdentifier.Type);
        }

        [Fact]
        public static void TestKeyAgreeRecipientIdType_Ski_FixedValue()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1(SubjectIdentifierType.SubjectKeyIdentifier);
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            Assert.Equal(SubjectIdentifierType.SubjectKeyIdentifier, subjectIdentifier.Type);
        }

        [Fact]
        public static void TestKeyAgreeRecipientIdValue_Ski_RoundTrip()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1(SubjectIdentifierType.SubjectKeyIdentifier);
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            object value = subjectIdentifier.Value;
            Assert.True(value is string);
            string ski = (string)value;
            Assert.Equal("10DA1370316788112EB8594C864C2420AE7FBA42", ski);
        }

        [Fact]
        public static void TestKeyAgreeRecipientIdValue_Ski_FixedValue()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1(SubjectIdentifierType.SubjectKeyIdentifier);
            SubjectIdentifier subjectIdentifier = recipient.RecipientIdentifier;
            object value = subjectIdentifier.Value;
            Assert.True(value is string);
            string ski = (string)value;
            Assert.Equal("10DA1370316788112EB8594C864C2420AE7FBA42", ski);
        }

        [Fact]
        public static void TestKeyAgreeKeyEncryptionAlgorithm_RoundTrip()
        {
            KeyAgreeRecipientInfo recipient = EncodeKeyAgreel();
            AlgorithmIdentifier a = recipient.KeyEncryptionAlgorithm;
            Assert.Equal(Oids.Esdh, a.Oid.Value);
            Assert.Equal(0, a.KeyLength);
        }

        [Fact]
        public static void TestKeyAgreeKeyEncryptionAlgorithm_FixedValue()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1();
            AlgorithmIdentifier a = recipient.KeyEncryptionAlgorithm;
            Assert.Equal(Oids.Esdh, a.Oid.Value);
            Assert.Equal(0, a.KeyLength);
        }

        [Fact]
        public static void TestKeyAgreeEncryptedKey_RoundTrip()
        {
            KeyAgreeRecipientInfo recipient = EncodeKeyAgreel();
            byte[] encryptedKey = recipient.EncryptedKey;
            Assert.Equal(0x28, encryptedKey.Length);   // Since the content encryption key is randomly generated each time, we can only test the length.
        }

        [Fact]
        public static void TestKeyAgreeEncryptedKey_FixedValue()
        {
            byte[] expectedEncryptedKey = "c39323a9f5113c1465bf27b558ffeda656d606e08f8dc37e67cb8cbf7fb04d71dbe20071eaaa20db".HexToByteArray();

            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1();
            byte[] encryptedKey = recipient.EncryptedKey;
            Assert.Equal<byte>(expectedEncryptedKey, encryptedKey);
        }

        [Fact]
        public static void TestKeyAgreeOriginatorIdentifierOrKey_RoundTrip()
        {
            KeyAgreeRecipientInfo recipient = EncodeKeyAgreel();
            SubjectIdentifierOrKey originator = recipient.OriginatorIdentifierOrKey;
            Assert.Equal(SubjectIdentifierOrKeyType.PublicKeyInfo, originator.Type);
            object value = originator.Value;
            Assert.True(value is PublicKeyInfo);
            PublicKeyInfo pki = (PublicKeyInfo)value;
            AlgorithmIdentifier a = pki.Algorithm;
            Assert.Equal(Oids.Dh, a.Oid.Value);
            byte[] key = pki.KeyValue;
            Assert.NotNull(key);  // Key is randomly generated (and encoding makes the length unpredictable) so not much we can do here.
            Assert.NotEmpty(key);
        }

        [Fact]
        public static void TestKeyAgreeOriginatorIdentifierOrKey_FixedValue()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1();
            SubjectIdentifierOrKey originator = recipient.OriginatorIdentifierOrKey;
            Assert.Equal(SubjectIdentifierOrKeyType.PublicKeyInfo, originator.Type);
            object value = originator.Value;
            Assert.True(value is PublicKeyInfo);
            PublicKeyInfo pki = (PublicKeyInfo)value;
            AlgorithmIdentifier a = pki.Algorithm;
            Assert.Equal(Oids.Dh, a.Oid.Value);
            byte[] key = pki.KeyValue;
            byte[] expectedKey =
                ("0281806F96EF8C53A6919CC976E88B8F426696E7B7970ABC6BD4ABBDCF4CF34F89CEB6E8EF675000FAD2ECA3CAF9D0E51B00"
                + "4FD19A943F1779748F343FE2059E6E8208D64CB2A5BF33B2C41C20F4AE950D8F8BD720F5747D7930AF86C612088747B5315A"
                + "E68159A5AE8A80E928AA71F4E889CB2D581845EDC8F79DA5894CB7A40F9FBE").HexToByteArray();

            Assert.Equal(expectedKey, key);
        }

        [Fact]
        public static void TestKeyAgreeDate_RoundTrip()
        {
            KeyAgreeRecipientInfo recipient = EncodeKeyAgreel();
            DateTime ignore;
            Assert.Throws<InvalidOperationException>(() => ignore = recipient.Date);
        }

        [Fact]
        public static void TestKeyAgreeDate_FixedValue()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1();
            DateTime ignore;
            Assert.Throws<InvalidOperationException>(() => ignore = recipient.Date);
        }

        [Fact]
        public static void TestKeyAgreeDate_RoundTrip_Ski()
        {
            KeyAgreeRecipientInfo recipient = EncodeKeyAgreel(SubjectIdentifierType.SubjectKeyIdentifier);
            DateTime date = recipient.Date;
            long ticks = date.Ticks;
            long expectedTicks = 0x0701ce1722770000;
            Assert.Equal(expectedTicks, ticks);
        }

        [Fact]
        public static void TestKeyAgreeDate_FixedValue_Ski()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1(SubjectIdentifierType.SubjectKeyIdentifier);
            DateTime date = recipient.Date;
            long ticks = date.Ticks;
            long expectedTicks = 0x0701ce1722770000;
            Assert.Equal(expectedTicks, ticks);
        }


        [Fact]
        public static void TestKeyAgreeOtherKeyAttribute_RoundTrip()
        {
            KeyAgreeRecipientInfo recipient = EncodeKeyAgreel();
            CryptographicAttributeObject ignore;
            Assert.Throws<InvalidOperationException>(() => ignore = recipient.OtherKeyAttribute);
        }

        [Fact]
        public static void TestKeyAgreeOtherKeyAttribute_FixedValue()
        {
            KeyAgreeRecipientInfo recipient = FixedValueKeyAgree1();
            CryptographicAttributeObject ignore;
            Assert.Throws<InvalidOperationException>(() => ignore = recipient.OtherKeyAttribute);
        }

        [Fact]
        public static void TestKeyAgreeOtherKeyAttribute_FixedValue_Ski()
        {
            //
            // Test a KeyAgree CMS that embeds an OtherKeyAttribute in the recipient. We don't surface a way to generate this
            // in the EnvelopedCms class so we can only do a fixed-value test here.
            //
            byte[] encryptedCms =
                 ("3082015e06092a864886f70d010703a082014f3082014b02010231820117a1820113020103a08195a18192300906072a8648"
                + "ce3e020103818400028180313d5659f9a8633243f97d11462a3b07702802fed45abbe68e3f2670bfa500d6b4f70c1c0dceac"
                + "adffe9736764204806710d4e834b48773b7a4696a690a03abbc38c936483d2f3ccb8d764f66dc269d78a77821edd0decdef2"
                + "b1d4356c1d1f1b34cc81c76214b8a04f3d70e1e9dc9a589cc8410599dceafad903b7b01b6b2489301e060b2a864886f70d01"
                + "09100305300f060b2a864886f70d0109100306050030563054a028041413213309b39348347fb6a2155a53172be79c19c530"
                + "1006092a864886f70d0107010403424d580428da28837b15e9bdf5528180a2e9beb91bf0d4519a5d76b655ebac9c43012d50"
                + "d6b55f0618380210e4302b06092a864886f70d010701301406082a864886f70d030704087bb5f0d33e6e0354800814219a66"
                + "c2ea1449").HexToByteArray();

            EnvelopedCms envelopedCms = new EnvelopedCms();
            envelopedCms.Decode(encryptedCms);
            KeyAgreeRecipientInfo r = (KeyAgreeRecipientInfo)(envelopedCms.RecipientInfos[0]);
            CryptographicAttributeObject attribute = r.OtherKeyAttribute;
            Assert.Equal(Oids.Pkcs7Data, attribute.Oid.Value);
            Assert.Equal(1, attribute.Values.Count);
            AsnEncodedData asnData = attribute.Values[0];
            byte[] expectedAsnData = "0403424d58".HexToByteArray();
            Assert.Equal<byte>(expectedAsnData, asnData.RawData);
        }

        private static KeyAgreeRecipientInfo EncodeKeyAgreel(SubjectIdentifierType type = SubjectIdentifierType.IssuerAndSerialNumber)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);
            using (X509Certificate2 cert = Certificates.DHKeyAgree1.GetCertificate())
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
            Assert.True(recipientInfo is KeyAgreeRecipientInfo);
            return (KeyAgreeRecipientInfo)recipientInfo;
        }

        private static KeyAgreeRecipientInfo FixedValueKeyAgree1(SubjectIdentifierType type = SubjectIdentifierType.IssuerAndSerialNumber)
        {
            byte[] encodedMessage;
            switch (type)
            {
                case SubjectIdentifierType.IssuerAndSerialNumber:
                    encodedMessage = s_KeyAgreeEncodedMessage;
                    break;

                case SubjectIdentifierType.SubjectKeyIdentifier:
                    encodedMessage = s_KeyAgreeEncodedMessage_Ski;
                    break;

                default:
                    throw new Exception("Bad SubjectIdentifierType.");
            }

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            RecipientInfoCollection recipients = ecms.RecipientInfos;
            Assert.Equal(1, recipients.Count);
            RecipientInfo recipientInfo = recipients[0];
            Assert.True(recipientInfo is KeyAgreeRecipientInfo);
            return (KeyAgreeRecipientInfo)recipientInfo;
        }



        private static byte[] s_KeyAgreeEncodedMessage =
             ("3082019b06092a864886f70d010703a082018c3082018802010231820154a1820150020103a08195a18192300906072a8648"
            + "ce3e0201038184000281806f96ef8c53a6919cc976e88b8f426696e7b7970abc6bd4abbdcf4cf34f89ceb6e8ef675000fad2"
            + "eca3caf9d0e51b004fd19a943f1779748f343fe2059e6e8208d64cb2a5bf33b2c41c20f4ae950d8f8bd720f5747d7930af86"
            + "c612088747b5315ae68159a5ae8a80e928aa71f4e889cb2d581845edc8f79da5894cb7a40f9fbe301e060b2a864886f70d01"
            + "09100305300f060b2a864886f70d0109100306050030819230818f3063304f314d304b06035504031e44004d0061006e0061"
            + "00670065006400200050004b00430053002300370020005400650073007400200052006f006f007400200041007500740068"
            + "006f007200690074007902100ae59b0cb8119f8942eda74163413a020428c39323a9f5113c1465bf27b558ffeda656d606e0"
            + "8f8dc37e67cb8cbf7fb04d71dbe20071eaaa20db302b06092a864886f70d010701301406082a864886f70d0307040879d81f"
            + "ee4a736dde800892bc977ea496752d").HexToByteArray();

        private static byte[] s_KeyAgreeEncodedMessage_Ski =
            ("3082014d06092a864886f70d010703a082013e3082013a02010231820106a1820102020103a08196a18193300906072a8648"
            + "ce3e02010381850002818100ac89002e19d3a7dc35dafbf083413483ef14691fc00a465b957496ca860ba49181821cafb50e"
            + "b25330952bb11a71a44b44691cf9779999f1115497cd1ce238b452ca95622af968e39f06e165d2ebe199149370334d925aa4"
            + "7273751ac63a0ef80cdcf6331ed3324cd689bffc90e61e9cc921c88ef5fb92b863053c4c1fabfe15301e060b2a864886f70d"
            + "0109100305300f060b2a864886f70d0109100306050030443042a016041410da1370316788112eb8594c864c2420ae7fba42"
            + "0428dfbdc19ad44063478a0c125641be274113441ad5891c78f925097f06a3df57f3f1e6d1160f8d3c22302b06092a864886"
            + "f70d010701301406082a864886f70d030704088aadc286f258f6d78008fc304f518a653f83").HexToByteArray();
    }
}


