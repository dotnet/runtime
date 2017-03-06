// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    // Simplified implementation of MSDN sample:
    // https://msdn.microsoft.com/en-us/library/sb7w85t6(v=vs.110).aspx
    public class EncryptingAndDecryptingSymmetric
    {
        private static XmlDocument LoadXmlFromString(string xml)
        {
            var doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);
            return doc;
        }

        private static void EncryptElement(XmlDocument doc, string elementName, SymmetricAlgorithm key)
        {
            var elementToEncrypt = (XmlElement)doc.GetElementsByTagName(elementName)[0];

            var encryptedXml = new EncryptedXml();
            var encryptedData = new EncryptedData()
            {
                Type = EncryptedXml.XmlEncElementUrl,
                EncryptionMethod = new EncryptionMethod(GetEncryptionMethodName(key))
            };

            encryptedData.CipherData.CipherValue = encryptedXml.EncryptData(elementToEncrypt, key, false);

            EncryptedXml.ReplaceElement(elementToEncrypt, encryptedData, false);
        }

        private static string GetEncryptionMethodName(SymmetricAlgorithm key)
        {
            if (key is TripleDES)
            {
                return EncryptedXml.XmlEncTripleDESUrl;
            }
            else if (key is DES)
            {
                return EncryptedXml.XmlEncDESUrl;
            }
            else if (key is Rijndael || key is Aes)
            {
                switch (key.KeySize)
                {
                    case 128:
                        return EncryptedXml.XmlEncAES128Url;
                    case 192:
                        return EncryptedXml.XmlEncAES192Url;
                    case 256:
                        return EncryptedXml.XmlEncAES256Url;
                }
            }

            throw new CryptographicException("The specified algorithm is not supported for XML Encryption.");
        }

        private static void Decrypt(XmlDocument doc, SymmetricAlgorithm key)
        {
            var encryptedElement = (XmlElement)doc.GetElementsByTagName("EncryptedData")[0];

            var encryptedData = new EncryptedData();
            encryptedData.LoadXml(encryptedElement);

            var encryptedXml = new EncryptedXml();

            byte[] rgbOutput = encryptedXml.DecryptData(encryptedData, key);

            encryptedXml.ReplaceData(encryptedElement, rgbOutput);
        }

        [Fact]
        public void SymmetricEncryptionRoundtrip()
        {
            const string testString = "some text node";
            const string ExampleXmlRootElement = "example";
            const string ExampleXml = @"<?xml version=""1.0""?>
<example>
<test>some text node</test>
</example>";

            using (var key = Aes.Create())
            {
                XmlDocument xmlDocToEncrypt = LoadXmlFromString(ExampleXml);
                Assert.Contains(testString, xmlDocToEncrypt.OuterXml);
                EncryptElement(xmlDocToEncrypt, ExampleXmlRootElement, key);

                Assert.DoesNotContain(testString, xmlDocToEncrypt.OuterXml);
                XmlDocument xmlDocToDecrypt = LoadXmlFromString(xmlDocToEncrypt.OuterXml);
                Decrypt(xmlDocToDecrypt, key);

                Assert.Equal(ExampleXml.Replace("\r\n", "\n"), xmlDocToDecrypt.OuterXml.Replace("\r\n", "\n"));
            }
        }
    }
}
