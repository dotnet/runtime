// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml.XPath;
using XPathTests.Common;
using Xunit;

namespace XPathTests.FunctionalTests.Expressions
{
    /// <summary>
    /// Expressions - Basics
    /// </summary>
    public static partial class BasicsTests
    {
        /// <summary>
        /// Expected: Selects the 1st element child of the context node.
        /// child::*[((((1 + 2) * 3) - 7) div 2)]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BasicsTest161(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"child::*[((((1 + 2) * 3) - 7) div 2)]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 2nd element child of the context node.
        /// child::*[(2 * (2 -  (3 div (1 + 2))))]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BasicsTest162(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"child::*[(2 * (2 -  (3 div (1 + 2))))]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Summary",
                    Name = "Summary",
                    HasNameTable = true,
                    Value = "This shall test XPath test"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Expected: Selects the 1st element child of the context node.
        /// child::*[(4 - 1) div  (1 + 2)]
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BasicsTest163(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"child::*[(4 - 1) div  (1 + 2)]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Title",
                    Name = "Title",
                    HasNameTable = true,
                    Value = "XPath test"
                });

            Utils.XPathNodesetTest(kind, xml, testExpression, expected, startingNodePath: startingNodePath);
        }

        /// <summary>
        /// Set and get variable. Expected: Error (variables are only supported for XSL/T)
        /// child::$$Var
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void BasicsTest164(Utils.NavigatorKind kind)
        {
            var xml = "xp001.xml";
            var startingNodePath = "/Doc";
            var testExpression = @"child::$$Var";

            Utils.XPathNodesetTestThrows<System.Xml.XPath.XPathException>(kind, xml, testExpression,
                startingNodePath: startingNodePath);
        }

        [Fact]
        public static void XPathDocumentNullStream()
        {
            Assert.Throws<ArgumentNullException>(() => new XPathDocument(default(Stream)));
        }

        [Fact]
        public static void XPathDocumentNullTextReader()
        {
            Assert.Throws<ArgumentNullException>(() => new XPathDocument(default(TextReader)));
        }
    }
}
