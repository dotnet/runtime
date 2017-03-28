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
    // https://msdn.microsoft.com/en-us/library/ms229746(v=vs.110).aspx
    public class EncryptingAndDecryptingAsymmetric
    {
        private static XmlDocument LoadXmlFromString(string xml)
        {
            var doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);
            return doc;
        }

        private static void Encrypt(XmlDocument doc, string elementName, string encryptionElementID, RSA rsaKey, string keyName, bool useOAEP)
        {
            var elementToEncrypt = (XmlElement)doc.GetElementsByTagName(elementName)[0];

            using (var sessionKey = Aes.Create())
            {
                sessionKey.KeySize = 256;

                // Encrypt the session key and add it to an EncryptedKey element.
                var encryptedKey = new EncryptedKey()
                {
                    CipherData = new CipherData(EncryptedXml.EncryptKey(sessionKey.Key, rsaKey, useOAEP)),
                    EncryptionMethod = new EncryptionMethod(useOAEP ? EncryptedXml.XmlEncRSAOAEPUrl : EncryptedXml.XmlEncRSA15Url)
                };

                // Specify which EncryptedData
                // uses this key. An XML document can have
                // multiple EncryptedData elements that use
                // different keys.
                encryptedKey.AddReference(new DataReference()
                {
                    Uri = "#" + encryptionElementID
                });

                var encryptedData = new EncryptedData()
                {
                    Type = EncryptedXml.XmlEncElementUrl,
                    Id = encryptionElementID,

                    // Create an EncryptionMethod element so that the
                    // receiver knows which algorithm to use for decryption.
                    EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES256Url)
                };

                encryptedData.KeyInfo.AddClause(new KeyInfoEncryptedKey(encryptedKey));
                encryptedKey.KeyInfo.AddClause(new KeyInfoName()
                {
                    Value = keyName
                });

                var encryptedXml = new EncryptedXml();
                encryptedData.CipherData.CipherValue = encryptedXml.EncryptData(elementToEncrypt, sessionKey, false);

                EncryptedXml.ReplaceElement(elementToEncrypt, encryptedData, false);
            }
        }

        public static void Decrypt(XmlDocument doc, RSA rsaKey, string keyName)
        {
            var encrypted = new EncryptedXml(doc);
            encrypted.AddKeyNameMapping(keyName, rsaKey);
            encrypted.DecryptDocument();
        }

        [Theory]
        [InlineData(true)] // OAEP is recommended
        [InlineData(false)]
        public void AsymmetricEncryptionRoundtrip(bool useOAEP)
        {
            const string testString = "some text node";
            const string exampleXmlRootElement = "example";
            const string exampleXml = @"<?xml version=""1.0""?>
<example>
<test>some text node</test>
</example>";

            using (RSA key = RSA.Create())
            {
                XmlDocument xmlDocToEncrypt = LoadXmlFromString(exampleXml);
                Assert.Contains(testString, xmlDocToEncrypt.OuterXml);
                Encrypt(xmlDocToEncrypt, exampleXmlRootElement, "EncryptedElement1", key, "rsaKey", useOAEP);

                Assert.DoesNotContain(testString, xmlDocToEncrypt.OuterXml);
                XmlDocument xmlDocToDecrypt = LoadXmlFromString(xmlDocToEncrypt.OuterXml);
                Decrypt(xmlDocToDecrypt, key, "rsaKey");

                Assert.Equal(exampleXml.Replace("\r\n", "\n"), xmlDocToDecrypt.OuterXml.Replace("\r\n", "\n"));
            }
        }
    }
}
