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
    /// Node sets
    /// </summary>
    public static partial class NodeSetsTests
    {
        /// <summary>
        /// Trying to union 2 node-sets
        /// | can union node-sets
        /// </summary>
        [Fact]
        public static void NodeSetsTest511()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book | /bookstore/magazine";
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
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// | cannot be used with types other than node sets. Error expected
        /// /bookstore/book | 3
        /// </summary>
        [Fact]
        public static void NodeSetsTest512()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book | 3";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// Expression to be filtered is not a nodeset. It is an error if the expression to be filtered does not result in a node-set
        /// 3[3]
        /// </summary>
        [Fact]
        public static void NodeSetsTest513()
        {
            var xml = "books.xml";
            var testExpression = @"3[3]";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }
    }
}
