// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class SignedXml_Limits
    {
        private const int MaxTransformsPerReference = 10;
        private const int MaxReferencesPerSignedInfo = 100;

        [Theory]
        [InlineData(1, 1, false)]
        [InlineData(MaxTransformsPerReference, 1, false)]
        [InlineData(MaxTransformsPerReference + 1, 1, true)]
        [InlineData(1, MaxReferencesPerSignedInfo, false)]
        [InlineData(1, MaxReferencesPerSignedInfo + 1, true)]
        [InlineData(MaxTransformsPerReference, MaxReferencesPerSignedInfo, false)]
        [InlineData(MaxTransformsPerReference, MaxReferencesPerSignedInfo + 1, true)]
        [InlineData(MaxTransformsPerReference + 1, MaxReferencesPerSignedInfo, true)]
        [InlineData(MaxTransformsPerReference + 1, MaxReferencesPerSignedInfo + 1, true)]
        public static void TestReferenceLimits(int numTransformsPerReference, int numReferencesPerSignedInfo, bool loadXmlThrows)
        {
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#dsa-sha1""/>";
            for (int i = 0; i < numReferencesPerSignedInfo; i++)
            {
                xml += $@"<Reference URI = """"><Transforms>";
                for (int j = 0; j < numTransformsPerReference; j++)
                {
                    xml += $@"<Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/>";
                }
                xml += $@"</Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference>";
            }
            xml += $@"</SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";
            Helpers.VerifyCryptoExceptionOnLoad(xml, loadXmlThrows);
        }
    }
}
