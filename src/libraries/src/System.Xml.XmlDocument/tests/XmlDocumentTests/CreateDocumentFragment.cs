using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlDocumentTests
{
    public class CreateDocumentFragment
    {
        [Fact]
        public static void CheckNodeType()
        {
            var xmlDocument = new XmlDocument();
            var documentFragment=xmlDocument.CreateDocumentFragment();

            Assert.Equal(XmlNodeType.DocumentFragment, documentFragment.NodeType);
        }
    }
}
