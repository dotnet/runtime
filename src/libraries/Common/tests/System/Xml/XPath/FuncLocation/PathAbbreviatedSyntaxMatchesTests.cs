// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

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
        [Fact]
        public static void MatchesTest131()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"@*";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).  Fix test code to move to testexpr node, thus expected=true.
        /// @* (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest132()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"@*";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// @attr (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest133()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"@Attr1";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).  Fix test code to move to testexpr node, thus expected=true.
        /// @attr (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest134()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"@Attr1";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// para[@attr="attrval"] (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest135()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"Title[@Attr1=""value1""]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// para[@attr="attrval"] (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest136()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"Title[@Attr1=""value2""]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// //title
        /// </summary>
        [Fact]
        public static void MatchesTest137()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/title";
            var testExpression = @"//title";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error, .. is not a valid pattern
        /// /bookstore/..//title
        /// </summary>
        [Fact]
        public static void MatchesTest138()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/title";
            var testExpression = @"/bookstore/..//title";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error, . is not a valid pattern
        /// /bookstore/book/./title
        /// </summary>
        [Fact]
        public static void MatchesTest139()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/title";
            var testExpression = @"/bookstore/book/./title";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// @frequency[.="monthly"]
        /// </summary>
        [Fact]
        public static void MatchesTest1310()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[1]/@*[2]";
            var testExpression = @"@frequency[.=""monthly""]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// @frequency[../@frequency]
        /// </summary>
        [Fact]
        public static void MatchesTest1311()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[1]/@*[2]";
            var testExpression = @"@frequency[../@frequency]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }
    }
}
