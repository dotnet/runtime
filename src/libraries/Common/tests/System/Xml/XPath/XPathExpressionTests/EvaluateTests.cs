// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using XPathTests.Common;
using Xunit;

namespace XPathTests.XPathExpressionTests
{
    public class EvaluateTests
    {
        private const string xml = @"<DocumentElement>
    <Level1 Data='0'>
        <Name>first</Name>
        <Level2 Data='1'></Level2>
    </Level1>
    <Level1 Data='1'>
        <Name>second</Name>
        <Level2 Data='2'></Level2>
    </Level1>
    <Level1 Data='2'>
        <Name>third</Name>
        <Level2 Data='3'></Level2>
    </Level1>
    <Level1 Data='3'>
        <Name>last</Name>
        <Level2 Data='4'></Level2>
    </Level1>
</DocumentElement>";

        private static void EvaluateTestNonCompiled<T>(Utils.NavigatorKind kind, string toEvaluate, T expected)
        {
            var navigator = Utils.CreateNavigator(kind, xml);
            var result = navigator.Evaluate(toEvaluate);
            var convertedResult = Convert.ChangeType(result, typeof(T));

            Assert.Equal(expected, convertedResult);
        }

        private static void EvaluateTestCompiledXPathExpression<T>(Utils.NavigatorKind kind, string toEvaluate, T expected)
        {
            var navigator = Utils.CreateNavigator(kind, xml);
            var xPathExpression = XPathExpression.Compile(toEvaluate);
            var result = navigator.Evaluate(xPathExpression);
            var convertedResult = Convert.ChangeType(result, typeof(T));

            Assert.Equal(expected, convertedResult);
        }


        private static void EvaluateTestsBoth<T>(Utils.NavigatorKind kind, string toEvaluate, T expected)
        {
            EvaluateTestNonCompiled(kind, toEvaluate, expected);
            EvaluateTestCompiledXPathExpression(kind, toEvaluate, expected);
        }

        private static void EvaluateTestsErrors(Utils.NavigatorKind kind, string toEvaluate, string exceptionString)
        {
            Assert.Throws<XPathException>(() => EvaluateTestCompiledXPathExpression<object>(kind, toEvaluate, null));
            Assert.Throws<XPathException>(() => EvaluateTestNonCompiled<object>(kind, toEvaluate, null));
        }

        /// <summary>
        /// Pass in valid XPath Expression (return type = String)
        /// Priority: 0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_1(Utils.NavigatorKind kind)
        {
            EvaluateTestsBoth(kind, "string(1)", "1");
        }

        /// <summary>
        /// Pass in valid XPath Expression (return type = Number)
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_2(Utils.NavigatorKind kind)
        {
            EvaluateTestsBoth(kind, "number('1')", 1);
        }

        /// <summary>
        /// Pass in valid XPath Expression (return type = Boolean)
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_3(Utils.NavigatorKind kind)
        {
            EvaluateTestsBoth(kind, "true()", true);
        }

        /// <summary>
        /// Pass in empty String
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_5(Utils.NavigatorKind kind)
        {
            EvaluateTestsErrors(kind, string.Empty, "Xp_NodeSetExpected");
        }

        /// <summary>
        /// Pass in invalid XPath Expression (wrong syntax)
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_6(Utils.NavigatorKind kind)
        {
            EvaluateTestsErrors(kind, "string(1, 2)", "Xp_InvalidNumArgs");
        }


        private static void EvaluateTestNonCompiledNodeset(Utils.NavigatorKind kind, string toEvaluate, string[] expected)
        {
            var navigator = Utils.CreateNavigator(kind, xml);
            var iter = (XPathNodeIterator)navigator.Evaluate(toEvaluate);

            foreach (var e in expected)
            {
                iter.MoveNext();
                Assert.Equal(e, iter.Current.Value.Trim());
            }
        }

        private static void EvaluateTestCompiledNodeset(Utils.NavigatorKind kind, string toEvaluate, string[] expected)
        {
            var navigator = Utils.CreateNavigator(kind, xml);
            var xPathExpression = XPathExpression.Compile(toEvaluate);
            var iter = (XPathNodeIterator)navigator.Evaluate(xPathExpression);

            foreach (var e in expected)
            {
                iter.MoveNext();
                Assert.Equal(e, iter.Current.Value.Trim());
            }
        }

        /// <summary>
        /// Pass in valid XPath Expression
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_7(Utils.NavigatorKind kind)
        {
            EvaluateTestCompiledNodeset(kind, "DocumentElement/child::*", new[] { "first", "second", "third", "last" });
            EvaluateTestNonCompiledNodeset(kind, "DocumentElement/child::*", new[] { "first", "second", "third", "last" });
        }

        /// <summary>
        /// Pass in NULL
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_9(Utils.NavigatorKind kind)
        {
            EvaluateTestsErrors(kind, null, "Xp_ExprExpected");
        }

        /// <summary>
        /// Pass in invalid XPath Expression (wrong syntax)
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_10(Utils.NavigatorKind kind)
        {
            EvaluateTestsErrors(kind, "DocumentElement/child:::*", "Xp_InvalidToken");
        }

        /// <summary>
        /// Pass in two different XPath Expression in a row
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_11(Utils.NavigatorKind kind)
        {
            var navigator = Utils.CreateNavigator(kind, xml);

            navigator.Evaluate("child::*");
            navigator.Evaluate("descendant::*");
        }

        /// <summary>
        /// Pass in valid XPath Expression, then empty string
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_12(Utils.NavigatorKind kind)
        {
            var navigator = Utils.CreateNavigator(kind, xml);

            navigator.Select("/DocumentElement/child::*");
            Assert.Throws<XPathException>(() => navigator.Select(string.Empty));
        }
    }

    public class XPathEvaluateTests
    {
        [Fact]
        public static void EvaluateTextNode_1()
        {
            XElement element = XElement.Parse("<element>Text.</element>");
            IEnumerable result = (IEnumerable)element.XPathEvaluate("/text()");
            Assert.Equal(1, result.Cast<XText>().Count());
            Assert.Equal("Text.", result.Cast<XText>().First().ToString());
        }

        [Fact]
        public static void EvaluateTextNode_2()
        {
            XElement element = XElement.Parse("<root>1<element></element>2</root>");
            IEnumerable result = (IEnumerable)element.XPathEvaluate("/text()[1]");
            Assert.Equal(1, result.Cast<XText>().Count());
            Assert.Equal("1", result.Cast<XText>().First().ToString());
        }

        [Fact]
        public static void EvaluateTextNode_3()
        {
            XElement element = XElement.Parse("<root>1<element></element>2</root>");
            IEnumerable result = (IEnumerable)element.XPathEvaluate("/text()[2]");
            Assert.Equal(1, result.Cast<XText>().Count());
            Assert.Equal("2", result.Cast<XText>().First().ToString());
        }

        [Fact]
        public static void EvaluateTextNode_4()
        {
            XElement element = XElement.Parse("<root>1<element>2</element><element>3</element>4</root>");
            IEnumerable result = (IEnumerable)element.XPathEvaluate("/element/text()[1]");
            Assert.Equal(2, result.Cast<XText>().Count());
            Assert.Equal("2", result.Cast<XText>().First().ToString());
            Assert.Equal("3", result.Cast<XText>().Last().ToString());
        }
    }
}
