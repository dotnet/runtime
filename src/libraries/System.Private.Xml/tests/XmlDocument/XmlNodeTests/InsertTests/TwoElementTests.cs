// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class TwoElementTests
    {
        private static readonly InsertType[] s_InsertTypes = new[] { InsertType.Prepend, InsertType.Append, InsertType.InsertBefore };
        private static readonly XmlNodeType[] s_XmlNodeTypes = new[] { XmlNodeType.Whitespace, XmlNodeType.SignificantWhitespace, XmlNodeType.CDATA, XmlNodeType.Text, XmlNodeType.Comment };

        private static void OneTextNode_OneNonTextNodeBase(string xml, InsertType insertType, XmlNodeType nodeType, bool deleteFirst)
        {
            var xmlDocument = new XmlDocument { PreserveWhitespace = true };
            xmlDocument.LoadXml(xml);

            XmlNode parent = xmlDocument.DocumentElement;
            XmlNode refChild = (insertType == InsertType.Prepend) ? parent.FirstChild : parent.LastChild;
            XmlNode newChild = TestHelper.CreateNode(xmlDocument, nodeType);
            XmlNode nodeToRemove = (deleteFirst) ? parent.FirstChild : parent.LastChild;

            // populate the expected result, where expected[0] is the expected result after insertion, and expected[1] is the expected result after deletion
            string[] expected = new string[2];
            expected[0] = (insertType == InsertType.Prepend) ? (newChild.OuterXml + parent.InnerXml)
                : ((insertType == InsertType.Append) ? (parent.InnerXml + newChild.OuterXml)
                : (refChild.PreviousSibling.OuterXml + newChild.OuterXml + refChild.OuterXml));
            if (deleteFirst)
                expected[1] = (insertType == InsertType.Append) ? (parent.LastChild.OuterXml + newChild.OuterXml) : (newChild.OuterXml + parent.LastChild.OuterXml);
            else
                expected[1] = (insertType == InsertType.Prepend) ? (newChild.OuterXml + parent.FirstChild.OuterXml) : (parent.FirstChild.OuterXml + newChild.OuterXml);

            // insert new child
            var insertDelegate = TestHelper.CreateInsertBeforeOrAfter(insertType);
            insertDelegate(parent, newChild, refChild);

            // verify
            Assert.Equal(3, parent.ChildNodes.Count);
            Assert.Equal(expected[0], parent.InnerXml);

            TestHelper.Verify(parent, refChild, newChild);
            TestHelper.VerifySiblings(refChild, newChild, insertType);

            if (insertType == InsertType.Prepend || insertType == InsertType.Append)
                TestHelper.Verify(refChild, newChild, insertType);

            // delete new child
            parent.RemoveChild(nodeToRemove);
            Assert.Equal(2, parent.ChildNodes.Count);
            TestHelper.VerifySiblings(parent.FirstChild, parent.LastChild, InsertType.Append);
            Assert.Equal(expected[1], parent.InnerXml);
        }

        [Fact]
        public static void Comment_Text()
        {
            var xml = @"<MT><!-- comments -->text</MT>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    OneTextNode_OneNonTextNodeBase(xml, insertType, nodeType, true);
        }

        [Fact]
        public static void Comment_CDATA()
        {
            var xml = @"<MC><!-- comments --><![CDATA[ &lt; &amp; <tag> < ! > & </tag> 	 ]]></MC>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    OneTextNode_OneNonTextNodeBase(xml, insertType, nodeType, true);
        }

        [Fact]
        public static void Comment_SignificantWhitespace()
        {
            var xml = @"<MS xml:space=""preserve""><!-- comments -->  	</MS>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    OneTextNode_OneNonTextNodeBase(xml, insertType, nodeType, true);
        }

        [Fact]
        public static void Whitespace_Element()
        {
            var xml = @"<WE> 	<E/></WE>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    OneTextNode_OneNonTextNodeBase(xml, insertType, nodeType, false);
        }

        [Fact]
        public static void CDATA_Element()
        {
            var xml = @"<CE><![CDATA[ &lt; &amp; <tag> < ! > & </tag> 	 ]]><E/></CE>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    OneTextNode_OneNonTextNodeBase(xml, insertType, nodeType, false);
        }

        [Fact]
        public static void Text_Element()
        {
            var xml = "<TE>text<E/></TE>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    OneTextNode_OneNonTextNodeBase(xml, insertType, nodeType, false);
        }
    }
}
