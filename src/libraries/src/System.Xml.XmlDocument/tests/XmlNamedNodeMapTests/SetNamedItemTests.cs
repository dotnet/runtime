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
            namedNodeMap.SetNamedItem(newAttribute);

            Assert.NotNull(newAttribute);
            Assert.Equal(1, namedNodeMap.Count);
            Assert.Equal(newAttribute,namedNodeMap.GetNamedItem("newNode"));
        }

        [Fact]
        public static void NamedItemIsNull()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<foo />");

            var namedNodeMap = (XmlNamedNodeMap)xmlDocument.FirstChild.Attributes;

            // providing a null parameter does not throw an exception; returns null
            var oldNode = new object();
            Assert.DoesNotThrow(() => oldNode = namedNodeMap.SetNamedItem(null));
            Assert.Null(oldNode);           
        }

    }
}
