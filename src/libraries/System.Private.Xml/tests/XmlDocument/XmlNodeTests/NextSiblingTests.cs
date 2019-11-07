// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class NextSiblingTests
    {
        [Fact]
        public static void OnTextNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root>some text<child1/></root>");

            Assert.Equal(XmlNodeType.Text, xmlDocument.DocumentElement.ChildNodes[0].NodeType);
            Assert.Equal(xmlDocument.DocumentElement.ChildNodes[0].NextSibling, xmlDocument.DocumentElement.ChildNodes[1]);
        }

        [Fact]
        public static void OnTextNodeSplit()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root>some text</root>");

            var textNode = (XmlText)xmlDocument.DocumentElement.FirstChild;

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Null(textNode.NextSibling);

            var split = textNode.SplitText(4);

            Assert.Equal(2, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal(split, textNode.NextSibling);
            Assert.Null(split.NextSibling);
        }

        [Fact]
        public static void OnCommentNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><!--some text--><child1/></root>");

            Assert.Equal(XmlNodeType.Comment, xmlDocument.DocumentElement.ChildNodes[0].NodeType);
            Assert.Equal(xmlDocument.DocumentElement.ChildNodes[0].NextSibling, xmlDocument.DocumentElement.ChildNodes[1]);
        }

        [Fact]
        public static void SiblingOfLastChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root>some text<child1/><child2/></root>");

            Assert.Null(xmlDocument.DocumentElement.LastChild.NextSibling);
        }

        [Fact]
        public static void OnCDataNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><![CDATA[ <opentag> without an </endtag> and & <! are all ok here ]]><child1/></root>");

            Assert.Equal(XmlNodeType.CDATA, xmlDocument.DocumentElement.ChildNodes[0].NodeType);
            Assert.Equal(xmlDocument.DocumentElement.ChildNodes[0].NextSibling, xmlDocument.DocumentElement.ChildNodes[1]);
        }

        [Fact]
        public static void OnDocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/>some text<child2/><child3/></root>");

            var documentFragment = xmlDocument.CreateDocumentFragment();
            documentFragment.AppendChild(xmlDocument.DocumentElement);

            Assert.Null(documentFragment.NextSibling);
        }

        [Fact]
        public static void OnDocumentElement()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root/><?PI pi info?>");

            var piInfo = xmlDocument.ChildNodes[1];

            Assert.Equal(XmlNodeType.ProcessingInstruction, piInfo.NodeType);
            Assert.Equal(piInfo, xmlDocument.DocumentElement.NextSibling);
        }

        [Fact]
        public static void OnAttributeNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr1='test' attr2='test2' />");

            var attr1 = xmlDocument.DocumentElement.Attributes[0];

            Assert.Equal("attr1", attr1.Name);
            Assert.Null(attr1.NextSibling);
        }

        [Fact]
        public static void OnAttributeNodeWithChildren()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr1='test' attr2='test2'><child1/><child2/><child3/></root>");

            var node = xmlDocument.DocumentElement.ChildNodes[2];

            Assert.Equal("child3", node.Name);
            Assert.Null(node.NextSibling);
        }

        [Fact]
        public static void ElementOneChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child/></root>");

            Assert.Null(xmlDocument.DocumentElement.ChildNodes[0].NextSibling);
        }

        [Fact]
        public static void OnAllSiblings()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/><child3 attr='1'/>Some Text<child4/><!-- comment --><?PI processing info?></root>");

            var count = xmlDocument.DocumentElement.ChildNodes.Count;
            var previousNode = xmlDocument.DocumentElement.ChildNodes[0];

            for (var idx = 1; idx < count; idx++)
            {
                var currentNode = xmlDocument.DocumentElement.ChildNodes[idx];
                Assert.Equal(previousNode.NextSibling, currentNode);
                previousNode = currentNode;
            }

            Assert.Null(previousNode.NextSibling);
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
            Assert.Equal(child2, child1.NextSibling);

            xmlDocument.DocumentElement.RemoveChild(child1);

            Assert.Null(child1.NextSibling);
        }

        [Fact]
        public static void ReplaceChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/><child3/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var child2 = xmlDocument.DocumentElement.ChildNodes[1];
            var child3 = xmlDocument.DocumentElement.ChildNodes[2];

            Assert.Equal(child2, child1.NextSibling);
            Assert.Equal(child3, child2.NextSibling);
            Assert.Null(child3.NextSibling);

            xmlDocument.DocumentElement.RemoveChild(child2);

            Assert.Equal(child3, child1.NextSibling);
            Assert.Null(child2.NextSibling);
            Assert.Null(child3.NextSibling);
        }

        [Fact]
        public static void InsertChildAfter()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var newNode = xmlDocument.CreateElement("child2");

            Assert.Null(child1.NextSibling);

            xmlDocument.DocumentElement.InsertAfter(newNode, child1);

            Assert.Equal(newNode, child1.NextSibling);
            Assert.Null(newNode.NextSibling);
        }

        [Fact]
        public static void AppendChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var newNode = xmlDocument.CreateElement("child2");

            Assert.Null(child1.NextSibling);

            xmlDocument.DocumentElement.AppendChild(newNode);

            Assert.Equal(newNode, child1.NextSibling);
            Assert.Null(newNode.NextSibling);
        }

        [Fact]
        public static void NewlyCreatedElement()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateElement("element");

            Assert.Null(node.NextSibling);
        }

        [Fact]
        public static void NewlyCreatedAttribute()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateAttribute("attribute");

            Assert.Null(node.NextSibling);
        }

        [Fact]
        public static void NewlyCreatedTextNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateTextNode("textnode");

            Assert.Null(node.NextSibling);
        }

        [Fact]
        public static void NewlyCreatedCDataNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateCDataSection("cdata section");

            Assert.Null(node.NextSibling);
        }

        [Fact]
        public static void NewlyCreatedProcessingInstruction()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateProcessingInstruction("PI", "data");

            Assert.Null(node.NextSibling);
        }

        [Fact]
        public static void NewlyCreatedComment()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateComment("comment");

            Assert.Null(node.NextSibling);
        }

        [Fact]
        public static void NewlyCreatedDocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateDocumentFragment();

            Assert.Null(node.NextSibling);
        }

        [Fact]
        public static void FirstChildNextSibling()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/><child3/></root>");

            Assert.Equal(xmlDocument.DocumentElement.ChildNodes[1], xmlDocument.DocumentElement.FirstChild.NextSibling);
        }
    }
}
