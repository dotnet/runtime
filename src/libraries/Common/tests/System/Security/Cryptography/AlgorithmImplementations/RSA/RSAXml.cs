// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    public partial class RSAXml
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void ExportLiteralMatch(bool includePrivate)
        {
            // Initialize with RSAParameters, then test Xml output
            RSAParameters imported = TestData.RSA384Parameters;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.ImportParameters(imported);
                string rsaXml = rsa.ToXmlString(includePrivate);
                string expectedXml = TestData.GetRSA384Xml(includePrivate);

                if (includePrivate)
                {
                    if (!expectedXml.Equals(rsaXml, StringComparison.Ordinal))
                    {
                        // The D parameter can vary; strip off the D parameter and compare
                        expectedXml = StripOffParameterD(expectedXml);
                        rsaXml = StripOffParameterD(rsaXml);
                    }
                }

                Assert.Equal(expectedXml, rsaXml);
            }
        }

        [Fact]
        public static void TestWhitespaceAndUnusedElements()
        {
            // Modify the existing test data by adding whitespace and additional elements.
            StringBuilder sb = new StringBuilder();
            string imported = TestData.GetRSA384Xml(true);
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

            tweakedImported.Replace("<Exponent>", "<ExtraStuff>x</ExtraStuff><Exponent>");

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.FromXmlString(tweakedImported);
                string exported = rsa.ToXmlString(true);
                Assert.Equal(imported, exported);
            }
        }

        [Fact]
        public static void TestFromXml_BadInput()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                Assert.Throws<ArgumentNullException>(() => rsa.FromXmlString(null));

                // Test missing values; exception type may vary between Csp, Cng, etc
                Assert.ThrowsAny<CryptographicException>(() => rsa.FromXmlString("<RSAKeyValue></RSAKeyValue>"));
                Assert.ThrowsAny<CryptographicException>(() => rsa.FromXmlString("<RSAKeyValue><Exponent></Exponent></RSAKeyValue>"));

                // Test data parsing (Convert.FromBase64String)
                Assert.Throws<FormatException>(() => rsa.FromXmlString("<RSAKeyValue><Exponent>x</Exponent></RSAKeyValue>"));

                // Test Xml parsing; desktop clr uses different Xml processing classes so exceptions may vary.
                Assert.ThrowsAny<Exception>(() => rsa.FromXmlString(string.Empty));
                Assert.ThrowsAny<Exception>(() => rsa.FromXmlString("<RSAKeyValue>"));
                Assert.ThrowsAny<Exception>(() => rsa.FromXmlString("garbage"));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void ImportRoundTrip_KnownValues(bool includePrivate)
        {
            string imported = TestData.GetRSA384Xml(includePrivate);

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.FromXmlString(imported);
                string exported = rsa.ToXmlString(includePrivate);
                using (RSA rsa2 = RSAFactory.Create())
                {
                    rsa2.FromXmlString(exported);
                    string exported2 = rsa2.ToXmlString(includePrivate);
                    Assert.Equal(exported, exported2);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void ImportRoundTrip_GeneratedValues(bool includePrivate)
        {
            using (RSA rsa = RSAFactory.Create())
            {
                string exported = rsa.ToXmlString(includePrivate);
                using (RSA rsa2 = RSAFactory.Create())
                {
                    rsa2.FromXmlString(exported);
                    string exported2 = rsa2.ToXmlString(includePrivate);
                    Assert.Equal(exported, exported2);
                }
            }
        }

        [Fact]
        public static void ImportWithSignAndVerify()
        {
            byte[] data = TestData.HelloBytes;

            using (RSA rsa = RSAFactory.Create())
            {
                rsa.FromXmlString(TestData.GetRSA384Xml(true));
                byte[] signature = rsa.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                bool signatureMatched = rsa.VerifyData(data, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                Assert.True(signatureMatched);
            }
        }

        private static string StripOffParameterD(string xml)
        {
            int startTag = xml.IndexOf("<D>");
            Assert.True(startTag > 0);
            int endTag = xml.IndexOf("</D>");
            Assert.True(endTag > startTag);
            endTag += 4;
            Assert.True(endTag < xml.Length);

            return xml.Substring(0, startTag) + xml.Substring(endTag);
        }
    }
}
