// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.Xml.Tests
{
    public class NormalizeTests
    {
        public static String StripWhiteSpace(String inString)
        {
            var returnString = new StringBuilder();

            foreach (var c in inString)
            {
                if (!Char.IsWhiteSpace(c))
                    returnString.Append(c);
            }

            return returnString.ToString();
        }

        [Fact]
        public static void EmptyWork()
        {
            var xml = "<root>\r\n  text node one\r\n  <elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\">\r\n     text node two e1; text node three\r\n  </elem1><!-- comment3 --><?PI3 processing instruction?>e2;<foo /><![CDATA[ <opentag> without an </endtag> and & <! are all ok here ]]><elem2 att1=\"id1\" att2=\"up\" att3=\"attribute3\"><a /></elem2><elem2> \r\n      elem2-text1\r\n      <a> \r\n          this-is-a    \r\n      </a> \r\n\r\n      elem2-text2\r\n      e3;e4;<!-- elem2-comment1-->\r\n      elem2-text3\r\n\r\n      <b> \r\n          this-is-b\r\n      </b>\r\n\r\n      elem2-text4\r\n      <?elem2_PI elem2-PI?>\r\n      elem2-text5\r\n\r\n  </elem2></root>";
            var xmlDocument = new XmlDocument();

            xmlDocument.LoadXml(xml);
            xmlDocument.Normalize();

            Assert.Equal(StripWhiteSpace(xml), StripWhiteSpace(xmlDocument.OuterXml));
        }

        [Fact]
        public static void NormalWork()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<doc><elem1>hello</elem1></doc>");

            var text = xmlDocument.CreateTextNode("Test_test");
            xmlDocument.DocumentElement.FirstChild.AppendChild(text);

            xmlDocument.Normalize();

            Assert.Equal(1, xmlDocument.DocumentElement.FirstChild.ChildNodes.Count);
        }

        [Fact]
        public static void EmptyDocument()
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.Normalize();
            Assert.Equal(String.Empty, xmlDocument.OuterXml);
        }

        [Fact]
        public static void EmptyDocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            var fragment = xmlDocument.CreateDocumentFragment();

            fragment.Normalize();
            Assert.Equal(String.Empty, fragment.OuterXml);
        }

        [Fact]
        public static void DocumentFragment()
        {
            var xmlDocument = new XmlDocument();
            var fragment = xmlDocument.CreateDocumentFragment();

            var text1 = xmlDocument.CreateTextNode("test_test1");
            var text2 = xmlDocument.CreateTextNode("test_test2");

            fragment.Normalize();
            Assert.Equal(String.Empty, fragment.OuterXml);
        }

        [Fact]
        public static void TwoCalls()
        {
            var xmlDocument = new XmlDocument();
            var xml = "<root>\r\n  text node one\r\n  <elem1 child1=\"\" child2=\"duu\" child3=\"e1;e2;\" child4=\"a1\" child5=\"goody\">\r\n     text node two e1; text node three\r\n  </elem1><!-- comment3 --><?PI3 processing instruction?>e2;<foo /><![CDATA[ <opentag> without an </endtag> and & <! are all ok here ]]><elem2 att1=\"id1\" att2=\"up\" att3=\"attribute3\"><a /></elem2><elem2> \r\n      elem2-text1\r\n      <a> \r\n          this-is-a    \r\n      </a> \r\n\r\n      elem2-text2\r\n      e3;e4;<!-- elem2-comment1-->\r\n      elem2-text3\r\n\r\n      <b> \r\n          this-is-b\r\n      </b>\r\n\r\n      elem2-text4\r\n      <?elem2_PI elem2-PI?>\r\n      elem2-text5\r\n\r\n  </elem2></root>";

            xmlDocument.LoadXml(xml);

            var text1 = xmlDocument.CreateTextNode("test_1");
            var text2 = xmlDocument.CreateTextNode("test_2");

            xmlDocument.DocumentElement.AppendChild(text1);
            xmlDocument.DocumentElement.ChildNodes[1].AppendChild(text2);

            var outerXml = xmlDocument.OuterXml;

            xmlDocument.Normalize();

            Assert.Equal(StripWhiteSpace(outerXml), StripWhiteSpace(xmlDocument.OuterXml));
        }
    }
}
