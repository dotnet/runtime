using Xunit;
using System;
using System.Xml;

namespace XmlDocumentTests.XmlCharacterDataTests
{
    public class LengthTests
    {
        [Fact]
        public static void LengthOfCdata()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><![CDATA[abcdefgh]]></root>");

            var cdataNode = (XmlCharacterData)xmlDocument.DocumentElement.FirstChild;

            Assert.Equal(8, cdataNode.Length);
        }

        [Fact]
        public static void CreateCdata()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("abcde");

            Assert.Equal(5, cdataNode.Length);
        }

        [Fact]
        public static void CreateEmptyCdata()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection(String.Empty);

            Assert.Equal(0, cdataNode.Length);
        }

        [Fact]
        public static void LengthOfCdataAfterDelete()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("abcde");

            cdataNode.DeleteData(0, 1);
            Assert.Equal(4, cdataNode.Length);
        }
    }
}
