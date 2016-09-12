// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DocumentElement_InsertBeforeTests
    {
        [Fact]
        public static void NodeWithOneChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><b/></a>");

            var firstChild = xmlDocument.DocumentElement.FirstChild;
            var newNode = xmlDocument.CreateElement("newElem");
            var returned = xmlDocument.DocumentElement.InsertBefore(newNode, firstChild);

            Assert.Same(newNode, returned);
            Assert.Same(xmlDocument.DocumentElement.ChildNodes[0], newNode);
            Assert.Same(xmlDocument.DocumentElement.ChildNodes[1], firstChild);
        }

        [Fact]
        public static void NodeWithNoChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a></a>");
            var root = xmlDocument.DocumentElement;

            Assert.False(root.HasChildNodes);

            var newNode = xmlDocument.CreateElement("elem");
            var result = root.InsertBefore(newNode, null);

            Assert.Same(newNode, result);
            Assert.Equal(1, root.ChildNodes.Count);
            Assert.Same(result, root.ChildNodes[0]);
        }

        [Fact]
        public static void RemoveAndInsert()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><b/></a>");
            var root = xmlDocument.DocumentElement;

            root.RemoveChild(root.FirstChild);
            Assert.False(root.HasChildNodes);

            var newNode = xmlDocument.CreateElement("elem");
            var result = root.InsertBefore(newNode, null);

            Assert.Same(newNode, result);
            Assert.Equal(1, root.ChildNodes.Count);
            Assert.Same(result, root.ChildNodes[0]);
        }

        [Fact]
        public static void InsertNewNodeToElement()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><b/></a>");
            var root = xmlDocument.DocumentElement;
            var newNode = xmlDocument.CreateProcessingInstruction("PI", "pi data");

            root.InsertBefore(newNode, root.FirstChild);

            Assert.Equal(2, root.ChildNodes.Count);
        }

        [Fact]
        public static void InsertCDataNodeToDocumentNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a/>");
            var cDataSection = xmlDocument.CreateCDataSection("data");

            Assert.Throws<InvalidOperationException>(() => xmlDocument.InsertBefore(cDataSection, null));
        }

        [Fact]
        public static void InsertCDataNodeToDocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a/>");
            var docFragment = xmlDocument.CreateDocumentFragment();
            var cDataSection = xmlDocument.CreateCDataSection("data");

            Assert.Equal(0, docFragment.ChildNodes.Count);
            docFragment.InsertBefore(cDataSection, null);
            Assert.Equal(1, docFragment.ChildNodes.Count);
        }

        [Fact]
        public static void InsertCDataNodeToAnAttributeNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a attr='test' />");
            var attribute = xmlDocument.DocumentElement.Attributes[0];
            var cDataSection = xmlDocument.CreateCDataSection("data");

            Assert.Equal(1, attribute.ChildNodes.Count);
            Assert.Throws<InvalidOperationException>(() => attribute.InsertBefore(cDataSection, null));
            Assert.Equal(1, attribute.ChildNodes.Count);
        }

        [Fact]
        public static void InsertCDataToElementNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><b/></a>");
            var node = xmlDocument.DocumentElement.FirstChild;
            var cDataSection = xmlDocument.CreateCDataSection("data");

            Assert.Equal(0, node.ChildNodes.Count);
            Assert.Same(cDataSection, node.InsertBefore(cDataSection, null));
            Assert.Equal(1, node.ChildNodes.Count);
        }

        [Fact]
        public static void InsertChildNodeToItself()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><b/></a>");
            var node = xmlDocument.DocumentElement.FirstChild;

            var outerXmlBefore = xmlDocument.OuterXml;
            var result = xmlDocument.DocumentElement.InsertBefore(node, node);

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Same(node, result);
            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);

            Assert.Equal(outerXmlBefore, xmlDocument.OuterXml);
        }

        [Fact]
        public static void InsertCommentNodeToDocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            var documentFragment = xmlDocument.CreateDocumentFragment();
            var node = xmlDocument.CreateComment("some comment");

            Assert.Equal(0, documentFragment.ChildNodes.Count);

            var result = documentFragment.InsertBefore(node, null);

            Assert.Same(node, result);
            Assert.Equal(1, documentFragment.ChildNodes.Count);
        }

        [Fact]
        public static void InsertCommentNodeToDocument()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root/>");

            var node = xmlDocument.CreateComment("some comment");

            Assert.Equal(0, xmlDocument.DocumentElement.ChildNodes.Count);

            var result = xmlDocument.DocumentElement.InsertBefore(node, null);

            Assert.Same(node, result);
            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
        }

        [Fact]
        public static void InsertDocFragmentToDocFragment()
        {
            var xmlDocument = new XmlDocument();

            var root = xmlDocument.CreateElement("root");
            var docFrag1 = xmlDocument.CreateDocumentFragment();
            var docFrag2 = xmlDocument.CreateDocumentFragment();

            docFrag1.AppendChild(root);
            docFrag2.InsertBefore(docFrag1, null);

            Assert.Equal(1, docFrag2.ChildNodes.Count);
        }

        [Fact]
        public static void InsertTextNodeToCommentNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><!-- comment here--></root>");

            var commentNode = xmlDocument.DocumentElement.FirstChild;
            var textNode = xmlDocument.CreateTextNode("text node");

            Assert.Equal(XmlNodeType.Comment, commentNode.NodeType);
            Assert.Throws<InvalidOperationException>(() => commentNode.InsertBefore(textNode, null));
        }

        [Fact]
        public static void InsertTextNodeToProcessingInstructionNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><?PI pi instructions ?></root>");

            var processingNode = xmlDocument.DocumentElement.FirstChild;
            var textNode = xmlDocument.CreateTextNode("text node");

            Assert.Equal(XmlNodeType.ProcessingInstruction, processingNode.NodeType);
            Assert.Throws<InvalidOperationException>(() => processingNode.InsertBefore(textNode, null));
        }

        [Fact]
        public static void InsertTextNodeToTextNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root>text node</root>");

            var textNode = xmlDocument.DocumentElement.FirstChild;
            var newTextNode = xmlDocument.CreateTextNode("text node");

            Assert.Equal(XmlNodeType.Text, textNode.NodeType);
            Assert.Throws<InvalidOperationException>(() => textNode.InsertBefore(newTextNode, null));
        }

        [Fact]
        public static void InsertTextNodeToElementNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><elem/></root>");

            var node = xmlDocument.DocumentElement.FirstChild;
            var newTextNode = xmlDocument.CreateTextNode("text node");

            Assert.Equal(XmlNodeType.Element, node.NodeType);
            var result = node.InsertBefore(newTextNode, null);

            Assert.Equal(1, node.ChildNodes.Count);
            Assert.Equal(result, node.ChildNodes[0]);
        }

        [Fact]
        public static void InsertTextNodeToDocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root/>");

            var node = xmlDocument.CreateTextNode("some comment");
            var result = xmlDocument.DocumentElement.InsertBefore(node, null);

            Assert.Same(node, result);
            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
        }

        [Fact]
        public static void InsertAttributeNodeToDocumentNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root/>");

            var attribute = xmlDocument.CreateAttribute("attr");

            Assert.Throws<InvalidOperationException>(() => xmlDocument.InsertBefore(attribute, null));
        }

        [Fact]
        public static void InsertAttributeNodeToAttributeNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr='value'/>");

            var attribute = xmlDocument.CreateAttribute("attr");

            Assert.Throws<InvalidOperationException>(() => xmlDocument.InsertBefore(attribute, null));
        }
    }
}
