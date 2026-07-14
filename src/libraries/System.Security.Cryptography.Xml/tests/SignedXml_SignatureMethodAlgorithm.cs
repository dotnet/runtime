// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class SignedXml_SignatureMethodAlgorithm
    {
        private const string MaxDecryptedDataElementsAppContextSwitch = "System.Security.Cryptography.Xml.MaxDecryptedDataElements";

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

#if NET
        [Fact]
        public void SignedXml_LargeTransformType_ThrowsCryptographicException()
        {
            StringBuilder hugeNestedType = new StringBuilder("Fake.Type");
            for (int i = 0; i < 100_000; i++)
            {
                hugeNestedType.Append("+A");
            }

            string xmlString = $"""
                <Signature xmlns="http://www.w3.org/2000/09/xmldsig#">
                    <SignedInfo>
                        <CanonicalizationMethod Algorithm="http://www.w3.org/TR/2001/REC-xml-c14n-20010315" />
                        <SignatureMethod Algorithm="http://www.w3.org/2001/04/xmldsig-more#rsa-sha256" />
                        <Reference URI="">
                        <Transforms>
                            <Transform Algorithm="{hugeNestedType}" />
                        </Transforms>
                        <DigestMethod Algorithm="http://www.w3.org/2001/04/xmlenc#sha256" />
                            <DigestValue>NOT_USED</DigestValue>
                        </Reference>
                    </SignedInfo>
                    <SignatureValue>NOT_USED</SignatureValue>
                </Signature>
                """;

            XmlDocument xmlDoc = new();
            xmlDoc.LoadXml(xmlString);

            var signatureNode = (XmlElement)xmlDoc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl)[0];

            SignedXml signedXml = new(xmlDoc);
            Assert.Throws<CryptographicException>(() => signedXml.LoadXml(signatureNode));
        }

        [Theory]
        [InlineData(101)]
        [InlineData(500)]
        [InlineData(1000)]
        public void SignedXml_LargeEncryptedElementList_ThrowsCryptographicException(int encryptedElementCount)
        {
            using RSA rsa = RSA.Create();
            using Aes aes = Aes.Create();

            SignedXml signedXml = BuildSignedXmlWithManyEncryptedElements(aes, rsa, encryptedElementCount);
            Assert.Throws<CryptographicException>(() => signedXml.CheckSignature(rsa));
        }

        [Theory]
        [InlineData(50)]
        [InlineData(99)]
        public void SignedXml_EncryptedElementListWithinDefaultLimit_DoesNotThrowCryptographicException(int encryptedElementCount)
        {
            using RSA rsa = RSA.Create();
            using Aes aes = Aes.Create();

            SignedXml signedXml = BuildSignedXmlWithManyEncryptedElements(aes, rsa, encryptedElementCount);

            // Document is processed fully without throwing a CryptographiException
            Assert.Throws<NullReferenceException>(() => signedXml.CheckSignature(rsa));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(150, 200, false)] // raise the limit above the default; 150 elements no longer trips the count cap
        [InlineData(99, 200, false)]  // raise the limit; counts well below the new cap still don't trip it
        [InlineData(50, 30, true)]    // lower the limit; 50 elements now exceed the configured cap
        [InlineData(10, 5, true)]     // lower the limit aggressively; even 10 elements exceed the cap
        [InlineData(9, 10, false)]    // boundary: count == limit - 1
        [InlineData(10, 10, true)]    // boundary: count == limit
        public void SignedXml_EncryptedElementListLimit_RespectsAppContextSwitch(
            int encryptedElementCount,
            int maxDecryptedDataElements,
            bool expectThrow)
        {
            RemoteExecutor.Invoke(static (string countText, string limitText, string expectThrowText) =>
            {
                int count = int.Parse(countText, CultureInfo.InvariantCulture);
                int limit = int.Parse(limitText, CultureInfo.InvariantCulture);
                bool shouldThrow = bool.Parse(expectThrowText);

                AppContext.SetData(MaxDecryptedDataElementsAppContextSwitch, limit);

                using RSA rsa = RSA.Create();
                using Aes aes = Aes.Create();

                SignedXml signedXml = BuildSignedXmlWithManyEncryptedElements(aes, rsa, count);

                if (shouldThrow)
                {
                    Assert.Throws<CryptographicException>(() => signedXml.CheckSignature(rsa));
                }
                else
                {
                    Assert.Throws<NullReferenceException>(() => signedXml.CheckSignature(rsa));
                }
            },
            encryptedElementCount.ToString(CultureInfo.InvariantCulture),
            maxDecryptedDataElements.ToString(CultureInfo.InvariantCulture),
            expectThrow.ToString(CultureInfo.InvariantCulture)).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void SignedXml_EncryptedElementListLimit_ZeroDisablesLimit()
        {
            RemoteExecutor.Invoke(static () =>
            {
                AppContext.SetData(MaxDecryptedDataElementsAppContextSwitch, 0);

                using RSA rsa = RSA.Create();
                using Aes aes = Aes.Create();

                // 200 sibling elements would normally exceed the default cap of 100 and throw a
                // CryptographicException. Setting the AppContext switch to 0 must disable the cap
                // entirely, mirroring how MaxTransformsPerChain treats 0 as "no limit".
                SignedXml signedXml = BuildSignedXmlWithManyEncryptedElements(aes, rsa, 200);

                // With the cap disabled, the new limit does not fire. The sibling repro surfaces
                // NullReferenceException due to the inflated-counter behavior documented in the
                // companion tests; the assertion confirms the failure is not CryptographicException.
                Assert.Throws<NullReferenceException>(() => signedXml.CheckSignature(rsa));
            }).Dispose();
        }

        private static SignedXml BuildSignedXmlWithManyEncryptedElements(SymmetricAlgorithm aes, RSA rsa, int encryptedElementCount)
        {
            XmlDocument document = new()
            {
                PreserveWhitespace = true,
            };

            document.LoadXml("<root />");
            XmlElement root = document.DocumentElement!;
            XmlElement payloads = document.CreateElement("payloads");
            root.AppendChild(payloads);

            AppendEncryptedElement(document, payloads, aes, "signed-payload");

            SignedXml signer = new(document)
            {
                SigningKey = rsa,
            };

            signer.EncryptedXml.AddKeyNameMapping("aes", aes);

            Reference reference = new("");
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDecryptionTransform());
            signer.AddReference(reference);
            signer.ComputeSignature();
            root.AppendChild(document.ImportNode(signer.GetXml(), deep: true));

            payloads.RemoveAll();
            for (int i = 0; i < encryptedElementCount; i++)
            {
                AppendEncryptedElement(document, payloads, aes, "payload-" + i);
            }

            SignedXml signedXml = new(document);
            signedXml.LoadXml((XmlElement)document.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl)[0]!);
            signedXml.EncryptedXml.AddKeyNameMapping("aes", aes);
            return signedXml;

            static void AppendEncryptedElement(XmlDocument document, XmlElement parent, SymmetricAlgorithm aes, string value)
            {
                XmlElement clear = document.CreateElement("clear");
                clear.InnerText = value;
                parent.AppendChild(clear);

                EncryptedXml encryptedXml = new(document);
                encryptedXml.AddKeyNameMapping("aes", aes);

                EncryptedData encryptedData = encryptedXml.Encrypt(clear, "aes");
                EncryptedXml.ReplaceElement(clear, encryptedData, content: false);
            }
        }
#endif
    }
}
