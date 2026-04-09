// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.XPath;
using XPathTests.Common;
using Xunit;

namespace XPathTests.XPathExpressionTests
{
    public class CompileTests
    {
        private static void XPathExpressionCompileTest(string toCompile)
        {
            Assert.NotNull(XPathExpression.Compile(toCompile));
        }
        private static void NavigatorCompileTest(Utils.NavigatorKind kind, string toCompile)
        {
            var xml = @"<DocumentElement>
    <Level1 Data='0'>
        <Name>first</Name>
        <Level2 Data='1'></Level2>
    </Level1>
    <Level1 Data='1'>
        <Name>second</Name>
        <Level2 Data='2'></Level2>
    </Level1>
    <Level1 Data='2'>
        <Name>third</Name>
        <Level2 Data='3'></Level2>
    </Level1>
    <Level1 Data='3'>
        <Name>last</Name>
        <Level2 Data='4'></Level2>
    </Level1>
</DocumentElement>";

            var dataNav = Utils.CreateNavigator(kind, xml);
            Assert.NotNull(dataNav.Compile(toCompile));
        }

        private static void RunCompileTests(Utils.NavigatorKind kind, string toCompile)
        {
            XPathExpressionCompileTest(toCompile);
            NavigatorCompileTest(kind, toCompile);
        }

        private static void CompileTestsErrors(Utils.NavigatorKind kind, string toCompile, string exceptionString)
        {
            Assert.Throws<XPathException>(() => XPathExpressionCompileTest(toCompile));
            Assert.Throws<XPathException>(() => NavigatorCompileTest(kind, toCompile));
        }

        /// <summary>
        /// Pass in valid XPath Expression (return type = Nodeset)
        /// Priority: 0
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_1(Utils.NavigatorKind kind)
        {
            RunCompileTests(kind, "child::*");
        }

        /// <summary>
        /// Pass in valid XPath Expression (return type = String)
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_2(Utils.NavigatorKind kind)
        {
            RunCompileTests(kind, "string(1)");
        }

        /// <summary>
        ///  Pass in valid XPath Expression (return type = Number)
        ///  Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_3(Utils.NavigatorKind kind)
        {
            RunCompileTests(kind, "number('1')");
        }

        /// <summary>
        /// Pass in valid XPath Expression (return type = Boolean)
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_4(Utils.NavigatorKind kind)
        {
            RunCompileTests(kind, "true()");
        }

        /// <summary>
        /// Pass in invalid XPath Expression
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_5(Utils.NavigatorKind kind)
        {
            CompileTestsErrors(kind, "invalid:::", "Xp_InvalidToken");
        }

        /// <summary>
        /// Pass in empty string
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_6(Utils.NavigatorKind kind)
        {
            CompileTestsErrors(kind, string.Empty, "Xp_NodeSetExpected");
        }

        /// <summary>
        /// Pass in NULL
        /// Priority: 1
        /// </summary>
        [Theory]
        [InlineData(Utils.NavigatorKind.XmlDocument)]
        [InlineData(Utils.NavigatorKind.XPathDocument)]
        [InlineData(Utils.NavigatorKind.XDocument)]
        public static void Variation_7(Utils.NavigatorKind kind)
        {
            CompileTestsErrors(kind, null, "Xp_ExprExpected");
        }
    }
}
