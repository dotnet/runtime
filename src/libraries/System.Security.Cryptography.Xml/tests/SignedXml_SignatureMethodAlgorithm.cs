// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class SignedXml_SignatureMethodAlgorithm
    {
        [Fact]
        public static void TestDummySignatureAlgorithm()
        {
            string objectToConstruct = typeof(DummyClass).AssemblyQualifiedName;
            string xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
            <a><b xmlns:ns1=""http://www.contoso.com/"">X<Signature xmlns=""http://www.w3.org/2000/09/xmldsig#""><SignedInfo><CanonicalizationMethod Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/><SignatureMethod Algorithm=""{objectToConstruct}""/><Reference URI=""""><Transforms><Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature""/><Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315""/></Transforms><DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha1""/><DigestValue>ZVZLYkc1BAx+YtaqeYlxanb2cGI=</DigestValue></Reference></SignedInfo><SignatureValue>Kx8xs0of766gimu5girTqiTR5xoiWjN4XMx8uzDDhG70bIqpSzlhh6IA3iI54R5mpqCCPWrJJp85ps4jpQk8RGHe4KMejstbY6YXCfs7LtRPzkNzcoZB3vDbr3ijUSrbMk+0wTaZeyeYs8Z6cOicDIVN6bN6yC/Se5fbzTTCSmg=</SignatureValue><KeyInfo><KeyValue><RSAKeyValue><Modulus>ww2w+NbXwY/GRBZfFcXqrAM2X+P1NQoU+QEvgLO1izMTB8kvx1i/bodBvHTrKMwAMGEO4kVATA1f1Vf5/lVnbqiCLMJPVRZU6rWKjOGD28T/VRaIGywTV+mC0HvMbe4DlEd3dBwJZLIMUNvOPsj5Ua+l9IS4EoszFNAg6F5Lsyk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue></KeyValue></KeyInfo></Signature></b></a>";

            var xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.LoadXml(xml);

            var signatureNode = (XmlElement)xmlDoc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl)[0];

            SignedXml signedXml = new SignedXml(xmlDoc);
            signedXml.LoadXml(signatureNode);
            Assert.Throws<CryptographicException>(() => signedXml.CheckSignature());
        }

        public class DummyClass
        {
            public DummyClass()
            {
                Assert.Fail();
            }
        }
    }
}
