// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class TwoTextNodeTests
    {
        private static readonly InsertType[] s_InsertTypes = new[] { InsertType.Prepend, InsertType.Append, InsertType.InsertBefore };
        private static readonly XmlNodeType[] s_XmlNodeTypes = new XmlNodeType[] { XmlNodeType.Whitespace, XmlNodeType.SignificantWhitespace, XmlNodeType.CDATA, XmlNodeType.Element /*, XmlNodeType.EntityReference*/ };

        private static void TwoTextNodeBase(XmlDocument xmlDocument, InsertType insertType, XmlNodeType nodeType)
        {
            XmlNode parent = xmlDocument.DocumentElement;
            XmlNode refChild = (insertType == InsertType.Prepend) ? parent.FirstChild : parent.LastChild;
            XmlNode newChild = TestHelper.CreateNode(xmlDocument, nodeType);

            string original = parent.InnerXml;
            string expected = (insertType == InsertType.Prepend) ? (newChild.OuterXml + parent.InnerXml)
                : ((insertType == InsertType.Append) ? (parent.InnerXml + newChild.OuterXml)
                : (refChild.PreviousSibling.OuterXml + newChild.OuterXml + refChild.OuterXml));

            // insert new child
            var insertDelegate = TestHelper.CreateInsertBeforeOrAfter(insertType);
            insertDelegate(parent, newChild, refChild);

            // verify
            Assert.Equal(3, parent.ChildNodes.Count);
            Assert.Equal(expected, parent.InnerXml);

            TestHelper.Verify(parent, refChild, newChild);
            TestHelper.VerifySiblings(refChild, newChild, insertType);

            if (insertType == InsertType.Prepend || insertType == InsertType.Append)
                TestHelper.Verify(refChild, newChild, insertType);

            // delete new child
            parent.RemoveChild(newChild);
            Assert.Equal(2, parent.ChildNodes.Count);
            TestHelper.VerifySiblings(parent.FirstChild, parent.LastChild, InsertType.Append);
            Assert.Equal(original, parent.InnerXml);
        }

        private static void TwoTextNodeBase(string xml, InsertType insertType, XmlNodeType nodeType)
        {
            XmlDocument xmlDocument = new XmlDocument { PreserveWhitespace = true };
            xmlDocument.LoadXml(xml);
            TwoTextNodeBase(xmlDocument, insertType, nodeType);
        }

        private static void TwoTextNodeBase(XmlNodeType[] nodeTypes, InsertType insertType, XmlNodeType nodeType)
        {
            XmlDocument xmlDocument = new XmlDocument { PreserveWhitespace = true };
            var elem = xmlDocument.CreateElement("elem");

            xmlDocument.AppendChild(elem);

            for (int i = 0; i < nodeTypes.Length; i++)
                elem.AppendChild(TestHelper.CreateNode(xmlDocument, nodeTypes[i]));

            TwoTextNodeBase(xmlDocument, insertType, nodeType);
        }

        [Fact]
        public static void Whitespace_CDATA()
        {
            var xml = @" <WC> 	<![CDATA[ &lt; &amp; <tag> < ! > & </tag> 	 ]]></WC>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    TwoTextNodeBase(xml, insertType, nodeType);
        }

        [Fact]
        public static void CDATA_Text()
        {
            var xml = @"<CT><![CDATA[ &lt; &amp; <tag> < ! > & </tag> 	 ]]>text</CT>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    TwoTextNodeBase(xml, insertType, nodeType);
        }

        [Fact]
        public static void Whitespace_Whitespace()
        {
            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    TwoTextNodeBase(new XmlNodeType[] { XmlNodeType.Whitespace, XmlNodeType.Whitespace }, insertType, nodeType);
        }

        [Fact]
        public static void SignificantWhitespace_SignificantWhitespace()
        {
            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    TwoTextNodeBase(new XmlNodeType[] { XmlNodeType.SignificantWhitespace, XmlNodeType.SignificantWhitespace }, insertType, nodeType);
        }

        [Fact]
        public static void Text_Text()
        {
            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    TwoTextNodeBase(new XmlNodeType[] { XmlNodeType.Text, XmlNodeType.Text }, insertType, nodeType);
        }

        [Fact]
        public static void Text_SignificantWhitespace()
        {
            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    TwoTextNodeBase(new XmlNodeType[] { XmlNodeType.Text, XmlNodeType.SignificantWhitespace }, insertType, nodeType);
        }
    }
}
