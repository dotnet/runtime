// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class RemoveNamedItemTests
    {
        [Fact]
        public static void NamedItemDoesNotExist()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two text node three </elem1>");

            var namedNodeMap = (XmlNamedNodeMap)xmlDocument.FirstChild.Attributes;
            Assert.Equal(5, namedNodeMap.Count);

            var removedItem = namedNodeMap.RemoveNamedItem("foo");

            Assert.Equal(5, namedNodeMap.Count);
            Assert.Null(removedItem);
        }

        [Fact]
        public static void RemoveFirstItem()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two text node three </elem1>");

            var namedNodeMap = (XmlNamedNodeMap)xmlDocument.FirstChild.Attributes;
            var item = namedNodeMap.Item(0);

            Assert.Equal(5, namedNodeMap.Count);

            var removedItem = namedNodeMap.RemoveNamedItem("child1");

            Assert.Equal(4, namedNodeMap.Count);
            Assert.Equal(item, removedItem);
        }

        [Fact]
        public static void RemoveMiddleItem()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two text node three </elem1>");

            var namedNodeMap = (XmlNamedNodeMap)xmlDocument.FirstChild.Attributes;
            var item = namedNodeMap.Item(1);

            Assert.Equal(5, namedNodeMap.Count);

            var removedItem = namedNodeMap.RemoveNamedItem("child2");

            Assert.Equal(4, namedNodeMap.Count);
            Assert.Equal(item, removedItem);
        }

        [Fact]
        public static void RemoveLastItem()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<elem1 child1='' child2='duu' child3='e1;e2;' child4='a1' child5='goody'> text node two text node three </elem1>");

            var namedNodeMap = (XmlNamedNodeMap)xmlDocument.FirstChild.Attributes;
            var item = namedNodeMap.Item(4);

            Assert.Equal(5, namedNodeMap.Count);

            var removedItem = namedNodeMap.RemoveNamedItem("child5");

            Assert.Equal(4, namedNodeMap.Count);
            Assert.Equal(item, removedItem);
        }

        [Fact]
        public static void ExistingNameWrongNamespace()
        {
            var xml = "<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            var nodeMap = xmlDocument.DocumentElement.Attributes;
            var count = nodeMap.Count;
            var node = nodeMap.RemoveNamedItem("att2", "ns6");

            Assert.Null(node);
            Assert.Equal(count, nodeMap.Count);
        }

        [Fact]
        public static void WrongNameExistingNamespace()
        {
            var xml = "<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            var nodeMap = xmlDocument.DocumentElement.Attributes;
            var count = nodeMap.Count;
            var node = nodeMap.GetNamedItem("atte", "ns3");

            Assert.Null(node);
            Assert.Equal(count, nodeMap.Count);
        }

        [Fact]
        public static void WrongNameWrongNamespace()
        {
            var xml = "<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            var nodeMap = xmlDocument.DocumentElement.Attributes;
            var count = nodeMap.Count;
            var node = nodeMap.GetNamedItem("atte", "nsa");

            Assert.Null(node);
            Assert.Equal(count, nodeMap.Count);
        }
    }
}
