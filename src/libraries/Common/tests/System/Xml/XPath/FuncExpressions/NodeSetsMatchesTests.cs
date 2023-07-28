// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using XPathTests.Common;
using Xunit;

namespace XPathTests.FunctionalTests.Expressions.NodeSets
{
    /// <summary>
    /// Expressions - Node Sets (matches)
    /// </summary>
    public static partial class MatchesTests
    {
        /// <summary>
        /// Expected: True (based on context node).
        /// paraA | paraB (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest191(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Title";
            var testExpression = @"Title | Chap";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).  Fix test code to move to testexpr node, thus expected=true.
        /// paraA | paraB (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest192(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"Title | Chap";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// paraA//paraB (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest193(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para";
            var testExpression = @"Chap//Para";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// //paraA//paraB (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest194(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"//Chap//Para";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// paraA/paraB (Matches = true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest195(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"Chap/Para";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// paraA/paraB (Matches = false)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest196(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"Doc/Para";
            var expected = false;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all bar element nodes with para/bar children.
        /// para/bar[para/bar] (Use location path as expression against context)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest197(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"Chap/Para[Para/Origin]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }
    }
}
