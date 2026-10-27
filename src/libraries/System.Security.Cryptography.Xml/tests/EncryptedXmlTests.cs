// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        private const int DefaultMaxTransformsPerChain = 20;
        private const string MaxTransformsPerChainAppContextSwitch = "System.Security.Cryptography.Xml.MaxTransformsPerChain";
        private const string CryptoXmlAllowDtdProcessingAppContextSwitch = "System.Security.Cryptography.Xml.CryptoXmlAllowDtdProcessing";
        private const string CryptoXmlMaxInputDocumentDepthAppContextSwitch = "System.Security.Cryptography.Xml.CryptoXmlMaxInputDocumentDepth";
        internal const string MaxDecryptedDataElementsAppContextSwitch = "System.Security.Cryptography.Xml.MaxDecryptedDataElements";
        private const string DangerousMaxDecryptionCountAppContextSwitch = "System.Security.Cryptography.Xml.DangerousMaxDecryptionCount";
        private const string MalformedTransformsMessage = "Malformed element Transforms.";
        private const int DefaultMaxCount = 256;
        private const string MaxDecryptionCountExceededMessage = "The XML document has exceeded the maximum number of elements allowed for decryption.";

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

        [Fact]
        public static void EncryptedXml_DecryptedEncodedDtd()
        {
            // Keep the payload just above the default limit so deserialization fails
            // without executing an unnecessarily expensive transform chain.
            Assert.Equal(DefaultMaxTransformsPerChain + 1, GetEncodedDtdPayloadTransformCount());

            EncryptedXml encryptedXml = CreateEncryptedXmlWithEncodedDtdPayload();
            CryptographicException ex = Assert.Throws<CryptographicException>(() => encryptedXml.DecryptDocument());
            Assert.Equal(MalformedTransformsMessage, ex.Message);
        }

        public static IEnumerable<object[]> EncodedDtdTransformChainLimits()
        {
            int requiredTransformCount = GetEncodedDtdPayloadTransformCount();

            yield return new object[] { requiredTransformCount - 1, true };
            yield return new object[] { requiredTransformCount, false };
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/131638")]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(EncodedDtdTransformChainLimits))]
        public static void EncryptedXml_DecryptedEncodedDtd_WithExactTransformChainLimit(int maxTransformsPerChain, bool expectTransformLimit)
        {
            RemoteExecutor.Invoke(static (string maxTransformsPerChainText, string expectTransformLimitText) =>
            {
                AppContext.SetSwitch(CryptoXmlAllowDtdProcessingAppContextSwitch, true);
                AppContext.SetData(MaxTransformsPerChainAppContextSwitch, int.Parse(maxTransformsPerChainText, CultureInfo.InvariantCulture));

                EncryptedXml encryptedXml = CreateEncryptedXmlWithEncodedDtdPayload();
                CryptographicException ex = Assert.ThrowsAny<CryptographicException>(() => encryptedXml.DecryptDocument());

                if (bool.Parse(expectTransformLimitText))
                {
                    Assert.Equal(MalformedTransformsMessage, ex.Message);
                }
                else
                {
                    Assert.NotEqual(MalformedTransformsMessage, ex.Message);
                }
            }, maxTransformsPerChain.ToString(CultureInfo.InvariantCulture), expectTransformLimit.ToString()).Dispose();
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/131638")]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void EncryptedXml_DecryptedEncodedDtd_WithUnlimitedTransformChain()
        {
            // With the transform chain limit removed, the operation should still release
            // intermediate state so processing terminates without excessive memory usage.
            RemoteExecutor.Invoke(static () =>
            {
                AppContext.SetSwitch(CryptoXmlAllowDtdProcessingAppContextSwitch, true);
                AppContext.SetData(MaxTransformsPerChainAppContextSwitch, 0);

                EncryptedXml encryptedXml = CreateEncryptedXmlWithEncodedDtdPayload();
                CryptographicException ex = Assert.ThrowsAny<CryptographicException>(() => encryptedXml.DecryptDocument());
                Assert.NotEqual(MalformedTransformsMessage, ex.Message);
            }).Dispose();
        }

        private static int GetEncodedDtdPayloadTransformCount()
        {
            using Stream encryptedXmlStream = TestHelpers.LoadResourceStream("System.Security.Cryptography.Xml.Tests.EncryptedXmlSample5.xml");
            XmlDocument encryptedXmlDocument = new();
            encryptedXmlDocument.Load(encryptedXmlStream);

            XmlNamespaceManager namespaceManager = new(encryptedXmlDocument.NameTable);
            namespaceManager.AddNamespace("xenc", EncryptedXml.XmlEncNamespaceUrl);
            namespaceManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

            return encryptedXmlDocument.SelectNodes(
                "/xenc:EncryptedData/xenc:CipherData/xenc:CipherReference/xenc:Transforms/ds:Transform",
                namespaceManager)!.Count;
        }

        private static EncryptedXml CreateEncryptedXmlWithEncodedDtdPayload()
        {
            Stream encryptedXmlStream = TestHelpers.LoadResourceStream("System.Security.Cryptography.Xml.Tests.EncryptedXmlSample5.xml");
            XmlDocument encryptedXmlDocument = new();
            encryptedXmlDocument.Load(encryptedXmlStream);
            EncryptedXml encryptedXml = new(encryptedXmlDocument);

            Aes symAlg = Aes.Create();
            symAlg.KeySize = 128;
            encryptedXml.AddKeyNameMapping("TheKey", symAlg);

            return encryptedXml;
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void NestedParentPlusDeepDecryptedFragment_Throws()
        {
            RemoteExecutor.Invoke(static () =>
            {
                // ReplaceData mutates the containing document in-place, so the fragment-relative
                // depth check must include the target parent's ancestor depth. Placing the
                // EncryptedData under a wrapper chain that is already near the cap and
                // decrypting to a fragment near the cap must be rejected, not silently
                // produce a document deeper than the configured maximum.
                const int MaxDepth = 30;
                const int WrapperDepth = 20;
                const int FragmentDepth = 20;

                AppContext.SetData(CryptoXmlMaxInputDocumentDepthAppContextSwitch, MaxDepth);

                using Aes aes = Aes.Create();

                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml("<Root>" + BuildDeepMarkup(WrapperDepth, "<target/>") + "</Root>");
                XmlElement target = (XmlElement)doc.SelectSingleNode("//target");

                XmlDocument plaintextDoc = new XmlDocument { PreserveWhitespace = true };
                plaintextDoc.LoadXml("<x>" + BuildDeepMarkup(FragmentDepth, "y") + "</x>");
                EncryptedXml encryptor = new EncryptedXml();
                encryptor.AddKeyNameMapping("k", aes);
                EncryptedData ed = encryptor.Encrypt(plaintextDoc.DocumentElement, "k");
                EncryptedXml.ReplaceElement(target, ed, false);

                EncryptedXml decryptor = new EncryptedXml(doc);
                decryptor.AddKeyNameMapping("k", aes);
                Assert.Throws<CryptographicException>(() => decryptor.DecryptDocument());
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DeepNonRootDecryptedXml_Throws()
        {
            RemoteExecutor.Invoke(static () => Assert.Throws<CryptographicException>(() => DecryptDeepXml(root: false))).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DeepRootDecryptedXml_Throws()
        {
            RemoteExecutor.Invoke(static () => Assert.Throws<CryptographicException>(() => DecryptDeepXml(root: true))).Dispose();
        }

        private static void DecryptDeepXml(bool root)
        {
            const int Depth = 100000;
            XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
            string plaintext = "<target>" + BuildDeepMarkup(Depth, "x") + "</target>";
            doc.LoadXml(root ? plaintext : "<root><data>" + plaintext + "</data></root>");
            XmlElement target = root ? doc.DocumentElement : (XmlElement)doc.SelectSingleNode("//target");
            using (Aes aes = Aes.Create())
            {
                EncryptedXml encryptedXml = new EncryptedXml();
                encryptedXml.AddKeyNameMapping("k", aes);
                EncryptedXml.ReplaceElement(target, encryptedXml.Encrypt(target, "k"), false);

                EncryptedXml decryptor = new EncryptedXml(doc);
                decryptor.AddKeyNameMapping("k", aes);
                decryptor.DecryptDocument();
            }
        }

        private static string BuildDeepMarkup(int depth, string inner)
        {
            StringBuilder builder = new StringBuilder(depth * 7 + inner.Length);
            for (int i = 0; i < depth; i++)
                builder.Append("<a>");
            builder.Append(inner);
            for (int i = 0; i < depth; i++)
                builder.Append("</a>");
            return builder.ToString();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public static void DtdOptIn_EntityRefInDecryptedNonRoot_Resolves(bool disableDepthLimit)
        {
            RemoteExecutor.Invoke(static (string disableDepthLimitText) =>
            {
                // With DTD processing opted in, entity references defined by the containing
                // document's DTD must still resolve on the non-root decrypt path, matching
                // the root and stream re-parse paths. The depth guard's EntityReference
                // rejection is a default-safe measure and must not fire when the caller
                // has opted in.
                AppContext.SetSwitch(CryptoXmlAllowDtdProcessingAppContextSwitch, true);
                if (bool.Parse(disableDepthLimitText))
                {
                    AppContext.SetData(CryptoXmlMaxInputDocumentDepthAppContextSwitch, 0);
                }

                XmlDocument wire = new XmlDocument { PreserveWhitespace = true };
                using (System.IO.StringReader sr = new System.IO.StringReader("<!DOCTYPE root [<!ENTITY e \"x\">]><root><wrap></wrap></root>"))
                using (XmlReader xr = XmlReader.Create(sr, new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse }))
                {
                    wire.Load(xr);
                }
                XmlElement wrap = (XmlElement)wire.SelectSingleNode("//wrap");

                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    wrap.AppendChild(BuildEncryptedDataElement("<r>&e;</r>", aes, wire));
                    EncryptedXml decryptor = new EncryptedXml(wire);
                    decryptor.AddKeyNameMapping("k", aes);
                    decryptor.DecryptDocument();
                    Assert.Equal("x", wire.SelectSingleNode("//r").InnerText);
                }
            }, disableDepthLimit.ToString()).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DtdInDecryptedNonRoot_WithDepthDisabled_Throws()
        {
            RemoteExecutor.Invoke(static () =>
            {
                // Disabling the depth cap must not also disable DTD enforcement on the
                // non-root re-parse path.
                AppContext.SetData(CryptoXmlMaxInputDocumentDepthAppContextSwitch, 0);

                XmlDocument wire = new XmlDocument { PreserveWhitespace = true };
                wire.LoadXml("<root><wrap></wrap></root>");
                XmlElement wrap = (XmlElement)wire.SelectSingleNode("//wrap");

                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    // Non-root replacement: EncryptedData sits under <wrap>, so ReplaceData
                    // hits the "dummy" child branch that reparses via InnerXml.
                    wrap.AppendChild(BuildEncryptedDataElement("<r>&test;</r>", aes, wire));
                    EncryptedXml decryptor = new EncryptedXml(wire);
                    decryptor.AddKeyNameMapping("k", aes);
                    Assert.ThrowsAny<Exception>(() => decryptor.DecryptDocument());
                }
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DtdInDecryptedRoot_Throws()
        {
            RemoteExecutor.Invoke(static () => Assert.Throws<XmlException>(() => DecryptRootDtdPayload())).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DtdInDecryptedRoot_WithOptIn_Succeeds()
        {
            RemoteExecutor.Invoke(static () =>
            {
                AppContext.SetSwitch(CryptoXmlAllowDtdProcessingAppContextSwitch, true);
                DecryptRootDtdPayload();
            }).Dispose();
        }

        private static void DecryptRootDtdPayload()
        {
            const string dtdContent = "<!DOCTYPE r []><r>x</r>";
            XmlDocument wire = new XmlDocument { PreserveWhitespace = true };
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                wire.AppendChild(BuildEncryptedDataElement(dtdContent, aes, wire));
                EncryptedXml decryptor = new EncryptedXml(wire);
                decryptor.AddKeyNameMapping("k", aes);
                decryptor.DecryptDocument();
            }
        }

        private static XmlElement BuildEncryptedDataElement(string plaintext, Aes aes, XmlDocument targetDoc)
        {
            EncryptedXml helper = new EncryptedXml();
            helper.AddKeyNameMapping("k", aes);
            byte[] cipher = helper.EncryptData(Encoding.UTF8.GetBytes(plaintext), aes);
            EncryptedData encData = new EncryptedData
            {
                Type = EncryptedXml.XmlEncElementUrl,
                EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url),
                KeyInfo = new KeyInfo(),
                CipherData = new CipherData(cipher),
            };
            encData.KeyInfo.AddClause(new KeyInfoName("k"));
            XmlElement encXml = encData.GetXml();
            return (XmlElement)targetDoc.ImportNode(encXml, deep: true);
        }

        [Fact]
        public static void EncryptedXml_GetDecryptionKey_MaxCount_Default()
        {
            AssertKeyDecryptionCount(DefaultMaxCount, useGetDecryptionKey: true, operationCount: 2);
            CryptographicException exception = Assert.Throws<CryptographicException>(
                () => AssertKeyDecryptionCount(DefaultMaxCount + 1, useGetDecryptionKey: true));
            Assert.Equal(MaxDecryptionCountExceededMessage, exception.Message);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void EncryptedXml_GetDecryptionKey_MaxCount_AppContext()
        {
            RemoteExecutor.Invoke(static () =>
            {
                const int ConfiguredMaxCount = 1;
                AppContext.SetData(DangerousMaxDecryptionCountAppContextSwitch, ConfiguredMaxCount);

                AssertKeyDecryptionCount(ConfiguredMaxCount, useGetDecryptionKey: true);
                CryptographicException exception = Assert.Throws<CryptographicException>(
                    () => AssertKeyDecryptionCount(ConfiguredMaxCount + 1, useGetDecryptionKey: true));
                Assert.Equal(MaxDecryptionCountExceededMessage, exception.Message);
            }).Dispose();
        }

        [Fact]
        public static void EncryptedXml_DecryptEncryptedKey_MaxCount_Default()
        {
            AssertKeyDecryptionCount(DefaultMaxCount, useGetDecryptionKey: false, operationCount: 2);
            CryptographicException exception = Assert.Throws<CryptographicException>(
                () => AssertKeyDecryptionCount(DefaultMaxCount + 1, useGetDecryptionKey: false));
            Assert.Equal(MaxDecryptionCountExceededMessage, exception.Message);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void EncryptedXml_DecryptEncryptedKey_MaxCount_AppContext()
        {
            RemoteExecutor.Invoke(static () =>
            {
                const int ConfiguredMaxCount = 1;
                AppContext.SetData(DangerousMaxDecryptionCountAppContextSwitch, ConfiguredMaxCount);

                AssertKeyDecryptionCount(ConfiguredMaxCount, useGetDecryptionKey: false);
                CryptographicException exception = Assert.Throws<CryptographicException>(
                    () => AssertKeyDecryptionCount(ConfiguredMaxCount + 1, useGetDecryptionKey: false));
                Assert.Equal(MaxDecryptionCountExceededMessage, exception.Message);
            }).Dispose();
        }

        [Fact]
        public static void EncryptedXml_DecryptDocument_MaxCount_Default()
        {
            RemoteExecutor.Invoke(static () =>
            {
                // Make sure we don't hit the decrypt data count, which is a different counter.
                AppContext.SetData(MaxDecryptedDataElementsAppContextSwitch, 0);

                AssertDecryptDocumentCount(DefaultMaxCount / 2);
                CryptographicException exception = Assert.Throws<CryptographicException>(() => AssertDecryptDocumentCount(DefaultMaxCount / 2 + 1));
                Assert.Equal(MaxDecryptionCountExceededMessage, exception.Message);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void EncryptedXml_DecryptDocument_MaxCount_AppContext()
        {
            RemoteExecutor.Invoke(static () =>
            {
                const int ConfiguredMaxCount = 2;
                AppContext.SetData(DangerousMaxDecryptionCountAppContextSwitch, ConfiguredMaxCount);

                AssertDecryptDocumentCount(ConfiguredMaxCount / 2);
                CryptographicException exception = Assert.Throws<CryptographicException>(() => AssertDecryptDocumentCount(ConfiguredMaxCount / 2 + 1));
                Assert.Equal(MaxDecryptionCountExceededMessage, exception.Message);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void EncryptedXml_DecryptData_NestedDecryptionCountExceeded()
        {
            RemoteExecutor.Invoke(static () =>
            {
                AppContext.SetData(DangerousMaxDecryptionCountAppContextSwitch, 1);

                using Aes aes = Aes.Create();
                XmlDocument document = new();
                document.LoadXml("<root><cipher Id=\"cipher\" /></root>");

                EncryptedXml encryptedXml = new(document);
                byte[] cipherValue = encryptedXml.EncryptData("<secret />"u8.ToArray(), aes);
                CipherReference cipherReference = new("#cipher");
                cipherReference.AddTransform(new NestedDecryptionTransform(cipherValue));
                EncryptedData encryptedData = new()
                {
                    EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url),
                    CipherData = new CipherData(cipherReference),
                };

                CryptographicException exception = Assert.Throws<CryptographicException>(
                    () => encryptedXml.DecryptData(encryptedData, aes));
                Assert.Equal(MaxDecryptionCountExceededMessage, exception.Message);
            }).Dispose();
        }

        private static void AssertKeyDecryptionCount(int count, bool useGetDecryptionKey, int operationCount = 1)
        {
            const string EncNamespace = "http://www.w3.org/2001/04/xmlenc#";
            const string DsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

            using Aes contentKey = Aes.Create();
            using Aes keyEncryptionKey = Aes.Create();
            using Aes outerKey = Aes.Create();
            bool useNestedKey = count > 1;
            string outerCipher = Convert.ToBase64String(
                EncryptedXml.EncryptKey(contentKey.Key, useNestedKey ? outerKey : keyEncryptionKey));
            string innerCipher = Convert.ToBase64String(EncryptedXml.EncryptKey(outerKey.Key, keyEncryptionKey));

            StringBuilder builder = new(count * 200 + 4096);
            builder.Append("<root>");
            builder.Append("<EncryptedData xmlns=\"").Append(EncNamespace).Append("\">");
            builder.Append("<EncryptionMethod Algorithm=\"").Append(EncNamespace).Append("aes256-cbc\"/>");
            builder.Append("<KeyInfo xmlns=\"").Append(DsigNamespace).Append("\">");
            builder.Append("<EncryptedKey xmlns=\"").Append(EncNamespace).Append("\">");
            builder.Append("<EncryptionMethod Algorithm=\"").Append(EncNamespace).Append("kw-aes256\"/>");
            if (useNestedKey)
            {
                builder.Append("<KeyInfo xmlns=\"").Append(DsigNamespace).Append("\">");
                for (int i = 0; i < count - 2; i++)
                {
                    builder.Append("<EncryptedKey xmlns=\"").Append(EncNamespace).Append("\">");
                    builder.Append("<EncryptionMethod Algorithm=\"").Append(EncNamespace).Append("kw-aes256\"/>");
                    builder.Append("<KeyInfo xmlns=\"").Append(DsigNamespace).Append("\"><KeyName>no-such-key</KeyName></KeyInfo>");
                    builder.Append("<CipherData><CipherValue>AAAA</CipherValue></CipherData>");
                    builder.Append("</EncryptedKey>");
                }
                builder.Append("<EncryptedKey xmlns=\"").Append(EncNamespace).Append("\">");
                builder.Append("<EncryptionMethod Algorithm=\"").Append(EncNamespace).Append("kw-aes256\"/>");
                builder.Append("<KeyInfo xmlns=\"").Append(DsigNamespace).Append("\"><KeyName>kek</KeyName></KeyInfo>");
                builder.Append("<CipherData><CipherValue>").Append(innerCipher).Append("</CipherValue></CipherData>");
                builder.Append("</EncryptedKey>");
                builder.Append("</KeyInfo>");
            }
            else
            {
                builder.Append("<KeyInfo xmlns=\"").Append(DsigNamespace).Append("\"><KeyName>kek</KeyName></KeyInfo>");
            }
            builder.Append("<CipherData><CipherValue>").Append(outerCipher).Append("</CipherValue></CipherData>");
            builder.Append("</EncryptedKey>");
            builder.Append("</KeyInfo>");
            builder.Append("<CipherData><CipherValue>AAAA</CipherValue></CipherData>");
            builder.Append("</EncryptedData>");
            builder.Append("</root>");

            XmlDocument document = new() { PreserveWhitespace = true };
            document.LoadXml(builder.ToString());
            EncryptedData encryptedData = new();
            encryptedData.LoadXml((XmlElement)document.GetElementsByTagName("EncryptedData", EncNamespace)[0]);
            EncryptedKey encryptedKey = new();
            encryptedKey.LoadXml((XmlElement)document.GetElementsByTagName("EncryptedKey", EncNamespace)[0]);

            EncryptedXml encryptedXml = new(document);
            encryptedXml.AddKeyNameMapping("kek", keyEncryptionKey);

            for (int i = 0; i < operationCount; i++)
            {
                if (useGetDecryptionKey)
                {
                    using SymmetricAlgorithm decryptionKey = encryptedXml.GetDecryptionKey(encryptedData, null);
                    Assert.NotNull(decryptionKey);
                    Assert.Equal(contentKey.Key, decryptionKey.Key);
                }
                else
                {
                    Assert.Equal(contentKey.Key, encryptedXml.DecryptEncryptedKey(encryptedKey));
                }
            }
        }

        private static void AssertDecryptDocumentCount(int count)
        {
            using RSA rsa = RSA.Create(2048);
            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();

            XmlDocument seed = new();
            seed.LoadXml("<data>secret</data>");
            string encryptedDataCipher = Convert.ToBase64String(new EncryptedXml().EncryptData(seed.DocumentElement, aes, false));
            string encryptedKeyCipher = Convert.ToBase64String(EncryptedXml.EncryptKey(aes.Key, rsa, useOAEP: false));

            StringBuilder builder = new(count * 400);
            builder.Append("<root>");
            builder.Append("<EncryptedKey Id=\"k\" xmlns=\"http://www.w3.org/2001/04/xmlenc#\">");
            builder.Append("<EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#rsa-1_5\"/>");
            builder.Append("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><KeyName>rsaKey</KeyName></KeyInfo>");
            builder.Append("<CipherData><CipherValue>").Append(encryptedKeyCipher).Append("</CipherValue></CipherData>");
            builder.Append("</EncryptedKey>");
            for (int i = 0; i < count; i++)
            {
                builder.Append("<EncryptedData xmlns=\"http://www.w3.org/2001/04/xmlenc#\">");
                builder.Append("<EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#aes256-cbc\"/>");
                builder.Append("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\">");
                builder.Append("<RetrievalMethod URI=\"#k\" Type=\"http://www.w3.org/2001/04/xmlenc#EncryptedKey\"/></KeyInfo>");
                builder.Append("<CipherData><CipherValue>").Append(encryptedDataCipher).Append("</CipherValue></CipherData>");
                builder.Append("</EncryptedData>");
            }
            builder.Append("</root>");

            XmlDocument document = new() { PreserveWhitespace = true };
            document.LoadXml(builder.ToString());
            EncryptedXml encryptedXml = new(document);
            encryptedXml.AddKeyNameMapping("rsaKey", rsa);

            encryptedXml.DecryptDocument();
            Assert.Equal(0, document.GetElementsByTagName("EncryptedData", EncryptedXml.XmlEncNamespaceUrl).Count);
        }

        private sealed class NestedDecryptionTransform : Transform
        {
            private static readonly Type[] s_inputTypes = { typeof(Stream) };
            private static readonly Type[] s_outputTypes = { typeof(Stream) };
            private readonly byte[] _cipherValue;

            public NestedDecryptionTransform(byte[] cipherValue)
            {
                _cipherValue = cipherValue;
                Algorithm = SignedXml.XmlDecryptionTransformUrl;
            }

            public override Type[] InputTypes => s_inputTypes;

            public override Type[] OutputTypes => s_outputTypes;

            public override void LoadInnerXml(XmlNodeList nodeList)
            {
            }

            protected override XmlNodeList GetInnerXml()
            {
                return null;
            }

            public override void LoadInput(object obj)
            {
            }

            public override object GetOutput()
            {
                new EncryptedXml().DecryptEncryptedKey(new EncryptedKey());
                return new MemoryStream(_cipherValue);
            }

            public override object GetOutput(Type type)
            {
                return GetOutput();
            }
        }
#endif

        [Fact]
        public static void EncryptedKeyNameRescan_Completes()
        {
            using (Aes wrappingKey = Aes.Create())
            using (Aes dataKey = Aes.Create())
            {
                wrappingKey.KeySize = 256;
                dataKey.KeySize = 256;
                string xml = BuildEncryptedKeyRescanDocument(20, dataKey, wrappingKey);
                XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(xml);
                EncryptedXml encryptedXml = new EncryptedXml(doc);
                encryptedXml.AddKeyNameMapping("wrapping-key", wrappingKey);

                encryptedXml.DecryptDocument();

                Assert.Equal(20, doc.GetElementsByTagName("payload").Count);
            }
        }

        private static string BuildEncryptedKeyRescanDocument(int count, SymmetricAlgorithm dataKey, SymmetricAlgorithm wrappingKey)
        {
            XmlDocument scratch = new XmlDocument { PreserveWhitespace = true };
            scratch.LoadXml("<payload />");
            EncryptedXml helper = new EncryptedXml(scratch);
            string encryptedElement = Convert.ToBase64String(helper.EncryptData(scratch.DocumentElement, dataKey, false));
            string encryptedKey = Convert.ToBase64String(EncryptedXml.EncryptKey(dataKey.Key, wrappingKey));

            StringWriter writer = new StringWriter();
            writer.Write("<root xmlns:enc=\"");
            writer.Write(EncryptedXml.XmlEncNamespaceUrl);
            writer.Write("\" xmlns:ds=\"");
            writer.Write(SignedXml.XmlDsigNamespaceUrl);
            writer.Write("\">");
            for (int i = 0; i < count; i++)
            {
                writer.Write("<enc:EncryptedData Type=\"");
                writer.Write(EncryptedXml.XmlEncElementUrl);
                writer.Write("\"><enc:EncryptionMethod Algorithm=\"");
                writer.Write(EncryptedXml.XmlEncAES256Url);
                writer.Write("\" /><ds:KeyInfo><ds:KeyName>data-key</ds:KeyName></ds:KeyInfo><enc:CipherData><enc:CipherValue>");
                writer.Write(encryptedElement);
                writer.Write("</enc:CipherValue></enc:CipherData></enc:EncryptedData>");
            }
            writer.Write("<enc:EncryptedKey><enc:EncryptionMethod Algorithm=\"");
            writer.Write(EncryptedXml.XmlEncAES256KeyWrapUrl);
            writer.Write("\" /><ds:KeyInfo><ds:KeyName>wrapping-key</ds:KeyName></ds:KeyInfo><enc:CipherData><enc:CipherValue>");
            writer.Write(encryptedKey);
            writer.Write("</enc:CipherValue></enc:CipherData><enc:CarriedKeyName>data-key</enc:CarriedKeyName></enc:EncryptedKey></root>");
            return writer.ToString();
        }
    }
}
