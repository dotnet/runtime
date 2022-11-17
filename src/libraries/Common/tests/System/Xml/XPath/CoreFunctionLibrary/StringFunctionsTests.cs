// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using XPathTests.Common;
using Xunit;

namespace XPathTests.FunctionalTests.CoreFunctionLibrary
{
    /// <summary>
    /// Core Function Library - String Functions
    /// </summary>
    public static partial class StringFunctionsTests
    {
        /// <summary>
        /// Verify result.
        /// string()="context node data"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest241(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1/Para[1]";
            var testExpression = @"string()";
            var expected = @"Test";

            Utils.XPathStringTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// string(1) = "1"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest242(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string(1)";
            var expected = 1d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// string(-0) = "0"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest243(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string(-0)";
            var expected = 0d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// string(+0) = "0"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest244(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string(+0)";

            Utils.XPathStringTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// Verify result.
        /// string(number("NotANumber")) = "NaN"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest245(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string(number(""NotANumber""))";
            var expected = double.NaN;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// string(true()) = "true"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest246(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string(true())";
            var expected = @"true";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// string(false()) = "false"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest247(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string(false())";
            var expected = @"false";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// string(child::para) = "1st child node data"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest248(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"string(child::Para)";
            var expected = @"Test";

            Utils.XPathStringTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// concat("AA", "BB") = "AABB"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest249(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"concat(""AA"", ""BB"")";
            var expected = @"AABB";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// concat("AA", "BB", "CC") = "AABBCC"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2410(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"concat(""AA"", ""BB"", ""CC"")";
            var expected = @"AABBCC";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// concat(string(child::*), "BB") = "AABB"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2411(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"concat(string(child::*), ""BB"")";
            var expected = @"TestBB";

            Utils.XPathStringTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// starts-with("AABB", "AA") = true
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2412(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"starts-with(""AABB"", ""AA"")";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// starts-with("AABB", "BB") = false
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2413(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"starts-with(""AABB"", ""BB"")";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// starts-with("AABB", string(child::*)) = true
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2414(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"starts-with(""TestBB"", string(child::*))";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// contains("AABBCC", "BB") = true
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2415(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"contains(""AABBCC"", ""BB"")";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// contains("AABBCC", "DD") = false
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2416(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"contains(""AABBCC"", ""DD"")";
            var expected = false;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// contains("AABBCC", string(child::*)) = true
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2417(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"contains(""AATestBB"", string(child::*))";
            var expected = true;

            Utils.XPathBooleanTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// substring-before("AA/BB", "/") = "AA"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2418(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring-before(""AA/BB"", ""/"")";
            var expected = @"AA";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// substring-before("AA/BB", "D") = ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2419(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring-before(""AA/BB"", ""D"")";
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// substring-before(string(child::*), "/") = "AA"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2420(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"substring-before(string(child::*), ""t"")";
            var expected = @"Tes";

            Utils.XPathStringTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// substring-after("AA/BB", "/") = "BB"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2421(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring-after(""AA/BB"", ""/"")";
            var expected = @"BB";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// substring-after("AA/BB", "D") != ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2422(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring-after(""AA/BB"", ""D"")";
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// substring-after(string(child::*), "/") = "BB"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2423(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"substring-after(string(child::*), ""T"")";
            var expected = @"est";

            Utils.XPathStringTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// substring("ABC", 2) = "BC"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2424(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring(""ABC"", 2)";
            var expected = @"BC";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// substring("ABCD", 2, 2) = "BC"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2425(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring(""ABCD"", 2, 2)";
            var expected = @"BC";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// substring("ABCDE", 1.5, 2.6) = "BCD"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2426(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring(""ABCDE"", 1.5, 2.6)";
            var expected = @"BCD";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// substring("ABCDE", 0, 3) = "AB"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2427(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring(""ABCDE"", 0, 3)";
            var expected = @"AB";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// substring("ABCDE", 0 div 0, 3) = ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2428(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring(""ABCDE"", 0 div 0, 3)";
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// substring("ABCDE", 1, 0 div 0) = ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2429(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring(""ABCDE"", 1, 0 div 0)";
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// substring("ABCDE", -42, 1 div 0) = "ABCDE"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2430(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring(""ABCDE"", -42, 1 div 0)";
            var expected = @"ABCDE";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// substring("ABCDE", -1 div 0, 1 div 0) = ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2431(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring(""ABCDE"", -1 div 0, 1 div 0)";
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// substring(string(child::*), 2) = "BC"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2432(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"substring(string(child::*), 2)";
            var expected = @"est";

            Utils.XPathStringTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// string-length("ABCDE") = 5
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2433(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string-length(""ABCDE"")";
            var expected = 5d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result ( assuming the string-value of the context node has 5 characters).
        /// string-length() = 5
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2434(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1/Para[1]";
            var testExpression = @"string-length()";
            var expected = 4d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// string-length("") = 0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2435(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string-length("""")";
            var expected = 0d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// string-length(string(child::*)) = 2
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2436(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"string-length(string(child::*))";
            var expected = 4d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// normalize-space("") = ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2473(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"normalize-space("""")";
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// normalize-space(" \t\n\r") = ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2474(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = "normalize-space(\" \t\n\r\")";
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// normalize-space(" Surrogate-Pair-String ") = ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2475(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var fourCircles = char.ConvertFromUtf32(0x1F01C);
            var testExpression = "normalize-space(\" " + fourCircles + " \")";
            var expected = fourCircles;

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// normalize-space(" AB") = "AB"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2437(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"normalize-space("" AB"")";
            var expected = @"AB";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// normalize-space("AB ") = "AB"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2438(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"normalize-space(""AB "")";
            var expected = @"AB";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// normalize-space("A B") = "A B"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2439(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"normalize-space(""A B"")";
            var expected = @"A B";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// normalize-space("     AB") = "AB"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2440(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"normalize-space(""   AB"")";
            var expected = @"AB";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// normalize-space("AB     ") = "AB"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2441(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"normalize-space(""AB   "")";
            var expected = @"AB";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// normalize-space("A     B") = "A B"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2442(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"normalize-space(""A   B"")";
            var expected = @"A B";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// normalize-space("     A     B     ") = "A B"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2443(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"normalize-space(""   A   B   "")";
            var expected = @"A B";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// normalize-space() = "A B"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2444(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test4/Para[1]";
            var testExpression = @"normalize-space()";
            var expected = @"A B";

            Utils.XPathStringTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Checks for preceding and trailing whitespace
        /// normalize-space('   abc   ')
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2445(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"normalize-space('   abc   ')";
            var expected = @"abc";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Checks for preceding and trailing whitespace (characters other than space)
        /// normalize-space('   abc   ')
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2446(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"normalize-space('			abc			')";
            var expected = @"abc";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Checks for a sequence of whitespace between characters
        /// normalize-space('a     bc')
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2447(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"normalize-space('a	bc')";
            var expected = @"a bc";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Checks for a sequence of whitespace between characters (characters other than space)
        /// normalize-space('a   bc')
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2448(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"normalize-space('a			bc')";
            var expected = @"a bc";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// A single tab should be replaced with a space
        /// normalize-space('a bc')
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2449(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"normalize-space('a	bc')";
            var expected = @"a bc";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// normalize-space(string(child::*)) = "A B"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2450(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test4";
            var testExpression = @"normalize-space(string(child::*))";
            var expected = @"A B";

            Utils.XPathStringTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify result.
        /// translate("", "abc", "ABC") = ""
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2472(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"translate("""", ""abc"", ""ABC"")";
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// translate("unicode", "unicode", "uppercase-unicode") = "uppercase -unicode"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2476(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = "translate(\"\0x03B1\0x03B2\0x03B3\", \"\0x03B1\0x03B2\0x03B3\", \"\0x0391\0x0392\0x0393\")";
            var expected = "\0x0391\0x0392\0x0393";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// translate("surrogate-pairs", "ABC", "") = "surrogate-pairs"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2477(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var fourOClock = char.ConvertFromUtf32(0x1F553);
            var fiveOClock = char.ConvertFromUtf32(0x1F554);
            var testExpression = @"translate(""" + fourOClock + fiveOClock + @""", ""ABC"", """")";
            var expected = fourOClock + fiveOClock;

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// translate("abc", "abca", "ABCZ") = "ABC"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2478(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"translate(""abc"", ""abca"", ""ABCZ"")";
            var expected = "ABC";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// translate("abc", "abc", "ABC") = "ABC"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2451(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"translate(""abc"", ""abc"", ""ABC"")";
            var expected = @"ABC";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// translate("aba", "b", "B") = "aBa"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2452(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"translate(""aba"", ""b"", ""B"")";
            var expected = @"aBa";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// translate("--aaa--", "abc-", "ABC") = "AAA"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2453(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"translate(""-aaa-"", ""abc-"", ""ABC"")";
            var expected = @"AAA";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Verify result.
        /// translate(string(child::*), "AB", "ab") = "aa"
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2454(Utils.NavigatorKind kind)
        {
            var xml = "xp004.xml";
            var startingNodePath = "/Doc/Test1";
            var testExpression = @"translate(string(child::*), ""est"", ""EST"")";
            var expected = @"TEST";

            Utils.XPathStringTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// string(NaN)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2455(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string(number(0 div 0))";
            var expected = double.NaN;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// string(-0)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2456(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string(-0)";
            var expected = 0d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// string(infinity)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2457(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string(number(1 div 0))";
            var expected = double.PositiveInfinity;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// string(-Infinity)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2458(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string(number(-1 div 0))";
            var expected = double.NegativeInfinity;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// for integers, leading zeros and decimal should be removed
        /// string(007.00)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2459(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string(007.00)";
            var expected = 7d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// string(-007.00)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2460(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"string(-007.00)";
            var expected = -7d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage: covers the substring() function with in a query
        /// child::*[substring(name(),0,1)="b"]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2461(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"child::*[substring(name(),0,1)=""b""]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage: covers the substring-after() function with in a query
        /// child::*[substring-after(name(),"b")="ook"]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2462(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"child::*[substring-after(name(),""b"")=""ook""]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Code coverage: covers the normalize-space() function with in a query
        /// child::*[normalize-space(" book")=name()]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2463(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"child::*[normalize-space("" book"")=name()]";
            var expected = new XPathResult(0);
            Utils.XPathNodesetTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Expected: namespace uri
        /// string() (namespace node)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2464(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var startingNodePath = "/ns:store/ns:booksection/namespace::NSbook";
            var testExpression = @"string()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "http://default.htm");
            var expected = @"http://book.htm";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: xml namespace uri
        /// string() (namespace node = xml)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2465(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var startingNodePath = "/ns:store/namespace::*[last()]";
            var testExpression = @"string()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "http://default.htm");
            var expected = @"http://www.w3.org/XML/1998/namespace";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: default namespace uri
        /// string() (namespace node = default ns)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2466(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var startingNodePath = "/ns:store/namespace::*[1]";
            var testExpression = @"string()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "http://default.htm");
            var expected = @"http://default.htm";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: uri of namespace
        /// string() (namespace node)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2467(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var testExpression = @"string(ns:store/ns:booksection/namespace::*)";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("ns", "http://default.htm");
            var expected = @"http://book.htm";

            Utils.XPathStringTest(kind, xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// substring("ABCDE", 1, -1)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2468(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring(""ABCDE"", 1, -1)";
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// substring("ABCDE", 1, -1 div 0)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2469(Utils.NavigatorKind kind)
        {
            var xml = "dummy.xml";
            var testExpression = @"substring(""ABCDE"", 1, -1 div 0)";
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// string(/bookstore/book/title)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void StringFunctionsTest2471(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"string(/bookstore/book/title)";
            var expected = @"Seven Years in Trenton";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }
    }
}
