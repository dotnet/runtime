// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using XPathTests.Common;
using Xunit;

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
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest211(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"1 + 1";
            var expected = 2d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 0.5 + 0.5 = 1.0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest212(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"0.5 + 0.5";
            var expected = 1.0d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 1 + child::para[1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest213(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"1 + child::Para[1]";
            var expected = 11d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] + 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest214(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] + 1";
            var expected = 11d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// 2 - 1 = 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest215(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"2 - 1";
            var expected = 1d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 1.5 - 0.5 = 1.0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest216(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"1.5 - 0.5";
            var expected = 1.0d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 5 mod 2 = 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest217(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"5 mod 2";
            var expected = 1d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 5 mod -2 = 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest218(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"5 mod -2";
            var expected = 1d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// -5 mod 2 = -1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest219(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"-5 mod 2";
            var expected = -1d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// -5 mod -2 = -1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2110(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"-5 mod -2";
            var expected = -1d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 50 div 10 = 5
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2111(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"50 div 10";
            var expected = 5d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 2.5 div 0.5 = 5.0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2112(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"2.5 div 0.5";
            var expected = 5.0d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 50 div child::para[1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2113(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"50 div child::Para[1]";
            var expected = 5d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] div 2
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2114(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] div 2";
            var expected = 5d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// 2 * 1 = 2
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2115(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"2 * 1";
            var expected = 2d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 2.5 * 0.5 = 1.25
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2116(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"2.5 * 0.5";
            var expected = 1.25d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// if any of the operands is NaN result should be NaN
        /// NaN mod 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2117(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(0 div 0) mod 1";
            var expected = double.NaN;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Expected NaN
        /// 1 mod NaN
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2118(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"1 mod number(0 div 0)";
            var expected = double.NaN;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// NaN expected
        /// Infinity mod 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2119(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(1 div 0) mod 1";
            var expected = double.NaN;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// NaN expected
        /// Infinity mod 0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2120(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(1 div 0) mod 0";
            var expected = double.NaN;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// NaN expected
        /// 1 mod 0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2121(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"1 mod 0";
            var expected = double.NaN;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// 1 mod Infinity = 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2122(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"1 mod number(1 div 0)";
            var expected = 1d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// -1 mod Infinity = -1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2123(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"-1 mod number(1 div 0)";
            var expected = -1d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// 1 mod -Infinity =1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2124(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"1 mod number(-1 div 0)";
            var expected = 1d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// 0 mod 5 = 0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2125(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"0 mod 5";
            var expected = 0d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// 5.2345 mod 3.0 = 2.2344999999999997
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2126(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"5.2345 mod 3.0";
            var expected = 2.2344999999999997d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Test for the scanner. It has different code path for digits of the form .xxx and x.xxx
        /// .5 + .5 = 1.0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2127(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @".5 + .5";
            var expected = 1.0d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Test for the scanner. It has different code path for digits of the form .xxx and x.xxx
        /// .0 + .0 = 0.0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2128(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @".0 + .0";
            var expected = 0.0d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Test for the scanner. It has different code path for digits of the form .xxx and x.xxx
        /// .0 + .0 = 0.0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void NumbersTest2129(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @".0 + .0=.0";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }
    }
}
