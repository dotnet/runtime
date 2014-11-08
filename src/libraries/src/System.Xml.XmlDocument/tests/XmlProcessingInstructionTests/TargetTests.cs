using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlProcessingInstructionTests
{
    public static class TargetTests
    {
        [Fact]
        public static void TargetTest1()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<?PI1 processing instruction?> <root> <?PI2 processing instruction2?> </root>");

            Assert.Equal("PI1", ((XmlProcessingInstruction)xmlDocument.FirstChild).Target);
            Assert.Equal("PI2", ((XmlProcessingInstruction)xmlDocument.DocumentElement.FirstChild).Target);
        }
    }
}
