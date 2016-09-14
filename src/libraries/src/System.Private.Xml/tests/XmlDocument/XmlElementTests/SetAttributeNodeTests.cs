// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class SetAttributeNodeTests
    {
        [Fact]
        public static void ElementWithNoAttributes()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<foo />");

            var node = (XmlElement)xmlDocument.DocumentElement;
            var newAttribute = xmlDocument.CreateAttribute("attr1");
            newAttribute.Value = "newValue";

            Assert.Equal(0, node.Attributes.Count);
            node.SetAttributeNode(newAttribute);

            Assert.Equal(1, node.Attributes.Count);
            Assert.Same(node.Attributes.Item(0), newAttribute);
        }

        [Fact]
        public static void ElementWithSameAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<foo attr1='oldvalue' />");

            var node = (XmlElement)xmlDocument.DocumentElement;

            Assert.Equal(1, node.Attributes.Count);
            Assert.Equal("oldvalue", node.Attributes[0].Value);

            var newAttribute = xmlDocument.CreateAttribute("attr1");
            newAttribute.Value = "newValue";
            node.SetAttributeNode(newAttribute);

            Assert.Equal(1, node.Attributes.Count);
            Assert.Same(node.Attributes[0], newAttribute);
            Assert.Equal("newValue", node.Attributes[0].Value);
        }

        [Fact]
        public static void ElementWithAttributes()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<foo attr1='value' />");

            var node = (XmlElement)xmlDocument.DocumentElement;

            Assert.Equal(1, node.Attributes.Count);

            var newAttribute = xmlDocument.CreateAttribute("attr2");
            newAttribute.Value = "value2";
            node.SetAttributeNode(newAttribute);

            Assert.Equal(2, node.Attributes.Count);
            Assert.Same(node.Attributes[1], newAttribute);
            Assert.Equal("value2", node.Attributes[1].Value);
        }

        [Fact]
        public static void InsertAttributeInAnotherElement()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1 /><child2 /></root>");

            var child1 = (XmlElement)xmlDocument.DocumentElement.ChildNodes[0];
            var child2 = (XmlElement)xmlDocument.DocumentElement.ChildNodes[1];

            var attribute = xmlDocument.CreateAttribute("att1");

            child1.SetAttributeNode(attribute);

            Assert.Throws<InvalidOperationException>(() => child2.SetAttributeNode(attribute));
        }
    }
}
