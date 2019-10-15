// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests.Expressions
{
    /// <summary>
    /// XPathExpression - SetContext Functional Tests
    /// </summary>
    public static partial class SetContextFunctionalTestsTests
    {
        /// <summary>
        /// Select node with qname
        /// //NSbook:book
        /// </summary>
        [Fact]
        public static void SetContextFunctionalTestsTest431()
        {
            var xml = "name.xml";
            var testExpression = @"//NSbook:book[1]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSbook", "http://book.htm");
            var expected = new XPathResult(0,
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// Prefix is not defined, should throw an error
        /// //NSbook:book
        /// </summary>
        [Fact]
        public static void SetContextFunctionalTestsTest432()
        {
            var xml = "name.xml";
            var testExpression = @"//NSbook:book[1]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSbook", "http://book.htm");
            var expected = new XPathResult(0,
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
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// use of multiple namespaces
        /// /doc/prefix1:elem/prefix2:elem
        /// </summary>
        [Fact]
        public static void SetContextFunctionalTestsTest433()
        {
            var xml = "name4.xml";
            var testExpression = @"/doc/prefix1:elem/prefix2:elem";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("prefix1", "http://prefix1.htm");
            namespaceManager.AddNamespace("prefix2", "http://prefix2.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    IsEmptyElement = true,
                    LocalName = "elem",
                    Name = "prefix2:elem",
                    NamespaceURI = "http://prefix2.htm",
                    HasNameTable = true,
                    Prefix = "prefix2"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// Prefix points to a namespace that is not defined in the document, should return empty nodeset.
        /// //NSbook:book
        /// </summary>
        [Fact]
        public static void SetContextFunctionalTestsTest434()
        {
            var xml = "name.xml";
            var testExpression = @"//NSbook:book[1]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("NSbook", "http://notbook.htm");
            var expected = new XPathResult(0);
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }

        /// <summary>
        /// The document's default namespace is defined with a prefix in the XmlNamespaceManager, XPath should find the nodes with the default namespace in the document.
        /// //foo:book[1]
        /// </summary>
        [Fact]
        public static void SetContextFunctionalTestsTest435()
        {
            var xml = "name2.xml";
            var testExpression = @"//foo:book[1]";
            var namespaceManager = new XmlNamespaceManager(new NameTable());

            namespaceManager.AddNamespace("foo", "http://default.htm");
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    HasAttributes = true,
                    LocalName = "book",
                    Name = "book",
                    NamespaceURI = "http://default.htm",
                    HasNameTable = true,
                    Value = "\n\t\t\tNewton's Time Machine\n\t\t"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected, namespaceManager: namespaceManager);
        }
    }
}
