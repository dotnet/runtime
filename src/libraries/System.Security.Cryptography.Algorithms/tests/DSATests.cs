// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography.Dsa.Tests;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public partial class DSATests
    {
        private const int Sha1HashOutputSize = 20;

        [Fact]
        public static void TestParams_Counter_And_Seed()
        {
            using (var dsa = new DSAMock())
            {
                DSAParameters dsaParameters = DSATestData.GetDSA1024Params();

                // Add some mock Seed and Counter values
                dsaParameters.Seed = new byte[Sha1HashOutputSize];
                dsaParameters.Seed[0] = 100;
                dsaParameters.Counter = 258;
                dsa.ImportParameters(dsaParameters);

                // Verify ToXmlString generates correct base64 values
                string xml = dsa.ToXmlString(true);
                string seedXml = GetXmlParameter(xml, "Seed");
                Assert.Equal("ZAAAAAAAAAAAAAAAAAAAAAAAAAA=", seedXml);

                string counterXml = GetXmlParameter(xml, "PgenCounter");
                Assert.Equal("AQI=", counterXml);

                // Clear current values to esnure FromXmlString re-imports
                dsaParameters.Seed = null;
                dsaParameters.Counter = 0;
                dsa.ImportParameters(dsaParameters);

                // Verify FromXmlString can import Seed and Counter
                dsa.FromXmlString(xml);
                dsaParameters = dsa.ExportParameters(true);
                Assert.Equal(20, dsaParameters.Seed.Length);
                Assert.Equal(100, dsaParameters.Seed[0]);
                Assert.Equal(258, dsaParameters.Counter);

                // Verify exception semantics of an having Seed or PgenCounter empty
                string tweakedXml = ChangeXmlParameter(xml, "Seed", string.Empty);
                Assert.Throws<CryptographicException>(() => dsa.FromXmlString(tweakedXml));
                tweakedXml = ChangeXmlParameter(xml, "PgenCounter", string.Empty);
                Assert.Throws<CryptographicException>(() => dsa.FromXmlString(tweakedXml));
            }
        }

        private static string GetXmlParameter(string xml, string elementName)
        {
            int startTag = xml.IndexOf("<" + elementName + ">");
            Assert.True(startTag > 0);
            int endTag = xml.IndexOf("</" + elementName + ">");
            Assert.True(endTag > startTag);

            int startIndex = startTag + elementName.Length + 2;
            return xml.Substring(startIndex, endTag - startIndex);
        }

        private static string ChangeXmlParameter(string xml, string elementName, string elementValue)
        {
            int startTag = xml.IndexOf("<" + elementName + ">");
            Assert.True(startTag > 0);
            int endTag = xml.IndexOf("</" + elementName + ">");
            Assert.True(endTag > startTag);

            return xml.Substring(0, startTag + elementName.Length + 2) + elementValue + xml.Substring(endTag);
        }

        public class DSAMock : DSA
        {
            private DSAParameters _dsaParameters;

            public override DSAParameters ExportParameters(bool includePrivateParameters)
            {
                return _dsaParameters;
            }

            public override void ImportParameters(DSAParameters parameters)
            {
                _dsaParameters = parameters;
            }

            public override byte[] CreateSignature(byte[] hash)
            {
                throw new NotImplementedException();
            }

            public override bool VerifySignature(byte[] hash, byte[] signature)
            {
                throw new NotImplementedException();
            }
        }
    }
}
