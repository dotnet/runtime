// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class SignedXml_Limits
    {
        private const int MaxTransformsPerReference = 10;
        private const int DefaultMaxReferencesPerSignedInfo = 100;
        private const string MaxReferencesPerSignedInfoAppContextSwitch = "System.Security.Cryptography.MaxReferencesPerSignedInfo";

        [Theory]
        [InlineData(1, 1, false)]
        [InlineData(MaxTransformsPerReference, 1, false)]
        [InlineData(MaxTransformsPerReference + 1, 1, true)]
        [InlineData(1, DefaultMaxReferencesPerSignedInfo, false)]
        [InlineData(1, DefaultMaxReferencesPerSignedInfo + 1, true)]
        [InlineData(MaxTransformsPerReference, DefaultMaxReferencesPerSignedInfo, false)]
        [InlineData(MaxTransformsPerReference, DefaultMaxReferencesPerSignedInfo + 1, true)]
        [InlineData(MaxTransformsPerReference + 1, DefaultMaxReferencesPerSignedInfo, true)]
        [InlineData(MaxTransformsPerReference + 1, DefaultMaxReferencesPerSignedInfo + 1, true)]
        public static void TestReferenceLimits(int numTransformsPerReference, int numReferencesPerSignedInfo, bool loadXmlThrows)
        {
            string xml = ConstructXml(numTransformsPerReference, numReferencesPerSignedInfo);
            Helpers.VerifyCryptoExceptionOnLoad(xml, loadXmlThrows, validSignature: false);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(2, "1", true)] // Can be configured below the default, if wanted; passes
        [InlineData(123, "123", false)] // Configured above default, exactly at the boundary; passes
        [InlineData(123, "124", false)] // Configured above default, below at the boundary; passes
        [InlineData(123, "122", true)] // Configured above default, above at the boundary; throws
        [InlineData(100, "hi", false)] // Config is bogus, exactly at the default; passes
        [InlineData(101, "hi", true)] // Config is bogus, above the default; throws
        [InlineData(101, "100.00", true)] // Config is incorrectly formatted and ignored, above the default; throws
        [InlineData(101, "0xFF", true)] // Config is incorrectly formatted and ignored, above the default; throws
        public static void MaxReferences_AppContextSwitch(int numReferencesPerSignedInfo, string appContextSwitchValue, bool loadXmlThrows)
        {
            // AppContext value is a string
            RemoteExecutor.Invoke(static (string numReferencesPerSignedInfo, string appContextSwitchValue, string loadXmlThrows) =>
            {
                AppContext.SetData(MaxReferencesPerSignedInfoAppContextSwitch, appContextSwitchValue);
                string xml = ConstructXml(1, int.Parse(numReferencesPerSignedInfo));
                Helpers.VerifyCryptoExceptionOnLoad(xml, bool.Parse(loadXmlThrows), validSignature: false);
            }, numReferencesPerSignedInfo.ToString(), appContextSwitchValue, loadXmlThrows.ToString()).Dispose();

            if (int.TryParse(appContextSwitchValue, out _))
            {
                // AppContext value is an int
                RemoteExecutor.Invoke(static (string numReferencesPerSignedInfo, string appContextSwitchValue, string loadXmlThrows) =>
                {
                    AppContext.SetData(MaxReferencesPerSignedInfoAppContextSwitch, int.Parse(appContextSwitchValue));
                    string xml = ConstructXml(1, int.Parse(numReferencesPerSignedInfo));
                    Helpers.VerifyCryptoExceptionOnLoad(xml, bool.Parse(loadXmlThrows), validSignature: false);
                }, numReferencesPerSignedInfo.ToString(), appContextSwitchValue, loadXmlThrows.ToString()).Dispose();
            }
        }

        private static string ConstructXml(int numTransformsPerReference, int numReferencesPerSignedInfo)
        {
            StringBuilder xml = new StringBuilder($@"<?xml version=""1.0"" encoding=""UTF-8""?>
<a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#dsa-sha1""/>");

            for (int i = 0; i < numReferencesPerSignedInfo; i++)
            {
                xml.Append($@"<Reference URI = """"><Transforms>");
                for (int j = 0; j < numTransformsPerReference; j++)
                {
                    xml.Append($@"<Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/>");
                }

                xml.Append($@"</Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference>");
            }

            xml.Append($@"</SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>");
            return xml.ToString();
        }
    }
}
