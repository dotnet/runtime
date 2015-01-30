// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Xml;

namespace XmlDocumentTests.XmlDocumentTests
{
    public class CreateElementTests
    {
        [Fact]
        public static void ElementName()
        {
            var xmlDocument = new XmlDocument();
            var nodeName = "nodeName";

            var newNode = xmlDocument.CreateElement(nodeName);

            Assert.Equal(nodeName, newNode.Name);
        }

        [Fact]
        [OuterLoop]
        public static void LongElementName()
        {
            var xmlDocument = new XmlDocument();
            var nodeName = new String('a', 2097152);

            var newNode = xmlDocument.CreateElement(nodeName);

            Assert.Equal(nodeName, newNode.Name);
        }

        [Fact]
        public static void SetAttributeAndInnerText()
        {
            var xmlDocument = new XmlDocument();
            var newNode = xmlDocument.CreateElement("newElem");

            newNode.SetAttribute("newAttribute", "newAttributeValue");
            newNode.InnerText = "this is a text node";

            Assert.Equal("<newElem newAttribute=\"newAttributeValue\">this is a text node</newElem>", newNode.OuterXml);
        }

        [Fact]
        public static void EmptyName()
        {
            var xmlDocument = new XmlDocument();

            Assert.Throws<ArgumentException>(() => xmlDocument.CreateElement(String.Empty));
        }

        [Fact]
        public static void NullName()
        {
            var xmlDocument = new XmlDocument();

            Assert.Throws<NullReferenceException>(() => xmlDocument.CreateElement(null));
        }

        [Fact]
        public static void NamespaceWithNoLocalName()
        {
            var xmlDocument = new XmlDocument();
            Assert.Throws<XmlException>(() => xmlDocument.CreateElement("foo:"));
        }

        [Fact]
        public static void NamespaceAndLocalNameWithColon()
        {
            var xmlDocument = new XmlDocument();
            Assert.Throws<XmlException>(() => xmlDocument.CreateElement("foo:b:::ar"));
        }

        [Fact]
        public static void NamespaceWithLocalName()
        {
            var xmlDocument = new XmlDocument();
            var newNode = xmlDocument.CreateElement("foo:bar", String.Empty);

            Assert.Equal("foo", newNode.Prefix);
            Assert.Equal("bar", newNode.LocalName);
            Assert.Equal("foo:bar", newNode.Name);
        }

        [Fact]
        [ActiveIssue(228)]
        public static void NameWithWhitespace()
        {
            var xmlDocument = new XmlDocument();
            Assert.Throws<XmlException>(() => xmlDocument.CreateElement("test name"));
            Assert.Throws<XmlException>(() => xmlDocument.CreateElement("test\rname"));
            Assert.Throws<XmlException>(() => xmlDocument.CreateElement("test\nname"));
            Assert.Throws<XmlException>(() => xmlDocument.CreateElement("test\bname"));
        }
    }
}
