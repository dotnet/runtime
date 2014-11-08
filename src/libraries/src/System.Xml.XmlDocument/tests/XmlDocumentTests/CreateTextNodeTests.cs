using Xunit;
using System;
using System.Xml;

namespace XmlDocumentTests.XmlDocumentTests
{
    public class CreateTextNodeTests
    {
        [Fact]
        public static void NodeTypeTest()
        {
            var xmlDocument = new XmlDocument();
            var newNode = xmlDocument.CreateTextNode(String.Empty);

            Assert.Equal(XmlNodeType.Text, newNode.NodeType);
        }
    }
}
