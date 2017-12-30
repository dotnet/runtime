// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class SigningVerifyingX509Cert
    {
        const string ExampleXml = @"<?xml version=""1.0""?>
<example>
<test>some text node</test>
</example>";
        
        private static bool SupportsNewRsaTypes => !PlatformDetection.IsFullFramework || PlatformDetection.IsNetfx462OrNewer;

        private static void SignXml(XmlDocument doc, AsymmetricAlgorithm key)
        {
            var signedXml = new SignedXml(doc)
            {
                SigningKey = key
            };

            // Note: Adding KeyInfo (KeyInfoX509Data) does not provide more security
            //       Signing with private key is enough

            var reference = new Reference();
            reference.Uri = "";
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);

            signedXml.ComputeSignature();

            XmlElement xmlDigitalSignature = signedXml.GetXml();
            doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));
        }

        private static bool VerifyXml(string signedXmlText, X509Certificate2 certificate)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.LoadXml(signedXmlText);

            SignedXml signedXml = new SignedXml(xmlDoc);
            var signatureNode = (XmlElement)xmlDoc.GetElementsByTagName("Signature")[0];
            signedXml.LoadXml(signatureNode);

            // Note: `verifySignatureOnly: true` should not be used in the production
            //       without providing application logic to verify the certificate.
            // This test bypasses certificate verification because:
            // - certificates expire - test should not be based on time
            // - we cannot guarantee that the certificate is trusted on the machine
            return signedXml.CheckSignature(certificate, verifySignatureOnly: true);
        }

        // https://github.com/dotnet/corefx/issues/19270
        [ConditionalFact(nameof(SupportsNewRsaTypes))]
        public void SignedXmlHasCertificateVerifiableSignature()
        {
            using (X509Certificate2 x509cert = TestHelpers.GetSampleX509Certificate())
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                xmlDoc.LoadXml(ExampleXml);

                using (RSA key = x509cert.GetRSAPrivateKey())
                {
                    SignXml(xmlDoc, key);
                }

                Assert.True(VerifyXml(xmlDoc.OuterXml, x509cert));
            }
        }
    }
}
