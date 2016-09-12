// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xml.Tests
{
    public static class ThreeElementTests
    {
        private static readonly XmlNodeType[] s_XmlNodeTypes = new[] { XmlNodeType.Whitespace, XmlNodeType.SignificantWhitespace, XmlNodeType.CDATA, XmlNodeType.Text };
        private static readonly InsertType[] s_InsertTypes = new[] { InsertType.InsertBefore, InsertType.InsertAfter };

        private static void InsertTestBase(string xml, InsertType insertType, XmlNodeType nodeType)
        {
            string[] expected = new string[2];

            var xmlDocument = new XmlDocument { PreserveWhitespace = true };
            xmlDocument.LoadXml(xml);

            XmlNode parent = xmlDocument.DocumentElement;
            XmlNode firstChild = parent.FirstChild;
            XmlNode lastChild = parent.LastChild;
            XmlNode refChild = parent.FirstChild.NextSibling;
            XmlNode newChild = TestHelper.CreateNode(xmlDocument, nodeType);

            expected[0] = parent.InnerXml;
            expected[1] = (insertType == InsertType.InsertBefore)
                ? (firstChild.OuterXml + newChild.OuterXml + refChild.OuterXml + lastChild.OuterXml)
                : (firstChild.OuterXml + refChild.OuterXml + newChild.OuterXml + lastChild.OuterXml);

            // insertion
            var insertDelegate = TestHelper.CreateInsertBeforeOrAfter(insertType);
            insertDelegate(parent, newChild, refChild);

            // verify
            Assert.Equal(4, parent.ChildNodes.Count);
            Assert.Equal(expected[1], parent.InnerXml);

            TestHelper.Verify(parent, refChild, newChild);
            TestHelper.Verify(parent, firstChild, lastChild);
            TestHelper.VerifySiblings(refChild, newChild, insertType);

            if (insertType == InsertType.InsertBefore)
            {
                TestHelper.VerifySiblings(firstChild, newChild, InsertType.Append);
                TestHelper.VerifySiblings(newChild, refChild, InsertType.Append);
                TestHelper.VerifySiblings(refChild, lastChild, InsertType.Append);
            }
            else
            {
                TestHelper.VerifySiblings(firstChild, refChild, InsertType.Append);
                TestHelper.VerifySiblings(refChild, newChild, InsertType.Append);
                TestHelper.VerifySiblings(newChild, lastChild, InsertType.Append);
            }

            // delete the newChild
            parent.RemoveChild(newChild);

            // verify
            Assert.Equal(3, parent.ChildNodes.Count);
            Assert.Equal(expected[0], parent.InnerXml);

            TestHelper.Verify(parent, firstChild, lastChild);
            TestHelper.VerifySiblings(firstChild, refChild, InsertType.Append);
            TestHelper.VerifySiblings(refChild, lastChild, InsertType.Append);
        }

        [Fact]
        public static void Text_Comment_CDATA()
        {
            var xml = @"<TMC>text<!-- comments --><![CDATA[ &lt; &amp; <tag> < ! > & </tag> 	 ]]></TMC>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    InsertTestBase(xml, insertType, nodeType);
        }

        [Fact]
        public static void Text_Comment_SignificantWhitespace()
        {
            var xml = @"<TCS xml:space=""preserve"">text<!-- comments -->   	</TCS>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    InsertTestBase(xml, insertType, nodeType);
        }

        [Fact]
        public static void Whitespace_Comment_Text()
        {
            var xml = @"<WMT>
            <!-- comments -->text</WMT>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    InsertTestBase(xml, insertType, nodeType);
        }

        [Fact]
        public static void Whitespace_Element_Whitespace()
        {
            var xml = @"<WEW>
            <E/>
        </WEW>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    InsertTestBase(xml, insertType, nodeType);
        }

        [Fact]
        public static void Text_Element_Text()
        {
            var xml = @"<TET>text1<E/>text2</TET>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    InsertTestBase(xml, insertType, nodeType);
        }

        [Fact]
        public static void SignificantWhitespace_Element_SignificantWhitespace()
        {
            var xml = @"<SES xml:space=""preserve""> 	<E/>		</SES>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    InsertTestBase(xml, insertType, nodeType);
        }

        [Fact]
        public static void CDATA_Element_CDATA()
        {
            var xml = @"<CEC><![CDATA[ &lt; &amp; <tag> < ! > & </tag> 	 ]]><E/><![CDATA[ &lt; &amp; <tag> < ! > & </tag> 	 ]]></CEC>";

            foreach (var insertType in s_InsertTypes)
                foreach (var nodeType in s_XmlNodeTypes)
                    InsertTestBase(xml, insertType, nodeType);
        }
    }
}
