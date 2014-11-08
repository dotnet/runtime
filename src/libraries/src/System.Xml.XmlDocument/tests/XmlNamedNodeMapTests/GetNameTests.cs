using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlNamedNodeMapTests
{
    public static class GetNameTests 
    {
        [Fact]
        public static void EmptyElementCountTest()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two text node three </elem1>");

            var namedNodeMap = (XmlNamedNodeMap)xmlDocument.FirstChild.Attributes;

            Assert.Equal(namedNodeMap.Item(0), namedNodeMap.GetNamedItem("child1"));
            Assert.Equal(namedNodeMap.Item(0).Value, string.Empty);

            Assert.Equal(namedNodeMap.Item(1), namedNodeMap.GetNamedItem("child2"));
            Assert.Equal(namedNodeMap.Item(1).Value, "duu");

            Assert.Equal(namedNodeMap.Item(4), namedNodeMap.GetNamedItem("child5"));
            Assert.Equal(namedNodeMap.Item(4).Value, "goody");
        }
    }
}
