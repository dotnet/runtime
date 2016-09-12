// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class LocalNameTests
    {
        [Fact]
        public static void AllNodesForEmptyString()
        {
            var xml = "<root>\r\n  text node one\r\n  <elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\">\r\n     text node two e1; text node three\r\n  </elem1><!-- comment3 --><?PI3 processing instruction?>e2;<foo /><![CDATA[ <opentag> without an </endtag> and & <! are all ok here ]]><elem2 att1=\"id1\" att2=\"up\" att3=\"attribute3\"><a /></elem2><elem2> \r\n      elem2-text1\r\n      <a> \r\n          this-is-a    \r\n      </a> \r\n\r\n      elem2-text2\r\n      e3;e4;<!-- elem2-comment1-->\r\n      elem2-text3\r\n\r\n      <b> \r\n          this-is-b\r\n      </b>\r\n\r\n      elem2-text4\r\n      <?elem2_PI elem2-PI?>\r\n      elem2-text5\r\n\r\n  </elem2></root>";
            var xmlDocument = new XmlDocument();

            xmlDocument.LoadXml(xml);

            foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                Assert.Equal(node.Name, node.LocalName);

            var documentFragment = xmlDocument.CreateDocumentFragment();
            Assert.Equal(documentFragment.Name, documentFragment.LocalName);
        }

        [Fact]
        public static void ElementWithPrefix()
        {
            var xmlDocument = new XmlDocument();
            var elem = xmlDocument.CreateElement("elem");

            elem.Prefix = "foo";

            Assert.Equal("elem", elem.LocalName);
        }

        [Fact]
        public static void ElementWithNamespaces()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<Root xmlns:aa=\"aa\" xmlns:bb=\"bb\"><aa:elem1><bb:elem2 /></aa:elem1></Root>");

            Assert.Equal(xmlDocument.DocumentElement.FirstChild.LocalName, "elem1");
            Assert.Equal(xmlDocument.DocumentElement.FirstChild.FirstChild.LocalName, "elem2");
        }
    }
}
