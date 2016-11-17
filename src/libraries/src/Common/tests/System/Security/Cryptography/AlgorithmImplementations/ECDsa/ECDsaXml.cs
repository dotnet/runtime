// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    public partial class ECDsaXml : ECDsaTestsBase
    {
        [ConditionalFact(nameof(ECDsa224Available))]
        public static void ExportLiteralMatch()
        {
            // Initialize with ECParameters, then test Xml output
            ECParameters imported = ECDsaTestData.GetNistP224KeyTestData();

            using (ECDsa ec = ECDsaFactory.Create())
            {
                ec.ImportParameters(imported);
                string ecXml = ec.ToXmlString(ECKeyXmlFormat.Rfc4050);
                string expectedXml = ECDsaTestData.GetNistP224Xml();

                Assert.Equal(expectedXml, ecXml);
            }
        }

        [ConditionalFact(nameof(ECDsa224Available))]
        public static void TestWhitespaceAndUnusedElements()
        {
            // Modify the existing test data by adding whitespace and additional elements.
            StringBuilder sb = new StringBuilder();
            string imported = ECDsaTestData.GetNistP224Xml();
            for (int i = 0; i < imported.Length; i++)
            {
                sb.Append(imported[i]);

                if (imported[i] == '>')
                {
                    sb.Append(' ');
                    sb.Append(Environment.NewLine);
                }
            }
            string tweakedImported = sb.ToString();

            tweakedImported.Replace("<Y Value", "<ExtraStuff>x</ExtraStuff><Y Value");

            using (ECDsa ec = ECDsaFactory.Create())
            {
                ec.FromXmlString(tweakedImported, ECKeyXmlFormat.Rfc4050);
                string exported = ec.ToXmlString(ECKeyXmlFormat.Rfc4050);
                Assert.Equal(imported, exported);
            }
        }

        [Fact]
        public static void TestFromXml_BadInput()
        {
            using (ECDsa ec = ECDsaFactory.Create())
            {
                Assert.Throws<NotImplementedException>(() => ec.ToXmlString(true));
                Assert.Throws<NotImplementedException>(() => ec.FromXmlString(string.Empty));

                Assert.Throws<ArgumentOutOfRangeException>(() => ec.ToXmlString((ECKeyXmlFormat)999));
                Assert.Throws<ArgumentOutOfRangeException>(() => ec.FromXmlString(string.Empty, (ECKeyXmlFormat)999));

                Assert.Throws<ArgumentNullException>(() => ec.FromXmlString(null, ECKeyXmlFormat.Rfc4050));

                // Test for missing values
                Assert.Throws<ArgumentException>(() => ec.FromXmlString(
                    "<ECDSAKeyValue xmlns=\"http://www.w3.org/2001/04/xmldsig-more#\">" +
                    "<DomainParameters></DomainParameters></ECDSAKeyValue>",
                    ECKeyXmlFormat.Rfc4050));

                // Test for invalid OID
                Assert.Throws<PlatformNotSupportedException>(() => ec.FromXmlString(
                    "<ECDSAKeyValue xmlns=\"http://www.w3.org/2001/04/xmldsig-more#\"><DomainParameters>" +
                    "<NamedCurve URN=\"urn:oid:INVALID\"/></DomainParameters><PublicKey>" +
                    "<X Value=\"75190203480250265324918629466525629790629130339101094484563787968\" " +
                    "xsi:type=\"PrimeFieldElemType\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"/>" +
                    "<Y Value=\"13934987398491338668904233989939116029337323268225038983559815153778\" " +
                    "xsi:type=\"PrimeFieldElemType\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"/>" +
                    "</PublicKey></ECDSAKeyValue>",
                    ECKeyXmlFormat.Rfc4050));

                // Test for invalid X parameter (raised from BigInteger.Parse)
                Assert.Throws<FormatException>(() => ec.FromXmlString(
                    "<ECDSAKeyValue xmlns=\"http://www.w3.org/2001/04/xmldsig-more#\"><DomainParameters>" +
                    "<NamedCurve URN=\"urn:oid:1.3.132.0.33\"/></DomainParameters><PublicKey>" +
                    "<X Value=\"blah\" " +
                    "xsi:type=\"PrimeFieldElemType\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"/>" +
                    "<Y Value=\"13934987398491338668904233989939116029337323268225038983559815153778\" " +
                    "xsi:type=\"PrimeFieldElemType\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"/>" +
                    "</PublicKey></ECDSAKeyValue>",
                    ECKeyXmlFormat.Rfc4050));

                // Test Xml parsing
                Assert.ThrowsAny<System.Xml.XmlException>(() => ec.FromXmlString(string.Empty, ECKeyXmlFormat.Rfc4050));
                Assert.ThrowsAny<System.Xml.XmlException>(() => ec.FromXmlString("garbage", ECKeyXmlFormat.Rfc4050));
            }
        }

        [ConditionalFact(nameof(ECDsa224Available))]
        public static void ImportRoundTrip_KnownValues()
        {
            string imported = ECDsaTestData.GetNistP224Xml();

            using (ECDsa ec = ECDsaFactory.Create())
            {
                ec.FromXmlString(imported, ECKeyXmlFormat.Rfc4050);
                string exported = ec.ToXmlString(ECKeyXmlFormat.Rfc4050);
                using (ECDsa ec2 = ECDsaFactory.Create())
                {
                    ec2.FromXmlString(exported, ECKeyXmlFormat.Rfc4050);
                    string exported2 = ec2.ToXmlString(ECKeyXmlFormat.Rfc4050);
                    Assert.Equal(exported, exported2);
                }
            }
        }

        [Fact]
        public static void ImportRoundTrip_GeneratedValues()
        {
            using (ECDsa ec = ECDsaFactory.Create())
            {
                string exported = ec.ToXmlString(ECKeyXmlFormat.Rfc4050);
                using (ECDsa ec2 = ECDsaFactory.Create())
                {
                    ec2.FromXmlString(exported, ECKeyXmlFormat.Rfc4050);
                    string exported2 = ec2.ToXmlString(ECKeyXmlFormat.Rfc4050);
                    Assert.Equal(exported, exported2);
                }
            }
        }

        [ConditionalFact(nameof(ECDsa224Available))]
        public static void ImportWithSignAndVerify()
        {
            byte[] data = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] sig;

            var sha1 = new HashAlgorithmName("SHA1");

            // Get the signature using full (non-xml) parameters that include private key
            using (ECDsa ec = ECDsaFactory.Create())
            {
                ec.ImportParameters(ECDsaTestData.GetNistP224KeyTestData());
                sig = ec.SignData(data, sha1);
            }

            using (ECDsa ec = ECDsaFactory.Create())
            {
                ec.FromXmlString(ECDsaTestData.GetNistP224Xml(), ECKeyXmlFormat.Rfc4050);
                // The xml paramters do not include the private key, so we can only verify here
                bool signatureMatched = ec.VerifyData(data, sig, sha1);
                Assert.True(signatureMatched);
            }
        }
    }
}
