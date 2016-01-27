// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Location.Paths
{
    /// <summary>
    /// Location Paths - Predicates
    /// </summary>
    public static partial class PredicatesTests
    {
        /// <summary>
        /// Verify: Returned node is correct (document order).
        /// Forward-Axis. Set predicate filter to return 3rd node
        /// </summary>
        [Fact]
        public static void PredicatesTest101()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"descendant::*[3]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value =
                        "\n   XPath test\n   First paragraph  Nested  Paragraph  End of first paragraph \n   Second paragraph \n "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Verify: Returned node is correct (reverse document order).
        /// Reverse Axis. Set predicate filter to return 3rd node.
        /// </summary>
        [Fact]
        public static void PredicatesTest102()
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc/Chap/Para/Para/Origin";
            var testExpression = @"ancestor::*[3]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Chap",
                    Name = "Chap",
                    HasNameTable = true,
                    Value =
                        "\n   XPath test\n   First paragraph  Nested  Paragraph  End of first paragraph \n   Second paragraph \n "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects 2nd, 3rd, 4th and 5th element children of the context node.
        /// child::*[position() >= 2][position() <= 4]
        /// </summary>
        [Fact]
        public static void PredicatesTest103()
        {
            var xml = "xp005.xml";
            var startingNodePath = "Doc/Test1";
            var testExpression = @"child::*[position() >= 2][position() <= 4]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child2",
                    Name = "Child2",
                    HasNameTable = true,
                    Value = "Second"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child3",
                    Name = "Child3",
                    HasNameTable = true,
                    Value = "Third"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child4",
                    Name = "Child4",
                    HasNameTable = true,
                    Value = "Fourth"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Child5",
                    Name = "Child5",
                    HasNameTable = true,
                    Value = "Last"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Simple expression - Should return author node with a country node in a publication node.
        /// book/author[publication/country]
        /// </summary>
        [Fact]
        public static void PredicatesTest104()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[publication/country]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate within a predicate - Return all authors with a last-name equal to the first last-name node.
        /// (book/author)[last-name=(/bookstore/book/author)[1]/last-name]
        /// </summary>
        [Fact]
        public static void PredicatesTest105()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"(book/author)[last-name=(/bookstore/book/author)[1]/last-name]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return all authors with a last-name not equal to the first last-name node. Cascaded predicates using !=
        /// book/author[last-name!=(/bookstore/book/author)[1]/last-name]
        /// </summary>
        [Fact]
        public static void PredicatesTest106()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[last-name!=(/bookstore/book/author)[1]/last-name]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Cascaded predicates using <= Return all authors with a last name less than or equal to the first author's last-name.
        /// book/author[last-name<=(/bookstore/book/author[1]/last-name)]
        /// </summary>
        [Fact]
        public static void PredicatesTest107()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[last-name<=(/bookstore/book/author[1]/last-name)]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Cascaded predicates using >= Return all authors with a first name greater than or equal to the first author's first-name.
        /// book/author[first-name>=(/bookstore/book/author[1]/first-name)]
        /// </summary>
        [Fact]
        public static void PredicatesTest108()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[first-name>=(/bookstore/book/author[1]/first-name)]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Cascaded predicates using > - Return all authors with a first-name greater than the first book's title.
        /// book/author[first-name>(/bookstore/book/title)[1]]
        /// </summary>
        [Fact]
        public static void PredicatesTest109()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[first-name>(/bookstore/book/title)[1]]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Cascaded predicates using < - Return all authors with a first-name less than the first book's title.
        /// book/author[first-name<(/bookstore/book/title)[1]]
        /// </summary>
        [Fact]
        public static void PredicatesTest1010()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[first-name<(/bookstore/book/title)[1]]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator = - Return all books with a price of 55.
        /// book[price=55]
        /// </summary>
        [Fact]
        public static void PredicatesTest1011()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[price=55]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator != - Return all authors of books with a price not equal to 55.56.
        /// book/author[/bookstore/book/price!=55.56]
        /// </summary>
        [Fact]
        public static void PredicatesTest1012()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[/bookstore/book/price!=55.56]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator <= - Return all authors of books with a price less than or equal to 10.0001.
        /// book/author[/bookstore/book/price<=10.0001]
        /// </summary>
        [Fact]
        public static void PredicatesTest1013()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[/bookstore/book/price<=10.0001]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator >= - Return all authors of books with a price greater than or equal to .0001.
        /// book/author[/bookstore/book/price>=.0001]
        /// </summary>
        [Fact]
        public static void PredicatesTest1014()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[/bookstore/book/price>=.0001]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator < - Return all authors of books with a price less than 10.0.
        /// book/author[/bookstore/book/price<10.0]
        /// </summary>
        [Fact]
        public static void PredicatesTest1015()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[/bookstore/book/price<10.0]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a numeric constant, using operator > - Return all authors of books with a price greater than 55.0.
        /// book/author[/bookstore/book/price>55.0]
        /// </summary>
        [Fact]
        public static void PredicatesTest1016()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[/bookstore/book/price>55.0]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a sting constant, using operator = - Return all book authors with a last-name equal to ""Bob"".
        /// book/author[last-name="Bob"]
        /// </summary>
        [Fact]
        public static void PredicatesTest1017()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[last-name='Bob']";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a sting constant, using operator != - Return all book authors with a last-name not equal to ""Bob"".
        /// book/author[last-name!="Bob"]
        /// </summary>
        [Fact]
        public static void PredicatesTest1018()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[last-name!='Bob']";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a sting constant, using operator >= - Return all book authors with a last-name greater than or equal to ""Robinson"".
        /// book/author[last-name>="Robinson"]
        /// </summary>
        [Fact]
        public static void PredicatesTest1019()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[last-name>='Robinson']";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a sting constant, using operator > - Return all book authors with a last-name greater than ""R"".
        /// book/author[last-name>"R"]
        /// </summary>
        [Fact]
        public static void PredicatesTest1020()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[last-name>'R']";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node value to a sting constant, using operator < - Return all book authors with a last-name less than ""Boc"".
        /// book/author[last-name<"Boc"]
        /// </summary>
        [Fact]
        public static void PredicatesTest1021()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[last-name<'Boc']";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing node to true() - Return all book authors with an award.
        /// book/author[award=true()]
        /// </summary>
        [Fact]
        public static void PredicatesTest1022()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[award=true()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing with true() - Return all book authors with an award.
        /// book/author[award=true()]
        /// </summary>
        [Fact]
        public static void PredicatesTest1023()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/author[award=true()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing with true() and false() - Return all book titles.
        /// book/title[true()!=false()]
        /// </summary>
        [Fact]
        public static void PredicatesTest1024()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/title[true()!=false()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing true() with a positive number - Return all book titles.
        /// book/title[true()=5]
        /// </summary>
        [Fact]
        public static void PredicatesTest1025()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/title[true()=5]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing false() with a string constant - Return all book titles.
        /// book/title[false()!='gramps']
        /// </summary>
        [Fact]
        public static void PredicatesTest1026()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book/title[false()!='gramps']";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing negative numbers - Return all magazine titles.
        /// magazine/title[-23.987 = -23.987]
        /// </summary>
        [Fact]
        public static void PredicatesTest1027()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"magazine/title[-23.987 = -23.987]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Road and Track"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Week"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Magazine"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing number to string constant - Return all magazine titles.
        /// magazine/title[0!="z"]
        /// </summary>
        [Fact]
        public static void PredicatesTest1028()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"magazine/title[0!=' -  100   ']";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Road and Track"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Week"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Magazine"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Comparing string constant to string constant - Return all magazine titles.
        /// magazine/title["z"="z"]
        /// </summary>
        [Fact]
        public static void PredicatesTest1029()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"magazine/title['z'='z']";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Road and Track"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Week"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Magazine"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Using a mathematical expression in the predicate - Return 3rd book's title.
        /// (book/title)[2+1]
        /// </summary>
        [Fact]
        public static void PredicatesTest1030()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"(book/title)[2+1]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Using a mathematical expression in the predicate - Return 2nd book's title.
        /// (book/title)[100-98]
        /// </summary>
        [Fact]
        public static void PredicatesTest1031()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"(book/title)[100-98]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Using a mathematical expression in the predicate - Return all book's titles.
        /// (book/title)[(8/4)=2]
        /// </summary>
        [Fact]
        public static void PredicatesTest1032()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"(book/title)[(8/4)=2]";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Using a mathematical expression in the predicate - Return 4th book's title.
        /// (book/title)[2*2]
        /// </summary>
        [Fact]
        public static void PredicatesTest1033()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"(book/title)[2*2]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Using a non-integer number as the position - Return empty node-list.
        /// (book/title)[5.01]
        /// </summary>
        [Fact]
        public static void PredicatesTest1034()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"(book/title)[5.01]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Whitespace check - (Whitespace test) Return all book's author's last-names.
        /// book / author  /      last-name
        /// </summary>
        [Fact]
        public static void PredicatesTest1035()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book / author  /      last-name";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Anderson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "er"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Invalid expression - Error Case.
        /// child::node()[Schema|]
        /// </summary>
        [Fact]
        public static void PredicatesTest1036()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"child::node()[Schema|]";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Invalid expression - Error Case.
        /// child::node()[Schema|]
        /// </summary>
        [Fact]
        public static void PredicatesTest1037()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"child::node()[Schema|]";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate containing attribute - Return all nodes with a style attribute.
        /// *[@style]
        /// </summary>
        [Fact]
        public static void PredicatesTest1042()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"*[@style]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere is Trenton?\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere in the world is Trenton?\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Predicate using and - Return all author's with a degree and an award.\
        /// bookstore//author[degree and award]
        /// </summary>
        [Fact]
        public static void PredicatesTest1043()
        {
            var xml = "books.xml";
            var testExpression = @"bookstore//author[degree and award]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate using or - Return all author's with a degree or an award and a publication.
        /// bookstore//author[(degree or award) and publication]
        /// </summary>
        [Fact]
        public static void PredicatesTest1044()
        {
            var xml = "books.xml";
            var testExpression = @"bookstore//author[(degree or award) and publication]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate using not - Return all author's with a degree and not a publication.
        /// bookstore//author[degree and not(publication)]
        /// </summary>
        [Fact]
        public static void PredicatesTest1045()
        {
            var xml = "books.xml";
            var testExpression = @"bookstore//author[degree and not(publication)]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Using invalid name character - Should return empty node list.
        /// book["!"]
        /// </summary>
        [Fact]
        public static void PredicatesTest1046()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book['!']";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// book[!]
        /// </summary>
        [Fact]
        public static void PredicatesTest1047()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"book[!]";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression,
                startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Return empty node-list.
        /// *[self::comment]
        /// </summary>
        [Fact]
        public static void PredicatesTest1048()
        {
            var xml = "books.xml";
            var testExpression = @"*[self::comment()]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Return first books price text node (12).
        /// book[position() = 1 or position() = 2]/preceding::*[1]/text()
        /// </summary>
        [Fact]
        public static void PredicatesTest1049()
        {
            var xml = "books_2.xml";
            var testExpression = @"book[position() = 1 or position() = 2]/preceding::*[1]/text()";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Should return ERROR.
        /// "/bookstore/book[ancestor(.)]
        /// </summary>
        [Fact]
        public static void PredicatesTest1050()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[ancestor(.)]";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(xml, testExpression);
        }

        /// <summary>
        /// Predicate uses ancestor axis
        /// /bookstore/*/title [ancestor::book]
        /// </summary>
        [Fact]
        public static void PredicatesTest1051()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/*/title[ancestor::book]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate uses following axis
        /// //*[following::book]
        /// </summary>
        [Fact]
        public static void PredicatesTest1052()
        {
            var xml = "books.xml";
            var testExpression = @"//*[following::book]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Joe"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "my:country",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "USA"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "12"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "JoeBob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Loser"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mike"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Hyman"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Jonathan"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Marsh"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55.95"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Road and Track"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.50"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "special_edition",
                    Name = "special_edition",
                    HasNameTable = true,
                    Value = "Yes"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Week"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "free"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "PC Magazine"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "3.95"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story1",
                    Name = "story1",
                    HasNameTable = true,
                    Value = "Create a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story2",
                    Name = "story2",
                    HasNameTable = true,
                    Value =
                        "The future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Can Netscape stay alive with Microsoft eating up its browser share?"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "stock",
                    Name = "stock",
                    HasNameTable = true,
                    Value = "MSFT 99.30"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "date",
                    Name = "date",
                    HasNameTable = true,
                    Value = "1998-06-23"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story3",
                    Name = "story3",
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value = "\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story1",
                    Name = "story1",
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first.name",
                    Name = "first.name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Frank"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Anderson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Pulizer"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Mary F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "10"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "first-name",
                    Name = "first-name",
                    HasNameTable = true,
                    Value = "Hack"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "er"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "08"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "2.50"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "0.98"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate uses following axis
        /// /bookstore/magazine[following::book]
        /// </summary>
        [Fact]
        public static void PredicatesTest1053()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/magazine[following::book]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate uses preceding axis
        /// /bookstore/magazine[preceding::book[title='How To Fix Computers']]
        /// </summary>
        [Fact]
        public static void PredicatesTest1054()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/magazine[preceding::book[title='How To Fix Computers']]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate contains preceding-sibling axis
        /// /bookstore/magazine[preceding-sibling::book]
        /// </summary>
        [Fact]
        public static void PredicatesTest1055()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/magazine[preceding-sibling::book]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate contains ancestor-or-self axis
        /// /bookstore/magazine[ancestor-or-self::magazine]
        /// </summary>
        [Fact]
        public static void PredicatesTest1056()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/magazine[ancestor-or-self::magazine]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate contains child axis
        /// /bookstore/book [child::title]
        /// </summary>
        [Fact]
        public static void PredicatesTest1057()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[child::title]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate contains descendant axis
        /// /bookstore/* [descendant::title]
        /// </summary>
        [Fact]
        public static void PredicatesTest1058()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/* [descendant::title]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate contains parent axis
        /// /bookstore/*/title [parent::book]
        /// </summary>
        [Fact]
        public static void PredicatesTest1059()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/*/title [parent::book]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate contains following-sibling axis
        /// /bookstore/* [following-sibling::book]
        /// </summary>
        [Fact]
        public static void PredicatesTest1060()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/* [following-sibling::book]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate contains attribute axis
        /// /bookstore/* [attribute::frequency]
        /// </summary>
        [Fact]
        public static void PredicatesTest1061()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/* [attribute::frequency]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate contains namespace axis
        /// //* [namespace::NSbook]
        /// </summary>
        [Fact]
        public static void PredicatesTest1062()
        {
            var xml = "name.xml";
            var testExpression = @"//* [namespace::NSbook]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "booksection",
                    Name = "booksection",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\t\n\t\t\tA Brief History Of Time\n\t\t\n\t\t\n\t\t\tThe Beautiful Universe\n\t\t\n\t\t\n\t\t\tNewton's Time Machine\n\t\t\n\t\t\n\t\t\tThe Quark And The Jaguar\n\t\t\n\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "NSbook:book",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "\n\t\t\tA Brief History Of Time\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "NSbook:title",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "A Brief History Of Time"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "NSbook:book",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "\n\t\t\tThe Beautiful Universe\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "NSbook:title",
                    NamespaceURI = "http://book.htm",
                    HasNameTable = true,
                    Prefix = "NSbook",
                    Value = "The Beautiful Universe"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\t\tNewton's Time Machine\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "Newton's Time Machine"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\t\tThe Quark And The Jaguar\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "title",
                    HasNameTable = true,
                    Value = "The Quark And The Jaguar"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate contains descendant or self
        /// /bookstore/* [descendant-or-self::*]
        /// </summary>
        [Fact]
        public static void PredicatesTest1063()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/*[descendant-or-self::*]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "my:magazine",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere is Trenton?\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere in the world is Trenton?\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// The first position of a node set is 1. Predicate uses 0. Expected empty node set.
        /// /bookstore/book[0]
        /// </summary>
        [Fact]
        public static void PredicatesTest1064()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[0]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate expression resulting in a number is converted to true, if number is equal to context position.
        /// /bookstore/book[1] [2]
        /// </summary>
        [Fact]
        public static void PredicatesTest1065()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[1] [2]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// '//' is the abbreviated representation of '/descendant-or-self::node()/'
        /// // represents /descendant-or-self::node()/
        /// </summary>
        [Fact]
        public static void PredicatesTest1066()
        {
            var xml = "books.xml";
            var testExpression = @"//self::node() = /descendant-or-self::node()/self::node()";
            var expected = true;

            Utils.XPathBooleanTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicates filter node sets on reverse axis as if the document order was bottom up.
        /// /bookstore/book/ancestor::node()[2]
        /// </summary>
        [Fact]
        public static void PredicatesTest1067()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book/ancestor::node()[2]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    HasChildren = true,
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Expected : should select all nodes that are the last child of their parent
        /// /bookstore//* [position()=count(parent::*/child::*)]
        /// </summary>
        [Fact]
        public static void PredicatesTest1068()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore//* [position()=count(parent::node()/child::*)]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "my:country",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "USA"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "12"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Marsh"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55.95"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "special_edition",
                    Name = "special_edition",
                    HasNameTable = true,
                    Value = "Yes"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "date",
                    Name = "date",
                    HasNameTable = true,
                    Value = "1998-06-23"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story3",
                    Name = "story3",
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value = "\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story1",
                    Name = "story1",
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "10"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "08"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Trenton Forever"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "excerpt",
                    Name = "excerpt",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "emph",
                    Name = "emph",
                    HasNameTable = true,
                    Value = "I"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition-list",
                    Name = "definition-list",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition",
                    Name = "definition",
                    HasNameTable = true,
                    Value = "misery"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "my:author",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Robert Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where is Trenton?"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere in the world is Trenton?\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where in the world is Trenton?"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Only bookstore should be selected, since its position in nodeset=1 and it is the first child of its parent. No other node's position in the node-set is the same as the number of children of its parent.
        /// //* [position()=count(parent::*/child::*)]
        /// </summary>
        [Fact]
        public static void PredicatesTest1069()
        {
            var xml = "books.xml";
            var startingNodePath = "/bookstore";
            var testExpression = @"//* [position()=count(parent::node()/child::*)]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "bookstore",
                    Name = "bookstore",
                    HasNameTable = true,
                    Value =
                        "\n\t\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t\n\t\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t\n\t\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t\n\t\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t\n\t\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t\n\t\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t\n\t\n\t\tPC Magazine Best Product of 1997\n\t\n\t\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t\n\t\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t\n\t\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t\n\t\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t\n\t\n\t\tTracking Trenton Stocks\n\t\t0.98\n\t\t\n\t\n\t\n\t\tTrenton Today, Trenton Tomorrow\n\t\t\n\t\t\tToni\n\t\t\tBob\n\t\t\tB.A.\n\t\t\tPh.D.\n\t\t\tPulizer\n\t\t\tStill in Trenton\n\t\t\tTrenton Forever\n\t\t\n\t\t6.50\n\t\t\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t\n\t\n\t\n\t\tWho's Who in Trenton\n\t\tRobert Bob\n\t\n\t\n\t\tWhere is Trenton?\n\t\n\t\n\t\tWhere in the world is Trenton?\n\t\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "my:country",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "USA"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "12"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "country",
                    Name = "country",
                    HasNameTable = true,
                    Value = "US"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last.name",
                    Name = "last.name",
                    HasNameTable = true,
                    Value = "Marsh"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55.95"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "special_edition",
                    Name = "special_edition",
                    HasNameTable = true,
                    Value = "Yes"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publisher",
                    Name = "publisher",
                    HasNameTable = true,
                    Value = "Ziff Davis"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "details",
                    Name = "details",
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "date",
                    Name = "date",
                    HasNameTable = true,
                    Value = "1998-06-23"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story3",
                    Name = "story3",
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "articles",
                    Name = "articles",
                    HasNameTable = true,
                    Value = "\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "story1",
                    Name = "story1",
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "55"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "last-name",
                    Name = "last-name",
                    HasNameTable = true,
                    Value = "Robinson"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "10"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "degree",
                    Name = "degree",
                    HasNameTable = true,
                    Value = "Ph.D."
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "price",
                    Name = "price",
                    HasNameTable = true,
                    Value = "08"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "subscription",
                    Name = "subscription",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "publication",
                    Name = "publication",
                    HasNameTable = true,
                    Value = "Trenton Forever"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "excerpt",
                    Name = "excerpt",
                    HasNameTable = true,
                    Value =
                        "\n\t\t\tIt was a dark and stormy night.\n\t\t\tBut then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\tI have.\n\t\t\t\n\t\t\t\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t\n\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "emph",
                    Name = "emph",
                    HasNameTable = true,
                    Value = "I"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition-list",
                    Name = "definition-list",
                    HasNameTable = true,
                    Value = "\n\t\t\t\tTrenton\n\t\t\t\tmisery\n\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "definition",
                    Name = "definition",
                    HasNameTable = true,
                    Value = "misery"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "my:author",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Robert Bob"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where is Trenton?"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere in the world is Trenton?\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "title",
                    Name = "my:title",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "Where in the world is Trenton?"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Select second last child of bookstore
        /// /bookstore/* [position()=last()-1]
        /// </summary>
        [Fact]
        public static void PredicatesTest1070()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/* [position()=last()-1]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "my:book",
                    NamespaceURI = "urn:http//www.placeholder-name-here.com/schema/",
                    HasNameTable = true,
                    Prefix = "my",
                    Value = "\n\t\tWhere is Trenton?\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Select the author node (checking position on reverse axis)
        /// /bookstore/book[1]/author/*/ancestor::* [position() = last()-2]
        /// </summary>
        [Fact]
        public static void PredicatesTest1071()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[1]/author/*/ancestor::* [position() = last()-2]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "author",
                    Name = "author",
                    HasNameTable = true,
                    Value = "\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate filters the elements in the node-set at number 8,10,12,55 (these numbers appear as text nodes in the document)
        /// //* [position() = //*]
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void PredicatesTest1072()
        {
            var xml = "books.xml";
            var testExpression = @"//* [position() = //*]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tTracking Trenton\n\t\t2.50\n\t\t\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Select the first magazine node
        /// /bookstore/book[7]/preceding::*[ self::magazine and position()=last()-count(//*[self::magazine])+21]
        /// </summary>
        [Fact]
        public static void PredicatesTest1073()
        {
            var xml = "books.xml";
            var testExpression =
                @"/bookstore/book[7]/preceding::*[ self::magazine and position()=last()-count(//*[self::magazine])+21]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Select the last magazine node
        /// /bookstore/book[7]/preceding::* [self::magazine and position()=last()-count(//*[self::book])]
        /// </summary>
        [Fact]
        public static void PredicatesTest1074()
        {
            var xml = "books.xml";
            var testExpression =
                @"/bookstore/book[7]/preceding::* [self::magazine and position()=last()-count(//*[self::book])]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Select the last magazine node
        /// /bookstore/book[7]/preceding::* [self::magazine and position()=last()-count(//book)]
        /// </summary>
        [Fact]
        public static void PredicatesTest1075()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/book[7]/preceding::* [self::magazine and position()=last()-count(//book)]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Select all nodes that are not elements
        /// //node() [boolean(self::*)=false()]
        /// </summary>
        [Fact]
        public static void PredicatesTest1076()
        {
            var xml = "books.xml";
            var testExpression = @"//node() [boolean(self::*)=false()]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Comment,
                    HasNameTable = true,
                    Value = " This file represents a fragment of a book store inventory database "
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Seven Years in Trenton"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Joe" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "USA" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "12" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "History of Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "JoeBob" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Loser" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "US" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "XQL The Golden Years"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mike" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hyman" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tXQL For Dummies\n\t\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Jonathan" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Marsh" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55.95" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Road and Track" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.50" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Yes" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Week" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "free" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "PC Magazine" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "3.95" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ziff Davis" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Create a dream PC\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Create a list of needed hardware"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "The future of the web\n\t\t\t\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Can Netscape stay alive with Microsoft eating up its browser share?"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "MSFT 99.30" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "1998-06-23" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Visual Basic 5.0 - Will it stand the test of time?\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Sport Cars - Can you really dream?\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "PC Magazine Best Product of 1997"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton 2"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "55" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "History of Trenton Vol 3"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Frank" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Anderson" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "\n\t\t\t\tSelected Short Stories of\n\t\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Mary F" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robinson" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "10" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "How To Fix Computers"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Hack" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "er" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D." },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "08" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Tracking Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "2.50" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Tracking Trenton Stocks"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "0.98" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Trenton Today, Trenton Tomorrow"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Toni" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Bob" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "B.A." },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Ph.D." },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Pulizer" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Still in Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton Forever" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "6.50" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "It was a dark and stormy night."
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value =
                        "But then all nights in Trenton seem dark and\n\t\t\tstormy to someone who has gone through what\n\t\t\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "I" },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = " have.\n\t\t\t" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Trenton" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "misery" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Who's Who in Trenton"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Robert Bob" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Text, HasNameTable = true, Value = "Where is Trenton?" },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Text,
                    HasNameTable = true,
                    Value = "Where in the world is Trenton?"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Select the 2nd and 3rd book nodes in the document
        /// /bookstore/book[1]/following::* [following-sibling::*[self::magazine] and count(following-sibling::magazine)>5]
        /// </summary>
        [Fact]
        public static void PredicatesTest1077()
        {
            var xml = "books.xml";
            var testExpression =
                @"/bookstore/book[1]/following::* [following-sibling::*[self::magazine] and count(following-sibling::magazine)>5]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Select the 1st book node
        /// /bookstore/book[1]/self::* [(following-sibling::*[self::magazine]) and count(following-sibling::magazine)>5 and boolean(preceding-sibling::*)=false()][position()=last() and position()= 1]
        /// </summary>
        [Fact]
        public static void PredicatesTest1078()
        {
            var xml = "books.xml";
            var testExpression =
                @"/bookstore/book[1]/self::* [(following-sibling::*[self::magazine]) and count(following-sibling::magazine)>5 and boolean(preceding-sibling::*)=false()][position()=last() and position()= 1]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// /bookstore/magazine [following-sibling::magazine]
        /// </summary>
        [Fact]
        public static void PredicatesTest1079()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/magazine [following-sibling::magazine]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// /bookstore/node() [following-sibling::magazine]
        /// </summary>
        [Fact]
        public static void PredicatesTest1080()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/node() [following-sibling::magazine]";
            var expected = new XPathResult(0,
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// /bookstore/magazine [following::magazine]
        /// </summary>
        [Fact]
        public static void PredicatesTest1081()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/magazine [following::magazine]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// /bookstore/node() [following::magazine]
        /// </summary>
        [Fact]
        public static void PredicatesTest1082()
        {
            var xml = "books.xml";
            var testExpression = @"/bookstore/node() [following::magazine]";
            var expected = new XPathResult(0,
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tSeven Years in Trenton\n\t\t\n\t\t\tJoe\n\t\t\tBob\n\t\t\tTrenton Literary Review Honorable Mention\n\t\t\tUSA\n\t\t\n\t\t12\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton\n\t\t\n\t\t\tMary\n\t\t\tBob\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tJoeBob\n\t\t\t\tLoser\n\t\t\t\tUS\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tXQL The Golden Years\n\t\t\n\t\t\tMike\n\t\t\tHyman\n\t\t\t\n\t\t\t\tXQL For Dummies\n\t\t\t\tJonathan\n\t\t\t\tMarsh\n\t\t\t\n\t\t\n\t\t55.95\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tRoad and Track\n\t\t3.50\n\t\t\n\t\tYes\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Week\n\t\tfree\n\t\tZiff Davis\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value =
                        "\n\t\tPC Magazine\n\t\t3.95\n\t\tZiff Davis\n\t\t\n\t\t\tCreate a dream PC\n\t\t\t\tCreate a list of needed hardware\n\t\t\t\n\t\t\tThe future of the web\n\t\t\t\tCan Netscape stay alive with Microsoft eating up its browser share?\n\t\t\t\tMSFT 99.30\n\t\t\t\t1998-06-23\n\t\t\t\n\t\t\tVisual Basic 5.0 - Will it stand the test of time?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\t\n\t\t\tSport Cars - Can you really dream?\n\t\t\t\n\t\t\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "magazine",
                    Name = "magazine",
                    HasNameTable = true,
                    Value = "\n\t\tPC Magazine Best Product of 1997\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton 2\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t55\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value =
                        "\n\t\tHistory of Trenton Vol 3\n\t\t\n\t\t\tMary F\n\t\t\tRobinson\n\t\t\tFrank\n\t\t\tAnderson\n\t\t\tPulizer\n\t\t\t\n\t\t\t\tSelected Short Stories of\n\t\t\t\tMary F\n\t\t\t\tRobinson\n\t\t\t\n\t\t\n\t\t10\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    HasNameTable = true,
                    Value = "\n\t\tHow To Fix Computers\n\t\t\n\t\t\tHack\n\t\t\ter\n\t\t\tPh.D.\n\t\t\n\t\t08\n\t"
                },
                new XPathResultToken { NodeType = XPathNodeType.Whitespace, HasNameTable = true });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// /bookstore/book[7]/preceding::*[ self::magazine and position()=last()-count(//*[self::magazine])]
        /// </summary>
        [Fact]
        public static void PredicatesTest1083()
        {
            var xml = "books.xml";
            var testExpression =
                @"/bookstore/book[7]/preceding::*[ self::magazine and position()=last()-count(//*[self::magazine])]";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// /bookstore/book[7]/preceding::*[ self::award and position()=last()-count(//*[self::magazine])+1]
        /// </summary>
        [Fact]
        public static void PredicatesTest1084()
        {
            var xml = "books.xml";
            var testExpression =
                @"/bookstore/book[7]/preceding::*[ self::award and position()=last()-count(//*[self::magazine])+1]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "award",
                    Name = "award",
                    HasNameTable = true,
                    Value = "Trenton Literary Review Honorable Mention"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate expression for namespace axis
        /// store/p1:booksection/p2:book[2]/namespace::*[.="http://book2.htm"][name()="NSbook"]
        /// </summary>
        [Fact]
        public static void PredicatesTest1085()
        {
            var xml = "name2.xml";
            var testExpression =
                @"store/p1:booksection/p2:book[2]/namespace::*[.=""http://book2.htm""][name()=""NSbook""]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("p1", "http://default.htm");
            namespaceManager.AddNamespace("p2", "http://book.htm");
            namespaceManager.AddNamespace("p3", "http://movie.htm");
            namespaceManager.AddNamespace("p4", "http://book2.htm");
            namespaceManager.AddNamespace("p5", "http://documentry.htm");
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// Predicate expression for namespace axis
        /// store/namespace::*/following::*/namespace::*/parent::*/namespace::*/ancestor::*
        /// </summary>
        [Fact]
        public static void PredicatesTest1086()
        {
            var xml = "name2.xml";
            var testExpression = @"store/namespace::*/following::*/namespace::*/parent::*/namespace::*/ancestor::*";
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Predicate expression for namespace axis
        /// descendant-or-self::*[namespace::NSmovie or (not(namespace::NSmovie|namespace::NSbook))]
        /// </summary>
        [Fact]
        public static void PredicatesTest1087()
        {
            var xml = "name2.xml";
            var startingNodePath = "/p1:store/p1:booksection";
            var testExpression =
                @"descendant-or-self::*[namespace::NSmovie or (not(namespace::NSmovie|namespace::NSbook))]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("p1", "http://default.htm");
            namespaceManager.AddNamespace("p2", "http://book.htm");
            namespaceManager.AddNamespace("p3", "http://movie.htm");
            namespaceManager.AddNamespace("p4", "http://book2.htm");
            namespaceManager.AddNamespace("p5", "http://documentry.htm");
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager,
                startingNodePath: startingNodePath);
        }
    }
}
