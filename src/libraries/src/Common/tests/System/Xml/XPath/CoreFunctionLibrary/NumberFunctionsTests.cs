// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.CoreFunctionLibrary
{
    /// <summary>
    /// Core Function Library - Number Functions
    /// </summary>
    public static partial class NumberFunctionsTests
    {
        /// <summary>
        /// Verify result.
        /// number("1") = 1
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest261()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"")";
            var expected = 1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// number("-1") = -1
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest262()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""-1"")";
            var expected = -1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// number(" -2 ") = -2
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest263()
        {
            var xml = "dummy.xml";
            var testExpression = @"number("" -2"")";
            var expected = -2d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// number("- 1") = NaN
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest264()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""- 1"")";
            var expected = double.NaN;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// number("0.1234") = 0.1234
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest265()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""0.1234"")";
            var expected = 0.1234d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// number("test") = NaN
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest266()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""test"")";
            var expected = double.NaN;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// number(true()) = 1
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest267()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(true())";
            var expected = 1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// number(false()) = 0
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest268()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(false())";
            var expected = 0d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// number(child::*)
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest269()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"number(child::*)";
            var expected = 10d;

            Utils.XPathNumberTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// number(2) = 2
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2610()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(2)";
            var expected = 2d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result (Assuming that there are 3 element children with the string values ""1"", ""2"" and ""3"").
        /// sum(child::*)
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2611()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"sum(child::*)";
            var expected = 60d;

            Utils.XPathNumberTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result (Assuming that there are 3 element children with the string values ""1"", ""2"" and ""not a number"").
        /// sum(child::*) = NaN
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2612()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"sum(child::*)";
            var expected = double.NaN;

            Utils.XPathNumberTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// floor(2.9) = 2
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2613()
        {
            var xml = "dummy.xml";
            var testExpression = @"floor(2.9)";
            var expected = 2d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// floor(2.1) = 2
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2614()
        {
            var xml = "dummy.xml";
            var testExpression = @"floor(2.1)";
            var expected = 2d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// floor(0.9) = 0
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2615()
        {
            var xml = "dummy.xml";
            var testExpression = @"floor(0.9)";
            var expected = 0d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// floor(-2.9) = -3
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2616()
        {
            var xml = "dummy.xml";
            var testExpression = @"floor(-2.9)";
            var expected = -3d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// floor(number(child::*))
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2617()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test5";
            var testExpression = @"floor(number(child::Para[1]))";
            var expected = 2d;

            Utils.XPathNumberTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// ceiling(2.9) = 3
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2618()
        {
            var xml = "dummy.xml";
            var testExpression = @"ceiling(2.9)";
            var expected = 3d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// ceiling(2.1) = 3
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2619()
        {
            var xml = "dummy.xml";
            var testExpression = @"ceiling(2.1)";
            var expected = 3d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// ceiling(0.1) = 1
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2620()
        {
            var xml = "dummy.xml";
            var testExpression = @"ceiling(0.1)";
            var expected = 1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// ceiling(-2.9) = -2
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2621()
        {
            var xml = "dummy.xml";
            var testExpression = @"ceiling(-2.9)";
            var expected = -2d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// ceiling(number(child::*))
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2622()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test5";
            var testExpression = @"ceiling(number(child::Para[2]))";
            var expected = 3d;

            Utils.XPathNumberTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// round(2.9) = 3
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2623()
        {
            var xml = "dummy.xml";
            var testExpression = @"round(2.9)";
            var expected = 3d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// round(2.1) = 2
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2624()
        {
            var xml = "dummy.xml";
            var testExpression = @"round(2.1)";
            var expected = 2d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// round(2.5) = 3
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2625()
        {
            var xml = "dummy.xml";
            var testExpression = @"round(2.5)";
            var expected = 3d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// round(-2.5) = -2
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2626()
        {
            var xml = "dummy.xml";
            var testExpression = @"round(-2.5)";
            var expected = -2d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// round(number(child::*))
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2627()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test5";
            var testExpression = @"round(number(child::Para[3]))";
            var expected = 3d;

            Utils.XPathNumberTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Number(): passing in a string with a number > max(double-precision 64-bit IEEE 754 value)
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2628()
        {
            var xml = "dummy.xml";
            var testExpression =
                @"number(""99999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999"")";
            var expected =
                99999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// number("//notAbook")
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2629()
        {
            var xml = "books.xml";
            var testExpression = @"number(""//notAbook"")";
            var expected = double.NaN;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Test for the scanner. It has different code path for digits of the form .xxx and x.xxx
        /// floor(.9) = 0
        /// </summary>
        [Fact]
        public static void NumberFunctionsTest2630()
        {
            var xml = "dummy.xml";
            var testExpression = @"floor(.9)";
            var expected = 0d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }
    }
}
