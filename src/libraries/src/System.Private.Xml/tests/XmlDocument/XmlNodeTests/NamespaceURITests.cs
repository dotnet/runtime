// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class NamespaceURITests
    {
        private const string xmlnsName = "http://www.w3.org/2000/xmlns/";

        [Fact]
        public static void AllNodesForEmptyString()
        {
            var xml = "<root>\r\n  text node one\r\n  <elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\">\r\n     text node two e1; text node three\r\n  </elem1><!-- comment3 --><?PI3 processing instruction?>e2;<foo /><![CDATA[ <opentag> without an </endtag> and & <! are all ok here ]]><elem2 att1=\"id1\" att2=\"up\" att3=\"attribute3\"><a /></elem2><elem2> \r\n      elem2-text1\r\n      <a> \r\n          this-is-a    \r\n      </a> \r\n\r\n      elem2-text2\r\n      e3;e4;<!-- elem2-comment1-->\r\n      elem2-text3\r\n\r\n      <b> \r\n          this-is-b\r\n      </b>\r\n\r\n      elem2-text4\r\n      <?elem2_PI elem2-PI?>\r\n      elem2-text5\r\n\r\n  </elem2></root>";
            var xmlDocument = new XmlDocument();

            xmlDocument.LoadXml(xml);

            foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                Assert.Equal(string.Empty, node.NamespaceURI);

            Assert.Equal(string.Empty, xmlDocument.CreateDocumentFragment().NamespaceURI);
        }

        [Fact]
        public static void XmlDocumentEmptyString()
        {
            var xmlDocument = new XmlDocument();
            Assert.Equal(string.Empty, xmlDocument.NamespaceURI);
        }

        [Fact]
        public static void XmlElementForEquals()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><Level1 xmlns=\"Value1\"></Level1><Level2 xmlns=\"Value2\"></Level2></root>");

            Assert.Equal("Value1", xmlDocument.DocumentElement.FirstChild.NamespaceURI);
        }

        [Fact]
        public static void ChildWithDifferentNamespaceURI()
        {
            var xDoc = new XmlDocument();

            var elem = xDoc.CreateElement("Elem1", "NameSpace1");
            var attr = xDoc.CreateAttribute("Elem1", "NameSpace2");

            Assert.Equal("NameSpace1", elem.NamespaceURI);
            Assert.Equal("NameSpace2", attr.NamespaceURI);
        }

        [Fact]
        public static void ChildWithDifferentNamespaceURIWithXmlns()
        {
            var xDoc = new XmlDocument();

            var elem = xDoc.CreateElement("Elem1", "NameSpace1");
            var attr = xDoc.CreateAttribute("Elem1", "NameSpace2");

            elem.Attributes.Append(attr);
            elem.SetAttribute("xmlns", "Namespace3");

            Assert.Equal("NameSpace1", elem.NamespaceURI);
            Assert.Equal("NameSpace2", attr.NamespaceURI);
            Assert.Equal("Namespace3", elem.GetAttribute("xmlns"));
        }

        [Fact]
        public static void ChangeXmlnsPropertyAfterLoading()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><Level1 xmlns=\"Value1\"></Level1><Level2 xmlns=\"Value2\"></Level2></root>");

            var attr = xmlDocument.DocumentElement.FirstChild.Attributes[0];

            Assert.Equal("xmlns", attr.Name);
            attr.Value = "foo";

            Assert.Equal("Value1", xmlDocument.DocumentElement.FirstChild.NamespaceURI);
            Assert.Equal("foo", attr.Value);
        }

        [Fact]
        public static void CreateAttributeXmlns()
        {
            var xmlDocument = new XmlDocument();

            var attr = xmlDocument.CreateAttribute("xmlns");
            Assert.Equal(xmlnsName, attr.NamespaceURI);
        }

        [Fact]
        public static void ReadXmlnsFromFile()
        {
            var xml = "<root><Level1 xmlns=\"Value1\"></Level1><Level2 xmlns=\"Value2\"></Level2></root>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            var attr = xmlDocument.DocumentElement.ChildNodes[1].Attributes[0];

            Assert.Equal("xmlns", attr.LocalName);
            Assert.Equal(xmlnsName, attr.NamespaceURI);
        }

        [Fact]
        public static void CloneAttributeXmlns()
        {
            var xmlDocument = new XmlDocument();
            var attr = xmlDocument.CreateAttribute("xmlns");
            var cloned = (XmlAttribute)attr.CloneNode(true);

            Assert.Equal(xmlnsName, cloned.NamespaceURI);
        }

        [Fact]
        public static void CreateAttributeXlmnsWithWrongNamespace()
        {
            var xmlDocument = new XmlDocument();

            AssertExtensions.Throws<ArgumentException>(null, () => xmlDocument.CreateAttribute("xmlns", "aa"));
        }
    }
}
