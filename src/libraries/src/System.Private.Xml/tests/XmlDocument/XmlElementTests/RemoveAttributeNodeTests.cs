// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class RemoveAttributeNodeTests
    {
        [Fact]
        public static void GetElements()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root> <elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\"> text node two e1; text node three </elem1></root>");

            var node = (XmlElement)xmlDocument.DocumentElement.FirstChild;
            var attribute = (XmlAttribute)node.Attributes.Item(4);

            Assert.NotNull(node.Attributes.GetNamedItem("child5"));
            node.RemoveAttributeNode(attribute);

            Assert.Equal(4, node.Attributes.Count);
            Assert.Null(node.Attributes.GetNamedItem("child5"));
        }

        [Fact]
        public static void NormalWork()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>");

            Assert.Equal(6, xmlDocument.DocumentElement.Attributes.Count);
            var attr = xmlDocument.DocumentElement.RemoveAttributeNode("att1", "ns2");

            Assert.NotNull(attr);
            Assert.Equal(5, xmlDocument.DocumentElement.Attributes.Count);
        }

        [Fact]
        public static void WrongNamespace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>");

            Assert.Equal(6, xmlDocument.DocumentElement.Attributes.Count);
            var attr = xmlDocument.DocumentElement.RemoveAttributeNode("att1", "nsaa");

            Assert.Null(attr);
            Assert.Equal(6, xmlDocument.DocumentElement.Attributes.Count);
        }

        [Fact]
        public static void CallWithNull()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>");

            Assert.Equal(6, xmlDocument.DocumentElement.Attributes.Count);
            var attr = xmlDocument.DocumentElement.RemoveAttributeNode(null, null);

            Assert.Null(attr);
            Assert.Equal(6, xmlDocument.DocumentElement.Attributes.Count);
        }

        [Fact]
        public static void WrongName()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>");

            Assert.Equal(6, xmlDocument.DocumentElement.Attributes.Count);
            var attr = xmlDocument.DocumentElement.RemoveAttributeNode("att26", "ns2");

            Assert.Null(attr);
            Assert.Equal(6, xmlDocument.DocumentElement.Attributes.Count);
        }
    }
}
