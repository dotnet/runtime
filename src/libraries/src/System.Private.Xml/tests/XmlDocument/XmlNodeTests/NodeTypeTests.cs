// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
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
