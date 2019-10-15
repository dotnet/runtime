// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.CustomerScenarios
{
    /// <summary>
    /// Customer Scenarios (matches)
    /// </summary>
    public static partial class MatchesTests
    {
        /// <summary>
        /// Expected: The last line element of the first section of the last chapter of the context node.
        /// Chapter[last()]/Section[1]/Line[last()]
        /// </summary>
        [Fact]
        public static void MatchesTest311()
        {
            var xml = "xpC001.xml";
            var startingNodePath = "/Book/Chapter[3]/Section[1]/Line[3]";
            var testExpression = @"Chapter[last()]/Section[1]/Line[last()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: The last line element of the first section of the last chapter of the doc.
        /// /Book/Chapter[last()]/Section[1]/Line[last()]
        /// </summary>
        [Fact]
        public static void MatchesTest312()
        {
            var xml = "xpC001.xml";
            var startingNodePath = "/Book/Chapter[3]/Section[1]/Line[3]";
            var testExpression = @"/Book/Chapter[last()]/Section[1]/Line[last()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the second chapter element child if that child has a name attribute with value ""Chapter2"".
        /// Chapter[2][@name="Chapter2"]
        /// </summary>
        [Fact]
        public static void MatchesTest313()
        {
            var xml = "xpC001.xml";
            var startingNodePath = "/Book/Chapter[2]";
            var testExpression = @"Chapter[2][@name='Entrees']";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: The 2nd to 4th chapter child node of the context node.
        /// Chapter[position() >= 2 and position() <= 4]
        /// </summary>
        [Fact]
        public static void MatchesTest314()
        {
            var xml = "xpC001.xml";
            var startingNodePath = "/Book/Chapter[3]";
            var testExpression = @"Chapter[position() >= 2 and position() <= 4]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }
    }
}
