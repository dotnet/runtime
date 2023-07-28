// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using XPathTests.Common;
using Xunit;

namespace XPathTests.FunctionalTests.Location.Paths.AbbreviatedSyntax
{
    /// <summary>
    /// Location Paths - Abbreviated Syntax (matches)
    /// </summary>
    public static partial class MatchesTests
    {
        /// <summary>
        /// Expected: True (based on context node).
        /// @* (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest131(Utils.NavigatorKind kind)
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"@*";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).  Fix test code to move to testexpr node, thus expected=true.
        /// @* (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest132(Utils.NavigatorKind kind)
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"@*";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// @attr (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest133(Utils.NavigatorKind kind)
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"@Attr1";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).  Fix test code to move to testexpr node, thus expected=true.
        /// @attr (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest134(Utils.NavigatorKind kind)
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"@Attr1";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// para[@attr="attrval"] (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest135(Utils.NavigatorKind kind)
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"Title[@Attr1=""value1""]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// para[@attr="attrval"] (Matches = false)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest136(Utils.NavigatorKind kind)
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"Title[@Attr1=""value2""]";
            var expected = false;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// //title
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest137(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/title";
            var testExpression = @"//title";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error, .. is not a valid pattern
        /// /bookstore/..//title
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest138(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/title";
            var testExpression = @"/bookstore/..//title";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error, . is not a valid pattern
        /// /bookstore/book/./title
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest139(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/title";
            var testExpression = @"/bookstore/book/./title";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// @frequency[.="monthly"]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest1310(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[1]/@*[2]";
            var testExpression = @"@frequency[.=""monthly""]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// @frequency[../@frequency]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest1311(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[1]/@*[2]";
            var testExpression = @"@frequency[../@frequency]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }
    }
}
