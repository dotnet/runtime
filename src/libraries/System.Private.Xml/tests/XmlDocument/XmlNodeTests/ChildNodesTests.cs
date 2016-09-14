// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class ChildNodesTests
    {
        [Fact]
        public static void GetChildNodesOfAnElementNodeWithNoChildren()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc />");

            Assert.Equal(0, xmlDocument.DocumentElement.ChildNodes.Count);
        }

        [Fact]
        public static void GetChildNodesOfAnElementNodeWithAttributesAndChildren()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc doc=\"doc\"><doc> doc </doc></doc>");

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
        }

        [Fact]
        public static void GetChildNodesOfAnElementNodeWithAttributesButNoChildNodes()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc doc=\"doc\"></doc>");
            Assert.Equal(0, xmlDocument.DocumentElement.ChildNodes.Count);
        }

        [Fact]
        public static void AddChildNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc/>");

            var ch = xmlDocument.DocumentElement;
            var node = xmlDocument.CreateElement("newElem");
            var nu = node.FirstChild;

            node.AppendChild(ch);
            node.InsertBefore(ch, nu);

            Assert.Equal(1, node.ChildNodes.Count);
            Assert.Equal("doc", node.ChildNodes[0].Name);
        }

        [Fact]
        public static void CountMixture1()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root>text<node1><nestednode/></node1><node2/><?PI pi_info?><!-- comment --></root>");

            Assert.Equal(5, xmlDocument.FirstChild.ChildNodes.Count);
        }

        [Fact]
        public static void XmlWithWhitespace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(@"<a> 
          this-is-a    
      </a>");

            Assert.Equal(1, xmlDocument.FirstChild.ChildNodes.Count);
        }

        [Fact]
        public static void ChildNodeOfAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(@"<a attr1='test' />");

            var attr1 = xmlDocument.FirstChild.Attributes[0];

            Assert.Equal(1, attr1.ChildNodes.Count);
            Assert.Equal("test", attr1.FirstChild.Value);
        }

        [Fact]
        public static void DocumentFragmentChildNodes()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(@"<a attr1='test' />");
            var fragment = xmlDocument.CreateDocumentFragment();

            fragment.AppendChild(xmlDocument.DocumentElement);

            Assert.Equal(1, fragment.ChildNodes.Count);
        }

        [Fact]
        public static void EmptyElementChildNodeCount()
        {
            var xmlDocument = new XmlDocument();
            var item = xmlDocument.CreateElement("newElem");
            var clonedTrue = item.CloneNode(true);
            var clonedFalse = item.CloneNode(false);

            Assert.Equal(0, item.ChildNodes.Count);
            Assert.Equal(0, clonedTrue.ChildNodes.Count);
            Assert.Equal(0, clonedFalse.ChildNodes.Count);
        }

        [Fact]
        public static void EmptyAttributeChildNodeCount()
        {
            var xmlDocument = new XmlDocument();
            var item = xmlDocument.CreateAttribute("newElem");
            var clonedTrue = item.CloneNode(true);
            var clonedFalse = item.CloneNode(false);

            Assert.Equal(0, item.ChildNodes.Count);
            Assert.Equal(0, clonedTrue.ChildNodes.Count);
            Assert.Equal(0, clonedFalse.ChildNodes.Count);
        }

        [Fact]
        public static void TextNodeChildNodeCount()
        {
            var xmlDocument = new XmlDocument();
            var item = xmlDocument.CreateTextNode("text");
            var clonedTrue = item.CloneNode(true);
            var clonedFalse = item.CloneNode(false);

            Assert.Equal(0, item.ChildNodes.Count);
            Assert.Equal(0, clonedTrue.ChildNodes.Count);
            Assert.Equal(0, clonedFalse.ChildNodes.Count);
        }

        [Fact]
        public static void CommentChildNodeCount()
        {
            var xmlDocument = new XmlDocument();
            var item = xmlDocument.CreateComment("comment");
            var clonedTrue = item.CloneNode(true);
            var clonedFalse = item.CloneNode(false);

            Assert.Equal(0, item.ChildNodes.Count);
            Assert.Equal(0, clonedTrue.ChildNodes.Count);
            Assert.Equal(0, clonedFalse.ChildNodes.Count);
        }

        [Fact]
        public static void CDataChildNodeCount()
        {
            var xmlDocument = new XmlDocument();
            var item = xmlDocument.CreateCDataSection("cdata");
            var clonedTrue = item.CloneNode(true);
            var clonedFalse = item.CloneNode(false);

            Assert.Equal(0, item.ChildNodes.Count);
            Assert.Equal(0, clonedTrue.ChildNodes.Count);
            Assert.Equal(0, clonedFalse.ChildNodes.Count);
        }

        [Fact]
        public static void ProcessingInstructionChildNodeCount()
        {
            var xmlDocument = new XmlDocument();
            var item = xmlDocument.CreateProcessingInstruction("PI", "pi_info");
            var clonedTrue = item.CloneNode(true);
            var clonedFalse = item.CloneNode(false);

            Assert.Equal(0, item.ChildNodes.Count);
            Assert.Equal(0, clonedTrue.ChildNodes.Count);
            Assert.Equal(0, clonedFalse.ChildNodes.Count);
        }

        [Fact]
        public static void EmptyXmlDocument()
        {
            XmlDocument xd = new XmlDocument();
            Assert.Equal(0, xd.ChildNodes.Count);
        }

        [Fact]
        public static void ListWithMultipleKinds()
        {
            var xml = @"<root>
  text node one
  <elem1 child1="""" child2=""duu"" child3=""e1;e2;"" child4=""a1"" child5=""goody"">
     text node two e1; text node three
  </elem1><!-- comment3 --><?PI3 processing instruction?>e2;<foo /><![CDATA[ <opentag> without an </endtag> and & <! are all ok here ]]><elem2 att1=""id1"" att2=""up"" att3=""attribute3""><a /></elem2><elem2> 
      elem2-text1
      <a> 
          this-is-a    
      </a> 

      elem2-text2
      e3;e4;<!-- elem2-comment1-->
      elem2-text3

      <b> 
          this-is-b
      </b>

      elem2-text4
      <?elem2_PI elem2-PI?>
      elem2-text5

  </elem2></root>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            Assert.Equal(9, xmlDocument.DocumentElement.ChildNodes.Count);
        }

        [Fact]
        public static void ElementNodeWithChildButNoAttribute()
        {
            var xml = @"<a> 
          this-is-a    
      </a>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
        }

        [Fact]
        public static void ReplacingNodeDoesNotChangeChildNodesList()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(@" <doc doc=""doc""><doc>
     doc 
  </doc><html>Just for testing purpose</html></doc>");

            var newNode = xmlDocument.CreateTextNode("new text node");
            var countBefore = xmlDocument.DocumentElement.ChildNodes.Count;
            xmlDocument.DocumentElement.ReplaceChild(newNode, xmlDocument.DocumentElement.FirstChild);

            Assert.Equal(countBefore, xmlDocument.DocumentElement.ChildNodes.Count);
        }

        [Fact]
        public static void NestedChildren()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc><a><b><c><a><b><c_end></c_end></b></a></c></b></a></doc>");

            XmlNode node = xmlDocument.DocumentElement;

            while (node.HasChildNodes)
                node = node.ChildNodes[0];

            Assert.Equal(0, node.ChildNodes.Count);
            Assert.Equal("c_end", node.Name);
        }
    }
}
