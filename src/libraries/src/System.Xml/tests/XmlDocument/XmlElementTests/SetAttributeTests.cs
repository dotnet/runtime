// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class SetAttributeTests
    {
        [Fact]
        public static void ElementWithNoAttributes()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<foo />");

            var node = (XmlElement)xmlDocument.DocumentElement;

            Assert.Equal(0, node.Attributes.Count);
            node.SetAttribute("att1", "newvalue");
            Assert.Equal(1, node.Attributes.Count);

            Assert.Equal("att1", node.Attributes.Item(0).Name);
            Assert.Equal("newvalue", node.Attributes.Item(0).Value);
        }

        [Fact]
        public static void AddingNewWithNamespace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<foo />");

            var node = (XmlElement)xmlDocument.DocumentElement;

            Assert.Equal(0, node.Attributes.Count);
            node.SetAttribute("att1", "nss", "newvalue");
            Assert.Equal(1, node.Attributes.Count);

            Assert.Equal("att1", node.Attributes.Item(0).Name);
            Assert.Equal("nss", node.Attributes.Item(0).NamespaceURI);
            Assert.Equal("newvalue", node.Attributes.Item(0).Value);
        }

        [Fact]
        public static void NoNamespaceWhenAttributeByThatNameWithNamespaceExists()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<ns1:elem1 ns1:att1=\"value1\" att1=\"value2\" xmlns:ns1=\"urn:URN1\"></ns1:elem1>");

            var node = (XmlElement)xmlDocument.DocumentElement;
            node.SetAttribute("att3", "value3");
            var attribute = node.GetAttributeNode("att3");

            Assert.Equal(string.Empty, attribute.NamespaceURI);
            Assert.Equal("value3", attribute.Value);
        }

        [Fact]
        public static void ValueWithNamespace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<ns1:elem1 ns1:att1=\"value1\" att1=\"value2\" xmlns:ns1=\"urn:URN1\"></ns1:elem1>");

            var node = (XmlElement)xmlDocument.DocumentElement;
            var attribute = node.GetAttributeNode("ns1:att1");

            Assert.Equal(attribute.Value, "value1");
            node.SetAttribute("ns1:att1", "value3");
            Assert.Equal(attribute.Value, "value3");
        }

        [Fact]
        public static void AttributeAlreadyExists()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr1='test' />");

            var node = (XmlElement)xmlDocument.DocumentElement;
            var attribute = node.GetAttributeNode("attr1");
            Assert.Equal(attribute.Value, "test");
            node.SetAttribute("attr1", "newvalue");
            Assert.Equal(attribute.Value, "newvalue");
        }

        [Fact]
        public static void AttributeThatHasBeenDefined()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr1='test' />");

            var node = (XmlElement)xmlDocument.DocumentElement;
            var attribute = node.GetAttributeNode("attr1");
            Assert.Equal(attribute.Value, "test");
            node.SetAttribute("attr1", "newvalue");
            Assert.Equal(attribute.Value, "newvalue");
        }

        [Fact]
        public static void AttributeThatHasBeenDefinedWithMoreThanTwoAttributes()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr0='test0' attr1='test' attr2='test2' />");

            var node = (XmlElement)xmlDocument.DocumentElement;
            var attribute = node.GetAttributeNode("attr1");
            Assert.Equal(attribute.Value, "test");
            node.SetAttribute("attr1", "newvalue");
            Assert.Equal(attribute.Value, "newvalue");
        }

        [Fact]
        public static void ElementWithAttributes()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr1='test' />");

            var node = (XmlElement)xmlDocument.DocumentElement;

            Assert.Null(node.Attributes.GetNamedItem("attr2"));
            Assert.Equal(1, node.Attributes.Count);

            node.SetAttribute("attr2", "newvalue");
            var attribute = node.Attributes.GetNamedItem("attr2");

            Assert.Equal(attribute.Value, "newvalue");
            Assert.Equal(2, node.Attributes.Count);
        }
    }
}
