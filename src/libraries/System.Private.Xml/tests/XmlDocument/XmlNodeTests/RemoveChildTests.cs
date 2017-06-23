// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class RemoveChildTests
    {
        private static readonly XmlNodeType[] s_XmlNodeTypes = new XmlNodeType[] { XmlNodeType.Whitespace, XmlNodeType.SignificantWhitespace, XmlNodeType.CDATA, XmlNodeType.Text, XmlNodeType.Comment };
        private enum InsertType { InsertBefore, InsertAfter }

        [Fact]
        public static void IterativelyRemoveAllChildNodes()
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

            var count = xmlDocument.DocumentElement.ChildNodes.Count;

            for (int idx = 0; idx < count; idx++)
                xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.ChildNodes[0]);

            Assert.Equal(0, xmlDocument.DocumentElement.ChildNodes.Count);
        }

        [Fact]
        public static void RemoveDocumentElement()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<?PI pi1?><root><child1/><child2/><child3/></root><!--comment-->");

            var root = xmlDocument.DocumentElement;

            Assert.Equal(3, xmlDocument.ChildNodes.Count);

            xmlDocument.RemoveChild(root);

            Assert.Equal(2, xmlDocument.ChildNodes.Count);
            Assert.Equal(XmlNodeType.ProcessingInstruction, xmlDocument.ChildNodes[0].NodeType);
            Assert.Equal(XmlNodeType.Comment, xmlDocument.ChildNodes[1].NodeType);
        }

        [Fact]
        public static void OldChildIsNull()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child/></root>");

            Assert.Throws<NullReferenceException>(() => xmlDocument.DocumentElement.RemoveChild(null));
        }

        [Fact]
        public static void OldChildIsFirstChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var child2 = xmlDocument.DocumentElement.ChildNodes[1];

            Assert.Equal(2, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal(child1, xmlDocument.DocumentElement.FirstChild);

            xmlDocument.DocumentElement.RemoveChild(child1);

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal(child2, xmlDocument.DocumentElement.FirstChild);
        }

        [Fact]
        public static void NotChildNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1><gchild1/></child1><child2/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var child2 = xmlDocument.DocumentElement.ChildNodes[1];

            AssertExtensions.Throws<ArgumentException>(null, () => child1.RemoveChild(child2));
        }

        [Fact]
        public static void AccessRemovedChildByReference()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root>child1</root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];

            Assert.Equal("child1", child1.Value);
            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);

            xmlDocument.DocumentElement.RemoveChild(child1);

            Assert.Equal("child1", child1.Value);
            Assert.Equal(0, xmlDocument.DocumentElement.ChildNodes.Count);
        }

        [Fact]
        public static void RepeatedRemoveInsert()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root/>");

            var element = xmlDocument.CreateElement("elem");

            for (int i = 0; i < 100; i++)
            {
                Assert.False(xmlDocument.DocumentElement.HasChildNodes);
                xmlDocument.DocumentElement.AppendChild(element);
                Assert.True(xmlDocument.DocumentElement.HasChildNodes);
                xmlDocument.DocumentElement.RemoveChild(element);
            }

            Assert.False(xmlDocument.DocumentElement.HasChildNodes);
            Assert.Null(element.ParentNode);
        }

        [Fact]
        public static void FromNodesThatCannotContainChildren()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><?PI pi node?>some test<!--comment--><![CDATA[some data]]></root>");

            var piNode = xmlDocument.DocumentElement.ChildNodes[0];
            var textNode = xmlDocument.DocumentElement.ChildNodes[1];
            var commentNode = xmlDocument.DocumentElement.ChildNodes[2];
            var cdataNode = xmlDocument.DocumentElement.ChildNodes[3];

            Assert.Equal(XmlNodeType.ProcessingInstruction, piNode.NodeType);
            Assert.Throws<InvalidOperationException>(() => piNode.RemoveChild(null));

            Assert.Equal(XmlNodeType.Text, textNode.NodeType);
            Assert.Throws<InvalidOperationException>(() => textNode.RemoveChild(null));

            Assert.Equal(XmlNodeType.Comment, commentNode.NodeType);
            Assert.Throws<InvalidOperationException>(() => commentNode.RemoveChild(null));

            Assert.Equal(XmlNodeType.CDATA, cdataNode.NodeType);
            Assert.Throws<InvalidOperationException>(() => cdataNode.RemoveChild(null));
        }

        [Fact]
        public static void Text_Text_Text()
        {
            RemoveChildTestBase(new[] { XmlNodeType.Text, XmlNodeType.Text, XmlNodeType.Text });
        }

        [Fact]
        public static void Whitespace_Whitespace_Whitespace()
        {
            RemoveChildTestBase(new XmlNodeType[] { XmlNodeType.Whitespace, XmlNodeType.Whitespace, XmlNodeType.Whitespace });
        }

        [Fact]
        public static void SignificantWhitespace_SignificantWhitespace_SignificantWhitespace()
        {
            RemoveChildTestBase(new XmlNodeType[] { XmlNodeType.SignificantWhitespace, XmlNodeType.SignificantWhitespace, XmlNodeType.SignificantWhitespace });
        }

        [Fact]
        public static void CDATA_CDATA_CDATA()
        {
            RemoveChildTestBase(new XmlNodeType[] { XmlNodeType.CDATA, XmlNodeType.CDATA, XmlNodeType.CDATA });
        }

        [Fact]
        public static void Whitespace_Text_Text_CDATA_SignificantWhitespace_SignificantWhitespace()
        {
            RemoveChildTestBase(new XmlNodeType[] { XmlNodeType.Whitespace, XmlNodeType.Text, XmlNodeType.Text, XmlNodeType.CDATA, XmlNodeType.SignificantWhitespace, XmlNodeType.SignificantWhitespace });
        }

        [Fact]
        public static void Text_Comment_CDATA()
        {
            var xml = @"<TMC>text<!-- comments --><![CDATA[ &lt; &amp; <tag> < ! > & </tag> 	 ]]></TMC>";

            foreach (var nodeType in s_XmlNodeTypes)
                DeleteNonTextNodeBase(xml, InsertType.InsertBefore, nodeType);
        }

        [Fact]
        public static void Text_Comment_SignificantWhitespace()
        {
            var xml = @"<TCS xml:space=""preserve"">text<!-- comments -->   	</TCS>";

            foreach (var nodeType in s_XmlNodeTypes)
                DeleteNonTextNodeBase(xml, InsertType.InsertBefore, nodeType);
        }

        [Fact]
        public static void Whitespace_Comment_Text()
        {
            var xml = @"<WMT>
            <!-- comments -->text</WMT>";

            foreach (var nodeType in s_XmlNodeTypes)
                DeleteNonTextNodeBase(xml, InsertType.InsertBefore, nodeType);
        }

        [Fact]
        public static void Whitespace_Element_Whitespace()
        {
            var xml = @"<WEW>
            <E/>
        </WEW>";

            foreach (var nodeType in s_XmlNodeTypes)
                DeleteNonTextNodeBase(xml, InsertType.InsertAfter, nodeType);
        }

        [Fact]
        public static void Text_Element_Text()
        {
            var xml = @"<TET>text1<E/>text2</TET>";

            foreach (var nodeType in s_XmlNodeTypes)
                DeleteNonTextNodeBase(xml, InsertType.InsertAfter, nodeType);
        }

        [Fact]
        public static void SignificantWhitespace_Element_SignificantWhitespace()
        {
            var xml = @" <SES xml:space=""preserve""> 	<E/>		</SES>";

            foreach (var nodeType in s_XmlNodeTypes)
                DeleteNonTextNodeBase(xml, InsertType.InsertAfter, nodeType);
        }

        [Fact]
        public static void CDATA_Element_CDATA()
        {
            var xml = @"<CEC><![CDATA[ &lt; &amp; <tag> < ! > & </tag> 	 ]]><E/><![CDATA[ &lt; &amp; <tag> < ! > & </tag> 	 ]]></CEC>";

            foreach (var nodeType in s_XmlNodeTypes)
                DeleteNonTextNodeBase(xml, InsertType.InsertAfter, nodeType);
        }

        private static void RemoveChildTestBase(XmlNodeType[] nodeTypes)
        {
            for (int i = 0; i < nodeTypes.Length; i++)
                RemoveChildTestBase(nodeTypes, i);
        }

        private static void DeleteNonTextNodeBase(string xml, InsertType insertType, XmlNodeType nodeType)
        {
            string[] expected = new string[3];

            var xmlDocument = new XmlDocument { PreserveWhitespace = true };
            xmlDocument.LoadXml(xml);

            XmlNode parent = xmlDocument.DocumentElement;
            XmlNode firstChild = parent.FirstChild;
            XmlNode lastChild = parent.LastChild;
            XmlNode nodeToRemove = parent.FirstChild.NextSibling;

            expected[0] = firstChild.OuterXml + lastChild.OuterXml;

            // deletion
            parent.RemoveChild(nodeToRemove);

            // verify
            Assert.Equal(2, parent.ChildNodes.Count);
            Assert.Equal(expected[0], parent.InnerXml);

            Assert.Equal(firstChild.ParentNode, parent);
            Assert.Equal(lastChild.ParentNode, parent);

            VerifySiblings(firstChild, lastChild, InsertType.InsertAfter);

            // now, parent contains two textnodes only
            XmlNode newChild = CreateNode(xmlDocument, nodeType);
            XmlNode refChild = (insertType == InsertType.InsertBefore) ? lastChild : firstChild;

            expected[1] = firstChild.OuterXml + newChild.OuterXml + lastChild.OuterXml;
            expected[2] = parent.InnerXml;

            // insertion
            var insertDelegate = CreateInsertBeforeOrAfter(insertType);
            insertDelegate(parent, newChild, refChild);

            // verify
            Assert.Equal(3, parent.ChildNodes.Count);
            Assert.Equal(expected[1], parent.InnerXml);

            Assert.Equal(newChild.ParentNode, parent);
            Assert.Equal(lastChild.ParentNode, parent);
            Assert.Equal(firstChild.ParentNode, parent);

            VerifySiblings(firstChild, newChild, InsertType.InsertAfter);
            VerifySiblings(newChild, lastChild, InsertType.InsertAfter);

            // delete the newChild
            parent.RemoveChild(newChild);

            // verify
            Assert.Equal(2, parent.ChildNodes.Count);
            Assert.Equal(expected[2], parent.InnerXml);

            Assert.Equal(firstChild.ParentNode, parent);
            Assert.Equal(lastChild.ParentNode, parent);

            VerifySiblings(firstChild, lastChild, InsertType.InsertAfter);
        }

        private static void RemoveChildTestBase(XmlNodeType[] nodeTypes, int ithNodeToRemove)
        {
            int total = nodeTypes.Length;
            XmlDocument doc = new XmlDocument { PreserveWhitespace = true };
            var parent = doc.CreateElement("root");
            doc.AppendChild(parent);

            var newChildren = new XmlNode[total];

            string expected = string.Empty;

            for (int i = 0; i < total; i++)
            {
                newChildren[i] = CreateNode(doc, nodeTypes[i]);
                parent.AppendChild(newChildren[i]);
                expected += newChildren[i].OuterXml;
            }

            Assert.Equal(total, parent.ChildNodes.Count);
            Assert.Equal(expected, parent.InnerXml);

            for (int i = 0; i < total - 1; i++)
            {
                Verify(parent, newChildren[i], newChildren[i + 1]);
                VerifySiblings(newChildren[i], newChildren[i + 1]);
            }

            expected = string.Empty;
            for (int i = 0; i < total; i++)
                if (i != ithNodeToRemove)
                    expected += newChildren[i].OuterXml;

            // remove either the FirstChild or LastChild according to the value of ithNodeToRemove
            parent.RemoveChild(newChildren[ithNodeToRemove]);

            Assert.Equal(total - 1, parent.ChildNodes.Count);
            Assert.Equal(expected, parent.InnerXml);
        }

        private static void VerifySiblings(XmlNode refChild, XmlNode newChild)
        {
            Assert.Equal(newChild, refChild.NextSibling);
            Assert.Equal(refChild, newChild.PreviousSibling);
        }

        private static void VerifySiblings(XmlNode refChild, XmlNode newChild, InsertType insertType)
        {
            switch (insertType)
            {
                case InsertType.InsertBefore:
                    VerifySiblings(newChild, refChild);
                    break;
                case InsertType.InsertAfter:
                    VerifySiblings(refChild, newChild);
                    break;
                default:
                    throw new ArgumentException("Wrong InsertType: '" + insertType + "'");
            }
        }

        private static void Verify(XmlNode parent, XmlNode child, XmlNode newChild)
        {
            Assert.Equal(child.ParentNode, parent);
            Assert.Equal(newChild.ParentNode, parent);
        }

        private static XmlNode CreateNode(XmlDocument doc, XmlNodeType nodeType)
        {
            Assert.NotNull(doc);

            switch (nodeType)
            {
                case XmlNodeType.CDATA:
                    return doc.CreateCDataSection(@"&lt; &amp; <tag> < ! > & </tag> 	 ");
                case XmlNodeType.Comment:
                    return doc.CreateComment(@"comment");
                case XmlNodeType.Element:
                    return doc.CreateElement("E");
                case XmlNodeType.Text:
                    return doc.CreateTextNode("text");
                case XmlNodeType.Whitespace:
                    return doc.CreateWhitespace(@"	  ");
                case XmlNodeType.SignificantWhitespace:
                    return doc.CreateSignificantWhitespace("	");
                default:
                    throw new ArgumentException("Wrong XmlNodeType: '" + nodeType + "'");
            }
        }

        private static XmlNode InsertBefore(XmlNode parent, XmlNode newChild, XmlNode refChild)
        {
            Assert.NotNull(parent);
            Assert.NotNull(newChild);
            return parent.InsertBefore(newChild, refChild);
        }

        private static XmlNode InsertAfter(XmlNode parent, XmlNode newChild, XmlNode refChild)
        {
            Assert.NotNull(parent);
            Assert.NotNull(newChild);
            return parent.InsertAfter(newChild, refChild);
        }

        private static Func<XmlNode, XmlNode, XmlNode, XmlNode> CreateInsertBeforeOrAfter(InsertType insertType)
        {
            switch (insertType)
            {
                case InsertType.InsertBefore:
                    return InsertBefore;
                case InsertType.InsertAfter:
                    return InsertAfter;
            }

            throw new ArgumentException("Unknown type");
        }
    }
}
