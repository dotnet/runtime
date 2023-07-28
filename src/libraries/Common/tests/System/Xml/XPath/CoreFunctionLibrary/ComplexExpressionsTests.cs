// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using XPathTests.Common;
using Xunit;

namespace XPathTests.FunctionalTests.CoreFunctionLibrary
{
    /// <summary>
    /// Core Function Library - Complex Expressions
    /// </summary>
    public static partial class ComplexExpressionsTests
    {
        /// <summary>
        /// Complex expression for count(Utils.NavigatorKind kind)
        /// count(/bookstore/*[count(ancestor::*) = 1])
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void ComplexExpressionsTest272(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"count(/bookstore/*[count(ancestor::*) = 1])";
            var expected = 17d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Complex expression for local-name(Utils.NavigatorKind kind)
        /// local-name(/bookstore/magazine[3]/articles/story1/text()/following::*)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void ComplexExpressionsTest273(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"local-name(/bookstore/magazine[3]/articles/story1/text()/following::*)";
            var expected = @"details";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Complex expression for local-name(Utils.NavigatorKind kind)
        /// local-name(child::*/following::*[last()])
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void ComplexExpressionsTest274(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"local-name(child::*/following::*[last()])";
            var expected = @"title";

            Utils.XPathStringTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Complex expression for name(Utils.NavigatorKind kind)
        /// name(/bookstore/magazine[3]/articles/story1/text()/following::*)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void ComplexExpressionsTest275(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"name(/bookstore/magazine[3]/articles/story1/text()/following::*)";
            var expected = @"details";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Complex expression for name(Utils.NavigatorKind kind)
        /// name(child::*/following::*[last()])
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void ComplexExpressionsTest276(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"name(child::*/following::*[last()])";
            var expected = @"my:title";

            Utils.XPathStringTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Complex expression for namespace-uri(Utils.NavigatorKind kind)
        /// namespace-uri(/bookstore/magazine[3]/articles/story1/text()/following::*)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void ComplexExpressionsTest277(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"namespace-uri(/bookstore/magazine[3]/articles/story1/text()/following::*)";
            var expected = @"";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Complex expression for namespace-uri(Utils.NavigatorKind kind)
        /// namespace-uri(child::*/following::*[last()])
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void ComplexExpressionsTest278(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"namespace-uri(child::*/following::*[last()])";
            var expected = @"urn:http//www.placeholder-name-here.com/schema/";

            Utils.XPathStringTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Complex expression for namespace-uri(Utils.NavigatorKind kind)
        /// namespace-uri(/*/*)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void ComplexExpressionsTest279(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var testExpression = @"namespace-uri(/*/*)";
            var expected = @"http://default.htm";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Complex expression for namespace-uri(Utils.NavigatorKind kind)
        /// namespace-uri(/*/*/*[1])
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void ComplexExpressionsTest2710(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var testExpression = @"namespace-uri(/*/*/*[1])";
            var expected = @"http://book.htm";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// Complex expression for namespace-uri(Utils.NavigatorKind kind)
        /// namespace-uri(/*/*/*[2]/*[1])
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void ComplexExpressionsTest2711(Utils.NavigatorKind kind)
        {
            var xml = "name2.xml";
            var testExpression = @"namespace-uri(/*/*/*[2]/*[1])";
            var expected = @"http://book2.htm";

            Utils.XPathStringTest(kind, xml, testExpression, expected);
        }

        /// <summary>
        /// count((/comment() | /bookstore/book[2]/author[1]/publication/text())/following-sibling::node())
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void ComplexExpressionsTest2712(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression =
                @"count((/comment() | /bookstore/book[2]/author[1]/publication/text())/following-sibling::node())";
            var expected = 7d;

            Utils.XPathNumberTest(kind, xml, testExpression, expected);
        }
    }
}
