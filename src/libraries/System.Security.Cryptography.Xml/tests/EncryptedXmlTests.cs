// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public static class EncryptedXmlTests
    {
        private const string AllowDangerousEncryptedXmlTransformsAppContextSwitch = "System.Security.Cryptography.Xml.AllowDangerousEncryptedXmlTransforms";

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49871", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51370", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public static void DecryptWithCertificate_NotInStore()
        {
            const string SecretMessage = "Grilled cheese is tasty";

            XmlDocument document = new XmlDocument();
            document.LoadXml($"<data><secret>{SecretMessage}</secret></data>");
            XmlElement toEncrypt = (XmlElement)document.DocumentElement.FirstChild;

            using (X509Certificate2 cert = TestHelpers.GetSampleX509Certificate())
            {
                EncryptedXml encryptor = new EncryptedXml(document);
                EncryptedData encryptedElement = encryptor.Encrypt(toEncrypt, cert);
                EncryptedXml.ReplaceElement(toEncrypt, encryptedElement, false);

                XmlDocument document2 = new XmlDocument();
                document2.LoadXml(document.OuterXml);

                EncryptedXml decryptor = new EncryptedXml(document2);

                Assert.Throws<CryptographicException>(() => decryptor.DecryptDocument());
                Assert.DoesNotContain(SecretMessage, document2.OuterXml);
            }
        }

#if NET // Remove once netfx has been serviced
        [Fact]
        public static void EncryptedXml_RecursiveKey_Default()
        {
            EncryptedXml_RecursiveKey(allowDangerousTransform: false);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public static void EncryptedXml_RecursiveKey_AppContext(bool allowDangerousTransform)
        {
            RemoteExecutor.Invoke(static (string allowDangerousTransformStr) =>
            {
                bool allowDangerousTransform = bool.Parse(allowDangerousTransformStr);
                AppContext.SetSwitch(AllowDangerousEncryptedXmlTransformsAppContextSwitch, allowDangerousTransform);
                EncryptedXml_RecursiveKey(allowDangerousTransform);
            }, allowDangerousTransform.ToString()).Dispose();
        }

        private static void EncryptedXml_RecursiveKey(bool allowDangerousTransform)
        {
            using Aes aes = Aes.Create();

            // Craft the recursive key payload

            XmlDocument doc = new();
            XmlElement dummy = doc.CreateElement("Dummy");
            dummy.SetAttribute("Id", "dummy");
            XmlDsigXPathTransform xpath = new();
            XmlDocument xpathDoc = new();
            xpathDoc.LoadXml("<XPath xmlns:ds='http://www.w3.org/2000/09/xmldsig#'>self::text()</XPath>");
            xpath.LoadInnerXml(xpathDoc.ChildNodes);

            EncryptedData recursiveED = new()
            {
                Type = EncryptedXml.XmlEncElementUrl,
                EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url),
                CipherData = new CipherData
                {
                    CipherReference = new("#dummy"),
                }
            };

            recursiveED.KeyInfo.AddClause(new KeyInfoName("recursiveKey"));
            recursiveED.CipherData.CipherReference.AddTransform(xpath);
            recursiveED.CipherData.CipherReference.AddTransform(new XmlDsigBase64Transform());

            XmlElement recursiveEDElem = recursiveED.GetXml();
            string payloadXml = recursiveEDElem.OuterXml;
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadXml);

            EncryptedXml exml = new()
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.ISO10126
            };

            byte[] cipherValue = exml.EncryptData(payloadBytes, aes);
            dummy.InnerText = Convert.ToBase64String(cipherValue);

            XmlElement root = doc.CreateElement("Root");
            doc.AppendChild(root);
            root.AppendChild(dummy); // Append the Base64 encoded ciphertext containing the recursive EncryptedData
            root.AppendChild(doc.ImportNode(recursiveEDElem, deep: true)); // Append the recursive EncryptedData element

            // Now attempt to decrypt
            EncryptedXml targetExml = new(doc)
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.ISO10126
            };

            targetExml.AddKeyNameMapping("recursiveKey", aes);

            XmlDecryptionTransform xdt = new()
            {
                EncryptedXml = targetExml
            };

            xdt.LoadInput(doc);

            // Act & assert
            CryptographicException ex = Assert.Throws<CryptographicException>(() => xdt.GetOutput());

            if (!allowDangerousTransform)
            {
                Assert.Equal("The specified cryptographic transform is not supported.", ex.Message);
            }
            else
            {
                Assert.Equal("The XML element has exceeded the maximum nesting depth allowed for decryption.", ex.Message);
            }
        }

        [Fact]
        public static void EncryptedKey_InfiniteLoopXsltTransform()
        {
            using RSA rsa = RSA.Create(2048);
            using Aes aes = Aes.Create();

            // Craft an encrypted key with an infinite XSLT transform
            XmlDocument doc = new();
            XmlElement root = doc.CreateElement("Root");
            doc.AppendChild(root);

            // Create a dummy element that the CipherReference will point to
            XmlElement dummy = doc.CreateElement("Dummy");
            dummy.SetAttribute("Id", "dummyId");
            dummy.InnerText = "input data";
            root.AppendChild(dummy);

            // Create the encrypted key with infinite XSLT transform.
            EncryptedData encryptedData = new()
            {
                Type = EncryptedXml.XmlEncElementUrl,
                EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url),
                CipherData = new CipherData
                {
                    CipherReference = new()
                    {
                        Uri = "#dummyId" // Points to local element
                    }
                }
            };

            XmlDsigXsltTransform xsltTransform = new();
            XmlDocument xsltDoc = new();
            xsltDoc.LoadXml("""
                <xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>
                <xsl:template match='/'>
                <xsl:call-template name='loop'/>
                </xsl:template>
                <xsl:template name='loop'>
                <xsl:text>A</xsl:text>
                <xsl:call-template name='loop'/>
                </xsl:template>
                </xsl:stylesheet>
                """);
            xsltTransform.LoadInnerXml(xsltDoc.ChildNodes);
            encryptedData.CipherData.CipherReference.AddTransform(xsltTransform);

            EncryptedKey ek = new()
            {
                EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncRSAOAEPUrl),
                CipherData = new CipherData
                {
                    CipherValue = EncryptedXml.EncryptKey(aes.Key, rsa, true),
                }
            };

            ek.KeyInfo.AddClause(new KeyInfoName("serverKey")); // KeyName for the RSA key
            encryptedData.KeyInfo.AddClause(new KeyInfoEncryptedKey(ek));

            XmlNode edElem = doc.ImportNode(encryptedData.GetXml(), true);
            root.AppendChild(edElem);

            // Attempt to decrypt the document
            EncryptedXml exml = new EncryptedXml(doc);
            exml.AddKeyNameMapping("serverKey", rsa);

            CryptographicException ex = Assert.Throws<CryptographicException>(exml.DecryptDocument);
            Assert.Equal("The specified cryptographic transform is not supported.", ex.Message);
        }

        [Fact]
        public static void EncryptedXml_BillionLaughsXsltTransform()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml($"""
                <Root>
                  <Data Id="data">Sensitive Data</Data>
                  <EncryptedData Type="http://www.w3.org/2001/04/xmlenc#Element" xmlns="http://www.w3.org/2001/04/xmlenc#">
                    <EncryptionMethod Algorithm="http://www.w3.org/2001/04/xmlenc#aes128-cbc" />
                    <KeyInfo xmlns="http://www.w3.org/2000/09/xmldsig#">
                      <KeyName>mykey</KeyName>
                    </KeyInfo>
                    <CipherData>
                      <CipherReference URI="#data">
                        <Transforms>
                          <Transform Algorithm="http://www.w3.org/TR/1999/REC-xslt-19991116" xmlns="http://www.w3.org/2000/09/xmldsig#">
                            {GenerateBillionLaughsXSLT()}
                          </Transform>
                        </Transforms>
                      </CipherReference>
                    </CipherData>
                  </EncryptedData>
                </Root>
                """);

            EncryptedXml exml = new EncryptedXml(doc);
            exml.Padding = PaddingMode.None;

            using Aes aes = Aes.Create();
            exml.AddKeyNameMapping("mykey", aes);
            CryptographicException ex = Assert.Throws<CryptographicException>(exml.DecryptDocument);
            Assert.Equal("The specified cryptographic transform is not supported.", ex.Message);

            static string GenerateBillionLaughsXSLT()
            {
                // 32 chars
                string vars = $"""<xsl:variable name="v0" select="'XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX'"/>{Environment.NewLine}""";

                // 32 * 2^28 is roughly 8GB
                int iterations = 28;
                for (int i = 1; i <= iterations; i++)
                {
                    vars += $"""<xsl:variable name="v{i}" select="concat($v{i - 1}, $v{i - 1})"/>{Environment.NewLine}""";
                }

                return $"""
                    <xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
                    <xsl:template match="/">
                    {vars}
                    <xsl:value-of select="$v{iterations}"/>
                    </xsl:template>
                    </xsl:stylesheet>
                    """;
            }
        }

        [Fact]
        public static void EncryptedXml_LoadDeepFile()
        {
            IO.Stream deepEncryptedXml = TestHelpers.LoadResourceStream("System.Security.Cryptography.Xml.Tests.EncryptedXmlSample4.xml");
            XmlDocument doc = new();
            doc.Load(deepEncryptedXml);

            EncryptedXml exml = new(doc);
            CryptographicException ex = Assert.Throws<CryptographicException>(() => exml.DecryptDocument());
            Assert.Equal("The XML element has exceeded the maximum nesting depth allowed for decryption.", ex.Message);
        }
#endif
    }
}
