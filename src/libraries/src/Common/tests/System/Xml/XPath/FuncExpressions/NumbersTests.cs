// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Expressions
{
    /// <summary>
    /// Expressions - Numbers
    /// </summary>
    public static partial class NumbersTests
    {
        /// <summary>
        /// Verify result.
        /// 1 + 1 = 2
        /// </summary>
        [Fact]
        public static void NumbersTest211()
        {
            var xml = "dummy.xml";
            var testExpression = @"1 + 1";
            var expected = 2d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 0.5 + 0.5 = 1.0
        /// </summary>
        [Fact]
        public static void NumbersTest212()
        {
            var xml = "dummy.xml";
            var testExpression = @"0.5 + 0.5";
            var expected = 1.0d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 1 + child::para[1]
        /// </summary>
        [Fact]
        public static void NumbersTest213()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"1 + child::Para[1]";
            var expected = 11d;

            Utils.XPathNumberTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] + 1
        /// </summary>
        [Fact]
        public static void NumbersTest214()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] + 1";
            var expected = 11d;

            Utils.XPathNumberTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// 2 - 1 = 1
        /// </summary>
        [Fact]
        public static void NumbersTest215()
        {
            var xml = "dummy.xml";
            var testExpression = @"2 - 1";
            var expected = 1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 1.5 - 0.5 = 1.0
        /// </summary>
        [Fact]
        public static void NumbersTest216()
        {
            var xml = "dummy.xml";
            var testExpression = @"1.5 - 0.5";
            var expected = 1.0d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 5 mod 2 = 1
        /// </summary>
        [Fact]
        public static void NumbersTest217()
        {
            var xml = "dummy.xml";
            var testExpression = @"5 mod 2";
            var expected = 1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 5 mod -2 = 1
        /// </summary>
        [Fact]
        public static void NumbersTest218()
        {
            var xml = "dummy.xml";
            var testExpression = @"5 mod -2";
            var expected = 1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// -5 mod 2 = -1
        /// </summary>
        [Fact]
        public static void NumbersTest219()
        {
            var xml = "dummy.xml";
            var testExpression = @"-5 mod 2";
            var expected = -1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// -5 mod -2 = -1
        /// </summary>
        [Fact]
        public static void NumbersTest2110()
        {
            var xml = "dummy.xml";
            var testExpression = @"-5 mod -2";
            var expected = -1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 50 div 10 = 5
        /// </summary>
        [Fact]
        public static void NumbersTest2111()
        {
            var xml = "dummy.xml";
            var testExpression = @"50 div 10";
            var expected = 5d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 2.5 div 0.5 = 5.0
        /// </summary>
        [Fact]
        public static void NumbersTest2112()
        {
            var xml = "dummy.xml";
            var testExpression = @"2.5 div 0.5";
            var expected = 5.0d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 50 div child::para[1]
        /// </summary>
        [Fact]
        public static void NumbersTest2113()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"50 div child::Para[1]";
            var expected = 5d;

            Utils.XPathNumberTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] div 2
        /// </summary>
        [Fact]
        public static void NumbersTest2114()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] div 2";
            var expected = 5d;

            Utils.XPathNumberTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// 2 * 1 = 2
        /// </summary>
        [Fact]
        public static void NumbersTest2115()
        {
            var xml = "dummy.xml";
            var testExpression = @"2 * 1";
            var expected = 2d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 2.5 * 0.5 = 1.25
        /// </summary>
        [Fact]
        public static void NumbersTest2116()
        {
            var xml = "dummy.xml";
            var testExpression = @"2.5 * 0.5";
            var expected = 1.25d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// if any of the operands is NaN result should be NaN
        /// NaN mod 1
        /// </summary>
        [Fact]
        public static void NumbersTest2117()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(0 div 0) mod 1";
            var expected = Double.NaN;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Expected NaN
        /// 1 mod NaN
        /// </summary>
        [Fact]
        public static void NumbersTest2118()
        {
            var xml = "dummy.xml";
            var testExpression = @"1 mod number(0 div 0)";
            var expected = Double.NaN;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// NaN expected
        /// Infinity mod 1
        /// </summary>
        [Fact]
        public static void NumbersTest2119()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(1 div 0) mod 1";
            var expected = Double.NaN;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// NaN expected
        /// Infinity mod 0
        /// </summary>
        [Fact]
        public static void NumbersTest2120()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(1 div 0) mod 0";
            var expected = Double.NaN;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// NaN expected
        /// 1 mod 0
        /// </summary>
        [Fact]
        public static void NumbersTest2121()
        {
            var xml = "dummy.xml";
            var testExpression = @"1 mod 0";
            var expected = Double.NaN;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// 1 mod Infinity = 1
        /// </summary>
        [Fact]
        public static void NumbersTest2122()
        {
            var xml = "dummy.xml";
            var testExpression = @"1 mod number(1 div 0)";
            var expected = 1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// -1 mod Infinity = -1
        /// </summary>
        [Fact]
        public static void NumbersTest2123()
        {
            var xml = "dummy.xml";
            var testExpression = @"-1 mod number(1 div 0)";
            var expected = -1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// 1 mod -Infinity =1
        /// </summary>
        [Fact]
        public static void NumbersTest2124()
        {
            var xml = "dummy.xml";
            var testExpression = @"1 mod number(-1 div 0)";
            var expected = 1d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// 0 mod 5 = 0
        /// </summary>
        [Fact]
        public static void NumbersTest2125()
        {
            var xml = "dummy.xml";
            var testExpression = @"0 mod 5";
            var expected = 0d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// 5.2345 mod 3.0 = 2.2344999999999997
        /// </summary>
        [Fact]
        public static void NumbersTest2126()
        {
            var xml = "dummy.xml";
            var testExpression = @"5.2345 mod 3.0";
            var expected = 2.2344999999999997d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Test for the scanner. It has different code path for digits of the form .xxx and x.xxx
        /// .5 + .5 = 1.0
        /// </summary>
        [Fact]
        public static void NumbersTest2127()
        {
            var xml = "dummy.xml";
            var testExpression = @".5 + .5";
            var expected = 1.0d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Test for the scanner. It has different code path for digits of the form .xxx and x.xxx
        /// .0 + .0 = 0.0
        /// </summary>
        [Fact]
        public static void NumbersTest2128()
        {
            var xml = "dummy.xml";
            var testExpression = @".0 + .0";
            var expected = 0.0d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Test for the scanner. It has different code path for digits of the form .xxx and x.xxx
        /// .0 + .0 = 0.0
        /// </summary>
        [Fact]
        public static void NumbersTest2129()
        {
            var xml = "dummy.xml";
            var testExpression = @".0 + .0=.0";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }
    }
}
