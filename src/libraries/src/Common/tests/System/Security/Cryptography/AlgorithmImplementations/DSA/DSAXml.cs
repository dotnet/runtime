// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    public partial class DSAXml
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void ExportLiteralMatch(bool includePrivate)
        {
            // Initialize with DSAParameters, then test Xml output
            DSAParameters imported = DSATestData.GetDSA1024Params();

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(imported);
                string dsaXml = dsa.ToXmlString(includePrivate);
                string expectedXml = DSATestData.GetDSA1024Xml(includePrivate);

                Assert.Equal(expectedXml, dsaXml);
            }
        }

        [Fact]
        public static void TestWhitespaceAndUnusedElements()
        {
            // Modify the existing test data by adding whitespace and additional elements.
            StringBuilder sb = new StringBuilder();
            string imported = DSATestData.GetDSA1024Xml(true);
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

            tweakedImported.Replace("<P>", "<ExtraStuff>x</ExtraStuff><P>");

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.FromXmlString(tweakedImported);
                string exported = dsa.ToXmlString(true);
                Assert.Equal(imported, exported);
            }
        }

        [Fact]
        public static void TestFromXml_BadInput()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                Assert.Throws<ArgumentNullException>(() => dsa.FromXmlString(null));

                // Test missing values; exception type may vary between Csp, Cng, etc
                Assert.Throws<CryptographicException>(() => dsa.FromXmlString("<DSAKeyValue><P></P></DSAKeyValue>"));
                Assert.ThrowsAny<Exception>(() => dsa.FromXmlString("<DSAKeyValue></DSAKeyValue>"));

                // Test data parsing (Convert.FromBase64String)
                Assert.Throws<FormatException>(() => dsa.FromXmlString("<DSAKeyValue><P>x</P></DSAKeyValue>"));

                // Test Xml parsing; desktop clr uses different Xml processing classes so exceptions may vary.
                Assert.ThrowsAny<Exception>(() => dsa.FromXmlString(string.Empty));
                Assert.ThrowsAny<Exception>(() => dsa.FromXmlString("<DSAKeyValue>"));
                Assert.ThrowsAny<Exception>(() => dsa.FromXmlString("garbage"));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void ImportRoundTrip_KnownValues(bool includePrivate)
        {
            string imported = DSATestData.GetDSA1024Xml(includePrivate);

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.FromXmlString(imported);
                string exported = dsa.ToXmlString(includePrivate);
                using (DSA dsa2 = DSAFactory.Create())
                {
                    dsa2.FromXmlString(exported);
                    string exported2 = dsa2.ToXmlString(includePrivate);
                    Assert.Equal(exported, exported2);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void ImportRoundTrip_GeneratedValues(bool includePrivate)
        {
            using (DSA dsa = DSAFactory.Create())
            {
                string exported = dsa.ToXmlString(includePrivate);
                using (DSA dsa2 = DSAFactory.Create())
                {
                    dsa2.FromXmlString(exported);
                    string exported2 = dsa2.ToXmlString(includePrivate);
                    Assert.Equal(exported, exported2);
                }
            }
        }

        [Fact]
        public static void ImportWithSignAndVerify()
        {
            byte[] data = DSATestData.HelloBytes;

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.FromXmlString(DSATestData.GetDSA1024Xml(true));
                byte[] signature = dsa.SignData(data, HashAlgorithmName.SHA1);
                bool signatureMatched = dsa.VerifyData(data, signature, HashAlgorithmName.SHA1);
                Assert.True(signatureMatched);
            }
        }
    }
}
