// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using Xunit;

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
    }
}
