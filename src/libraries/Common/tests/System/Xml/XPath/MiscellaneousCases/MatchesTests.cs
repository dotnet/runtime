// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using XPathTests.Common;
using Xunit;

namespace XPathTests.FunctionalTests.MiscellaneousCases
{
    /// <summary>
    /// Miscellaneous Cases (matches)
    /// </summary>
    public static partial class MatchesTests
    {
        /// <summary>
        /// Throw an exception on undefined variables
        /// child::*[$$abc=1]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest541(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"child::*[$$abc=1]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Match should throw an exception on expression that don't have a return type of nodeset
        /// true() and true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest542(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"true() and true()";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// Match should throw an exception on expression that don't have a return type of nodeset
        /// false() or true(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest543(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"true() and true()";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// 1 and 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest544(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"1 and 1";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest545(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"1";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// //node()[abc:xyz()]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest546(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var testExpression = @"//node()[abc:xyz()]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression);
        }

        /// <summary>
        /// //node()[abc:xyz()]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest547(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"//node()[abc:xyz()]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("abc", "http://abc.htm");

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                namespaceManager: namespaceManager, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Contains a function fasle(), which is not defined, so it should throw an exception
        /// descendant::node()/self::node() [self::text() = false() and self::attribute=fasle()]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest548(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant::node()/self::node() [self::text() = false() and self::attribute=fasle()]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("abc", "http://abc.htm");

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                namespaceManager: namespaceManager, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// No namespace manager provided
        /// //*[abc()]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest549(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"//*[abc()]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Namespace manager provided
        /// //*[abc()]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest5410(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"//*[abc()]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("abc", "http://abc.htm");

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                namespaceManager: namespaceManager, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Trying several patterns connected with |
        /// /bookstore | /bookstore//@* | //magazine
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest5411(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine/@frequency";
            var testExpression = @"/bookstore | /bookstore//@* | //magazine";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Trying several patterns connected with |
        /// /bookstore | /bookstore//@* | //magazine | comment(Utils.NavigatorKind kind)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest5412(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "//comment()";
            var testExpression = @"/bookstore | /bookstore//@* | //magazine | comment()";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Trying several patterns connected with |.  Fix test code to move to testexpr node, thus expected=true.
        /// /bookstore | /bookstore//@* | //magazine | comment() (true)
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest5413(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "//book";
            var testExpression = @"/bookstore | /bookstore//@* | //magazine | comment()";
            var expected = true;

            Utils.XPathMatchTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected Error
        /// /bookstore | /bookstore//@* | //magazine |
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void MatchesTest5414(Utils.NavigatorKind kind)
        {
            var xml = "books.xml";
            var startingNodePath = "//book";
            var testExpression = @"/bookstore | /bookstore//@* | //magazine |";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                startingNodePath: startingNodePath);
        }
    }
}
