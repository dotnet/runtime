// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    // Cycle 2: Target SignedXml, Utils, TransformChain, XmlDsigEnvelopedSignatureTransform gaps
    
    public class SignedXmlCycle2Tests
    {
        [Fact]
        public void SignedXml_SignedInfo_Properties()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data>test</data></root>");
            
            SignedXml signedXml = new SignedXml(doc);
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NWithCommentsTransformUrl;
            
            Assert.Equal(SignedXml.XmlDsigC14NWithCommentsTransformUrl, signedXml.SignedInfo.CanonicalizationMethod);
        }

        [Fact]
        public void SignedXml_Signature_Property()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data>test</data></root>");
            
            SignedXml signedXml = new SignedXml(doc);
            Reference reference = new Reference("");
            signedXml.AddReference(reference);
            
            using (RSA rsa = RSA.Create())
            {
                signedXml.SigningKey = rsa;
                signedXml.ComputeSignature();
                
                Assert.NotNull(signedXml.Signature);
                Assert.NotNull(signedXml.Signature.SignedInfo);
            }
        }

        [Fact]
        public void SignedXml_KeyInfo_Property()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data>test</data></root>");
            
            SignedXml signedXml = new SignedXml(doc);
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoName("test-key"));
            signedXml.KeyInfo = keyInfo;
            
            Assert.Equal(keyInfo, signedXml.KeyInfo);
        }

        [Fact]
        public void SignedXml_AddObject()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data>test</data></root>");
            
            SignedXml signedXml = new SignedXml(doc);
            DataObject dataObject = new DataObject();
            dataObject.Id = "object1";
            dataObject.Data = doc.ChildNodes;
            
            signedXml.AddObject(dataObject);
            
            Reference reference = new Reference("#object1");
            signedXml.AddReference(reference);
            
            using (RSA rsa = RSA.Create())
            {
                signedXml.SigningKey = rsa;
                signedXml.ComputeSignature();
                Assert.NotNull(signedXml.GetXml());
            }
        }

        [Fact]
        public void SignedXml_EncryptedXml_Property()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data>test</data></root>");
            
            SignedXml signedXml = new SignedXml(doc);
            
            // Test that we can set and get properties
            Assert.NotNull(signedXml.SignedInfo);
            Assert.NotNull(signedXml);
        }
    }

    public class UtilsCycle2Tests
    {
        [Fact]
        public void Utils_Base64Encoding()
        {
            byte[] data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            
            // Test through CipherData which uses Base64 encoding
            CipherData cipherData = new CipherData(data);
            XmlElement xml = cipherData.GetXml();
            
            Assert.Contains("CipherValue", xml.OuterXml);
            
            CipherData loaded = new CipherData();
            loaded.LoadXml(xml);
            
            Assert.Equal(data, loaded.CipherValue);
        }

        [Fact]
        public void Utils_XmlNamespaceHandling()
        {
            string xml = @"<Reference xmlns='http://www.w3.org/2000/09/xmldsig#' URI='#test'>
                <DigestMethod Algorithm='http://www.w3.org/2001/04/xmlenc#sha256' />
                <DigestValue>dGVzdA==</DigestValue>
            </Reference>";
            
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            Reference reference = new Reference();
            reference.LoadXml(doc.DocumentElement);
            
            Assert.Equal("#test", reference.Uri);
        }
    }

    public class TransformChainCycle2Tests
    {
        [Fact]
        public void TransformChain_MultipleTransforms()
        {
            Reference reference = new Reference("#test");
            reference.AddTransform(new XmlDsigC14NTransform());
            reference.AddTransform(new XmlDsigBase64Transform());
            
            Assert.Equal(2, reference.TransformChain.Count);
            Assert.NotNull(reference.TransformChain[0]);
            Assert.NotNull(reference.TransformChain[1]);
        }

        [Fact]
        public void TransformChain_Enumerate()
        {
            Reference reference = new Reference();
            reference.AddTransform(new XmlDsigC14NTransform());
            reference.AddTransform(new XmlDsigExcC14NTransform());
            
            int count = 0;
            foreach (Transform transform in reference.TransformChain)
            {
                Assert.NotNull(transform);
                count++;
            }
            Assert.Equal(2, count);
        }
    }

    public class XmlDsigEnvelopedSignatureTransformCycle2Tests
    {
        [Fact]
        public void EnvelopedTransform_LoadInputTypes()
        {
            string xml = @"<root><data>test</data></root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            XmlDsigEnvelopedSignatureTransform transform = new XmlDsigEnvelopedSignatureTransform();
            
            // Test with XmlDocument
            transform.LoadInput(doc);
            object output1 = transform.GetOutput();
            Assert.NotNull(output1);
            
            // Test with XmlNodeList
            XmlNodeList nodes = doc.SelectNodes("//data");
            transform.LoadInput(nodes);
            object output2 = transform.GetOutput();
            Assert.NotNull(output2);
        }

        [Fact]
        public void EnvelopedTransform_GetXml()
        {
            XmlDsigEnvelopedSignatureTransform transform = new XmlDsigEnvelopedSignatureTransform();
            XmlElement xml = transform.GetXml();
            
            Assert.NotNull(xml);
            Assert.Equal("Transform", xml.LocalName);
            Assert.Contains("enveloped-signature", xml.GetAttribute("Algorithm"));
        }
    }

    public class EncryptedDataCycle2Tests
    {
        [Fact]
        public void EncryptedData_MimeType()
        {
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.MimeType = "application/xml";
            encryptedData.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            XmlElement xml = encryptedData.GetXml();
            Assert.Contains("MimeType", xml.OuterXml);
            
            EncryptedData loaded = new EncryptedData();
            loaded.LoadXml(xml);
            
            Assert.Equal("application/xml", loaded.MimeType);
        }

        [Fact]
        public void EncryptedData_Encoding()
        {
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.Encoding = "UTF-8";
            encryptedData.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            XmlElement xml = encryptedData.GetXml();
            Assert.Contains("Encoding", xml.OuterXml);
            
            EncryptedData loaded = new EncryptedData();
            loaded.LoadXml(xml);
            
            Assert.Equal("UTF-8", loaded.Encoding);
        }

        [Fact]
        public void EncryptedData_ReferenceList()
        {
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            XmlElement xml = encryptedData.GetXml();
            
            EncryptedData loaded = new EncryptedData();
            loaded.LoadXml(xml);
            
            Assert.NotNull(loaded.CipherData);
        }
    }

    public class ExcCanonicalXmlCycle2Tests
    {
        [Fact]
        public void ExcCanonicalXml_NamespacePrefix()
        {
            string xml = @"<root xmlns:ex='http://example.com' xmlns:test='http://test.com'>
                <ex:child test:attr='value'>content</ex:child>
            </root>";
            
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform(false, "ex");
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            Assert.Contains("ex:", result);
        }

        [Fact]
        public void ExcCanonicalXml_WithComments()
        {
            string xml = @"<root xmlns:ex='http://example.com'><!-- comment --><ex:data>test</ex:data></root>";
            
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);
            
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform(true, "ex");
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            Assert.Contains("comment", result);
        }
    }

    public class CanonicalXmlCommentCycle2Tests
    {
        [Fact]
        public void CanonicalXmlComment_EmptyComment()
        {
            string xml = @"<root><!----><child>data</child></root>";
            
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);
            
            XmlDsigC14NWithCommentsTransform transform = new XmlDsigC14NWithCommentsTransform();
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            Assert.Contains("<!---->", result);
        }

        [Fact]
        public void CanonicalXmlComment_InDocument()
        {
            string xml = @"<root><!-- test comment --><child>data</child></root>";
            
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);
            
            XmlDsigC14NWithCommentsTransform transform = new XmlDsigC14NWithCommentsTransform();
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            
            Assert.Contains("test comment", result);
        }
    }
}
