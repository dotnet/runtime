// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Location.Paths.Axes.ComplexExpressions
{
    /// <summary>
    /// Location Paths - Axes (Complex expressions) (matches)
    /// </summary>
    public static partial class MatchesTests
    {
        /// <summary>
        /// Context node has an ancestor 'publication' so matches should return true
        /// node()[starts-with(string(name()),'p')]//node()[local-name()=""]
        /// </summary>
        [Fact]
        public static void MatchesTest41()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[3]/author/publication/first.name/text()";
            var testExpression = @"node()[starts-with(string(name()),'p')]//node()[local-name()=""""]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: true
        /// node()//node()[local-name()=""]
        /// </summary>
        [Fact]
        public static void MatchesTest42()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[3]/author/publication/first.name/text()";
            var testExpression = @"node()//node()[local-name()=""""]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Uses namespace
        /// my:*
        /// </summary>
        [Fact]
        public static void MatchesTest43()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/*[13]";
            var testExpression = @"my:*";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("my", "urn:http//www.placeholder-name-here.com/schema/");
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: true
        /// bookstore//articles[story1[details]]//node()
        /// </summary>
        [Fact]
        public static void MatchesTest44()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]/articles/story2/stock/node()";
            var testExpression = @"bookstore//articles[story1[details]]//node()";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: true
        /// @dt:dt[name()="dt:dt"][namespace-uri()="urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/"][local-name()="dt"]
        /// </summary>
        [Fact]
        public static void MatchesTest45()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]/articles/story2/date/@*";
            var testExpression =
                @"@dt:dt[name()=""dt:dt""][namespace-uri()=""urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/""][local-name()=""dt""]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("dt", "urn:uuid:C2F41010-65B3-11d1-A29F-00AA00C14882/");
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// book[3]//node()
        /// </summary>
        [Fact]
        public static void MatchesTest46()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[3]/author/publication/first.name/text()";
            var testExpression = @"book[3]//node()";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// //book[3]//node()//text()
        /// </summary>
        [Fact]
        public static void MatchesTest47()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[3]/author/publication/first.name/text()";
            var testExpression = @"//book[3]//node()//text()";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }
    }
}
