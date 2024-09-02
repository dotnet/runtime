using System;
using System.Security.Cryptography.Xml;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class EncryptionMethodTest
    {
        [Fact]
        public void GetXml_ReturnsExpectedXml()
        {
            var encryptionMethod = new EncryptionMethod("http://www.w3.org/2001/04/xmlenc#aes256-cbc");
            var xmlElement = encryptionMethod.GetXml();
            Assert.Equal("<EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#aes256-cbc\" />", xmlElement.OuterXml);
        }

        [Fact]
        public void LoadXml_ValidXml_Success()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<EncryptionMethod Algorithm=\"http://www.w3.org/2001/04/xmlenc#aes256-cbc\" />");
            var encryptionMethod = new EncryptionMethod();
            encryptionMethod.LoadXml(xmlDocument.DocumentElement);
            Assert.Equal("http://www.w3.org/2001/04/xmlenc#aes256-cbc", encryptionMethod.KeyAlgorithm);
        }

        [Fact]
        public void LoadXml_InvalidXml_ThrowsException()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<InvalidXml></InvalidXml>");
            var encryptionMethod = new EncryptionMethod();
            Assert.Throws<XmlException>(() => encryptionMethod.LoadXml(xmlDocument.DocumentElement));
        }

        [Fact]
        public void KeyAlgorithm_SetGet_ReturnsExpected()
        {
            var encryptionMethod = new EncryptionMethod();
            encryptionMethod.KeyAlgorithm = "http://www.w3.org/2001/04/xmlenc#aes256-cbc";
            Assert.Equal("http://www.w3.org/2001/04/xmlenc#aes256-cbc", encryptionMethod.KeyAlgorithm);
        }

        [Fact]
        public void KeySize_SetGet_ReturnsExpected()
        {
            var encryptionMethod = new EncryptionMethod();
            encryptionMethod.KeySize = 256;
            Assert.Equal(256, encryptionMethod.KeySize);
        }
    }
}
