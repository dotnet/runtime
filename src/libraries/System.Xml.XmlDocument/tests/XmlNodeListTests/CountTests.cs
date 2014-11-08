using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlNodeListTests
{
    class CountTests
    {
        [Fact]
        public static void CountTest1()
        {
            var xd = new XmlDocument();
            xd.LoadXml("<a><sub1/><sub2/></a>");

            Assert.Equal(2, xd.DocumentElement.ChildNodes.Count);
        }
    }
}
