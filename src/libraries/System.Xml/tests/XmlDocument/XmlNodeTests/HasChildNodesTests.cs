// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class HasChildNodesTests
    {
        [Fact]
        public static void ElementWithManyChildren()
        {
            var xml = "<root>\r\n  text node one\r\n  <elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\">\r\n     text node two e1; text node three\r\n  </elem1><!-- comment3 --><?PI3 processing instruction?>e2;<foo /><![CDATA[ <opentag> without an </endtag> and & <! are all ok here ]]><elem2 att1=\"id1\" att2=\"up\" att3=\"attribute3\"><a /></elem2><elem2> \r\n      elem2-text1\r\n      <a> \r\n          this-is-a    \r\n      </a> \r\n\r\n      elem2-text2\r\n      e3;e4;<!-- elem2-comment1-->\r\n      elem2-text3\r\n\r\n      <b> \r\n          this-is-b\r\n      </b>\r\n\r\n      elem2-text4\r\n      <?elem2_PI elem2-PI?>\r\n      elem2-text5\r\n\r\n  </elem2></root>";
            var xmlDocument = new XmlDocument();

            xmlDocument.LoadXml(xml);

            Assert.True(xmlDocument.HasChildNodes);
        }

        [Fact]
        public static void DocumentWithOnlyDocumentElement()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc/>");

            Assert.False(xmlDocument.DocumentElement.HasChildNodes);
            Assert.True(xmlDocument.HasChildNodes);
        }

        [Fact]
        public static void AddAnAttributeToAnElementWithNoChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc><elem1/></doc>");

            var node = (XmlElement)xmlDocument.DocumentElement.FirstChild;
            node.SetAttribute("att1", "foo");

            Assert.False(node.HasChildNodes);
        }

        [Fact]
        public static void AppendAnElementNOdeToAnElementNodeWithNoChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc><elem1 att1='foo'/></doc>");

            var node = (XmlElement)xmlDocument.DocumentElement.FirstChild;
            var newNode = xmlDocument.CreateElement("newElem");

            Assert.False(node.HasChildNodes);
            node.AppendChild(newNode);
            Assert.True(node.HasChildNodes);
        }

        [Fact]
        public static void AttributeWithEmptyString()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr='value'/>");

            Assert.True(xmlDocument.DocumentElement.Attributes[0].HasChildNodes);
        }

        [Fact]
        public static void AttributeWithStringValue()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateAttribute("attribute");

            node.Value = "attribute_value";

            Assert.True(node.HasChildNodes);
        }

        [Fact]
        public static void ElementWithAttributeAndNoChild()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateElement("elem1");
            var attribute = xmlDocument.CreateAttribute("attrib");

            attribute.Value = "foo";
            node.Attributes.Append(attribute);

            Assert.False(node.HasChildNodes);
        }

        [Fact]
        public static void CloneAnElementWithChildNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 att1='foo'>text<a /></elem1>");

            var clonedTrue = xmlDocument.DocumentElement.CloneNode(true);
            var clonedFalse = xmlDocument.DocumentElement.CloneNode(false);

            Assert.True(clonedTrue.HasChildNodes);
            Assert.False(clonedFalse.HasChildNodes);
        }

        [Fact]
        public static void RemoveAllChildren()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/><child3/></root>");

            Assert.Equal(3, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.True(xmlDocument.DocumentElement.HasChildNodes);

            for (int i = 0; i < 3; i++)
                xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.ChildNodes[0]);

            Assert.Equal(0, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.False(xmlDocument.DocumentElement.HasChildNodes);
        }

        [Fact]
        public static void RemoveAllChildrenAddAnElement()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/><child3/></root>");

            Assert.Equal(3, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.True(xmlDocument.DocumentElement.HasChildNodes);

            for (int i = 0; i < 3; i++)
                xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.ChildNodes[0]);

            Assert.Equal(0, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.False(xmlDocument.DocumentElement.HasChildNodes);

            var elem = xmlDocument.CreateElement("elem");
            xmlDocument.DocumentElement.AppendChild(elem);

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.True(xmlDocument.DocumentElement.HasChildNodes);
        }


        [Fact]
        public static void RemoveOnlyChildOfNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/></root>");

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.True(xmlDocument.DocumentElement.HasChildNodes);

            xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.ChildNodes[0]);

            Assert.Equal(0, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.False(xmlDocument.DocumentElement.HasChildNodes);
        }

        [Fact]
        public static void InsertAChildToDocumentFragment()
        {
            var xmlDocument = new XmlDocument();

            var fragment = xmlDocument.CreateDocumentFragment();
            var elem = xmlDocument.CreateElement("elem");

            fragment.AppendChild(elem);

            Assert.True(fragment.HasChildNodes);
        }

        [Fact]
        public static void CheckNoChildrenOnPI()
        {
            var xmlDocument = new XmlDocument();
            Assert.False(xmlDocument.CreateProcessingInstruction("PI", "info").HasChildNodes);
        }

        [Fact]
        public static void CheckNoChildrenOnComment()
        {
            var xmlDocument = new XmlDocument();
            Assert.False(xmlDocument.CreateComment("info").HasChildNodes);
        }

        [Fact]
        public static void CheckNoChildrenOnText()
        {
            var xmlDocument = new XmlDocument();
            Assert.False(xmlDocument.CreateTextNode("info").HasChildNodes);
        }

        [Fact]
        public static void CheckNoChildrenOnCData()
        {
            var xmlDocument = new XmlDocument();
            Assert.False(xmlDocument.CreateCDataSection("info").HasChildNodes);
        }

        [Fact]
        public static void ReplaceNodeWithChildrenWithEmptyNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/></root>");

            var newNode = xmlDocument.CreateElement("newElement");

            Assert.True(xmlDocument.DocumentElement.HasChildNodes);
            xmlDocument.ReplaceChild(newNode, xmlDocument.DocumentElement);
            Assert.False(xmlDocument.DocumentElement.HasChildNodes);
        }
    }
}
