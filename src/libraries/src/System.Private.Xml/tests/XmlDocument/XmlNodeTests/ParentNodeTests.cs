// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class ParentNodeTests
    {
        private const string ComplexDocument = "<?xml version=\"1.0\" standalone=\"no\"?><!-- comment1 the different between this one and domtest.xml is att in elem1 --><?PI1 processing instruction?><?xmlnamespace1 ns=\"something\" prefix=\"something\"?><?xmlnamespace2 ns=\"http://www.placeholder-name-here.com/schema/\" prefix=\"my\"?><?xmlnamespace3 ns=\"urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/\" prefix=\"dt\"?><!-- comment2 --><?PI2 processing instruction?><root xmlns:my=\"urn:http://www.placeholder-name-here.com/schema/\" xmlns:dt=\"urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/\">\r\n  text node one\r\n  <elem1 child1=\"\" child2=\"duu\" child3=\"blar\" child4=\"a1\" child5=\"goody\">\r\n     text node two \r\n     <ie att=\"sun\"></ie>\r\n     text node three\r\n  </elem1><!-- comment3 --><?PI3 processing instruction?>\r\n  something \r\n  <foo /><![CDATA[ <opentag> without an </endtag> and & <! are all ok here ]]><elem2 att1=\"id1\" att2=\"up\" att3=\"attribute3\"><a /></elem2><elem2> \r\n      elem2-text1\r\n      <a> \r\n          this-is-a    \r\n      </a> \r\n\r\n      elem2-text2\r\n      <!-- elem2-comment1-->\r\n      elem2-text3\r\n\r\n      <b> \r\n          this-is-b\r\n      </b>\r\n\r\n      elem2-text4\r\n      <?elem2_PI elem2-PI?>\r\n      elem2-text5\r\n\r\n  </elem2><my:book style=\"leather\" price=\"29.50\"><my:title>Who's Who in Trenton</my:title><my:author>Robert Bob</my:author></my:book><my:book price=\"99.95\"><my:book></my:book></my:book><my:book dt:style=\"string\" style=\"hard back\" price=\"19.99\"><my:title>Where in the world is Trenton?</my:title></my:book></root><!-- comment --><?PI processing instruction?>";

        private static void CheckChildren(XmlNode parent)
        {
            foreach (XmlNode child in parent.ChildNodes)
            {
                Assert.Same(parent, child.ParentNode);
                CheckChildren(child);
            }

            if (parent.Attributes == null)
                return;

            foreach (XmlNode attribute in parent.Attributes)
            {
                Assert.Same(attribute, attribute.FirstChild.ParentNode);
            }
        }

        [Fact]
        public static void MultipleTypesInComplexDocument()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(ComplexDocument);

            CheckChildren(xmlDocument);
        }

        [Fact]
        public static void ParentOfFirstChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top><child1/><child2/></top>");

            Assert.Same(xmlDocument.DocumentElement, xmlDocument.DocumentElement.FirstChild.ParentNode);
        }

        [Fact]
        public static void OnDocumentElement()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top><child1/><child2/></top>");

            Assert.Same(xmlDocument, xmlDocument.DocumentElement.ParentNode);
        }

        [Fact]
        public static void OnDocumentNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top><child1/><child2/></top>");

            Assert.Null(xmlDocument.ParentNode);
        }

        [Fact]
        public static void OnAttributeNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top attr1='test' />");

            var attribute = xmlDocument.DocumentElement.Attributes[0];

            Assert.Equal(XmlNodeType.Attribute, attribute.NodeType);
            Assert.Null(attribute.ParentNode);
        }

        [Fact]
        public static void OnAttributeNodeChildTextNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top attr1='test' />");

            var attribute = xmlDocument.DocumentElement.Attributes[0];
            var value = attribute.ChildNodes[0];

            Assert.Equal(XmlNodeType.Attribute, attribute.NodeType);
            Assert.Equal(XmlNodeType.Text, value.NodeType);
            Assert.Same(attribute, value.ParentNode);
        }

        [Fact]
        public static void NewlyCreatedElement()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateElement("element");

            Assert.Null(node.ParentNode);
        }

        [Fact]
        public static void NewlyCreatedAttribute()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateAttribute("attribute");

            Assert.Null(node.ParentNode);
        }

        [Fact]
        public static void NewlyCreatedTextNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateTextNode("textnode");

            Assert.Null(node.ParentNode);
        }

        [Fact]
        public static void NewlyCreatedCDataNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateCDataSection("cdata section");

            Assert.Null(node.ParentNode);
        }

        [Fact]
        public static void NewlyCreatedProcessingInstruction()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateProcessingInstruction("PI", "data");

            Assert.Null(node.ParentNode);
        }

        [Fact]
        public static void NewlyCreatedComment()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateComment("comment");

            Assert.Null(node.ParentNode);
        }

        [Fact]
        public static void NewlyCreatedDocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateDocumentFragment();

            Assert.Null(node.ParentNode);
        }
    }
}
