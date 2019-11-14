// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Location.Paths
{
    /// <summary>
    /// Location Paths - Axes
    /// </summary>
    public static partial class AxesTests
    {
        /// <summary>
        /// Expected: Selects all element ancestors of the context node.
        /// ancestor::*
        /// </summary>
        [Fact]
        public static void AxesTest11()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para/Origin";
            var testExpression = @"ancestor::*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("book", "http://book.htm");
            namespaceManager.AddNamespace("movie", "http://movie.htm");
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all para element ancestors of the context node.
        /// ancestor::para
        /// </summary>
        [Fact]
        public static void AxesTest12()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para/Origin";
            var testExpression = @"ancestor::Para";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all element ancestors of the context node.
        /// ancestor::node()
        /// </summary>
        [Fact]
        public static void AxesTest13()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para/Origin";
            var testExpression = @"ancestor::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    HasChildren = true,
                    HasNameTable = true,
                    Value =
                        "\n XPath test\n This shall test XPath test\n \n   XPath test\n   First paragraph  Nested  Paragraph  End of first paragraph \n   Second paragraph \n \n \n   XPath test\n   Direct content\n \n"
                },
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (the ancestor axis can not be combined with other axes).
        /// ancestor::*/child::*
        /// </summary>
        [Fact]
        public static void AxesTest14()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"ancestor::*/child::*";
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
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                },
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
        /// Expected: Empty node list.
        /// ancestor (assuming context node is root node)
        /// </summary>
        [Fact]
        public static void AxesTest15()
        {
            var xml = "xp001.xml";
            var testExpression = @"ancestor::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Expected: Selects the element ancestors of the context node and the context node itself.
        /// ancestor-or-self::*
        /// </summary>
        [Fact]
        public static void AxesTest16()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para/Origin";
            var testExpression = @"ancestor-or-self::*";
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
                    IsEmptyElement = true,
                    LocalName = "Origin",
                    Name = "Origin",
                    HasNameTable = true
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the para element ancestors of the context node and the context node itself.
        /// ancestor-or-self::para (assuming context node is a para element)
        /// </summary>
        [Fact]
        public static void AxesTest17()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para";
            var testExpression = @"ancestor-or-self::Para";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the para element ancestors of the context node.
        /// ancestor-or-self::para (assuming context node is not a para element)
        /// </summary>
        [Fact]
        public static void AxesTest18()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para/Origin";
            var testExpression = @"ancestor-or-self::Para";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the element ancestors of the context node and the context node itself.
        /// ancestor-or-self::node()
        /// </summary>
        [Fact]
        public static void AxesTest19()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para/Origin";
            var testExpression = @"ancestor-or-self::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    HasChildren = true,
                    HasNameTable = true,
                    Value =
                        "\n XPath test\n This shall test XPath test\n \n   XPath test\n   First paragraph  Nested  Paragraph  End of first paragraph \n   Second paragraph \n \n \n   XPath test\n   Direct content\n \n"
                },
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
                    IsEmptyElement = true,
                    LocalName = "Origin",
                    Name = "Origin",
                    HasNameTable = true
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected error(the ancestor axis can not be combined with other axes).
        /// ancestor-or-self::*/child::*
        /// </summary>
        [Fact]
        public static void AxesTest110()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para/Origin";
            var testExpression = @"ancestor-or-self::*/child::*";
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
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                },
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
                    IsEmptyElement = true,
                    LocalName = "Origin",
                    Name = "Origin",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "Second paragraph "
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
        /// Expected: Selects all attributes of the context node.
        /// attribute::*
        /// </summary>
        [Fact]
        public static void AxesTest111()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para/Origin";
            var testExpression = @"attribute::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "Attr1",
                    Name = "Attr1",
                    HasNameTable = true,
                    Value = "value1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "Attr2",
                    Name = "Attr2",
                    HasNameTable = true,
                    Value = "value2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "Attr3",
                    Name = "Attr3",
                    HasNameTable = true,
                    Value = "value3"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all attributes of the context node.
        /// attribute::node()
        /// </summary>
        [Fact]
        public static void AxesTest112()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para/Origin";
            var testExpression = @"attribute::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "Attr1",
                    Name = "Attr1",
                    HasNameTable = true,
                    Value = "value1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "Attr2",
                    Name = "Attr2",
                    HasNameTable = true,
                    Value = "value2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "Attr3",
                    Name = "Attr3",
                    HasNameTable = true,
                    Value = "value3"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Empty node list.
        /// attribute::node() (assuming context node is an attribute node)
        /// </summary>
        [Fact]
        public static void AxesTest113()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para/Origin/@Attr1";
            var testExpression = @"attribute::node()";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the name attribute of the context node.
        /// attribute::name
        /// </summary>
        [Fact]
        public static void AxesTest114()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"attribute::name";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "name",
                    Name = "name",
                    HasNameTable = true,
                    Value = "This is the name"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all element children of the context node.
        /// child::*
        /// </summary>
        [Fact]
        public static void AxesTest115()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"child::*";
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
        /// Expected: Selects the para element children of the context node
        /// child::para
        /// </summary>
        [Fact]
        public static void AxesTest116()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"child::Para";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all children of the context node.
        /// child::node()
        /// </summary>
        [Fact]
        public static void AxesTest117()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"child::node()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Summary",
                    Name = "Summary",
                    HasNameTable = true,
                    Value = "This shall test XPath test"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
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
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value = "\n   XPath test\n   Direct content\n "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all element descendants of the context node.
        /// descendant::*
        /// </summary>
        [Fact]
        public static void AxesTest118()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"descendant::*";
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
                    IsEmptyElement = true,
                    LocalName = "Origin",
                    Name = "Origin",
                    HasNameTable = true
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
        /// Expected: Selects all descendants of the context node.
        /// descendant::node()
        /// </summary>
        [Fact]
        public static void AxesTest119()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"descendant::node()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "XPath test"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "First paragraph  Nested  Paragraph  End of first paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "First paragraph "},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = " Nested  Paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " Nested "},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    IsEmptyElement = true,
                    LocalName = "Origin",
                    Name = "Origin",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " Paragraph "},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = " End of first paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "Second paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Second paragraph "},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all para element descendants of the context node.
        /// descendant::para
        /// </summary>
        [Fact]
        public static void AxesTest120()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"descendant::Para";
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
        /// Expected: Selects the para element descendants of the context node and the context node itself.
        /// descendant-or-self::para  (assuming context node is a para element)
        /// </summary>
        [Fact]
        public static void AxesTest121()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"descendant-or-self::Para";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the para element descendants of the context node.
        /// descendant-or-self::para  (assuming context node is not a para element)
        /// </summary>
        [Fact]
        public static void AxesTest122()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"descendant-or-self::Para";
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
        /// Expected: Selects the element descendants of the context node and the context node itself.
        /// descendant-or-self::node()
        /// </summary>
        [Fact]
        public static void AxesTest123()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"descendant-or-self::node()";
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
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "XPath test"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "First paragraph  Nested  Paragraph  End of first paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "First paragraph "},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = " Nested  Paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " Nested "},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    IsEmptyElement = true,
                    LocalName = "Origin",
                    Name = "Origin",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " Paragraph "},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = " End of first paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "Second paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Second paragraph "},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// following::*
        /// </summary>
        [Fact]
        public static void AxesTest124()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"following::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value = "\n   XPath test\n   Direct content\n "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// following-sibling::*
        /// </summary>
        [Fact]
        public static void AxesTest125()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"following-sibling::*";
            var expected = new XPathResult(0,
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
        /// Expected: Selects parent node of the context node (if it is an element node).
        /// parent::*
        /// </summary>
        [Fact]
        public static void AxesTest126()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"parent::*";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Empty node list.
        /// parent::* (assuming context node is root node)
        /// </summary>
        [Fact]
        public static void AxesTest127()
        {
            var xml = "xp001.xml";
            var testExpression = @"parent::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Expected: Error.
        /// child::*/parent::*
        /// </summary>
        [Fact]
        public static void AxesTest128()
        {
            var xml = "xp001.xml";
            var testExpression = @"child::*/parent::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Expected: Selects parent node of the context node, if this node is a para element.
        /// parent::para
        /// </summary>
        [Fact]
        public static void AxesTest129()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para";
            var testExpression = @"parent::Para";
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

        /// <summary>
        /// Expected: Selects parent node of the context node.
        /// parent::node()
        /// </summary>
        [Fact]
        public static void AxesTest130()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"parent::node()";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding::*
        /// </summary>
        [Fact]
        public static void AxesTest131()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"preceding::*";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding-sibling::*
        /// </summary>
        [Fact]
        public static void AxesTest132()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"preceding-sibling::*";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the context node (if it is an element node).
        /// self::*
        /// </summary>
        [Fact]
        public static void AxesTest133()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"self::*";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the context node if the context node is a para element.
        /// self::para
        /// </summary>
        [Fact]
        public static void AxesTest134()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"self::Para";
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

        /// <summary>
        /// Expected: Selects the context node.
        /// self::node()
        /// </summary>
        [Fact]
        public static void AxesTest135()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"self::node()";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the document root.
        /// /
        /// </summary>
        [Fact]
        public static void AxesTest136()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"/";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    HasChildren = true,
                    HasNameTable = true,
                    Value =
                        "\n XPath test\n This shall test XPath test\n \n   XPath test\n   First paragraph  Nested  Paragraph  End of first paragraph \n   Second paragraph \n \n \n   XPath test\n   Direct content\n \n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all the para elements in the document.
        /// /descendant::para
        /// </summary>
        [Fact]
        public static void AxesTest137()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"/descendant::Para";
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
        /// context node is leaf node and has some nodes following it in the document order
        /// following::*
        /// </summary>
        [Fact]
        public static void AxesTest138()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"following::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value = "\n   XPath test\n   Direct content\n "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// following (last node in the doc order)
        /// following::*
        /// </summary>
        [Fact]
        public static void AxesTest139()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap[2]/Title/text()";
            var testExpression = @"following::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// following (attribute)
        /// following::*
        /// </summary>
        [Fact]
        public static void AxesTest140()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"following::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "First paragraph  End of first paragraph "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "Origin",
                    Name = "Origin",
                    HasNameTable = true
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
        /// BVT for namespace
        /// NS1: //namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest141()
        {
            var xml = "name.xml";
            var testExpression = @"//namespace::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSmovie",
                    Name = "NSmovie",
                    HasNameTable = true,
                    Value = "http://movie.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSmovie",
                    Name = "NSmovie",
                    HasNameTable = true,
                    Value = "http://movie.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSmovie",
                    Name = "NSmovie",
                    HasNameTable = true,
                    Value = "http://movie.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSmovie",
                    Name = "NSmovie",
                    HasNameTable = true,
                    Value = "http://documentry.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSmovie",
                    Name = "NSmovie",
                    HasNameTable = true,
                    Value = "http://documentry.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSmovie",
                    Name = "NSmovie",
                    HasNameTable = true,
                    Value = "http://documentry.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// NS2: namespace::*/following::*
        /// </summary>
        [Fact]
        public static void AxesTest142()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection";
            var testExpression = @"namespace::*/following::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "NSbook:book",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "\n\t\t\tA Brief History Of Time\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "NSbook:title",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "A Brief History Of Time"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "NSbook:book",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "\n\t\t\tThe Beautiful Universe\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "NSbook:title",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "The Beautiful Universe"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\t\tNewton's Time Machine\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Newton's Time Machine"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\t\tThe Quark And The Jaguar\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "The Quark And The Jaguar"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "moviesection",
                    Name = "moviesection",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\n\t\t\t\n\t\t\tVeritcal Limit\n\t\t\n\t\t\n\t\t\t\n\t\t\t\tJinnah\n\t\t\t\n\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "movie",
                    Name = "NSmovie:movie",
                    NamespaceURI = "http://movie.htm",
                    HasNameTable = true,
                    Prefix = "NSmovie",
                    Value = "\n\t\t\t\n\t\t\tVeritcal Limit\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "NSmovie:title",
                    NamespaceURI = "http://movie.htm",
                    HasNameTable = true,
                    Prefix = "NSmovie",
                    Value = "Veritcal Limit"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "documentry",
                    Name = "documentry",
                    HasNameTable = true,
                    Value = "\n\t\t\t\n\t\t\t\tJinnah\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "movie",
                    Name = "NSmovie:movie",
                    NamespaceURI = "http://documentry.htm",
                    HasNameTable = true,
                    Prefix = "NSmovie",
                    Value = "\n\t\t\t\tJinnah\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "NSmovie:title",
                    NamespaceURI = "http://documentry.htm",
                    HasNameTable = true,
                    Prefix = "NSmovie",
                    Value = "Jinnah"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// following (root node)
        /// following::*
        /// </summary>
        [Fact]
        public static void AxesTest143()
        {
            var xml = "xp001.xml";
            var testExpression = @"following::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following (try to access a child)
        /// following::Para
        /// </summary>
        [Fact]
        public static void AxesTest144()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"following::Para";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// following (try to access an attribute)
        /// following::Origin/Para/@Attr1
        /// </summary>
        [Fact]
        public static void AxesTest145()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"following::Origin/Para/@Attr1";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// following (try to access a namespace node)
        /// following::xmlns:NSbook
        /// </summary>
        [Fact]
        public static void AxesTest146()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection";
            var testExpression = @"following::xmlns:NSbook";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// following (condition on attr, true)
        /// following::*[@Attr1="value1"]
        /// </summary>
        [Fact]
        public static void AxesTest147()
        {
            var xml = "xp002.xml";
            var testExpression = @"following::*[@Attr1=""value1""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following (condition on attr, false)
        /// following::*[@Attr1="value2"]
        /// </summary>
        [Fact]
        public static void AxesTest148()
        {
            var xml = "xp002.xml";
            var testExpression = @"following::*[@Attr1=""value2""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following (condition on attr, non-existent)
        /// following::*[@Attr5="value5"]
        /// </summary>
        [Fact]
        public static void AxesTest149()
        {
            var xml = "xp002.xml";
            var testExpression = @"following::*[@Attr5=""value5""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following (condition on value, true)
        /// following::*[.="XPath test"]
        /// </summary>
        [Fact]
        public static void AxesTest150()
        {
            var xml = "xp002.xml";
            var testExpression = @"following::*[.=""XPath test""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following (condition on value, false)
        /// following::*[.="XPath tests"]
        /// </summary>
        [Fact]
        public static void AxesTest151()
        {
            var xml = "xp002.xml";
            var testExpression = @"following::*[.=""XPath tests""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following (condition on value, non-existent)
        /// following::Origin[.="XPath test"]
        /// </summary>
        [Fact]
        public static void AxesTest152()
        {
            var xml = "xp002.xml";
            var testExpression = @"following::Origin[.=""XPath test""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following (condition on attr and value, true)
        /// following::*[.="XPath test"] [@Attr1= "value1"]
        /// </summary>
        [Fact]
        public static void AxesTest153()
        {
            var xml = "xp002.xml";
            var testExpression = @"following::*[.=""XPath test""] [@Attr1= ""value1""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following (condition on attr and value, false)
        /// following::*[.="XPath tests"][ @Attr1= "value1"]
        /// </summary>
        [Fact]
        public static void AxesTest154()
        {
            var xml = "xp002.xml";
            var testExpression = @"following::*[.=""XPath tests""][ @Attr1= ""value1""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following (condition on attr and value, non-existent)
        /// following::*[.="XPath test"][@Attrib1= "value1"]
        /// </summary>
        [Fact]
        public static void AxesTest155()
        {
            var xml = "xp002.xml";
            var testExpression = @"following::*[.=""XPath test""][@Attrib1= ""value1""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following (condition on multiple attrs, true)
        /// following::*[@Attr1="value1"][@Attr2="value2"]
        /// </summary>
        [Fact]
        public static void AxesTest156()
        {
            var xml = "xp002.xml";
            var testExpression = @"following::*[@Attr1=""value1""][@Attr2=""value2""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following (condition on multiple attrs, false)
        /// following::*[@Attr1="value1"][@Attr2="value3"]
        /// </summary>
        [Fact]
        public static void AxesTest157()
        {
            var xml = "xp002.xml";
            var testExpression = @"following::*[@Attr1=""value1""][@Attr2=""value3""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following (condition on multiple attrs, non-existent)
        /// following::*[@Attr1="value1"][@Attr5="value5"]
        /// </summary>
        [Fact]
        public static void AxesTest158()
        {
            var xml = "xp002.xml";
            var testExpression = @"following::*[@Attr1=""value1""][@Attr5=""value5""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// preceding (element other than root)
        /// preceding::*
        /// </summary>
        [Fact]
        public static void AxesTest159()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"preceding::*";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (root node)
        /// preceding::*
        /// </summary>
        [Fact]
        public static void AxesTest160()
        {
            var xml = "xp001.xml";
            var testExpression = @"preceding::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// preceding (context node = attribute)
        /// preceding::*
        /// </summary>
        [Fact]
        public static void AxesTest161()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr2";
            var testExpression = @"preceding::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (try to get an attribute)
        /// preceding::Title/@Attr1
        /// </summary>
        [Fact]
        public static void AxesTest162()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"preceding::Title/@Attr1";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "Attr1",
                    Name = "Attr1",
                    HasNameTable = true,
                    Value = "value1"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (try to get a namespace node)
        /// preceding::xmlns:NSbook
        /// </summary>
        [Fact]
        public static void AxesTest163()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/moviesection";
            var testExpression = @"preceding::xmlns:NSbook";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (try to access an ancestor)
        /// preceding::Chap
        /// </summary>
        [Fact]
        public static void AxesTest164()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"preceding::Chap";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (last node in doc order)
        /// preceding::*
        /// </summary>
        [Fact]
        public static void AxesTest165()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap[2]/Title";
            var testExpression = @"preceding::*";
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
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                },
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
                    IsEmptyElement = true,
                    LocalName = "Origin",
                    Name = "Origin",
                    HasNameTable = true
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
        /// preceding (condition on attr, true)
        /// preceding::*[@Attr1="value1"]
        /// </summary>
        [Fact]
        public static void AxesTest166()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"preceding::*[@Attr1=""value1""]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (condition on attr, false)
        /// preceding::*[@Attr1="value2"]
        /// </summary>
        [Fact]
        public static void AxesTest167()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"preceding::*[@Attr1=""value2""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (condition on attr, non-existent)
        /// preceding::*[@Attr5="value5"]
        /// </summary>
        [Fact]
        public static void AxesTest168()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"preceding::*[@Attr5=""value5""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (condition on value, true)
        /// preceding::*[.="XPath test"]
        /// </summary>
        [Fact]
        public static void AxesTest169()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"preceding::*[.=""XPath test""]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (condition on value, false)
        /// preceding::*[.="XPath tests"]
        /// </summary>
        [Fact]
        public static void AxesTest170()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"preceding::*[.=""XPath tests""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (condition value, non-existent)
        /// preceding::Chap[.="XPath test"]
        /// </summary>
        [Fact]
        public static void AxesTest171()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"preceding::Chap[.=""XPath test""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (condition on attr and value, true)
        /// preceding::*[name="This is the name"][.="XPath test"]
        /// </summary>
        [Fact]
        public static void AxesTest172()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para[2]";
            var testExpression = @"preceding::*[name=""This is the name""][.=""XPath test""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (condition on attr and value, false)
        /// preceding::*[name="This is the name, false!"][.="XPath tests"]
        /// </summary>
        [Fact]
        public static void AxesTest173()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para[2]";
            var testExpression = @"preceding::*[name=""This is the name, false!""][.=""XPath tests""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (condition on attr and value, non-existent)
        /// preceding::*[desc="This is the name"][ .="XPath test"]
        /// </summary>
        [Fact]
        public static void AxesTest174()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para[2]";
            var testExpression = @"preceding::*[desc=""This is the name""][ .=""XPath test""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (condition on multiple attrs, true)
        /// preceding::*[@Attr1="value1"][@Attr2="value2"]
        /// </summary>
        [Fact]
        public static void AxesTest175()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para[2]";
            var testExpression = @"preceding::*[@Attr1=""value1""][@Attr2=""value2""]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "Origin",
                    Name = "Origin",
                    HasNameTable = true
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (condition on multiple attr, false)
        /// preceding::*[@Attr1="value2"][@Attr2="value1"]
        /// </summary>
        [Fact]
        public static void AxesTest176()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para[2]";
            var testExpression = @"preceding::*[@Attr1=""value2""][@Attr2=""value1""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding (condition on multiple attr, non-existent)
        /// preceding::*[@Attr5="value5"][@Attr2="value2"]
        /// </summary>
        [Fact]
        public static void AxesTest177()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para[2]";
            var testExpression = @"preceding::*[@Attr5=""value5""][@Attr2=""value2""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (select an existing parent)
        /// parent::Test1
        /// </summary>
        [Fact]
        public static void AxesTest178()
        {
            var xml = "xp005.xml";
            var startingNodePath = "//Child1";
            var testExpression = @"parent::Test1";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Test1",
                    Name = "Test1",
                    HasNameTable = true,
                    Value = "\n\t\tFirst\n\t\tSecond\n\t\tThird\n\t\tFourth\n\t\tLast\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (select non-existing parent)
        /// parent::Doc
        /// </summary>
        [Fact]
        public static void AxesTest179()
        {
            var xml = "xp005.xml";
            var startingNodePath = "//Child1";
            var testExpression = @"parent::Doc";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (root node)
        /// parent::*
        /// </summary>
        [Fact]
        public static void AxesTest180()
        {
            var xml = "xp005.xml";
            var testExpression = @"parent::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// parent (attribute)
        /// parent::*
        /// </summary>
        [Fact]
        public static void AxesTest181()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/@Attr1";
            var testExpression = @"parent::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Test1",
                    Name = "Test1",
                    HasNameTable = true,
                    Value = "\n\t\tFirst\n\t\tSecond\n\t\tThird\n\t\tFourth\n\t\tLast\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (namespace node)
        /// namespace::NSbook/parent::*
        /// </summary>
        [Fact]
        public static void AxesTest182()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection";
            var testExpression = @"namespace::NSbook/parent::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "booksection",
                    Name = "booksection",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\t\n\t\t\tA Brief History Of Time\n\t\t\n\t\t\n\t\t\tThe Beautiful Universe\n\t\t\n\t\t\n\t\t\tNewton's Time Machine\n\t\t\n\t\t\n\t\t\tThe Quark And The Jaguar\n\t\t\n\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (condition on attribute, true)
        /// parent::*[@Attr1="First"]
        /// </summary>
        [Fact]
        public static void AxesTest183()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child1";
            var testExpression = @"parent::*[@Attr1=""First""]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Test1",
                    Name = "Test1",
                    HasNameTable = true,
                    Value = "\n\t\tFirst\n\t\tSecond\n\t\tThird\n\t\tFourth\n\t\tLast\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (condition on attr, false)
        /// parent::*[@Attr1="Second"]
        /// </summary>
        [Fact]
        public static void AxesTest184()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child1";
            var testExpression = @"parent::*[@Attr1=""Second""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (condition on attr, non-existent)
        /// parent::*[@Attr6="Last"]
        /// </summary>
        [Fact]
        public static void AxesTest185()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child1";
            var testExpression = @"parent::*[@Attr6=""Last""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (condition on value, true)
        /// parent::*[.="First paragraph  End of first paragraph  "]
        /// </summary>
        [Fact]
        public static void AxesTest186()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para/Origin";
            var testExpression = @"parent::*[.=""First paragraph  End of first paragraph  ""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (condition on value, false)
        /// parent::*[.="FirstparagraphEndoffirstparagraph"]
        /// </summary>
        [Fact]
        public static void AxesTest187()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para/Origin";
            var testExpression = @"parent::*[.=""FirstparagraphEndoffirstparagraph""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (condition on multiple attributes, true)
        /// parent::*[@Attr1="First"][@Attr2="Second"]
        /// </summary>
        [Fact]
        public static void AxesTest189()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child2";
            var testExpression = @"parent::*[@Attr1=""First""][@Attr2=""Second""]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Test1",
                    Name = "Test1",
                    HasNameTable = true,
                    Value = "\n\t\tFirst\n\t\tSecond\n\t\tThird\n\t\tFourth\n\t\tLast\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (condition on multiple attr, false)
        /// parent::*[@Attr1="First"][@Attr2="Last"]
        /// </summary>
        [Fact]
        public static void AxesTest190()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child2";
            var testExpression = @"parent::*[@Attr1=""First""][@Attr2=""Last""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (condition on multiple attr, non-existent)
        /// parent::*[@Attr1="First"][@Attr6="Last"]
        /// </summary>
        [Fact]
        public static void AxesTest191()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child2";
            var testExpression = @"parent::*[@Attr1=""First""][@Attr6=""Last""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (condition on attr and value, true)
        /// parent::*[@Attr1="only one"][.="only one"]
        /// </summary>
        [Fact]
        public static void AxesTest192()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test2/Child1";
            var testExpression = @"parent::*[@Attr1=""only one""][.=""only one""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (condition on attr and value, false)
        /// parent::*[@Attr1="only one"][.="only two"]
        /// </summary>
        [Fact]
        public static void AxesTest193()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test2/Child1";
            var testExpression = @"parent::*[@Attr1=""only one""][.=""only two""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// parent (condition on attr and value, non-existent)
        /// parent::*[@Attr6="Last"][.="onlyone"]
        /// </summary>
        [Fact]
        public static void AxesTest194()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test2/Child1";
            var testExpression = @"parent::*[@Attr6=""Last""][.=""onlyone""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (select an existing ancestor)
        /// ancestor::Doc
        /// </summary>
        [Fact]
        public static void AxesTest195()
        {
            var xml = "xp005.xml";
            var startingNodePath = "//Child1";
            var testExpression = @"ancestor::Doc";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Doc",
                    Name = "Doc",
                    HasNameTable = true,
                    Value = "\n\t\n\t\tFirst\n\t\tSecond\n\t\tThird\n\t\tFourth\n\t\tLast\n\t\n\t\n\t\tonly one\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (root node)
        /// ancestor::*
        /// </summary>
        [Fact]
        public static void AxesTest196()
        {
            var xml = "xp005.xml";
            var testExpression = @"ancestor::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// ancestor (attribute)
        /// ancestor::*
        /// </summary>
        [Fact]
        public static void AxesTest197()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/@Attr1";
            var testExpression = @"ancestor::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Doc",
                    Name = "Doc",
                    HasNameTable = true,
                    Value = "\n\t\n\t\tFirst\n\t\tSecond\n\t\tThird\n\t\tFourth\n\t\tLast\n\t\n\t\n\t\tonly one\n\t\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Test1",
                    Name = "Test1",
                    HasNameTable = true,
                    Value = "\n\t\tFirst\n\t\tSecond\n\t\tThird\n\t\tFourth\n\t\tLast\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (namespace node)
        /// NS3: namespace::NSbook/ancestor::*
        /// </summary>
        [Fact]
        public static void AxesTest198()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection";
            var testExpression = @"namespace::NSbook/ancestor::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "store",
                    Name = "store",
                    HasNameTable = true,
                    Value =
                        "\n\n\t\n\t\n\t\t\n\t\t\tA Brief History Of Time\n\t\t\n\t\t\n\t\t\tThe Beautiful Universe\n\t\t\n\t\t\n\t\t\tNewton's Time Machine\n\t\t\n\t\t\n\t\t\tThe Quark And The Jaguar\n\t\t\n\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\t\n\t\t\tVeritcal Limit\n\t\t\n\t\t\n\t\t\t\n\t\t\t\tJinnah\n\t\t\t\n\t\t\n\t\t\n\t\n\t\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "booksection",
                    Name = "booksection",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\t\n\t\t\tA Brief History Of Time\n\t\t\n\t\t\n\t\t\tThe Beautiful Universe\n\t\t\n\t\t\n\t\t\tNewton's Time Machine\n\t\t\n\t\t\n\t\t\tThe Quark And The Jaguar\n\t\t\n\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (try to select a non-existent ancestor)
        /// ancestor::Test1
        /// </summary>
        [Fact]
        public static void AxesTest199()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test2/Child1";
            var testExpression = @"ancestor::Test1";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (condition on attribute, true)
        /// ancestor::*[@Attr1="First"]
        /// </summary>
        [Fact]
        public static void AxesTest1100()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child1";
            var testExpression = @"ancestor::*[@Attr1=""First""]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Test1",
                    Name = "Test1",
                    HasNameTable = true,
                    Value = "\n\t\tFirst\n\t\tSecond\n\t\tThird\n\t\tFourth\n\t\tLast\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (condition on attr, false)
        /// ancestor::*[@Attr1="Second"]
        /// </summary>
        [Fact]
        public static void AxesTest1101()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child1";
            var testExpression = @"ancestor::*[@Attr1=""Second""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (condition on attr, non-existent)
        /// ancestor::*[@Attr6="Last"]
        /// </summary>
        [Fact]
        public static void AxesTest1102()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child1";
            var testExpression = @"ancestor::*[@Attr6=""Last""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (condition on value, true)
        /// ancestor::*[.="First paragraph  End of first paragraph  "]
        /// </summary>
        [Fact]
        public static void AxesTest1103()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para/Origin";
            var testExpression = @"ancestor::*[.=""First paragraph  End of first paragraph  ""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (condition on value, false)
        /// ancestor::*[.="FirstparagraphEndoffirstparagraph"]
        /// </summary>
        [Fact]
        public static void AxesTest1104()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Para/Origin";
            var testExpression = @"ancestor::*[.=""FirstparagraphEndoffirstparagraph""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (condition on multiple attributes, true)
        /// ancestor::*[@Attr1="First"][@Attr2="Second"]
        /// </summary>
        [Fact]
        public static void AxesTest1106()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child2";
            var testExpression = @"ancestor::*[@Attr1=""First""][@Attr2=""Second""]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Test1",
                    Name = "Test1",
                    HasNameTable = true,
                    Value = "\n\t\tFirst\n\t\tSecond\n\t\tThird\n\t\tFourth\n\t\tLast\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (condition on multiple attr, false)
        /// ancestor::*[@Attr1="First"][@Attr2="Third"]
        /// </summary>
        [Fact]
        public static void AxesTest1107()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child2";
            var testExpression = @"ancestor::*[@Attr1=""First""][@Attr2=""Third""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (condition on multiple attr, non-existent)
        /// ancestor::*[@Attr1="First"][@Attr6="Last"]
        /// </summary>
        [Fact]
        public static void AxesTest1108()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child2";
            var testExpression = @"ancestor::*[@Attr1=""First""][@Attr6=""Last""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (condition on attr and value, true)
        /// ancestor::*[@Attr1="only one"][.="only one"]
        /// </summary>
        [Fact]
        public static void AxesTest1109()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test2/Child1";
            var testExpression = @"ancestor::*[@Attr1=""only one""][.=""only one""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (condition on attr and value, false)
        /// ancestor::*[@Attr1="only one"][.="only two"]
        /// </summary>
        [Fact]
        public static void AxesTest1110()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test2/Child1";
            var testExpression = @"ancestor::*[@Attr1=""only one""][.=""only two""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor (condition on attr and value, non-existent)
        /// ancestor::*[@Attr6="Last"][.="only one"]
        /// </summary>
        [Fact]
        public static void AxesTest1111()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test2/Child1";
            var testExpression = @"ancestor::*[@Attr6=""Last""][.=""only one""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Should return nothing.
        /// parent::foo
        /// </summary>
        [Fact]
        public static void AxesTest1112()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"parent::foo";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return all text nodes after first book.  CDATA is grouped into text nodes so CDATA should also be returned.
        /// book[1]/following::text()
        /// </summary>
        [Fact]
        public static void AxesTest1113()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/following::text()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "JoeBob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Loser"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "US"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mike"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hyman"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Jonathan"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Marsh"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55.95"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Road and Track"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Yes"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Week"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "free"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Magazine"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.95"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Create a dream PC\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "The future of the web\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Can Netscape stay alive with Microsoft eating up its browser share?"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "MSFT 99.30"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "1998-06-23"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Frank"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Anderson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "10"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hack"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "er"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "08"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Tracking Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "2.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "0.98"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Toni"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\tCDATA Section in Author\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "B.A."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Still in Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton Forever"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "6.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "I"},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " have.\n\t\t\t"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "misery"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robert Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tHere is some CDATA\n\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Where is Trenton?"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Where in the world is Trenton?"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return all comment nodes after and not a descendant of the first book.
        /// book[1]/following::comment()
        /// </summary>
        [Fact]
        public static void AxesTest1114()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/following::comment()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = "Here's another comment sibling of Book element"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return all PI nodes that follow the first book.
        /// book[1]/following::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1115()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/following::processing-instruction()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PIMagazine",
                    Name = "PIMagazine",
                    HasNameTable = true,
                    Value = "processing Instruction "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return ERROR.
        /// book[2]/following::@*
        /// </summary>
        [Fact]
        public static void AxesTest1116()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[2]/following::@*";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return ERROR.
        /// book[3]/preceding::@*
        /// </summary>
        [Fact]
        public static void AxesTest1117()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[3]/preceding::@*";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return empty node-list.
        /// book[1]/title/following-sibling::text()
        /// </summary>
        [Fact]
        public static void AxesTest1118()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/title/following-sibling::text()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return comment node sibling of the first book element.
        /// book[1]/following-sibling::comment()
        /// </summary>
        [Fact]
        public static void AxesTest1119()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/following-sibling::comment()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = "Here's another comment sibling of Book element"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return PI nodes that are siblings of and after the first book.
        /// book[1]/following-sibling::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1120()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/following-sibling::processing-instruction()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return all siblings of the first book no-matter what type.
        /// book[1]/following-sibling::node()
        /// </summary>
        [Fact]
        public static void AxesTest1121()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/following-sibling::node()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = "Here's another comment sibling of Book element"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tCDATA Section in Author\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\t\n\t\t\tHere is some CDATA\n\t\t\n\t\tWhere is Trenton?\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere in the world is Trenton?\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return ERROR.
        /// book[2]/following-sibling::@*
        /// </summary>
        [Fact]
        public static void AxesTest1122()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[2]/following-sibling::@*";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return empty node-list.
        /// book[last()]/@*/following-sibling::*
        /// </summary>
        [Fact]
        public static void AxesTest1123()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[last()]/@*/following-sibling::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return first and second book elements..
        /// book[3]/preceding-sibling::book
        /// </summary>
        [Fact]
        public static void AxesTest1124()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[3]/preceding-sibling::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return empty node-list.
        /// book[1]/preceding-sibling::*
        /// </summary>
        [Fact]
        public static void AxesTest1125()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/preceding-sibling::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return all comments that are siblings of last book element.
        /// book[last()]/preceding-sibling::comment()
        /// </summary>
        [Fact]
        public static void AxesTest1126()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[last()]/preceding-sibling::comment()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = " Books or Magazines Here "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = "Here's another comment sibling of Book element"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return all PI siblings of last my:book element.
        /// *[last()]/preceding-sibling::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1127()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"*[last()]/preceding-sibling::processing-instruction()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return any node that is before first book and at the same level (sibling).
        /// book[1]/preceding-sibling::node()
        /// </summary>
        [Fact]
        public static void AxesTest1128()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/preceding-sibling::node()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = " Books or Magazines Here "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return second book element (History of Trenton).
        /// book[3]/preceding-sibling::book[1]/title/text()
        /// </summary>
        [Fact]
        public static void AxesTest1129()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[3]/preceding-sibling::book[1]/title/text()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton"});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return empty node-list.
        /// book[3]/@*/preceding-sibling::*
        /// </summary>
        [Fact]
        public static void AxesTest1130()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[3]/@*/preceding-sibling::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return empty node list.
        /// preceding::node()
        /// </summary>
        [Fact]
        public static void AxesTest1131()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"preceding::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = " This file represents a fragment of a book store inventory database "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return empty node list.
        /// preceding-sibling::node()
        /// </summary>
        [Fact]
        public static void AxesTest1132()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"preceding-sibling::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = " This file represents a fragment of a book store inventory database "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return first book in document order.
        /// book/preceding-sibling::*[last()]
        /// </summary>
        [Fact]
        public static void AxesTest1133()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/preceding-sibling::*[last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return nodes after the first book except descendants, attribute::, and namespaces.
        /// book[1]/following::node()
        /// </summary>
        [Fact]
        public static void AxesTest1134()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/following::node()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "JoeBob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "JoeBob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Loser"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Loser"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "US"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mike"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mike"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Hyman"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hyman"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Jonathan"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Jonathan"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Marsh"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Marsh"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55.95"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55.95"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Road and Track"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Road and Track"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.50"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "special_edition",
                    Name = "special_edition",
                    HasNameTable = true,
                    Value = "Yes"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Yes"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Week"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Week"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "free"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "free"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Magazine"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Magazine"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.95"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.95"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story1",
                    Name = "story1",
                    HasNameTable = true,
                    Value = "Create a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Create a dream PC\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story2",
                    Name = "story2",
                    HasNameTable = true,
                    Value =
                        "The future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "The future of the web\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Can Netscape stay alive with Microsoft eating up its browser share?"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Can Netscape stay alive with Microsoft eating up its browser share?"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "stock",
                    Name = "stock",
                    HasNameTable = true,
                    Value = "MSFT 99.30"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "MSFT 99.30"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "date",
                    Name = "date",
                    HasNameTable = true,
                    Value = "1998-06-23"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "1998-06-23"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story3",
                    Name = "story3",
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value = "\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story1",
                    Name = "story1",
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Frank"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Frank"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Anderson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Anderson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "10"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "10"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Hack"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hack"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "er"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "er"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "08"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "08"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PIMagazine",
                    Name = "PIMagazine",
                    HasNameTable = true,
                    Value = "processing Instruction "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Tracking Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "2.50"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "2.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = "Here's another comment sibling of Book element"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "0.98"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "0.98"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tCDATA Section in Author\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tCDATA Section in Author\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Toni"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Toni"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\tCDATA Section in Author\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "B.A."
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "B.A."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Still in Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Still in Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Trenton Forever"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton Forever"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "6.50"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "6.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "excerpt",
                    Name = "excerpt",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "emph",
                    Name = "emph",
                    HasNameTable = true,
                    Value = "I"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "I"},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " have.\n\t\t\t"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition-list",
                    Name = "definition-list",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "term",
                    Name = "term",
                    HasNameTable = true,
                    Value = "Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition",
                    Name = "definition",
                    HasNameTable = true,
                    Value = "misery"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "misery"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "my:author",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Robert Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robert Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\t\n\t\t\tHere is some CDATA\n\t\t\n\t\tWhere is Trenton?\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tHere is some CDATA\n\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where is Trenton?"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Where is Trenton?"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere in the world is Trenton?\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where in the world is Trenton?"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Where in the world is Trenton?"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return all elements after the last book node.
        /// book[last()]/@*/following::*
        /// </summary>
        [Fact]
        public static void AxesTest1135()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[last()]/@*/following::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tCDATA Section in Author\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Toni"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "B.A."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Still in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Trenton Forever"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "6.50"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "excerpt",
                    Name = "excerpt",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "emph",
                    Name = "emph",
                    HasNameTable = true,
                    Value = "I"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition-list",
                    Name = "definition-list",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "term",
                    Name = "term",
                    HasNameTable = true,
                    Value = "Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition",
                    Name = "definition",
                    HasNameTable = true,
                    Value = "misery"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "my:author",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Robert Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\t\n\t\t\tHere is some CDATA\n\t\t\n\t\tWhere is Trenton?\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where is Trenton?"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere in the world is Trenton?\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where in the world is Trenton?"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return text nodes in the first and second book and their descendants.
        /// book[3]/preceding::text()
        /// </summary>
        [Fact]
        public static void AxesTest1136()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[3]/preceding::text()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Joe"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "USA"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "12"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "JoeBob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Loser"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "US"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return all comment nodes from beginning of document to the third book.
        /// book[3]/preceding::comment()
        /// </summary>
        [Fact]
        public static void AxesTest1137()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[3]/preceding::comment()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = " This file represents a fragment of a book store inventory database "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = " Books or Magazines Here "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = " Enter first-name, last-name and any awards "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return all PI's in the document header (empty node-list for this case).
        /// //*[last()]/preceding::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1138()
        {
            var xml = "books_2.xml";
            var testExpression = @"//*[last()]/preceding::processing-instruction()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PIMagazine",
                    Name = "PIMagazine",
                    HasNameTable = true,
                    Value = "processing Instruction "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Return any node that is before the first book.
        /// book[1]/preceding::node()
        /// </summary>
        [Fact]
        public static void AxesTest1139()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/preceding::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = " This file represents a fragment of a book store inventory database "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = " Books or Magazines Here "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return all magazine elements after first book.
        /// book[1]/following-sibling::magazine
        /// </summary>
        [Fact]
        public static void AxesTest1140()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/following-sibling::magazine";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return empty node-list.
        /// *[last()]/following-sibling::*
        /// </summary>
        [Fact]
        public static void AxesTest1141()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"*[last()]/following-sibling::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return comment node sibling of the first book element.
        /// book[1]/following-sibling::comment()
        /// </summary>
        [Fact]
        public static void AxesTest1142()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/following-sibling::comment()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = "Here's another comment sibling of Book element"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return PI nodes that are siblings of and after the first book.
        /// book[1]/following-sibling::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1143()
        {
            var xml = "books_2.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[1]/following-sibling::processing-instruction()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Should give an error for expressions ending in '/'
        /// /bookstore/book/
        /// </summary>
        [Fact]
        public static void AxesTest1144()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book/";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// following-sibling axis is empty for attribute nodes
        /// /Doc/Test1/@Attr1/following-sibling::*
        /// </summary>
        [Fact]
        public static void AxesTest1145()
        {
            var xml = "xp005.xml";
            var testExpression = @"/Doc/Test1/@Attr1/following-sibling::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// following-sibling axis is empty for namespace nodes
        /// NS4: namespace::NSbook/following-sibling::*
        /// </summary>
        [Fact]
        public static void AxesTest1146()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection";
            var testExpression = @"namespace::NSbook/following-sibling::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding-sibling axis should be empty for attribute nodes
        /// /Doc/Test1/@Attr2/preceding-sibling::*
        /// </summary>
        [Fact]
        public static void AxesTest1147()
        {
            var xml = "xp005.xml";
            var testExpression = @"/Doc/Test1/@Attr2/preceding-sibling::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// preceding-sibling is empty for namespace nodes
        /// NS5: namespace::NSbook/preceding-sibling::*
        /// </summary>
        [Fact]
        public static void AxesTest1148()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection";
            var testExpression = @"namespace::NSbook/preceding-sibling::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Try to get a descendant. Descendants of the context node are not on the following axis
        /// following::Test1
        /// </summary>
        [Fact]
        public static void AxesTest1149()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"following::Test1";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Attribute axis is empty if the context node is not an element. Context node =attribute
        /// attribute::*
        /// </summary>
        [Fact]
        public static void AxesTest1150()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/@Attr1";
            var testExpression = @"attribute::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Attribute axis is empty if the context node is not an element. Context node = text
        /// attribute::*
        /// </summary>
        [Fact]
        public static void AxesTest1151()
        {
            var xml = "xp005.xml";
            var startingNodePath = "/Doc/Test1/Child1/text()";
            var testExpression = @"attribute::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Attribute axis is empty if the context node is not an element. Context node = comment
        /// attribute::*
        /// </summary>
        [Fact]
        public static void AxesTest1152()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/comment()";
            var testExpression = @"attribute::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Attribute axis is empty if the context node is not an element. Context node = namespace node
        /// NS6: namespace::NSbook/attribute::*
        /// </summary>
        [Fact]
        public static void AxesTest1153()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection";
            var testExpression = @"namespace::NSbook/attribute::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// namespace axis should be empty if the context node is not an element (attribute in this case)
        /// NS7: //@*/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1154()
        {
            var xml = "name.xml";
            var testExpression = @"//@*/namespace::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// namespace axis should be empty if the context node is not an element (text in this case)
        /// NS8: //text()/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1155()
        {
            var xml = "name.xml";
            var testExpression = @"//text()/namespace::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// namespace axis should be empty if the context node is not an element (PI in this case)
        /// NS9: //processing-instruction()/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1156()
        {
            var xml = "name.xml";
            var testExpression = @"//processing-instruction()/namespace::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// namespace axis should be empty if the context node is not an element (comment in this case)
        /// NS10: //comment()/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1157()
        {
            var xml = "name.xml";
            var testExpression = @"//comment()/namespace::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// NS11: namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1158()
        {
            var xml = "name.xml";
            var testExpression = @"/store/booksection/book/namespace::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// a node for xml is declared implicitly
        /// NS12: namespace::xml
        /// </summary>
        [Fact]
        public static void AxesTest1159()
        {
            var xml = "name.xml";
            var testExpression = @"/store/booksection/book/namespace::xml";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// check that namespace node has the element node as its parent node
        /// NS13: /store/booksection/namespace::NSbook/parent::*
        /// </summary>
        [Fact]
        public static void AxesTest1160()
        {
            var xml = "name.xml";
            var testExpression = @"/store/booksection/namespace::NSbook/parent::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "booksection",
                    Name = "booksection",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\t\n\t\t\tA Brief History Of Time\n\t\t\n\t\t\n\t\t\tThe Beautiful Universe\n\t\t\n\t\t\n\t\t\tNewton's Time Machine\n\t\t\n\t\t\n\t\t\tThe Quark And The Jaguar\n\t\t\n\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// namespace declared in an ancestor
        /// NS15: /store/booksection/book/namespace::NSbook
        /// </summary>
        [Fact]
        public static void AxesTest1161()
        {
            var xml = "name.xml";
            var testExpression = @"/store/booksection/book/namespace::NSbook";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// namespace declared in the ancestors twice. Check that the last one overwrites the previous one.
        /// NS16: string(/store/moviesection/documentry/NSmovie:movie/namespace::NSmovie)
        /// </summary>
        [Fact]
        public static void AxesTest1162()
        {
            var xml = "name.xml";
            var testExpression = @"string(/store/moviesection/documentry/NSmovie:movie/namespace::NSmovie)";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSmovie", "http://documentry.htm");
            var expected = @"http://documentry.htm";

            Utils.XPathStringTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// namespace declared in the ancestors twice. Check that the last one overwrites the previous one.
        /// NS17: /store/moviesection/documentry/NSmovie:movie/namespace::NSmovie
        /// </summary>
        [Fact]
        public static void AxesTest1163()
        {
            var xml = "name.xml";
            var testExpression = @"/store/moviesection/documentry/NSmovie:movie/namespace::NSmovie";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSmovie", "http://documentry.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSmovie",
                    Name = "NSmovie",
                    HasNameTable = true,
                    Value = "http://documentry.htm"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// string value of the namespace node is the namespace URI that is bound to the prefix.
        /// NS18: string(/store/moviesection/namespace::*[1])
        /// </summary>
        [Fact]
        public static void AxesTest1164()
        {
            var xml = "name.xml";
            var testExpression = @"string(/store/moviesection/namespace::*[1])";
            var expected = @"http://movie.htm";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// the namespace-uri() should always be a null string
        /// NS19: namespace-uri(/store/moviesection/namespace::*[1])
        /// </summary>
        [Fact]
        public static void AxesTest1165()
        {
            var xml = "name.xml";
            var testExpression = @"namespace-uri(/store/moviesection/namespace::*[1])";
            var expected = @"";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// the namespace-uri() should always be a null string, node has a default NS
        /// NS20: namespace-uri(/store/moviesection/namespace::*[1])
        /// </summary>
        [Fact]
        public static void AxesTest1166()
        {
            var xml = "name2.xml";
            var testExpression = @"namespace-uri(/store/moviesection/namespace::*[1])";
            var expected = @"";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// the namespace-uri() should always be a null string, node has a prefix
        /// NS21: namespace-uri(/store/moviesection/namespace::*[1])
        /// </summary>
        [Fact]
        public static void AxesTest1167()
        {
            var xml = "name2.xml";
            var testExpression = @"namespace-uri(/store/moviesection/NSMovie:*/namespace::*[1])";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSMovie", "http://movie.htm");
            var expected = @"";

            Utils.XPathStringTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// NS22: //namespace::cnn
        /// </summary>
        [Fact]
        public static void AxesTest1168()
        {
            var xml = "namespaces.xml";
            var testExpression = @"//namespace::cnn";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "cnn",
                    Name = "cnn",
                    HasNameTable = true,
                    Value = "http://www.cnn.com"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "cnn",
                    Name = "cnn",
                    HasNameTable = true,
                    Value = "http://www.cnn.com"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// select all elements that have the default namespace child, should return all main companies
        /// NS23: //* [namespace::*="http://companies_default.htm"]
        /// </summary>
        [Fact]
        public static void AxesTest1169()
        {
            var xml = "namespaces.xml";
            var testExpression = @"//* [namespace::*=""http://companies_default.htm""]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "companies",
                    Name = "companies",
                    NamespaceURI = "http://companies_default.htm",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\thttp://www.microsoft.com\n\t\tmsft\n\t\t\n\t\t\thttp://expedia.com\n\t\t\texpe\n\t\t\t\t\n\t\n\t\n\t\thttp://www.aol.com\n\t\taol\n\t\t\n\t\t\thttp://timewarner.com\n\t\t\ttw\n\t\t\t\n\t\t\t\thttp://www.cnn.com\n\t\t\t\t\t\t\n\t\t\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// NS24: //company[2]/aol:company/tw:company/namespace::cnn
        /// </summary>
        [Fact]
        public static void AxesTest1170()
        {
            var xml = "namespaces.xml";
            var testExpression = @"//company[2]/aol:company/tw:company/namespace::cnn";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("aol", "http://www.aol.com");
            namespaceManager.AddNamespace("tw", "http://www.timewarner.com");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "cnn",
                    Name = "cnn",
                    HasNameTable = true,
                    Value = "http://www.cnn.com"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// NS25: //company[2]/aol:company/tw:company/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1171()
        {
            var xml = "namespaces.xml";
            var testExpression = @"//company[2]/aol:company/tw:company/namespace::*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("aol", "http://www.aol.com");
            namespaceManager.AddNamespace("tw", "http://www.timewarner.com");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "cnn",
                    Name = "cnn",
                    HasNameTable = true,
                    Value = "http://www.cnn.com"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "tw",
                    Name = "tw",
                    HasNameTable = true,
                    Value = "http://www.timewarner.com"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "aol",
                    Name = "aol",
                    HasNameTable = true,
                    Value = "http://www.aol.com"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// Prefixes are redefined, checks that this is done correctly
        /// NS26: //prefix:*//namespace::node()
        /// </summary>
        [Fact]
        public static void AxesTest1172()
        {
            var xml = "ns_prefixes.xml";
            var testExpression = @"//prefix:*//namespace::node()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("prefix", "http://prefix4.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "prefix",
                    Name = "prefix",
                    HasNameTable = true,
                    Value = "http://prefix4.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "prefix",
                    Name = "prefix",
                    HasNameTable = true,
                    Value = "http://prefix4.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// Default namespace is redefined, checks that this is done correctly
        /// NS27: //namespace::node()
        /// </summary>
        [Fact]
        public static void AxesTest1173()
        {
            var xml = "ns_default.xml";
            var testExpression = @"//namespace::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    HasNameTable = true,
                    Value = "http://default1.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    HasNameTable = true,
                    Value = "http://default2.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    HasNameTable = true,
                    Value = "http://default1.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    HasNameTable = true,
                    Value = "http://default4.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "prefix",
                    Name = "prefix",
                    HasNameTable = true,
                    Value = "http://prefix.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    HasNameTable = true,
                    Value = "http://default3.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "prefix",
                    Name = "prefix",
                    HasNameTable = true,
                    Value = "http://prefix.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    HasNameTable = true,
                    Value = "http://default3.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// All namespace nodes belong to the null namespace
        /// NS28: //namespace::prefix:*
        /// </summary>
        [Fact]
        public static void AxesTest1174()
        {
            var xml = "namespaces.xml";
            var testExpression = @"//namespace::prefix:*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("prefix", "http://companies_default.htm");
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// All namespace nodes belong to the null namespace
        /// NS29: //namespace::prefix:*
        /// </summary>
        [Fact]
        public static void AxesTest1175()
        {
            var xml = "ns_prefixes.xml";
            var testExpression = @"//namespace::prefix:*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("prefix", "http://prefix2.htm");
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// Default namespace should be cleared by the xmlns="""" declaration
        /// NS30: //node()[namespace-uri()=""]
        /// </summary>
        [Fact]
        public static void AxesTest1176()
        {
            var xml = "ns_default.xml";
            var testExpression = @"//node()[namespace-uri()=""""]";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "\n\tElement1\n\t"},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\tElement11\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\tElement111\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tElement1111\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "elem",
                    Name = "elem",
                    HasNameTable = true,
                    Value = "\n\t\tElement12\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "\n\t\tElement12\n\t"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\tElement13\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\tElement131\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// preceding, predicate uses position()
        /// </summary>
        [Fact]
        public static void AxesTest1177()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[7]/preceding::book[1]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// preceding-sibling, predicate uses position()
        /// </summary>
        [Fact]
        public static void AxesTest1178()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[7]/preceding-sibling::book[1]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify that . is equivalent to self::node()
        /// . = self::node()
        /// </summary>
        [Fact]
        public static void AxesTest1179()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/magazine/.";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// verify that .. is equivalent to parent::node()
        /// .. = parent::node()
        /// </summary>
        [Fact]
        public static void AxesTest1180()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/magazine/title/..";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Nodes are not repeated in a node-set. Magazine nodes should not be repeated
        /// /bookstore/book/following-sibling::magazine
        /// </summary>
        [Fact]
        public static void AxesTest1181()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book/following-sibling::magazine";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Nodes are not repeated in a node-set. Uses node() to get nodes of all types
        /// /bookstore/book/following-sibling::node()
        /// </summary>
        [Fact]
        public static void AxesTest1182()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book/following-sibling::node()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere is Trenton?\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere in the world is Trenton?\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Combines following with child axis
        /// following::book/child::title
        /// </summary>
        [Fact]
        public static void AxesTest1195()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::book/child::title";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with descendant axis
        /// following::book/descendant::*
        /// </summary>
        [Fact]
        public static void AxesTest1196()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::book/descendant::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "JoeBob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Loser"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mike"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Hyman"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Jonathan"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Marsh"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55.95"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Frank"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Anderson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "10"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Hack"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "er"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "08"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Toni"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "B.A."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Still in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Trenton Forever"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "6.50"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "excerpt",
                    Name = "excerpt",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "emph",
                    Name = "emph",
                    HasNameTable = true,
                    Value = "I"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition-list",
                    Name = "definition-list",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "term",
                    Name = "term",
                    HasNameTable = true,
                    Value = "Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition",
                    Name = "definition",
                    HasNameTable = true,
                    Value = "misery"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with parent axis
        /// following::book/parent::bookstore
        /// </summary>
        [Fact]
        public static void AxesTest1197()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::book/parent::bookstore";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with ancestor axis
        /// following::book/title/ancestor::bookstore
        /// </summary>
        [Fact]
        public static void AxesTest1198()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::book/title/ancestor::bookstore";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with following-sibling axis
        /// following::book/following-sibling::book
        /// </summary>
        [Fact]
        public static void AxesTest1199()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::book/following-sibling::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with preceding-sibling axis
        /// following::book/preceding-sibling::book
        /// </summary>
        [Fact]
        public static void AxesTest1200()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::book/preceding-sibling::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with following axis
        /// following::book/following::book
        /// </summary>
        [Fact]
        public static void AxesTest1201()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::book/following::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with preceding axis
        /// following::book/preceding::book
        /// </summary>
        [Fact]
        public static void AxesTest1202()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::book/preceding::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with attribute axis
        /// following::magazine/attribute::*
        /// </summary>
        [Fact]
        public static void AxesTest1203()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::magazine/attribute::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "weekly"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "bi-monthly"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with namespace axis
        /// NS31: following::NSmovie:*/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1204()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection";
            var testExpression = @"following::NSmovie:*/namespace::*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSmovie", "http://movie.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSmovie",
                    Name = "NSmovie",
                    HasNameTable = true,
                    Value = "http://movie.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSmovie",
                    Name = "NSmovie",
                    HasNameTable = true,
                    Value = "http://movie.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with self axis
        /// following::book/self::node()
        /// </summary>
        [Fact]
        public static void AxesTest1205()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::book/self::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with descendant-or-self axis
        /// following::book/descendant-or-self::node()
        /// </summary>
        [Fact]
        public static void AxesTest1206()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::book/descendant-or-self::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "JoeBob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "JoeBob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Loser"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Loser"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "US"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mike"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mike"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Hyman"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hyman"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Jonathan"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Jonathan"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Marsh"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Marsh"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55.95"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55.95"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Frank"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Frank"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Anderson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Anderson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "10"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "10"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Hack"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hack"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "er"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "er"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "08"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "08"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Toni"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Toni"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "B.A."
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "B.A."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Still in Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Still in Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Trenton Forever"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton Forever"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "6.50"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "6.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "excerpt",
                    Name = "excerpt",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "emph",
                    Name = "emph",
                    HasNameTable = true,
                    Value = "I"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "I"},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " have.\n\t\t\t"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition-list",
                    Name = "definition-list",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "term",
                    Name = "term",
                    HasNameTable = true,
                    Value = "Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition",
                    Name = "definition",
                    HasNameTable = true,
                    Value = "misery"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "misery"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with ancestor-or-self axis
        /// following::book/ancestor-or-self::*
        /// </summary>
        [Fact]
        public static void AxesTest1207()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book";
            var testExpression = @"following::book[1]/ancestor-or-self::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines following with namespace axis
        /// NS32: following::node()/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1208()
        {
            var xml = "ns_prefixes.xml";
            var startingNodePath = "//prefix:*";
            var testExpression = @"following::node()/namespace::*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("prefix", "http://prefix3.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "pre4",
                    Name = "pre4",
                    HasNameTable = true,
                    Value = "http://pre4.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "pre3",
                    Name = "pre3",
                    HasNameTable = true,
                    Value = "http://pre3.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "pre2",
                    Name = "pre2",
                    HasNameTable = true,
                    Value = "http://pre2.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "pre1",
                    Name = "pre1",
                    HasNameTable = true,
                    Value = "http://pre1.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with child axis
        /// preceding::book/child::title
        /// </summary>
        [Fact]
        public static void AxesTest1209()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]";
            var testExpression = @"preceding::book/child::title";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with descendant axis
        /// preceding::book/descendant::*
        /// </summary>
        [Fact]
        public static void AxesTest1210()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]";
            var testExpression = @"preceding::book/descendant::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Joe"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "my:country",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "USA"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "12"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with parent axis
        /// preceding::book/parent::bookstore
        /// </summary>
        [Fact]
        public static void AxesTest1211()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]";
            var testExpression = @"preceding::book/parent::bookstore";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with ancestor axis
        /// preceding::book/title/ancestor::bookstore
        /// </summary>
        [Fact]
        public static void AxesTest1212()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]";
            var testExpression = @"preceding::book/title/ancestor::bookstore";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with following-sibling axis
        /// preceding::book/following-sibling::book
        /// </summary>
        [Fact]
        public static void AxesTest1213()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]";
            var testExpression = @"preceding::book/following-sibling::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with preceding-sibling axis
        /// preceding::book/preceding-sibling::book
        /// </summary>
        [Fact]
        public static void AxesTest1214()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[3]";
            var testExpression = @"preceding::book/preceding-sibling::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with following axis
        /// preceding::book/following::book
        /// </summary>
        [Fact]
        public static void AxesTest1215()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]";
            var testExpression = @"preceding::book/following::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with preceding axis
        /// preceding::book/preceding::book
        /// </summary>
        [Fact]
        public static void AxesTest1216()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[3]";
            var testExpression = @"preceding::book/preceding::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with attribute axis
        /// preceding::magazine/attribute::*
        /// </summary>
        [Fact]
        public static void AxesTest1217()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]";
            var testExpression = @"preceding::magazine/attribute::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "weekly"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "bi-monthly"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with namespace axis
        /// NS33: preceding::NSbook:book/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1218()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection/book[1]";
            var testExpression = @"preceding::NSbook:book/namespace::*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSbook", "http://book.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with namespace axis
        /// NS34: preceding::prefix1:elem/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1219()
        {
            var xml = "ns_prefixes.xml";
            var startingNodePath = "//prefix:*";
            var testExpression = @"preceding::prefix1:elem/namespace::*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("prefix", "http://prefix5.htm");
            namespaceManager.AddNamespace("prefix1", "http://prefix4.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "prefix",
                    Name = "prefix",
                    HasNameTable = true,
                    Value = "http://prefix4.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "prefix",
                    Name = "prefix",
                    HasNameTable = true,
                    Value = "http://prefix4.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with namespace axis
        /// NS35: preceding::prefix1:elem/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1220()
        {
            var xml = "ns_prefixes.xml";
            var startingNodePath = "//prefix:*";
            var testExpression = @"preceding::prefix1:elem/namespace::*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("prefix", "http://prefix5.htm");
            namespaceManager.AddNamespace("prefix1", "http://prefix1.htm");
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with self axis
        /// preceding::book/self::node()
        /// </summary>
        [Fact]
        public static void AxesTest1221()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]";
            var testExpression = @"preceding::book/self::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with descendant-or-self axis
        /// preceding::book/descendant-or-self::node()
        /// </summary>
        [Fact]
        public static void AxesTest1222()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]";
            var testExpression = @"preceding::book/descendant-or-self::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Joe"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Joe"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "my:country",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "USA"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "USA"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "12"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "12"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines preceding with ancestor-or-self axis
        /// preceding::book/ancestor-or-self::*
        /// </summary>
        [Fact]
        public static void AxesTest1223()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]";
            var testExpression = @"preceding::book/ancestor-or-self::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with child axis
        /// parent::book/child::author
        /// </summary>
        [Fact]
        public static void AxesTest1224()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/title";
            var testExpression = @"parent::book/child::author";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with descendant axis
        /// parent::*/descendant::*
        /// </summary>
        [Fact]
        public static void AxesTest1225()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/title";
            var testExpression = @"parent::*/descendant::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Joe"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "my:country",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "USA"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "12"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with parent axis
        /// parent::*/parent::*
        /// </summary>
        [Fact]
        public static void AxesTest1226()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author";
            var testExpression = @"parent::*/parent::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with ancestor axis
        /// parent::author/ancestor::bookstore
        /// </summary>
        [Fact]
        public static void AxesTest1227()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[4]/author/last-name";
            var testExpression = @"parent::author/ancestor::bookstore";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with following-sibling axis
        /// parent::*/following-sibling::magazine
        /// </summary>
        [Fact]
        public static void AxesTest1228()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/author";
            var testExpression = @"parent::*/following-sibling::magazine";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with following axis
        /// parent::book/following::book
        /// </summary>
        [Fact]
        public static void AxesTest1229()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/title";
            var testExpression = @"parent::book/following::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with preceding axis
        /// parent::*/preceding::book
        /// </summary>
        [Fact]
        public static void AxesTest1230()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/title";
            var testExpression = @"parent::*/preceding::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with preceding-sibling axis
        /// parent::*/preceding-sibling::book
        /// </summary>
        [Fact]
        public static void AxesTest1231()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/title";
            var testExpression = @"parent::*/preceding-sibling::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with attribute axis
        /// parent::*/attribute::*
        /// </summary>
        [Fact]
        public static void AxesTest1232()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[1]/title";
            var testExpression = @"parent::*/attribute::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with namespace axis
        /// NS36: parent::*/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1233()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection/NSbook:book[1]/NSbook:title";
            var testExpression = @"parent::*/namespace::*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSbook", "http://book.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with namespace axis
        /// NS37: parent::*[local-name()="elem"]/namespace::prefix
        /// </summary>
        [Fact]
        public static void AxesTest1234()
        {
            var xml = "ns_prefixes.xml";
            var startingNodePath = "//prefix:*";
            var testExpression = @"parent::*[local-name()=""elem""]/namespace::prefix";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("prefix", "http://prefix4.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "prefix",
                    Name = "prefix",
                    HasNameTable = true,
                    Value = "http://prefix3.htm"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with self axis
        /// parent::book/self::node()
        /// </summary>
        [Fact]
        public static void AxesTest1235()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/title";
            var testExpression = @"parent::book/self::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with descendant-or-self axis
        /// parent::book/descendant-or-self::node()
        /// </summary>
        [Fact]
        public static void AxesTest1236()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/title";
            var testExpression = @"parent::book/descendant-or-self::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "JoeBob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "JoeBob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Loser"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Loser"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "US"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines parent with ancestor-or-self axis
        /// parent::*/ancestor-or-self::bookstore
        /// </summary>
        [Fact]
        public static void AxesTest1237()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[4]/author/last-name";
            var testExpression = @"parent::*/ancestor-or-self::bookstore";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with child axis
        /// ancestor::book/child::author
        /// </summary>
        [Fact]
        public static void AxesTest1238()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/title";
            var testExpression = @"ancestor::book/child::author";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with descendant axis
        /// ancestor::*/descendant::magazine
        /// </summary>
        [Fact]
        public static void AxesTest1239()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/title";
            var testExpression = @"ancestor::*/descendant::magazine";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with parent axis
        /// ancestor::*/parent::*
        /// </summary>
        [Fact]
        public static void AxesTest1240()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author";
            var testExpression = @"ancestor::*/parent::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with ancestor axis
        /// ancestor::book/ancestor::bookstore
        /// </summary>
        [Fact]
        public static void AxesTest1241()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[4]/author/last-name";
            var testExpression = @"ancestor::book/ancestor::bookstore";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with following-sibling axis
        /// ancestor::book/following-sibling::book
        /// </summary>
        [Fact]
        public static void AxesTest1242()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[4]/author/last-name";
            var testExpression = @"ancestor::book/following-sibling::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with following axis
        /// ancestor::*/following::book
        /// </summary>
        [Fact]
        public static void AxesTest1243()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/title/text()";
            var testExpression = @"ancestor::*/following::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with preceding axis
        /// ancestor::*/preceding::book
        /// </summary>
        [Fact]
        public static void AxesTest1244()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/title";
            var testExpression = @"ancestor::*/preceding::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with preceding-sibling axis
        /// ancestor::*/preceding-sibling::book
        /// </summary>
        [Fact]
        public static void AxesTest1245()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/title";
            var testExpression = @"ancestor::*/preceding-sibling::book";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with attribute axis
        /// ancestor::magazine/attribute::*
        /// </summary>
        [Fact]
        public static void AxesTest1246()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[1]/title";
            var testExpression = @"ancestor::magazine/attribute::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "glossy"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with namespace axis
        /// NS38: ancestor::*/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1247()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection/NSbook:book[1]/NSbook:title";
            var testExpression = @"ancestor::*/namespace::*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSbook", "http://book.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with namespace axis
        /// NS39: ancestor::store/namespace::*
        /// </summary>
        [Fact]
        public static void AxesTest1248()
        {
            var xml = "name2.xml";
            var startingNodePath = "/default:store/default:booksection/NSbook:book[1]/NSbook:title";
            var testExpression = @"ancestor::store/namespace::*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("default", "http://default.htm");
            namespaceManager.AddNamespace("NSbook", "http://book.htm");
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with self axis
        /// ancestor::book/self::node()
        /// </summary>
        [Fact]
        public static void AxesTest1249()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/title";
            var testExpression = @"ancestor::book/self::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with descendant-or-self axis
        /// ancestor::book/descendant-or-self::node()
        /// </summary>
        [Fact]
        public static void AxesTest1250()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/title";
            var testExpression = @"ancestor::book/descendant-or-self::node()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "JoeBob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "JoeBob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Loser"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Loser"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "US"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Combines ancestor with ancestor-or-self axis
        /// ancestor::*/ancestor-or-self::bookstore
        /// </summary>
        [Fact]
        public static void AxesTest1251()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[4]/author/last-name";
            var testExpression = @"ancestor::*/ancestor-or-self::bookstore";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// following-sibling::node()
        /// </summary>
        [Fact]
        public static void AxesTest1252()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"following-sibling::node()";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// attr node on following axis
        /// </summary>
        [Fact]
        public static void AxesTest1253()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"following::node()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "XPath test"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "First paragraph  End of first paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "First paragraph "},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "Origin",
                    Name = "Origin",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = " End of first paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "Second paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Second paragraph "},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// attr node on following axis
        /// </summary>
        [Fact]
        public static void AxesTest1254()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"following-sibling::node()";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// attr node on following axis
        /// </summary>
        [Fact]
        public static void AxesTest1255()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"preceding::node()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// //*[last()]/preceding::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1256()
        {
            var xml = "books_2.xml";
            var testExpression = @"//*[last()]/preceding::processing-instruction()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PIMagazine",
                    Name = "PIMagazine",
                    HasNameTable = true,
                    Value = "processing Instruction "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //*[last()]/preceding-sibling::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1257()
        {
            var xml = "books_2.xml";
            var testExpression = @"//*[last()]/preceding-sibling::processing-instruction()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PIMagazine",
                    Name = "PIMagazine",
                    HasNameTable = true,
                    Value = "processing Instruction "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //*[1]/following::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1258()
        {
            var xml = "books_2.xml";
            var testExpression = @"//*[1]/following::processing-instruction()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PIMagazine",
                    Name = "PIMagazine",
                    HasNameTable = true,
                    Value = "processing Instruction "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //*[1]/descendant-or-self::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1259()
        {
            var xml = "books_2.xml";
            var testExpression = @"//*[1]/descendant-or-self::processing-instruction()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PIMagazine",
                    Name = "PIMagazine",
                    HasNameTable = true,
                    Value = "processing Instruction "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //*[last()]/parent::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1260()
        {
            var xml = "books_2.xml";
            var testExpression = @"//*[last()]/parent::processing-instruction()";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //*[1]/following::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1261()
        {
            var xml = "books_2.xml";
            var testExpression = @"//*[1]/following::processing-instruction()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PIMagazine",
                    Name = "PIMagazine",
                    HasNameTable = true,
                    Value = "processing Instruction "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //*[1]/following-sibling::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1262()
        {
            var xml = "books_2.xml";
            var testExpression = @"//*[1]/following-sibling::processing-instruction()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //*[last()]/child::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1263()
        {
            var xml = "books_2.xml";
            var testExpression = @"//*[last()]/child::processing-instruction()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.ProcessingInstruction,
                    LocalName = "PI",
                    Name = "PI",
                    HasNameTable = true,
                    Value = "my:book processing instruction "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //*[last()]/ancestor::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1264()
        {
            var xml = "books_2.xml";
            var testExpression = @"//*[last()]/ancestor::processing-instruction()";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //*[last()]/ancestor-or-self::processing-instruction()
        /// </summary>
        [Fact]
        public static void AxesTest1265()
        {
            var xml = "books_2.xml";
            var testExpression = @"//*[last()]/ancestor-or-self::processing-instruction()";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Test to verify that namespace nodes are before attribute nodes in document order
        /// NS40: namespace::* | attribute::*
        /// </summary>
        [Fact]
        public static void AxesTest1266()
        {
            var xml = "name2.xml";
            var startingNodePath = "/def:store/def:booksection/bk:book";
            var testExpression = @"namespace::* | attribute::*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("def", "http://default.htm");
            namespaceManager.AddNamespace("bk", "http://book.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    HasNameTable = true,
                    Value = "http://default.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "quantity",
                    Name = "quantity",
                    HasNameTable = true,
                    Value = "100"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "inventory",
                    Name = "inventory",
                    HasNameTable = true,
                    Value = "b111"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Adhoc
        /// //NSbook:* | //NSmovie
        /// </summary>
        [Fact]
        public static void AxesTest1267()
        {
            var xml = "name.xml";
            var testExpression = @"//NSbook:* | //NSmovie:*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSbook", "http://book.htm");
            namespaceManager.AddNamespace("NSmovie", "http://movie.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "NSbook:book",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "\n\t\t\tA Brief History Of Time\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "NSbook:title",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "A Brief History Of Time"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "NSbook:book",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "\n\t\t\tThe Beautiful Universe\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "NSbook:title",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "The Beautiful Universe"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "movie",
                    Name = "NSmovie:movie",
                    NamespaceURI = "http://movie.htm",
                    HasNameTable = true,
                    Prefix = "NSmovie",
                    Value = "\n\t\t\t\n\t\t\tVeritcal Limit\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "NSmovie:title",
                    NamespaceURI = "http://movie.htm",
                    HasNameTable = true,
                    Prefix = "NSmovie",
                    Value = "Veritcal Limit"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// //NSbook:*
        /// </summary>
        [Fact]
        public static void AxesTest1268()
        {
            var xml = "name.xml";
            var testExpression = @"//NSbook:*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSbook", "http://book.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "NSbook:book",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "\n\t\t\tA Brief History Of Time\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "NSbook:title",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "A Brief History Of Time"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "NSbook:book",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "\n\t\t\tThe Beautiful Universe\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "NSbook:title",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "The Beautiful Universe"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// /bookstore/NSbook:*/NSbook:*
        /// </summary>
        [Fact]
        public static void AxesTest1269()
        {
            var xml = "name.xml";
            var testExpression = @"/bookstore/NSbook:*/NSbook:*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSbook", "http://book.htm");
            namespaceManager.AddNamespace("NSmovie", "http://movie.htm");
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// //*//*
        /// </summary>
        [Fact]
        public static void AxesTest1270()
        {
            var xml = "test1.xml";
            var testExpression = @"//*//*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "b",
                    Name = "b",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    IsEmptyElement = true,
                    LocalName = "c",
                    Name = "c",
                    HasNameTable = true
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //*//*
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void AxesTest1271()
        {
            var xml = "books.xml";
            var testExpression = @"//*//*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Joe"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "my:country",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "USA"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "12"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "JoeBob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Loser"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mike"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Hyman"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Jonathan"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Marsh"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55.95"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Road and Track"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.50"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "special_edition",
                    Name = "special_edition",
                    HasNameTable = true,
                    Value = "Yes"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Week"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "free"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Magazine"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.95"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story1",
                    Name = "story1",
                    HasNameTable = true,
                    Value = "Create a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story2",
                    Name = "story2",
                    HasNameTable = true,
                    Value =
                        "The future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Can Netscape stay alive with Microsoft eating up its browser share?"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "stock",
                    Name = "stock",
                    HasNameTable = true,
                    Value = "MSFT 99.30"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "date",
                    Name = "date",
                    HasNameTable = true,
                    Value = "1998-06-23"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story3",
                    Name = "story3",
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value = "\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story1",
                    Name = "story1",
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Frank"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Anderson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "10"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Hack"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "er"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "08"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "2.50"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "0.98"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Toni"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "B.A."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Still in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Trenton Forever"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "6.50"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "excerpt",
                    Name = "excerpt",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "emph",
                    Name = "emph",
                    HasNameTable = true,
                    Value = "I"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition-list",
                    Name = "definition-list",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "term",
                    Name = "term",
                    HasNameTable = true,
                    Value = "Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition",
                    Name = "definition",
                    HasNameTable = true,
                    Value = "misery"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "my:author",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Robert Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere is Trenton?\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where is Trenton?"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere in the world is Trenton?\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where in the world is Trenton?"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //node()//node()
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void AxesTest1272()
        {
            var xml = "books.xml";
            var testExpression = @"//node()//node()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Joe"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Joe"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "my:country",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "USA"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "USA"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "12"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "12"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "JoeBob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "JoeBob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Loser"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Loser"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "US"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mike"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mike"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Hyman"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hyman"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Jonathan"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Jonathan"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Marsh"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Marsh"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55.95"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55.95"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Road and Track"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Road and Track"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.50"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "special_edition",
                    Name = "special_edition",
                    HasNameTable = true,
                    Value = "Yes"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Yes"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Week"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Week"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "free"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "free"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Magazine"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Magazine"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.95"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.95"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story1",
                    Name = "story1",
                    HasNameTable = true,
                    Value = "Create a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Create a dream PC\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story2",
                    Name = "story2",
                    HasNameTable = true,
                    Value =
                        "The future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "The future of the web\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Can Netscape stay alive with Microsoft eating up its browser share?"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Can Netscape stay alive with Microsoft eating up its browser share?"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "stock",
                    Name = "stock",
                    HasNameTable = true,
                    Value = "MSFT 99.30"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "MSFT 99.30"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "date",
                    Name = "date",
                    HasNameTable = true,
                    Value = "1998-06-23"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "1998-06-23"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story3",
                    Name = "story3",
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value = "\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story1",
                    Name = "story1",
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Frank"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Frank"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Anderson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Anderson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "10"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "10"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Hack"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hack"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "er"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "er"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "08"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "08"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Tracking Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "2.50"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "2.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "0.98"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "0.98"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Toni"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Toni"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "B.A."
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "B.A."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Still in Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Still in Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Trenton Forever"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton Forever"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "6.50"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "6.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "excerpt",
                    Name = "excerpt",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "emph",
                    Name = "emph",
                    HasNameTable = true,
                    Value = "I"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "I"},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " have.\n\t\t\t"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition-list",
                    Name = "definition-list",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "term",
                    Name = "term",
                    HasNameTable = true,
                    Value = "Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition",
                    Name = "definition",
                    HasNameTable = true,
                    Value = "misery"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "misery"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "my:author",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Robert Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robert Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere is Trenton?\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where is Trenton?"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Where is Trenton?"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere in the world is Trenton?\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where in the world is Trenton?"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Where in the world is Trenton?"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// /*//node()
        /// </summary>
        [Fact]
        public static void AxesTest1273()
        {
            var xml = "books.xml";
            var testExpression = @"/*//node()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Joe"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Joe"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "my:country",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "USA"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "USA"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "12"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "12"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "JoeBob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "JoeBob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Loser"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Loser"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "US"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mike"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mike"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Hyman"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hyman"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Jonathan"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Jonathan"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Marsh"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Marsh"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55.95"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55.95"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Road and Track"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Road and Track"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.50"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "special_edition",
                    Name = "special_edition",
                    HasNameTable = true,
                    Value = "Yes"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Yes"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Week"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Week"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "free"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "free"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Magazine"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Magazine"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.95"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.95"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story1",
                    Name = "story1",
                    HasNameTable = true,
                    Value = "Create a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Create a dream PC\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story2",
                    Name = "story2",
                    HasNameTable = true,
                    Value =
                        "The future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "The future of the web\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Can Netscape stay alive with Microsoft eating up its browser share?"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Can Netscape stay alive with Microsoft eating up its browser share?"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "stock",
                    Name = "stock",
                    HasNameTable = true,
                    Value = "MSFT 99.30"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "MSFT 99.30"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "date",
                    Name = "date",
                    HasNameTable = true,
                    Value = "1998-06-23"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "1998-06-23"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story3",
                    Name = "story3",
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value = "\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story1",
                    Name = "story1",
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Frank"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Frank"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Anderson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Anderson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "10"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "10"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Hack"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hack"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "er"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "er"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "08"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "08"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Tracking Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "2.50"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "2.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "0.98"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "0.98"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Toni"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Toni"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "B.A."
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "B.A."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D."},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Still in Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Still in Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Trenton Forever"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton Forever"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "6.50"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "6.50"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "excerpt",
                    Name = "excerpt",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "emph",
                    Name = "emph",
                    HasNameTable = true,
                    Value = "I"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "I"},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " have.\n\t\t\t"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition-list",
                    Name = "definition-list",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "term",
                    Name = "term",
                    HasNameTable = true,
                    Value = "Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition",
                    Name = "definition",
                    HasNameTable = true,
                    Value = "misery"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "misery"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "my:author",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Robert Bob"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robert Bob"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere is Trenton?\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where is Trenton?"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Where is Trenton?"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere in the world is Trenton?\n\t"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where in the world is Trenton?"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Where in the world is Trenton?"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Regression case for 72400
        /// NSXX: namespace::* | attribute::*
        /// </summary>
        [Fact]
        public static void AxesTest1274()
        {
            var xml = "name2.xml";
            var startingNodePath = "/def:store/def:booksection/def:book";
            var testExpression = @"namespace::* | attribute::*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("def", "http://default.htm");
            namespaceManager.AddNamespace("bk", "http://book.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "NSbook",
                    Name = "NSbook",
                    HasNameTable = true,
                    Value = "http://book.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    HasNameTable = true,
                    Value = "http://default.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "quantity",
                    Name = "quantity",
                    HasNameTable = true,
                    Value = "10"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "inventory",
                    Name = "inventory",
                    HasNameTable = true,
                    Value = "b112"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Regression case for 72400
        /// NSXX: namespace::xml | attribute::inventory
        /// </summary>
        [Fact]
        public static void AxesTest1275()
        {
            var xml = "name2.xml";
            var startingNodePath = "/def:store/def:booksection/def:book";
            var testExpression = @"namespace::xml | attribute::inventory";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("def", "http://default.htm");
            namespaceManager.AddNamespace("bk", "http://book.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "inventory",
                    Name = "inventory",
                    HasNameTable = true,
                    Value = "b112"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Regression case for 72400
        /// NSXX: namespace::* | attribute::*
        /// </summary>
        [Fact]
        public static void AxesTest1276()
        {
            var xml = "ns_prefixes.xml";
            var startingNodePath = "/document/elem";
            var testExpression = @"namespace::* | attribute::*";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "prefix",
                    Name = "prefix",
                    HasNameTable = true,
                    Value = "http://prefix1.htm"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// variations/node()
        /// </summary>
        [Fact]
        public static void AxesTest1277()
        {
            var xml = "test63733.xml";
            var testExpression = @"variations/node()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.SignificantWhitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "variation",
                    Name = "variation",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.SignificantWhitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //node()
        /// </summary>
        [Fact]
        public static void AxesTest1278()
        {
            var xml = "space.xml";
            var testExpression = @"//node()";
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
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "XPath test"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
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
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "This shall test XPath test"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value =
                        "\n   XPath test\n   First paragraph  Nested  Paragraph  End of first paragraph \n   Second paragraph \n "
                },
                new XPathResultToken {NodeType = XPathNodeType.SignificantWhitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "XPath test"},
                new XPathResultToken {NodeType = XPathNodeType.SignificantWhitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "First paragraph  Nested  Paragraph  End of first paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "First paragraph "},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = " Nested  Paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " Nested "},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    IsEmptyElement = true,
                    LocalName = "Origin",
                    Name = "Origin",
                    HasNameTable = true
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " Paragraph "},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = " End of first paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.SignificantWhitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Para",
                    Name = "Para",
                    HasNameTable = true,
                    Value = "Second paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Second paragraph "},
                new XPathResultToken {NodeType = XPathNodeType.SignificantWhitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value = "\n   XPath test\n   Direct content\n "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                },
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "XPath test"},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n   Direct content\n "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //text()
        /// </summary>
        [Fact]
        public static void AxesTest1279()
        {
            var xml = "space.xml";
            var testExpression = @"//text()";
            var expected = new XPathResult(0,
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "XPath test"},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "This shall test XPath test"
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.SignificantWhitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "XPath test"},
                new XPathResultToken {NodeType = XPathNodeType.SignificantWhitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "First paragraph "},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " Nested "},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = " Paragraph "},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = " End of first paragraph "
                },
                new XPathResultToken {NodeType = XPathNodeType.SignificantWhitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Second paragraph "},
                new XPathResultToken {NodeType = XPathNodeType.SignificantWhitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true},
                new XPathResultToken {NodeType = XPathNodeType.Text, HasNameTable = true, Value = "XPath test"},
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n   Direct content\n "
                },
                new XPathResultToken {NodeType = XPathNodeType.Whitespace, HasNameTable = true});
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }
    }
}
