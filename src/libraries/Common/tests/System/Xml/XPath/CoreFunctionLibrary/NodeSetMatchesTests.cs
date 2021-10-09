// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.CoreFunctionLibrary.NodeSetFunctions
{
    /// <summary>
    /// Core Function Library - Node Set Functions (matches)
    /// </summary>
    public static partial class MatchesTests
    {
        /// <summary>
        /// Expected: Selects the last element child of the context node.
        /// child::*[last()]
        /// </summary>
        [Fact]
        public static void MatchesTest231()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child5";
            var testExpression = @"child::*[last()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the second last element child of the context node.
        /// child::*[last() - 1]
        /// </summary>
        [Fact]
        public static void MatchesTest232()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child4";
            var testExpression = @"child::*[last() - 1]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the last attribute node of the context node.
        /// attribute::*[last()]
        /// </summary>
        [Fact]
        public static void MatchesTest233()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr5";
            var testExpression = @"attribute::*[last()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the second last attribute node of the context node.
        /// attribute::*[last() - 1]
        /// </summary>
        [Fact]
        public static void MatchesTest234()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"attribute::*[last() - 1]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Attribute,
                    LocalName = "Attr4",
                    Name = "Attr4",
                    HasNameTable = true,
                    Value = "Fourth"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the last element child of the context node.
        /// child::*[position() = last()]
        /// </summary>
        [Fact]
        public static void MatchesTest235()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child5";
            var testExpression = @"child::*[position() = last()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// *[position() = last()] (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest236()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child5";
            var testExpression = @"*[position() = last()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// *[position() = last()] (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest237()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child4";
            var testExpression = @"*[position() = last()]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// @*[position() = last()] (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest238()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr5";
            var testExpression = @"@*[position() = last()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// @*[position() = last()] (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest239()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr4";
            var testExpression = @"@*[position() = last()]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// *[last() = 1] (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest2310()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test2/Child1";
            var testExpression = @"*[last() = 1]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// *[last() = 1] (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest2311()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child1";
            var testExpression = @"*[last() = 1]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// child::*[position() = 2]
        /// </summary>
        [Fact]
        public static void MatchesTest2312()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child2";
            var testExpression = @"child::*[position() = 2]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd attribute of the context node.
        /// attribute::*[position() = 2]
        /// </summary>
        [Fact]
        public static void MatchesTest2313()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr2";
            var testExpression = @"attribute::*[position() = 2]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// child::*[2]
        /// </summary>
        [Fact]
        public static void MatchesTest2314()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child2";
            var testExpression = @"child::*[2]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd attribute of the context node.
        /// attribute::*[2]
        /// </summary>
        [Fact]
        public static void MatchesTest2315()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr2";
            var testExpression = @"attribute::*[2]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all element children of the context node except the first two.
        /// child::*[position() > 2]
        /// </summary>
        [Fact]
        public static void MatchesTest2316()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child4";
            var testExpression = @"child::*[position() > 2]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects all element children of the context node.
        /// child::*[position()]
        /// </summary>
        [Fact]
        public static void MatchesTest2317()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child3";
            var testExpression = @"child::*[position()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// *[position() = 2] (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest2318()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child2";
            var testExpression = @"*[position() = 2]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// *[position() = 2] (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest2319()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child3";
            var testExpression = @"*[position() = 2]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// @*[position() = 2] (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest2320()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr2";
            var testExpression = @"@*[position() = 2]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// @*[position() = 2] (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest2321()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr3";
            var testExpression = @"@*[position() = 2]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// *[2] (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest2322()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child2";
            var testExpression = @"*[2]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// *[2] (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest2323()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child3";
            var testExpression = @"*[2]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// @*[2] (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest2324()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr2";
            var testExpression = @"@*[2]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// @*[2] (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest2325()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr1";
            var testExpression = @"@*[2]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// *[position() > 2] (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest2326()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child3";
            var testExpression = @"*[position() > 2]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// *[position() > 2] (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest2327()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child2";
            var testExpression = @"*[position() > 2]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// @*[position() > 2] (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest2328()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr3";
            var testExpression = @"@*[position() > 2]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// @*[position() > 2] (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest2329()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/@Attr2";
            var testExpression = @"@*[position() > 2]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

#if FEATURE_XML_XPATH_ID
        /// <summary>
        /// Data file has no DTD, so no element has an ID, expected empty node-set
        /// id("1")
        /// </summary>
        [Fact]
        public static void MatchesTest2352()
        {
            var xml = "id4.xml";
            var startingNodePath = "/DMV/Vehicle[1]";
            var testExpression = @"id(""1"")";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }
#endif
    }
}
