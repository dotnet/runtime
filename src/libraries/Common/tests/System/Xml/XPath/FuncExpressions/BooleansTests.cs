// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using XPathTests.Common;
using Xunit;

namespace XPathTests.FunctionalTests.Expressions
{
    /// <summary>
    /// Expressions - Booleans
    /// </summary>
    public static partial class BooleansTests
    {
        /// <summary>
        /// Verify result.
        /// child::para[1] = child::para[2]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest201(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"child::Para[1] = child::Para[2]";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] != child::para[2]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest202(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"child::Para[1] != child::Para[2]";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] <= child::para[2]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest203(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] <= child::Para[2]";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] >= child::para[2]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest204(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] >= child::Para[2]";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] > child::para[2]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest205(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] > child::Para[2]";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] < child::para[2]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest206(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] < child::Para[2]";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] < child::para[2] and child::para[2] < child::para[3]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest207(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] < child::Para[2] and child::Para[2] < child::Para[3]";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] < child::para[2] or child::para[2] > child::para[3]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest208(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] < child::Para[2] or child::Para[2] > child::Para[3]";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::* = child::*[1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest209(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"child::* = child::*[1]";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] = 10
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2010(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] = 10";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// 10 = child::para[1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2011(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"10 = child::Para[1]";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] = "Test"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2012(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"child::Para[1] = ""Test""";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// "Test" = child::para[1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2013(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"""Test"" = child::Para[1]";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] = "Test" or child::para[1] = "test"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2014(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"child::Para[1] = ""Test"" or child::Para[1] = ""test""";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] = true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2015(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test3";
            var testExpression = @"child::Para[1] = true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// true() = child::para[1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2016(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test3";
            var testExpression = @"true() = child::Para[1]";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// true() != false(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2017(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != false()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 0.09 = 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2018(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 = 0.09";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 0.09 != 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2019(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 != 0.09";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 0.09 = 0.08
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2020(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 = 0.08";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 0.09 != 0.08
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2021(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 != 0.08";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// .000033 = .000033
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2022(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @".000033 =  .000033";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// "Test" = "Test"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2023(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" = ""Test""";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// "Test" != "Test"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2024(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" != ""Test""";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// "TestA" = "TestB"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2025(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""TestA"" = ""TestB""";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// "TestA" != "TestB"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2026(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""TestA"" != ""TestB""";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result (should resolve to true).
        /// true() = 5
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2027(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = 5";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result (should resolve to true).
        /// 5 = true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2028(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"5 = true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result (should resolve to false).
        /// "Test" = 0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2029(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" = 0";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result (should resolve to false).
        /// 0 = "Test"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2030(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"0 = ""Test""";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result (should resolve to true).
        /// false() != "Test"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2031(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"false() !=  ""Test""";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Equality of node sets - true. Two node sets should be equal if for any node in the first node set there is a node in the second node set such that the string value of the two are equal.
        /// /mydoc/numbers[2]/n = /mydoc/numbers[1]/n
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2032(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[2]/n = /mydoc/numbers[1]/n";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Equality of node sets - false. Two node sets should be equal if for any node in the first node set there is a node in the second node set such that the string value of the two are equal.
        /// /mydoc/numbers[2]/n = /mydoc/numbers[3]/n
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2033(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[2]/n = /mydoc/numbers[3]/n";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// = node set and number - true. = is true if a node in node set has a numeric value equal to the number. (Not testing other operators since they work similarly)
        /// /mydoc/numbers[1]/n = 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2034(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n = 1";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// = node set and number - false
        /// /mydoc/numbers[1]/n = 4
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2035(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n = 4";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// operator precedence and,or. or has precedence over and
        /// true() and false() or true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2036(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"true() and false() or true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Comparison of node set with string. = is true if a node in node set has a string value equal to the string constant it is being compared with.
        /// /bookstore/book/title = 'Seven Years in Trenton'
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2037(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book/title = 'Seven Years in Trenton'";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage: Covering case for AndExpr constructor .ctor(bool,bool)
        /// boolean(true()) and boolean(true())
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2038(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"boolean(true()) and boolean(true())";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage: Covering case for AndExpr getValue where first condition fails
        /// boolean(false()) and boolean(true())
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2039(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"boolean(false()) and boolean(true())";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "Test" > "Test"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2040(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" > ""Test""";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "Test" < "Test"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2041(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" < ""Test""";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "Test" <= "Test"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2042(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" <= ""Test""";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "Test" >= "Test"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2043(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" >= ""Test""";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = false(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2044(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = false()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2045(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() != true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2046(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() != false(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2047(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != false()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() > true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2048(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() > true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() >= true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2049(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() >= true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() <= true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2050(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() <= true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = number("1")
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2051(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = number(""1"")";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = number(0 div 0)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2052(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = number(0 div 0)";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() != number("1")
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2053(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != number(""1"")";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() != number('abc')
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2054(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != number('abc')";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() >= number("1")
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2055(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() >= number(""1"")";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() >= number("abc")
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2056(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() >= number(""abc"")";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() <= number("1")
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2057(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() <= number(""1"")";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() <= number("abc")
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2058(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() <= number(""abc"")";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() < number("1")
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2059(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() < number(""1"")";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() > number("1")
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2060(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() > number(""1"")";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("1") = true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2061(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"") = true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("abc") = true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2062(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""abc"") = true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("1") != true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2063(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"") != true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("abc") != true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2064(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""abc"") != true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("1") >= true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2065(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"") >= true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("abc") >= true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2066(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""abc"") >= true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("1") <= true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2067(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"") <= true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("abc") <= true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2068(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""abc"") <= true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("1") < true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2069(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"") < true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("1") > true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2070(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"") > true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = "1"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2071(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = ""1""";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2072(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = """"";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() != "1"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2073(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != ""1""";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() != ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2074(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != """"";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() >= "1"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2075(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() >= ""1""";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() >= ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2076(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() >= """"";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() <= "1"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2077(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() <= ""1""";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() <= ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2078(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() <= """"";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() < "1"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2079(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() < ""1""";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() > "1"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2080(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"true() > ""1""";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" = true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2081(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""1"" = true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "" = true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2082(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @""""" = true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" != true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2083(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""1"" != true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "" != true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2084(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @""""" != true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" >= true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2085(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""1"" >= true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "" >= true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2086(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @""""" >= true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" <= true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2087(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""1"" <= true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "" <= true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2088(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @""""" < true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" < true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2089(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""1"" < true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" > true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2090(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""1"" > true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 0.09 > 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2091(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 > 0.09";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 1.09 > 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2092(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"1.09 > 0.09";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 1.09 < 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2093(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"1.09 < 0.09";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 0.09 < 1.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2094(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 < 1.09";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 0.09 >= 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2095(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 >= 0.09";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 0.09 <= 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2096(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 <= 0.09";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 0.09 >= 1.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2097(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 >= 1.09";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 1.09 <= 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2098(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"1.09 <= 0.09";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.09" = 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest2099(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.09"" = 0.09";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.20" = 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20100(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.20"" = 0.09";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.09" != 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20101(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.09"" != 0.09";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1.09" != 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20102(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""1.09"" != 0.09";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.09" >= 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20103(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.09"" >= 0.09";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.01" >= 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20104(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.01"" >= 0.09";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.01" <= 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20105(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.01"" <= 0.09";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.11" <= 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20106(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.11"" <= 0.09";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.09" > 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20107(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.09"" > 0.09";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.19" > 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20108(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.19"" > 0.09";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.09" < 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20109(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.09"" < 0.09";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.01" < 0.09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20110(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.01"" < 0.09";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = /mydoc/numbers[1]/n[1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20111(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"true() = /mydoc/numbers[1]/n[1]";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = /bookstore/title
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20112(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"true() = /bookstore/title";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() > /mydoc/numbers[1]/n[1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20113(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"true() > /mydoc/numbers[1]/n[1]";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /mydoc/numbers[1]/n[1] = true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20114(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n[1] = true()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /bookstore/title = true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20115(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/title = true()";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /mydoc/numbers[1]/n[1] > true
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20116(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n[1] > true";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 1 = /mydoc/numbers[1]/n[1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20117(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"1 = /mydoc/numbers[1]/n[1]";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 1 > /mydoc/numbers[1]/n[1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20118(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"1 > /mydoc/numbers[1]/n[1]";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 1 = /bookstore/book[1]/title
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20119(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"1 = /bookstore/book[1]/title";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /mydoc/numbers[1]/n[1] = 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20120(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n[1] = 1";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /mydoc/numbers[1]/n[1] > 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20121(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n[1] > 1";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /bookstore/book[1]/title = 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20122(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[1]/title = 1";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" = /bookstore/book[1]/title
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20123(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"""1"" = /bookstore/book[1]/title";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "5" > /mydoc/numbers[1]/n[1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20124(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"""5"" > /mydoc/numbers[1]/n[1]";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "Seven Years in Trenton" = /bookstore/book[1]/title
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20125(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"""Seven Years in Trenton"" = /bookstore/book[1]/title";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /bookstore/book[1]/title = "1"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20126(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[1]/title = ""1""";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /mydoc/numbers[1]/n[1] < "5"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20127(Utils.NavigatorKind kind)
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n[1] < ""5""";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /bookstore/book[1]/title = "Seven Years in Trenton"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20128(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[1]/title = ""Seven Years in Trenton""";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// 5 &lt; unknown
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20129(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"5 < unknown";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// true() &gt; unknown
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20130(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"true() > unknown";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// true() &lt; book/price
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20131(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"true() < book/price";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// book &gt; false(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20132(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book > false()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// book/price &gt; magazine/price
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20133(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/price > magazine/price";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// book/price &lt; magazine/price
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20134(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/price < magazine/price";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// "1" &gt; false(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20135(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"""1"" > false()";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// nodeset is first converted to boolean(true) and then number (1)
        /// true() &gt; book
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20136(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"true() > book";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// true() and(true()) or(true() and (false() or true()))
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20137(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"true() and(true()) or(true() and (false() or true()))";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Test for the scanner. It has different code path for digits of the form .xxx and x.xxx
        /// ".09" < .09
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BooleansTest20138(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @""".09"" < .09";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }
    }
}
