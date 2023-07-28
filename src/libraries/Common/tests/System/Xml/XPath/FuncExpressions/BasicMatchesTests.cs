// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using XPathTests.Common;
using Xunit;

namespace XPathTests.FunctionalTests.Expressions.Basics
{
    /// <summary>
    /// Expressions - Basics (matches)
    /// </summary>
    public static partial class MatchesTests
    {
        /// <summary>
        /// Expected: Selects the 1st element child of the context node.
        /// child::*[((((1 + 2) * 3) - 7) div 2)]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest171(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/child::*[1]";
            var testExpression = @"child::*[((((1 + 2) * 3) - 7) div 2)]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// child::*[(2 * (2 -  (3 div (1 + 2))))]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest172(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/child::*[2]";
            var testExpression = @"child::*[(2 * (2 -  (3 div (1 + 2))))]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 1st element child of the context node.
        /// child::*[(4 - 1) div  (1 + 2)]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest173(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/child::*[1]";
            var testExpression = @"child::*[(4 - 1) div  (1 + 2)]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Set and get variable. Expected: Error (variables are only supported for XSL/T)
        /// child::$$Var
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest174(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"child::$$Var";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                startingNodePath: startingNodePath);
        }
    }
}
