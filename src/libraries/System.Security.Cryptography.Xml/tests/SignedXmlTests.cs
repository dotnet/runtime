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
