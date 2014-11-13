using System;
using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlNamedNodeMapTests
{
    public static class SetNamedItemTests
    {
        [Fact]
        public static void NamedItemDoesNotExist()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<foo />");

            var namedNodeMap = (XmlNamedNodeMap)xmlDocument.FirstChild.Attributes;
            Assert.Equal(0, namedNodeMap.Count);

            var newAttribute = xmlDocument.CreateNode(XmlNodeType.Attribute, "newNode", string.Empty);

            // null value should throw an argument exception
            Assert.Throws<ArgumentException>((() => namedNodeMap.SetNamedItem(newAttribute as XmlDocument)));

            namedNodeMap.SetNamedItem(newAttribute);

            Assert.NotNull(newAttribute);
            Assert.Equal(1, namedNodeMap.Count);
            Assert.Equal(newAttribute,namedNodeMap.GetNamedItem("newNode"));
        }

    }
}
