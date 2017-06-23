// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class ReplaceChildTests
    {
        [Fact]
        public static void ReplaceFirstChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var child2 = xmlDocument.DocumentElement.ChildNodes[1];
            var newChild = xmlDocument.CreateElement("newChild");

            Assert.Same(child1, xmlDocument.DocumentElement.FirstChild);
            Assert.Same(child2, xmlDocument.DocumentElement.LastChild);

            xmlDocument.DocumentElement.ReplaceChild(newChild, child1);

            Assert.Same(newChild, xmlDocument.DocumentElement.FirstChild);
            Assert.Same(child2, xmlDocument.DocumentElement.LastChild);
        }

        [Fact]
        public static void ReplaceLastChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/><child2/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var child2 = xmlDocument.DocumentElement.ChildNodes[1];
            var newChild = xmlDocument.CreateElement("newChild");

            Assert.Same(child1, xmlDocument.DocumentElement.FirstChild);
            Assert.Same(child2, xmlDocument.DocumentElement.LastChild);

            xmlDocument.DocumentElement.ReplaceChild(newChild, child2);

            Assert.Same(child1, xmlDocument.DocumentElement.FirstChild);
            Assert.Same(newChild, xmlDocument.DocumentElement.LastChild);
        }

        [Fact]
        public static void DocumentElementNodeWithNewNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root/>");

            var root = xmlDocument.DocumentElement;
            var newRoot = xmlDocument.CreateElement("newroot");

            xmlDocument.ReplaceChild(newRoot, root);
            Assert.Same(newRoot, xmlDocument.DocumentElement);
        }

        [Fact]
        public static void NodeWithNoChild()
        {
            var xmlDocument = new XmlDocument();
            var oldElement = xmlDocument.CreateElement("element");
            var newElement = xmlDocument.CreateElement("element2");

            Assert.Throws<NullReferenceException>(() => oldElement.ReplaceChild(newElement, null));
        }

        [Fact]
        public static void NotNodeChild()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1><gchild1/></child1><child2/></root>");

            var child1 = xmlDocument.DocumentElement.ChildNodes[0];
            var child2 = xmlDocument.DocumentElement.ChildNodes[1];
            var newChild = xmlDocument.CreateElement("newElem");

            AssertExtensions.Throws<ArgumentException>(null, () => child1.ReplaceChild(newChild, child2));
        }

        [Fact]
        public static void DifferentTrees()
        {
            var xmlDocument1 = new XmlDocument();
            var xmlDocument2 = new XmlDocument();

            xmlDocument1.LoadXml("<root><child1/></root>");

            var child1 = xmlDocument1.DocumentElement.ChildNodes[0];
            var newChild = xmlDocument2.CreateElement("newChild");

            AssertExtensions.Throws<ArgumentException>(null, () => xmlDocument1.DocumentElement.ReplaceChild(newChild, child1));
        }

        [Fact]
        public static void ReplaceElementWithAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/></root>");

            var child1 = xmlDocument.DocumentElement.FirstChild;
            var attribute = xmlDocument.CreateAttribute("attr");

            Assert.Throws<InvalidOperationException>(() => xmlDocument.DocumentElement.ReplaceChild(attribute, child1));
        }

        [Fact]
        public static void ReplaceAttributeChildWithElement()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr='test'/>");

            var attribute = xmlDocument.DocumentElement.Attributes[0];
            var attributeChild = attribute.FirstChild;
            var newElement = xmlDocument.CreateElement("newElement");

            Assert.Throws<InvalidOperationException>(() => attribute.ReplaceChild(newElement, attributeChild));
            Assert.Null(attribute.FirstChild);
        }

        [Fact]
        public static void ReplaceAttributeChildWithAttribute()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr='test'/>");

            var attribute = xmlDocument.DocumentElement.Attributes[0];
            var attributeChild = attribute.FirstChild;
            var newAttribute = xmlDocument.CreateAttribute("newAttribute");

            Assert.Throws<InvalidOperationException>(() => attribute.ReplaceChild(newAttribute, attributeChild));
            Assert.Null(attribute.FirstChild);
        }

        [Fact]
        public static void ReplaceAttributeChildWithText()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr='test'/>");

            var attribute = xmlDocument.DocumentElement.Attributes[0];
            var attributeChild = attribute.FirstChild;
            var newText = xmlDocument.CreateTextNode("text");

            var replaced = attribute.ReplaceChild(newText, attributeChild);

            Assert.Same(replaced, attributeChild);
            Assert.Same(newText, attribute.FirstChild);
        }

        [Fact]
        public static void ReplaceAttributeChildWithProcessingInstruction()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root attr='test'/>");

            var attribute = xmlDocument.DocumentElement.Attributes[0];
            var attributeChild = attribute.FirstChild;
            var newText = xmlDocument.CreateProcessingInstruction("PI", "instructions");

            Assert.Throws<InvalidOperationException>(() => attribute.ReplaceChild(newText, attributeChild));
        }

        [Fact]
        public static void ReplaceElementWithDocumentFragmentNode()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><child1/></root>");

            var element = xmlDocument.DocumentElement.FirstChild;
            var newDocumentFragment = xmlDocument.CreateDocumentFragment();

            var result = xmlDocument.DocumentElement.ReplaceChild(newDocumentFragment, element);

            Assert.Same(element, result);
            Assert.Null(xmlDocument.DocumentElement.FirstChild);
        }
    }
}
