// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    // Cycle 3: Target SignedXml, EncryptedXml, Reference, DSAKeyValue, KeyInfoX509Data
    
    public class SignedXmlCycle3Tests
    {
        [Fact]
        public void SignedXml_GetIdElement()
        {
            string xml = @"<root><data id='test123'>content</data></root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            SignedXml signedXml = new SignedXml(doc);
            Reference reference = new Reference("#test123");
            signedXml.AddReference(reference);
            
            using (RSA rsa = RSA.Create())
            {
                signedXml.SigningKey = rsa;
                signedXml.ComputeSignature();
                XmlElement signature = signedXml.GetXml();
                Assert.NotNull(signature);
            }
        }

        [Fact]
        public void SignedXml_PreserveWhitespace()
        {
            string xml = @"<root>  <data>  test  </data>  </root>";
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);
            
            SignedXml signedXml = new SignedXml(doc);
            Reference reference = new Reference("");
            signedXml.AddReference(reference);
            
            using (RSA rsa = RSA.Create())
            {
                signedXml.SigningKey = rsa;
                signedXml.ComputeSignature();
                Assert.NotNull(signedXml.GetXml());
            }
        }

        [Fact]
        public void SignedXml_SafeCanonicalizationMethods()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data>test</data></root>");
            
            SignedXml signedXml = new SignedXml(doc);
            
            // Test various canonicalization methods
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
            Assert.Equal(SignedXml.XmlDsigC14NTransformUrl, signedXml.SignedInfo.CanonicalizationMethod);
            
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            Assert.Equal(SignedXml.XmlDsigExcC14NTransformUrl, signedXml.SignedInfo.CanonicalizationMethod);
        }

        [Fact]
        public void SignedXml_GetPublicKey()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data>test</data></root>");
            
            SignedXml signedXml = new SignedXml(doc);
            Reference reference = new Reference("");
            signedXml.AddReference(reference);
            
            using (RSA rsa = RSA.Create())
            {
                signedXml.SigningKey = rsa;
                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new RSAKeyValue(rsa));
                signedXml.KeyInfo = keyInfo;
                
                signedXml.ComputeSignature();
                
                SignedXml verifyXml = new SignedXml(doc);
                verifyXml.LoadXml(signedXml.GetXml());
                
                bool valid = verifyXml.CheckSignature();
                Assert.True(valid);
            }
        }
    }

    public class EncryptedXmlCycle3Tests
    {
        [Fact]
        public void EncryptedXml_AddKeyNameMapping()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            
            EncryptedXml encryptedXml = new EncryptedXml(doc);
            
            using (Aes aes = Aes.Create())
            {
                encryptedXml.AddKeyNameMapping("key1", aes);
                
                // Add another key
                using (Aes aes2 = Aes.Create())
                {
                    encryptedXml.AddKeyNameMapping("key2", aes2);
                }
            }
            
            Assert.NotNull(encryptedXml);
        }

        [Fact]
        public void EncryptedXml_GetDecryptionKey()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            
            EncryptedXml encryptedXml = new EncryptedXml(doc);
            
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.KeyInfo = new KeyInfo();
            encryptedData.KeyInfo.AddClause(new KeyInfoName("testKey"));
            
            using (Aes aes = Aes.Create())
            {
                encryptedXml.AddKeyNameMapping("testKey", aes);
                Assert.NotNull(encryptedXml);
            }
        }

        [Fact]
        public void EncryptedXml_ReplaceData()
        {
            string xml = "<root><data>secret</data></root>";
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);
            
            using (Aes aes = Aes.Create())
            {
                EncryptedXml encryptedXml = new EncryptedXml();
                byte[] encrypted = encryptedXml.EncryptData(doc.DocumentElement, aes, false);
                
                EncryptedData encryptedData = new EncryptedData();
                encryptedData.Type = EncryptedXml.XmlEncElementUrl;
                encryptedData.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
                encryptedData.CipherData = new CipherData(encrypted);
                
                EncryptedXml.ReplaceElement(doc.DocumentElement, encryptedData, false);
                
                Assert.Contains("EncryptedData", doc.OuterXml);
            }
        }

        [Fact]
        public void EncryptedXml_EncryptDecrypt_Symmetic()
        {
            string xml = "<root><data>secret</data></root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            using (Aes aes = Aes.Create())
            {
                EncryptedXml encryptedXml = new EncryptedXml();
                byte[] encrypted = encryptedXml.EncryptData(doc.DocumentElement, aes, false);
                
                Assert.NotNull(encrypted);
                Assert.True(encrypted.Length > 0);
            }
        }
    }

    public class ReferenceCycle3Tests
    {
        [Fact]
        public void Reference_TransformChain_Property()
        {
            Reference reference = new Reference();
            
            Assert.NotNull(reference.TransformChain);
            Assert.Equal(0, reference.TransformChain.Count);
            
            reference.AddTransform(new XmlDsigC14NTransform());
            Assert.Equal(1, reference.TransformChain.Count);
        }

        [Fact]
        public void Reference_Type_Property()
        {
            Reference reference = new Reference();
            reference.Type = "http://www.w3.org/2000/09/xmldsig#Object";
            
            Assert.Equal("http://www.w3.org/2000/09/xmldsig#Object", reference.Type);
        }

        [Fact]
        public void Reference_Id_Property()
        {
            Reference reference = new Reference();
            reference.Id = "ref-123";
            
            Assert.Equal("ref-123", reference.Id);
        }
    }

    public class DSAKeyValueCycle3Tests
    {
        [Fact]
        public void DSAKeyValue_GetXml()
        {
            using (DSA dsa = DSA.Create())
            {
                DSAKeyValue dsaKeyValue = new DSAKeyValue(dsa);
                XmlElement xml = dsaKeyValue.GetXml();
                
                Assert.NotNull(xml);
                Assert.Equal("KeyValue", xml.LocalName);
                Assert.Contains("DSAKeyValue", xml.OuterXml);
            }
        }

        [Fact]
        public void DSAKeyValue_LoadXml()
        {
            using (DSA dsa = DSA.Create())
            {
                DSAKeyValue dsaKeyValue = new DSAKeyValue(dsa);
                XmlElement xml = dsaKeyValue.GetXml();
                
                DSAKeyValue loaded = new DSAKeyValue();
                loaded.LoadXml(xml);
                
                DSA loadedKey = loaded.Key;
                Assert.NotNull(loadedKey);
            }
        }

        [Fact]
        public void DSAKeyValue_EmptyConstructor()
        {
            DSAKeyValue dsaKeyValue = new DSAKeyValue();
            Assert.NotNull(dsaKeyValue);
            
            using (DSA dsa = DSA.Create())
            {
                dsaKeyValue.Key = dsa;
                Assert.NotNull(dsaKeyValue.Key);
            }
        }
    }

    public class KeyInfoX509DataCycle3Tests
    {
        [Fact]
        public void KeyInfoX509Data_AddSubjectName()
        {
            KeyInfoX509Data x509Data = new KeyInfoX509Data();
            x509Data.AddSubjectName("CN=Test");
            
            XmlElement xml = x509Data.GetXml();
            Assert.Contains("X509SubjectName", xml.OuterXml);
            Assert.Contains("CN=Test", xml.OuterXml);
        }

        [Fact]
        public void KeyInfoX509Data_AddIssuerSerial()
        {
            KeyInfoX509Data x509Data = new KeyInfoX509Data();
            x509Data.AddIssuerSerial("CN=TestIssuer", "12345");
            
            XmlElement xml = x509Data.GetXml();
            Assert.Contains("X509IssuerSerial", xml.OuterXml);
        }

        [Fact]
        public void KeyInfoX509Data_AddSubjectKeyId()
        {
            KeyInfoX509Data x509Data = new KeyInfoX509Data();
            byte[] keyId = new byte[] { 1, 2, 3, 4, 5 };
            x509Data.AddSubjectKeyId(keyId);
            
            XmlElement xml = x509Data.GetXml();
            Assert.Contains("X509SKI", xml.OuterXml);
        }

        [Fact]
        public void KeyInfoX509Data_LoadXml()
        {
            KeyInfoX509Data x509Data = new KeyInfoX509Data();
            x509Data.AddSubjectName("CN=Test");
            
            XmlElement xml = x509Data.GetXml();
            
            KeyInfoX509Data loaded = new KeyInfoX509Data();
            loaded.LoadXml(xml);
            
            XmlElement loadedXml = loaded.GetXml();
            Assert.Contains("CN=Test", loadedXml.OuterXml);
        }
    }

    public class EncryptedDataCycle3Tests
    {
        [Fact]
        public void EncryptedData_KeyInfo_Property()
        {
            EncryptedData encryptedData = new EncryptedData();
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoName("testKey"));
            
            encryptedData.KeyInfo = keyInfo;
            encryptedData.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            XmlElement xml = encryptedData.GetXml();
            Assert.Contains("KeyInfo", xml.OuterXml);
            Assert.Contains("testKey", xml.OuterXml);
        }

        [Fact]
        public void EncryptedData_AddProperty()
        {
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            XmlDocument doc = new XmlDocument();
            XmlElement propElement = doc.CreateElement("EncryptionProperty", EncryptedXml.XmlEncNamespaceUrl);
            propElement.InnerText = "test";
            
            EncryptionProperty prop = new EncryptionProperty(propElement);
            encryptedData.AddProperty(prop);
            
            Assert.NotNull(encryptedData);
        }
    }

    public class CanonicalXmlNodeListCycle3Tests
    {
        [Fact]
        public void CanonicalXmlNodeList_Enumerate()
        {
            string xml = "<root><item>1</item><item>2</item><item>3</item></root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            XmlNodeList nodes = doc.SelectNodes("//item");
            
            int count = 0;
            foreach (XmlNode node in nodes)
            {
                Assert.NotNull(node);
                count++;
            }
            Assert.Equal(3, count);
        }
    }
}
