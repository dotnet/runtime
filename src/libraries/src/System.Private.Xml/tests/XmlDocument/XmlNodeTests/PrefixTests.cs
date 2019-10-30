// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public class PrefixTests
    {
        [Fact]
        public static void EmptyWork()
        {
            var xmlDocument = new XmlDocument();
            var xml = "<root>\r\n  text node one\r\n  <elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\">\r\n     text node two e1; text node three\r\n  </elem1><!-- comment3 --><?PI3 processing instruction?>e2;<foo /><![CDATA[ <opentag> without an </endtag> and & <! are all ok here ]]><elem2 att1=\"id1\" att2=\"up\" att3=\"attribute3\"><a /></elem2><elem2> \r\n      elem2-text1\r\n      <a> \r\n          this-is-a    \r\n      </a> \r\n\r\n      elem2-text2\r\n      e3;e4;<!-- elem2-comment1-->\r\n      elem2-text3\r\n\r\n      <b> \r\n          this-is-b\r\n      </b>\r\n\r\n      elem2-text4\r\n      <?elem2_PI elem2-PI?>\r\n      elem2-text5\r\n\r\n  </elem2></root>";

            xmlDocument.LoadXml(xml);

            foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                Assert.Equal(string.Empty, node.Prefix);

            Assert.Equal(string.Empty, xmlDocument.CreateDocumentFragment().Prefix);
        }

        [Fact]
        public static void SetForAllNodes()
        {
            var xmlDocument = new XmlDocument();
            var xml = "<root>\r\n  text node one\r\n  <elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\">\r\n     text node two e1; text node three\r\n  </elem1><!-- comment3 --><?PI3 processing instruction?>e2;<foo /><![CDATA[ <opentag> without an </endtag> and & <! are all ok here ]]><elem2 att1=\"id1\" att2=\"up\" att3=\"attribute3\"><a /></elem2><elem2> \r\n      elem2-text1\r\n      <a> \r\n          this-is-a    \r\n      </a> \r\n\r\n      elem2-text2\r\n      e3;e4;<!-- elem2-comment1-->\r\n      elem2-text3\r\n\r\n      <b> \r\n          this-is-b\r\n      </b>\r\n\r\n      elem2-text4\r\n      <?elem2_PI elem2-PI?>\r\n      elem2-text5\r\n\r\n  </elem2></root>";

            xmlDocument.LoadXml(xml);

            int i = 0;
            foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
            {
                switch (node.NodeType)
                {
                    case XmlNodeType.Text:
                    case XmlNodeType.Comment:
                    case XmlNodeType.EntityReference:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.ProcessingInstruction:
                        node.Prefix = "Prefix" + i++;
                        Assert.Equal(string.Empty, node.Prefix);
                        break;
                    default:
                        var newPrefix = "Prefix" + i++;
                        node.Prefix = newPrefix;
                        Assert.Equal(newPrefix, node.Prefix);
                        break;
                }
            }
        }

        [Fact]
        public static void ChangeQualifiedName()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<root><elem /></root>");
            var elem = xmlDocument.DocumentElement.FirstChild;

            Assert.Equal("elem", elem.Name);
            elem.Prefix = "foo";
            Assert.Equal("foo:elem", elem.Name);
        }

        [Fact]
        public static void LoadPrefix()
        {
            var xml = "<Root xmlns:aa=\"aa\" xmlns:bb=\"bb\"><aa:elem1><bb:elem2 /></aa:elem1></Root>";
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);

            Assert.Equal("aa", xmlDocument.DocumentElement.FirstChild.Prefix);
            Assert.Equal("bb", xmlDocument.DocumentElement.FirstChild.FirstChild.Prefix);
        }
    }
}
