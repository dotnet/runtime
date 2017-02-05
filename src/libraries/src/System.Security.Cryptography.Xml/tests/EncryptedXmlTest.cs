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
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class EncryptedXmlTest
    {
        [Fact]
        public void Sample1()
        {
            AssertDecryption1("Test/System.Security.Cryptography.Xml/EncryptedXmlSample1.xml");
        }

        void AssertDecryption1(string filename)
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(filename);
            EncryptedXml encxml = new EncryptedXml(doc);
            RSACryptoServiceProvider rsa = new X509Certificate2("Test/System.Security.Cryptography.Xml/sample.pfx", "mono").PrivateKey as RSACryptoServiceProvider;
            XmlNamespaceManager nm = new XmlNamespaceManager(doc.NameTable);
            nm.AddNamespace("s", "http://www.w3.org/2003/05/soap-envelope");
            nm.AddNamespace("o", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            nm.AddNamespace("e", EncryptedXml.XmlEncNamespaceUrl);
            XmlElement el = doc.SelectSingleNode("/s:Envelope/s:Header/o:Security/e:EncryptedKey", nm) as XmlElement;
            EncryptedKey ekey = new EncryptedKey();
            ekey.LoadXml(el);
            byte[] key = rsa.Decrypt(ekey.CipherData.CipherValue, true);
            Rijndael aes = new RijndaelManaged();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            ArrayList al = new ArrayList();
            foreach (XmlElement ed in doc.SelectNodes("//e:EncryptedData", nm))
                al.Add(ed);
            foreach (XmlElement ed in al)
            {
                EncryptedData edata = new EncryptedData();
                edata.LoadXml(ed);
                encxml.ReplaceData(ed, encxml.DecryptData(edata, aes));
            }
        }

        [Fact]
        public void Sample2()
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.Mode = CipherMode.CBC;
            aes.KeySize = 256;
            aes.Key = Convert.FromBase64String("o/ilseZu+keLBBWGGPlUHweqxIPc4gzZEFWr2nBt640=");
            aes.Padding = PaddingMode.Zeros;

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load("Test/System.Security.Cryptography.Xml/EncryptedXmlSample2.xml");
            EncryptedXml encxml = new EncryptedXml(doc);
            EncryptedData edata = new EncryptedData();
            edata.LoadXml(doc.DocumentElement);
            encxml.ReplaceData(doc.DocumentElement, encxml.DecryptData(edata, aes));
        }

        [Fact]
        public void Sample3()
        {
            AssertDecryption1("Test/System.Security.Cryptography.Xml/EncryptedXmlSample3.xml");
        }

        [Fact]
        public void RoundtripSample1()
        {
            StringWriter sw = new StringWriter();

            // Encryption
            {
                XmlDocument doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                doc.LoadXml("<root>  <child>sample</child>   </root>");

                XmlElement body = doc.DocumentElement;

                RijndaelManaged aes = new RijndaelManaged();
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

            // Decryption
            {
                RijndaelManaged aes = new RijndaelManaged();
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
            Assert.Null(ex.GetIdElement(new XmlDocument(), null));
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
            Assert.Null(ex.GetDecryptionIV(new EncryptedData(), null));
            // Might be a CryptographicException
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
            Assert.Throws<ArgumentNullException>(() => ex.DecryptData(null, Rijndael.Create()));
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
            Assert.Throws<ArgumentNullException>(() => ex.EncryptData(null, Rijndael.Create()));
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
            Assert.Throws<ArgumentNullException>(() => ex.EncryptData(null, Rijndael.Create(), true));
        }

        [Fact]
        public void DecryptEncryptedKey_Null()
        {
            EncryptedXml ex = new EncryptedXml();
            Assert.Throws<ArgumentNullException>(() => ex.DecryptEncryptedKey(null));
        }
    }
}
