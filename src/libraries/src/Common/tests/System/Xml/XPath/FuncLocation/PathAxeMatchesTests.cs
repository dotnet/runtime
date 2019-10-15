// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Location.Paths.Axes
{
    /// <summary>
    /// Location Paths - Axes (matches)
    /// </summary>
    public static partial class MatchesTests
    {
        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// ancestor::* (Matches)
        /// </summary>
        [Fact]
        public static void MatchesTest71()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para/Origin";
            var testExpression = @"ancestor::*";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// attribute::* (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest72()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"attribute::*";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node). Fix test code to move to testexpr node, thus expected=true.
        /// attribute::* (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest73()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"attribute::*";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// /para/attribute::* (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest74()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"/Doc/Chap/Title/attribute::*";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// /para/attribute::* (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest75()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"/Doc/Chap/attribute::*";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// child::* (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest76()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"child::*";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).
        /// child::* (Matches = false)
        /// </summary>
        [Fact]
        public static void MatchesTest77()
        {
            var xml = "xp002.xml";
            var startingNodePath = "/Doc/Chap/Title/@Attr1";
            var testExpression = @"child::*";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: True (based on context node).
        /// /para/child::* (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest78()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Title";
            var testExpression = @"/Doc/child::*";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: False (based on context node).  Fix test code to move to testexpr node, thus expected=true.
        /// /para/child::* (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest79()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Title";
            var testExpression = @"/Doc/child::*";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// descendant::* (Matches)
        /// </summary>
        [Fact]
        public static void MatchesTest710()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"descendant::*";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected Error, not supported for matches
        /// descendant-or-self::* (Matches)
        /// </summary>
        [Fact]
        public static void MatchesTest711()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"descendant-or-self::*";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// following::* (Matches)
        /// </summary>
        [Fact]
        public static void MatchesTest712()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"following::*";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// following-sibling::* (Matches)
        /// </summary>
        [Fact]
        public static void MatchesTest713()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"following-sibling::*";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// parent::* (Matches)
        /// </summary>
        [Fact]
        public static void MatchesTest714()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para";
            var testExpression = @"parent::*";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// preceding::* (Matches)
        /// </summary>
        [Fact]
        public static void MatchesTest715()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"preceding::*";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// preceding-sibling::* (Matches)
        /// </summary>
        [Fact]
        public static void MatchesTest716()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"preceding-sibling::*";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// self::* (Matches)
        /// </summary>
        [Fact]
        public static void MatchesTest717()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"self::*";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the document root.
        /// / (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest718()
        {
            var xml = "xp001.xml";
            var testExpression = @"/";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Expected: Selects the document root.  Fix test code to move to testexpr node, thus expected=true.
        /// / (Matches = true)
        /// </summary>
        [Fact]
        public static void MatchesTest719()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"/";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// ancestor::bookstore
        /// </summary>
        [Fact]
        public static void MatchesTest720()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"ancestor::bookstore";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// ancestor-or-self::node()
        /// </summary>
        [Fact]
        public static void MatchesTest721()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"ancestor-or-self::node()";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// descendant::title
        /// </summary>
        [Fact]
        public static void MatchesTest722()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"descendant::title";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// descendant-or-self::node()
        /// </summary>
        [Fact]
        public static void MatchesTest723()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"descendant-or-self::node()";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// parent::bookstore
        /// </summary>
        [Fact]
        public static void MatchesTest724()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"parent::bookstore";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// following::book
        /// </summary>
        [Fact]
        public static void MatchesTest725()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following::book";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// following-sibling::node()
        /// </summary>
        [Fact]
        public static void MatchesTest726()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"following-sibling::node()";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// preceding::book
        /// </summary>
        [Fact]
        public static void MatchesTest727()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[6]";
            var testExpression = @"preceding::book";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// preceding-sibling::magazine
        /// </summary>
        [Fact]
        public static void MatchesTest728()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[6]";
            var testExpression = @"preceding-sibling::magazine";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// self::book
        /// </summary>
        [Fact]
        public static void MatchesTest729()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[6]";
            var testExpression = @"self::book";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// bookstore//book/parent::bookstore//title
        /// </summary>
        [Fact]
        public static void MatchesTest730()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[6]/title";
            var testExpression = @"bookstore//book/parent::bookstore//title";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// //magazine/ancestor-or-self::bookstore//book[6]
        /// </summary>
        [Fact]
        public static void MatchesTest731()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[6]";
            var testExpression = @"//magazine/ancestor-or-self::bookstore//book[6]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error (Not supported for Matches).
        /// bookstore/self::bookstore/magazine
        /// </summary>
        [Fact]
        public static void MatchesTest732()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine";
            var testExpression = @"bookstore/self::bookstore/magazine";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Error
        /// string('abc')
        /// </summary>
        [Fact]
        public static void MatchesTest733()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine";
            var testExpression = @"string('abc')";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// bookstore|bookstore/magazine|//title
        /// </summary>
        [Fact]
        public static void MatchesTest734()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine/title";
            var testExpression = @"bookstore|bookstore/magazine|//title";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// /bookstore/book|/bookstore/magazine/@*|/bookstore/book[last()]/@style
        /// </summary>
        [Fact]
        public static void MatchesTest735()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/@style";
            var testExpression = @"/bookstore/book|/bookstore/magazine/@*|/bookstore/book[last()]/@style";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected Error : following::* not supported for matches
        /// /bookstore/book|/bookstore/magazine/@*|/bookstore/book[last()]/@style/following::*
        /// </summary>
        [Fact]
        public static void MatchesTest736()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/@style";
            var testExpression = @"/bookstore/book|/bookstore/magazine/@*|/bookstore/book[last()]/@style/following::*";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// /bookstore/book[last()]/title[text()]
        /// </summary>
        [Fact]
        public static void MatchesTest737()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/title";
            var testExpression = @"/bookstore/book[last()]/title[text()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// /bookstore/*[last()]/node()
        /// </summary>
        [Fact]
        public static void MatchesTest738()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/my:book[3]/my:title";
            var testExpression = @"/bookstore/*[last()]/node()";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("my", "urn:http//www.placeholder-name-here.com/schema/");
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// 72687
        /// child::*[position() =3][position() =3]
        /// </summary>
        [Fact]
        public static void MatchesTest739()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child3";
            var testExpression = @"child::*[position() =3][position() =3]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// 72687
        /// child::*[position() =3 and position() =3]
        /// </summary>
        [Fact]
        public static void MatchesTest740()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child3";
            var testExpression = @"child::*[position() =3 and position() =3]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Regression case for 71617
        /// child::*[position() >1][position() <=3]
        /// </summary>
        [Fact]
        public static void MatchesTest741()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child3";
            var testExpression = @"child::*[position() >1][position() <=3]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }
    }
}
