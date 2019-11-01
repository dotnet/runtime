// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    // Based on implementation of MSDN samples:
    // Signing: https://msdn.microsoft.com/en-us/library/ms229745(v=vs.110).aspx
    // Verifying: https://msdn.microsoft.com/en-us/library/ms229745(v=vs.110).aspx
    public class SigningAndVerifyingWithCustomSignatureMethod
    {
        const string ExampleXml = @"<?xml version=""1.0""?>
<example>
<test>some text node</test>
</example>";

        private static bool SupportsSha2Algorithms =>
            !PlatformDetection.IsFullFramework ||
            CryptoConfig.CreateFromName("http://www.w3.org/2001/04/xmldsig-more#rsa-sha384") as SignatureDescription != null;

        private static void SignXml(XmlDocument doc, RSA key, string signatureMethod, string digestMethod)
        {
            var signedXml = new SignedXml(doc)
            {
                SigningKey = key
            };

            signedXml.SignedInfo.SignatureMethod = signatureMethod;

            var reference = new Reference();
            reference.Uri = "";

            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.DigestMethod = digestMethod;

            signedXml.AddReference(reference);

            signedXml.ComputeSignature();
            XmlElement xmlDigitalSignature = signedXml.GetXml();
            doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));
        }

        private static bool VerifyXml(string signedXmlText, RSA key)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.LoadXml(signedXmlText);

            SignedXml signedXml = new SignedXml(xmlDoc);
            var signatureNode = (XmlElement)xmlDoc.GetElementsByTagName("Signature")[0];
            signedXml.LoadXml(signatureNode);
            return signedXml.CheckSignature(key);
        }

        // https://github.com/dotnet/corefx/issues/19269
        [ConditionalTheory(nameof(SupportsSha2Algorithms))]
        [InlineData("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256", "http://www.w3.org/2001/04/xmlenc#sha256")]
        [InlineData("http://www.w3.org/2001/04/xmldsig-more#rsa-sha384", "http://www.w3.org/2001/04/xmldsig-more#sha384")]
        [InlineData("http://www.w3.org/2001/04/xmldsig-more#rsa-sha512", "http://www.w3.org/2001/04/xmlenc#sha512")]
        [InlineData("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256", "http://www.w3.org/2001/04/xmlenc#sha512")]
        public void SignedXmlHasVerifiableSignature(string signatureMethod, string digestMethod)
        {
            using (RSA key = RSA.Create())
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.LoadXml(ExampleXml);
                SignXml(xmlDoc, key, signatureMethod, digestMethod);
                Assert.True(VerifyXml(xmlDoc.OuterXml, key));
            }
        }
    }
}
