//
// EncryptedXmlTest.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class EncryptedXmlTest
    {
        [Fact]
        public void Constructor_Default()
        {
            EncryptedXml encryptedXml = new EncryptedXml();
            Assert.Equal(Encoding.UTF8, encryptedXml.Encoding);
            Assert.Equal(CipherMode.CBC, encryptedXml.Mode);
            Assert.Equal(PaddingMode.ISO10126, encryptedXml.Padding);
            Assert.Equal(string.Empty, encryptedXml.Recipient);
            Assert.Equal(null, encryptedXml.Resolver);
            Assert.Equal(20, encryptedXml.XmlDSigSearchDepth);
        }

        [Fact]
        public void Constructor_XmlDocument()
        {
            EncryptedXml encryptedXml = new EncryptedXml(null);
            Assert.Equal(Encoding.UTF8, encryptedXml.Encoding);
            Assert.Equal(CipherMode.CBC, encryptedXml.Mode);
            Assert.Equal(PaddingMode.ISO10126, encryptedXml.Padding);
            Assert.Equal(string.Empty, encryptedXml.Recipient);
            Assert.Equal(null, encryptedXml.Resolver);
            Assert.Equal(20, encryptedXml.XmlDSigSearchDepth);
        }

        [Theory]
        [InlineData("System.Security.Cryptography.Xml.Tests.EncryptedXmlSample1.xml")]
        [InlineData("System.Security.Cryptography.Xml.Tests.EncryptedXmlSample3.xml")]
        public void RsaDecryption(string resourceName)
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            string originalXml;
            using (Stream stream = LoadResourceStream(resourceName))
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
                aes.Mode = CipherMode.CBC;
                aes.KeySize = 256;
                aes.Key = Convert.FromBase64String("o/ilseZu+keLBBWGGPlUHweqxIPc4gzZEFWr2nBt640=");
                aes.Padding = PaddingMode.Zeros;

                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                doc.Load(LoadResourceStream("System.Security.Cryptography.Xml.Tests.EncryptedXmlSample2.xml"));
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
                        aes.Mode = CipherMode.CBC;
                        aes.KeySize = 256;
                        aes.IV = Convert.FromBase64String("pBUM5P03rZ6AE4ZK5EyBrw==");
                        aes.Key = Convert.FromBase64String("o/ilseZu+keLBBWGGPlUHweqxIPc4gzZEFWr2nBt640=");
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
                        edata.KeyInfo = new KeyInfo();
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
                        aes.Mode = CipherMode.CBC;
                        aes.KeySize = 256;
                        aes.Key = Convert.FromBase64String(
                            "o/ilseZu+keLBBWGGPlUHweqxIPc4gzZEFWr2nBt640=");
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
            Assert.Throws<ArgumentNullException>(() => EncryptedXml.ReplaceElement(doc.DocumentElement, null, false));
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
        public void GetDecryptionKey_StringNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Null(ex.GetDecryptionKey(new EncryptedData(), null));
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
        public void DecryptKey_KeyNull()
        {
            Assert.Throws<ArgumentNullException>(() => EncryptedXml.DecryptKey(null, Rijndael.Create()));
        }

        [Fact]
        public void DecryptKey_SymmetricAlgorithmNull()
        {
            Assert.Throws<ArgumentNullException>(() => EncryptedXml.DecryptKey(new byte[16], null));
        }

        [Fact]
        public void EncryptKey_KeyNull()
        {
            Assert.Throws<ArgumentNullException>(() => EncryptedXml.EncryptKey(null, Rijndael.Create()));
        }

        [Fact]
        public void EncryptKey_SymmetricAlgorithmNull()
        {
            Assert.Throws<ArgumentNullException>(() => EncryptedXml.EncryptKey(new byte[16], null));
        }

        [Fact]
        public void DecryptData_EncryptedDataNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.DecryptData(null, Aes.Create()));
        }

        [Fact]
        public void DecryptData_SymmetricAlgorithmNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.DecryptData(new EncryptedData(), null));
        }

        [Fact]
        public void EncryptData_DataNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.EncryptData(null, Aes.Create()));
        }

        [Fact]
        public void EncryptData_SymmetricAlgorithmNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.EncryptData(new byte[16], null));
        }

        [Fact]
        public void EncryptData_XmlElementNull()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.EncryptData(null, Aes.Create(), true));
        }

        [Fact]
        public void DecryptEncryptedKey_Null()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.DecryptEncryptedKey(null));
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

        private Stream LoadResourceStream(string resourceName)
        {
            return Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName);
        }

        private byte[] LoadResource(string resourceName)
        {
            using (Stream stream = Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName))
            {
                long length = stream.Length;
                byte[] buffer = new byte[length];
                stream.Read(buffer, 0, (int)length);
                return buffer;
            }
        }
    }
}
