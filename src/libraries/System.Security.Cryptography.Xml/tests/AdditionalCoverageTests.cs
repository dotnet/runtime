// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class EncryptedDataCoverageTests
    {
        [Fact]
        public void EncryptedData_GetXml()
        {
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.Id = "ED1";
            encryptedData.Type = EncryptedXml.XmlEncElementUrl;
            encryptedData.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
            encryptedData.CipherData = new CipherData(new byte[] { 1, 2, 3, 4 });
            
            XmlElement element = encryptedData.GetXml();
            Assert.NotNull(element);
            Assert.Equal("EncryptedData", element.LocalName);
            Assert.Equal("ED1", element.GetAttribute("Id"));
        }

        [Fact]
        public void EncryptedData_CipherDataWithValue()
        {
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.CipherData = new CipherData(new byte[] { 1, 2, 3, 4 });
            
            XmlElement element = encryptedData.GetXml();
            Assert.NotNull(element);
            Assert.Contains("CipherData", element.OuterXml);
        }

        [Fact]
        public void EncryptedData_WithKeyInfo()
        {
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.KeyInfo = new KeyInfo();
            encryptedData.KeyInfo.AddClause(new KeyInfoName("MyKey"));
            encryptedData.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            XmlElement element = encryptedData.GetXml();
            Assert.Contains("KeyInfo", element.OuterXml);
            Assert.Contains("KeyName", element.OuterXml);
        }
    }

    public class EncryptedKeyCoverageTests
    {
        [Fact]
        public void EncryptedKey_Constructor()
        {
            EncryptedKey encryptedKey = new EncryptedKey();
            Assert.NotNull(encryptedKey);
        }

        [Fact]
        public void EncryptedKey_WithCarriedKeyName()
        {
            EncryptedKey encryptedKey = new EncryptedKey();
            encryptedKey.CarriedKeyName = "MyCarriedKey";
            encryptedKey.CipherData = new CipherData(new byte[] { 1, 2, 3, 4, 5 });
            
            XmlElement element = encryptedKey.GetXml();
            Assert.Contains("CarriedKeyName", element.OuterXml);
            Assert.Contains("MyCarriedKey", element.OuterXml);
        }

        [Fact]
        public void EncryptedKey_AddReference()
        {
            EncryptedKey encryptedKey = new EncryptedKey();
            encryptedKey.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            DataReference dataRef = new DataReference("#data1");
            encryptedKey.AddReference(dataRef);
            
            XmlElement element = encryptedKey.GetXml();
            Assert.Contains("ReferenceList", element.OuterXml);
            Assert.Contains("DataReference", element.OuterXml);
        }

        [Fact]
        public void EncryptedKey_WithRecipient()
        {
            EncryptedKey encryptedKey = new EncryptedKey();
            encryptedKey.Recipient = "recipient@example.com";
            encryptedKey.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            XmlElement element = encryptedKey.GetXml();
            Assert.Contains("recipient@example.com", element.OuterXml);
        }
    }

    public class KeyInfoEncryptedKeyCoverageTests
    {
        [Fact]
        public void KeyInfoEncryptedKey_Constructor()
        {
            EncryptedKey encryptedKey = new EncryptedKey();
            encryptedKey.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            KeyInfoEncryptedKey keyInfoEncryptedKey = new KeyInfoEncryptedKey(encryptedKey);
            Assert.NotNull(keyInfoEncryptedKey);
            Assert.Equal(encryptedKey, keyInfoEncryptedKey.EncryptedKey);
        }

        [Fact]
        public void KeyInfoEncryptedKey_GetXml()
        {
            EncryptedKey encryptedKey = new EncryptedKey();
            encryptedKey.Id = "EK1";
            encryptedKey.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            KeyInfoEncryptedKey keyInfoEncryptedKey = new KeyInfoEncryptedKey(encryptedKey);
            XmlElement element = keyInfoEncryptedKey.GetXml();
            
            Assert.NotNull(element);
            Assert.Equal("EncryptedKey", element.LocalName);
            Assert.Contains("EK1", element.OuterXml);
        }

        [Fact]
        public void KeyInfoEncryptedKey_LoadXml()
        {
            string xml = @"<EncryptedKey xmlns='http://www.w3.org/2001/04/xmlenc#' Id='EK1'>
                <CipherData><CipherValue>AQIDBA==</CipherValue></CipherData>
            </EncryptedKey>";
            
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            KeyInfoEncryptedKey keyInfoEncryptedKey = new KeyInfoEncryptedKey();
            keyInfoEncryptedKey.LoadXml(doc.DocumentElement);
            
            Assert.NotNull(keyInfoEncryptedKey.EncryptedKey);
            Assert.Equal("EK1", keyInfoEncryptedKey.EncryptedKey.Id);
        }
    }

    public class EncryptionPropertyCollectionCoverageTests
    {
        [Fact]
        public void EncryptionPropertyCollection_ItemGet()
        {
            EncryptionPropertyCollection collection = new EncryptionPropertyCollection();
            EncryptionProperty prop1 = new EncryptionProperty();
            
            collection.Add(prop1);
            
            EncryptionProperty retrieved = collection.Item(0);
            Assert.Equal(prop1, retrieved);
        }

        [Fact]
        public void EncryptionPropertyCollection_IndexerGet()
        {
            EncryptionPropertyCollection collection = new EncryptionPropertyCollection();
            EncryptionProperty prop1 = new EncryptionProperty();
            
            collection.Add(prop1);
            
            EncryptionProperty retrieved = collection[0];
            Assert.Equal(prop1, retrieved);
        }

        [Fact]
        public void EncryptionPropertyCollection_AddMultiple()
        {
            EncryptionPropertyCollection collection = new EncryptionPropertyCollection();
            EncryptionProperty prop1 = new EncryptionProperty();
            EncryptionProperty prop2 = new EncryptionProperty();
            
            int index1 = collection.Add(prop1);
            int index2 = collection.Add(prop2);
            
            Assert.Equal(0, index1);
            Assert.Equal(1, index2);
            Assert.Equal(2, collection.Count);
        }

        [Fact]
        public void EncryptionPropertyCollection_Remove()
        {
            EncryptionPropertyCollection collection = new EncryptionPropertyCollection();
            EncryptionProperty prop1 = new EncryptionProperty();
            
            collection.Add(prop1);
            collection.Remove(prop1);
            
            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public void EncryptionPropertyCollection_Clear()
        {
            EncryptionPropertyCollection collection = new EncryptionPropertyCollection();
            collection.Add(new EncryptionProperty());
            collection.Add(new EncryptionProperty());
            
            collection.Clear();
            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public void EncryptionPropertyCollection_Insert()
        {
            EncryptionPropertyCollection collection = new EncryptionPropertyCollection();
            EncryptionProperty prop1 = new EncryptionProperty();
            EncryptionProperty prop2 = new EncryptionProperty();
            EncryptionProperty prop3 = new EncryptionProperty();
            
            collection.Add(prop1);
            collection.Add(prop3);
            collection.Insert(1, prop2);
            
            Assert.Equal(3, collection.Count);
            Assert.Equal(prop2, collection[1]);
        }

        [Fact]
        public void EncryptionPropertyCollection_Contains()
        {
            EncryptionPropertyCollection collection = new EncryptionPropertyCollection();
            EncryptionProperty prop1 = new EncryptionProperty();
            
            collection.Add(prop1);
            
            Assert.True(collection.Contains(prop1));
        }

        [Fact]
        public void EncryptionPropertyCollection_IndexOf()
        {
            EncryptionPropertyCollection collection = new EncryptionPropertyCollection();
            EncryptionProperty prop1 = new EncryptionProperty();
            EncryptionProperty prop2 = new EncryptionProperty();
            
            collection.Add(prop1);
            collection.Add(prop2);
            
            Assert.Equal(0, collection.IndexOf(prop1));
            Assert.Equal(1, collection.IndexOf(prop2));
        }

        [Fact]
        public void EncryptionPropertyCollection_RemoveAt()
        {
            EncryptionPropertyCollection collection = new EncryptionPropertyCollection();
            EncryptionProperty prop1 = new EncryptionProperty();
            EncryptionProperty prop2 = new EncryptionProperty();
            
            collection.Add(prop1);
            collection.Add(prop2);
            collection.RemoveAt(0);
            
            Assert.Equal(1, collection.Count);
            Assert.Equal(prop2, collection[0]);
        }

        [Fact]
        public void EncryptionPropertyCollection_CopyTo()
        {
            EncryptionPropertyCollection collection = new EncryptionPropertyCollection();
            EncryptionProperty prop1 = new EncryptionProperty();
            EncryptionProperty prop2 = new EncryptionProperty();
            
            collection.Add(prop1);
            collection.Add(prop2);
            
            EncryptionProperty[] array = new EncryptionProperty[2];
            collection.CopyTo(array, 0);
            
            Assert.Equal(prop1, array[0]);
            Assert.Equal(prop2, array[1]);
        }

        [Fact]
        public void EncryptionPropertyCollection_GetEnumerator()
        {
            EncryptionPropertyCollection collection = new EncryptionPropertyCollection();
            collection.Add(new EncryptionProperty());
            collection.Add(new EncryptionProperty());
            
            int count = 0;
            foreach (EncryptionProperty prop in collection)
            {
                Assert.NotNull(prop);
                count++;
            }
            Assert.Equal(2, count);
        }
    }

    public class KeyInfoRetrievalMethodCoverageTests
    {
        [Fact]
        public void KeyInfoRetrievalMethod_ConstructorWithUri()
        {
            string uri = "#KeyValue";
            KeyInfoRetrievalMethod retrievalMethod = new KeyInfoRetrievalMethod(uri);
            Assert.Equal(uri, retrievalMethod.Uri);
        }

        [Fact]
        public void KeyInfoRetrievalMethod_ConstructorWithUriAndType()
        {
            string uri = "#KeyValue";
            string type = "http://www.w3.org/2001/04/xmlenc#EncryptedKey";
            KeyInfoRetrievalMethod retrievalMethod = new KeyInfoRetrievalMethod(uri, type);
            
            Assert.Equal(uri, retrievalMethod.Uri);
            Assert.Equal(type, retrievalMethod.Type);
        }

        [Fact]
        public void KeyInfoRetrievalMethod_GetXml()
        {
            KeyInfoRetrievalMethod retrievalMethod = new KeyInfoRetrievalMethod("#KeyValue");
            XmlElement element = retrievalMethod.GetXml();
            
            Assert.NotNull(element);
            Assert.Equal("RetrievalMethod", element.LocalName);
        }

        [Fact]
        public void KeyInfoRetrievalMethod_LoadXml()
        {
            string xml = @"<RetrievalMethod xmlns='http://www.w3.org/2000/09/xmldsig#' URI='#KeyValue' />";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            KeyInfoRetrievalMethod retrievalMethod = new KeyInfoRetrievalMethod();
            retrievalMethod.LoadXml(doc.DocumentElement);
            
            Assert.Equal("#KeyValue", retrievalMethod.Uri);
        }
    }

    public class XmlDsigExcC14NTransformCoverageTests
    {
        [Fact]
        public void XmlDsigExcC14NTransform_ConstructorWithComments()
        {
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform(true);
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", transform.Algorithm);
        }

        [Fact]
        public void XmlDsigExcC14NTransform_ConstructorWithoutComments()
        {
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform(false);
            Assert.Equal("http://www.w3.org/2001/10/xml-exc-c14n#", transform.Algorithm);
        }

        [Fact]
        public void XmlDsigExcC14NTransform_ConstructorWithInclusiveNamespaces()
        {
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform(false, "ds xsi");
            Assert.Equal("ds xsi", transform.InclusiveNamespacesPrefixList);
        }

        [Fact]
        public void XmlDsigExcC14NTransform_LoadInput()
        {
            string xml = @"<root xmlns:ex='http://example.com'><ex:child>data</ex:child></root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
            transform.LoadInput(doc);
            
            object output = transform.GetOutput();
            Assert.NotNull(output);
        }

        [Fact]
        public void XmlDsigExcC14NTransform_GetDigestedOutput()
        {
            string xml = @"<root><child>data</child></root>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            
            XmlDsigExcC14NTransform transform = new XmlDsigExcC14NTransform();
            transform.LoadInput(doc);
            
            using (SHA256 hash = SHA256.Create())
            {
                byte[] digest = transform.GetDigestedOutput(hash);
                Assert.NotNull(digest);
                Assert.Equal(32, digest.Length);
            }
        }
    }
}
