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
    public static partial class UnprotectedAttributeTests
    {
        [Fact]
        public static void TestUnprotectedAttributes0_RoundTrip()
        {
            byte[] encodedMessage = CreateEcmsWithAttributes();
            VerifyUnprotectedAttributes0(encodedMessage);
        }

        [Fact]
        public static void TestUnprotectedAttributes0_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082010c06092a864886f70d010703a081fe3081fb0201003181c83081c5020100302e301a311830160603550403130f5253"
                + "414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d010101050004818048"
                + "fea7a626159f3792b8daa8e064de2f986cf0e432428c6cb80ed4bf6e593db74a7f907e0918b4bee6d55e1e0f9da6b6519e42"
                + "23d58b0f717165a727d7dac87556916c800e2346beac5a825c973e9bba4fe6c549baafd151d85fd7c266769dbb57f28e45f8"
                + "6bb5478d018e132cb576079d8c2a7f4217973c3ff1f0617364809c302b06092a864886f70d010701301406082a864886f70d"
                + "030704083979569d26db4c278008d2fa4271358f9a2f").HexToByteArray();
            VerifyUnprotectedAttributes0(encodedMessage);
        }

        private static void VerifyUnprotectedAttributes0(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AsnEncodedData[] attributes = ecms.UnprotectedAttributes.FlattenAndSort();
            Assert.Equal(0, attributes.Length);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_DocumentDescription_RoundTrip()
        {
            byte[] encodedMessage = CreateEcmsWithAttributes(new Pkcs9DocumentDescription("My Description"));
            VerifyUnprotectedAttributes1_DocumentDescription(encodedMessage);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_DocumentDescription_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082014006092a864886f70d010703a08201313082012d0201023181c83081c5020100302e301a311830160603550403130f"
                + "5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481"
                + "801dba607648ab56b88dffb4bfe36170d698910562cef64dc2f3098ca2d7fa97334e721b6d00250470f8a16b5d2f5f38cc84"
                + "c05d49e5aa7390ed4e0d6a7369f72f8bb0209545b6b8d3302c2c3fc8c8e7f5a54dd30b20855a77b5cd40c6f2b59376252aef"
                + "e1d90965916e25c63f509e32f86a9213b740796927bbb0573b024b7ba3302b06092a864886f70d010701301406082a864886"
                + "f70d03070408df8ced363e2a76288008262ce8fe027530a3a130302e060a2b0601040182375802023120041e4d0079002000"
                + "4400650073006300720069007000740069006f006e000000").HexToByteArray();
            VerifyUnprotectedAttributes1_DocumentDescription(encodedMessage);
        }

        private static void VerifyUnprotectedAttributes1_DocumentDescription(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AsnEncodedData[] attributes = ecms.UnprotectedAttributes.FlattenAndSort();
            Assert.Equal(1, attributes.Length);
            attributes[0].AssertIsDocumentationDescription("My Description");
        }

        [Fact]
        public static void TestUnprotectedAttributes1_DocumenName_RoundTrip()
        {
            byte[] encodedMessage = CreateEcmsWithAttributes(new Pkcs9DocumentName("My Name"));
            VerifyUnprotectedAttributes1_DocumentName(encodedMessage);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_DocumentName_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082013206092a864886f70d010703a08201233082011f0201023181c83081c5020100302e301a311830160603550403130f"
                + "5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481"
                + "8000d17895fe3d35a30f0666e4064cc40757251404f785c4878ab98d1440d7f1706991cf854b2498bca8781c7728805b03e8"
                + "d36329f34d7508bf38e4e4d5447e8c7d6d1c652f0a40bb3fc396e1139094d6349e08a7d233b9323c4760a96199660c87c7f0"
                + "2c891711efcee6085f07fa0060da6f9b22d895b312caed824916b14314302b06092a864886f70d010701301406082a864886"
                + "f70d03070408817ee1c4bb617f828008de69d9d27afef823a1223020060a2b060104018237580201311204104d0079002000"
                + "4e0061006d0065000000").HexToByteArray();
            VerifyUnprotectedAttributes1_DocumentName(encodedMessage);
        }

        private static void VerifyUnprotectedAttributes1_DocumentName(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AsnEncodedData[] attributes = ecms.UnprotectedAttributes.FlattenAndSort();
            Assert.Equal(1, attributes.Length);
            attributes[0].AssertIsDocumentationName("My Name");
        }

        [Fact]
        public static void TestUnprotectedAttributes1_SigningTime_RoundTrip()
        {
            byte[] encodedMessage = CreateEcmsWithAttributes(new Pkcs9SigningTime(new DateTime(2018, 4, 1, 8, 30, 05)));
            VerifyUnprotectedAttributes1_SigningTime(encodedMessage);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_SigningTime_FixedValue()
        {
            byte[] encodedMessage =
                ("3082012e06092a864886f70d010703a082011f3082011b0201023181c83081c5020100302e301a311830160603550403130f"
                + "5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481"
                + "801cf4efe18f457c32753fd3c908372ac8075cd4c4bdec98a912cc241b604b7ba46f44c657c769e84998384c28ee9a670d7d"
                + "8d7427f5a3bffb2a6e09269f1c89a7b1906eb0a828487d6e4fcb2c6e9023f3d9f8114701cf01c2fcb4db0c9ab7144c8e2b73"
                + "6551ca1a348ac3c25cca9de1bdef79c0bdd2ba8b79e6e668f947cf1bc7302b06092a864886f70d010701301406082a864886"
                + "f70d03070408e7575fbec5da862080084306defef088dd0ea11e301c06092a864886f70d010905310f170d31383034303130"
                + "38333030355a").HexToByteArray();

            VerifyUnprotectedAttributes1_SigningTime(encodedMessage);
        }

        private static void VerifyUnprotectedAttributes1_SigningTime(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AsnEncodedData[] attributes = ecms.UnprotectedAttributes.FlattenAndSort();
            Assert.Equal(1, attributes.Length);
            DateTime expectedSigningTIme = new DateTime(2018, 4, 1, 8, 30, 05);
            attributes[0].AssertIsSigningTime(expectedSigningTIme);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_ContentType_RoundTrip()
        {
            byte[] rawData = "06072a9fa20082f300".HexToByteArray();
            Pkcs9AttributeObject pkcs9ContentType = new Pkcs9AttributeObject(Oids.ContentType, rawData);

            byte[] encodedMessage = CreateEcmsWithAttributes(pkcs9ContentType);
            VerifyUnprotectedAttributes1_ContentType(encodedMessage);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_ContentType_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082012806092a864886f70d010703a0820119308201150201023181c83081c5020100302e301a311830160603550403130f"
                + "5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481"
                + "804abd2e5b20b7d255234ffa6b025a9c6a35362ac4affc4d78ed02a647ac9d9cb8d02bf64f924f19c731fc6e7f333180c6f1"
                + "0be40c382b5da5db96b0303819573dc3598aa978704ee96a98113ec110d48aef57c745ee0188feceac27a3739663bb52fccc"
                + "37e106ed3d8ecf3806bcc4df83ce989080405e3e856e725a85aa205bda302b06092a864886f70d010701301406082a864886"
                + "f70d030704084a0375f470805f908008522c8448feaf357da118301606092a864886f70d010903310906072a9fa20082f300").HexToByteArray();

            VerifyUnprotectedAttributes1_ContentType(encodedMessage);
        }

        private static void VerifyUnprotectedAttributes1_ContentType(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AsnEncodedData[] attributes = ecms.UnprotectedAttributes.FlattenAndSort();
            Assert.Equal(1, attributes.Length);
            attributes[0].AssertIsContentType("1.2.512256.47488");
        }

        [Fact]
        public static void TestUnprotectedAttributes1_MessageDigest_RoundTrip()
        {
            byte[] rawData = "0405032d58805d".HexToByteArray();
            Pkcs9AttributeObject pkcs9MessageDigest = new Pkcs9AttributeObject(Oids.MessageDigest, rawData);

            byte[] encodedMessage = CreateEcmsWithAttributes(pkcs9MessageDigest);
            VerifyUnprotectedAttributes1_MessageDigest(encodedMessage);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_MessageDigest_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082012606092a864886f70d010703a0820117308201130201023181c83081c5020100302e301a311830160603550403130f"
                + "5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481"
                + "802ed2085366bf01caca75c260d900c12ef3377f868d879c7b33734dd912aeffcb55f8f8fefea550bd75472ad20f56dd6edf"
                + "d1c9669f3653e41d48f03c0796513da5b922587415853fc46ef5f452cea25a58c6da296527c51111a2fa6e472651391e2c3a"
                + "ffb081ce6f7e4aee275d0f3d3e351b5e76c84afb5f80bb1ef594eb9b92302b06092a864886f70d010701301406082a864886"
                + "f70d030704088b5d38dad37f599280081d626f58eabeeb0aa116301406092a864886f70d01090431070405032d58805d").HexToByteArray();

            VerifyUnprotectedAttributes1_MessageDigest(encodedMessage);
        }

        private static void VerifyUnprotectedAttributes1_MessageDigest(byte[] encodedMessage)
        {
            byte[] expectedMessageDigest = "032d58805d".HexToByteArray();
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AsnEncodedData[] attributes = ecms.UnprotectedAttributes.FlattenAndSort();
            Assert.Equal(1, attributes.Length);
            attributes[0].AssertIsMessageDigest(expectedMessageDigest);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_Merge3_RoundTrip()
        {
            byte[] encodedMessage = CreateEcmsWithAttributes(
                new Pkcs9DocumentDescription("My Description 1"),
                new Pkcs9DocumentDescription("My Description 2"),
                new Pkcs9DocumentDescription("My Description 3")
            );
            VerifyUnprotectedAttributes_Merge3(encodedMessage);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_Merge3_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082018c06092a864886f70d010703a082017d308201790201023181c83081c5020100302e301a311830160603550403130f"
                + "5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481"
                + "809203ec6ad0877ae56dbc8a480e59e94ca953839f8f769ec1b262b4f4916c9863ee9c41007c356fbf2fc3f3d03e4aa1ea46"
                + "575d0fbc0c5f47e41778f34535eea84e648009955f271a9e3c24f8c192d31406d46a40396b78d4013cfe3dcc443ac9ca9213"
                + "7ffa503297ca1d241f68e905c60134c02e7ce8e1e67481abebe3d7faa1302b06092a864886f70d010701301406082a864886"
                + "f70d030704085e2e9ca7859583a08008fb219dbb2a84a2eda17c307a060a2b060104018237580202316c04224d0079002000"
                + "4400650073006300720069007000740069006f006e0020003100000004224d00790020004400650073006300720069007000"
                + "740069006f006e0020003200000004224d00790020004400650073006300720069007000740069006f006e00200033000000").HexToByteArray();
            VerifyUnprotectedAttributes_Merge3(encodedMessage);
        }

        private static void VerifyUnprotectedAttributes_Merge3(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AsnEncodedData[] attributes = ecms.UnprotectedAttributes.FlattenAndSort();
            Assert.Equal(3, attributes.Length);
            attributes[0].AssertIsDocumentationDescription("My Description 1");
            attributes[1].AssertIsDocumentationDescription("My Description 2");
            attributes[2].AssertIsDocumentationDescription("My Description 3");
        }

        [Fact]
        public static void TestUnprotectedAttributes1_Heterogenous3_RoundTrip()
        {
            byte[] encodedMessage = CreateEcmsWithAttributes(
                new Pkcs9DocumentDescription("My Description 1"),
                new Pkcs9DocumentDescription("My Description 2"),
                new Pkcs9DocumentName("My Name 1")
            );
            VerifyUnprotectedAttributes_Heterogenous3(encodedMessage);
        }

        private static void VerifyUnprotectedAttributes_Heterogenous3(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AsnEncodedData[] attributes = ecms.UnprotectedAttributes.FlattenAndSort();
            Assert.Equal(3, attributes.Length);
            attributes[0].AssertIsDocumentationName("My Name 1");
            attributes[1].AssertIsDocumentationDescription("My Description 1");
            attributes[2].AssertIsDocumentationDescription("My Description 2");
        }

        [Fact]
        public static void TestUnprotectedAttributes1_EmptySet()
        {
            // This tests the behavior of unprotected attribute extraction when one of the attribute sequences declares an
            // attribute type, but the contained SET OF AttributeValue is empty.
            //
            // Attribute ::= SEQUENCE {
            //      attrType OBJECT IDENTIFIER,
            //      attrValues SET OF AttributeValue }
            //
            // The encoded message was built in ASN.1 editor and tested in framework.It contains an enveloped message
            // version 2 with a key transport recipient, the enveloped message contains data encrypted with 3DES.
            //
            // The attributes set is built as
            // {
            //      { attrType: document description, attrValues: { value1, value2 } },
            //      { attrType: document name, attrValues: { } },
            // }
            //
            // The important part of this test is that there are 0 attributes of a type that is declared within the encoded message.
            // This should return 2 as it should create a CryptographicAttributeObjectCollection with two CryptographicAttributeObjects, 
            // the first one holding a list of document description with the two values, the second one holding an empty list of
            // document name.

            byte[] encodedMessage =
                ("3082017806092A864886F70D010703A0820169308201650201023181C83081C5020100302E301A311830160603550403"
                + "130F5253414B65795472616E7366657231021031D935FB63E8CFAB48A0BF7B397B67C0300D06092A864886F70D010101"
                + "05000481802EE6A4AAA9F907E8EF472D8CD8603098488EC1C462815E6FC5A53A3DF6EB730F3D191746FDBBCA89114C6D"
                + "45FB6C4F26088043894D5A706889A29D52E03ABEDFAC98336BD01B0A9CFA57CC6C80908F4B42EFCE5E60E7A761451A4D"
                + "1A39783072000E551062027795A1CEB079791BA48C5F77D360EE48E185DE6C8CCB1C093D4B302B06092A864886F70D01"
                + "0701301406082A864886F70D03070408F55F613664678EE9800800BC3504D1F59470A168300E060A2B06010401823758"
                + "020131003056060A2B060104018237580202314804224D00790020004400650073006300720069007000740069006F00"
                + "6E0020003100000004224D00790020004400650073006300720069007000740069006F006E00200032000000").HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);

            Assert.Equal(2, ecms.UnprotectedAttributes.Count);

            CryptographicAttributeObjectCollection collection = ecms.UnprotectedAttributes;
            string attrObj0Oid = collection[0].Oid.Value;

            CryptographicAttributeObject documentDescObj = (attrObj0Oid == Oids.DocumentDescription) ?
                collection[0] :
                collection[1];

            CryptographicAttributeObject documentNameObj = (attrObj0Oid == Oids.DocumentName) ?
                collection[0] :
                collection[1];

            Assert.Equal(0, documentNameObj.Values.Count);
            Assert.Equal(2, documentDescObj.Values.Count);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_Arbitrary_RoundTrip()
        {
            byte[] encodedMessage = CreateEcmsWithAttributes(
                new AsnEncodedData(new Oid(Oids.Pkcs7Data), new byte[] { 4, 3, 6, 7, 8 })
            );
            VerifyUnprotectedAttributes_Arbitrary1(encodedMessage);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_Arbitrary_FixedValue()
        {
            byte[] encodedMessage =
                 ("3082012406092a864886f70d010703a0820115308201110201023181c83081c5020100302e301a311830160603550403130f"
                + "5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481"
                + "800f310e1c63177fd87bbacd13de65aae943fff87a1da112b9d1a2578c444ebfeb5d72db13104e1f72231651f1a46dec72c1"
                + "91ae859e2cd96df3f94599fff2fdc9074ea9722739c9b0ac870acd073c11375d79ab7679b0d2ebab839f0c2ee975d7ef4a59"
                + "5933aebfcae745f98109c0e5cfd298960cebd244d6a029d9f21bfe60fb302b06092a864886f70d010701301406082a864886"
                + "f70d03070408aa76edcbc0a03d7d8008aefe2086db6f8f7ca114301206092a864886f70d01070131050403060708").HexToByteArray();
            VerifyUnprotectedAttributes_Arbitrary1(encodedMessage);
        }

        private static void VerifyUnprotectedAttributes_Arbitrary1(byte[] encodedMessage)
        {
            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AsnEncodedData[] attributes = ecms.UnprotectedAttributes.FlattenAndSort();
            Assert.Equal(1, attributes.Length);
            AsnEncodedData a = attributes[0];
            Assert.Equal(Oids.Pkcs7Data, a.Oid.Value);
            byte[] expectedRawData = { 4, 3, 6, 7, 8 };
            Assert.Equal<byte>(expectedRawData, a.RawData);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_OutOfNamespace_RoundTrip()
        {
            byte[] constraintsRawData = "30070101ff02020100".HexToByteArray();

            byte[] encodedMessage = CreateEcmsWithAttributes(
                new AsnEncodedData(Oids.BasicConstraints2, constraintsRawData)
            );
            VerifyUnprotectedAttributes1_OutOfNamespace(encodedMessage);
        }

        [Fact]
        public static void TestUnprotectedAttributes1_OutOfNamespace_FixedValue()
        {
            byte[] encodedMessage =
                ("3082012206092a864886f70d010703a08201133082010f0201023181c83081c5020100302e301a311830160603550403130f"
               + "5253414b65795472616e7366657231021031d935fb63e8cfab48a0bf7b397b67c0300d06092a864886f70d01010105000481"
               + "801dd92ffa848bc03011c8aae71d63c9a81fdbb2e2c82c284bcbe40d77c3f67beb8cdd2c017470a48189e8ccd3d310bde567"
               + "202e3a03cb9866d19262bda6a489c957e14b1068ecfb2ae8ea1cbf47c6a934a5eed8ce05965356e033f2a1c68001cd308604"
               + "50f28b7949af886727fb506d64ae7889f613c03729a7b834591881666c302b06092a864886f70d010701301406082a864886"
               + "f70d0307040814a020bca56417de8008a260d0e4e138743ea11230100603551d13310930070101ff02020100").HexToByteArray();

            VerifyUnprotectedAttributes1_OutOfNamespace(encodedMessage);
        }

        private static void VerifyUnprotectedAttributes1_OutOfNamespace(byte[] encodedMessage)
        {
            byte[] constraintsRawData = "30070101ff02020100".HexToByteArray();

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AsnEncodedData[] attributes = ecms.UnprotectedAttributes.FlattenAndSort();
            Assert.Equal(1, attributes.Length);
            AsnEncodedData a = attributes[0];
            Assert.Equal(Oids.BasicConstraints2, a.Oid.Value);
            Assert.Equal<byte>(constraintsRawData, a.RawData);
        }

        [Fact]
        public static void TestUnprotectedAttributes_AlwaysReturnsPkcs9AttributeObject()
        {
            byte[] encodedMessage = CreateEcmsWithAttributes(
                new AsnEncodedData(new Oid(Oids.Pkcs7Data), new byte[] { 4, 3, 6, 7, 8 })
            );

            // ecms.Decode() always populates UnprotectedAttribute objects with Pkcs9AttributeObjects (or one of its derived classes) rather than
            // AsnEncodedData instances. Verify that any new implementation does this as someone out there is probably
            // casting to Pkcs9AttributeObject without checking.

            EnvelopedCms ecms = new EnvelopedCms();
            ecms.Decode(encodedMessage);
            AsnEncodedData[] attributes = ecms.UnprotectedAttributes.FlattenAndSort();
            Assert.Equal(1, attributes.Length);
            AsnEncodedData a = attributes[0];
            Assert.True(a is Pkcs9AttributeObject);
        }

        private static void AssertIsDocumentationDescription(this AsnEncodedData attribute, string expectedDocumentDescription)
        {
            Assert.Equal(Oids.DocumentDescription, attribute.Oid.Value);
            Pkcs9DocumentDescription enhancedAttribute = attribute as Pkcs9DocumentDescription;
            Assert.NotNull(enhancedAttribute);
            Assert.Equal(expectedDocumentDescription, enhancedAttribute.DocumentDescription);
        }

        private static void AssertIsDocumentationName(this AsnEncodedData attribute, string expectedDocumentName)
        {
            Assert.Equal(Oids.DocumentName, attribute.Oid.Value);
            Pkcs9DocumentName enhancedAttribute = attribute as Pkcs9DocumentName;
            Assert.NotNull(enhancedAttribute);
            Assert.Equal(expectedDocumentName, enhancedAttribute.DocumentName);
        }

        private static void AssertIsSigningTime(this AsnEncodedData attribute, DateTime expectedTime)
        {
            Assert.Equal(Oids.SigningTime, attribute.Oid.Value);
            Pkcs9SigningTime enhancedAttribute = attribute as Pkcs9SigningTime;
            Assert.NotNull(enhancedAttribute);
            Assert.Equal(expectedTime, enhancedAttribute.SigningTime);
        }

        private static void AssertIsContentType(this AsnEncodedData attribute, string expectedContentType)
        {
            Assert.Equal(Oids.ContentType, attribute.Oid.Value);
            Pkcs9ContentType enhancedAttribute = attribute as Pkcs9ContentType;
            Assert.NotNull(enhancedAttribute);
            Assert.Equal(expectedContentType, enhancedAttribute.ContentType.Value);
        }

        private static void AssertIsMessageDigest(this AsnEncodedData attribute, byte[] expectedDigest)
        {
            Assert.Equal(Oids.MessageDigest, attribute.Oid.Value);
            Pkcs9MessageDigest enhancedAttribute = attribute as Pkcs9MessageDigest;
            Assert.NotNull(enhancedAttribute);
            Assert.Equal<byte>(expectedDigest, enhancedAttribute.MessageDigest);
        }

        private static byte[] CreateEcmsWithAttributes(params AsnEncodedData[] attributes)
        {
            ContentInfo contentInfo = new ContentInfo(new byte[] { 1, 2, 3 });
            EnvelopedCms ecms = new EnvelopedCms(contentInfo);

            foreach (AsnEncodedData attribute in attributes)
            {
                ecms.UnprotectedAttributes.Add(attribute);
            }

            using (X509Certificate2 cert = Certificates.RSAKeyTransfer1.GetCertificate())
            {
                CmsRecipient cmsRecipient = new CmsRecipient(cert);
                ecms.Encrypt(cmsRecipient);
            }
            byte[] encodedMessage = ecms.Encode();
            return encodedMessage;
        }

        private static AsnEncodedData[] FlattenAndSort(this CryptographicAttributeObjectCollection col)
        {
            List<AsnEncodedData> attributes = new List<AsnEncodedData>();
            foreach (CryptographicAttributeObject cao in col)
            {
                AsnEncodedDataCollection acol = cao.Values;
                foreach (AsnEncodedData a in acol)
                {
                    attributes.Add(a);
                }
            }

            return attributes.OrderBy(a => a.RawData.ByteArrayToHex()).ToArray();
        }
    }
}


