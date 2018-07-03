// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit;
using XPathTests.Common;

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

        private static void EvaluateTestNonCompiled<T>(string toEvaluate, T expected)
        {
            var navigator = Utils.CreateNavigator(xml);
            var result = navigator.Evaluate(toEvaluate);
            var convertedResult = Convert.ChangeType(result, typeof(T));

            Assert.Equal(expected, convertedResult);
        }

        private static void EvaluateTestCompiledXPathExpression<T>(string toEvaluate, T expected)
        {
            var navigator = Utils.CreateNavigator(xml);
            var xPathExpression = XPathExpression.Compile(toEvaluate);
            var result = navigator.Evaluate(xPathExpression);
            var convertedResult = Convert.ChangeType(result, typeof(T));

            Assert.Equal(expected, convertedResult);
        }


        private static void EvaluateTestsBoth<T>(string toEvaluate, T expected)
        {
            EvaluateTestNonCompiled(toEvaluate, expected);
            EvaluateTestCompiledXPathExpression(toEvaluate, expected);
        }

        private static void EvaluateTestsErrors(string toEvaluate, string exceptionString)
        {
            Assert.Throws<XPathException>(() => EvaluateTestCompiledXPathExpression<object>(toEvaluate, null));
            Assert.Throws<XPathException>(() => EvaluateTestNonCompiled<object>(toEvaluate, null));
        }

        /// <summary>
        /// Pass in valid XPath Expression (return type = String)
        /// Priority: 0
        /// </summary>
        [Fact]
        public static void Variation_1()
        {
            EvaluateTestsBoth("string(1)", "1");
        }

        /// <summary>
        /// Pass in valid XPath Expression (return type = Number)
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_2()
        {
            EvaluateTestsBoth("number('1')", 1);
        }

        /// <summary>
        /// Pass in valid XPath Expression (return type = Boolean)
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_3()
        {
            EvaluateTestsBoth("true()", true);
        }

        /// <summary>
        /// Pass in empty String
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_5()
        {
            EvaluateTestsErrors(string.Empty, "Xp_NodeSetExpected");
        }

        /// <summary>
        /// Pass in invalid XPath Expression (wrong syntax)
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_6()
        {
            EvaluateTestsErrors("string(1, 2)", "Xp_InvalidNumArgs");
        }


        private static void EvaluateTestNonCompiledNodeset(string toEvaluate, string[] expected)
        {
            var navigator = Utils.CreateNavigator(xml);
            var iter = (XPathNodeIterator)navigator.Evaluate(toEvaluate);

            foreach (var e in expected)
            {
                iter.MoveNext();
                Assert.Equal(e, iter.Current.Value.Trim());
            }
        }

        private static void EvaluateTestCompiledNodeset(string toEvaluate, string[] expected)
        {
            var navigator = Utils.CreateNavigator(xml);
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
        [Fact]
        public static void Variation_7()
        {
            EvaluateTestCompiledNodeset("DocumentElement/child::*", new[] { "first", "second", "third", "last" });
            EvaluateTestNonCompiledNodeset("DocumentElement/child::*", new[] { "first", "second", "third", "last" });
        }

        /// <summary>
        /// Pass in NULL
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_9()
        {
            EvaluateTestsErrors(null, "Xp_ExprExpected");
        }

        /// <summary>
        /// Pass in invalid XPath Expression (wrong syntax)
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_10()
        {
            EvaluateTestsErrors("DocumentElement/child:::*", "Xp_InvalidToken");
        }

        /// <summary>
        /// Pass in two different XPath Expression in a row
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_11()
        {
            var navigator = Utils.CreateNavigator(xml);

            navigator.Evaluate("child::*");
            navigator.Evaluate("descendant::*");
        }

        /// <summary>
        /// Pass in valid XPath Expression, then empty string
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_12()
        {
            var navigator = Utils.CreateNavigator(xml);

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
