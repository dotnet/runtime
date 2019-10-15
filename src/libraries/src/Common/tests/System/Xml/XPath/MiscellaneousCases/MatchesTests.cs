// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

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
        [Fact]
        public static void MatchesTest541()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"child::*[$$abc=1]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Match should throw an exception on expression that don't have a return type of nodeset
        /// true() and true()
        /// </summary>
        [Fact]
        public static void MatchesTest542()
        {
            var xml = "books.xml";
            var testExpression = @"true() and true()";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// Match should throw an exception on expression that don't have a return type of nodeset
        /// false() or true()
        /// </summary>
        [Fact]
        public static void MatchesTest543()
        {
            var xml = "books.xml";
            var testExpression = @"true() and true()";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// 1 and 1
        /// </summary>
        [Fact]
        public static void MatchesTest544()
        {
            var xml = "books.xml";
            var testExpression = @"1 and 1";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// 1
        /// </summary>
        [Fact]
        public static void MatchesTest545()
        {
            var xml = "books.xml";
            var testExpression = @"1";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// //node()[abc:xyz()]
        /// </summary>
        [Fact]
        public static void MatchesTest546()
        {
            var xml = "books.xml";
            var testExpression = @"//node()[abc:xyz()]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// //node()[abc:xyz()]
        /// </summary>
        [Fact]
        public static void MatchesTest547()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"//node()[abc:xyz()]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("abc", "http://abc.htm");

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                namespaceManager: namespaceManager, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Contains a function fasle(), which is not defined, so it should throw an exception
        /// descendant::node()/self::node() [self::text() = false() and self::attribute=fasle()]
        /// </summary>
        [Fact]
        public static void MatchesTest548()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"descendant::node()/self::node() [self::text() = false() and self::attribute=fasle()]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("abc", "http://abc.htm");

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                namespaceManager: namespaceManager, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// No namespace manager provided
        /// //*[abc()]
        /// </summary>
        [Fact]
        public static void MatchesTest549()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"//*[abc()]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Namespace manager provided
        /// //*[abc()]
        /// </summary>
        [Fact]
        public static void MatchesTest5410()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"//*[abc()]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("abc", "http://abc.htm");

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                namespaceManager: namespaceManager, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Trying several patterns connected with |
        /// /bookstore | /bookstore//@* | //magazine
        /// </summary>
        [Fact]
        public static void MatchesTest5411()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine/@frequency";
            var testExpression = @"/bookstore | /bookstore//@* | //magazine";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Trying several patterns connected with |
        /// /bookstore | /bookstore//@* | //magazine | comment()
        /// </summary>
        [Fact]
        public static void MatchesTest5412()
        {
            var xml = "books.xml";
            var startingNodePath = "//comment()";
            var testExpression = @"/bookstore | /bookstore//@* | //magazine | comment()";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Trying several patterns connected with |.  Fix test code to move to testexpr node, thus expected=true.
        /// /bookstore | /bookstore//@* | //magazine | comment() (true)
        /// </summary>
        [Fact]
        public static void MatchesTest5413()
        {
            var xml = "books.xml";
            var startingNodePath = "//book";
            var testExpression = @"/bookstore | /bookstore//@* | //magazine | comment()";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected Error
        /// /bookstore | /bookstore//@* | //magazine |
        /// </summary>
        [Fact]
        public static void MatchesTest5414()
        {
            var xml = "books.xml";
            var startingNodePath = "//book";
            var testExpression = @"/bookstore | /bookstore//@* | //magazine |";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }
    }
}
