// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class GetNamedItemTests
    {
        [Fact]
        public static void NormalWork()
        {
            var xml = "<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            var nodeMap = xmlDocument.DocumentElement.Attributes;
            var node = nodeMap.GetNamedItem("att2", "ns3");

            Assert.NotNull(node);
            Assert.Equal("bar", node.Value);
        }

        [Fact]
        public static void ExistingNameWrongNamespace()
        {
            var xml = "<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            var nodeMap = xmlDocument.DocumentElement.Attributes;
            var node = nodeMap.GetNamedItem("att2", "ns6");

            Assert.Null(node);
        }

        [Fact]
        public static void WrongNameExistingNamespace()
        {
            var xml = "<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            var nodeMap = xmlDocument.DocumentElement.Attributes;
            var node = nodeMap.GetNamedItem("atte", "ns3");

            Assert.Null(node);
        }

        [Fact]
        public static void WrongNameWrongNamespace()
        {
            var xml = "<elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            var nodeMap = xmlDocument.DocumentElement.Attributes;
            var node = nodeMap.GetNamedItem("atte", "nsa");

            Assert.Null(node);
        }
    }
}
