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
    // Implementation of MSDN samples:
    // Signing: https://msdn.microsoft.com/en-us/library/ms229745(v=vs.110).aspx
    // Verifying: https://msdn.microsoft.com/en-us/library/ms229745(v=vs.110).aspx
    public class SigningAndVerifying
    {
        const string ExampleXml = @"<?xml version=""1.0""?>
<example>
<test>some text node</test>
</example>";

        private static void SignXml(XmlDocument doc, RSA key)
        {
            var signedXml = new SignedXml(doc)
            {
                SigningKey = key
            };

            var reference = new Reference();
            reference.Uri = "";

            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());

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

        [Fact]
        public void SignedXmlHasVerifiableSignature()
        {
            using (RSA key = RSA.Create())
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.LoadXml(ExampleXml);
                SignXml(xmlDoc, key);

                Assert.True(VerifyXml(xmlDoc.OuterXml, key));
            }
        }
    }
}
