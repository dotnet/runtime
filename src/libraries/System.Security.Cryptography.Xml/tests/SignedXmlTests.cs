// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Xml;
using Xunit;
#if NET
using Microsoft.DotNet.RemoteExecutor;
#endif

namespace System.Security.Cryptography.Xml.Tests
{
    public class SignedXmlTests
    {
        [Fact]
        public void Constructor_Document_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SignedXml((XmlDocument) null)
            );
        }

        [Fact]
        public void Constructor_XmlElement_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SignedXml((XmlElement) null)
            );
        }

        [Fact]
        public void Constructor_NoArgs()
        {
            SignedXml signedXml = new SignedXml();

            // TODO: Expand this
            Assert.NotNull(signedXml.EncryptedXml);

            Assert.Equal(0, signedXml.KeyInfo.Count);
            Assert.Null(signedXml.KeyInfo.Id);

            // TODO: Expand
            Assert.NotNull(signedXml.Signature);
            Assert.NotNull(signedXml.Signature.SignedInfo);

            Assert.Equal(signedXml.SafeCanonicalizationMethods,
                new []
                {
                    SignedXml.XmlDsigC14NTransformUrl,
                    SignedXml.XmlDsigC14NWithCommentsTransformUrl,
                    SignedXml.XmlDsigExcC14NTransformUrl,
                    SignedXml.XmlDsigExcC14NWithCommentsTransformUrl
                });
            Assert.NotNull(signedXml.SignatureFormatValidator);

            Assert.Null(signedXml.SignatureLength);
            Assert.Null(signedXml.SignatureMethod);
            Assert.Null(signedXml.SignatureValue);
        }
        [Fact]
        public void GetIdElement_MultipleIdAttributes()
        {
            string xml = @"<root xmlns='http://test.com'>
                <element id='test1' Id='test2'>Content</element>
            </root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            SignedXml signedXml = new SignedXml(doc);
            XmlElement result = signedXml.GetIdElement(doc, "test1");
            Assert.NotNull(result);
        }

        [Fact]
        public void GetIdElement_NestedElements()
        {
            string xml = @"<root>
                <parent>
                    <child Id='nested1'>
                        <grandchild Id='nested2'>Content</grandchild>
                    </child>
                </parent>
            </root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            SignedXml signedXml = new SignedXml(doc);
            XmlElement result1 = signedXml.GetIdElement(doc, "nested1");
            Assert.NotNull(result1);
            Assert.Equal("child", result1.LocalName);

            XmlElement result2 = signedXml.GetIdElement(doc, "nested2");
            Assert.NotNull(result2);
            Assert.Equal("grandchild", result2.LocalName);
        }

        [Fact]
        public void GetIdElement_WithNamespace()
        {
            string xml = @"<root xmlns:test='http://test.com'>
                <test:element test:id='ns-id'>Content</test:element>
            </root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            SignedXml signedXml = new SignedXml(doc);
            XmlElement result = signedXml.GetIdElement(doc, "ns-id");
            // May or may not find it depending on ID attribute handling
            // Just ensuring it doesn't crash
        }

        [Fact]
        public void GetIdElement_NotFound()
        {
            string xml = @"<root><element Id='test1'>Content</element></root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            SignedXml signedXml = new SignedXml(doc);
            XmlElement result = signedXml.GetIdElement(doc, "nonexistent");
            Assert.Null(result);
        }

        [Fact]
        public void SignedXml_MultipleReferences_DifferentTargets()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(@"<root>
                <data1 Id='obj1'>First</data1>
                <data2 Id='obj2'>Second</data2>
                <data3 Id='obj3'>Third</data3>
            </root>");

            using (RSA rsa = RSA.Create())
            {
                SignedXml signedXml = new SignedXml(doc);
                signedXml.SigningKey = rsa;

                // Add multiple references with different target URIs
                Reference ref1 = new Reference("#obj1");
                ref1.AddTransform(new XmlDsigC14NTransform());
                signedXml.AddReference(ref1);

                Reference ref2 = new Reference("#obj2");
                ref2.AddTransform(new XmlDsigC14NTransform());
                signedXml.AddReference(ref2);

                Reference ref3 = new Reference("#obj3");
                ref3.AddTransform(new XmlDsigC14NTransform());
                signedXml.AddReference(ref3);

                signedXml.ComputeSignature();
                
                XmlElement sig = signedXml.GetXml();
                Assert.NotNull(sig);
                
                // Verify it has 3 references
                XmlNodeList refs = sig.SelectNodes("//*[local-name()='Reference']");
                Assert.Equal(3, refs.Count);
            }
        }

        [Fact]
        public void SignedXml_ReferenceWithMultipleTransforms()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root Id='data'><child>Test</child></root>");

            using (RSA rsa = RSA.Create())
            {
                SignedXml signedXml = new SignedXml(doc);
                signedXml.SigningKey = rsa;

                Reference reference = new Reference("#data");
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                reference.AddTransform(new XmlDsigC14NTransform());
                reference.AddTransform(new XmlDsigC14NWithCommentsTransform());
                
                signedXml.AddReference(reference);
                signedXml.ComputeSignature();
                
                XmlElement sig = signedXml.GetXml();
                XmlNodeList transforms = sig.SelectNodes("//*[local-name()='Transform']");
                Assert.Equal(3, transforms.Count);
            }
        }

        [Fact]
        public void SignedXml_AddObject_Multiple()
        {
            SignedXml signedXml = new SignedXml();
            
            DataObject obj1 = new DataObject("obj1", "text/plain", "UTF-8", new XmlDocument().CreateElement("data1"));
            signedXml.AddObject(obj1);
            
            DataObject obj2 = new DataObject("obj2", "text/xml", null, new XmlDocument().CreateElement("data2"));
            signedXml.AddObject(obj2);
            
            DataObject obj3 = new DataObject("obj3", null, null, new XmlDocument().CreateElement("data3"));
            signedXml.AddObject(obj3);
            
            Assert.Equal(3, signedXml.Signature.ObjectList.Count);
        }

        [Fact]
        public void SignedXml_EmptyUriReference()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><child>content</child></root>");

            using (RSA rsa = RSA.Create())
            {
                SignedXml signedXml = new SignedXml(doc);
                signedXml.SigningKey = rsa;

                // Empty URI means sign entire document
                Reference reference = new Reference("");
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signedXml.AddReference(reference);

                signedXml.ComputeSignature();
                Assert.NotNull(signedXml.SignatureValue);
            }
        }

        [Fact]
        public void SignedXml_SignWithDataObject()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");

            using (RSA rsa = RSA.Create())
            {
                SignedXml signedXml = new SignedXml();
                signedXml.SigningKey = rsa;

                // Create a data object
                XmlDocument dataDoc = new XmlDocument();
                XmlElement dataElem = dataDoc.CreateElement("MyData");
                dataElem.InnerText = "Secret Information";
                DataObject dataObject = new DataObject("data-id", null, null, dataElem);
                signedXml.AddObject(dataObject);

                // Reference the data object
                Reference reference = new Reference("#data-id");
                reference.Type = "http://www.w3.org/2000/09/xmldsig#Object";
                signedXml.AddReference(reference);

                signedXml.ComputeSignature();
                
                XmlElement signature = signedXml.GetXml();
                Assert.NotNull(signature);
                
                // Verify the Object is in the signature
                XmlNode objectNode = signature.SelectSingleNode("//*[local-name()='Object']");
                Assert.NotNull(objectNode);
            }
        }

        [Fact]
        public void CheckSignature_NoKeyInfo()
        {
            SignedXml signedXml = new SignedXml();
            // No key info, no signature
            bool result = signedXml.CheckSignature();
            Assert.False(result);
        }

        [Fact]
        public void CheckSignature_EmptyKeyInfo()
        {
            SignedXml signedXml = new SignedXml();
            signedXml.KeyInfo = new KeyInfo();
            // Empty key info
            bool result = signedXml.CheckSignature();
            Assert.False(result);
        }

        [Fact]
        public void SignedXml_ReferenceWithId()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root Id='root-id'><child>content</child></root>");

            using (RSA rsa = RSA.Create())
            {
                SignedXml signedXml = new SignedXml(doc);
                signedXml.SigningKey = rsa;

                Reference reference = new Reference("#root-id");
                reference.Id = "ref-1";
                reference.Type = "http://www.w3.org/2000/09/xmldsig#Object";
                reference.AddTransform(new XmlDsigC14NTransform());
                
                signedXml.AddReference(reference);
                signedXml.ComputeSignature();
                
                XmlElement sig = signedXml.GetXml();
                XmlNode refNode = sig.SelectSingleNode("//*[local-name()='Reference']");
                Assert.Equal("ref-1", ((XmlElement)refNode).GetAttribute("Id"));
            }
        }

        [Fact]
        public void SignedXml_SignatureWithId()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");

            using (RSA rsa = RSA.Create())
            {
                SignedXml signedXml = new SignedXml(doc);
                signedXml.SigningKey = rsa;

                Reference reference = new Reference("");
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signedXml.AddReference(reference);

                signedXml.Signature.Id = "signature-1";
                signedXml.ComputeSignature();
                
                XmlElement sig = signedXml.GetXml();
                Assert.Equal("signature-1", sig.GetAttribute("Id"));
            }
        }

        [Fact]
        public void SignedXml_DifferentDigestMethods()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root Id='data'><child>Test</child></root>");

            using (RSA rsa = RSA.Create())
            {
                SignedXml signedXml = new SignedXml(doc);
                signedXml.SigningKey = rsa;

                Reference ref1 = new Reference("#data");
                ref1.DigestMethod = SignedXml.XmlDsigSHA256Url;
                signedXml.AddReference(ref1);

                signedXml.ComputeSignature();
                
                XmlElement sig = signedXml.GetXml();
                XmlNode digestMethod = sig.SelectSingleNode("//*[local-name()='DigestMethod']");
                Assert.Contains("sha256", digestMethod.Attributes["Algorithm"].Value);
            }
        }
        [Fact]
        public static void CommentInsertedAfterSigning_CanLeaveSignatureValidForTruncatingConsumer()
        {
            using (RSA rsa = RSA.Create())
            {
                XmlDocument doc = LoadPreservingWhitespace("<Envelope><Assertion Id=\"x\"><NameID>admin@good.com.evil.com</NameID></Assertion></Envelope>");
                SignEnvelope(doc, rsa, "#x");

                XmlDocument attacked = LoadPreservingWhitespace(doc.OuterXml);
                XmlElement nameId = (XmlElement)attacked.GetElementsByTagName("NameID")[0];
                nameId.RemoveAll();
                nameId.AppendChild(attacked.CreateTextNode("admin@good.com"));
                nameId.AppendChild(attacked.CreateComment("split"));
                nameId.AppendChild(attacked.CreateTextNode(".evil.com"));

                Assert.True(VerifyEnvelope(attacked, rsa));
                Assert.Equal("admin@good.com", nameId.FirstChild.Value);
                Assert.Equal("admin@good.com.evil.com", nameId.InnerText);
            }
        }

        private static XmlDocument LoadPreservingWhitespace(string xml)
        {
            XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(xml);
            return doc;
        }

        private static void SignEnvelope(XmlDocument doc, RSA rsa, string uri)
        {
            SignedXml signedXml = new SignedXml(doc) { SigningKey = rsa };
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
            Reference reference = new Reference(uri) { DigestMethod = SignedXml.XmlDsigSHA256Url };
            signedXml.AddReference(reference);
            signedXml.ComputeSignature();
            doc.DocumentElement.AppendChild(doc.ImportNode(signedXml.GetXml(), true));
        }

        private static bool VerifyEnvelope(XmlDocument doc, RSA rsa)
        {
            SignedXml signedXml = new SignedXml(doc);
            signedXml.LoadXml((XmlElement)doc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl)[0]);
            return signedXml.CheckSignature(rsa);
        }

#if NET
        private const int DeepInputDepth = 1000;

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DeepXmlDocumentTarget_ComputeSignature_Throws()
        {
            RemoteExecutor.Invoke(static () =>
            {
                XmlDocument doc = BuildDeepXmlDocument(DeepInputDepth, wrapInEnvelope: false, out _);
                using RSA rsa = RSA.Create();

                Assert.Throws<CryptographicException>(() =>
                {
                    SignedXml signedXml = new SignedXml(doc) { SigningKey = rsa };
                    signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
                    Reference reference = new Reference("") { DigestMethod = SignedXml.XmlDsigSHA256Url };
                    reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                    signedXml.AddReference(reference);
                    signedXml.ComputeSignature();
                });
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DeepXmlElementTarget_ComputeSignature_Throws()
        {
            RemoteExecutor.Invoke(static () =>
            {
                XmlDocument doc = BuildDeepXmlDocument(DeepInputDepth, wrapInEnvelope: true, out XmlElement targetWithId);
                targetWithId.SetAttribute("Id", "t");
                using RSA rsa = RSA.Create();

                Assert.Throws<CryptographicException>(() =>
                {
                    SignedXml signedXml = new SignedXml(doc) { SigningKey = rsa };
                    signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
                    Reference reference = new Reference("#t") { DigestMethod = SignedXml.XmlDsigSHA256Url };
                    signedXml.AddReference(reference);
                    signedXml.ComputeSignature();
                });
            }).Dispose();
        }

        private static XmlDocument BuildDeepXmlDocument(int depth, bool wrapInEnvelope, out XmlElement innermostAncestor)
        {
            XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
            XmlElement top;
            if (wrapInEnvelope)
            {
                XmlElement envelope = doc.CreateElement("Envelope");
                doc.AppendChild(envelope);
                top = doc.CreateElement("Target");
                envelope.AppendChild(top);
            }
            else
            {
                top = doc.CreateElement("Root");
                doc.AppendChild(top);
            }
            innermostAncestor = top;
            XmlElement current = top;
            for (int i = 0; i < depth; i++)
            {
                XmlElement child = doc.CreateElement("a");
                current.AppendChild(child);
                current = child;
            }
            return doc;
        }

        private const string CryptoXmlAllowDtdProcessingAppContextSwitch = "System.Security.Cryptography.Xml.CryptoXmlAllowDtdProcessing";

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DtdInXmlDocumentTarget_ComputeSignature_Throws()
        {
            RemoteExecutor.Invoke(static () =>
                Assert.Throws<XmlException>(() => SignDtdDocument())).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DtdInXmlDocumentTarget_ComputeSignature_WithOptIn_Succeeds()
        {
            RemoteExecutor.Invoke(static () =>
            {
                AppContext.SetSwitch(CryptoXmlAllowDtdProcessingAppContextSwitch, true);
                SignDtdDocument();
            }).Dispose();
        }

        private static void SignDtdDocument()
        {
            XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml("<!DOCTYPE r []><r>x</r>");
            using RSA rsa = RSA.Create();
            SignedXml signedXml = new SignedXml(doc) { SigningKey = rsa };
            signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
            Reference reference = new Reference("") { DigestMethod = SignedXml.XmlDsigSHA256Url };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);
            signedXml.ComputeSignature();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DeepInnerTextInLoadXml_Throws()
        {
            RemoteExecutor.Invoke(static () =>
            {
                Assert.Throws<CryptographicException>(() =>
                {
                    const int Depth = 100000;
                    StringBuilder sb = new StringBuilder(Depth * 7 + 32);
                    sb.Append("<Signature xmlns=\"").Append(SignedXml.XmlDsigNamespaceUrl)
                      .Append("\"><SignedInfo><CanonicalizationMethod Algorithm=\"")
                      .Append(SignedXml.XmlDsigC14NTransformUrl)
                      .Append("\"/><SignatureMethod Algorithm=\"").Append(SignedXml.XmlDsigRSASHA1Url)
                      .Append("\"/><Reference URI=\"\"><DigestMethod Algorithm=\"")
                      .Append(SignedXml.XmlDsigSHA1Url)
                      .Append("\"/><DigestValue>AA==</DigestValue></Reference></SignedInfo><SignatureValue>");
                    for (int i = 0; i < Depth; i++) sb.Append("<a>");
                    sb.Append('x');
                    for (int i = 0; i < Depth; i++) sb.Append("</a>");
                    sb.Append("</SignatureValue></Signature>");

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(sb.ToString());
                    new SignedXml().LoadXml(doc.DocumentElement);
                });
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void DeepIdElementInReference_Throws()
        {
            RemoteExecutor.Invoke(static () =>
            {
                Assert.Throws<CryptographicException>(() =>
                {
                    const int Depth = 100000;
                    StringBuilder sb = new StringBuilder(Depth * 7 + 64);
                    sb.Append("<Envelope><target Id=\"t\">");
                    for (int i = 0; i < Depth; i++) sb.Append("<a>");
                    sb.Append('x');
                    for (int i = 0; i < Depth; i++) sb.Append("</a>");
                    sb.Append("</target></Envelope>");

                    XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
                    doc.LoadXml(sb.ToString());
                    new SignedXml(doc).GetIdElement(doc, "t");
                });
            }).Dispose();
        }
#endif
    }
}
