// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class LastChildTests
    {
        [Fact]
        public static void ElementWithNoChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top />");

            Assert.Null(xmlDocument.DocumentElement.LastChild);
        }

        [Fact]
        public static void ElementWithNoChildTwoAttributes()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top attr1='test1' attr2='test2' />");

            Assert.Null(xmlDocument.DocumentElement.LastChild);
        }

        [Fact]
        public static void DeleteOnlyChildInsertNewNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem att1='foo'><a /></elem>");

            var node = xmlDocument.DocumentElement;
            var old = node.FirstChild;

            node.RemoveChild(old);

            var newNode = xmlDocument.CreateTextNode("textNode");
            node.AppendChild(newNode);

            Assert.Equal("textNode", node.LastChild.Value);
            Assert.Equal(XmlNodeType.Text, node.LastChild.NodeType);
            Assert.Equal(node.ChildNodes.Count, 1);
        }

        [Fact]
        public static void DeleteOnlyChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem att1='foo'><a /></elem>");

            var node = xmlDocument.DocumentElement;
            var oldNode = node.FirstChild;

            node.RemoveChild(oldNode);

            Assert.Null(node.LastChild);
            Assert.Equal(0, node.ChildNodes.Count);
        }

        [Fact]
        public static void DeleteOnlyChildAddTwoChildren()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem att1='foo'><a /></elem>");

            var node = xmlDocument.DocumentElement;
            var oldNode = node.FirstChild;

            node.RemoveChild(oldNode);

            var element1 = xmlDocument.CreateElement("elem1");
            var element2 = xmlDocument.CreateElement("elem2");

            node.AppendChild(element1);
            node.AppendChild(element2);

            Assert.Equal(2, node.ChildNodes.Count);
            Assert.Equal(element2, node.LastChild);
        }

        [Fact]
        public static void DeleteOnlyChildAddTwoChildrenDeleteBoth()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem att1='foo'><a /></elem>");

            var node = xmlDocument.DocumentElement;
            var oldNode = node.FirstChild;

            node.RemoveChild(oldNode);

            var element1 = xmlDocument.CreateElement("elem1");
            var element2 = xmlDocument.CreateElement("elem2");

            node.AppendChild(element1);
            node.AppendChild(element2);

            node.RemoveChild(element1);
            node.RemoveChild(element2);

            Assert.Null(node.LastChild);
            Assert.Equal(0, node.ChildNodes.Count);
        }

        [Fact]
        public static void AttributeWithOnlyText()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<element attrib='helloworld' />");

            var node = xmlDocument.DocumentElement.GetAttributeNode("attrib");

            Assert.Equal("helloworld", node.LastChild.Value);
            Assert.Equal(1, node.ChildNodes.Count);
        }

        [Fact]
        public static void ElementWithTwoAttributes()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(" <element attrib1='hello' attrib2='world' />");

            Assert.Null(xmlDocument.DocumentElement.LastChild);
        }

        [Fact]
        public static void ElementWithOneChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/></root>");

            Assert.Equal(XmlNodeType.Element, xmlDocument.DocumentElement.LastChild.NodeType);
            Assert.Equal("child1", xmlDocument.DocumentElement.LastChild.Name);
        }

        [Fact]
        public static void ElementWithMoreThanOneChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2>Some Text</child2><!-- comment --><?PI pi comments?></root>");

            Assert.Equal(4, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.NotNull(xmlDocument.DocumentElement.LastChild);
            Assert.Equal(XmlNodeType.ProcessingInstruction, xmlDocument.DocumentElement.LastChild.NodeType);
        }

        [Fact]
        public static void ElementNodeWithOneChildAndOneElement()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<element attrib1='value'>content</element>");

            Assert.Equal(XmlNodeType.Text, xmlDocument.DocumentElement.LastChild.NodeType);
            Assert.Equal("content", xmlDocument.DocumentElement.LastChild.Value);
        }


        [Fact]
        public static void NewlyCreatedElement()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateElement("element");

            Assert.Null(node.LastChild);
        }

        [Fact]
        public static void NewlyCreatedAttribute()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateAttribute("attribute");

            Assert.Null(node.LastChild);
        }

        [Fact]
        public static void NewlyCreatedTextNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateTextNode("textnode");

            Assert.Null(node.LastChild);
        }

        [Fact]
        public static void NewlyCreatedCDataNode()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateCDataSection("cdata section");

            Assert.Null(node.LastChild);
        }

        [Fact]
        public static void NewlyCreatedProcessingInstruction()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateProcessingInstruction("PI", "data");

            Assert.Null(node.LastChild);
        }

        [Fact]
        public static void NewlyCreatedComment()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateComment("comment");

            Assert.Null(node.LastChild);
        }

        [Fact]
        public static void NewlyCreatedDocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            var node = xmlDocument.CreateDocumentFragment();

            Assert.Null(node.LastChild);
        }

        [Fact]
        public static void InsertChildAtLengthMinus1()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/><child3/></root>");
            var child3 = xmlDocument.DocumentElement.LastChild;

            Assert.Equal(3, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal("child3", child3.Name);

            var newNode = xmlDocument.CreateElement("elem1");
            xmlDocument.DocumentElement.InsertBefore(newNode, child3);

            Assert.Equal(4, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal(child3, xmlDocument.DocumentElement.LastChild);
        }

        [Fact]
        public static void InsertChildToElementWithNoNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root/>");

            Assert.False(xmlDocument.DocumentElement.HasChildNodes);

            var newNode = xmlDocument.CreateElement("elem1");
            xmlDocument.DocumentElement.AppendChild(newNode);

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal(newNode, xmlDocument.DocumentElement.LastChild);
        }

        [Fact]
        public static void ReplaceOnlyChildOfNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child/></root>");

            var oldNode = xmlDocument.DocumentElement.LastChild;
            var newNode = xmlDocument.CreateElement("elem1");

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal(oldNode, xmlDocument.DocumentElement.LastChild);

            xmlDocument.DocumentElement.ReplaceChild(newNode, oldNode);

            Assert.Equal(1, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal(newNode, xmlDocument.DocumentElement.LastChild);
        }

        [Fact]
        public static void ReplaceChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/><child3/></root>");

            var oldNode = xmlDocument.DocumentElement.LastChild;
            var newNode = xmlDocument.CreateElement("elem1");

            Assert.Equal(3, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal(oldNode, xmlDocument.DocumentElement.LastChild);

            xmlDocument.DocumentElement.ReplaceChild(newNode, oldNode);

            Assert.Equal(3, xmlDocument.DocumentElement.ChildNodes.Count);
            Assert.Equal(newNode, xmlDocument.DocumentElement.LastChild);
        }
    }
}
