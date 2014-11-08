using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlImplementationTests
{
    public class CreateDocumentTests
    {
        [Fact]
        public static void CreateDocument()
        {
            var imp = new XmlImplementation();

            var doc1 = imp.CreateDocument();
            var doc2 = imp.CreateDocument();

            Assert.NotNull(doc1);
            Assert.NotNull(doc2);
            Assert.NotSame(doc1, doc2);
        }
    }
}
