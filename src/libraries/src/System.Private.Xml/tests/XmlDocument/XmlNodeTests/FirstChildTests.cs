// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class FirstChildTests
    {
        [Fact]
        public static void ElementWithNoChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top />");

            Assert.Null(xmlDocument.DocumentElement.FirstChild);
        }

        [Fact]
        public static void ElementWithNoChildTwoAttributes()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top attr1='test1' attr2='test2' />");

            Assert.Null(xmlDocument.DocumentElement.FirstChild);
        }

        [Fact]
        public static void ElementNodeWithOneChildOneAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top attr1='test1'><child /></top>");

            Assert.NotNull(xmlDocument.DocumentElement.FirstChild);
            Assert.Equal("child", xmlDocument.DocumentElement.FirstChild.Name);
        }

        [Fact]
        public static void ElementNodeWithOneChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top><child /></top>");

            Assert.NotNull(xmlDocument.DocumentElement.FirstChild);
            Assert.Equal("child", xmlDocument.DocumentElement.FirstChild.Name);
        }

        [Fact]
        public static void ElementWithOneChildAnotherInsertedAtBeginning()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top><child /></top>");
            var node = xmlDocument.CreateElement("element");

            Assert.Equal("child", xmlDocument.DocumentElement.FirstChild.Name);
            xmlDocument.DocumentElement.InsertBefore(node, xmlDocument.DocumentElement.FirstChild);
            Assert.Same(node, xmlDocument.DocumentElement.FirstChild);
        }

        [Fact]
        public static void ElementNodeWithOneChildDeleteIt()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top><child /></top>");
            xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.FirstChild);

            Assert.Null(xmlDocument.DocumentElement.FirstChild);
        }

        [Fact]
        public static void ElementNodeWithOneChildDeleteItInsertNewChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top><child /></top>");
            xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.FirstChild);

            var newNode = xmlDocument.CreateTextNode("text node");
            xmlDocument.DocumentElement.AppendChild(newNode);

            Assert.NotNull(xmlDocument.DocumentElement.FirstChild);
            Assert.Same(newNode, xmlDocument.DocumentElement.FirstChild);
        }

        [Fact]
        public static void ElementNodeWithOneChildDeleteItInsertTwoNewChildren()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top><child /></top>");
            xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.FirstChild);

            var newNode1 = xmlDocument.CreateTextNode("text node");
            var newNode2 = xmlDocument.CreateComment("comment here");

            xmlDocument.DocumentElement.AppendChild(newNode1);
            xmlDocument.DocumentElement.AppendChild(newNode2);

            Assert.NotNull(xmlDocument.DocumentElement.FirstChild);
            Assert.Same(newNode1, xmlDocument.DocumentElement.FirstChild);
        }

        [Fact]
        public static void ElementNodeWithOneChildDeleteItInsertTwoNewChildrenDeleteAll()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top><child /></top>");
            xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.FirstChild);

            var newNode1 = xmlDocument.CreateTextNode("text node");
            var newNode2 = xmlDocument.CreateComment("comment here");

            xmlDocument.DocumentElement.AppendChild(newNode1);
            xmlDocument.DocumentElement.AppendChild(newNode2);

            xmlDocument.DocumentElement.RemoveChild(newNode1);
            xmlDocument.DocumentElement.RemoveChild(newNode2);

            Assert.Null(xmlDocument.DocumentElement.FirstChild);
        }

        [Fact]
        public static void ElementNodeWithTwoChildren()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top><child /><child2 /></top>");

            Assert.NotNull(xmlDocument.DocumentElement.FirstChild);
            Assert.Equal("child", xmlDocument.DocumentElement.FirstChild.Name);
        }

        [Fact]
        public static void ElementNodeWithTwoChildrenDeleteFirst()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<top><child /><child2 /></top>");

            xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.FirstChild);
            Assert.Equal("child2", xmlDocument.DocumentElement.FirstChild.Name);
        }

        [Fact]
        public static void FirstChildOfNewElement()
        {
            var xmlDocument = new XmlDocument();

            Assert.Null(xmlDocument.CreateElement("element").FirstChild);
        }

        [Fact]
        public static void FirstChildOfNewAttribute()
        {
            var xmlDocument = new XmlDocument();

            Assert.Null(xmlDocument.CreateAttribute("attribute").FirstChild);
        }

        [Fact]
        public static void FirstChildOfAttributeWithOnlyText()
        {
            var xmlDocument = new XmlDocument();

            var attribute = xmlDocument.CreateAttribute("attribute");
            attribute.Value = "value";

            Assert.Equal(XmlNodeType.Text, attribute.FirstChild.NodeType);
            Assert.Equal("value", attribute.FirstChild.Value);
        }

        [Fact]
        public static void FirstChildOfNewTextNode()
        {
            var xmlDocument = new XmlDocument();

            Assert.Null(xmlDocument.CreateTextNode("text").FirstChild);
        }

        [Fact]
        public static void FirstChildOfNewCDataSection()
        {
            var xmlDocument = new XmlDocument();

            Assert.Null(xmlDocument.CreateCDataSection("some clear text").FirstChild);
        }

        [Fact]
        public static void FirstChildOfNewProcessingInstruction()
        {
            var xmlDocument = new XmlDocument();

            Assert.Null(xmlDocument.CreateProcessingInstruction("PI", "pi_info").FirstChild);
        }

        [Fact]
        public static void FirstChildOfNewComment()
        {
            var xmlDocument = new XmlDocument();

            Assert.Null(xmlDocument.CreateComment("a comment").FirstChild);
        }

        [Fact]
        public static void FirstChildOfNewDocumentFragment()
        {
            var xmlDocument = new XmlDocument();

            Assert.Null(xmlDocument.CreateDocumentFragment().FirstChild);
        }

        [Fact]
        public static void FirstChildOfNewDocumentFragmentWithChildren()
        {
            var xmlDocument = new XmlDocument();
            var fragment = xmlDocument.CreateDocumentFragment();
            var node1 = xmlDocument.CreateComment("comment");
            var node2 = xmlDocument.CreateCDataSection("some random text");

            fragment.AppendChild(node1);
            fragment.AppendChild(node2);

            Assert.Same(node1, fragment.FirstChild);
        }
    }
}
