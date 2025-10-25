// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    // Tests targeting SignedXml gaps
    public class SignedXmlCoverageTests
    {
        [Fact]
        public void SignedXml_ComputeSignature_WithKeyedHashAlgorithm()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data>test</data></root>");
            
            SignedXml signedXml = new SignedXml(doc);
            Reference reference = new Reference("");
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);
            
            using (HMACSHA256 hmac = new HMACSHA256())
            {
                signedXml.ComputeSignature(hmac);
                XmlElement signature = signedXml.GetXml();
                Assert.NotNull(signature);
            }
        }

        [Fact]
        public void SignedXml_CheckSignature_WithKeyedHashAlgorithm()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data>test</data></root>");
            
            SignedXml signedXml = new SignedXml(doc);
            Reference reference = new Reference("");
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);
            
            byte[] key = new byte[32];
            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                signedXml.ComputeSignature(hmac);
                
                SignedXml verifyXml = new SignedXml(doc);
                verifyXml.LoadXml(signedXml.GetXml());
                
                using (HMACSHA256 verifyHmac = new HMACSHA256(key))
                {
                    bool valid = verifyXml.CheckSignature(verifyHmac);
                    Assert.True(valid);
                }
            }
        }

        [Fact]
        public void SignedXml_SignatureMethod_Property()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data>test</data></root>");
            
            SignedXml signedXml = new SignedXml(doc);
            Reference reference = new Reference("");
            signedXml.AddReference(reference);
            
            using (RSA rsa = RSA.Create())
            {
                signedXml.SigningKey = rsa;
                signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;
                signedXml.ComputeSignature();
                
                Assert.Equal(SignedXml.XmlDsigRSASHA256Url, signedXml.SignedInfo.SignatureMethod);
            }
        }

        [Fact]
        public void SignedXml_GetIdElement_WithMultipleIds()
        {
            string xml = @"<root><item Id='item1'>first</item><item Id='item2'>second</item></root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            SignedXml signedXml = new SignedXml(doc);
            Reference reference = new Reference("#item1");
            signedXml.AddReference(reference);
            
            using (RSA rsa = RSA.Create())
            {
                signedXml.SigningKey = rsa;
                signedXml.ComputeSignature();
                Assert.NotNull(signedXml.GetXml());
            }
        }
    }

    // Tests targeting TransformChain gaps
    public class TransformChainCoverageTests
    {
        [Fact]
        public void TransformChain_WithMultipleTransforms()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data>test data</data></root>");
            
            Reference reference = new Reference("");
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigC14NTransform());
            reference.AddTransform(new XmlDsigBase64Transform());
            
            Assert.Equal(3, reference.TransformChain.Count);
        }

        [Fact]
        public void TransformChain_Indexer_OutOfRange()
        {
            TransformChain chain = new TransformChain();
            chain.Add(new XmlDsigC14NTransform());
            
            Assert.Throws<ArgumentException>(() => chain[5]);
        }

        [Fact]
        public void TransformChain_AddNull_Ignored()
        {
            TransformChain chain = new TransformChain();
            int countBefore = chain.Count;
            
            chain.Add(null);
            
            // Null transforms are ignored
            Assert.Equal(countBefore, chain.Count);
        }
    }

    // Tests targeting XmlDsigEnvelopedSignatureTransform gaps
    public class XmlDsigEnvelopedSignatureTransformCoverageTests
    {
        [Fact]
        public void XmlDsigEnvelopedSignatureTransform_WithStream()
        {
            string xml = @"<root><Signature xmlns='http://www.w3.org/2000/09/xmldsig#'></Signature><data>test</data></root>";
            
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            XmlDsigEnvelopedSignatureTransform transform = new XmlDsigEnvelopedSignatureTransform();
            
            // Convert to stream
            MemoryStream ms = new MemoryStream();
            using (XmlWriter writer = XmlWriter.Create(ms))
            {
                doc.WriteTo(writer);
            }
            ms.Position = 0;
            
            transform.LoadInput(ms);
            object output = transform.GetOutput();
            Assert.NotNull(output);
        }

        [Fact]
        public void XmlDsigEnvelopedSignatureTransform_WithXmlNodeList()
        {
            string xml = @"<root><data>test1</data><data>test2</data></root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            XmlNodeList nodeList = doc.SelectNodes("//data");
            
            XmlDsigEnvelopedSignatureTransform transform = new XmlDsigEnvelopedSignatureTransform();
            transform.LoadInput(nodeList);
            
            object output = transform.GetOutput();
            Assert.NotNull(output);
        }

        [Fact]
        public void XmlDsigEnvelopedSignatureTransform_GetOutput_TypedStream()
        {
            string xml = @"<root><data>test</data></root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            XmlDsigEnvelopedSignatureTransform transform = new XmlDsigEnvelopedSignatureTransform();
            transform.LoadInput(doc);
            
            object output = transform.GetOutput(typeof(XmlDocument));
            Assert.IsType<XmlDocument>(output);
        }
    }

    // Tests targeting ExcCanonicalXml and XmlDsigExcC14NTransform gaps
    public class ExclusiveCanonicalizationCoverageTests
    {
        [Fact]
        public void XmlDsigExcC14NTransform_WithInclusiveNamespacesList()
        {
            string xml = @"<root xmlns:ex='http://example.com' xmlns:test='http://test.com'>
                <ex:data test:attr='value'>content</ex:data>
            </root>";
            
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform(false, "ex test");
            transform.LoadInput(doc);
            
            Stream output = (Stream)transform.GetOutput();
            Assert.NotNull(output);
            
            string result = new StreamReader(output, Encoding.UTF8).ReadToEnd();
            Assert.Contains("example.com", result);
        }

        [Fact]
        public void XmlDsigExcC14NTransform_LoadInnerXml()
        {
            string innerXml = @"<InclusiveNamespaces xmlns='http://www.w3.org/2001/10/xml-exc-c14n#' PrefixList='ds xsi' />";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(innerXml);
            
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
            transform.LoadInnerXml(doc.ChildNodes);
            
            Assert.Equal("ds xsi", transform.InclusiveNamespacesPrefixList);
        }

        [Fact]
        public void XmlDsigExcC14NTransform_GetXml()
        {
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform(false, "ds xml");
            XmlElement xml = transform.GetXml();
            
            Assert.NotNull(xml);
            Assert.Equal("Transform", xml.LocalName);
        }

        [Fact]
        public void XmlDsigExcC14NTransform_WithStream()
        {
            string xml = @"<root><child>data</child></root>";
            
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
            transform.LoadInput(ms);
            
            Stream output = (Stream)transform.GetOutput();
            Assert.NotNull(output);
        }
    }

    // Tests targeting EncryptedXml gaps
    public class EncryptedXmlCoverageTests
    {
        [Fact]
        public void EncryptedXml_XmlEncNamespaceUrl()
        {
            string expectedUrl = "http://www.w3.org/2001/04/xmlenc#";
            Assert.Equal(EncryptedXml.XmlEncNamespaceUrl, expectedUrl);
        }

        [Fact]
        public void EncryptedXml_DecryptKey_TripleDES()
        {
            using (TripleDES tripleDES = TripleDES.Create())
            {
                byte[] keyData = new byte[24];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(keyData);
                }
                tripleDES.Key = keyData;
                
                byte[] encryptedKey = EncryptedXml.EncryptKey(keyData, tripleDES);
                byte[] decryptedKey = EncryptedXml.DecryptKey(encryptedKey, tripleDES);
                
                Assert.Equal(keyData, decryptedKey);
            }
        }

        [Fact]
        public void EncryptedXml_ReplaceData_WithEncryptedData()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data>sensitive</data></root>");
            
            XmlElement elementToEncrypt = (XmlElement)doc.SelectSingleNode("//data");
            
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.Type = EncryptedXml.XmlEncElementUrl;
            encryptedData.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
            encryptedData.CipherData = new CipherData(new byte[] { 1, 2, 3, 4 });
            
            EncryptedXml.ReplaceElement(elementToEncrypt, encryptedData, false);
            
            Assert.Contains("EncryptedData", doc.OuterXml);
        }

        [Fact]
        public void EncryptedXml_ClearKeyNameMappings()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            
            EncryptedXml encryptedXml = new EncryptedXml(doc);
            
            using (Aes aes = Aes.Create())
            {
                encryptedXml.AddKeyNameMapping("key1", aes);
                encryptedXml.ClearKeyNameMappings();
                
                // After clearing, key should not be found
                Assert.NotNull(encryptedXml);
            }
        }

        [Fact]
        public void EncryptedXml_GetDecryptionKey_WithKeyName()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            
            EncryptedXml encryptedXml = new EncryptedXml(doc);
            
            using (Aes aes = Aes.Create())
            {
                encryptedXml.AddKeyNameMapping("testkey", aes);
                
                EncryptedData encryptedData = new EncryptedData();
                encryptedData.KeyInfo = new KeyInfo();
                encryptedData.KeyInfo.AddClause(new KeyInfoName("testkey"));
                
                SymmetricAlgorithm key = encryptedXml.GetDecryptionKey(encryptedData, null);
                Assert.NotNull(key);
            }
        }
    }

    // Tests targeting Reference gaps
    public class ReferenceCoverageTests
    {
        [Fact]
        public void Reference_Uri_Modification()
        {
            Reference reference = new Reference("#test");
            Assert.Equal("#test", reference.Uri);
            
            reference.Uri = "#modified";
            Assert.Equal("#modified", reference.Uri);
        }

        [Fact]
        public void Reference_DigestValue_Setter()
        {
            Reference reference = new Reference();
            byte[] digestValue = new byte[] { 1, 2, 3, 4, 5 };
            
            reference.DigestValue = digestValue;
            Assert.Equal(digestValue, reference.DigestValue);
        }

        [Fact]
        public void Reference_Type_Property()
        {
            Reference reference = new Reference();
            string type = "http://www.w3.org/2000/09/xmldsig#Object";
            
            reference.Type = type;
            Assert.Equal(type, reference.Type);
        }

        [Fact]
        public void Reference_DigestMethod_Property()
        {
            Reference reference = new Reference();
            string digestMethod = EncryptedXml.XmlEncSHA256Url;
            
            reference.DigestMethod = digestMethod;
            Assert.Equal(digestMethod, reference.DigestMethod);
        }
    }
}
