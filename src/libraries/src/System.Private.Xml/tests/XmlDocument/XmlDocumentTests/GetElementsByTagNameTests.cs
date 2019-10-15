// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class GetElementsByTagNameTests
    {
        [Fact]
        public static void WrongLocalName()
        {
            var xml = "<root><elem1 xmlns=\"ns1\" xmlns:bb=\"ns2\" xmlns:cc=\"ns3\" bb:att1=\"foo\" attr=\"some\" cc:att2=\"bar\"></elem1><elem2 /></root>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            var xmlNodeList = xmlDocument.GetElementsByTagName("elem1", "ns2");

            Assert.Equal(0, xmlNodeList.Count);
        }

        [Fact]
        public static void LoadXmlTest1()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc><a /><a><b><c><b /></c></b><c /><c><a></a></c></a></doc>");

            var xmlNodeList = xmlDocument.GetElementsByTagName("a");

            Assert.Equal(3, xmlNodeList.Count);
        }

        [Fact]
        public static void LoadXmlTest2()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<Root><Level><Level><Level><Level><Level></Level></Level></Level></Level></Level></Root>");

            var xmlNodeList = xmlDocument.GetElementsByTagName("Level");

            Assert.Equal(5, xmlNodeList.Count);
        }

        [Fact]
        public static void MissingName()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<Root><Level><Level><Level><Level><Level></Level></Level></Level></Level></Level></Root>");

            var xmlNodeList = xmlDocument.GetElementsByTagName("bla");

            Assert.Equal(0, xmlNodeList.Count);
            Assert.Null(xmlNodeList.Item(0));
            Assert.Null(xmlNodeList[0]);
        }

        [Fact]
        public static void EmptyDocument()
        {
            var xmlDocument = new XmlDocument();
            var xmlNodeList = xmlDocument.GetElementsByTagName("a");

            Assert.Equal(0, xmlNodeList.Count);
            Assert.Null(xmlNodeList.Item(0));
            Assert.Null(xmlNodeList[0]);
        }

        [Fact]
        public static void NameIsQualified()
        {
            Verify("xs:sequence", 1);
            Verify("xs:element", 4);
            Verify("N:ID", 4);
            Verify("N:products", 1);
            Verify("root", 1);
            Verify("*", 23);
        }

        [Fact]
        public static void SanityCheck()
        {
            Verify("products", 1, "MyNamespace");
            Verify("sequence", 1, @"http://www.w3.org/2001/XMLSchema");
            Verify("element", 4, @"http://www.w3.org/2001/XMLSchema");
            Verify("ID", 4, "MyNamespace");
            Verify("*", 13, "MyNamespace");
        }

        [Fact]
        public static void MissingElement()
        {
            Verify("Products", 0, "MyNamespace");
        }

        [Fact]
        public static void Attribute()
        {
            Verify("IsDataSet", 0, @"urn:schemas-microsoft-com:xml-msdata");
        }

        [Fact]
        public static void NullReference()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root />");

            Assert.Throws<ArgumentNullException>(() => xmlDocument.GetElementsByTagName(null, "MyNamespace"));
            Assert.Throws<ArgumentNullException>(() => xmlDocument.GetElementsByTagName("product", null));
            Assert.Throws<ArgumentNullException>(() => xmlDocument.GetElementsByTagName(null, null));
        }

        #region Helper verify methods

        private const string xml = "<?xml version='1.0'?><root><xs:schema targetNamespace='MyNamespace' elementFormDefault='qualified' attributeFormDefault='unqualified' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'><xs:element name='products' msdata:IsDataSet='true' msdata:UseCurrentLocale='true'><xs:complexType><xs:choice minOccurs='0' maxOccurs='unbounded'><xs:element name='product'><xs:complexType><xs:sequence><xs:element name='ID' type='xs:int' minOccurs='0' maxOccurs='2' /><xs:element name='name' type='xs:string' minOccurs='1' /></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><N:products xmlns:N='MyNamespace'><N:product><N:ID>1</N:ID><N:name>A</N:name></N:product><N:product><N:ID>1</N:ID><N:name>A</N:name></N:product><N:product><N:ID>1</N:ID><N:ID>1</N:ID><N:name>A</N:name></N:product><N:product><N:name>A</N:name></N:product></N:products></root>";

        private static void Verify(string localName, int count, string namespaceUri)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            var nodeList = xmlDocument.GetElementsByTagName(localName, namespaceUri);

            Assert.NotNull(nodeList);
            Assert.Equal(count, nodeList.Count);

            for (int i = 0; i < count; i++)
            {
                var n = nodeList[i];

                Assert.Equal(XmlNodeType.Element, n.NodeType);

                if (!localName.Equals("*"))
                    Assert.Equal(localName, n.LocalName);

                Assert.Equal(namespaceUri, n.NamespaceURI);
                Assert.Same(n, nodeList.Item(i));
                Assert.Same(n, nodeList[i]);
            }
        }

        private static void Verify(string name, int count)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            XmlNodeList nodeList = xmlDocument.GetElementsByTagName(name);

            Assert.NotNull(nodeList);
            Assert.Equal(count, nodeList.Count);

            for (int i = 0; i < count; i++)
            {
                var n = nodeList[i];

                Assert.Equal(XmlNodeType.Element, n.NodeType);

                if (!name.Equals("*"))
                    Assert.Equal(name, n.Name);

                Assert.Same(n, nodeList.Item(i));
                Assert.Same(n, nodeList[i]);
            }
        }
        #endregion
    }
}
