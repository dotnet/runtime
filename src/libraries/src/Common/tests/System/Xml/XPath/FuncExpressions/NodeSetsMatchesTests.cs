// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

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
        [Fact]
        public static void MatchesTest191()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Title";
            var testExpression = @"Title | Chap";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).  Fix test code to move to testexpr node, thus expected=true.
        /// paraA | paraB (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest192()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"Title | Chap";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// paraA//paraB (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest193()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para";
            var testExpression = @"Chap//Para";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// //paraA//paraB (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest194()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"//Chap//Para";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// paraA/paraB (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest195()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"Chap/Para";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// paraA/paraB (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest196()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"Doc/Para";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all bar element nodes with para/bar children.
        /// para/bar[para/bar] (Use location path as expression against context)
        /// </summary>
        [Fact]
        public static void MatchesTest197()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"Chap/Para[Para/Origin]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }
    }
}
