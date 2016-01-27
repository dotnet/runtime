// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Location.Paths.Predicates
{
    /// <summary>
    /// Location Paths - Predicates (Using position() Function)
    /// </summary>
    public static partial class UsingPositionFunctionTests
    {
        /// <summary>
        /// Position on the child axis
        /// child::*[position()=10]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest61()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"child::*[position()=10]";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the descendant axis
        /// descendant::node[position()=7 or position()=2]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest62()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant::node()[position()=7 or position()=2]";
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
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the descendant axis, and node set
        /// (descendant::node[position()=7 or position()=2])[last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest63()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"(descendant::node()[position()=7 or position()=2])[last()]";
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
        /// Position on the descendant axis, uses multiple predicates
        /// descendant::node()[position()&lt;7][position()=2]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest64()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant::node()[position()<7][position()=2]";
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
        /// Position on the ancestor axis, uses multiple predicates. Checks that reverse orientation is maintained on all predicates
        /// ancestor::node()[position()&lt;5][position()>2][last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest65()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author/publication/country/text()";
            var testExpression = @"ancestor::node()[position()<5][position()>2][last()]";
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
        /// Position on the ancestor axis, checks that the position works in the reverse direction
        /// ancestor::node()[position()=3]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest66()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author/publication/country/text()";
            var testExpression = @"ancestor::node()[position()=3]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the ancestor axis, checks that position() is applied to the node set formed by the location step and not the whole expression. Multiple nodes should be selected
        /// ancestor::node()/ancestor::node()[position()=last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest67()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author/publication/country/text()";
            var testExpression = @"ancestor::node()/ancestor::node()[position()=last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    HasChildren = true,
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the ancestor axis, checks that position() is applied to the node set formed by the location step and not the whole expression. Multiple nodes should be selected
        /// ancestor::node()/ancestor::node()[position()=1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest68()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author/publication/country/text()";
            var testExpression = @"ancestor::node()/ancestor::node()[position()=1]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    HasChildren = true,
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                },
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
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the descendant-or-self axis
        /// descendant-or-self::node[position()=7 or position()=2]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest69()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant-or-self::node()[position()=7 or position()=2]";
            var expected = new XPathResult(0,
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the descendant-or-self axis, uses multiple predicates
        /// descendant-or-self::node()[position()&lt;7][position()=2]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest610()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant-or-self::node()[position()<7][position()=2]";
            var expected = new XPathResult(0,
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Should select the context node
        /// descendant-or-self::node[position()=1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest611()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant-or-self::node()[position()=1]";
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
        /// Position on the ancestor-or-self axis, checks that the position works in the reverse direction
        /// ancestor-or-self::node()[position()=3]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest612()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author/publication/country/text()";
            var testExpression = @"ancestor-or-self::node()[position()=3]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the ancestor-or-self axis, uses multiple predicates. Checks that reverse orientation is maintained on all predicates
        /// ancestor-or-self::node()[position()&lt;5][position()>2][last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest613()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author/publication/country/text()";
            var testExpression = @"ancestor-or-self::node()[position()<5][position()>2][last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the ancestor-or-self axis, and node set
        /// (ancestor-or-self::node[position()=5 or position()=2])[last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest614()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author/publication/country/text()";
            var testExpression = @"(ancestor-or-self::node()[position()=5 or position()=2])[last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the following axis
        /// following::node[position()=9]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest615()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::node()[position()=9]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the following-sibling axis
        /// following-sibling::node()[position()=2]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest616()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book";
            var testExpression = @"following-sibling::node()[position()=2]";
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
        /// Position on the preceding axis, checks that the position works in the reverse direction
        /// preceding::node()[position()=6]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest617()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[5]/author";
            var testExpression = @"preceding::node()[position()=6]";
            var expected = new XPathResult(0,
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the preceding-sibling axis, checks that the position works in the reverse direction
        /// preceding-sibling::node()[position()=6]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest618()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]";
            var testExpression = @"preceding-sibling::node()[position()=6]";
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

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the attribute axis
        /// magazine/attribute::*[position()=2]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest619()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"magazine/attribute::*[position()=2]";
            var expected = new XPathResult(0,
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
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "weekly"
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
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
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
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the attribute axis, multiple predicates
        /// magazine/attribute::*[position()=1 or position()=2][position()=last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest620()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"magazine/attribute::*[position()=1 or position()=2][position()=last()]";
            var expected = new XPathResult(0,
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
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "weekly"
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
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
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
                    LocalName = "frequency",
                    Name = "frequency",
                    HasNameTable = true,
                    Value = "monthly"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the parent axis, should only have one parent
        /// parent::*[position()=1 or position()=2]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest621()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/author";
            var testExpression = @"parent::*[position()=1 or position()=2]";
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
        /// Position on the parent axis, checking for invalid values, should select nothing
        /// parent::*[position()>1 or position()<1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest622()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/author";
            var testExpression = @"parent::*[position()>1 or position()<1]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the self axis, checking for invalid values, should select nothing
        /// self::node()[position()>1 or position()<1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest623()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/author";
            var testExpression = @"self::node()[position()>1 or position()<1]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Position on the self axis, should only have one node on the self axis
        /// self::node()[position()=1 or position()=2]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest624()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/author";
            var testExpression = @"self::node()[position()=1 or position()=2]";
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
        /// Checking for position on a node set. Should always work with reference to the child axis
        /// (ancestor::*)[position()=1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest625()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/author";
            var testExpression = @"(ancestor::*)[position()=1]";
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
        /// Checking for position on a node set. Should give only one node
        /// (child::*/child::*)[position()=last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest626()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"(child::*/child::*)[position()=last()]";
            var expected = new XPathResult(0,
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
        /// Checking for position not on a node set. Should give more than one node. This is a regression for the test case above
        /// child::*/child::*[position()=last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest627()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"child::*/child::*[position()=last()]";
            var expected = new XPathResult(0,
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
                    LocalName = "special_edition",
                    Name = "special_edition",
                    HasNameTable = true,
                    Value = "Yes"
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
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value = "\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t"
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
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
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
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "08"
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
        /// Compare position with a node set in a predicate. Should first convert node set to a number.
        /// //*[position()=//n]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest628()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()=//n]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "mydoc",
                    Name = "mydoc",
                    HasNameTable = true,
                    Value = "\n\n\t1\n\t2\n\t3\n\n\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n\n\n\t5\n\tNaN\n\t0\n\n\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t2\n\t3\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t5\n\tNaN\n\t0\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "NaN"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "0"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a boolean in a predicate. Should first convert boolean to a number.
        /// //*[position()=true()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest629()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()=true()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "mydoc",
                    Name = "mydoc",
                    HasNameTable = true,
                    Value = "\n\n\t1\n\t2\n\t3\n\n\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n\n\n\t5\n\tNaN\n\t0\n\n\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t2\n\t3\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "4"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t5\n\tNaN\n\t0\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "NaN"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "0"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a boolean in a predicate. Should first convert boolean to a number.
        /// //*[position()=false()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest630()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()=false()]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a string. Should first convert string to a number.
        /// //*[position()="5"]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest631()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()=""5""]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a string. Should first convert string to a number. String should be converted to NaN and nothing should be selected.
        /// //*[position()="String"]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest632()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()=""String""]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a NaN. Nothing should be selected
        /// //*[position()=0 div 0]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest633()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()=0 div 0]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a NaN. Nothing should be selected
        /// //*[position()>0 div 0]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest634()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>0 div 0]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a NaN. Nothing should be selected
        /// //*[position()<0 div 0]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest635()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()<0 div 0]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a Infinity. Nothing should be selected
        /// //*[position()=1 div 0]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest636()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()=1 div 0]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a Infinity. All nodes should be selected
        /// //*[position()<1 div 0]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest637()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()<1 div 0]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "mydoc",
                    Name = "mydoc",
                    HasNameTable = true,
                    Value = "\n\n\t1\n\t2\n\t3\n\n\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n\n\n\t5\n\tNaN\n\t0\n\n\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t2\n\t3\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "4"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t5\n\tNaN\n\t0\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "NaN"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "0"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a Infinity. No nodes should be selected
        /// //*[position()>1 div 0]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest638()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>1 div 0]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a Infinity. No nodes should be selected
        /// //*[position()<-1 div 0]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest639()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()<-1 div 0]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a Infinity. All nodes should be selected
        /// //*[position()>-1 div 0]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest640()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>-1 div 0]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "mydoc",
                    Name = "mydoc",
                    HasNameTable = true,
                    Value = "\n\n\t1\n\t2\n\t3\n\n\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n\n\n\t5\n\tNaN\n\t0\n\n\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t2\n\t3\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "4"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t5\n\tNaN\n\t0\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "NaN"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "0"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Compare position with a Infinity. Nothing should be selected
        /// //*[position()=-1 div 0]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest641()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()=-1 div 0]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Use position with a range of numbers
        /// //*[position()>1 and position()<20]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest642()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>1 and position()<20]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "4"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t5\n\tNaN\n\t0\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "NaN"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "0"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Use position with a range of numbers
        /// //*[position()>NaN and position()<20]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest643()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>0 div 0 and position()<20]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Use position with a range of numbers
        /// //*[position()>0 and position()<NaN]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest644()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>0 and position()<0 div 0]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Use position with a range of numbers
        /// //*[position()>Infinity and position()<20]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest645()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>1 div 0 and position()<20]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Use position with a range of numbers
        /// //*[position()>-Infinity and position()<20]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest646()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>-1 div 0 and position()<20]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "mydoc",
                    Name = "mydoc",
                    HasNameTable = true,
                    Value = "\n\n\t1\n\t2\n\t3\n\n\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n\n\n\t5\n\tNaN\n\t0\n\n\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t2\n\t3\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "4"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t5\n\tNaN\n\t0\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "NaN"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "0"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Use position with a range of numbers
        /// //*[position()>0 and position()<Infinity]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest647()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>0 and position()<1 div 0]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "mydoc",
                    Name = "mydoc",
                    HasNameTable = true,
                    Value = "\n\n\t1\n\t2\n\t3\n\n\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n\n\n\t5\n\tNaN\n\t0\n\n\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t2\n\t3\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "4"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t5\n\tNaN\n\t0\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "NaN"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "0"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Use position with a range of numbers
        /// //*[position()>0 and position()<-Infinity]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest648()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>0 and position()<-1 div 0]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Use position with a range of numbers. Selects all nodes
        /// //*[position()>1 or position()<20]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest649()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>1 or position()<20]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "mydoc",
                    Name = "mydoc",
                    HasNameTable = true,
                    Value = "\n\n\t1\n\t2\n\t3\n\n\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n\n\n\t5\n\tNaN\n\t0\n\n\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t2\n\t3\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "4"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t5\n\tNaN\n\t0\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "NaN"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "0"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Use position with a range of numbers. Selects all nodes
        /// //*[position()>-1 div 0 or position()<1 div 0]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest650()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>-1 div 0 or position()<1 div 0]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "mydoc",
                    Name = "mydoc",
                    HasNameTable = true,
                    Value = "\n\n\t1\n\t2\n\t3\n\n\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n\n\n\t5\n\tNaN\n\t0\n\n\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t2\n\t3\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "4"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t5\n\tNaN\n\t0\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "NaN"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "0"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Use position with a range of numbers. Selects all nodes. In Old Xpath it is a bug that we convert the string ""Infinity"" to the really infinity. XPath spec under Number function states that only number in string are converted back to number all other string should be converted to NaN.
        /// //*[position()>"-Infinity" or position()<"Infinity"]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest651()
        {
            var xml = "numbers.xml";
            var testExpression = @"//*[position()>""-Infinity"" or position()<""Infinity""]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "mydoc",
                    Name = "mydoc",
                    HasNameTable = true,
                    Value = "\n\n\t1\n\t2\n\t3\n\n\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n\n\n\t5\n\tNaN\n\t0\n\n\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t2\n\t3\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t1\n\t1\n\t2\n\t2\n\t3\n\t4\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "4"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "numbers",
                    Name = "numbers",
                    HasNameTable = true,
                    Value = "\n\t5\n\tNaN\n\t0\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "NaN"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "0"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Use position on a large selection
        /// (//node() | //attribute::*) [(position() >2 and position()<7)or (position() >8 and position()<16) or position()=last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest652()
        {
            var xml = "books.xml";
            var testExpression =
                @"(//node() | //attribute::*)[(position() >2 and position()<7)or (position() >8 and position()<16) or position()=last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "specialty",
                    Name = "specialty",
                    HasNameTable = true,
                    Value = "novel"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "style",
                    Name = "style",
                    HasNameTable = true,
                    Value = "autobiography"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Joe"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Joe" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Use position in a mathematical expression
        /// position()+last() div 13
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest653()
        {
            var xml = "numbers.xml";
            var startingNodePath = "//n";
            var testExpression = @"position()+last() div 13";
            var expected = 1.0769230769230769d;

            Utils.XPathNumberTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Use position in a boolean expression
        /// position()>5
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest654()
        {
            var xml = "numbers.xml";
            var startingNodePath = "//n";
            var testExpression = @"position()>5";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Use position in a boolean expression
        /// position()=0
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest655()
        {
            var xml = "numbers.xml";
            var startingNodePath = "//n";
            var testExpression = @"position()=0";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Use position in a string
        /// concat("The current position"," = ",position())
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest656()
        {
            var xml = "numbers.xml";
            var startingNodePath = "//n";
            var testExpression = @"concat(""The current position"","" = "",position())";
            var expected = @"The current position = 1";

            Utils.XPathStringTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Use position in query
        /// //n [. = position()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest657()
        {
            var xml = "numbers.xml";
            var startingNodePath = "//n";
            var testExpression = @"//n [. = position()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "n",
                    Name = "n",
                    HasNameTable = true,
                    Value = "1"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Complex expression involving position. Expected : Select the bookstore node
        /// descendant-or-self::node()[position() = ceiling(number(string(child::*[descendant::*[contains(string(),'MSFT')]]/child::price))) div round(19.123 mod 5)]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest658()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression =
                @"descendant-or-self::node()[position() = ceiling(number(string(child::*[descendant::*[contains(string(),'MSFT')]]/child::price))) div round(19.123 mod 5)]";
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
        /// Complex expression involving position. Uses multiple predicates Expected : Should select bookstore
        /// ancestor-or-self::*[following::price[.="free"]][ancestor::bookstore][child::title][position() = 1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest659()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]//story3";
            var testExpression =
                @"ancestor-or-self::*[following::price[.=""free""]][ancestor-or-self::bookstore][descendant::title][position() = 1]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Complex expression involving position. Uses multiple predicates. Expected : Should select 2 magazine nodes
        /// preceding::*[following::price[.="free"]][ancestor::bookstore][child::title][position() = 1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest660()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]//story3";
            var testExpression =
                @"preceding::*[following::price[.=""free""]][ancestor::bookstore][child::title][position() = 1]";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Selects every node that is the last child of it's parent
        /// /descendant-or-self/child::node()[position()=last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest661()
        {
            var xml = "books.xml";
            var testExpression = @"/descendant-or-self::node()/child::node()[position()=last()]";
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
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Joe" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "USA" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "12" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "JoeBob" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Loser" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "US" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mike" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hyman" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Jonathan" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Marsh" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55.95" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Road and Track" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.50" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Yes" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Week" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "free" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Magazine" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.95" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Can Netscape stay alive with Microsoft eating up its browser share?"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "MSFT 99.30" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "1998-06-23" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Frank" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Anderson" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "10" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hack" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "er" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D." },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "08" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Tracking Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "2.50" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "0.98" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Toni" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "B.A." },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D." },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Still in Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton Forever" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "6.50" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "I" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = " have.\n\t\t\t" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "misery" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robert Bob" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Where is Trenton?" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Where in the world is Trenton?"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Position in multiple predicates
        /// /descendant-or-self::node()[position()> 3 and position() &lt; 30]/child::node()[position()&lt;last() and position()&lt;30 and position()!=13]/preceding::*[position()>=2 and position()&lt;= last() - 2][position()>2][position()>2]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest662()
        {
            var xml = "books.xml";
            var testExpression =
                @"/descendant-or-self::node()[position()> 3 and position() < 30]/child::node()[position()<last() and position()<30 and position()!=13]/preceding::*[position()>=2 and position()<= last() - 2][position()>2][position()>2]";
            var expected = new XPathResult(0,
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Position in cascaded predicates
        /// //*[position()=2 and //*[position()=3 and //*[position()=2]]]
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void UsingPositionFunctionTest663()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"//*[position()=2 and //*[position()=3 and //*[position()=2]]]";
            var expected = new XPathResult(0,
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
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
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
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Hyman"
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
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.50"
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
                    LocalName = "stock",
                    Name = "stock",
                    HasNameTable = true,
                    Value = "MSFT 99.30"
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
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
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
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
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
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
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
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "2.50"
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
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
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
                    LocalName = "definition",
                    Name = "definition",
                    HasNameTable = true,
                    Value = "misery"
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// book[position()!= 1 and position()!=2 and position()!=3 and position()!=4 and position()!=5 and position()!=6 and position()!=7]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest664()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression =
                @"book[position()!= 1 and position()!=2 and position()!=3 and position()!=4 and position()!=5 and position()!=6 and position()!=7]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// book[position() < 2.999998999]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest665()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[position()<2.999998999]";
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
        /// book/title/text()/ancestor::*[position()=1 or position()=2][position()=1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest666()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/title/text()/ancestor::*[position()=1 or position()=2][position()=1]";
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
        /// namespace::*[last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest667()
        {
            var xml = "name2.xml";
            var startingNodePath = "def:store/def:booksection/bk:book";
            var testExpression = @"namespace::*[last()]";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// //namespace::*[position()<last()][position()>1][position()=last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest668()
        {
            var xml = "name2.xml";
            var testExpression = @"//namespace::*[position()<last()][position()>1][position()=last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    HasNameTable = true,
                    Value = "http://default.htm"
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
                    HasNameTable = true,
                    Value = "http://default.htm"
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
                    HasNameTable = true,
                    Value = "http://default.htm"
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
                    HasNameTable = true,
                    Value = "http://default.htm"
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
                    HasNameTable = true,
                    Value = "http://default.htm"
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
                    HasNameTable = true,
                    Value = "http://default.htm"
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
                    HasNameTable = true,
                    Value = "http://default.htm"
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
                    HasNameTable = true,
                    Value = "http://default.htm"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Test to verify that namespace nodes are before attribute nodes in document order
        /// (namespace::* | attribute::*)[position()=2]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest669()
        {
            var xml = "name2.xml";
            var startingNodePath = "def:store/def:booksection/bk:book";
            var testExpression = @"(namespace::* | attribute::*)[position()=2]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("def", "http://default.htm");
            namespaceManager.AddNamespace("bk", "http://book.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Namespace,
                    HasNameTable = true,
                    Value = "http://default.htm"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding::*[position()>2][position()>2][position()<last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest670()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[3]/author/last.name";
            var testExpression = @"preceding::*[position()>2][position()>2][position()<last()]";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding-sibling::*[position()&gt;2][position()&gt;1][position()&lt;last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest671()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/*[last()]";
            var testExpression = @"preceding-sibling::*[position()>2][position()>1][position()<last()]";
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
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor::node()[position()&lt;5][position()>2][last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest672()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author/publication/country/text()";
            var testExpression = @"ancestor::node()[position()<5][position()>2][last()]";
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
        /// //title/text()/ancestor::*[name()='book'][position()=1][last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest673()
        {
            var xml = "books.xml";
            var testExpression = @"//title/text()/ancestor::*[name()='book'][position()=1][last()]";
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
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //title/text()/ancestor-or-self::*[name()='book'][position()=1][last()]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest674()
        {
            var xml = "books.xml";
            var testExpression = @"//title/text()/ancestor-or-self::*[name()='book'][position()=1][last()]";
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
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //title/text()/ancestor::*[name()='book'][position()=1][following::magazine]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest675()
        {
            var xml = "books.xml";
            var testExpression = @"//title/text()/ancestor::*[name()='book'][position()=1][following::magazine]";
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

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// preceding::*/child::text()[position()=1 or position()=last()][1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest676()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[6]";
            var testExpression = @"preceding::*/child::text()[position()=1 or position()=last()][1]";
            var expected = new XPathResult(0,
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Joe" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "USA" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "12" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "JoeBob" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Loser" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "US" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mike" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hyman" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Jonathan" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Marsh" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55.95" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Road and Track" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.50" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Yes" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Week" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "free" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Magazine" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.95" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "MSFT 99.30" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "1998-06-23" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Frank" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Anderson" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "10" });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// preceding-sibling::*/self::*[position()=1][position()=last()][1][child::title]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest677()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[6]";
            var testExpression = @"preceding-sibling::*/self::*[position()=1][position()=last()][1][child::title]";
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// //book/descendant::*[position()=1][position()=last()][1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest678()
        {
            var xml = "books.xml";
            var testExpression = @"//book/descendant::*[position()=1][position()=last()][1]";
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

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book/descendant-or-self::*[position()=1][position()=last()][1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest679()
        {
            var xml = "books.xml";
            var testExpression = @"//book/descendant-or-self::*[position()=1][position()=last()][1]";
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
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book/descendant-or-self::*/parent[position()=1][position()=last()][1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest680()
        {
            var xml = "books.xml";
            var testExpression = @"//book/descendant-or-self::*/parent[position()=1][position()=last()][1]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book/namespace::*/parent[position()=1][position()=last()][1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest681()
        {
            var xml = "books.xml";
            var testExpression = @"//book/namespace::*/parent::*[position()=1][position()=last()][1]";
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
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book/following::*/parent::*[position()=1][position()=last()][1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest682()
        {
            var xml = "books.xml";
            var testExpression = @"//book/following::*/parent::*[position()=1][position()=last()][1]";
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
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
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
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t"
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
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
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
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
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
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
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
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t"
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
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere in the world is Trenton?\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book/following::*[position()=1][position()=last()][1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest683()
        {
            var xml = "books.xml";
            var testExpression = @"//book/following::*[position()=1][position()=last()][1]";
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
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book/following-sibling::*/parent::*[position()=1][position()=last()][1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest684()
        {
            var xml = "books.xml";
            var testExpression = @"//book/following-sibling::*/parent::*[position()=1][position()=last()][1]";
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

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book/following-sibling::*[position()=1][position()=last()][1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest685()
        {
            var xml = "books.xml";
            var testExpression = @"//book/following-sibling::*[position()=1][position()=last()][1]";
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
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book/preceding::*[position()=1][position()=last()][1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest686()
        {
            var xml = "books.xml";
            var testExpression = @"//book/preceding::*[position()=1][position()=last()][1]";
            var expected = new XPathResult(0,
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
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
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
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
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
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// //book/preceding-sibling::*[position()=1][position()=last()][1]
        /// </summary>
        [Fact]
        public static void UsingPositionFunctionTest687()
        {
            var xml = "books.xml";
            var testExpression = @"//book/preceding-sibling::*[position()=1][position()=last()][1]";
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
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }
    }
}
