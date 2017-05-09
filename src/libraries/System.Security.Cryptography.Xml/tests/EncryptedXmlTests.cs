// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public static class EncryptedXmlTests
    {
        [Fact]
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
    }
}
