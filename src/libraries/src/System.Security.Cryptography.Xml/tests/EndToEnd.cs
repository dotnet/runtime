using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class EndToEnd
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
            reference.AddTransform(new XmlDsigExcC14NTransform());

            signedXml.AddReference(reference);

            //XmlDsigExcC14NTransform
            //signedXml.SignedInfo.CanonicalizationMethod = typeof(XmlDsigExcC14NTransform).AssemblyQualifiedName;
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            signedXml.ComputeSignature(/*new HMACSHA256()*/);
            XmlElement xmlDigitalSignature = signedXml.GetXml();
            doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));
        }

        private static bool VerifyXml(string signedXmlText)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.LoadXml(signedXmlText);

            SignedXml signedXml = new SignedXml(xmlDoc);
            var signatureNode = (XmlElement)xmlDoc.GetElementsByTagName("Signature")[0];
            Console.WriteLine(signatureNode.OuterXml);
            signedXml.LoadXml(signatureNode);
            return signedXml.CheckSignature(new HMACSHA256());
        }

        // Implementation of MSDN samples:
        // Signing: https://msdn.microsoft.com/en-us/library/ms229745(v=vs.110).aspx
        // Verifying: https://msdn.microsoft.com/en-us/library/ms229745(v=vs.110).aspx
        [Fact]
        public void SignedXmlHasVerifiableSignature()
        {
            var cspParams = new CspParameters()
            {
                KeyContainerName = "XML_DSIG_RSA_KEY"
            };

            //cspParams.

            var rsaKey = new RSACryptoServiceProvider(cspParams);

            var xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.LoadXml(ExampleXml);
            SignXml(xmlDoc, rsaKey);

            //Console.WriteLine(xmlDoc.OuterXml);

            Assert.True(VerifyXml(xmlDoc.OuterXml));
        }
    }
}
