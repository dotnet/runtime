// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Location.Paths.NodeTests
{
    /// <summary>
    /// Location Paths - Node Tests (matches)
    /// </summary>
    public static partial class MatchesTests
    {
        /// <summary>
        /// Expected: True (based on context node).
        /// text() (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest91()
        {
            var xml = "xp003.xml";
            var startingNodePath = "//text()[1]";
            var testExpression = @"text()";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).  Fix test code to move to testexpr node, thus expected=true.
        /// text() (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest92()
        {
            var xml = "xp003.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"text()";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// comment() (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest93()
        {
            var xml = "xp003.xml";
            var startingNodePath = "/Doc/comment()";
            var testExpression = @"comment()";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// comment() (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest94()
        {
            var xml = "xp003.xml";
            var startingNodePath = "/Doc/comment()";
            var testExpression = @"comment()";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// node() (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest95()
        {
            var xml = "xp003.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"node()";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// node() (Matches = false, attribute node)
        /// </summary>
        [Fact]
        public static void MatchesTest96()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"node()";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).  Fix test code to move to testexpr node, thus expected=true.
        /// node() (Matches = true, root node)
        /// </summary>
        [Fact]
        public static void MatchesTest97()
        {
            var xml = "xp002.xml";
            var testExpression = @"node()";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Expected: Selects all PI node children of the context node.
        /// descendant::processing-instruction() (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest98()
        {
            var xml = "xp003.xml";
            var startingNodePath = "/processing-instruction()[1]";
            var testExpression = @"processing-instruction()";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all PI node children of the context node.
        /// descendant::processing-instruction() (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest99()
        {
            var xml = "xp003.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"processing-instruction()";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// para (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest910()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"Para";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).  Fix test code to move to testexpr node, thus expected=true.
        /// para (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest911()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"Para";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all PI node children of the context node with name = ""PI1"".
        /// processing-instruction('PI1') (true)
        /// </summary>
        [Fact]
        public static void MatchesTest912()
        {
            var xml = "xp003.xml";
            var startingNodePath = "//node()[1]";
            var testExpression = @"processing-instruction('PI1')";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all PI node children of the context node with name = ""PI1"".
        /// processing-instruction('PI1') (false)
        /// </summary>
        [Fact]
        public static void MatchesTest913()
        {
            var xml = "xp003.xml";
            var startingNodePath = "//node()[2]";
            var testExpression = @"processing-instruction('PI1')";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }
    }
}
