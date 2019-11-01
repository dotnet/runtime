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
    /// Core Function Library - Complex Expressions
    /// </summary>
    public static partial class ComplexExpressionsTests
    {
        /// <summary>
        /// Complex expression for count()
        /// count(/bookstore/*[count(ancestor::*) = 1])
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest272()
        {
            var xml = "books.xml";
            var testExpression = @"count(/bookstore/*[count(ancestor::*) = 1])";
            var expected = 17d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Complex expression for local-name()
        /// local-name(/bookstore/magazine[3]/articles/story1/text()/following::*)
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest273()
        {
            var xml = "books.xml";
            var testExpression = @"local-name(/bookstore/magazine[3]/articles/story1/text()/following::*)";
            var expected = @"details";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Complex expression for local-name()
        /// local-name(child::*/following::*[last()])
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest274()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"local-name(child::*/following::*[last()])";
            var expected = @"title";

            Utils.XPathStringTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Complex expression for name()
        /// name(/bookstore/magazine[3]/articles/story1/text()/following::*)
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest275()
        {
            var xml = "books.xml";
            var testExpression = @"name(/bookstore/magazine[3]/articles/story1/text()/following::*)";
            var expected = @"details";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Complex expression for name()
        /// name(child::*/following::*[last()])
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest276()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"name(child::*/following::*[last()])";
            var expected = @"my:title";

            Utils.XPathStringTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Complex expression for namespace-uri()
        /// namespace-uri(/bookstore/magazine[3]/articles/story1/text()/following::*)
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest277()
        {
            var xml = "books.xml";
            var testExpression = @"namespace-uri(/bookstore/magazine[3]/articles/story1/text()/following::*)";
            var expected = @"";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Complex expression for namespace-uri()
        /// namespace-uri(child::*/following::*[last()])
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest278()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"namespace-uri(child::*/following::*[last()])";
            var expected = @"urn:http//www.placeholder-name-here.com/schema/";

            Utils.XPathStringTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Complex expression for namespace-uri()
        /// namespace-uri(/*/*)
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest279()
        {
            var xml = "name2.xml";
            var testExpression = @"namespace-uri(/*/*)";
            var expected = @"http://default.htm";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Complex expression for namespace-uri()
        /// namespace-uri(/*/*/*[1])
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest2710()
        {
            var xml = "name2.xml";
            var testExpression = @"namespace-uri(/*/*/*[1])";
            var expected = @"http://book.htm";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Complex expression for namespace-uri()
        /// namespace-uri(/*/*/*[2]/*[1])
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest2711()
        {
            var xml = "name2.xml";
            var testExpression = @"namespace-uri(/*/*/*[2]/*[1])";
            var expected = @"http://book2.htm";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// count((/comment() | /bookstore/book[2]/author[1]/publication/text())/following-sibling::node())
        /// </summary>
        [Fact]
        public static void ComplexExpressionsTest2712()
        {
            var xml = "books.xml";
            var testExpression =
                @"count((/comment() | /bookstore/book[2]/author[1]/publication/text())/following-sibling::node())";
            var expected = 7d;

            Utils.XPathNumberTest(xml, testExpression, expected);
        }
    }
}
