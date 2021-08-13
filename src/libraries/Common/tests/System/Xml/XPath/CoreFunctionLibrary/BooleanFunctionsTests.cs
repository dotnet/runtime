// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.CoreFunctionLibrary
{
    /// <summary>
    /// Core Function Library - Boolean Functions
    /// </summary>
    public static partial class BooleanFunctionsTests
    {
        /// <summary>
        /// Verify result.
        /// boolean(1) = true
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest251()
        {
            var xml = "dummy.xml";
            var testExpression = @"boolean(1)";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// boolean(0) = false
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest252()
        {
            var xml = "dummy.xml";
            var testExpression = @"boolean(0)";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// boolean(infinity) = true
        /// boolean(1 div 0) = true
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest253()
        {
            var xml = "dummy.xml";
            var testExpression = @"boolean(1 div 0)";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// boolean(NaN) = false
        /// boolean(0 div 0) = false
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest254()
        {
            var xml = "dummy.xml";
            var testExpression = @"boolean(0 div 0)";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// boolean(-0) = false
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest255()
        {
            var xml = "dummy.xml";
            var testExpression = @"boolean(-0)";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// boolean(2.5) = true
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest256()
        {
            var xml = "dummy.xml";
            var testExpression = @"boolean(2.5)";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// boolean("test") = true
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest257()
        {
            var xml = "dummy.xml";
            var testExpression = @"boolean(""Test"")";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// boolean("") = false
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest258()
        {
            var xml = "dummy.xml";
            var testExpression = @"boolean("""")";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// boolean(child::*) = true
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest259()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"boolean(child::*)";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// boolean(child::DoesNotExist) = false
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest2510()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"boolean(child::DoesNotExist)";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// not(false()) = true
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest2511()
        {
            var xml = "dummy.xml";
            var testExpression = @"not(false())";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// not(true()) = false
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest2512()
        {
            var xml = "dummy.xml";
            var testExpression = @"not(true())";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// not(boolean(child::*)) = false
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest2513()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"not(boolean(child::*))";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// true() = true
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest2514()
        {
            var xml = "dummy.xml";
            var testExpression = @"true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// false() = false
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest2515()
        {
            var xml = "dummy.xml";
            var testExpression = @"false()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// expected true
        /// lang("en") context node has xml:lang="en"
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest2516()
        {
            var xml = "lang.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"lang(""en"")";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// expected true
        /// lang("en") ancestor has xml:lang = "en"
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest2517()
        {
            var xml = "lang.xml";
            var startingNodePath = "/bookstore/book[2]";
            var testExpression = @"lang(""en"")";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// expected false
        /// lang("en-us")  is a sub-category of xml-lang="en"
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest2518()
        {
            var xml = "lang.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"lang(""en-us"")";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// expected true
        /// lang("en") xml:lang = "en-us" is a sub category
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest2519()
        {
            var xml = "lang.xml";
            var startingNodePath = "/bookstore/book[3]";
            var testExpression = @"lang(""en"")";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// lang() should match ignoring case, expected : true
        /// lang("EN") context node has xml:lang = "en"
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest2520()
        {
            var xml = "lang.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"lang(""EN"")";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Code Coverage: Covers the case where lang() is used in an expression
        /// child::*[lang("en")]
        /// </summary>
        [Fact]
        public static void BooleanFunctionsTest2521()
        {
            var xml = "lang.xml";
            var testExpression = @"child::*[lang(""en"")]";
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
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n",
                    XmlLang = "en"
                });

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }
    }
}
