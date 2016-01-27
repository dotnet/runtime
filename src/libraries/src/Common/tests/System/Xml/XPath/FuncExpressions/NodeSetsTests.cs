// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Expressions
{
    /// <summary>
    /// Expressions - Node Sets
    /// </summary>
    public static partial class NodeSetsTests
    {
        /// <summary>
        /// Expected: Selects all paraA and paraB element children of the context node.
        /// child::paraA | child::paraB
        /// </summary>
        [Fact]
        public static void NodeSetsTest181()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"child::Title | child::Chap";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value =
                        "\n   XPath test\n   First paragraph  Nested  Paragraph  End of first paragraph \n   Second paragraph \n "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value = "\n   XPath test\n   Direct content\n "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all paraA, paraB and paraC element children of the context node.
        /// child::paraA | child::paraB | child::paraC
        /// </summary>
        [Fact]
        public static void NodeSetsTest182()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"child::Title | child::Chap | child::Summary";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Summary",
                    Name = "Summary",
                    HasNameTable = true,
                    Value = "This shall test XPath test"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value =
                        "\n   XPath test\n   First paragraph  Nested  Paragraph  End of first paragraph \n   Second paragraph \n "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value = "\n   XPath test\n   Direct content\n "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all para element children and the parent of the context node.
        /// self::para | child::para
        /// </summary>
        [Fact]
        public static void NodeSetsTest183()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"self::Doc | child::Chap";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Doc",
                    Name = "Doc",
                    HasNameTable = true,
                    Value =
                        "\n XPath test\n This shall test XPath test\n \n   XPath test\n   First paragraph  Nested  Paragraph  End of first paragraph \n   Second paragraph \n \n \n   XPath test\n   Direct content\n \n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value =
                        "\n   XPath test\n   First paragraph  Nested  Paragraph  End of first paragraph \n   Second paragraph \n "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value = "\n   XPath test\n   Direct content\n "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all paraB element descendants of the paraA element children of the context node.
        /// paraA//paraB
        /// </summary>
        [Fact]
        public static void NodeSetsTest184()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"Chap//Para";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "First paragraph  Nested  Paragraph  End of first paragraph "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = " Nested  Paragraph "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "Second paragraph "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all paraB element descendants of the paraA element children in the document.
        /// //paraA//paraB
        /// </summary>
        [Fact]
        public static void NodeSetsTest185()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"//Doc//Para";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "First paragraph  Nested  Paragraph  End of first paragraph "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = " Nested  Paragraph "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "Second paragraph "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all paraB element nodes with paraA parents that are children of the context node.
        /// paraA/paraB
        /// </summary>
        [Fact]
        public static void NodeSetsTest186()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"Chap/Para";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "First paragraph  Nested  Paragraph  End of first paragraph "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "Second paragraph "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all paraB element nodes with paraA parents that are children of the document root.
        /// /paraA/paraB
        /// </summary>
        [Fact]
        public static void NodeSetsTest187()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"/Doc/Chap";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value =
                        "\n   XPath test\n   First paragraph  Nested  Paragraph  End of first paragraph \n   Second paragraph \n "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value = "\n   XPath test\n   Direct content\n "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all bar element nodes with para/bar children.
        /// para/bar[para/bar] (Use location path as expression against context)
        /// </summary>
        [Fact]
        public static void NodeSetsTest188()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"Chap/Para[Para/Origin]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "First paragraph  Nested  Paragraph  End of first paragraph "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }
    }
}
