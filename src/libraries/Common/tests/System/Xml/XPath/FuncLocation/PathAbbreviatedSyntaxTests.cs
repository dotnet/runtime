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
    /// Location Paths - Abbreviated Syntax
    /// </summary>
    public static partial class AbbreviatedSyntaxTests
    {
        /// <summary>
        /// Find all title elements.
        /// bookstore//title
        /// </summary>
        [Fact]
        public static void AbbreviatedSyntaxTest121()
        {
            var xml = "books.xml";
            var testExpression = @"bookstore//title";
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
                    Value = "Road and Track"
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
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Magazine"
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
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Return all title children.
        /// //child::title
        /// </summary>
        [Fact]
        public static void AbbreviatedSyntaxTest122()
        {
            var xml = "books.xml";
            var testExpression = @"//child::title/text()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Road and Track" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Week" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Magazine" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Tracking Trenton" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Return all titles.
        /// bookstore//title
        /// </summary>
        [Fact]
        public static void AbbreviatedSyntaxTest123()
        {
            var xml = "books.xml";
            var testExpression = @"bookstore//title";
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
                    Value = "Road and Track"
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
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Magazine"
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
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Return all magazine elements
        /// book/./magazine
        /// </summary>
        [Fact]
        public static void AbbreviatedSyntaxTest124()
        {
            var xml = "books.xml";
            var testExpression = @"book/./magazine";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Error Case
        /// node()/.
        /// </summary>
        [Fact]
        public static void AbbreviatedSyntaxTest126()
        {
            var xml = "books.xml";
            var testExpression = @"node()/.";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = " This file represents a fragment of a book store inventory database "
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Return all children of the root node.
        /// /*
        /// </summary>
        [Fact]
        public static void AbbreviatedSyntaxTest127()
        {
            var xml = "books.xml";
            var startingNodePath = "/child::*[1]";
            var testExpression = @"/*";
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
        /// Return last child elements text node . ""Where in the world is Trenton?""
        /// (//*)[last()]/text()
        /// </summary>
        [Fact]
        public static void AbbreviatedSyntaxTest129()
        {
            var xml = "books.xml";
            var testExpression = @"(//*)[last()]/text()";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Where in the world is Trenton?"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Should only return last elements of each level (T and D).
        /// Query "//*[last()]
        /// </summary>
        [Fact]
        public static void AbbreviatedSyntaxTest1210()
        {
            var xml = "91893.xml";
            var testExpression = @"//*[last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "T",
                    Name = "T",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "D",
                    Name = "D",
                    HasNameTable = true
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Regression for 62969
        /// //*[position()!=last()]
        /// </summary>
        [Fact]
        public static void AbbreviatedSyntaxTest1211()
        {
            var xml = "91893.xml";
            var testExpression = @"//*[position()!=last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "A",
                    Name = "A",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "B",
                    Name = "B",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "C",
                    Name = "C",
                    HasNameTable = true
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Regression for 62969
        /// //*[not(last())]
        /// </summary>
        [Fact]
        public static void AbbreviatedSyntaxTest1212()
        {
            var xml = "91893.xml";
            var testExpression = @"//*[not( last())]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Regression for 62969
        /// //*[1]
        /// </summary>
        [Fact]
        public static void AbbreviatedSyntaxTest1213()
        {
            var xml = "books.xml";
            var testExpression = @"//*[1]";
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
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Joe"
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
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary"
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
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
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
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Jonathan"
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
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Week"
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
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Can Netscape stay alive with Microsoft eating up its browser share?"
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
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
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
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
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
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
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
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
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
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Hack"
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
                    LocalName = "p",
                    Name = "p",
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
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
                    LocalName = "term",
                    Name = "term",
                    HasNameTable = true,
                    Value = "Trenton"
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

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }
    }
}
