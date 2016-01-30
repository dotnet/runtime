// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class PreviousSiblingTests
    {
        [Fact]
        public static void OnElementNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><elem1/><elem2/><elem3/></root>");

            var elem1 = xmlDocument.DocumentElement.ChildNodes[0];
            var elem2 = xmlDocument.DocumentElement.ChildNodes[1];
            var elem3 = xmlDocument.DocumentElement.ChildNodes[2];

            Assert.Null(elem1.PreviousSibling);
            Assert.Same(elem1, elem2.PreviousSibling);
            Assert.Same(elem2, elem3.PreviousSibling);
        }

        [Fact]
        public static void OnTextNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/>some text</root>");

            Assert.Equal(XmlNodeType.Text, xmlDocument.DocumentElement.ChildNodes[1].NodeType);
            Assert.Equal(xmlDocument.DocumentElement.ChildNodes[1].PreviousSibling, xmlDocument.DocumentElement.ChildNodes[0]);
        }

        [Fact]
        public static void OnTextNodeSplit()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root>some text</root>");

            var textNode = (XmlText)xmlDocument.DocumentElement.FirstChild;

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Null(textNode.PreviousSibling);

            var split = textNode.SplitText(4);

            Assert.Equal(2, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal(textNode, split.PreviousSibling);
            Assert.Null(textNode.PreviousSibling);
        }

        [Fact]
        public static void OnCommentNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><!--some text--></root>");

            Assert.Equal(XmlNodeType.Comment, xmlDocument.DocumentElement.ChildNodes[1].NodeType);
            Assert.Equal(xmlDocument.DocumentElement.ChildNodes[0], xmlDocument.DocumentElement.ChildNodes[1].PreviousSibling);
        }

        [Fact]
        public static void SiblingOfLastChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root>some text<child1/><child2/></root>");

            Assert.Same(xmlDocument.DocumentElement.ChildNodes[1], xmlDocument.DocumentElement.LastChild.PreviousSibling);
        }

        [Fact]
        public static void OnCDataNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><![CDATA[ <opentag> without an </endtag> and & <! are all ok here ]]></root>");

            Assert.Equal(XmlNodeType.CDATA, xmlDocument.DocumentElement.ChildNodes[1].NodeType);
            Assert.Equal(xmlDocument.DocumentElement.ChildNodes[0], xmlDocument.DocumentElement.ChildNodes[1].PreviousSibling);
        }

        [Fact]
        public static void OnDocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/>some text<child2/><child3/></root>");

            var documentFragment = xmlDocument.CreateDocumentFragment();
            documentFragment.AppendChild(xmlDocument.DocumentElement);

            Assert.Null(documentFragment.PreviousSibling);
        }

        [Fact]
        public static void OnDocumentElement()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<?PI pi info?><root/>");

            var piInfo = xmlDocument.ChildNodes[0];

            Assert.Equal(XmlNodeType.ProcessingInstruction, piInfo.NodeType);
            Assert.Equal(piInfo, xmlDocument.DocumentElement.PreviousSibling);
        }

        [Fact]
        public static void OnAttributeNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr1='test' attr2='test2' />");

            var attr1 = xmlDocument.DocumentElement.Attributes[0];
            var attr2 = xmlDocument.DocumentElement.Attributes[1];

            Assert.Equal("attr1", attr1.Name);
            Assert.Equal("attr2", attr2.Name);

            Assert.Null(attr1.PreviousSibling);
            Assert.Null(attr2.PreviousSibling);
        }

        [Fact]
        public static void OnAttributeNodeWithChildren()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr1='test' attr2='test2'><child1/><child2/><child3/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var child2 = xmlDocument.DocumentElement.ChildNodes[1];
            var child3 = xmlDocument.DocumentElement.ChildNodes[2];

            Assert.Null(child1.PreviousSibling);
            Assert.Same(child1, child2.PreviousSibling);
            Assert.Same(child2, child3.PreviousSibling);
        }

        [Fact]
        public static void ElementOneChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child/></root>");

            Assert.Null(xmlDocument.DocumentElement.ChildNodes[0].PreviousSibling);
        }

        [Fact]
        public static void OnAllSiblings()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/><child3 attr='1'/>Some Text<child4/><!-- comment --><?PI processing info?></root>");

            var count = xmlDocument.DocumentElement.ChildNodes.Count;
            var nextNode = xmlDocument.DocumentElement.ChildNodes[count - 1];

            for (var idx = count - 2; idx >= 0; idx--)
            {
                var currentNode = xmlDocument.DocumentElement.ChildNodes[idx];
                Assert.Equal(currentNode, nextNode.PreviousSibling);
                nextNode = currentNode;
            }

            Assert.Null(nextNode.PreviousSibling);
        }

        [Fact]
        public static void RemoveChildCheckSibling()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var child2 = xmlDocument.DocumentElement.ChildNodes[1];

            Assert.Equal("child1", child1.Name);
            Assert.Equal("child2", child2.Name);
            Assert.Equal(child1, child2.PreviousSibling);
            Assert.Null(child1.PreviousSibling);

            xmlDocument.DocumentElement.RemoveChild(child2);

            Assert.Null(child2.PreviousSibling);
            Assert.Null(child1.PreviousSibling);
        }

        [Fact]
        public static void ReplaceChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/><child3/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var child2 = xmlDocument.DocumentElement.ChildNodes[1];
            var child3 = xmlDocument.DocumentElement.ChildNodes[2];

            Assert.Null(child1.PreviousSibling);
            Assert.Same(child1, child2.PreviousSibling);
            Assert.Same(child2, child3.PreviousSibling);

            var newNode = xmlDocument.CreateElement("child4");

            xmlDocument.DocumentElement.ReplaceChild(newNode, child2);

            Assert.Null(child1.PreviousSibling);
            Assert.Same(child1, newNode.PreviousSibling);
            Assert.Same(newNode, child3.PreviousSibling);
            Assert.Null(child2.PreviousSibling);
        }

        [Fact]
        public static void InsertChildAfter()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var newNode = xmlDocument.CreateElement("child2");

            Assert.Null(child1.PreviousSibling);

            xmlDocument.DocumentElement.InsertAfter(newNode, child1);

            Assert.Null(child1.PreviousSibling);
            Assert.Same(child1, newNode.PreviousSibling);
        }

        [Fact]
        public static void InsertChildBefore()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var newNode = xmlDocument.CreateElement("child2");

            Assert.Null(child1.PreviousSibling);

            xmlDocument.DocumentElement.InsertBefore(newNode, child1);

            Assert.Same(newNode, child1.PreviousSibling);
            Assert.Null(newNode.PreviousSibling);
        }

        [Fact]
        public static void AppendChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var newNode = xmlDocument.CreateElement("child2");

            Assert.Null(child1.PreviousSibling);

            xmlDocument.DocumentElement.AppendChild(newNode);

            Assert.Same(child1, newNode.PreviousSibling);
            Assert.Null(child1.PreviousSibling);
        }

        [Fact]
        public static void NewlyCreatedElement()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateElement("element");

            Assert.Null(node.PreviousSibling);
        }

        [Fact]
        public static void NewlyCreatedAttribute()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateAttribute("attribute");

            Assert.Null(node.PreviousSibling);
        }

        [Fact]
        public static void NewlyCreatedTextNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateTextNode("textnode");

            Assert.Null(node.PreviousSibling);
        }

        [Fact]
        public static void NewlyCreatedCDataNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateCDataSection("cdata section");

            Assert.Null(node.PreviousSibling);
        }

        [Fact]
        public static void NewlyCreatedProcessingInstruction()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateProcessingInstruction("PI", "data");

            Assert.Null(node.PreviousSibling);
        }

        [Fact]
        public static void NewlyCreatedComment()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateComment("comment");

            Assert.Null(node.PreviousSibling);
        }

        [Fact]
        public static void NewlyCreatedDocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateDocumentFragment();

            Assert.Null(node.PreviousSibling);
        }

        [Fact]
        public static void FirstChildNextSibling()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/><child3/></root>");

            Assert.Null(xmlDocument.DocumentElement.FirstChild.PreviousSibling);
        }
    }
}
