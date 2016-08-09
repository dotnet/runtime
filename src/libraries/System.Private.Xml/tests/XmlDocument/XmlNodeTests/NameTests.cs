// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class NameTests
    {
        [Fact]
        public static void XmlDeclaration()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<?xml version=\"1.0\" standalone=\"no\"?><root />");

            Assert.Equal("xml", xmlDocument.FirstChild.Name);
        }

        [Fact]
        public static void NameOfAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root dt:dt='test' xmlns:dt='testns' />");

            Assert.Equal("dt:dt", xmlDocument.DocumentElement.Attributes[0].Name);
        }

        [Fact]
        public static void NameOfDocumentElement()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root/>");

            Assert.Equal("#document", xmlDocument.Name);
        }

        [Fact]
        public static void NameOfAllTypes()
        {
            var xmlDocument = new XmlDocument();

            var element = xmlDocument.CreateElement("newElem");
            Assert.Equal("newElem", element.Name);

            var attribute = xmlDocument.CreateAttribute("newAttr");
            Assert.Equal("newAttr", attribute.Name);

            var text = xmlDocument.CreateTextNode("");
            Assert.Equal("#text", text.Name);

            var cdata = xmlDocument.CreateCDataSection("");
            Assert.Equal("#cdata-section", cdata.Name);

            var pi = xmlDocument.CreateProcessingInstruction("PI", "");
            Assert.Equal("PI", pi.Name);

            var comment = xmlDocument.CreateComment("some text");
            Assert.Equal("#comment", comment.Name);

            var fragment = xmlDocument.CreateDocumentFragment();
            Assert.Equal("#document-fragment", fragment.Name);
        }

        [Fact]
        public static void ElementWithExplicitlyStatedNamespace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root xmlns:my='http://something'><my:book price='99.95'><my:book></my:book></my:book></root>");

            Assert.Equal("my:book", xmlDocument.DocumentElement.FirstChild.Name);
        }

        [Fact]
        public static void ElementWithAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><book price='99.95' /></root>");

            Assert.Equal("book", xmlDocument.DocumentElement.FirstChild.Name);
        }
    }
}
