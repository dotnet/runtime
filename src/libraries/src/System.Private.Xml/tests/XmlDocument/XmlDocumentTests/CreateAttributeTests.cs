// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class CreateAttributeTests
    {
        [Fact]
        public static void CreateBasicAttributeTest()
        {
            var xmlDocument = new XmlDocument();
            var attribute = xmlDocument.CreateAttribute("attributeName");

            Assert.Equal("attributeName", attribute.Name);
            Assert.Equal("attributeName", attribute.LocalName);
            Assert.Equal(string.Empty, attribute.Value);
            Assert.Equal(string.Empty, attribute.Prefix);
            Assert.Equal("attributeName=\"\"", attribute.OuterXml);
            Assert.Equal(string.Empty, attribute.InnerXml);
            Assert.Equal(XmlNodeType.Attribute, attribute.NodeType);
        }

        [Fact]
        public static void AttributeWithNoPrefixIsTheSameAsWithEmptyPrefix()
        {
            var xmlDocument = new XmlDocument();
            var attribute = xmlDocument.CreateAttribute("Att1");
            var attributeWithEmptyNamespace = xmlDocument.CreateAttribute("Att1", "");

            Assert.Equal(attribute.Name, attributeWithEmptyNamespace.Name);
            Assert.Equal(attribute.LocalName, attributeWithEmptyNamespace.LocalName);
            Assert.Equal(attribute.NamespaceURI, attributeWithEmptyNamespace.NamespaceURI);
            Assert.Equal(attribute.Prefix, attributeWithEmptyNamespace.Prefix);
        }

        [Fact]
        public static void AttributeWithPrefix()
        {
            var xmlDocument = new XmlDocument();
            var attribute = xmlDocument.CreateAttribute("attributeName", "namespace");

            Assert.Equal("attributeName", attribute.LocalName);
            Assert.Equal(string.Empty, attribute.Value);
            Assert.Equal(string.Empty, attribute.Prefix);
            Assert.Equal("namespace", attribute.NamespaceURI);
            Assert.Equal(string.Empty, attribute.InnerXml);
            Assert.Equal(XmlNodeType.Attribute, attribute.NodeType);
        }
    }
}
