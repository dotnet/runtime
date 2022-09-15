// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using XPathTests.Common;
using Xunit;

namespace XPathTests.FunctionalTests.Location.Steps
{
    /// <summary>
    /// Location Steps (matches)
    /// </summary>
    public static partial class MatchesTests
    {
        /// <summary>
        /// Invalid Location Step - Missing axis . Expected error.
        /// /::bookstore
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest151(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"/::bookstore";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// Invalid Location Step - Missing Node test. Expected error.
        /// /child::
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest152(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"/child::";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// Invalid Location Step - Using an invalid axis. Expected error.
        /// /bookstore/sibling::book
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest153(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/sibling::book";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// Invalid Location Step - Multiple axis. Location step can have only one axis. Test expression uses multiple axis. Expected error.
        /// /bookstore/child::ancestor::book
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest154(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/child::ancestor::book";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// Invalid Location Step - Multiple node tests using | - Multiple node-tests are not allowed. Test uses | to union two node tests. Error expected.
        /// /bookstore/child::(book | magazine)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest155(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/child::(book | magazine)";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// Invalid Location Step - Multiple node tests using | - Multiple node-tests are not allowed. Test uses 'and' to combine two node tests. Error expected.
        /// /bookstore/book and magazine
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest156(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book and magazine";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// Invalid Location Step - Multiple node tests using 'or' - Multiple node-tests are not allowed. Test uses 'or' to combine two node tests. Error expected.
        /// /bookstore/book or magazine
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest157(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book or magazine";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// Valid Location step - Single predicate
        /// /bookstore/* [name()='book']
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest158(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book";
            var testExpression = @"/bookstore/* [name()='book']";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Valid Location Step - Multiple predicates
        /// /bookstore/* [name()='book' or name()='magazine'][name()='magazine']
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest159(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine";
            var testExpression = @"/bookstore/* [name()='book' or name()='magazine'][name()='magazine']";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Valid Location Step - No predicates
        /// /bookstore/book
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest1510(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[5]";
            var testExpression = @"/bookstore/book";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Check order of predicates - Test to check if the predicates are applied in the correct order. Should select the 3rd book node in the XML doc.
        /// /bookstore/book [position() = 1 or position() = 3 or position() = 6][position() = 2]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest1511(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[3]";
            var testExpression = @"/bookstore/book [position() = 1 or position() = 3 or position() = 6][position() = 2]";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected Error : Abbreviated Axis specifier '.' is a valid location step, but not allowed in matches
        /// /.
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest1512(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"/.";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected Error : Abbreviated Axis specifier '..' is a valid location step, but not allowed in matches
        /// /bookstore/book/..
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest1513(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"/bookstore/book/..";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Invalid Expression '..' with node test. Node test is not allowed with an abbreviated axis specifier
        /// /bookstore/*/title/.. book
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest1514(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"/bookstore/*/title/.. book";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Invalid Expression '.' with node test. Predicates are not allowed with abbreviated axis specifiers.
        /// /bookstore/*/title/.[name()='book']
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest1515(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book";
            var testExpression = @"/bookstore/*/title/.[name()='book']";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                startingNodePath: startingNodePath);
        }
    }
}
