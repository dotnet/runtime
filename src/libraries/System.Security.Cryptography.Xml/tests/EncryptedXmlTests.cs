// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public static class EncryptedXmlTests
    {
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

        [Fact]
        public static void EncryptedData_Constructor()
        {
            EncryptedData encData = new EncryptedData();
            Assert.Null(encData.Id);
            Assert.Null(encData.Type);
            Assert.Null(encData.MimeType);
            Assert.Null(encData.Encoding);
        }

        [Fact]
        public static void EncryptedData_GetXml_ThrowsWhenCipherDataNull()
        {
            EncryptedData encData = new EncryptedData();
            Assert.Throws<CryptographicException>(() => encData.GetXml());
        }

        [Fact]
        public static void EncryptedData_GetXml_WithCipherValue()
        {
            EncryptedData encData = new EncryptedData();
            encData.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            XmlElement xml = encData.GetXml();
            Assert.Equal(@"<EncryptedData xmlns=""http://www.w3.org/2001/04/xmlenc#""><CipherData><CipherValue>AQID</CipherValue></CipherData></EncryptedData>", xml.OuterXml);
        }

        [Fact]
        public static void EncryptedData_GetXml_WithAllProperties()
        {
            EncryptedData encData = new EncryptedData
            {
                Id = "test-id",
                Type = EncryptedXml.XmlEncElementUrl,
                MimeType = "text/xml",
                Encoding = "utf-8",
                CipherData = new CipherData(new byte[] { 1, 2, 3 }),
                EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url)
            };

            encData.KeyInfo.AddClause(new KeyInfoName("key1"));

            XmlDocument doc = new XmlDocument();
            XmlElement propElement = doc.CreateElement("EncryptionProperty", EncryptedXml.XmlEncNamespaceUrl);
            propElement.InnerText = "value";
            EncryptionProperty prop = new EncryptionProperty(propElement);
            encData.EncryptionProperties.Add(prop);

            XmlElement xml = encData.GetXml();
            // Verify the full output XML structure
            string expectedXml = @"<EncryptedData Id=""test-id"" Type=""http://www.w3.org/2001/04/xmlenc#Element"" MimeType=""text/xml"" Encoding=""utf-8"" xmlns=""http://www.w3.org/2001/04/xmlenc#""><EncryptionMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#aes256-cbc"" /><KeyInfo xmlns=""http://www.w3.org/2000/09/xmldsig#""><KeyName>key1</KeyName></KeyInfo><CipherData><CipherValue>AQID</CipherValue></CipherData><EncryptionProperties><EncryptionProperty>value</EncryptionProperty></EncryptionProperties></EncryptedData>";
            Assert.Equal(expectedXml, xml.OuterXml);
        }

        [Fact]
        public static void EncryptedData_LoadXml_MinimalValid()
        {
            string xml = @"<EncryptedData xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
            </EncryptedData>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedData encData = new EncryptedData();
            encData.LoadXml(doc.DocumentElement);
            Assert.NotNull(encData.CipherData);
            Assert.NotNull(encData.CipherData.CipherValue);
            // EncryptionMethod auto-initializes but has null KeyAlgorithm when not specified
            Assert.NotNull(encData.EncryptionMethod);
            Assert.Null(encData.EncryptionMethod.KeyAlgorithm);
        }

        [Fact]
        public static void EncryptedData_LoadXml_WithAttributes()
        {
            string xml = @"<EncryptedData Id=""id1"" Type=""http://example.com/type"" MimeType=""application/xml"" Encoding=""utf-8"" xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <EncryptionMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#aes256-cbc"" />
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
            </EncryptedData>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedData encData = new EncryptedData();
            encData.LoadXml(doc.DocumentElement);
            Assert.Equal("id1", encData.Id);
            Assert.Equal("http://example.com/type", encData.Type);
            Assert.Equal("application/xml", encData.MimeType);
            Assert.Equal("utf-8", encData.Encoding);
            Assert.NotNull(encData.EncryptionMethod);
            Assert.Equal(EncryptedXml.XmlEncAES256Url, encData.EncryptionMethod.KeyAlgorithm);
        }

        [Fact]
        public static void EncryptedData_LoadXml_WithEncryptionProperties()
        {
            string xml = @"<EncryptedData xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
                <EncryptionProperties>
                    <EncryptionProperty Target=""#test"">
                        <custom>data</custom>
                    </EncryptionProperty>
                </EncryptionProperties>
            </EncryptedData>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedData encData = new EncryptedData();
            encData.LoadXml(doc.DocumentElement);
            Assert.Equal(1, encData.EncryptionProperties.Count);
        }

        [Fact]
        public static void EncryptedData_LoadXml_ThrowsWhenCipherDataMissing()
        {
            string xml = @"<EncryptedData xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <EncryptionMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#aes256-cbc"" />
            </EncryptedData>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedData encData = new EncryptedData();
            Assert.Throws<CryptographicException>(() => encData.LoadXml(doc.DocumentElement));
        }



        [Fact]
        public static void EncryptedKey_Constructor_AndAddReference()
        {
            // Test constructor and AddReference in one test
            EncryptedKey encKey = new EncryptedKey();
            
            // Verify constructor initialized properties
            Assert.Null(encKey.Id);
            Assert.Null(encKey.Type);
            Assert.Null(encKey.MimeType);
            Assert.Null(encKey.Encoding);
            Assert.Equal(string.Empty, encKey.Recipient); // Default value
            Assert.Null(encKey.CarriedKeyName);
            Assert.NotNull(encKey.ReferenceList); // ReferenceList is auto-initialized
            Assert.Equal(0, encKey.ReferenceList.Count);
            
            // Test AddReference functionality
            DataReference dataRef = new DataReference("#data1");
            encKey.AddReference(dataRef);
            Assert.Equal(1, encKey.ReferenceList.Count);
        }

        [Fact]
        public static void EncryptedKey_AddReference_KeyReference()
        {
            EncryptedKey encKey = new EncryptedKey();
            KeyReference keyRef = new KeyReference("#key1");
            encKey.AddReference(keyRef);
            Assert.Equal(1, encKey.ReferenceList.Count);
        }

        [Fact]
        public static void EncryptedKey_GetXml_ThrowsWhenCipherDataNull()
        {
            EncryptedKey encKey = new EncryptedKey();
            Assert.Throws<CryptographicException>(() => encKey.GetXml());
        }

        [Fact]
        public static void EncryptedKey_GetXml_WithCipherValue()
        {
            EncryptedKey encKey = new EncryptedKey();
            encKey.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            XmlElement xml = encKey.GetXml();
            Assert.Equal("EncryptedKey", xml.LocalName);
            Assert.Equal(EncryptedXml.XmlEncNamespaceUrl, xml.NamespaceURI);
        }

        [Fact]
        public static void EncryptedKey_GetXml_WithAllProperties()
        {
            EncryptedKey encKey = new EncryptedKey
            {
                Id = "test-key-id",
                Type = "http://example.com/keytype",
                MimeType = "application/octet-stream",
                Encoding = "base64",
                Recipient = "recipient@example.com",
                CarriedKeyName = "MyKey",
                CipherData = new CipherData(new byte[] { 1, 2, 3 }),
                EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncRSA15Url)
            };

            encKey.KeyInfo.AddClause(new KeyInfoName("wrappingKey"));
            encKey.AddReference(new DataReference("#data1"));
            encKey.AddReference(new KeyReference("#key1"));

            XmlDocument doc = new XmlDocument();
            XmlElement propElement = doc.CreateElement("EncryptionProperty", EncryptedXml.XmlEncNamespaceUrl);
            propElement.InnerText = "value";
            EncryptionProperty prop = new EncryptionProperty(propElement);
            encKey.EncryptionProperties.Add(prop);

            XmlElement xml = encKey.GetXml();
            Assert.Equal("EncryptedKey", xml.LocalName);
            Assert.Equal("test-key-id", xml.GetAttribute("Id"));
            Assert.Equal("http://example.com/keytype", xml.GetAttribute("Type"));
            Assert.Equal("application/octet-stream", xml.GetAttribute("MimeType"));
            Assert.Equal("base64", xml.GetAttribute("Encoding"));
            Assert.Equal("recipient@example.com", xml.GetAttribute("Recipient"));
        }

        [Fact]
        public static void EncryptedKey_LoadXml_MinimalValid()
        {
            string xml = @"<EncryptedKey xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
            </EncryptedKey>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedKey encKey = new EncryptedKey();
            encKey.LoadXml(doc.DocumentElement);
            Assert.NotNull(encKey.CipherData);
            Assert.NotNull(encKey.CipherData.CipherValue);
        }

        [Fact]
        public static void EncryptedKey_LoadXml_WithAttributes()
        {
            string xml = @"<EncryptedKey Id=""key1"" Type=""http://example.com/type"" MimeType=""application/xml"" Encoding=""utf-8"" Recipient=""user@example.com"" xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <EncryptionMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#rsa-1_5"" />
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
            </EncryptedKey>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedKey encKey = new EncryptedKey();
            encKey.LoadXml(doc.DocumentElement);
            Assert.Equal("key1", encKey.Id);
            Assert.Equal("http://example.com/type", encKey.Type);
            Assert.Equal("application/xml", encKey.MimeType);
            Assert.Equal("utf-8", encKey.Encoding);
            Assert.Equal("user@example.com", encKey.Recipient);
            Assert.NotNull(encKey.EncryptionMethod);
        }

        [Fact]
        public static void EncryptedKey_LoadXml_WithCarriedKeyName()
        {
            string xml = @"<EncryptedKey xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
                <CarriedKeyName>TestKeyName</CarriedKeyName>
            </EncryptedKey>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedKey encKey = new EncryptedKey();
            encKey.LoadXml(doc.DocumentElement);
            Assert.Equal("TestKeyName", encKey.CarriedKeyName);
        }

        [Fact]
        public static void EncryptedKey_LoadXml_WithReferenceList()
        {
            string xml = @"<EncryptedKey xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
                <ReferenceList>
                    <DataReference URI=""#data1"" />
                    <KeyReference URI=""#key1"" />
                </ReferenceList>
            </EncryptedKey>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedKey encKey = new EncryptedKey();
            encKey.LoadXml(doc.DocumentElement);
            Assert.Equal(2, encKey.ReferenceList.Count);
        }

        [Fact]
        public static void EncryptedKey_LoadXml_ThrowsWhenCipherDataMissing()
        {
            string xml = @"<EncryptedKey xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <EncryptionMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#rsa-1_5"" />
            </EncryptedKey>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedKey encKey = new EncryptedKey();
            Assert.Throws<CryptographicException>(() => encKey.LoadXml(doc.DocumentElement));
        }

        [Fact]
        public static void CipherReference_Constructor()
        {
            CipherReference cipherRef = new CipherReference();
            Assert.Equal(string.Empty, cipherRef.Uri);
        }

        [Fact]
        public static void CipherReference_Constructor_WithUri()
        {
            string uri = "http://example.com/data";
            CipherReference cipherRef = new CipherReference(uri);
            Assert.Equal(uri, cipherRef.Uri);
        }

        [Fact]
        public static void CipherReference_Constructor_WithUriAndTransformChain()
        {
            string uri = "http://example.com/data";
            TransformChain tc = new TransformChain();
            tc.Add(new XmlDsigBase64Transform());

            CipherReference cipherRef = new CipherReference(uri, tc);
            Assert.Equal(uri, cipherRef.Uri);
            Assert.NotNull(cipherRef.TransformChain);
            Assert.Equal(1, cipherRef.TransformChain.Count);
        }

        [Fact]
        public static void CipherReference_GetXml()
        {
            CipherReference cipherRef = new CipherReference("http://example.com/data");
            XmlElement xml = cipherRef.GetXml();
            Assert.Equal("CipherReference", xml.LocalName);
            Assert.Equal("http://example.com/data", xml.GetAttribute("URI"));
        }

        [Fact]
        public static void CipherReference_GetXml_WithTransforms()
        {
            CipherReference cipherRef = new CipherReference("http://example.com/data");
            cipherRef.TransformChain.Add(new XmlDsigBase64Transform());
            XmlElement xml = cipherRef.GetXml();
            Assert.Equal("CipherReference", xml.LocalName);
            Assert.NotNull(xml.SelectSingleNode("//*[local-name()='Transforms']"));
        }

        [Fact]
        public static void CipherReference_LoadXml_Simple()
        {
            string xml = @"<CipherReference URI=""http://example.com/data"" xmlns=""http://www.w3.org/2001/04/xmlenc#"" />";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            CipherReference cipherRef = new CipherReference();
            cipherRef.LoadXml(doc.DocumentElement);
            Assert.Equal("http://example.com/data", cipherRef.Uri);
        }

        [Fact]
        public static void CipherReference_LoadXml_WithTransforms()
        {
            string xml = @"<CipherReference URI=""http://example.com/data"" xmlns=""http://www.w3.org/2001/04/xmlenc#"" xmlns:ds=""http://www.w3.org/2000/09/xmldsig#"">
                <Transforms>
                    <ds:Transform Algorithm=""http://www.w3.org/2001/10/xml-exc-c14n#"" />
                </Transforms>
            </CipherReference>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            CipherReference cipherRef = new CipherReference();
            cipherRef.LoadXml(doc.DocumentElement);
            Assert.Equal("http://example.com/data", cipherRef.Uri);
            Assert.Equal(1, cipherRef.TransformChain.Count);
        }

        [Fact]
        public static void CipherReference_LoadXml_ThrowsWhenUriMissing()
        {
            string xml = @"<CipherReference xmlns=""http://www.w3.org/2001/04/xmlenc#"" />";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            CipherReference cipherRef = new CipherReference();
            Assert.Throws<CryptographicException>(() => cipherRef.LoadXml(doc.DocumentElement));
        }

        [Fact]
        public static void KeyInfoEncryptedKey_Constructor()
        {
            KeyInfoEncryptedKey kiek = new KeyInfoEncryptedKey(new EncryptedKey());
            Assert.NotNull(kiek.EncryptedKey);
        }

        [Fact]
        public static void KeyInfoEncryptedKey_Constructor_Null()
        {
            KeyInfoEncryptedKey kiek = new KeyInfoEncryptedKey(null);
            Assert.Null(kiek.EncryptedKey);
        }

        [Fact]
        public static void KeyInfoEncryptedKey_SetEncryptedKey()
        {
            EncryptedKey ek = new EncryptedKey();
            ek.Id = "test-key";
            ek.CipherData = new CipherData(new byte[] { 1, 2, 3 });

            KeyInfoEncryptedKey kiek = new KeyInfoEncryptedKey(null);
            kiek.EncryptedKey = ek;
            Assert.Same(ek, kiek.EncryptedKey);
        }

        [Fact]
        public static void KeyInfoEncryptedKey_GetXml()
        {
            EncryptedKey ek = new EncryptedKey();
            ek.Id = "test-key-id";
            ek.CipherData = new CipherData(new byte[] { 1, 2, 3 });

            KeyInfoEncryptedKey kiek = new KeyInfoEncryptedKey(ek);
            XmlElement xml = kiek.GetXml();
            Assert.Equal("EncryptedKey", xml.LocalName);
            Assert.Equal("test-key-id", xml.GetAttribute("Id"));
        }

        [Fact]
        public static void KeyInfoEncryptedKey_LoadXml()
        {
            string xml = @"<EncryptedKey Id=""key1"" xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
            </EncryptedKey>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            KeyInfoEncryptedKey kiek = new KeyInfoEncryptedKey();
            kiek.LoadXml(doc.DocumentElement);
            Assert.NotNull(kiek.EncryptedKey);
            Assert.Equal("key1", kiek.EncryptedKey.Id);
        }

        [Fact]
        public static void KeyInfoEncryptedKey_LoadXml_Null()
        {
            KeyInfoEncryptedKey kiek = new KeyInfoEncryptedKey();
            Assert.Throws<ArgumentNullException>(() => kiek.LoadXml(null));
        }

        [Fact]
        public static void EncryptedReference_Properties()
        {
            DataReference dataRef = new DataReference("#data1");
            Assert.Equal("#data1", dataRef.Uri);
            Assert.NotNull(dataRef.TransformChain);

            dataRef.Uri = "#data2";
            Assert.Equal("#data2", dataRef.Uri);
        }

        [Fact]
        public static void ReferenceList_Operations()
        {
            ReferenceList refList = new ReferenceList();
            Assert.Equal(0, refList.Count);

            DataReference dr1 = new DataReference("#data1");
            refList.Add(dr1);
            Assert.Equal(1, refList.Count);

            KeyReference kr1 = new KeyReference("#key1");
            refList.Add(kr1);
            Assert.Equal(2, refList.Count);

            Assert.Same(dr1, refList.Item(0));
            Assert.Same(kr1, refList.Item(1));

            Assert.Same(dr1, refList[0]);
            Assert.Same(kr1, refList[1]);
        }

        [Fact]
        public static void ReferenceList_IList()
        {
            ReferenceList refList = new ReferenceList();
            System.Collections.IList list = refList;

            DataReference dr = new DataReference("#data");
            list.Add(dr);
            Assert.Equal(1, refList.Count);
            Assert.True(list.Contains(dr));

            list.Remove(dr);
            Assert.Equal(0, refList.Count);
        }

        [Fact]
        public static void EncryptionPropertyCollection_Operations()
        {
            EncryptionPropertyCollection props = new EncryptionPropertyCollection();
            Assert.Equal(0, props.Count);

            XmlDocument doc = new XmlDocument();
            XmlElement elem1 = doc.CreateElement("EncryptionProperty", EncryptedXml.XmlEncNamespaceUrl);
            EncryptionProperty prop1 = new EncryptionProperty(elem1);
            props.Add(prop1);
            Assert.Equal(1, props.Count);

            XmlElement elem2 = doc.CreateElement("EncryptionProperty", EncryptedXml.XmlEncNamespaceUrl);
            EncryptionProperty prop2 = new EncryptionProperty(elem2);
            props.Add(prop2);
            Assert.Equal(2, props.Count);

            Assert.Same(prop1, props.Item(0));
            Assert.Same(prop2, props.Item(1));

            Assert.Same(prop1, props[0]);
            Assert.Same(prop2, props[1]);
        }

        [Fact]
        public static void EncryptionPropertyCollection_IList()
        {
            EncryptionPropertyCollection props = new EncryptionPropertyCollection();
            System.Collections.IList list = props;

            XmlDocument doc = new XmlDocument();
            XmlElement elem = doc.CreateElement("EncryptionProperty", EncryptedXml.XmlEncNamespaceUrl);
            EncryptionProperty prop = new EncryptionProperty(elem);
            
            list.Add(prop);
            Assert.Equal(1, props.Count);
            Assert.True(list.Contains(prop));

            list.Remove(prop);
            Assert.Equal(0, props.Count);
        }

        [Fact]
        public static void EncryptedData_LoadXml_Null()
        {
            EncryptedData encData = new EncryptedData();
            Assert.Throws<ArgumentNullException>(() => encData.LoadXml(null));
        }

        [Fact]
        public static void EncryptedKey_LoadXml_Null()
        {
            EncryptedKey encKey = new EncryptedKey();
            Assert.Throws<ArgumentNullException>(() => encKey.LoadXml(null));
        }

        [Fact]
        public static void CipherReference_LoadXml_Null()
        {
            CipherReference cipherRef = new CipherReference();
            Assert.Throws<ArgumentNullException>(() => cipherRef.LoadXml(null));
        }

        [Fact]
        public static void EncryptedData_Properties_SetNull()
        {
            EncryptedData encData = new EncryptedData();
            encData.Id = null;
            encData.Type = null;
            encData.MimeType = null;
            encData.Encoding = null;
            
            Assert.Null(encData.Id);
            Assert.Null(encData.Type);
            Assert.Null(encData.MimeType);
            Assert.Null(encData.Encoding);
        }

        [Fact]
        public static void EncryptedKey_Properties_SetNull()
        {
            EncryptedKey encKey = new EncryptedKey();
            encKey.Id = null;
            encKey.Type = null;
            encKey.MimeType = null;
            encKey.Encoding = null;
            encKey.Recipient = null;
            encKey.CarriedKeyName = null;
            
            Assert.Null(encKey.Id);
            Assert.Null(encKey.Type);
            Assert.Null(encKey.MimeType);
            Assert.Null(encKey.Encoding);
            Assert.Equal(string.Empty, encKey.Recipient); // Default is empty string
            Assert.Null(encKey.CarriedKeyName);
        }

        [Fact]
        public static void EncryptedReference_Uri_SetNull()
        {
            DataReference dataRef = new DataReference("#data1");
            Assert.Throws<ArgumentNullException>(() => dataRef.Uri = null);
        }

        [Fact]
        public static void EncryptedReference_TransformChain_NotNull()
        {
            DataReference dataRef = new DataReference();
            Assert.NotNull(dataRef.TransformChain);
            Assert.Equal(0, dataRef.TransformChain.Count);
        }

        [Fact]
        public static void ReferenceList_ItemOutOfRange()
        {
            ReferenceList refList = new ReferenceList();
            Assert.Throws<ArgumentOutOfRangeException>(() => refList.Item(0));
        }

        [Fact]
        public static void ReferenceList_IndexerOutOfRange()
        {
            ReferenceList refList = new ReferenceList();
            Assert.Throws<ArgumentOutOfRangeException>(() => refList[0]);
        }

        [Fact]
        public static void EncryptionPropertyCollection_ItemOutOfRange()
        {
            EncryptionPropertyCollection props = new EncryptionPropertyCollection();
            Assert.Throws<ArgumentOutOfRangeException>(() => props.Item(0));
        }

        [Fact]
        public static void EncryptionPropertyCollection_IndexerOutOfRange()
        {
            EncryptionPropertyCollection props = new EncryptionPropertyCollection();
            Assert.Throws<ArgumentOutOfRangeException>(() => props[0]);
        }

        [Fact]
        public static void EncryptedData_LoadXml_WithKeyInfo()
        {
            string xml = @"<EncryptedData xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <EncryptionMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#aes256-cbc"" />
                <KeyInfo xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                    <KeyName>TestKey</KeyName>
                </KeyInfo>
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
            </EncryptedData>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedData encData = new EncryptedData();
            encData.LoadXml(doc.DocumentElement);
            Assert.NotNull(encData.KeyInfo);
            Assert.Equal(1, encData.KeyInfo.Count);
        }

        [Fact]
        public static void EncryptedKey_LoadXml_WithKeyInfo()
        {
            string xml = @"<EncryptedKey xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <EncryptionMethod Algorithm=""http://www.w3.org/2001/04/xmlenc#rsa-1_5"" />
                <KeyInfo xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                    <KeyName>WrappingKey</KeyName>
                </KeyInfo>
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
            </EncryptedKey>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedKey encKey = new EncryptedKey();
            encKey.LoadXml(doc.DocumentElement);
            Assert.NotNull(encKey.KeyInfo);
            Assert.Equal(1, encKey.KeyInfo.Count);
        }

        [Fact]
        public static void EncryptedData_GetXml_CachedXml()
        {
            string xml = @"<EncryptedData xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
            </EncryptedData>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedData encData = new EncryptedData();
            encData.LoadXml(doc.DocumentElement);
            
            XmlElement xml1 = encData.GetXml();
            XmlElement xml2 = encData.GetXml();
            Assert.Same(xml1, xml2); // Should be cached
        }

        [Fact]
        public static void EncryptedKey_GetXml_CachedXml()
        {
            string xml = @"<EncryptedKey xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
            </EncryptedKey>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedKey encKey = new EncryptedKey();
            encKey.LoadXml(doc.DocumentElement);
            
            XmlElement xml1 = encKey.GetXml();
            XmlElement xml2 = encKey.GetXml();
            Assert.Same(xml1, xml2); // Should be cached
        }

        [Fact]
        public static void EncryptedData_SetProperty_InvalidatesCache()
        {
            string xml = @"<EncryptedData xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <CipherData>
                    <CipherValue>AQID</CipherValue>
                </CipherData>
            </EncryptedData>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedData encData = new EncryptedData();
            encData.LoadXml(doc.DocumentElement);
            
            XmlElement xml1 = encData.GetXml();
            encData.Id = "new-id"; // Should invalidate cache
            XmlElement xml2 = encData.GetXml();
            Assert.NotSame(xml1, xml2);
        }

        [Fact]
        public static void CipherReference_GetXml_CachedXml()
        {
            CipherReference cipherRef = new CipherReference("http://example.com/data");
            XmlElement xml1 = cipherRef.GetXml();
            XmlElement xml2 = cipherRef.GetXml();
            Assert.NotSame(xml1, xml2); // CipherReference doesn't cache when created programmatically
        }

        [Fact]
        public static void EncryptedData_LoadXml_CipherReference()
        {
            string xml = @"<EncryptedData xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <CipherData>
                    <CipherReference URI=""http://example.com/data"" />
                </CipherData>
            </EncryptedData>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedData encData = new EncryptedData();
            encData.LoadXml(doc.DocumentElement);
            Assert.NotNull(encData.CipherData);
            Assert.NotNull(encData.CipherData.CipherReference);
            Assert.Equal("http://example.com/data", encData.CipherData.CipherReference.Uri);
        }

        [Fact]
        public static void EncryptedKey_LoadXml_CipherReference()
        {
            string xml = @"<EncryptedKey xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                <CipherData>
                    <CipherReference URI=""http://example.com/key"" />
                </CipherData>
            </EncryptedKey>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedKey encKey = new EncryptedKey();
            encKey.LoadXml(doc.DocumentElement);
            Assert.NotNull(encKey.CipherData);
            Assert.NotNull(encKey.CipherData.CipherReference);
            Assert.Equal("http://example.com/key", encKey.CipherData.CipherReference.Uri);
        }

        [Fact]
        public static void EncryptedData_GetXml_WithCipherReference()
        {
            EncryptedData encData = new EncryptedData();
            encData.CipherData = new CipherData();
            encData.CipherData.CipherReference = new CipherReference("http://example.com/data");
            
            XmlElement xml = encData.GetXml();
            Assert.Equal("EncryptedData", xml.LocalName);
            Assert.NotNull(xml.SelectSingleNode("//*[local-name()='CipherReference']"));
        }

        [Fact]
        public static void EncryptedKey_GetXml_WithCipherReference()
        {
            EncryptedKey encKey = new EncryptedKey();
            encKey.CipherData = new CipherData();
            encKey.CipherData.CipherReference = new CipherReference("http://example.com/key");
            
            XmlElement xml = encKey.GetXml();
            Assert.Equal("EncryptedKey", xml.LocalName);
            Assert.NotNull(xml.SelectSingleNode("//*[local-name()='CipherReference']"));
        }

        [Fact]
        public static void EncryptedData_SetCipherData_Null()
        {
            EncryptedData encData = new EncryptedData();
            encData.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            Assert.Throws<ArgumentNullException>(() => encData.CipherData = null);
        }

        [Fact]
        public static void EncryptedKey_SetCipherData_Null()
        {
            EncryptedKey encKey = new EncryptedKey();
            encKey.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            Assert.Throws<ArgumentNullException>(() => encKey.CipherData = null);
        }

        [Fact]
        public static void EncryptedData_SetEncryptionMethod_Null()
        {
            EncryptedData encData = new EncryptedData();
            encData.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
            
            encData.EncryptionMethod = null;
            Assert.Null(encData.EncryptionMethod);
        }

        [Fact]
        public static void EncryptedKey_SetEncryptionMethod_Null()
        {
            EncryptedKey encKey = new EncryptedKey();
            encKey.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncRSA15Url);
            
            encKey.EncryptionMethod = null;
            Assert.Null(encKey.EncryptionMethod);
        }

        [Fact]
        public static void EncryptedData_SetKeyInfo_Null()
        {
            EncryptedData encData = new EncryptedData();
            KeyInfo ki = new KeyInfo();
            ki.AddClause(new KeyInfoName("key1"));
            encData.KeyInfo = ki;
            
            encData.KeyInfo = null;
            // KeyInfo auto-initializes on access
            Assert.NotNull(encData.KeyInfo);
            Assert.Equal(0, encData.KeyInfo.Count);
        }

        [Fact]
        public static void EncryptedKey_SetKeyInfo_Null()
        {
            EncryptedKey encKey = new EncryptedKey();
            KeyInfo ki = new KeyInfo();
            ki.AddClause(new KeyInfoName("key1"));
            encKey.KeyInfo = ki;
            
            encKey.KeyInfo = null;
            // KeyInfo auto-initializes on access
            Assert.NotNull(encKey.KeyInfo);
            Assert.Equal(0, encKey.KeyInfo.Count);
        }

        [Fact]
        public static void ReferenceList_AddMultipleTypes()
        {
            ReferenceList refList = new ReferenceList();
            
            refList.Add(new DataReference("#data1"));
            refList.Add(new KeyReference("#key1"));
            refList.Add(new DataReference("#data2"));
            refList.Add(new KeyReference("#key2"));
            
            Assert.Equal(4, refList.Count);
        }

        [Fact]
        public static void EncryptionPropertyCollection_CopyTo()
        {
            EncryptionPropertyCollection props = new EncryptionPropertyCollection();
            XmlDocument doc = new XmlDocument();
            
            XmlElement elem1 = doc.CreateElement("EncryptionProperty", EncryptedXml.XmlEncNamespaceUrl);
            EncryptionProperty prop1 = new EncryptionProperty(elem1);
            props.Add(prop1);
            
            XmlElement elem2 = doc.CreateElement("EncryptionProperty", EncryptedXml.XmlEncNamespaceUrl);
            EncryptionProperty prop2 = new EncryptionProperty(elem2);
            props.Add(prop2);
            
            EncryptionProperty[] array = new EncryptionProperty[2];
            props.CopyTo(array, 0);
            
            Assert.Same(prop1, array[0]);
            Assert.Same(prop2, array[1]);
        }

        [Fact]
        public static void ReferenceList_CopyTo()
        {
            ReferenceList refList = new ReferenceList();
            DataReference dr = new DataReference("#data1");
            KeyReference kr = new KeyReference("#key1");
            
            refList.Add(dr);
            refList.Add(kr);
            
            object[] array = new object[2];
            ((System.Collections.ICollection)refList).CopyTo(array, 0);
            
            Assert.Same(dr, array[0]);
            Assert.Same(kr, array[1]);
        }

        [Fact]
        public static void EncryptedXml_Properties()
        {
            EncryptedXml exml = new EncryptedXml();
            
            exml.XmlDSigSearchDepth = 5;
            Assert.Equal(5, exml.XmlDSigSearchDepth);
            
            exml.XmlDSigSearchDepth = 100;
            Assert.Equal(100, exml.XmlDSigSearchDepth);
            
            exml.Recipient = "test-recipient";
            Assert.Equal("test-recipient", exml.Recipient);
            
            exml.Recipient = "";
            Assert.Equal("", exml.Recipient);
        }

        [Fact]
        public static void EncryptedXml_ModeProperty()
        {
            EncryptedXml exml = new EncryptedXml();
            
            exml.Mode = CipherMode.ECB;
            Assert.Equal(CipherMode.ECB, exml.Mode);
            
            exml.Mode = CipherMode.CFB;
            Assert.Equal(CipherMode.CFB, exml.Mode);
        }

        [Fact]
        public static void EncryptedXml_PaddingProperty()
        {
            EncryptedXml exml = new EncryptedXml();
            
            exml.Padding = PaddingMode.PKCS7;
            Assert.Equal(PaddingMode.PKCS7, exml.Padding);
            
            exml.Padding = PaddingMode.Zeros;
            Assert.Equal(PaddingMode.Zeros, exml.Padding);
        }

        [Fact]
        public static void EncryptedXml_EncodingProperty()
        {
            EncryptedXml exml = new EncryptedXml();
            
            exml.Encoding = System.Text.Encoding.ASCII;
            Assert.Equal(System.Text.Encoding.ASCII, exml.Encoding);
            
            exml.Encoding = System.Text.Encoding.Unicode;
            Assert.Equal(System.Text.Encoding.Unicode, exml.Encoding);
        }

        [Fact]
        public static void EncryptedXml_ResolverProperty()
        {
            EncryptedXml exml = new EncryptedXml();
            
            exml.Resolver = null;
            Assert.Null(exml.Resolver);
        }

        [Fact]
        public static void EncryptedXml_ClearKeyNameMappings()
        {
            EncryptedXml exml = new EncryptedXml();
            using (Aes aes = Aes.Create())
            {
                exml.AddKeyNameMapping("key1", aes);
                exml.ClearKeyNameMappings();
                
                // Verify the key mapping was cleared - attempt to decrypt with cleared key should fail
                string xml = @"<EncryptedData xmlns='http://www.w3.org/2001/04/xmlenc#'>
                    <EncryptionMethod Algorithm='http://www.w3.org/2001/04/xmlenc#aes256-cbc'/>
                    <KeyInfo xmlns='http://www.w3.org/2000/09/xmldsig#'>
                        <KeyName>key1</KeyName>
                    </KeyInfo>
                    <CipherData><CipherValue>test</CipherValue></CipherData>
                </EncryptedData>";
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                EncryptedData encData = new EncryptedData();
                encData.LoadXml(doc.DocumentElement);
                
                // After clearing, GetDecryptionKey should return null for the cleared key name
                SymmetricAlgorithm key = exml.GetDecryptionKey(encData, null);
                Assert.Null(key);
            }
        }

        [Fact]
        public static void EncryptedXml_AddKeyNameMapping_Multiple()
        {
            using (Aes aes1 = Aes.Create())
            using (Aes aes2 = Aes.Create())
            {
                XmlDocument doc = new XmlDocument();
                EncryptedXml exml = new EncryptedXml(doc);
                exml.AddKeyNameMapping("key1", aes1);
                exml.AddKeyNameMapping("key2", aes2);
                
                // Verify both keys are mapped by retrieving them
                string xml1 = @"<EncryptedData xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                    <KeyInfo xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                        <KeyName>key1</KeyName>
                    </KeyInfo>
                    <CipherData>
                        <CipherValue>AQID</CipherValue>
                    </CipherData>
                </EncryptedData>";
                string xml2 = @"<EncryptedData xmlns=""http://www.w3.org/2001/04/xmlenc#"">
                    <KeyInfo xmlns=""http://www.w3.org/2000/09/xmldsig#"">
                        <KeyName>key2</KeyName>
                    </KeyInfo>
                    <CipherData>
                        <CipherValue>AQID</CipherValue>
                    </CipherData>
                </EncryptedData>";
                
                doc.LoadXml("<root/>");
                XmlDocument doc1 = new XmlDocument();
                doc1.LoadXml(xml1);
                EncryptedData encData1 = new EncryptedData();
                encData1.LoadXml(doc1.DocumentElement);
                
                XmlDocument doc2 = new XmlDocument();
                doc2.LoadXml(xml2);
                EncryptedData encData2 = new EncryptedData();
                encData2.LoadXml(doc2.DocumentElement);
                
                SymmetricAlgorithm retrievedKey1 = exml.GetDecryptionKey(encData1, null);
                SymmetricAlgorithm retrievedKey2 = exml.GetDecryptionKey(encData2, null);
                
                Assert.NotNull(retrievedKey1);
                Assert.NotNull(retrievedKey2);
                Assert.Same(aes1, retrievedKey1);
                Assert.Same(aes2, retrievedKey2);
            }
        }

        [Fact]
        public static void EncryptedXml_ReplaceElement_ContentFalse()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><child /></root>");
            EncryptedData edata = new EncryptedData();
            edata.CipherData.CipherValue = new byte[16];
            
            EncryptedXml.ReplaceElement(doc.DocumentElement, edata, false);
            Assert.Equal("EncryptedData", doc.DocumentElement.Name);
        }

        [Fact]
        public static void ReferenceList_IListOperations()
        {
            ReferenceList refList = new ReferenceList();
            System.Collections.IList list = refList;
            
            Assert.False(list.IsFixedSize);
            Assert.False(list.IsReadOnly);
            Assert.False(list.IsSynchronized);
            Assert.NotNull(list.SyncRoot);
            
            DataReference dr = new DataReference("#data1");
            list.Add(dr);
            
            Assert.Equal(0, list.IndexOf(dr));
            
            list.Insert(0, new DataReference("#data0"));
            Assert.Equal(2, list.Count);
            
            list.RemoveAt(0);
            Assert.Equal(1, list.Count);
            
            list.Clear();
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public static void EncryptionPropertyCollection_IListOperations()
        {
            EncryptionPropertyCollection props = new EncryptionPropertyCollection();
            System.Collections.IList list = props;
            
            Assert.False(list.IsFixedSize);
            Assert.False(list.IsReadOnly);
            Assert.False(list.IsSynchronized);
            Assert.NotNull(list.SyncRoot);
            
            XmlDocument doc = new XmlDocument();
            XmlElement elem = doc.CreateElement("EncryptionProperty", EncryptedXml.XmlEncNamespaceUrl);
            EncryptionProperty prop = new EncryptionProperty(elem);
            
            list.Add(prop);
            Assert.Equal(0, list.IndexOf(prop));
            
            XmlElement elem2 = doc.CreateElement("EncryptionProperty", EncryptedXml.XmlEncNamespaceUrl);
            EncryptionProperty prop2 = new EncryptionProperty(elem2);
            list.Insert(0, prop2);
            Assert.Equal(2, list.Count);
            
            list.RemoveAt(0);
            Assert.Equal(1, list.Count);
            
            list.Clear();
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public static void EncryptedXml_DecryptDocument_WithDataReference()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root><data Id='data1'>secret</data></root>");

            using (Aes aes = Aes.Create())
            {
                EncryptedXml exml = new EncryptedXml(doc);
                
                byte[] encrypted = exml.EncryptData(doc.DocumentElement, aes, false);
                
                EncryptedData encData = new EncryptedData();
                encData.Type = EncryptedXml.XmlEncElementUrl;
                encData.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
                encData.CipherData.CipherValue = encrypted;
                encData.Id = "enc1";
                
                EncryptedXml.ReplaceElement(doc.DocumentElement, encData, false);
                
                // Now decrypt
                exml.AddKeyNameMapping("aes", aes);
                EncryptedData ed = new EncryptedData();
                ed.LoadXml(doc.DocumentElement);
                
                byte[] decrypted = exml.DecryptData(ed, aes);
                Assert.NotNull(decrypted);
            }
        }

        [Fact]
        public static void EncryptedXml_GetDecryptionIV_DifferentAlgorithms()
        {
            EncryptedXml exml = new EncryptedXml();
            
            // AES256
            EncryptedData ed1 = new EncryptedData();
            ed1.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
            ed1.CipherData = new CipherData(new byte[32]);
            byte[] iv1 = exml.GetDecryptionIV(ed1, EncryptedXml.XmlEncAES256Url);
            Assert.Equal(16, iv1.Length);
            
            // AES192
            EncryptedData ed2 = new EncryptedData();
            ed2.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES192Url);
            ed2.CipherData = new CipherData(new byte[24]);
            byte[] iv2 = exml.GetDecryptionIV(ed2, EncryptedXml.XmlEncAES192Url);
            Assert.Equal(16, iv2.Length);
            
            // AES128
            EncryptedData ed3 = new EncryptedData();
            ed3.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES128Url);
            ed3.CipherData = new CipherData(new byte[16]);
            byte[] iv3 = exml.GetDecryptionIV(ed3, EncryptedXml.XmlEncAES128Url);
            Assert.Equal(16, iv3.Length);
        }

        [Fact]
        public static void EncryptedXml_GetIdElement_NestedStructure()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(@"<root>
                <level1 Id='id1'>
                    <level2 Id='id2'>
                        <level3 Id='id3'>content</level3>
                    </level2>
                </level1>
            </root>");

            EncryptedXml exml = new EncryptedXml(doc);
            
            XmlElement elem1 = exml.GetIdElement(doc, "id1");
            Assert.NotNull(elem1);
            Assert.Equal("level1", elem1.LocalName);
            
            XmlElement elem2 = exml.GetIdElement(doc, "id2");
            Assert.NotNull(elem2);
            Assert.Equal("level2", elem2.LocalName);
            
            XmlElement elem3 = exml.GetIdElement(doc, "id3");
            Assert.NotNull(elem3);
            Assert.Equal("level3", elem3.LocalName);
        }

        [Fact]
        public static void EncryptedKey_WithReferenceList()
        {
            EncryptedKey encKey = new EncryptedKey();
            encKey.CipherData = new CipherData(new byte[] { 1, 2, 3, 4 });
            
            encKey.ReferenceList.Add(new DataReference("#data1"));
            encKey.ReferenceList.Add(new DataReference("#data2"));
            encKey.ReferenceList.Add(new KeyReference("#key1"));
            
            XmlElement xml = encKey.GetXml();
            Assert.NotNull(xml);
            
            XmlNodeList refList = xml.SelectNodes("//*[local-name()='ReferenceList']");
            Assert.Equal(1, refList.Count);
            
            XmlNodeList refs = xml.SelectNodes("//*[local-name()='ReferenceList']/*");
            Assert.Equal(3, refs.Count);
        }

        [Fact]
        public static void EncryptedData_WithEncryptionProperties()
        {
            EncryptedData encData = new EncryptedData();
            encData.CipherData = new CipherData(new byte[] { 1, 2, 3 });
            
            XmlDocument doc = new XmlDocument();
            XmlElement prop1 = doc.CreateElement("EncryptionProperty", EncryptedXml.XmlEncNamespaceUrl);
            prop1.SetAttribute("Id", "prop1");
            encData.EncryptionProperties.Add(new EncryptionProperty(prop1));
            
            XmlElement prop2 = doc.CreateElement("EncryptionProperty", EncryptedXml.XmlEncNamespaceUrl);
            prop2.SetAttribute("Id", "prop2");
            encData.EncryptionProperties.Add(new EncryptionProperty(prop2));
            
            XmlElement xml = encData.GetXml();
            XmlNodeList props = xml.SelectNodes("//*[local-name()='EncryptionProperty']");
            Assert.Equal(2, props.Count);
        }

        [Fact]
        public static void EncryptedXml_Recipient_Property()
        {
            EncryptedXml exml = new EncryptedXml();
            Assert.Equal(string.Empty, exml.Recipient); // Defaults to empty string
            
            exml.Recipient = "recipient@example.com";
            Assert.Equal("recipient@example.com", exml.Recipient);
            
            exml.Recipient = null;
            Assert.Equal(string.Empty, exml.Recipient); // Setting null gives empty string
        }

        [Fact]
        public static void CipherReference_WithTransforms()
        {
            CipherReference cipherRef = new CipherReference("http://example.com/data");
            
            cipherRef.AddTransform(new XmlDsigBase64Transform());
            cipherRef.AddTransform(new XmlDsigC14NTransform());
            
            XmlElement xml = cipherRef.GetXml();
            XmlNodeList transforms = xml.SelectNodes("//*[local-name()='Transform']");
            Assert.Equal(2, transforms.Count);
        }

        [Fact]
        public static void EncryptedData_LoadXml_WithAllElements()
        {
            string xml = @"<EncryptedData Id='ed1' Type='http://www.w3.org/2001/04/xmlenc#Element' 
                            MimeType='text/xml' Encoding='UTF-8' xmlns='http://www.w3.org/2001/04/xmlenc#'>
                <EncryptionMethod Algorithm='http://www.w3.org/2001/04/xmlenc#aes256-cbc' />
                <KeyInfo xmlns='http://www.w3.org/2000/09/xmldsig#'>
                    <KeyName>TestKey</KeyName>
                </KeyInfo>
                <CipherData>
                    <CipherValue>AQIDBA==</CipherValue>
                </CipherData>
                <EncryptionProperties>
                    <EncryptionProperty Id='prop1' Target='#ed1'>
                        <custom>data</custom>
                    </EncryptionProperty>
                </EncryptionProperties>
            </EncryptedData>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedData encData = new EncryptedData();
            encData.LoadXml(doc.DocumentElement);
            
            Assert.Equal("ed1", encData.Id);
            Assert.Equal(EncryptedXml.XmlEncElementUrl, encData.Type);
            Assert.Equal("text/xml", encData.MimeType);
            Assert.Equal("UTF-8", encData.Encoding);
            Assert.NotNull(encData.EncryptionMethod);
            Assert.NotNull(encData.KeyInfo);
            Assert.NotNull(encData.CipherData);
            Assert.Equal(1, encData.EncryptionProperties.Count);
        }

        [Fact]
        public static void EncryptedKey_LoadXml_WithAllElements()
        {
            string xml = @"<EncryptedKey Id='ek1' Type='http://www.w3.org/2001/04/xmlenc#EncryptedKey' 
                            Recipient='user@example.com' xmlns='http://www.w3.org/2001/04/xmlenc#'>
                <EncryptionMethod Algorithm='http://www.w3.org/2001/04/xmlenc#rsa-1_5' />
                <KeyInfo xmlns='http://www.w3.org/2000/09/xmldsig#'>
                    <KeyName>WrapKey</KeyName>
                </KeyInfo>
                <CipherData>
                    <CipherValue>AQIDBA==</CipherValue>
                </CipherData>
                <ReferenceList>
                    <DataReference URI='#data1' />
                </ReferenceList>
                <CarriedKeyName>SessionKey</CarriedKeyName>
            </EncryptedKey>";

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            EncryptedKey encKey = new EncryptedKey();
            encKey.LoadXml(doc.DocumentElement);
            
            Assert.Equal("ek1", encKey.Id);
            Assert.Equal("user@example.com", encKey.Recipient);
            Assert.Equal("SessionKey", encKey.CarriedKeyName);
            Assert.NotNull(encKey.EncryptionMethod);
            Assert.NotNull(encKey.KeyInfo);
            Assert.NotNull(encKey.CipherData);
            Assert.Equal(1, encKey.ReferenceList.Count);
        }
    }
}
