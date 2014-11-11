// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System.Xml;

namespace XmlDocumentTests.XmlElementTests
{
    public class RemoveAttributeTests
    {
        [Fact]
        public static void GetElements()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc> <elem1 attr1=\"attr1\" attr2=\"foo\" attr3=\"foo\">This is a test</elem1> text after </doc>");

            var node = (XmlElement)xmlDocument.DocumentElement.FirstChild;
            Assert.Equal(3, node.Attributes.Count);

            // Remove last node
            var attribute = node.Attributes.Item(2);
            Assert.NotNull(node.Attributes.GetNamedItem("attr3"));
            node.RemoveAttribute(attribute.Name);

            Assert.Equal(2, node.Attributes.Count);
            Assert.Null(node.Attributes.GetNamedItem("attr3"));
        }

        [Fact]
        public static void RemoveAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>");

            Assert.Equal(6, xmlDocument.DocumentElement.Attributes.Count);
            xmlDocument.DocumentElement.RemoveAttribute("att1", "ns2");
            Assert.Equal(5, xmlDocument.DocumentElement.Attributes.Count);
        }

        [Fact]
        public static void WrongNamespace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>");

            Assert.Equal(6, xmlDocument.DocumentElement.Attributes.Count);
            xmlDocument.DocumentElement.RemoveAttribute("att1", "nsaa");
            Assert.Equal(6, xmlDocument.DocumentElement.Attributes.Count);
        }

        [Fact]
        public static void WrongName()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>");

            Assert.Equal(6, xmlDocument.DocumentElement.Attributes.Count);
            xmlDocument.DocumentElement.RemoveAttribute("att26", "ns2");
            Assert.Equal(6, xmlDocument.DocumentElement.Attributes.Count);
        }
    }
}
