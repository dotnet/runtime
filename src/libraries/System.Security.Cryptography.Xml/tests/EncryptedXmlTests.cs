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
    }
}
