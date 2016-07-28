// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class GetAttributeNodeTests
    {
        [Fact]
        public static void GetAttributeWhenValueIsEmpty()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\"> text node two e1; text node three </elem1>");

            var expected = xmlDocument.DocumentElement.Attributes.Item(0);
            var attribute = xmlDocument.DocumentElement.GetAttributeNode("child1");

            Assert.Same(expected, attribute);
            Assert.Equal(string.Empty, attribute.Value);
            Assert.Equal("child1", attribute.Name);
        }

        [Fact]
        public static void GetAttributeWhenValueIsNotEmpty()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\"> text node two e1; text node three </elem1>");

            var expected = xmlDocument.DocumentElement.Attributes.Item(1);
            var attribute = xmlDocument.DocumentElement.GetAttributeNode("child2");

            Assert.Same(expected, attribute);
            Assert.Equal("duu", attribute.Value);
            Assert.Equal("child2", attribute.Name);
        }

        [Fact]
        public static void ElementWithoutAttributes()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1>text node two e1; text node three </elem1>");

            Assert.Null(xmlDocument.DocumentElement.GetAttributeNode("child2"));
        }

        [Fact]
        public static void CreateAttributeNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 attr1=\"attr1\">This is a test</elem1>");

            xmlDocument.DocumentElement.SetAttribute("attr2", "value");

            var attribute = xmlDocument.DocumentElement.GetAttributeNode("attr2");

            Assert.Equal("value", attribute.Value);
            Assert.Equal("attr2", attribute.Name);
            Assert.Equal(XmlNodeType.Attribute, attribute.NodeType);
        }

        [Fact]
        public static void CloneAttributeNodeDeepTrue()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 attr1=\"attr1\" attr2=\"value\">This is a test</elem1>");

            var attribute = xmlDocument.DocumentElement.GetAttributeNode("attr2");

            Assert.Equal("value", attribute.Value);
            Assert.Equal("attr2", attribute.Name);
            Assert.Equal(XmlNodeType.Attribute, attribute.NodeType);
        }

        [Fact]
        public static void CloneAttributeNodeDeepFalse()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 attr1=\"attr1\" attr2=\"value\">This is a test</elem1>");

            var attribute = xmlDocument.DocumentElement.GetAttributeNode("attr2");

            Assert.Equal("value", attribute.Value);
            Assert.Equal("attr2", attribute.Name);
            Assert.Equal(XmlNodeType.Attribute, attribute.NodeType);
        }

        [Fact]
        public static void CreateAttributeWithNamespaceNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 attr1=\"attr1\">This is a test</elem1>");

            var attribute = (XmlAttribute)xmlDocument.CreateAttribute("val1:val2");

            attribute.Value = "value";
            xmlDocument.DocumentElement.SetAttributeNode(attribute);

            var retrievedAttribute = xmlDocument.DocumentElement.GetAttributeNode("val1:val2");

            Assert.NotNull(retrievedAttribute);
            Assert.Equal("value", retrievedAttribute.Value);
            Assert.Equal("val1:val2", retrievedAttribute.Name);
            Assert.Equal(XmlNodeType.Attribute, retrievedAttribute.NodeType);
        }

        [Fact]
        public static void NonExistentAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 attr1=\"attr1\">This is a test</elem1>");

            Assert.Null(xmlDocument.DocumentElement.GetAttributeNode("attr2"));
        }

        [Fact]
        public static void EmptyString()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 attr1=\"attr1\">This is a test</elem1>");

            Assert.Null(xmlDocument.DocumentElement.GetAttributeNode(string.Empty));
        }

        [Fact]
        public static void GetRemovedAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 attr1=\"attr1\">This is a test</elem1>");
            xmlDocument.DocumentElement.RemoveAttribute("attr1");

            Assert.Null(xmlDocument.DocumentElement.GetAttributeNode("attr1"));
        }
    }
}
