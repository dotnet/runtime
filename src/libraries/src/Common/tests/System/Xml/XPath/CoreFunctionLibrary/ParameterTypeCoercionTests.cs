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
    /// Core Function Library - Parameter Type Coercion
    /// </summary>
    public static partial class ParameterTypeCoercionTests
    {
        /// <summary>
        /// count() can only take node sets as arguments.
        /// count(string('book')])
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest281()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"count(string('book'))";

            Utils.XPathNumberTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// count() can only take node sets as arguments.
        /// count(true())
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest282()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"count(true())";

            Utils.XPathNumberTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// count() can only take node sets as arguments.
        /// count(10)
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest283()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"count(10)";

            Utils.XPathNumberTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// count() can only take node sets as arguments.
        /// count()
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest284()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"count()";

            Utils.XPathNumberTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// expression returns empty node set
        /// count(//foo)
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest285()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"count(//foo)";
            var expected = 0d;

            Utils.XPathNumberTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// local-name() can only take node sets as arguments.
        /// local-name(string('book'))
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest286()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"local-name(string('book'))";

            Utils.XPathStringTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// local-name() can only take node sets as arguments.
        /// local-name(true())
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest287()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"local-name(true())";

            Utils.XPathStringTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// local-name() can only take node sets as arguments.
        /// local-name(10)
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest288()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"local-name(10)";

            Utils.XPathStringTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// local-name() can only take node sets as arguments.
        /// local-name()
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest289()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"local-name()";
            var expected = @"bookstore";

            Utils.XPathStringTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// expression returns empty node set
        /// local-name(//foo)
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2810()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"local-name(//foo)";
            var expected = @"";

            Utils.XPathStringTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// name() can only take node sets as arguments.
        /// name(string('book'))
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2811()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"name(string('book'))";

            Utils.XPathStringTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// name() can only take node sets as arguments.
        /// name(true())
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2812()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"name(true())";

            Utils.XPathStringTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// name() can only take node sets as arguments.
        /// name(10)
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2813()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"name(10)";

            Utils.XPathStringTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// expression returns empty node set
        /// name(//foo)
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2814()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"name(//foo)";
            var expected = @"";

            Utils.XPathStringTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// namespace-uri() can only take node sets as arguments.
        /// namespace-uri(string('book'))
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2815()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"namespace-uri(string('book'))";

            Utils.XPathStringTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// namespace-uri() can only take node sets as arguments.
        /// namespace-uri(true())
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2816()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"namespace-uri(true())";

            Utils.XPathStringTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// namespace-uri() can only take node sets as arguments.
        /// namespace-uri(10)
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2817()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"namespace-uri(10)";

            Utils.XPathStringTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// expression returns empty node set
        /// namespace-uri(//foo)
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2818()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"namespace-uri(//foo)";
            var expected = @"";

            Utils.XPathStringTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// position() takes no args
        /// position(string('book')])
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2819()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"position(string('book'))";

            Utils.XPathNumberTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// position() takes no args
        /// position(true())
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2820()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"position(true())";

            Utils.XPathNumberTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// position() takes no args
        /// position(10)
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2821()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"position(10)";

            Utils.XPathNumberTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// position() takes no args
        /// position(//foo)
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2822()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"position(//foo)";

            Utils.XPathNumberTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// last() takes no args
        /// last(string('book')])
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2823()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"last(string('book'))";

            Utils.XPathNumberTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// last() takes no args
        /// last(true())
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2824()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"last(true())";

            Utils.XPathNumberTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// last() takes no args
        /// last(10)
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2825()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"last(10)";

            Utils.XPathNumberTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// last() takes no args
        /// last(//foo)
        /// </summary>
        [Fact]
        public static void ParameterTypeCoercionTest2826()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"last(//foo)";

            Utils.XPathNumberTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }
    }
}
