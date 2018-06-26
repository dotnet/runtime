// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class ValueTests
    {
        [Fact]
        public static void OnDocumentNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root/>");

            Assert.Null(xmlDocument.Value);
            Assert.Throws<InvalidOperationException>(() => xmlDocument.Value = "some value");
        }

        [Fact]
        public static void NonEmptyAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr1='test' />");

            var attribute = xmlDocument.FirstChild.Attributes[0];

            Assert.Equal("test", attribute.Value);
        }

        [Fact]
        public static void ElementWithNoDescendents()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateElement("element");

            Assert.Null(node.Value);
            Assert.Throws<InvalidOperationException>(() => node.Value = "new value");
        }

        [Fact]
        public static void ElementWithDescendents()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><elem><child1/><child2/></elem></root>");

            var element = xmlDocument.DocumentElement.FirstChild;

            Assert.Equal(2, element.ChildNodes.Count);
            Assert.Null(element.Value);
            Assert.Throws<InvalidOperationException>(() => element.Value = "new value");
        }

        [Fact]
        public static void AttributeNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateAttribute("attribute");

            Assert.Equal(string.Empty, node.Value);
        }

        [Fact]
        public static void TextNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateTextNode("textnode");

            Assert.Equal("textnode", node.Value);
            node.Value = "some new text";
            Assert.Equal("some new text", node.Value);
        }

        [Fact]
        public static void CDataNodeNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateCDataSection("cdata section");

            Assert.Equal("cdata section", node.Value);
            node.Value = "new cdata";
            Assert.Equal("new cdata", node.Value);
        }

        [Fact]
        public static void ProcessingInstructionNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateProcessingInstruction("PI", "data");

            Assert.Equal("data", node.Value);
            node.Value = "new pi data";
            Assert.Equal("new pi data", node.Value);
        }

        [Fact]
        public static void CommentNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateComment("comment");

            Assert.Equal("comment", node.Value);
            node.Value = "new comment";
            Assert.Equal("new comment", node.Value);
        }

        [Fact]
        public static void RemoveCommentNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><!--comment--></root>");

            var commentNode = xmlDocument.DocumentElement.FirstChild;

            Assert.Equal(XmlNodeType.Comment, commentNode.NodeType);
            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal("comment", commentNode.Value);

            xmlDocument.DocumentElement.RemoveChild(commentNode);

            Assert.Equal(0, xmlDocument.DocumentElement.ChildNodes.Count);

            commentNode.Value = "new comment";
            xmlDocument.DocumentElement.AppendChild(commentNode);

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal(commentNode, xmlDocument.DocumentElement.ChildNodes[0]);
            Assert.Equal("new comment", commentNode.Value);
        }

        [Fact]
        public static void DocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateDocumentFragment();

            Assert.Null(node.Value);
            Assert.Throws<InvalidOperationException>(() => node.Value = "some value");
        }
    }
}
