// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Unit tests for XmlDecryptionTransform
//
// Author:
//  Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)

using System.IO;
using System.Text;
using System.Xml;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class UnprotectedXmlDecryptionTransform : XmlDecryptionTransform
    {
        public bool UnprotectedIsTargetElement(XmlElement inputElement, string idValue)
        {
            return base.IsTargetElement(inputElement, idValue);
        }

        public XmlNodeList UnprotectedGetInnerXml()
        {
            return base.GetInnerXml();
        }
    }

    public class XmlDecryptionTransformTest
    {
        private const string CryptoXmlMaxInputDocumentDepthAppContextSwitch = "System.Security.Cryptography.Xml.CryptoXmlMaxInputDocumentDepth";
        private const string DangerousMaxDecryptionCountAppContextSwitch = "System.Security.Cryptography.Xml.DangerousMaxDecryptionCount";
        private const string DangerousMaxRecursionDepthAppContextSwitch = "System.Security.Cryptography.Xml.DangerousMaxRecursionDepth";
        private const string MaxEncryptedDataElementsExceededMessage = "The number of EncryptedData elements processed during decryption has exceeded the maximum allowed for a single operation.";

        private UnprotectedXmlDecryptionTransform transform;

        public XmlDecryptionTransformTest()
        {
            transform = new UnprotectedXmlDecryptionTransform();
        }

        [Fact]
        public void IsTargetElement_XmlElementNull()
        {
            Assert.False(transform.UnprotectedIsTargetElement(null, "value"));
        }

        [Fact]
        public void IsTargetElement_StringNull()
        {
            XmlDocument doc = new XmlDocument();
            Assert.False(transform.UnprotectedIsTargetElement(doc.DocumentElement, null));
        }

        [Theory]
        [InlineData("<a id=\"1\" />", "1", true)]
        [InlineData("<a ID=\"1\" />", "1", true)]
        [InlineData("<a Id=\"1\" />", "1", true)]
        [InlineData("<a iD=\"1\" />", "1", false)]
        [InlineData("<a id=\"1\" />", "2", false)]
        public void IsTargetElement_ValidXml(string xml, string id, bool expectedResult)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            Assert.Equal(expectedResult, transform.UnprotectedIsTargetElement(doc.DocumentElement, id));
        }

        [Fact]
        public void AddExceptUri_Null()
        {
            Assert.Throws<ArgumentNullException>(() => transform.AddExceptUri(null));
        }

        [Fact]
        public void EncryptedXml_NotNull()
        {
            Assert.NotNull(transform.EncryptedXml);
        }

        [Fact]
        public void InputTypes()
        {
            Type[] inputTypes = transform.InputTypes;

            Assert.Equal(2, inputTypes.Length);
            Assert.Contains(typeof(Stream), inputTypes);
            Assert.Contains(typeof(XmlDocument), inputTypes);
        }

        [Fact]
        public void OutputTypes()
        {
            Type[] outputTypes = transform.OutputTypes;

            Assert.Equal(1, outputTypes.Length);
            Assert.Contains(typeof(XmlDocument), outputTypes);
        }

        [Fact]
        public void LoadInnerXml_XmlNull()
        {
            Assert.Throws<CryptographicException>(() => transform.LoadInnerXml(null));
        }

        [Fact]
        public void LoadInnerXml_XmlNoExcept()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<a />");

            Assert.Throws<CryptographicException>(() => transform.LoadInnerXml(doc.ChildNodes));

            Assert.Null(transform.UnprotectedGetInnerXml());
        }

        [Fact]
        public void LoadInnerXml_XmlNoUriForExcept()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(@"<dcrpt:Except xmlns:dcrpt=""http://www.w3.org/2002/07/decrypt#""/>");

            Assert.Throws<CryptographicException>(() => transform.LoadInnerXml(doc.ChildNodes));
        }

        [Fact]
        public void LoadInnerXml_XmlValidUriForExcept()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(@"<dcrpt:Except URI=""#item1"" xmlns:dcrpt=""http://www.w3.org/2002/07/decrypt#""/>");

            transform.LoadInnerXml(doc.ChildNodes);

            Assert.NotNull(transform.UnprotectedGetInnerXml());
        }

        [Fact]
        public void LoadStreamInput_CorrectXml()
        {
            XmlDocument doc = new XmlDocument();
            string xml = "<root><a /><b /></root>";
            doc.LoadXml(xml);

            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamWriter streamWriter = new StreamWriter(memoryStream, Text.Encoding.Unicode))
            {
                streamWriter.Write(xml);
                streamWriter.Flush();
                memoryStream.Position = 0;

                transform.LoadInput(memoryStream);
                XmlDocument output = (XmlDocument)transform.GetOutput();

                Assert.Equal(xml, output.OuterXml);
            }
        }

        [Fact]
        public void GetOutput_WrongType()
        {
            XmlDocument doc = new XmlDocument();
            string xml = "<test />";
            doc.LoadXml(xml);

            transform.LoadInput(doc);
            AssertExtensions.Throws<ArgumentException>("type", () => transform.GetOutput(typeof(string)));
        }

        [Fact]
        public void GetOutput_XmlNoEncryptedData()
        {
            XmlDocument doc = new XmlDocument();
            string xml = "<test />";
            doc.LoadXml(xml);

            transform.LoadInput(doc);
            XmlDocument transformedDocument = (XmlDocument)transform.GetOutput(typeof(XmlDocument));

            Assert.Equal(xml, transformedDocument.OuterXml);
        }

        [Fact]

        public void GetOutput_XmlWithEncryptedData()
        {
            XmlDocument doc = new XmlDocument();
            string xml = "<root><a /><b /><c>To Be Encrypted</c><d /></root>";
            doc.LoadXml(xml);

            XmlDocument transformedDocument = GetTransformedOutput(doc, "c");

            Assert.NotNull(transformedDocument);
            Assert.Equal(xml, transformedDocument.OuterXml);
        }

        [Fact]
        public void GetOutput_XmlWithEncryptedDataInRoot()
        {
            XmlDocument doc = new XmlDocument();
            string xml = "<root>To Be Encrypted</root>";
            doc.LoadXml(xml);

            XmlDocument transformedDocument = GetTransformedOutput(doc, "//root");

            Assert.NotNull(transformedDocument);
            Assert.Equal(xml, transformedDocument.OuterXml);
        }

        [Fact]
        public void GetOutput_XmlWithEncryptedDataAndExcept()
        {
            XmlDocument doc = new XmlDocument();
            string xml = "<root><a /><b /><c>To Be Encrypted</c><d /></root>";
            doc.LoadXml(xml);

            transform.AddExceptUri("#_notfound");
            transform.AddExceptUri("#_0");
            XmlDocument transformedDocument = GetTransformedOutput(doc, "c");

            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(doc.NameTable);
            xmlNamespaceManager.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);
            Assert.NotNull(transformedDocument.DocumentElement.SelectSingleNode("//enc:EncryptedData", xmlNamespaceManager));
            Assert.NotEqual(xml, transformedDocument.OuterXml);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(4, false)]
        [InlineData(5, true)]
        public void GetOutput_XmlWithNestedEncryptedData_EnforcesDepthLimit(int depth, bool shouldThrow)
        {
            RemoteExecutor.Invoke(static (string depthStr, string shouldThrowStr) =>
            {
                int depth = int.Parse(depthStr);
                bool shouldThrow = bool.Parse(shouldThrowStr);

                // Isolate recursive decryption depth so the independent input-document depth limit cannot mask the 3-pass/4-fail boundary.
                AppContext.SetData(CryptoXmlMaxInputDocumentDepthAppContextSwitch, 0);
                AppContext.SetData(DangerousMaxRecursionDepthAppContextSwitch, 3);

                if (shouldThrow)
                {
                    CryptographicException ex = Assert.Throws<CryptographicException>(() => DecryptNestedEncryptedDataDocument(depth));
                    Assert.Equal("The XML element has exceeded the maximum nesting depth allowed for decryption.", ex.Message);
                }
                else
                {
                    DecryptNestedEncryptedDataDocument(depth);
                }
            }, depth.ToString(), shouldThrow.ToString()).Dispose();
        }
#if NET
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetOutput_ExcludedEncryptedDataCandidates_ZeroDisablesLimit()
        {
            RemoteExecutor.Invoke(static () =>
            {
                AppContext.SetData(DangerousMaxDecryptionCountAppContextSwitch, 0);

                XmlDecryptionTransform transform = CreateTransformWithExcludedCandidates(257);
                Assert.NotNull(transform.GetOutput(typeof(XmlDocument)));
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetOutput_ExcludedEncryptedDataCandidates_ResetAfterException()
        {
            RemoteExecutor.Invoke(static () =>
            {
                const int MaxCandidateCount = 2;
                AppContext.SetData(DangerousMaxDecryptionCountAppContextSwitch, MaxCandidateCount);

                CryptographicException exception = Assert.Throws<CryptographicException>(
                    () => CreateTransformWithExcludedCandidates(MaxCandidateCount + 1).GetOutput());
                Assert.Equal(MaxEncryptedDataElementsExceededMessage, exception.Message);

                Assert.NotNull(CreateTransformWithExcludedCandidates(MaxCandidateCount).GetOutput());
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void GetOutput_RecursivelyRevealedEncryptedDataCandidate_CountsTowardLimit()
        {
            RemoteExecutor.Invoke(static () =>
            {
                AppContext.SetData(DangerousMaxDecryptionCountAppContextSwitch, 2);

                using Aes aes = Aes.Create();
                XmlDocument document = new XmlDocument();
                document.LoadXml(
                    $"<root><secret><EncryptedData xmlns=\"{EncryptedXml.XmlEncNamespaceUrl}\" Id=\"nested\" /></secret>" +
                    $"<EncryptedData xmlns=\"{EncryptedXml.XmlEncNamespaceUrl}\" Id=\"excluded\" /></root>");
                EncryptedXml encryptedXml = new EncryptedXml(document);
                encryptedXml.AddKeyNameMapping("aes", aes);

                XmlElement secret = (XmlElement)document.GetElementsByTagName("secret")[0];
                EncryptedData encryptedData = encryptedXml.Encrypt(secret, "aes");
                EncryptedXml.ReplaceElement(secret, encryptedData, content: false);

                XmlDecryptionTransform transform = new XmlDecryptionTransform
                {
                    EncryptedXml = encryptedXml,
                };
                transform.AddExceptUri("excluded");
                transform.LoadInput(document);

                CryptographicException exception = Assert.Throws<CryptographicException>(transform.GetOutput);
                Assert.Equal(MaxEncryptedDataElementsExceededMessage, exception.Message);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetOutput_EncryptedDataCountExceeded()
        {
            RemoteExecutor.Invoke(static () =>
            {
                AppContext.SetData("System.Security.Cryptography.Xml.DangerousMaxDecryptionCount", 2);

                using Aes aes = Aes.Create();
                XmlDocument document = new XmlDocument();
                document.LoadXml("<root><first>one</first><second>two</second></root>");
                EncryptedXml encryptedXml = new EncryptedXml(document);
                encryptedXml.AddKeyNameMapping("aes", aes);

                foreach (string elementName in new[] { "first", "second" })
                {
                    XmlElement element = (XmlElement)document.GetElementsByTagName(elementName)[0];
                    EncryptedData encryptedData = encryptedXml.Encrypt(element, "aes");
                    EncryptedXml.ReplaceElement(element, encryptedData, content: false);
                }

                XmlDecryptionTransform transform = new XmlDecryptionTransform
                {
                    EncryptedXml = encryptedXml,
                };
                transform.LoadInput(document);

                CryptographicException exception = Assert.Throws<CryptographicException>(transform.GetOutput);
                Assert.Equal(MaxEncryptedDataElementsExceededMessage, exception.Message);
            }).Dispose();
        }

        private static XmlDecryptionTransform CreateTransformWithExcludedCandidates(int candidateCount)
        {
            XmlDocument document = new XmlDocument();
            XmlElement root = document.CreateElement("root");
            document.AppendChild(root);

            XmlDecryptionTransform transform = new XmlDecryptionTransform();
            for (int i = 0; i < candidateCount; i++)
            {
                string id = $"candidate-{i}";
                XmlElement candidate = document.CreateElement("EncryptedData", EncryptedXml.XmlEncNamespaceUrl);
                candidate.SetAttribute("Id", id);
                root.AppendChild(candidate);
                transform.AddExceptUri(id);
            }

            transform.LoadInput(document);
            return transform;
        }

#endif

        private XmlDocument GetTransformedOutput(XmlDocument doc, string nodeToEncrypt)
        {
            using (var aesAlgo = Aes.Create())
            {
                var encryptedXml = new EncryptedXml();
                encryptedXml.AddKeyNameMapping("aes", aesAlgo);
                XmlElement elementToEncrypt = (XmlElement)doc.DocumentElement.SelectSingleNode(nodeToEncrypt);
                EncryptedData encryptedData = encryptedXml.Encrypt(elementToEncrypt, "aes");
                EncryptedXml.ReplaceElement(elementToEncrypt, encryptedData, false);

                XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(doc.NameTable);
                xmlNamespaceManager.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);
                XmlElement encryptedNode = (XmlElement)doc.DocumentElement.SelectSingleNode("//enc:EncryptedData", xmlNamespaceManager);
                encryptedNode.SetAttribute("ID", "#_0");

                transform.LoadInput(doc);
                transform.EncryptedXml = encryptedXml;
                XmlDocument transformedDocument = (XmlDocument)transform.GetOutput();

                transform.EncryptedXml = null;

                return transformedDocument;
            }
        }

        private static void DecryptNestedEncryptedDataDocument(int depth)
        {
            XmlDocument document = new();
            XmlElement current = document.CreateElement("Level0");
            document.AppendChild(current);

            XmlElement[] levels = new XmlElement[depth + 1];
            levels[0] = current;

            for (int i = 1; i <= depth; i++)
            {
                XmlElement child = document.CreateElement($"Level{i}");
                current.AppendChild(child);
                levels[i] = child;
                current = child;
            }

            current.InnerText = "payload";

            using Aes aes = Aes.Create();

            EncryptedXml encryptedXml = new();
            encryptedXml.AddKeyNameMapping("aes", aes);

            for (int i = depth; i >= 0; i--)
            {
                EncryptedData encryptedData = encryptedXml.Encrypt(levels[i], "aes");
                EncryptedXml.ReplaceElement(levels[i], encryptedData, false);
            }

            XmlDecryptionTransform decryptionTransform = new();
            decryptionTransform.LoadInput(document);
            decryptionTransform.EncryptedXml = encryptedXml;

            XmlDocument transformedDocument = (XmlDocument)decryptionTransform.GetOutput();

            Assert.Equal("payload", transformedDocument.DocumentElement!.InnerText);
        }
#if NET
        private const int DeepInputDepth = 100000;

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DeepStreamInput_LoadInput_Throws()
        {
            RemoteExecutor.Invoke(static () =>
            {
                byte[] payload = Encoding.UTF8.GetBytes(BuildDeepXml(DeepInputDepth));
                Assert.Throws<CryptographicException>(() =>
                {
                    using MemoryStream ms = new MemoryStream(payload);
                    XmlDecryptionTransform transform = new XmlDecryptionTransform();
                    transform.LoadInput(ms);
                    transform.GetOutput();
                });
            }).Dispose();
        }

        private static string BuildDeepXml(int depth)
        {
            StringBuilder sb = new StringBuilder(depth * 7 + 1);
            for (int i = 0; i < depth; i++)
                sb.Append("<a>");
            sb.Append('x');
            for (int i = 0; i < depth; i++)
                sb.Append("</a>");
            return sb.ToString();
        }

        private const string DtdPayload = "<!DOCTYPE r []><r>x</r>";
        private const string CryptoXmlAllowDtdProcessingAppContextSwitch = "System.Security.Cryptography.Xml.CryptoXmlAllowDtdProcessing";

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DtdStreamInput_LoadInput_Throws()
        {
            RemoteExecutor.Invoke(static () =>
            {
                Assert.Throws<XmlException>(() =>
                {
                    using MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(DtdPayload));
                    XmlDecryptionTransform transform = new XmlDecryptionTransform();
                    transform.LoadInput(ms);
                    transform.GetOutput();
                });
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DtdStreamInput_LoadInput_WithOptIn_Succeeds()
        {
            RemoteExecutor.Invoke(static () =>
            {
                AppContext.SetSwitch(CryptoXmlAllowDtdProcessingAppContextSwitch, true);
                using MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(DtdPayload));
                XmlDecryptionTransform transform = new XmlDecryptionTransform();
                transform.LoadInput(ms);
                Assert.NotNull(transform.GetOutput());
            }).Dispose();
        }
#endif
    }
}
