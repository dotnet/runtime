// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.MiscellaneousCases
{
    /// <summary>
    /// Miscellaneous Cases (regression tests)
    /// </summary>
    public static partial class RegressionTestsTests
    {
        /// <summary>
        /// translate() cannot handle surrogate pair mapping correctly
        /// </summary>
        [Fact]
        public static void RegressionTestsTest552()
        {
            var xml = "dummy.xml";
            var testExpression = "translate('+\uD800\uDF30+', '\uD800\uDF30', 'x')";
            var expected = @"+x+";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// translate() cannot handle surrogate pair mapping correctly
        /// </summary>
        [Fact]
        public static void RegressionTestsTest553()
        {
            var xml = "dummy.xml";
            var testExpression = "translate('+\uD800\uDF30+', '\uD800\uDF30', 'xy')";
            var expected = @"+xy+";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// translate() cannot handle surrogate pair mapping correctly
        /// </summary>
        [Fact]
        public static void RegressionTestsTest554()
        {
            var xml = "dummy.xml";
            var testExpression = @"translate('1.2.3.4.5.6.7.8.9.', '112233445566778899', 'axaxaxaxaxaxaxaxax')";
            var expected = @"a.a.a.a.a.a.a.a.a.";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// translate('--aaa--', 'xyz', '')
        /// </summary>
        [Fact]
        public static void RegressionTestsTest555()
        {
            var xml = "dummy.xml";
            var testExpression = @"translate('--aaa--', 'xyz', '')";
            var expected = @"--aaa--";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// translate('abc', 'abc', '')
        /// </summary>
        [Fact]
        public static void RegressionTestsTest556()
        {
            var xml = "dummy.xml";
            var testExpression = @"translate('abc', 'abc', '')";
            var expected = @"";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //namespace::*
        /// </summary>
        [Fact]
        public static void RegressionTestsTest557()
        {
            var xml = "t98598.xml";
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
                    LocalName = "foo2",
                    Name = "foo2",
                    HasNameTable = true,
                    Value = "f2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "foo1",
                    Name = "foo1",
                    HasNameTable = true,
                    Value = "f1"
                },
                new XPathResultToken { NodeType = XPathNodeType.Namespace, HasNameTable = true, Value = "f0" },
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
                    LocalName = "bar2",
                    Name = "bar2",
                    HasNameTable = true,
                    Value = "b2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "bar1",
                    Name = "bar1",
                    HasNameTable = true,
                    Value = "b1"
                },
                new XPathResultToken { NodeType = XPathNodeType.Namespace, HasNameTable = true, Value = "b0" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    LocalName = "xml",
                    Name = "xml",
                    HasNameTable = true,
                    Value = "http://www.w3.org/XML/1998/namespace"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// position()
        /// </summary>
        [Fact]
        public static void RegressionTestsTest558()
        {
            var xml = "dummy.xml";
            var testExpression = @"position()";
            var expected = 1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book[starts-with(@stype,'text')]
        /// </summary>
        [Fact]
        public static void RegressionTestsTest559()
        {
            var xml = "books.xml";
            var testExpression = @"//book[starts-with(@stype,'text')]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book[starts-with(@stype,'glo')]
        /// </summary>
        [Fact]
        public static void RegressionTestsTest5510()
        {
            var xml = "books.xml";
            var testExpression = @"//book[starts-with(@stype,'glo')]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book[starts-with(@stype,'')]
        /// </summary>
        [Fact]
        public static void RegressionTestsTest5511()
        {
            var xml = "books.xml";
            var testExpression = @"//book[starts-with(@stype,'')]";
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

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book[starts-with(@stype,' ')]
        /// </summary>
        [Fact]
        public static void RegressionTestsTest5512()
        {
            var xml = "books.xml";
            var testExpression = @"//book[starts-with(@stype,' ')]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book[starts-with(@stype,' text')]
        /// </summary>
        [Fact]
        public static void RegressionTestsTest5513()
        {
            var xml = "books.xml";
            var testExpression = @"//book[starts-with(@stype,' text')]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book[starts-with(@stype,'text ')]
        /// </summary>
        [Fact]
        public static void RegressionTestsTest5514()
        {
            var xml = "books.xml";
            var testExpression = @"//book[starts-with(@stype,'text ')]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// last()
        /// </summary>
        [Fact]
        public static void RegressionTestsTest5515()
        {
            var xml = "books.xml";
            var testExpression = @"last()";
            var expected = 1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// /child::MyComputer/descendant::UIntData/descendant::Value//@value
        /// last()
        /// </summary>
        [Fact]
        public static void RegressionTestsTest5516()
        {
            var xml = "t114730.xml";
            var testExpression = @"/child::MyComputer/descendant::UIntData/descendant::Value//@value";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Execution of XPath expressions like that compare string to node-set with arithmetic operators in context of parent expression results in NRE.
        /// </summary>
        [Fact]
        public static void RegressionTestsTest5519()
        {
            var xml = "books.xml";
            var testExpression = @"/*['6' > *]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Match() String functions inside predicate use wrong context node
        /// </summary>
        [Fact]
        public static void RegressionTestsTest5520()
        {
            var xml = "bookstore.xml";
            var testExpression = @"book[substring-before(local-name(),'store')]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Expected: Line 1 == Line 3, Actual Line 1 == Line 2 (this test cases is Line1==Line2?)
        /// count(/*[1]//node()[1]) = count(/*[1]/descendant::node()[1])
        /// </summary>
        [Fact]
        public static void RegressionTestsTest5523()
        {
            var xml = "bookstore.xml";
            var testExpression = @"count(/*[1]//node()[1]) = count(/*[1]/descendant::node()[1])";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Expected: Line 1 == Line 3, Actual Line 1 == Line 2 (this test cases is Line1==Line3?)
        /// count(/*[1]//node()[1]) = count(/*//node()[1])
        /// </summary>
        [Fact]
        public static void RegressionTestsTest5524()
        {
            var xml = "bookstore.xml";
            var testExpression = @"count(/*[1]//node()[1]) = count(/*//node()[1])";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Numeric operators are incorrectly treated as returning boolean
        /// not(0+0)
        /// </summary>
        [Fact]
        public static void RegressionTestsTest5525()
        {
            var xml = "dummy.xml";
            var testExpression = @"not(0+0)";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }
    }
}
