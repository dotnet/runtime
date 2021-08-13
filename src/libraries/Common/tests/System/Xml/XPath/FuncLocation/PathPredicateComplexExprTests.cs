// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Location.Paths.Predicates
{
    /// <summary>
    /// Location Paths - Predicates (Complex Expressions)
    /// </summary>
    public static partial class ComplexExpressionsTests
    {
        /// <summary>
        /// child::*[last() or following::* | count(preceding::*)> string-length(name())]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest51()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"child::*[last() or count(following::* | preceding::*)> string-length(name())]";
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

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// descendant::*[position()&lt;last() and count(*[following::* > 3]) and local-name!="title"]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest52()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression =
                @"descendant::*[position()<last() and count(*[following::* > 3]) and local-name!=""title""] ";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// descendant::*[position()&lt;last() and count(following::*)> 3 and local-name()!="title"]
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void ComplexExpressionsTest53()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression =
                @"descendant::node()[position()<last() and count(following::*) > 3 and local-name()!=""title""] ";
            var expected = new XPathResult(0,
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "USA" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "12"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "12" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "JoeBob" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Loser"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Loser" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "US" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mike"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mike" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Hyman"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hyman" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Jonathan" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Marsh"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Marsh" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55.95" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Road and Track" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.50"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.50" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Yes" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Week" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "free"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "free" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Magazine" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.95" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "stock",
                    Name = "stock",
                    HasNameTable = true,
                    Value = "MSFT 99.30"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "MSFT 99.30" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "1998-06-23" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value = "\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Frank"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Frank" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Anderson"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Anderson" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "10"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "10" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Hack"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hack" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "er"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "er" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D." },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "08"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "08" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Tracking Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "2.50"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "2.50" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "0.98"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "0.98" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Toni"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Toni" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "B.A." },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D." },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Still in Trenton"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Still in Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Trenton Forever"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton Forever" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "6.50" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "I" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = " have.\n\t\t\t" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition-list",
                    Name = "definition-list",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "term",
                    Name = "term",
                    HasNameTable = true,
                    Value = "Trenton"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition",
                    Name = "definition",
                    HasNameTable = true,
                    Value = "misery"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "misery" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robert Bob" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Should not select anything, since there is no child element called last-name
        /// descendant::*[position()&lt;last() and count(following::*)> 3 and local-name!="title"]
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void ComplexExpressionsTest54()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression =
                @"descendant::node()[position()<last() and count(following::*) > 3 and local-name!=""title""] ";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// descendant::*/parent::*[name(parent::* )!="bookstore" and name()!="title"]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest55()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant::*/parent::*[name(parent::* )!=""bookstore"" and name()!=""title""]";
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
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
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
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value = "\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t"
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
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
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
                });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// position on node set
        /// (//node()/ancestor::node())[position()<2]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest56()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"(//node()/ancestor::node())[position()<2]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    HasChildren = true,
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Define a range for the position
        /// //node()[position()>2 and position()<7]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest57()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"//node()[position()>2 and position()<7]";
            var expected = new XPathResult(0,
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
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "12"
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
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Loser"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
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
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Hyman"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Marsh"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.50"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "free"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "stock",
                    Name = "stock",
                    HasNameTable = true,
                    Value = "MSFT 99.30"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story3",
                    Name = "story3",
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Frank"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "10"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "er"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "08"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "2.50"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "0.98"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = " have.\n\t\t\t" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition-list",
                    Name = "definition-list",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition",
                    Name = "definition",
                    HasNameTable = true,
                    Value = "misery"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
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
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Define a range for the position
        /// /descendant::text()[contains(string(),"Trenton")]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest58()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"/descendant::text()[contains(string(),""Trenton"")]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton" },
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
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Still in Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton Forever" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Where is Trenton?" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Where in the world is Trenton?"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Cascaded predicates
        /// descendant::*[count(child::*[count(child::text())>0])>0]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest59()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant::*[count(child::*[count(child::text())>0])>0]";
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

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Cascaded predicates
        /// book/child::*[child::*[following::magazine[@frequency]]]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest510()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/child::*[child::*[following::magazine[@frequency]]]";
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
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Cascaded predicates
        /// descendant::*[contains(translate(string(),"bB","aA"),"Aoa")]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest511()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant::*[contains(translate(string(),""bB"",""aA""),""Aoa"")]";
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
                    LocalName = "author",
                    Name = "my:author",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Robert Bob"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Uses number
        /// descendant::node()[number(.)>12]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest512()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant::node()[number(.)>12]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55" },
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
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55.95" },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55" });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// predicate on node set. Only one node should be selected
        /// (child::*/child::*)[last()]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest513()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"(child::*/child::*)[last()]";
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

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// predicate on location path. More than one node should be selected
        /// child::*/child::*[last()]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest514()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"child::*/child::*[last()]";
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

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Complex expression
        /// child::*[descendant::*[contains(string(),"MSFT")]]/child::price
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest515()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"child::*[descendant::*[contains(string(),""MSFT"")]]/child::price";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.95"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// node()/following-sibling::*[preceding-sibling::*[local-name()='magazine']]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest516()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @" node()/following-sibling::*[preceding-sibling::*[local-name()='magazine']]";
            var expected = new XPathResult(0,
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

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// node()[local-name()='magazine']
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest517()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @" node()[local-name()='magazine']";
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

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// node()/preceding-sibling::*[following-sibling::*[local-name()='magazine']]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest518()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @" node()/preceding-sibling::*[following-sibling::*[local-name()='magazine']]";
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
                });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// descendant-or-self::*[child::*[local-name()='magazine']]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest519()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @" descendant-or-self::*[child::*[local-name()='magazine']]";
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
        /// Predicate expression for namespace axis
        /// store/namespace::*/following::*[string-length(string())>count(child::*)][count(namespace::*)>2][not(@*[local-name()="inventory"][.="m112"])]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest520()
        {
            var xml = "name2.xml";
            var testExpression =
                @"store/namespace::*/following::*[string-length(string())>count(child::*)][count(namespace::*)>2][not(@*[local-name()=""inventory""][.=""m112""])]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate expression for namespace axis
        /// store/p1:booksection/p2:book[namespace::NSbook[following::p5:*]]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest521()
        {
            var xml = "name2.xml";
            var testExpression = @"store/p1:booksection/p2:book[namespace::NSbook[following::p5:*]]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("p1", "http://default.htm");
            namespaceManager.AddNamespace("p2", "http://book.htm");
            namespaceManager.AddNamespace("p3", "http://book2.htm");
            namespaceManager.AddNamespace("p4", "http://movie.htm");
            namespaceManager.AddNamespace("p5", "http://documentry.htm");
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// predicate on location path. More than one node should be selected
        /// (//last-name)[. != 'Bob'][position() &lt; last()]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest522()
        {
            var xml = "data4.xml";
            var testExpression = @"(//last-name)[. != 'Bob'][position() < last()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Author",
                    XmlLang = "en-usabcd"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson",
                    XmlLang = "en-usabcd"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson",
                    XmlLang = "en-usabcd"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Author of Book 3",
                    XmlLang = "en-usabcd"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson",
                    XmlLang = "en-usabcd"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson",
                    XmlLang = "en-usabcd"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// /descendant-or-self::* [position()>=1]/ancestor-or-self::*[position() <=5]/descendant::* [position()=1 or position()=2 or position()>3]/ancestor::*
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void ComplexExpressionsTest523()
        {
            var xml = "books.xml";
            var testExpression =
                @"/descendant-or-self::* [position()>=1]/ancestor-or-self::*[position() <=5]/descendant::* [position()=1 or position()=2 or position()>3]/ancestor::* ";
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
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
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

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// predicate on location path. More than one node should be selected
        /// root/foo/bar[preceding::foo/bar[2]]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest524()
        {
            var xml = "test66246.xml";
            var testExpression = @"root/foo/bar[preceding::foo/bar[2]]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "bar",
                    Name = "bar",
                    HasNameTable = true,
                    Value = "4"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "bar",
                    Name = "bar",
                    HasNameTable = true,
                    Value = "5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "bar",
                    Name = "bar",
                    HasNameTable = true,
                    Value = "6"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "bar",
                    Name = "bar",
                    HasNameTable = true,
                    Value = "7"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "bar",
                    Name = "bar",
                    HasNameTable = true,
                    Value = "8"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "bar",
                    Name = "bar",
                    HasNameTable = true,
                    Value = "9"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Regression case for 73549
        /// descendant::*[child::*[.="Joe"][following-sibling::*[.="USA"]]]
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest525()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant::*[child::*[.=""Joe""][following-sibling::*[.=""USA""]]]";
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

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }
    }
}
