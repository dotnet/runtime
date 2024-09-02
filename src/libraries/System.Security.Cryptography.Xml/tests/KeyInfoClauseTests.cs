using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class KeyInfoClauseTests
    {
        [Fact]
        public void LoadXml_WithValidKeyNameXml_SetsKeyNameValue()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<KeyInfo><KeyName>MyKey</KeyName></KeyInfo>");
            var keyInfoName = new KeyInfoName();
            keyInfoName.LoadXml(xmlDocument.DocumentElement);

            Assert.Equal("MyKey", keyInfoName.Value);
        }

        [Fact]
        public void LoadXml_InvalidXml_ThrowsException()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<InvalidXml></InvalidXml>");
            var keyInfoClause = new TestKeyInfoClause();
            Assert.Throws<XmlException>(() => keyInfoClause.LoadXml(xmlDocument.DocumentElement));
        }

        [Fact]
        public void GetXml_ReturnsExpectedXml()
        {
            var keyInfoClause = new TestKeyInfoClause { KeyName = "Test" };
            XmlElement xmlElement = keyInfoClause.GetXml();
            Assert.Equal("<KeyInfo><KeyName>Test</KeyName></KeyInfo>", xmlElement.OuterXml);
        }

        private class TestKeyInfoClause : KeyInfoClause
        {
            public string KeyName { get; set; }

            public override XmlElement GetXml()
            {
                var xmlDocument = new XmlDocument();
                XmlElement keyInfoElement = xmlDocument.CreateElement("KeyInfo");
                XmlElement keyNameElement = xmlDocument.CreateElement("KeyName");
                keyNameElement.InnerText = KeyName;
                keyInfoElement.AppendChild(keyNameElement);
                return keyInfoElement;
            }

            public override void LoadXml(XmlElement element)
            {
                if (element.Name != "KeyInfo")
                {
                    throw new XmlException("Invalid XML element");
                }

                XmlElement keyNameElement = element["KeyName"];
                if (keyNameElement == null)
                {
                    throw new XmlException("Missing KeyName element");
                }

                KeyName = keyNameElement.InnerText;
            }
        }
    }
}
