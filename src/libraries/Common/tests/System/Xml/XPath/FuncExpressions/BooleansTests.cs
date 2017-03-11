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
    /// Expressions - Booleans
    /// </summary>
    public static partial class BooleansTests
    {
        /// <summary>
        /// Verify result.
        /// child::para[1] = child::para[2]
        /// </summary>
        [Fact]
        public static void BooleansTest201()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"child::Para[1] = child::Para[2]";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] != child::para[2]
        /// </summary>
        [Fact]
        public static void BooleansTest202()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"child::Para[1] != child::Para[2]";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] <= child::para[2]
        /// </summary>
        [Fact]
        public static void BooleansTest203()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] <= child::Para[2]";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] >= child::para[2]
        /// </summary>
        [Fact]
        public static void BooleansTest204()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] >= child::Para[2]";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] > child::para[2]
        /// </summary>
        [Fact]
        public static void BooleansTest205()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] > child::Para[2]";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] < child::para[2]
        /// </summary>
        [Fact]
        public static void BooleansTest206()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] < child::Para[2]";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] < child::para[2] and child::para[2] < child::para[3]
        /// </summary>
        [Fact]
        public static void BooleansTest207()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] < child::Para[2] and child::Para[2] < child::Para[3]";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] < child::para[2] or child::para[2] > child::para[3]
        /// </summary>
        [Fact]
        public static void BooleansTest208()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] < child::Para[2] or child::Para[2] > child::Para[3]";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::* = child::*[1]
        /// </summary>
        [Fact]
        public static void BooleansTest209()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"child::* = child::*[1]";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] = 10
        /// </summary>
        [Fact]
        public static void BooleansTest2010()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"child::Para[1] = 10";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// 10 = child::para[1]
        /// </summary>
        [Fact]
        public static void BooleansTest2011()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test2";
            var testExpression = @"10 = child::Para[1]";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] = "Test"
        /// </summary>
        [Fact]
        public static void BooleansTest2012()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"child::Para[1] = ""Test""";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// "Test" = child::para[1]
        /// </summary>
        [Fact]
        public static void BooleansTest2013()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"""Test"" = child::Para[1]";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] = "Test" or child::para[1] = "test"
        /// </summary>
        [Fact]
        public static void BooleansTest2014()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"child::Para[1] = ""Test"" or child::Para[1] = ""test""";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// child::para[1] = true()
        /// </summary>
        [Fact]
        public static void BooleansTest2015()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test3";
            var testExpression = @"child::Para[1] = true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// true() = child::para[1]
        /// </summary>
        [Fact]
        public static void BooleansTest2016()
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test3";
            var testExpression = @"true() = child::Para[1]";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// true() != false()
        /// </summary>
        [Fact]
        public static void BooleansTest2017()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != false()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 0.09 = 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest2018()
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 = 0.09";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 0.09 != 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest2019()
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 != 0.09";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 0.09 = 0.08
        /// </summary>
        [Fact]
        public static void BooleansTest2020()
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 = 0.08";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// 0.09 != 0.08
        /// </summary>
        [Fact]
        public static void BooleansTest2021()
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 != 0.08";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// .000033 = .000033
        /// </summary>
        [Fact]
        public static void BooleansTest2022()
        {
            var xml = "dummy.xml";
            var testExpression = @".000033 =  .000033";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// "Test" = "Test"
        /// </summary>
        [Fact]
        public static void BooleansTest2023()
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" = ""Test""";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// "Test" != "Test"
        /// </summary>
        [Fact]
        public static void BooleansTest2024()
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" != ""Test""";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// "TestA" = "TestB"
        /// </summary>
        [Fact]
        public static void BooleansTest2025()
        {
            var xml = "dummy.xml";
            var testExpression = @"""TestA"" = ""TestB""";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// "TestA" != "TestB"
        /// </summary>
        [Fact]
        public static void BooleansTest2026()
        {
            var xml = "dummy.xml";
            var testExpression = @"""TestA"" != ""TestB""";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result (should resolve to true).
        /// true() = 5
        /// </summary>
        [Fact]
        public static void BooleansTest2027()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = 5";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result (should resolve to true).
        /// 5 = true()
        /// </summary>
        [Fact]
        public static void BooleansTest2028()
        {
            var xml = "dummy.xml";
            var testExpression = @"5 = true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result (should resolve to false).
        /// "Test" = 0
        /// </summary>
        [Fact]
        public static void BooleansTest2029()
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" = 0";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result (should resolve to false).
        /// 0 = "Test"
        /// </summary>
        [Fact]
        public static void BooleansTest2030()
        {
            var xml = "dummy.xml";
            var testExpression = @"0 = ""Test""";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result (should resolve to true).
        /// false() != "Test"
        /// </summary>
        [Fact]
        public static void BooleansTest2031()
        {
            var xml = "dummy.xml";
            var testExpression = @"false() !=  ""Test""";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Equality of node sets - true. Two node sets should be equal if for any node in the first node set there is a node in the second node set such that the string value of the two are equal.
        /// /mydoc/numbers[2]/n = /mydoc/numbers[1]/n
        /// </summary>
        [Fact]
        public static void BooleansTest2032()
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[2]/n = /mydoc/numbers[1]/n";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Equality of node sets - false. Two node sets should be equal if for any node in the first node set there is a node in the second node set such that the string value of the two are equal.
        /// /mydoc/numbers[2]/n = /mydoc/numbers[3]/n
        /// </summary>
        [Fact]
        public static void BooleansTest2033()
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[2]/n = /mydoc/numbers[3]/n";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// = node set and number - true. = is true if a node in node set has a numeric value equal to the number. (Not testing other operators since they work similarly)
        /// /mydoc/numbers[1]/n = 1
        /// </summary>
        [Fact]
        public static void BooleansTest2034()
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n = 1";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// = node set and number - false
        /// /mydoc/numbers[1]/n = 4
        /// </summary>
        [Fact]
        public static void BooleansTest2035()
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n = 4";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// operator precedence and,or. or has precedence over and
        /// true() and false() or true()
        /// </summary>
        [Fact]
        public static void BooleansTest2036()
        {
            var xml = "numbers.xml";
            var testExpression = @"true() and false() or true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Comparison of node set with string. = is true if a node in node set has a string value equal to the string constant it is being compared with.
        /// /bookstore/book/title = 'Seven Years in Trenton'
        /// </summary>
        [Fact]
        public static void BooleansTest2037()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book/title = 'Seven Years in Trenton'";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage: Covering case for AndExpr constructor .ctor(bool,bool)
        /// boolean(true()) and boolean(true())
        /// </summary>
        [Fact]
        public static void BooleansTest2038()
        {
            var xml = "numbers.xml";
            var testExpression = @"boolean(true()) and boolean(true())";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage: Covering case for AndExpr getValue where first condition fails
        /// boolean(false()) and boolean(true())
        /// </summary>
        [Fact]
        public static void BooleansTest2039()
        {
            var xml = "numbers.xml";
            var testExpression = @"boolean(false()) and boolean(true())";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "Test" > "Test"
        /// </summary>
        [Fact]
        public static void BooleansTest2040()
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" > ""Test""";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "Test" < "Test"
        /// </summary>
        [Fact]
        public static void BooleansTest2041()
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" < ""Test""";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "Test" <= "Test"
        /// </summary>
        [Fact]
        public static void BooleansTest2042()
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" <= ""Test""";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "Test" >= "Test"
        /// </summary>
        [Fact]
        public static void BooleansTest2043()
        {
            var xml = "dummy.xml";
            var testExpression = @"""Test"" >= ""Test""";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = false()
        /// </summary>
        [Fact]
        public static void BooleansTest2044()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = false()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = true()
        /// </summary>
        [Fact]
        public static void BooleansTest2045()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() != true()
        /// </summary>
        [Fact]
        public static void BooleansTest2046()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() != false()
        /// </summary>
        [Fact]
        public static void BooleansTest2047()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != false()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() > true()
        /// </summary>
        [Fact]
        public static void BooleansTest2048()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() > true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() >= true()
        /// </summary>
        [Fact]
        public static void BooleansTest2049()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() >= true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() <= true()
        /// </summary>
        [Fact]
        public static void BooleansTest2050()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() <= true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = number("1")
        /// </summary>
        [Fact]
        public static void BooleansTest2051()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = number(""1"")";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = number(0 div 0)
        /// </summary>
        [Fact]
        public static void BooleansTest2052()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = number(0 div 0)";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() != number("1")
        /// </summary>
        [Fact]
        public static void BooleansTest2053()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != number(""1"")";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() != number('abc')
        /// </summary>
        [Fact]
        public static void BooleansTest2054()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != number('abc')";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() >= number("1")
        /// </summary>
        [Fact]
        public static void BooleansTest2055()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() >= number(""1"")";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() >= number("abc")
        /// </summary>
        [Fact]
        public static void BooleansTest2056()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() >= number(""abc"")";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() <= number("1")
        /// </summary>
        [Fact]
        public static void BooleansTest2057()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() <= number(""1"")";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() <= number("abc")
        /// </summary>
        [Fact]
        public static void BooleansTest2058()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() <= number(""abc"")";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() < number("1")
        /// </summary>
        [Fact]
        public static void BooleansTest2059()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() < number(""1"")";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() > number("1")
        /// </summary>
        [Fact]
        public static void BooleansTest2060()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() > number(""1"")";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("1") = true()
        /// </summary>
        [Fact]
        public static void BooleansTest2061()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"") = true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("abc") = true()
        /// </summary>
        [Fact]
        public static void BooleansTest2062()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""abc"") = true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("1") != true()
        /// </summary>
        [Fact]
        public static void BooleansTest2063()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"") != true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("abc") != true()
        /// </summary>
        [Fact]
        public static void BooleansTest2064()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""abc"") != true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("1") >= true()
        /// </summary>
        [Fact]
        public static void BooleansTest2065()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"") >= true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("abc") >= true()
        /// </summary>
        [Fact]
        public static void BooleansTest2066()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""abc"") >= true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("1") <= true()
        /// </summary>
        [Fact]
        public static void BooleansTest2067()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"") <= true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("abc") <= true()
        /// </summary>
        [Fact]
        public static void BooleansTest2068()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""abc"") <= true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("1") < true()
        /// </summary>
        [Fact]
        public static void BooleansTest2069()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"") < true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// number("1") > true()
        /// </summary>
        [Fact]
        public static void BooleansTest2070()
        {
            var xml = "dummy.xml";
            var testExpression = @"number(""1"") > true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = "1"
        /// </summary>
        [Fact]
        public static void BooleansTest2071()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = ""1""";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = ""
        /// </summary>
        [Fact]
        public static void BooleansTest2072()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() = """"";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() != "1"
        /// </summary>
        [Fact]
        public static void BooleansTest2073()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != ""1""";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() != ""
        /// </summary>
        [Fact]
        public static void BooleansTest2074()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() != """"";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() >= "1"
        /// </summary>
        [Fact]
        public static void BooleansTest2075()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() >= ""1""";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() >= ""
        /// </summary>
        [Fact]
        public static void BooleansTest2076()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() >= """"";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() <= "1"
        /// </summary>
        [Fact]
        public static void BooleansTest2077()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() <= ""1""";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() <= ""
        /// </summary>
        [Fact]
        public static void BooleansTest2078()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() <= """"";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() < "1"
        /// </summary>
        [Fact]
        public static void BooleansTest2079()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() < ""1""";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() > "1"
        /// </summary>
        [Fact]
        public static void BooleansTest2080()
        {
            var xml = "dummy.xml";
            var testExpression = @"true() > ""1""";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" = true()
        /// </summary>
        [Fact]
        public static void BooleansTest2081()
        {
            var xml = "dummy.xml";
            var testExpression = @"""1"" = true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "" = true()
        /// </summary>
        [Fact]
        public static void BooleansTest2082()
        {
            var xml = "dummy.xml";
            var testExpression = @""""" = true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" != true()
        /// </summary>
        [Fact]
        public static void BooleansTest2083()
        {
            var xml = "dummy.xml";
            var testExpression = @"""1"" != true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "" != true()
        /// </summary>
        [Fact]
        public static void BooleansTest2084()
        {
            var xml = "dummy.xml";
            var testExpression = @""""" != true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" >= true()
        /// </summary>
        [Fact]
        public static void BooleansTest2085()
        {
            var xml = "dummy.xml";
            var testExpression = @"""1"" >= true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "" >= true()
        /// </summary>
        [Fact]
        public static void BooleansTest2086()
        {
            var xml = "dummy.xml";
            var testExpression = @""""" >= true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" <= true()
        /// </summary>
        [Fact]
        public static void BooleansTest2087()
        {
            var xml = "dummy.xml";
            var testExpression = @"""1"" <= true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "" <= true()
        /// </summary>
        [Fact]
        public static void BooleansTest2088()
        {
            var xml = "dummy.xml";
            var testExpression = @""""" < true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" < true()
        /// </summary>
        [Fact]
        public static void BooleansTest2089()
        {
            var xml = "dummy.xml";
            var testExpression = @"""1"" < true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" > true()
        /// </summary>
        [Fact]
        public static void BooleansTest2090()
        {
            var xml = "dummy.xml";
            var testExpression = @"""1"" > true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 0.09 > 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest2091()
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 > 0.09";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 1.09 > 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest2092()
        {
            var xml = "dummy.xml";
            var testExpression = @"1.09 > 0.09";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 1.09 < 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest2093()
        {
            var xml = "dummy.xml";
            var testExpression = @"1.09 < 0.09";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 0.09 < 1.09
        /// </summary>
        [Fact]
        public static void BooleansTest2094()
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 < 1.09";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 0.09 >= 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest2095()
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 >= 0.09";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 0.09 <= 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest2096()
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 <= 0.09";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 0.09 >= 1.09
        /// </summary>
        [Fact]
        public static void BooleansTest2097()
        {
            var xml = "dummy.xml";
            var testExpression = @"0.09 >= 1.09";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 1.09 <= 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest2098()
        {
            var xml = "dummy.xml";
            var testExpression = @"1.09 <= 0.09";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.09" = 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest2099()
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.09"" = 0.09";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.20" = 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest20100()
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.20"" = 0.09";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.09" != 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest20101()
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.09"" != 0.09";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1.09" != 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest20102()
        {
            var xml = "dummy.xml";
            var testExpression = @"""1.09"" != 0.09";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.09" >= 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest20103()
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.09"" >= 0.09";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.01" >= 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest20104()
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.01"" >= 0.09";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.01" <= 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest20105()
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.01"" <= 0.09";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.11" <= 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest20106()
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.11"" <= 0.09";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.09" > 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest20107()
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.09"" > 0.09";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.19" > 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest20108()
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.19"" > 0.09";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.09" < 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest20109()
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.09"" < 0.09";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "0.01" < 0.09
        /// </summary>
        [Fact]
        public static void BooleansTest20110()
        {
            var xml = "dummy.xml";
            var testExpression = @"""0.01"" < 0.09";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = /mydoc/numbers[1]/n[1]
        /// </summary>
        [Fact]
        public static void BooleansTest20111()
        {
            var xml = "numbers.xml";
            var testExpression = @"true() = /mydoc/numbers[1]/n[1]";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() = /bookstore/title
        /// </summary>
        [Fact]
        public static void BooleansTest20112()
        {
            var xml = "books.xml";
            var testExpression = @"true() = /bookstore/title";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// true() > /mydoc/numbers[1]/n[1]
        /// </summary>
        [Fact]
        public static void BooleansTest20113()
        {
            var xml = "numbers.xml";
            var testExpression = @"true() > /mydoc/numbers[1]/n[1]";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /mydoc/numbers[1]/n[1] = true()
        /// </summary>
        [Fact]
        public static void BooleansTest20114()
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n[1] = true()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /bookstore/title = true()
        /// </summary>
        [Fact]
        public static void BooleansTest20115()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/title = true()";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /mydoc/numbers[1]/n[1] > true
        /// </summary>
        [Fact]
        public static void BooleansTest20116()
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n[1] > true";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 1 = /mydoc/numbers[1]/n[1]
        /// </summary>
        [Fact]
        public static void BooleansTest20117()
        {
            var xml = "numbers.xml";
            var testExpression = @"1 = /mydoc/numbers[1]/n[1]";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 1 > /mydoc/numbers[1]/n[1]
        /// </summary>
        [Fact]
        public static void BooleansTest20118()
        {
            var xml = "numbers.xml";
            var testExpression = @"1 > /mydoc/numbers[1]/n[1]";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// 1 = /bookstore/book[1]/title
        /// </summary>
        [Fact]
        public static void BooleansTest20119()
        {
            var xml = "books.xml";
            var testExpression = @"1 = /bookstore/book[1]/title";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /mydoc/numbers[1]/n[1] = 1
        /// </summary>
        [Fact]
        public static void BooleansTest20120()
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n[1] = 1";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /mydoc/numbers[1]/n[1] > 1
        /// </summary>
        [Fact]
        public static void BooleansTest20121()
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n[1] > 1";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /bookstore/book[1]/title = 1
        /// </summary>
        [Fact]
        public static void BooleansTest20122()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[1]/title = 1";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "1" = /bookstore/book[1]/title
        /// </summary>
        [Fact]
        public static void BooleansTest20123()
        {
            var xml = "books.xml";
            var testExpression = @"""1"" = /bookstore/book[1]/title";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "5" > /mydoc/numbers[1]/n[1]
        /// </summary>
        [Fact]
        public static void BooleansTest20124()
        {
            var xml = "numbers.xml";
            var testExpression = @"""5"" > /mydoc/numbers[1]/n[1]";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// "Seven Years in Trenton" = /bookstore/book[1]/title
        /// </summary>
        [Fact]
        public static void BooleansTest20125()
        {
            var xml = "books.xml";
            var testExpression = @"""Seven Years in Trenton"" = /bookstore/book[1]/title";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /bookstore/book[1]/title = "1"
        /// </summary>
        [Fact]
        public static void BooleansTest20126()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[1]/title = ""1""";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /mydoc/numbers[1]/n[1] < "5"
        /// </summary>
        [Fact]
        public static void BooleansTest20127()
        {
            var xml = "numbers.xml";
            var testExpression = @"/mydoc/numbers[1]/n[1] < ""5""";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage
        /// /bookstore/book[1]/title = "Seven Years in Trenton"
        /// </summary>
        [Fact]
        public static void BooleansTest20128()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[1]/title = ""Seven Years in Trenton""";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// 5 &lt; unknown
        /// </summary>
        [Fact]
        public static void BooleansTest20129()
        {
            var xml = "books.xml";
            var testExpression = @"5 < unknown";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// true() &gt; unknown
        /// </summary>
        [Fact]
        public static void BooleansTest20130()
        {
            var xml = "books.xml";
            var testExpression = @"true() > unknown";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// true() &lt; book/price
        /// </summary>
        [Fact]
        public static void BooleansTest20131()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"true() < book/price";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// book &gt; false()
        /// </summary>
        [Fact]
        public static void BooleansTest20132()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book > false()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// book/price &gt; magazine/price
        /// </summary>
        [Fact]
        public static void BooleansTest20133()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/price > magazine/price";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// book/price &lt; magazine/price
        /// </summary>
        [Fact]
        public static void BooleansTest20134()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/price < magazine/price";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// "1" &gt; false()
        /// </summary>
        [Fact]
        public static void BooleansTest20135()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"""1"" > false()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// nodeset is first converted to boolean(true) and then number (1)
        /// true() &gt; book
        /// </summary>
        [Fact]
        public static void BooleansTest20136()
        {
            var xml = "books.xml";
            var testExpression = @"true() > book";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// true() and(true()) or(true() and (false() or true()))
        /// </summary>
        [Fact]
        public static void BooleansTest20137()
        {
            var xml = "books.xml";
            var testExpression = @"true() and(true()) or(true() and (false() or true()))";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Test for the scanner. It has different code path for digits of the form .xxx and x.xxx
        /// ".09" < .09
        /// </summary>
        [Fact]
        public static void BooleansTest20138()
        {
            var xml = "dummy.xml";
            var testExpression = @""".09"" < .09";
            var expected = false;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }
    }
}
