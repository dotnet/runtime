// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Expressions.SetContextFunctionalTests
{
    /// <summary>
    /// XPathExpression - SetContext Functional Tests (matches)
    /// </summary>
    public static partial class MatchesTests
    {
        /// <summary>
        /// Match node with qname
        /// //NSbook:book
        /// </summary>
        [Fact]
        public static void MatchesTest441()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection/NSbook:book[1]";
            var testExpression = @"//NSbook:book[1]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSbook", "http://book.htm");
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Prefix is not defined, should throw an error
        /// //NSbook:book
        /// </summary>
        [Fact]
        public static void MatchesTest442()
        {
            var xml = "name.xml";
            var testExpression = @"//NSbook:book[1]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// use of multiple namespaces
        /// /doc/prefix1:elem/prefix2:elem
        /// </summary>
        [Fact]
        public static void MatchesTest443()
        {
            var xml = "name4.xml";
            var startingNodePath = "/doc/*[2]/*[1]";
            var testExpression = @"/doc/prefix1:elem/prefix2:elem";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("prefix1", "http://prefix1.htm");
            namespaceManager.AddNamespace("prefix2", "http://prefix2.htm");
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Prefix points to a namespace that is not defined in the document, should not match
        /// //NSbook:book
        /// </summary>
        [Fact]
        public static void MatchesTest444()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection/*[1]";
            var testExpression = @"//NSbook:book[1]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSbook", "http://notbook.htm");
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Uses default namespace in the XmlNamespaceManager. The document has the namespace defined with a prefix. XPath should not match this node since it is not in null namespace.
        /// (//book)[1]
        /// </summary>
        [Fact]
        public static void MatchesTest445()
        {
            var xml = "name.xml";
            var startingNodePath = "/store/booksection/*[1]";
            var testExpression = @"(//book)[1]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("", "http://book.htm");

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                namespaceManager: namespaceManager, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// The document's default namespace is also the default namespace in the XmlNamespaceManager, XPath should not be able to match the default namespaces, since it will be treated as null namespace.
        /// //book[1]
        /// </summary>
        [Fact]
        public static void MatchesTest446()
        {
            var xml = "name2.xml";
            var startingNodePath = "/*[1]/*[1]/*[3]";
            var testExpression = @"//book[1]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("", "http://default.htm");
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// The document's default namespace is defined with a prefix in the XmlNamespaceManager, XPath should find the nodes with the default namespace in the document.
        /// //foo:book[1]
        /// </summary>
        [Fact]
        public static void MatchesTest447()
        {
            var xml = "name2.xml";
            var startingNodePath = "/*[1]/*[1]/*[3]";
            var testExpression = @"//foo:book[1]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("foo", "http://default.htm");
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }
    }
}
