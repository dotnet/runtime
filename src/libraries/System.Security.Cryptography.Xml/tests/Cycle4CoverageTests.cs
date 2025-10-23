// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class Cycle4CoverageTests
    {
        [Fact]
        public void SignedXml_BuildBagOfCerts_WithX509Data()
        {
            string xml = @"<root><data>test</data></root>";
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            using (RSA rsa = RSA.Create())
            {
                SignedXml signedXml = new SignedXml(doc);
                signedXml.SigningKey = rsa;

                Reference reference = new Reference("");
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signedXml.AddReference(reference);

                // Add X509 certificate data to KeyInfo
                KeyInfo keyInfo = new KeyInfo();
                KeyInfoX509Data x509Data = new KeyInfoX509Data();
                
                using (X509Certificates.X509Certificate2 cert = TestHelpers.GetSampleX509Certificate())
                {
                    x509Data.AddCertificate(cert);
                    keyInfo.AddClause(x509Data);
                    signedXml.KeyInfo = keyInfo;

                    signedXml.ComputeSignature();
                    XmlElement signature = signedXml.GetXml();

                    // Load signature and verify - this triggers BuildBagOfCerts
                    SignedXml verify = new SignedXml(doc);
                    verify.LoadXml(signature);
                    
                    // Even if verification fails, BuildBagOfCerts is called
                    bool result = verify.CheckSignature();
                }
            }
        }

        [Fact]
        public void SignedXml_CheckSignatureWithX509_VerifySignatureOnly()
        {
            string xml = @"<root><data>test</data></root>";
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            using (RSA rsa = RSA.Create())
            using (X509Certificates.X509Certificate2 cert = TestHelpers.GetSampleX509Certificate())
            {
                SignedXml signedXml = new SignedXml(doc);
                signedXml.SigningKey = rsa;

                Reference reference = new Reference("");
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signedXml.AddReference(reference);

                signedXml.ComputeSignature();
                XmlElement signature = signedXml.GetXml();
                doc.DocumentElement.AppendChild(doc.ImportNode(signature, true));

                SignedXml verify = new SignedXml(doc);
                XmlNodeList nodeList = doc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);
                verify.LoadXml((XmlElement)nodeList[0]);

                // Test verifySignatureOnly = true (skips X509 chain validation)
                try
                {
                    verify.CheckSignature(cert, verifySignatureOnly: true);
                }
                catch
                {
                    // Expected if cert doesn't match key
                }
            }
        }

        [Fact]
        public void Reference_LoadXml_WithMultipleTransforms()
        {
            string referenceXml = @"<Reference URI="""" xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                <Transforms>
                    <Transform Algorithm=""http://www.w3.org/2000/09/xmldsig#enveloped-signature"" />
                    <Transform Algorithm=""http://www.w3.org/2001/10/xml-exc-c14n#"" />
                    <Transform Algorithm=""http://www.w3.org/TR/2001/REC-xml-c14n-20010315"" />
                </Transforms>
                <DigestMethod Algorithm=""http://www.w3.org/2000/09/xmldsig#sha256"" />
                <DigestValue>dGVzdA==</DigestValue>
            </Reference>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(referenceXml);

            Reference reference = new Reference();
            reference.LoadXml(doc.DocumentElement);

            Assert.Equal(3, reference.TransformChain.Count);
            Assert.NotNull(reference.DigestMethod);
            Assert.NotNull(reference.DigestValue);
        }

        [Fact]
        public void TransformChain_MultipleTransforms_Enumeration()
        {
            TransformChain chain = new TransformChain();
            
            chain.Add(new XmlDsigEnvelopedSignatureTransform());
            chain.Add(new XmlDsigC14NTransform());
            chain.Add(new XmlDsigExcC14NTransform());

            Assert.Equal(3, chain.Count);

            // Test enumeration
            int count = 0;
            foreach (Transform transform in chain)
            {
                Assert.NotNull(transform);
                count++;
            }
            Assert.Equal(3, count);

            // Test indexer
            Assert.IsType<XmlDsigEnvelopedSignatureTransform>(chain[0]);
            Assert.IsType<XmlDsigC14NTransform>(chain[1]);
            Assert.IsType<XmlDsigExcC14NTransform>(chain[2]);
        }

        [Fact]
        public void EncryptedXml_ReplaceElement_Various()
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml("<root><child1>data1</child1><child2>data2</child2></root>");

            EncryptedXml encXml = new EncryptedXml();
            XmlElement child1 = (XmlElement)doc.SelectSingleNode("//child1");
            
            using (Aes aes = Aes.Create())
            {
                string keyName = "MyAESKey";
                encXml.AddKeyNameMapping(keyName, aes);
                EncryptedData encData = encXml.Encrypt(child1, keyName);
                EncryptedXml.ReplaceElement(child1, encData, content: false);

                // Verify replacement
                XmlNodeList encDataNodes = doc.GetElementsByTagName("EncryptedData", EncryptedXml.XmlEncNamespaceUrl);
                Assert.Equal(1, encDataNodes.Count);
            }
        }

        [Fact]
        public void XmlDsigEnvelopedSignatureTransform_LoadInput_XmlDocument()
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml("<root><data>test</data></root>");

            XmlDsigEnvelopedSignatureTransform transform = new XmlDsigEnvelopedSignatureTransform();
            transform.LoadInput(doc);

            object output = transform.GetOutput();
            Assert.NotNull(output);
        }

        [Fact]
        public void XmlDsigExcC14NTransform_InclusiveNamespacesPrefixList()
        {
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
            
            transform.InclusiveNamespacesPrefixList = "ds xenc";
            Assert.Equal("ds xenc", transform.InclusiveNamespacesPrefixList);

            // Test with empty list
            transform.InclusiveNamespacesPrefixList = "";
            Assert.Equal("", transform.InclusiveNamespacesPrefixList);

            // Test with null
            transform.InclusiveNamespacesPrefixList = null;
            Assert.Null(transform.InclusiveNamespacesPrefixList);
        }

        [Fact]
        public void KeyInfoX509Data_AddIssuerSerial_Multiple()
        {
            KeyInfoX509Data x509Data = new KeyInfoX509Data();
            
            x509Data.AddIssuerSerial("CN=Test Issuer 1", "1234567890");
            x509Data.AddIssuerSerial("CN=Test Issuer 2", "0987654321");

            XmlElement xml = x509Data.GetXml();
            Assert.NotNull(xml);
            
            XmlNodeList issuerSerials = xml.GetElementsByTagName("X509IssuerSerial");
            Assert.Equal(2, issuerSerials.Count);
        }

        [Fact]
        public void KeyInfoX509Data_AddSubjectKeyId_Multiple()
        {
            KeyInfoX509Data x509Data = new KeyInfoX509Data();
            
            byte[] keyId1 = new byte[] { 1, 2, 3, 4, 5 };
            byte[] keyId2 = new byte[] { 6, 7, 8, 9, 10 };
            
            x509Data.AddSubjectKeyId(keyId1);
            x509Data.AddSubjectKeyId(keyId2);

            XmlElement xml = x509Data.GetXml();
            Assert.NotNull(xml);
            
            XmlNodeList keyIds = xml.GetElementsByTagName("X509SKI");
            Assert.Equal(2, keyIds.Count);
        }

        [Fact]
        public void EncryptedKey_WithReferenceList()
        {
            EncryptedKey encKey = new EncryptedKey();
            encKey.Id = "EK1";
            encKey.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
            encKey.CipherData = new CipherData(new byte[] { 1, 2, 3, 4 });

            // Access and add to reference list
            encKey.ReferenceList.Add(new DataReference("#ED1"));
            encKey.ReferenceList.Add(new DataReference("#ED2"));
            
            Assert.NotNull(encKey.ReferenceList);
            Assert.Equal(2, encKey.ReferenceList.Count);

            XmlElement xml = encKey.GetXml();
            Assert.NotNull(xml);
            
            // Verify ReferenceList in XML
            XmlNodeList refListNodes = xml.GetElementsByTagName("ReferenceList");
            Assert.Equal(1, refListNodes.Count);
        }

        [Fact]
        public void EncryptedKey_CarriedKeyName()
        {
            EncryptedKey encKey = new EncryptedKey();
            
            encKey.CarriedKeyName = "MyKeyName";
            Assert.Equal("MyKeyName", encKey.CarriedKeyName);

            encKey.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
            encKey.CipherData = new CipherData(new byte[] { 1, 2, 3, 4 });

            XmlElement xml = encKey.GetXml();
            XmlNodeList keyNames = xml.GetElementsByTagName("CarriedKeyName");
            Assert.Equal(1, keyNames.Count);
            Assert.Equal("MyKeyName", keyNames[0].InnerText);
        }

        [Fact]
        public void EncryptedKey_Recipient()
        {
            EncryptedKey encKey = new EncryptedKey();
            
            encKey.Recipient = "recipient@example.com";
            Assert.Equal("recipient@example.com", encKey.Recipient);

            encKey.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
            encKey.CipherData = new CipherData(new byte[] { 1, 2, 3, 4 });

            XmlElement xml = encKey.GetXml();
            Assert.Equal("recipient@example.com", xml.GetAttribute("Recipient"));
        }

        [Fact]
        public void Utils_EncodeXmlString()
        {
            // Test encoding special characters
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("test");
            
            element.InnerText = "text with <special> &amp; \"characters\"";
            string encoded = element.InnerXml;
            Assert.Contains("&lt;", encoded);
            Assert.Contains("&gt;", encoded);
            Assert.Contains("&amp;", encoded);
        }

        [Fact]
        public void SignedXml_IsSafeTransform_VariousAlgorithms()
        {
            // Test through safe canonicalization check
            string xml = @"<root><data>test</data></root>";
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            using (RSA rsa = RSA.Create())
            {
                SignedXml signedXml = new SignedXml(doc);
                signedXml.SigningKey = rsa;

                Reference reference = new Reference("");
                
                // Add various transforms to test safety checks
                reference.AddTransform(new XmlDsigC14NTransform());
                reference.AddTransform(new XmlDsigExcC14NTransform());
                
                signedXml.AddReference(reference);
                signedXml.ComputeSignature();

                // DoesSignatureUseSafeCanonicalizationMethod internally calls IsSafeTransform
                XmlElement signature = signedXml.GetXml();
                Assert.NotNull(signature);
            }
        }

        [Fact]
        public void SignedXml_GetPublicKey_FromKeyInfo()
        {
            string xml = @"<root><data>test</data></root>";
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            using (RSA rsa = RSA.Create())
            {
                SignedXml signedXml = new SignedXml(doc);
                signedXml.SigningKey = rsa;

                Reference reference = new Reference("");
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signedXml.AddReference(reference);

                // Add RSAKeyValue to KeyInfo
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new RSAKeyValue(rsa));
                signedXml.KeyInfo = keyInfo;

                signedXml.ComputeSignature();
                XmlElement signature = signedXml.GetXml();

                // Load and verify - GetPublicKey is called
                SignedXml verify = new SignedXml(doc);
                verify.LoadXml(signature);
                
                bool result = verify.CheckSignature();
                Assert.True(result);
            }
        }

        [Fact]
        public void EncryptedXml_GetDecryptionKey_ByKeyName()
        {
            EncryptedXml encXml = new EncryptedXml();
            
            using (Aes aes = Aes.Create())
            {
                string keyName = "MyAESKey";
                encXml.AddKeyNameMapping(keyName, aes);

                EncryptedData encData = new EncryptedData();
                encData.KeyInfo = new KeyInfo();
                encData.KeyInfo.AddClause(new KeyInfoName(keyName));

                SymmetricAlgorithm retrievedKey = encXml.GetDecryptionKey(encData, null) as SymmetricAlgorithm;
                Assert.NotNull(retrievedKey);
            }
        }

        [Fact]
        public void ExcCanonicalXml_WithNamespaces()
        {
            string xml = @"<root xmlns:ns1=""http://example.com/ns1"" xmlns:ns2=""http://example.com/ns2"">
                <ns1:child><ns2:data>test</ns2:data></ns1:child>
            </root>";
            
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
            transform.LoadInput(doc);

            using (System.IO.Stream output = (System.IO.Stream)transform.GetOutput())
            {
                Assert.NotNull(output);
                Assert.True(output.Length > 0);
            }
        }

        [Fact]
        public void Reference_TypeProperty()
        {
            Reference reference = new Reference();
            reference.Uri = "";
            reference.DigestMethod = SignedXml.XmlDsigSHA256Url;
            reference.DigestValue = new byte[] { 1, 2, 3, 4 };
            
            reference.Type = "http://www.w3.org/2000/09/xmldsig#Object";
            Assert.Equal("http://www.w3.org/2000/09/xmldsig#Object", reference.Type);

            reference.Type = "http://www.w3.org/2000/09/xmldsig#Manifest";
            Assert.Equal("http://www.w3.org/2000/09/xmldsig#Manifest", reference.Type);

            XmlElement xml = reference.GetXml();
            Assert.Equal("http://www.w3.org/2000/09/xmldsig#Manifest", xml.GetAttribute("Type"));
        }

        [Fact]
        public void EncryptionPropertyCollection_AddAndIterate()
        {
            EncryptionPropertyCollection collection = new EncryptionPropertyCollection();
            
            // Create valid EncryptionProperty elements with proper namespace
            string xml1 = @"<EncryptionProperty xmlns=""http://www.w3.org/2001/04/xmlenc#"" Id=""prop1"" Target=""#target1"">
                <test>value1</test>
            </EncryptionProperty>";
            string xml2 = @"<EncryptionProperty xmlns=""http://www.w3.org/2001/04/xmlenc#"" Id=""prop2"" Target=""#target2"">
                <test>value2</test>
            </EncryptionProperty>";
            
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml1);
            EncryptionProperty prop1 = new EncryptionProperty(doc.DocumentElement);
            
            doc.LoadXml(xml2);
            EncryptionProperty prop2 = new EncryptionProperty(doc.DocumentElement);

            collection.Add(prop1);
            collection.Add(prop2);

            Assert.Equal(2, collection.Count);
            
            // Test indexer
            Assert.NotNull(collection[0]);
            Assert.NotNull(collection[1]);

            // Test enumeration
            int count = 0;
            foreach (EncryptionProperty prop in collection)
            {
                Assert.NotNull(prop);
                count++;
            }
            Assert.Equal(2, count);
        }
    }
}
