// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// EncryptedXmlTest.cs
//
// Author:
//  Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class EncryptedXmlTest
    {
        private class NotSupportedSymmetricAlgorithm : SymmetricAlgorithm
        {
            public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotImplementedException();
            }

            public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotImplementedException();
            }

            public override void GenerateIV()
            {
                throw new NotImplementedException();
            }

            public override void GenerateKey()
            {
                throw new NotImplementedException();
            }
        }

        private static readonly Encoding DefaultEncoding = Encoding.UTF8;
        private const CipherMode DefaultCipherMode = CipherMode.CBC;
        private const PaddingMode DefaultPaddingMode = PaddingMode.ISO10126;
        private const string DefaultRecipient = "";
        private static readonly XmlResolver DefaultXmlResolver = null;
        private const int DefaultXmlDSigSearchDepth = 20;

        [Fact]
        public void Constructor_Default()
        {
            EncryptedXml encryptedXml = new EncryptedXml();
            Assert.Equal(DefaultEncoding, encryptedXml.Encoding);
            Assert.Equal(DefaultCipherMode, encryptedXml.Mode);
            Assert.Equal(DefaultPaddingMode, encryptedXml.Padding);
            Assert.Equal(DefaultRecipient, encryptedXml.Recipient);
            Assert.Equal(DefaultXmlResolver, encryptedXml.Resolver);
            Assert.Equal(DefaultXmlDSigSearchDepth, encryptedXml.XmlDSigSearchDepth);
        }

        [Fact]
        public void Constructor_XmlDocument()
        {
            EncryptedXml encryptedXml = new EncryptedXml(null);
            Assert.Equal(DefaultEncoding, encryptedXml.Encoding);
            Assert.Equal(DefaultCipherMode, encryptedXml.Mode);
            Assert.Equal(DefaultPaddingMode, encryptedXml.Padding);
            Assert.Equal(DefaultRecipient, encryptedXml.Recipient);
            Assert.Equal(DefaultXmlResolver, encryptedXml.Resolver);
            Assert.Equal(DefaultXmlDSigSearchDepth, encryptedXml.XmlDSigSearchDepth);
        }

        [Fact]
        public void Constructor_XmlDocumentAndEvidence()
        {
            EncryptedXml encryptedXml = new EncryptedXml(null, null);
            Assert.Equal(DefaultEncoding, encryptedXml.Encoding);
            Assert.Equal(DefaultCipherMode, encryptedXml.Mode);
            Assert.Equal(DefaultPaddingMode, encryptedXml.Padding);
            Assert.Equal(DefaultRecipient, encryptedXml.Recipient);
            Assert.Equal(DefaultXmlResolver, encryptedXml.Resolver);
            Assert.Equal(DefaultXmlDSigSearchDepth, encryptedXml.XmlDSigSearchDepth);
        }

        [Theory]
        [InlineData("System.Security.Cryptography.Xml.Tests.EncryptedXmlSample1.xml")]
        [InlineData("System.Security.Cryptography.Xml.Tests.EncryptedXmlSample3.xml")]
        public void RsaDecryption(string resourceName)
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            string originalXml;
            using (Stream stream = TestHelpers.LoadResourceStream(resourceName))
            using (StreamReader streamReader = new StreamReader(stream))
            {
                originalXml = streamReader.ReadToEnd();
                doc.LoadXml(originalXml);
            }

            EncryptedXml encxml = new EncryptedXml(doc);
            using (X509Certificate2 certificate = TestHelpers.GetSampleX509Certificate())
            using (RSA rsa = certificate.GetRSAPrivateKey())
            {
                Assert.NotNull(rsa);

                XmlNamespaceManager nm = new XmlNamespaceManager(doc.NameTable);
                nm.AddNamespace("s", "http://www.w3.org/2003/05/soap-envelope");
                nm.AddNamespace("o", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
                nm.AddNamespace("e", EncryptedXml.XmlEncNamespaceUrl);
                XmlElement el = doc.SelectSingleNode("/s:Envelope/s:Header/o:Security/e:EncryptedKey", nm) as XmlElement;
                EncryptedKey ekey = new EncryptedKey();
                ekey.LoadXml(el);
                byte[] key = rsa.Decrypt(ekey.CipherData.CipherValue, RSAEncryptionPadding.OaepSHA1);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.Mode = CipherMode.CBC;
                    List<XmlElement> elements = new List<XmlElement>();
                    foreach (XmlElement encryptedDataElement in doc.SelectNodes("//e:EncryptedData", nm))
                    {
                        elements.Add(encryptedDataElement);
                    }
                    foreach (XmlElement encryptedDataElement in elements)
                    {
                        EncryptedData edata = new EncryptedData();
                        edata.LoadXml(encryptedDataElement);
                        encxml.ReplaceData(encryptedDataElement, encxml.DecryptData(edata, aes));
                    }
                }
            }
        }

        [Fact]
        public void Sample2()
        {
            using (Aes aes = Aes.Create())
            {
                byte[] keydata = Convert.FromBase64String("o/ilseZu+keLBBWGGPlUHweqxIPc4gzZEFWr2nBt640=");
                aes.Mode = CipherMode.CBC;
                aes.KeySize = 256;
                aes.Key = keydata;
                aes.Padding = PaddingMode.Zeros;

                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                doc.Load(TestHelpers.LoadResourceStream("System.Security.Cryptography.Xml.Tests.EncryptedXmlSample2.xml"));
                EncryptedXml encxml = new EncryptedXml(doc);
                EncryptedData edata = new EncryptedData();
                edata.LoadXml(doc.DocumentElement);
                encxml.ReplaceData(doc.DocumentElement, encxml.DecryptData(edata, aes));
            }
        }

        [Fact]
        public void RoundtripSample1()
        {
            using (StringWriter sw = new StringWriter())
            {

                // Encryption
                {
                    XmlDocument doc = new XmlDocument();
                    doc.PreserveWhitespace = true;
                    doc.LoadXml("<root>  <child>sample</child>   </root>");

                    XmlElement body = doc.DocumentElement;

                    using (Aes aes = Aes.Create())
                    {
                        byte[] keydata = Convert.FromBase64String("o/ilseZu+keLBBWGGPlUHweqxIPc4gzZEFWr2nBt640=");
                        aes.Mode = CipherMode.CBC;
                        aes.KeySize = 256;
                        aes.IV = Convert.FromBase64String("pBUM5P03rZ6AE4ZK5EyBrw==");
                        aes.Key = keydata;
                        aes.Padding = PaddingMode.Zeros;

                        EncryptedXml exml = new EncryptedXml();
                        byte[] encrypted = exml.EncryptData(body, aes, false);
                        EncryptedData edata = new EncryptedData();
                        edata.Type = EncryptedXml.XmlEncElementUrl;
                        edata.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
                        EncryptedKey ekey = new EncryptedKey();
                        // omit key encryption, here for testing
                        byte[] encKeyBytes = aes.Key;
                        ekey.CipherData = new CipherData(encKeyBytes);
                        ekey.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncRSA15Url);
                        DataReference dr = new DataReference();
                        dr.Uri = "_0";
                        ekey.AddReference(dr);
                        edata.KeyInfo.AddClause(new KeyInfoEncryptedKey(ekey));
                        ekey.KeyInfo.AddClause(new RSAKeyValue(RSA.Create()));
                        edata.CipherData.CipherValue = encrypted;
                        EncryptedXml.ReplaceElement(doc.DocumentElement, edata, false);
                        doc.Save(new XmlTextWriter(sw));
                    }
                }

                // Decryption
                {
                    using (Aes aes = Aes.Create())
                    {
                        byte[] keydata = Convert.FromBase64String("o/ilseZu+keLBBWGGPlUHweqxIPc4gzZEFWr2nBt640=");
                        aes.Mode = CipherMode.CBC;
                        aes.KeySize = 256;
                        aes.Key = keydata;
                        aes.Padding = PaddingMode.Zeros;

                        XmlDocument doc = new XmlDocument();
                        doc.PreserveWhitespace = true;
                        doc.LoadXml(sw.ToString());
                        EncryptedXml encxml = new EncryptedXml(doc);
                        EncryptedData edata = new EncryptedData();
                        edata.LoadXml(doc.DocumentElement);
                        encxml.ReplaceData(doc.DocumentElement, encxml.DecryptData(edata, aes));
                    }
                }
            }
        }

        [Fact]
        public void Encrypt_DecryptDocument_AES()
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            string xml = "<root>  <child>sample</child>   </root>";
            doc.LoadXml(xml);

            using (Aes aes = Aes.Create())
            {
                EncryptedXml exml = new EncryptedXml();
                exml.AddKeyNameMapping("aes", aes);
                EncryptedData ed = exml.Encrypt(doc.DocumentElement, "aes");

                doc.LoadXml(ed.GetXml().OuterXml);
                EncryptedXml exmlDecryptor = new EncryptedXml(doc);
                exmlDecryptor.AddKeyNameMapping("aes", aes);
                exmlDecryptor.DecryptDocument();

                Assert.Equal(xml, doc.OuterXml);
            }
        }

        [Fact]
        public void Encrypt_X509()
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            string xml = "<root>  <child>sample</child>   </root>";
            doc.LoadXml(xml);

            using (X509Certificate2 certificate = TestHelpers.GetSampleX509Certificate())
            {
                EncryptedXml exml = new EncryptedXml();
                EncryptedData ed = exml.Encrypt(doc.DocumentElement, certificate);

                Assert.NotNull(ed);

                doc.LoadXml(ed.GetXml().OuterXml);
                XmlNamespaceManager nm = new XmlNamespaceManager(doc.NameTable);
                nm.AddNamespace("enc", EncryptedXml.XmlEncNamespaceUrl);

                Assert.NotNull(doc.SelectSingleNode("//enc:EncryptedKey", nm));
                Assert.DoesNotContain("sample", doc.OuterXml);
            }
        }

        [Fact]
        public void Encrypt_X509_XmlNull()
        {
            using (X509Certificate2 certificate = TestHelpers.GetSampleX509Certificate())
            {
                EncryptedXml exml = new EncryptedXml();
                Assert.Throws<ArgumentNullException>(() => exml.Encrypt(null, certificate));
            }
        }

        [Fact]
        public void Encrypt_X509_CertificateNull()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            EncryptedXml exml = new EncryptedXml();
            X509Certificate2 certificate = null;
            Assert.Throws<ArgumentNullException>(() => exml.Encrypt(doc.DocumentElement, certificate));
        }

        [Fact]
        public void Encrypt_XmlNull()
        {
            EncryptedXml exml = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => exml.Encrypt(null, "aes"));
        }

        [Fact]
        public void Encrypt_KeyNameNull()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            EncryptedXml exml = new EncryptedXml();
            string keyName = null;
            Assert.Throws<ArgumentNullException>(() => exml.Encrypt(doc.DocumentElement, keyName));
        }

        [Fact]
        public void Encrypt_MissingKey()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            EncryptedXml exml = new EncryptedXml();
            Assert.Throws<CryptographicException>(() => exml.Encrypt(doc.DocumentElement, "aes"));
        }

        [Fact]
        public void Encrypt_RSA()
        {
            using (RSA rsa = RSA.Create())
            {
                CheckEncryptionMethod(rsa, EncryptedXml.XmlEncRSA15Url);
            }
        }

        [Fact]
        public void Encrypt_TripleDES()
        {
            using (TripleDES tripleDes = TripleDES.Create())
            {
                CheckEncryptionMethod(tripleDes, EncryptedXml.XmlEncTripleDESKeyWrapUrl);
            }
        }

        [Fact]
        public void Encrypt_AES128()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 128;
                CheckEncryptionMethod(aes, EncryptedXml.XmlEncAES128KeyWrapUrl);
            }
        }

        [Fact]
        public void Encrypt_AES192()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 192;
                CheckEncryptionMethod(aes, EncryptedXml.XmlEncAES192KeyWrapUrl);
            }
        }

        [Fact]
        public void Encrypt_NotSupportedAlgorithm()
        {
            Assert.Throws<CryptographicException>(() => CheckEncryptionMethod(new NotSupportedSymmetricAlgorithm(), EncryptedXml.XmlEncAES192KeyWrapUrl));
        }

        [Fact]
        public void AddKeyNameMapping_KeyNameNull()
        {
            EncryptedXml exml = new EncryptedXml();
            using (Aes aes = Aes.Create())
            {
                Assert.Throws<ArgumentNullException>(() => exml.AddKeyNameMapping(null, aes));
            }
        }

        [Fact]
        public void AddKeyNameMapping_KeyObjectNull()
        {
            EncryptedXml exml = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => exml.AddKeyNameMapping("no_object", null));
        }

        [Fact]
        public void AddKeyNameMapping_KeyObjectWrongType()
        {
            EncryptedXml exml = new EncryptedXml();
            Assert.Throws<CryptographicException>(() => exml.AddKeyNameMapping("string", ""));
        }

        [Fact]
        public void ReplaceData_XmlElementNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.ReplaceData(null, new byte[0]));
        }

        [Fact]
        public void ReplaceData_EncryptedDataNull()
        {
            EncryptedXml ex = new EncryptedXml();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            Assert.Throws<ArgumentNullException>(() => ex.ReplaceData(doc.DocumentElement, null));
        }

        [Fact]
        public void ReplaceElement_XmlElementNull()
        {
            Assert.Throws<ArgumentNullException>(() => EncryptedXml.ReplaceElement(null, new EncryptedData(), true));
        }

        [Fact]
        public void ReplaceElement_EncryptedDataNull()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            Assert.Throws<ArgumentNullException>(() => EncryptedXml.ReplaceElement(doc.DocumentElement, null, false));
        }

        [Fact]
        public void ReplaceElement_ContentTrue()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            EncryptedData edata = new EncryptedData();
            edata.CipherData.CipherValue = new byte[16];
            EncryptedXml.ReplaceElement(doc.DocumentElement, edata, true);
            Assert.Equal("root", doc.DocumentElement.Name);
            Assert.Equal("EncryptedData", doc.DocumentElement.FirstChild.Name);
        }

        [Fact]
        public void GetIdElement_XmlDocumentNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Null(ex.GetIdElement(null, "value"));
        }

        [Fact]
        public void GetIdElement_StringNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.GetIdElement(new XmlDocument(), null));
        }

        [Fact]
        public void GetDecryptionKey_EncryptedDataNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.GetDecryptionKey(null, EncryptedXml.XmlEncAES128Url));
        }

        [Fact]
        public void GetDecryptionKey_NoEncryptionMethod()
        {
            EncryptedData edata = new EncryptedData();
            edata.KeyInfo = new KeyInfo();
            edata.KeyInfo.AddClause(new KeyInfoEncryptedKey(new EncryptedKey()));
            EncryptedXml exml = new EncryptedXml();
            Assert.Throws<CryptographicException>(() => exml.GetDecryptionKey(edata, null));
        }

        [Fact]
        public void GetDecryptionKey_StringNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Null(ex.GetDecryptionKey(new EncryptedData(), null));
        }

        [Fact]
        public void GetDecryptionKey_KeyInfoName()
        {
            using (Aes aes = Aes.Create())
            {
                EncryptedData edata = new EncryptedData();
                edata.KeyInfo = new KeyInfo();
                edata.KeyInfo.AddClause(new KeyInfoName("aes"));

                EncryptedXml exml = new EncryptedXml();
                exml.AddKeyNameMapping("aes", aes);
                SymmetricAlgorithm decryptedAlg = exml.GetDecryptionKey(edata, null);

                Assert.Equal(aes.Key, decryptedAlg.Key);
            }
        }

        [Fact]
        public void GetDecryptionKey_CarriedKeyName()
        {
            using (Aes aes = Aes.Create())
            using (Aes innerAes = Aes.Create())
            {
                innerAes.KeySize = 128;

                EncryptedData edata = new EncryptedData();
                edata.KeyInfo = new KeyInfo();
                edata.KeyInfo.AddClause(new KeyInfoName("aes"));

                EncryptedKey ekey = new EncryptedKey();
                byte[] encKeyBytes = EncryptedXml.EncryptKey(innerAes.Key, aes);
                ekey.CipherData = new CipherData(encKeyBytes);
                ekey.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
                ekey.CarriedKeyName = "aes";
                ekey.KeyInfo = new KeyInfo();
                ekey.KeyInfo.AddClause(new KeyInfoName("another_aes"));

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(ekey.GetXml().OuterXml);

                EncryptedXml exml = new EncryptedXml(doc);
                exml.AddKeyNameMapping("another_aes", aes);
                SymmetricAlgorithm decryptedAlg = exml.GetDecryptionKey(edata, EncryptedXml.XmlEncAES256Url);

                Assert.Equal(innerAes.Key, decryptedAlg.Key);
            }
        }

        [Fact]
        public void GetDecryptionIV_EncryptedDataNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.GetDecryptionIV(null, EncryptedXml.XmlEncAES128Url));
        }

        [Fact]
        public void GetDecryptionIV_StringNull()
        {
            EncryptedXml ex = new EncryptedXml();
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
            encryptedData.CipherData = new CipherData(new byte[16]);
            Assert.Equal(new byte[16], ex.GetDecryptionIV(encryptedData, null));
        }

        [Fact]
        public void GetDecryptionIV_StringNullWithoutEncryptionMethod()
        {
            EncryptedXml ex = new EncryptedXml();
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.CipherData = new CipherData(new byte[16]);
            Assert.Throws<CryptographicException>(() => ex.GetDecryptionIV(encryptedData, null));
        }

        [Fact]
        public void GetDecryptionIV_InvalidAlgorithmUri()
        {
            EncryptedXml ex = new EncryptedXml();
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.CipherData = new CipherData(new byte[16]);
            Assert.Throws<CryptographicException>(() => ex.GetDecryptionIV(encryptedData, "invalid"));
        }

        [Fact]
        public void GetDecryptionIV_TripleDesUri()
        {
            EncryptedXml ex = new EncryptedXml();
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.CipherData = new CipherData(new byte[16]);
            Assert.Equal(8, ex.GetDecryptionIV(encryptedData, EncryptedXml.XmlEncTripleDESUrl).Length);
        }

        [Fact]
        public void DecryptKey_KeyNull()
        {
            using (Aes aes = Aes.Create())
            {
                Assert.Throws<ArgumentNullException>(() => EncryptedXml.DecryptKey(null, aes));
            }
        }

        [Fact]
        public void DecryptKey_SymmetricAlgorithmNull()
        {
            Assert.Throws<ArgumentNullException>(() => EncryptedXml.DecryptKey(new byte[16], null));
        }

        [Fact]
        public void EncryptKey_KeyNull()
        {
            using (Aes aes = Aes.Create())
            {
                Assert.Throws<ArgumentNullException>(() => EncryptedXml.EncryptKey(null, aes));
            }
        }

        [Fact]
        public void EncryptKey_SymmetricAlgorithmNull()
        {
            Assert.Throws<ArgumentNullException>(() => EncryptedXml.EncryptKey(new byte[16], null));
        }

        [Fact]
        public void EncryptKey_WrongSymmetricAlgorithm()
        {
            Assert.Throws<CryptographicException>(() => EncryptedXml.EncryptKey(new byte[16], new NotSupportedSymmetricAlgorithm()));
        }

        [Fact]
        public void EncryptKey_RSA_KeyDataNull()
        {
            using (RSA rsa = RSA.Create())
            {
                Assert.Throws<ArgumentNullException>(() => EncryptedXml.EncryptKey(null, rsa, false));
            }
        }

        [Fact]
        public void EncryptKey_RSA_RSANull()
        {
            Assert.Throws<ArgumentNullException>(() => EncryptedXml.EncryptKey(new byte[16], null, false));
        }

        [Fact]
        public void EncryptKey_RSA_UseOAEP()
        {
            byte[] data = Encoding.ASCII.GetBytes("12345678");
            using (RSA rsa = RSA.Create())
            {
                byte[] encryptedData = EncryptedXml.EncryptKey(data, rsa, true);
                byte[] decryptedData = EncryptedXml.DecryptKey(encryptedData, rsa, true);
                Assert.Equal(data, decryptedData);
            }
        }

        [Fact]
        public void DecryptData_EncryptedDataNull()
        {
            EncryptedXml ex = new EncryptedXml();
            using (Aes aes = Aes.Create())
            {
                Assert.Throws<ArgumentNullException>(() => ex.DecryptData(null, aes));
            }
        }

        [Fact]
        public void DecryptData_SymmetricAlgorithmNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.DecryptData(new EncryptedData(), null));
        }

        [Fact]
        public void DecryptData_CipherReference_InvalidUri()
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            string xml = "<root>  <child>sample</child>   </root>";
            doc.LoadXml(xml);

            using (Aes aes = Aes.Create())
            {
                EncryptedXml exml = new EncryptedXml();
                exml.AddKeyNameMapping("aes", aes);
                EncryptedData ed = exml.Encrypt(doc.DocumentElement, "aes");
                ed.CipherData = new CipherData();
                ed.CipherData.CipherReference = new CipherReference("invaliduri");

                // https://github.com/dotnet/runtime/issues/21484
                Action decrypt = () => exml.DecryptData(ed, aes);
                if (PlatformDetection.IsNetFramework)
                    Assert.Throws<ArgumentNullException>(decrypt);
                else
                    Assert.Throws<CryptographicException>(decrypt);
            }
        }

        [Fact]
        public void DecryptData_CipherReference_IdUri()
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            string xml = "<root>  <child>sample</child>   </root>";
            doc.LoadXml(xml);

            using (Aes aes = Aes.Create())
            {
                EncryptedXml exml = new EncryptedXml(doc);
                string cipherValue = Convert.ToBase64String(exml.EncryptData(Encoding.UTF8.GetBytes(xml), aes));

                EncryptedData ed = new EncryptedData();
                ed.Type = EncryptedXml.XmlEncElementUrl;
                ed.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
                ed.CipherData = new CipherData();
                // Create CipherReference: first extract node value, then convert from base64 using Transforms
                ed.CipherData.CipherReference = new CipherReference("#ID_0");
                string xslt = "<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"><xsl:template match = \"/\"><xsl:value-of select=\".\" /></xsl:template></xsl:stylesheet>";
                XmlDsigXsltTransform xsltTransform = new XmlDsigXsltTransform();
                XmlDocument xsltDoc = new XmlDocument();
                xsltDoc.LoadXml(xslt);
                xsltTransform.LoadInnerXml(xsltDoc.ChildNodes);
                ed.CipherData.CipherReference.AddTransform(xsltTransform);
                ed.CipherData.CipherReference.AddTransform(new XmlDsigBase64Transform());

                // Create a document with EncryptedData and node with the actual cipher data (with the ID)
                doc.LoadXml("<root></root>");
                XmlNode encryptedDataNode = doc.ImportNode(ed.GetXml(), true);
                doc.DocumentElement.AppendChild(encryptedDataNode);
                XmlElement cipherDataByReference = doc.CreateElement("CipherData");
                cipherDataByReference.SetAttribute("ID", "ID_0");
                cipherDataByReference.InnerText = cipherValue;
                doc.DocumentElement.AppendChild(cipherDataByReference);

                if (PlatformDetection.IsXmlDsigXsltTransformSupported)
                {
                    string decryptedXmlString = Encoding.UTF8.GetString(exml.DecryptData(ed, aes));
                    Assert.Equal(xml, decryptedXmlString);
                }
            }
        }

        [Fact]
        public void EncryptData_DataNull()
        {
            EncryptedXml ex = new EncryptedXml();
            using (Aes aes = Aes.Create())
            {
                Assert.Throws<ArgumentNullException>(() => ex.EncryptData(null, aes));
            }
        }

        [Fact]
        public void EncryptData_SymmetricAlgorithmNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.EncryptData(new byte[16], null));
        }

        [Fact]
        public void EncryptData_Xml_SymmetricAlgorithmNull()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.EncryptData(doc.DocumentElement, null, true));
        }

        [Fact]
        public void EncryptData_Xml_XmlElementNull()
        {
            EncryptedXml ex = new EncryptedXml();
            using (Aes aes = Aes.Create())
            {
                Assert.Throws<ArgumentNullException>(() => ex.EncryptData(null, aes, true));
            }
        }

        [Fact]
        public void DecryptEncryptedKey_Null()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.DecryptEncryptedKey(null));
        }

        [Fact]
        public void DecryptEncryptedKey_Empty()
        {
            EncryptedXml ex = new EncryptedXml();
            EncryptedKey ek = new EncryptedKey();
            Assert.Null(ex.DecryptEncryptedKey(ek));
        }

        [Fact]
        public void DecryptEncryptedKey_KeyInfoRetrievalMethod()
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            string xml = "<root>  <child>sample</child>   </root>";
            doc.LoadXml(xml);

            using (Aes aes = Aes.Create())
            using (Aes innerAes = Aes.Create())
            {
                innerAes.KeySize = 128;

                EncryptedXml exml = new EncryptedXml(doc);
                exml.AddKeyNameMapping("aes", aes);

                EncryptedKey ekey = new EncryptedKey();
                byte[] encKeyBytes = EncryptedXml.EncryptKey(innerAes.Key, aes);
                ekey.CipherData = new CipherData(encKeyBytes);
                ekey.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
                ekey.Id = "Key_ID";
                ekey.KeyInfo = new KeyInfo();
                ekey.KeyInfo.AddClause(new KeyInfoName("aes"));

                doc.LoadXml(ekey.GetXml().OuterXml);

                EncryptedKey ekeyRetrieval = new EncryptedKey();
                KeyInfo keyInfoRetrieval = new KeyInfo();
                keyInfoRetrieval.AddClause(new KeyInfoRetrievalMethod("#Key_ID"));
                ekeyRetrieval.KeyInfo = keyInfoRetrieval;

                byte[] decryptedKey = exml.DecryptEncryptedKey(ekeyRetrieval);
                Assert.Equal(innerAes.Key, decryptedKey);

                EncryptedData eData = new EncryptedData();
                eData.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
                eData.KeyInfo = keyInfoRetrieval;
                SymmetricAlgorithm decryptedAlg = exml.GetDecryptionKey(eData, null);
                Assert.Equal(innerAes.Key, decryptedAlg.Key);
            }
        }

        [Fact]
        public void DecryptEncryptedKey_KeyInfoEncryptedKey()
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            string xml = "<root>  <child>sample</child>   </root>";
            doc.LoadXml(xml);

            using (Aes aes = Aes.Create())
            using (Aes outerAes = Aes.Create())
            using (Aes innerAes = Aes.Create())
            {
                outerAes.KeySize = 192;
                innerAes.KeySize = 128;

                EncryptedXml exml = new EncryptedXml(doc);
                exml.AddKeyNameMapping("aes", aes);

                EncryptedKey ekey = new EncryptedKey();
                byte[] encKeyBytes = EncryptedXml.EncryptKey(outerAes.Key, aes);
                ekey.CipherData = new CipherData(encKeyBytes);
                ekey.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
                ekey.Id = "Key_ID";
                ekey.KeyInfo = new KeyInfo();
                ekey.KeyInfo.AddClause(new KeyInfoName("aes"));

                KeyInfo topLevelKeyInfo = new KeyInfo();
                topLevelKeyInfo.AddClause(new KeyInfoEncryptedKey(ekey));

                EncryptedKey ekeyTopLevel = new EncryptedKey();
                byte[] encTopKeyBytes = EncryptedXml.EncryptKey(innerAes.Key, outerAes);
                ekeyTopLevel.CipherData = new CipherData(encTopKeyBytes);
                ekeyTopLevel.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
                ekeyTopLevel.KeyInfo = topLevelKeyInfo;

                doc.LoadXml(ekeyTopLevel.GetXml().OuterXml);

                byte[] decryptedKey = exml.DecryptEncryptedKey(ekeyTopLevel);
                Assert.Equal(innerAes.Key, decryptedKey);

                EncryptedData eData = new EncryptedData();
                eData.EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url);
                eData.KeyInfo = topLevelKeyInfo;
                SymmetricAlgorithm decryptedAlg = exml.GetDecryptionKey(eData, null);
                Assert.Equal(outerAes.Key, decryptedAlg.Key);
            }
        }

        [Fact]
        public void EncryptKey_TripleDES()
        {
            using (TripleDES tripleDES = TripleDES.Create())
            {
                byte[] key = Encoding.ASCII.GetBytes("123456781234567812345678");

                byte[] encryptedKey = EncryptedXml.EncryptKey(key, tripleDES);

                Assert.NotNull(encryptedKey);
                Assert.Equal(key, EncryptedXml.DecryptKey(encryptedKey, tripleDES));
            }
        }

        [Fact]
        public void EncryptKey_AES()
        {
            using (Aes aes = Aes.Create())
            {
                byte[] key = Encoding.ASCII.GetBytes("123456781234567812345678");

                byte[] encryptedKey = EncryptedXml.EncryptKey(key, aes);

                Assert.NotNull(encryptedKey);
                Assert.Equal(key, EncryptedXml.DecryptKey(encryptedKey, aes));
            }
        }

        [Fact]
        public void EncryptKey_AES8Bytes()
        {
            using (Aes aes = Aes.Create())
            {
                byte[] key = Encoding.ASCII.GetBytes("12345678");

                byte[] encryptedKey = EncryptedXml.EncryptKey(key, aes);

                Assert.NotNull(encryptedKey);
                Assert.Equal(key, EncryptedXml.DecryptKey(encryptedKey, aes));
            }
        }

        [Fact]
        public void EncryptKey_AESNotDivisibleBy8()
        {
            using (Aes aes = Aes.Create())
            {
                byte[] key = Encoding.ASCII.GetBytes("1234567");

                Assert.Throws<CryptographicException>(() => EncryptedXml.EncryptKey(key, aes));
            }
        }

        [Fact]
        public void DecryptKey_TripleDESWrongKeySize()
        {
            using (TripleDES tripleDES = TripleDES.Create())
            {
                byte[] key = Encoding.ASCII.GetBytes("123");

                Assert.Throws<CryptographicException>(() => EncryptedXml.DecryptKey(key, tripleDES));
            }
        }

        [Fact]
        public void DecryptKey_TripleDESCorruptedKey()
        {
            using (TripleDES tripleDES = TripleDES.Create())
            {
                byte[] key = Encoding.ASCII.GetBytes("123456781234567812345678");

                byte[] encryptedKey = EncryptedXml.EncryptKey(key, tripleDES);
                encryptedKey[0] ^= 0xFF;

                Assert.Throws<CryptographicException>(() => EncryptedXml.DecryptKey(encryptedKey, tripleDES));
            }
        }

        [Fact]
        public void DecryptKey_AESWrongKeySize()
        {
            using (Aes aes = Aes.Create())
            {
                byte[] key = Encoding.ASCII.GetBytes("123");

                Assert.Throws<CryptographicException>(() => EncryptedXml.DecryptKey(key, aes));
            }
        }

        [Fact]
        public void DecryptKey_AESCorruptedKey()
        {
            using (Aes aes = Aes.Create())
            {
                byte[] key = Encoding.ASCII.GetBytes("123456781234567812345678");

                byte[] encryptedKey = EncryptedXml.EncryptKey(key, aes);
                encryptedKey[0] ^= 0xFF;

                Assert.Throws<CryptographicException>(() => EncryptedXml.DecryptKey(encryptedKey, aes));
            }
        }

        [Fact]
        public void DecryptKey_AESCorruptedKey8Bytes()
        {
            using (Aes aes = Aes.Create())
            {
                byte[] key = Encoding.ASCII.GetBytes("12345678");

                byte[] encryptedKey = EncryptedXml.EncryptKey(key, aes);
                encryptedKey[0] ^= 0xFF;

                Assert.Throws<CryptographicException>(() => EncryptedXml.DecryptKey(encryptedKey, aes));
            }
        }

        [Fact]
        public void DecryptKey_NotSupportedAlgorithm()
        {
            Assert.Throws<CryptographicException>(() => EncryptedXml.DecryptKey(new byte[16], new NotSupportedSymmetricAlgorithm()));
        }

        [Fact]
        public void DecryptKey_RSA_KeyDataNull()
        {
            using (RSA rsa = RSA.Create())
            {
                Assert.Throws<ArgumentNullException>(() => EncryptedXml.DecryptKey(null, rsa, false));
            }
        }

        [Fact]
        public void DecryptKey_RSA_RSANull()
        {
            Assert.Throws<ArgumentNullException>(() => EncryptedXml.DecryptKey(new byte[16], null, false));
        }

        [Fact]
        public void Properties()
        {
            EncryptedXml exml = new EncryptedXml();
            exml.XmlDSigSearchDepth = 10;
            exml.Resolver = null;
            exml.Padding = PaddingMode.None;
            exml.Mode = CipherMode.CBC;
            exml.Encoding = Encoding.ASCII;
            exml.Recipient = "Recipient";

            Assert.Equal(10, exml.XmlDSigSearchDepth);
            Assert.Null(exml.Resolver);
            Assert.Equal(PaddingMode.None, exml.Padding);
            Assert.Equal(CipherMode.CBC, exml.Mode);
            Assert.Equal(Encoding.ASCII, exml.Encoding);
            Assert.Equal("Recipient", exml.Recipient);
        }

        private void CheckEncryptionMethod(object algorithm, string uri)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<root />");
            EncryptedXml exml = new EncryptedXml();
            exml.AddKeyNameMapping("key", algorithm);

            EncryptedData edata = exml.Encrypt(doc.DocumentElement, "key");
            IEnumerator keyInfoEnum = edata.KeyInfo.GetEnumerator();
            keyInfoEnum.MoveNext();
            KeyInfoEncryptedKey kiEncKey = keyInfoEnum.Current as KeyInfoEncryptedKey;

            Assert.NotNull(edata);
            Assert.Equal(uri, kiEncKey.EncryptedKey.EncryptionMethod.KeyAlgorithm);
            Assert.NotNull(edata.CipherData.CipherValue);
        }
    }
}
