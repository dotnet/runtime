// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests
{
    /// <summary>
    /// Miscellaneous Cases
    /// </summary>
    public static partial class MiscellaneousCasesTests
    {
        /// <summary>
        /// verify that attribute nodes occur before children of a node in document order
        /// attribute nodes before children
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest531()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/magazine[6]/@frequency/following::title";
            var expected = new XPathResult(0,
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
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
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

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Throw an exception on undefined variables
        /// child::*[$$abc=1]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest532()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"child::*[$$abc=1]";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Select() should throw an exception on expression that don't have a return type of nodeset
        /// true() and true()
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest533()
        {
            var xml = "books.xml";
            var testExpression = @"true() and true()";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// Select() should throw an exception on expression that don't have a return type of nodeset
        /// false() or true()
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest534()
        {
            var xml = "books.xml";
            var testExpression = @"false() or true()";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// 1 and 1
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest535()
        {
            var xml = "books.xml";
            var testExpression = @"1 and 1";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// 1[true()]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest536()
        {
            var xml = "books.xml";
            var testExpression = @"1[true()]";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// 1
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest537()
        {
            var xml = "books.xml";
            var testExpression = @"1";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// Regression for 62694
        /// //node()[abc:xyz()]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest538()
        {
            var xml = "books.xml";
            var testExpression = @"//node()[abc:xyz()]";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// Regression for 62694
        /// //node()[abc:xyz()]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest539()
        {
            var xml = "books.xml";
            var testExpression = @"//node()[abc:xyz()]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("abc", "http://abc.htm");

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                namespaceManager: namespaceManager);
        }

        /// <summary>
        /// descendant::node()/self::node() [self::text() = false() and self::attribute=fasle()]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest5310()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant::node()/self::node() [self::text() = false() and self::attribute=fasle()]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("abc", "http://abc.htm");

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                namespaceManager: namespaceManager, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// No namespace manager provided
        /// //*[abc()]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest5311()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"//*[abc()]";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Namespace manager provided
        /// //*[abc()]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest5312()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"//*[abc()]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("abc", "http://abc.htm");

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                namespaceManager: namespaceManager, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor::*[position>2][position()=5]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest5313()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]/articles/story1/details";
            var testExpression = @"ancestor::*[position()>2][position()=5]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor::*[position>2][position()=1]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest5314()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]/articles/story1/details";
            var testExpression = @"ancestor::*[position()>2][position()=1]";
            var expected = new XPathResult(0,
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
                });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor::*[position>2][position()=3]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest5315()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]/articles/story1/details";
            var testExpression = @"ancestor::*[position()>2][position()=3]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor::*[position>2][position()=4]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest5316()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]/articles/story1/details";
            var testExpression = @"ancestor::*[position()>2][position()=4]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor::*[position>2][last()]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest5317()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]/articles/story1/details";
            var testExpression = @"ancestor::*[position()>2][last()]";
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

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor::*[position>2][position()=2]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest5318()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]/articles/story1/details";
            var testExpression = @"ancestor::*[position()>2][position()=2]";
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

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// ancestor::*[position>2]
        /// </summary>
        [Fact]
        public static void MiscellaneousCasesTest5319()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]/articles/story1/details";
            var testExpression = @"ancestor::*[position()>2]";
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
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void WhenUsingCustomXPathNavigatorDescendantIteratorShouldBeDoneAfterIterationIsComplete()
        {
            // When XPathDescendantIterator.MoveNext returns false it should keep returning false
            // This is a regression test.
            // Previously this scenario was possible to hit only when inheriting from XPathNavigator
            // and not providing custom XPathDescendantsNavigator
            string xml = "<root><a/><b/><c/><d><e/><f/></d></root>";
            var xpnav = new CustomNavigator(Utils.CreateNavigator(xml));

            var it = xpnav.SelectDescendants(XPathNodeType.All, true);
            int ret = 0;
            while (it.MoveNext())
            {
                ret++;
            }

            Assert.Equal(8, ret);
            Assert.False(it.MoveNext());
        }

        class CustomNavigator : XPathNavigator
        {
            private XPathNavigator _inner;

            public CustomNavigator(XPathNavigator inner)
            {
                _inner = inner;
            }

            private static XPathNavigator Unwrap(XPathNavigator navigator)
            {
                while (navigator is CustomNavigator custom)
                {
                    navigator = custom._inner;
                }

                return navigator;
            }

            public override XmlNameTable NameTable => _inner.NameTable;
            public override XPathNodeType NodeType => _inner.NodeType;
            public override string LocalName => _inner.LocalName;
            public override string Name => _inner.Name;
            public override string NamespaceURI => _inner.NamespaceURI;
            public override string Prefix => _inner.Prefix;
            public override string BaseURI => _inner.BaseURI;
            public override bool IsEmptyElement => _inner.IsEmptyElement;
            public override string Value => _inner.Value;
            public override XPathNavigator Clone() => new CustomNavigator(_inner.Clone());
            public override bool IsSamePosition(XPathNavigator other) => _inner.IsSamePosition(Unwrap(other));
            public override bool MoveTo(XPathNavigator other) => _inner.MoveTo(Unwrap(other));
            public override bool MoveToFirstAttribute() => _inner.MoveToFirstAttribute();
            public override bool MoveToFirstChild() => _inner.MoveToFirstChild();
            public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope) => _inner.MoveToFirstNamespace(namespaceScope);
            public override bool MoveToId(string id) => _inner.MoveToId(id);
            public override bool MoveToNext() => _inner.MoveToNext();
            public override bool MoveToNextAttribute() => _inner.MoveToNextAttribute();
            public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope) => _inner.MoveToNextNamespace(namespaceScope);
            public override bool MoveToParent() => _inner.MoveToParent();
            public override bool MoveToPrevious() => _inner.MoveToPrevious();
        }
    }
}
