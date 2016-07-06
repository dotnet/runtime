// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class DocumentElement_GetElementsByTagNameTests
    {
        [Fact]
        public static void GetElements()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><elem1 /> <elem2 /> <elem3 /> <elem2 attr='test' /> </root>");

            var xmlNodeList = xmlDocument.DocumentElement.GetElementsByTagName("elem2");

            Assert.Equal(2, xmlNodeList.Count);
            Assert.Same(xmlDocument.DocumentElement.ChildNodes.Item(1), xmlNodeList.Item(0));
            Assert.Same(xmlDocument.DocumentElement.ChildNodes.Item(3), xmlNodeList.Item(1));
        }

        [Fact]
        public static void GetCaseSensitiveElements()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><elem1 /> <elEm2 /> <elem2 /> <elem2 attr='test' /> </root>");

            var xmlNodeList = xmlDocument.DocumentElement.GetElementsByTagName("elem2");

            Assert.Equal(2, xmlNodeList.Count);
            Assert.Same(xmlDocument.DocumentElement.ChildNodes.Item(2), xmlNodeList.Item(0));
            Assert.Same(xmlDocument.DocumentElement.ChildNodes.Item(3), xmlNodeList.Item(1));

            var xmlNodeList2 = xmlDocument.DocumentElement.GetElementsByTagName("elEm2");
            Assert.Equal(1, xmlNodeList2.Count);
            Assert.Same(xmlDocument.DocumentElement.ChildNodes.Item(1), xmlNodeList2.Item(0));
        }

        [Fact]
        public static void GetElementWhenBelongsToNamespace()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root xmlns:ns='test'><ns:elem1 /> <elem2 /> <elem3 /> <elem2 attr='test' /> </root>");

            var xmlNodeList = xmlDocument.DocumentElement.GetElementsByTagName("ns:elem1");

            Assert.Equal(1, xmlNodeList.Count);
            Assert.Same(xmlDocument.DocumentElement.ChildNodes.Item(0), xmlNodeList.Item(0));
        }

        [Fact]
        public static void CreateElementAndFind()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root />");
            var newElement = xmlDocument.CreateElement("test");

            xmlDocument.DocumentElement.AppendChild(newElement);

            var xmlNodeList = xmlDocument.DocumentElement.GetElementsByTagName("test");

            Assert.Equal(1, xmlNodeList.Count);
            Assert.Same(newElement, xmlNodeList.Item(0));
        }

        [Fact]
        public static void GetElementsWhenNoneExist()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><elem1 /> <elem2 /> <elem3 /> <elem2 attr='test' /> </root>");

            var xmlNodeList = xmlDocument.DocumentElement.GetElementsByTagName("test");

            Assert.Equal(0, xmlNodeList.Count);
        }

        [Fact]
        public static void RemoveOneElementFrom2SiblingElementsWithSameName()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc> <elem1 attr1=\"value1\">This is a test</elem1><elem1 attr2=\"value2\">this is also a test</elem1> text after </doc>");

            xmlDocument.DocumentElement.RemoveChild(xmlDocument.DocumentElement.FirstChild);

            Assert.Equal(1, xmlDocument.DocumentElement.GetElementsByTagName("elem1").Count);
        }

        [Fact]
        public static void GetByFirstAttributeNameOfElement()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc> <elem1 attr1=\"value1\">This is a test</elem1><elem1 attr2=\"value2\">this is also a test</elem1> text after </doc>");

            Assert.Equal(0, xmlDocument.DocumentElement.GetElementsByTagName("attr1").Count);
        }

        [Fact]
        public static void GetByTagnameEqualsAsterisk()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc> <elem1 attr1=\"value1\">This is a test</elem1><elem1 attr2=\"value2\">this is also a test</elem1> text after </doc>");
            var elementList = xmlDocument.DocumentElement.GetElementsByTagName("*");

            Assert.Equal(2, elementList.Count);
            Assert.Same(xmlDocument.DocumentElement.ChildNodes.Item(0), elementList.Item(0));
            Assert.Same(xmlDocument.DocumentElement.ChildNodes.Item(1), elementList.Item(1));
        }

        [Fact]
        public static void GetByTagnameEqualsRootNodeName()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc> <elem1 attr1=\"value1\">This is a test</elem1><elem1 attr2=\"value2\">this is also a test</elem1> text after </doc>");
            var elementList = xmlDocument.DocumentElement.GetElementsByTagName("doc");

            Assert.Equal(0, elementList.Count);
        }
    }
}
