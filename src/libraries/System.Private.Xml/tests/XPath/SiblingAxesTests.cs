// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit;

namespace XPathTests
{
    public static class SiblingAxesTests
    {
        private const string NestedXml =
            "<r><a x='1'><b/><a><b/><a/><b/><c><a/><b/></c></a><b/><a/></a><b/><c><a/><b/></c><a><b/></a><b/>t<a/></r>";

        public static IEnumerable<object[]> Navigators()
        {
            yield return new object[] { "XPathDocument" };
            yield return new object[] { "XmlDocument" };
            yield return new object[] { "XDocument" };
        }

        private static XPathNavigator CreateNavigator(string kind, string xml) => kind switch
        {
            "XPathDocument" => new XPathDocument(new StringReader(xml)).CreateNavigator(),
            "XmlDocument" => CreateXmlDocumentNavigator(xml),
            _ => XDocument.Parse(xml).CreateNavigator(),
        };

        private static XPathNavigator CreateXmlDocumentNavigator(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.CreateNavigator();
        }

        private static List<XPathNavigator> Select(XPathNavigator nav, string xpath)
        {
            var result = new List<XPathNavigator>();
            XPathNodeIterator iterator = nav.Select(xpath);
            while (iterator.MoveNext())
            {
                result.Add(iterator.Current.Clone());
            }
            return result;
        }

        private static void AssertSameNodes(XPathNavigator nav, string xpath, string equivalentXPath)
        {
            List<XPathNavigator> actual = Select(nav, xpath);
            List<XPathNavigator> expected = Select(nav, equivalentXPath);

            Assert.Equal(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.True(expected[i].IsSamePosition(actual[i]), $"{xpath}: node {i} differs from {equivalentXPath}");
            }
        }

        [Theory]
        [MemberData(nameof(Navigators))]
        public static void SiblingAxesOverNestedInput(string kind)
        {
            XPathNavigator nav = CreateNavigator(kind, NestedXml);

            AssertSameNodes(nav, "//a/following-sibling::*", "//*[preceding-sibling::a]");
            AssertSameNodes(nav, "//a/following-sibling::node()", "//node()[preceding-sibling::a]");
            AssertSameNodes(nav, "//a/following-sibling::b", "//b[preceding-sibling::a]");
            AssertSameNodes(nav, "//*/following-sibling::a", "//a[preceding-sibling::*]");
            AssertSameNodes(nav, "//a/preceding-sibling::*", "//*[following-sibling::a]");
            AssertSameNodes(nav, "//a/preceding-sibling::node()", "//node()[following-sibling::a]");
            AssertSameNodes(nav, "//a/preceding-sibling::b", "//b[following-sibling::a]");
            AssertSameNodes(nav, "//*/preceding-sibling::a", "//a[following-sibling::*]");
            AssertSameNodes(nav, "(//a | //@x)/following-sibling::*", "//*[preceding-sibling::a]");
            AssertSameNodes(nav, "//b/../following-sibling::*", "//*[preceding-sibling::*[b]]");
            AssertSameNodes(nav, "//b/../preceding-sibling::*", "//*[following-sibling::*[b]]");
        }

        [Theory]
        [MemberData(nameof(Navigators))]
        public static void SiblingAxesOverManyParents(string kind)
        {
            const int Rows = 2000;
            var xml = new StringBuilder("<root>");
            for (int i = 0; i < Rows; i++)
            {
                xml.Append("<row><c/><c/><c/></row>");
            }
            xml.Append("</root>");
            XPathNavigator nav = CreateNavigator(kind, xml.ToString());

            Assert.Equal(2.0 * Rows, nav.Evaluate("count(/root/row/c[1]/following-sibling::c)"));
            Assert.Equal(2.0 * Rows, nav.Evaluate("count(/root/row/c[3]/preceding-sibling::c)"));
            Assert.Equal(2.0 * Rows, nav.Evaluate("count(/root/row/c/following-sibling::c)"));
            Assert.Equal(2.0 * Rows, nav.Evaluate("count(/root/row/c/preceding-sibling::c)"));
            Assert.Equal(Rows - 1.0, nav.Evaluate("count(/root/row/following-sibling::row)"));
            AssertSameNodes(nav, "/root/row/c[2]/following-sibling::c", "/root/row/c[3]");
            AssertSameNodes(nav, "/root/row/c[2]/preceding-sibling::c", "/root/row/c[1]");
        }
    }
}
