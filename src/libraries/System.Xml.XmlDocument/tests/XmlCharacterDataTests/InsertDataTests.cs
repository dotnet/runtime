using Xunit;
using System;
using System.Xml;

namespace XmlDocumentTests.XmlCharacterDataTests
{
    public class InsertDataTests
    {
        [Fact]
        public static void InsertDataAtBeginningOfCdataNode()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("comment");

            cdataNode.InsertData(0, "hello ");

            Assert.Equal("hello comment", cdataNode.Data);
        }

        [Fact]
        public static void InsertDataAtMiddleOfCdataNode()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("comment");

            cdataNode.InsertData(3, " hello ");

            Assert.Equal("com hello ment", cdataNode.Data);
        }

        [Fact]
        public static void InsertDataInEmptyCdataNode()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection(null);

            cdataNode.InsertData(0, "hello");

            Assert.Equal("hello", cdataNode.Data);
        }

        [Fact]
        public static void InsertDataBeyondEndOfEmptyCdataNode()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection(null);

            Assert.Throws<ArgumentOutOfRangeException>(() => cdataNode.InsertData(1, "hello "));
        }

        [Fact]
        public static void InsertDataBeyondEndOfCdataNodeBigNumber()
        {
            var xmlDocument = new XmlDocument();
            var cdataNode = (XmlCharacterData)xmlDocument.CreateCDataSection("hello");

            Assert.Throws<ArgumentOutOfRangeException>(() => cdataNode.InsertData(10, "hello "));
        }
    }
}
