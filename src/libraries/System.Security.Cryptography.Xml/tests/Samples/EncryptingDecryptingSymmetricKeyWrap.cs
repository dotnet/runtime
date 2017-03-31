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
    public class EncryptingDecryptingSymmetricKeyWrap
    {
        private static XmlDocument LoadXmlFromString(string xml)
        {
            var doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);
            return doc;
        }

        private static void Encrypt(XmlDocument doc, string elementName, string encryptionElementID, SymmetricAlgorithm key, string keyName, SymmetricAlgorithmFactory innerKeyFactory)
        {
            var elementToEncrypt = (XmlElement)doc.GetElementsByTagName(elementName)[0];

            using (SymmetricAlgorithm innerKey = innerKeyFactory.Create())
            {
                // Encrypt the key with another key
                var encryptedKey = new EncryptedKey()
                {
                    CipherData = new CipherData(EncryptedXml.EncryptKey(innerKey.Key, key)),
                    EncryptionMethod = new EncryptionMethod(TestHelpers.GetEncryptionMethodName(key, keyWrap: true))
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
                    EncryptionMethod = new EncryptionMethod(TestHelpers.GetEncryptionMethodName(innerKey))
                };

                encryptedData.KeyInfo.AddClause(new KeyInfoEncryptedKey(encryptedKey));
                encryptedKey.KeyInfo.AddClause(new KeyInfoName()
                {
                    Value = keyName
                });

                var encryptedXml = new EncryptedXml();
                encryptedData.CipherData.CipherValue = encryptedXml.EncryptData(elementToEncrypt, innerKey, false);

                EncryptedXml.ReplaceElement(elementToEncrypt, encryptedData, false);
            }
        }

        public static void Decrypt(XmlDocument doc, SymmetricAlgorithm key, string keyName)
        {
            var encrypted = new EncryptedXml(doc);
            encrypted.AddKeyNameMapping(keyName, key);
            encrypted.DecryptDocument();
        }

        public static IEnumerable<object[]> GetSymmetricAlgorithmsPairs()
        {
            // DES is not supported in keywrap scenario, there is no specification string for it either
            foreach (var first in TestHelpers.GetSymmetricAlgorithms(skipDes: true))
            {
                foreach (var second in TestHelpers.GetSymmetricAlgorithms(skipDes: true))
                {
                    yield return new object[] { first, second };
                }
            }
        }

        [Theory, MemberData(nameof(GetSymmetricAlgorithmsPairs))]
        public void SymmetricKeyWrapEncryptionRoundtrip(SymmetricAlgorithmFactory keyFactory, SymmetricAlgorithmFactory innerKeyFactory)
        {
            const string testString = "some text node";
            const string exampleXmlRootElement = "example";
            const string exampleXml = @"<?xml version=""1.0""?>
<example>
<test>some text node</test>
</example>";
            const string keyName = "mytestkey";

            using (SymmetricAlgorithm key = keyFactory.Create())
            {
                XmlDocument xmlDocToEncrypt = LoadXmlFromString(exampleXml);
                Assert.Contains(testString, xmlDocToEncrypt.OuterXml);
                Encrypt(xmlDocToEncrypt, exampleXmlRootElement, "EncryptedElement1", key, keyName, innerKeyFactory);

                Assert.DoesNotContain(testString, xmlDocToEncrypt.OuterXml);
                XmlDocument xmlDocToDecrypt = LoadXmlFromString(xmlDocToEncrypt.OuterXml);
                Decrypt(xmlDocToDecrypt, key, keyName);

                Assert.Equal(exampleXml.Replace("\r\n", "\n"), xmlDocToDecrypt.OuterXml.Replace("\r\n", "\n"));
            }
        }
    }
}
