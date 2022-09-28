// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public static class Helpers
    {
        public static void VerifyCryptoExceptionOnLoad(string xml, bool loadXmlThrows, bool validSignature = true, bool checkSignatureThrows = false)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.LoadXml(xml);

            var signatureNode = (XmlElement)xmlDoc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl)[0];

            SignedXml signedXml = new SignedXml(xmlDoc);
            if (loadXmlThrows)
                Assert.Throws<CryptographicException>(() => signedXml.LoadXml(signatureNode));
            else
                signedXml.LoadXml(signatureNode);

            if (!loadXmlThrows)
            {
                if (checkSignatureThrows)
                {
                    Assert.ThrowsAny<CryptographicException>(() => signedXml.CheckSignature());
                }
                else
                {
                    bool checkSigResult = signedXml.CheckSignature();
                    Assert.Equal(validSignature, checkSigResult);
                }
            }
        }
    }
}
