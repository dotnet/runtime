// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlNodeTests
{
    public class NodeTypeTests
    {
        [Fact]
        public static void AttributeNodeTypeTest()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a b='item' />");
            var item = xmlDocument.DocumentElement.Attributes.Item(0);

            Assert.Equal(XmlNodeType.Attribute, item.NodeType);
        }

        [Fact]
        public static void ElementNodeTypeTest()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a />");
            var item = xmlDocument.DocumentElement;

            Assert.Equal(XmlNodeType.Element, item.NodeType);
        }

        [Fact]
        public static void DocumentNodeTypeTest()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a />");

            Assert.Equal(XmlNodeType.Document, xmlDocument.NodeType);
        }

        [Fact]
        public static void CommentNodeTypeTest()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><!--TestComment--></a>");
            var item = xmlDocument.DocumentElement.FirstChild;

            Assert.Equal(XmlNodeType.Comment, item.NodeType);
        }

        [Fact]
        public static void TextNodeTypeTest()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a>Text</a>");
            var item = xmlDocument.DocumentElement.FirstChild;

            Assert.Equal(XmlNodeType.Text, item.NodeType);
        }


        [Fact]
        public static void CDataNodeTypeTest()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><![CDATA[cdata test]]></a>");
            var item = xmlDocument.DocumentElement.FirstChild;

            Assert.Equal(XmlNodeType.CDATA, item.NodeType);
        }

        [Fact]
        public static void ProcessingInstructionNodeTypeTest()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<a><?PI processing instruction?></a>");
            var item = xmlDocument.DocumentElement.FirstChild;

            Assert.Equal(XmlNodeType.ProcessingInstruction, item.NodeType);
        }
    }
}
