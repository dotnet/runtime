// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Location.Paths.Predicates
{
    /// <summary>
    /// Location Paths - Predicates (matches)
    /// </summary>
    public static partial class MatchesTests
    {
        /// <summary>
        /// Verify: Returned node is correct (document order).
        /// Forward-Axis. Set predicate filter to return 3rd node
        /// </summary>
        [Fact]
        public static void MatchesTest111()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap";
            var testExpression = @"descendant::*[3]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects 2nd, 3rd, 4th and 5th element children of the context node.
        /// child::*[position() >= 2][position() <= 4]
        /// </summary>
        [Fact]
        public static void MatchesTest112()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1/Child3";
            var testExpression = @"child::*[position() >= 2][position() <= 4]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Simple expression - Should return author node with a country node in a publication node.
        /// book/author[publication/country]
        /// </summary>
        [Fact]
        public static void MatchesTest113()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author";
            var testExpression = @"book/author[publication/country]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate within a predicate - Return all authors with a last-name equal to the first last-name node.
        /// (book/author)[last-name=(/bookstore/book/author)[1]/last-name]
        /// </summary>
        [Fact]
        public static void MatchesTest114()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/author";
            var testExpression = @"(book/author)[last-name=(/bookstore/book/author/last-name)[1]]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return all authors with a last-name not equal to the first last-name node. Cascaded predicates using !=
        /// book/author[last-name!=(/bookstore/book/author)[1]/last-name]
        /// </summary>
        [Fact]
        public static void MatchesTest115()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[4]/author";
            var testExpression = @"book/author[last-name!=(/bookstore/book/author)[1]/last-name]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Cascaded predicates using <= Return all authors with a last name less than or equal to the first author's last-name.
        /// book/author[last-name<=(/bookstore/book/author[1]/last-name)]
        /// </summary>
        [Fact]
        public static void MatchesTest116()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author";
            var testExpression = @"book/author[last-name<=(/bookstore/book/author[1]/last-name)]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator = - Return all books with a price of 55.
        /// book[price=55]
        /// </summary>
        [Fact]
        public static void MatchesTest1110()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]";
            var testExpression = @"book[price=55]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator != - Return all authors of books with a price not equal to 55.56.
        /// book/author[parent::book/price!=55.56]
        /// </summary>
        [Fact]
        public static void MatchesTest1111()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/author";
            var testExpression = @"book/author[parent::book/price!=55.56]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator <= - Return all authors of books with a price less than or equal to 10.0001.
        /// book/author[parent::book/price&lt;=10.0001]
        /// </summary>
        [Fact]
        public static void MatchesTest1112()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[5]/author";
            var testExpression = @"book/author[parent::book/price<=10.0001]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator >= - Return all authors of books with a price greater than or equal to .0001.
        /// book/author[parent::book/price&gt;=.0001]
        /// </summary>
        [Fact]
        public static void MatchesTest1113()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[6]/author";
            var testExpression = @"book/author[parent::book/price>=.0001]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator < - Return all authors of books with a price less than 10.0.
        /// book/author[parent::book/price&lt;10.0]
        /// </summary>
        [Fact]
        public static void MatchesTest1114()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[6]/author";
            var testExpression = @"book/author[parent::book/price<10.0]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator > - Return all authors of books with a price greater than 55.0.
        /// book/author[parent::book/price&gt;55.0] (true)
        /// </summary>
        [Fact]
        public static void MatchesTest1115()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[3]/author";
            var testExpression = @"book/author[parent::book/price>55.0]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator > - Return all authors of books with a price greater than 55.0.
        /// book/author[parent::book/price&gt;55.0] (false)
        /// </summary>
        [Fact]
        public static void MatchesTest1116()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/author";
            var testExpression = @"book/author[parent::book/price>55.0]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a sting constant, using operator = - Return all book authors with a last-name equal to ""Bob"".
        /// book/author[last-name="Bob"]
        /// </summary>
        [Fact]
        public static void MatchesTest1117()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/author";
            var testExpression = @"book/author[last-name='Bob']";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a sting constant, using operator != - Return all book authors with a last-name not equal to ""Bob"".
        /// book/author[last-name!="Bob"]
        /// </summary>
        [Fact]
        public static void MatchesTest1118()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[4]/author";
            var testExpression = @"book/author[last-name!='Bob']";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a sting constant, using operator >= - Return all book authors with a last-name greater than or equal to ""Robinson"".
        /// book/author[last-name>="Robinson"]
        /// </summary>
        [Fact]
        public static void MatchesTest1119()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[4]";
            var testExpression = @"book/author[last-name>='Robinson']";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a sting constant, using operator > - Return all book authors with a last-name greater than ""R"".
        /// book/author[last-name>"R"]
        /// </summary>
        [Fact]
        public static void MatchesTest1120()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[4]";
            var testExpression = @"book/author[last-name>'R']";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a sting constant, using operator < - Return all book authors with a last-name less than ""Boc"".
        /// book/author[last-name<"Boc"]
        /// </summary>
        [Fact]
        public static void MatchesTest1121()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/author";
            var testExpression = @"book/author[last-name<'Boc']";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node to true() - Return all book authors with an award.
        /// book/author[award=true()]
        /// </summary>
        [Fact]
        public static void MatchesTest1122()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/author";
            var testExpression = @"book/author[award=true()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing with true() and false() - Return all book titles.
        /// book/title[true()!=false()]
        /// </summary>
        [Fact]
        public static void MatchesTest1123()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[5]/title";
            var testExpression = @"book/title[true()!=false()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing true() with a positive number - Return all book titles.
        /// book/title[true()=5]
        /// </summary>
        [Fact]
        public static void MatchesTest1124()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book";
            var testExpression = @"book/title[true()=5]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing false() with a string constant - Return all book titles.
        /// book/title[false()!='gramps']
        /// </summary>
        [Fact]
        public static void MatchesTest1125()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/title";
            var testExpression = @"book/title[false()!='gramps']";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing negative numbers - Return all magazine titles.
        /// magazine/title[-23.987 = -23.987]
        /// </summary>
        [Fact]
        public static void MatchesTest1126()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[1]/title";
            var testExpression = @"magazine/title[-23.987 = -23.987]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing number to string constant - Return all magazine titles.
        /// magazine/title[0!="z"]
        /// </summary>
        [Fact]
        public static void MatchesTest1127()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]/title";
            var testExpression = @"magazine/title[0!=' -  100   ']";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing string constant to string constant - Return all magazine titles.
        /// magazine/title["z"="z"]
        /// </summary>
        [Fact]
        public static void MatchesTest1128()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine/title";
            var testExpression = @"magazine/title['z'='z']";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Using a mathematical expression in the predicate - Return 3rd book's title.
        /// (book/title)[2+1]
        /// </summary>
        [Fact]
        public static void MatchesTest1129()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[3]/title";
            var testExpression = @"(book/title)[2+1]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Using a mathematical expression in the predicate - Return 2nd book's title.
        /// (book/title)[100-98]
        /// </summary>
        [Fact]
        public static void MatchesTest1130()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[2]/title";
            var testExpression = @"(book/title)[100-98]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Using a mathematical expression in the predicate - Return all book's titles.
        /// (book/title)[(8/4)=2]
        /// </summary>
        [Fact]
        public static void MatchesTest1131()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[3]/title";
            var testExpression = @"(book/title)[(8/4)=2]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Using a mathematical expression in the predicate - Return 4th book's title.
        /// (book/title)[2*2]
        /// </summary>
        [Fact]
        public static void MatchesTest1132()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[4]/title";
            var testExpression = @"(book/title)[2*2]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Using a non-integer number as the position - Return empty node-list.
        /// (book/title)[5.01]
        /// </summary>
        [Fact]
        public static void MatchesTest1133()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[5]/title";
            var testExpression = @"(book/title)[5.01]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Whitespace check - (Whitespace test) Return all book's author's last-names.
        /// book / author  /      last-name
        /// </summary>
        [Fact]
        public static void MatchesTest1134()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]//last-name";
            var testExpression = @"book / author  /      last-name";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Invalid expression - Error Case.
        /// child::node()[Schema|]
        /// </summary>
        [Fact]
        public static void MatchesTest1135()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"child::node()[Schema|]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate containing attribute - Return all nodes with a style attribute.
        /// *[@style]
        /// </summary>
        [Fact]
        public static void MatchesTest1139()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book";
            var testExpression = @"*[@style]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate using and - Return all author's with a degree and an award.\
        /// bookstore//author[degree and award]
        /// </summary>
        [Fact]
        public static void MatchesTest1140()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/author";
            var testExpression = @"bookstore//author[degree and award]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate using or - Return all author's with a degree or an award and a publication.
        /// bookstore//author[(degree or award) and publication]
        /// </summary>
        [Fact]
        public static void MatchesTest1141()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/author";
            var testExpression = @"bookstore//author[(degree or award) and publication]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate using not - Return all author's with a degree and not a publication.
        /// bookstore//author[degree and not(publication)]
        /// </summary>
        [Fact]
        public static void MatchesTest1142()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[6]/author";
            var testExpression = @"bookstore//author[degree and not(publication)]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// String length > 1 => predicate = true
        /// book["!"]
        /// </summary>
        [Fact]
        public static void MatchesTest1143()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book";
            var testExpression = @"book['!']";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return empty node-list.
        /// *[self::comment]
        /// </summary>
        [Fact]
        public static void MatchesTest1144()
        {
            var xml = "books.xml";
            var startingNodePath = "//comment()/parent::node()";
            var testExpression = @"*[self::comment()]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return first books price text node (12).
        /// book[position() = 1 or position() = 2]/preceding::*[1]/text()
        /// </summary>
        [Fact]
        public static void MatchesTest1145()
        {
            var xml = "books_2.xml";
            var testExpression = @"book[position() = 1 or position() = 2]/preceding::*[1]/text()";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// Should return ERROR.
        /// "/bookstore/book[ancestor(.)]
        /// </summary>
        [Fact]
        public static void MatchesTest1146()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book";
            var testExpression = @"/bookstore/book[ancestor(.)]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate uses ancestor axis
        /// /bookstore/*/title [ancestor::book]
        /// </summary>
        [Fact]
        public static void MatchesTest1147()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book/title";
            var testExpression = @"/bookstore/*/title[ancestor::book]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate uses following axis
        /// //*[following::book]
        /// </summary>
        [Fact]
        public static void MatchesTest1148()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[6]";
            var testExpression = @"//*[following::book]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate uses following axis
        /// /bookstore/magazine[following::book]
        /// </summary>
        [Fact]
        public static void MatchesTest1149()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[5]";
            var testExpression = @"/bookstore/magazine[following::book]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate uses preceding axis
        /// /bookstore/magazine[preceding::book[title='How To Fix Computers']]
        /// </summary>
        [Fact]
        public static void MatchesTest1150()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[6]";
            var testExpression = @"/bookstore/magazine[preceding::book[title='How To Fix Computers']]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate contains preceding-sibling axis
        /// /bookstore/magazine[preceding-sibling::book]
        /// </summary>
        [Fact]
        public static void MatchesTest1151()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[3]";
            var testExpression = @"/bookstore/magazine[preceding-sibling::book]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate contains ancestor-or-self axis
        /// /bookstore/magazine[ancestor-or-self::magazine]
        /// </summary>
        [Fact]
        public static void MatchesTest1152()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[1]";
            var testExpression = @"/bookstore/magazine[ancestor-or-self::magazine]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate contains child axis
        /// /bookstore/book [child::title]
        /// </summary>
        [Fact]
        public static void MatchesTest1153()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression = @"/bookstore/book[child::title]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate contains descendant axis
        /// /bookstore/* [descendant::title]
        /// </summary>
        [Fact]
        public static void MatchesTest1154()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[1]";
            var testExpression = @"/bookstore/* [descendant::title]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate contains parent axis
        /// /bookstore/*/title [parent::book]
        /// </summary>
        [Fact]
        public static void MatchesTest1155()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[3]/title";
            var testExpression = @"/bookstore/*/title [parent::book]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate contains following-sibling axis
        /// /bookstore/* [following-sibling::book]
        /// </summary>
        [Fact]
        public static void MatchesTest1156()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[1]";
            var testExpression = @"/bookstore/* [following-sibling::book]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate contains attribute axis
        /// /bookstore/* [attribute::frequency]
        /// </summary>
        [Fact]
        public static void MatchesTest1157()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[4]";
            var testExpression = @"/bookstore/* [attribute::frequency]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate contains namespace axis.  Fix test code to move to testexpr node, thus expected=true.
        /// //* [namespace::NSbook]
        /// </summary>
        [Fact]
        public static void MatchesTest1158()
        {
            var xml = "name.xml";
            var testExpression = @"//* [namespace::NSbook]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate contains descendant or self
        /// /bookstore/* [descendant-or-self::*]
        /// </summary>
        [Fact]
        public static void MatchesTest1159()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book";
            var testExpression = @"/bookstore/*[descendant-or-self::*]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// The first position of a node set is 1. Predicate uses 0. Expected empty node set.
        /// /bookstore/book[0]
        /// </summary>
        [Fact]
        public static void MatchesTest1160()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book";
            var testExpression = @"/bookstore/book[0]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate expression resulting in a number is converted to true, if number is equal to context position.
        /// /bookstore/book[1] [2]
        /// </summary>
        [Fact]
        public static void MatchesTest1161()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book";
            var testExpression = @"/bookstore/book[1] [2]";
            var expected = false;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicates filter node sets on reverse axis as if the document order was bottom up.
        /// /bookstore/book/ancestor::node()[2]
        /// </summary>
        [Fact]
        public static void MatchesTest1162()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"/bookstore/book/ancestor::node()[2]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected : should select all nodes that are the last child of their parent
        /// /bookstore//* [position()=count(parent::*/child::*)]
        /// </summary>
        [Fact]
        public static void MatchesTest1163()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/price";
            var testExpression = @"/bookstore//* [position()=count(parent::node()/child::*)]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Only bookstore should be selected, since its position in nodeset=2 and it is the second child of its parent (after the comment node). No other node's position in the node-set is the same as the number of children of its parent.
        /// //node() [position()=count(parent::node()/child::node())]
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void MatchesTest1164()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"//node() [position()=count(parent::node()/child::node())]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Select second last child of bookstore
        /// /bookstore/* [position()=last()-1]
        /// </summary>
        [Fact]
        public static void MatchesTest1165()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/my:book[2]";
            var testExpression = @"/bookstore/* [position()=last()-1]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("my", "urn:http//www.placeholder-name-here.com/schema/");
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Select the author node (checking position on reverse axis)
        /// /bookstore/book[1]/author/*/ancestor::* [position() = last()-2]
        /// </summary>
        [Fact]
        public static void MatchesTest1166()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[1]/author/*/ancestor::* [position() = last()-2]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// Predicate filters the elements in the node-set at number 8,10,12,55 (these numbers appear as text nodes in the document)
        /// //node() [position() = //*]
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void MatchesTest1167()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[6]";
            var testExpression = @"//node() [position() = //*]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Select the first magazine node
        /// /bookstore/book[7]/preceding::*[ self::magazine and position()=last()-count(//*[self::magazine])+21]
        /// </summary>
        [Fact]
        public static void MatchesTest1168()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[1]";
            var testExpression =
                @"/bookstore/book[7]/preceding::*[ self::magazine and position()=last()-count(//*[self::magazine])+21]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Select the last magazine node
        /// /bookstore/book[7]/preceding::* [self::magazine and position()=last()-count(//*[self::book])]
        /// </summary>
        [Fact]
        public static void MatchesTest1169()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[last()]";
            var testExpression =
                @"/bookstore/book[7]/preceding::* [self::magazine and position()=last()-count(//*[self::book])]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Select the last magazine node
        /// /bookstore/book[7]/preceding::* [self::magazine and position()=last()-count(//book)]
        /// </summary>
        [Fact]
        public static void MatchesTest1170()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/magazine[last()]";
            var testExpression = @"/bookstore/book[7]/preceding::* [self::magazine and position()=last()-count(//book)]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Select all nodes that are not elements
        /// //node() [boolean(self::*)=false()]
        /// </summary>
        [Fact]
        public static void MatchesTest1171()
        {
            var xml = "books.xml";
            var startingNodePath = "//comment()";
            var testExpression = @"//node() [boolean(self::*)=false()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Select the 2nd and 3rd book nodes in the document
        /// /bookstore/book[1]/following::* [following-sibling::*[self::magazine] and count(following-sibling::magazine)>5]
        /// </summary>
        [Fact]
        public static void MatchesTest1172()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression =
                @"/bookstore/book[1]/following::* [following-sibling::*[self::magazine] and count(following-sibling::magazine)>5]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Uses self
        /// /bookstore/book[1]/self::* [(following-sibling::*[self::magazine]) and count(following-sibling::magazine)>5 and boolean(preceding-sibling::*)=false()][position()=last() and position()= 1]
        /// </summary>
        [Fact]
        public static void MatchesTest1173()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]";
            var testExpression =
                @"/bookstore/book[1]/self::* [(following-sibling::*[self::magazine]) and count(following-sibling::magazine)>5 and boolean(preceding-sibling::*)=false()][position()=last() and position()= 1]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Regression case for 66458
        /// bookstore//author[true() and true()]
        /// </summary>
        [Fact]
        public static void MatchesTest1174()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/author";
            var testExpression = @"bookstore//author[true() and true()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Regression case for 66458
        /// bookstore//author[true() or true()]
        /// </summary>
        [Fact]
        public static void MatchesTest1175()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[7]/author";
            var testExpression = @"bookstore//author[true() or true()]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// /bookstore//*[position()=3]
        /// </summary>
        [Fact]
        public static void MatchesTest1176()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/price";
            var testExpression = @"/bookstore//*[position()=3]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// /bookstore//*[position()=3][.="12"]
        /// </summary>
        [Fact]
        public static void MatchesTest1177()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/price";
            var testExpression = @"/bookstore//*[position()=3][.=""12""]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// /bookstore/*[position()=1][title="Seven Years in Trenton"]/author[count(child::*)=4][count(preceding-sibling::*)=1]
        /// </summary>
        [Fact]
        public static void MatchesTest1178()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/author";
            var testExpression =
                @"/bookstore/*[position()=1][title=""Seven Years in Trenton""]/author[count(child::*)=4][count(preceding-sibling::*)=1]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// /bookstore/*[position()=1]/author[count(child::*)=4][count(preceding-sibling::*)=1]
        /// </summary>
        [Fact]
        public static void MatchesTest1179()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book[1]/author";
            var testExpression = @"/bookstore/*[position()=1]/author[count(child::*)=4][count(preceding-sibling::*)=1]";
            var expected = true;

            Utils.XPathMatchTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Invalid namecharacter
        /// book[!]
        /// </summary>
        [Fact]
        public static void MatchesTest1180()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore/book";
            var testExpression = @"book[!]";

            Utils.XPathMatchTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }
    }
}
