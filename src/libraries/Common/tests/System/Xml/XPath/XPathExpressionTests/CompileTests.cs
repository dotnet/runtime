// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.XPath;
using Xunit;
using XPathTests.Common;

namespace XPathTests.XPathExpressionTests
{
    public class CompileTests
    {
        private static void XPathExpressionCompileTest(string toCompile)
        {
            Assert.NotNull(XPathExpression.Compile(toCompile));
        }
        private static void NavigatorCompileTest(string toCompile)
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

            var dataNav = Utils.CreateNavigator(xml);
            Assert.NotNull(dataNav.Compile(toCompile));
        }

        private static void RunCompileTests(string toCompile)
        {
            XPathExpressionCompileTest(toCompile);
            NavigatorCompileTest(toCompile);
        }

        private static void CompileTestsErrors(string toCompile, string exceptionString)
        {
            Assert.Throws<XPathException>(() => XPathExpressionCompileTest(toCompile));
            Assert.Throws<XPathException>(() => NavigatorCompileTest(toCompile));
        }

        /// <summary>
        /// Pass in valid XPath Expression (return type = Nodeset)
        /// Priority: 0
        /// </summary>
        [Fact]
        public static void Variation_1()
        {
            RunCompileTests("child::*");
        }

        /// <summary>
        /// Pass in valid XPath Expression (return type = String)
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_2()
        {
            RunCompileTests("string(1)");
        }

        /// <summary>
        ///  Pass in valid XPath Expression (return type = Number)
        ///  Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_3()
        {
            RunCompileTests("number('1')");
        }

        /// <summary>
        /// Pass in valid XPath Expression (return type = Boolean)
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_4()
        {
            RunCompileTests("true()");
        }

        /// <summary>
        /// Pass in invalid XPath Expression
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_5()
        {
            CompileTestsErrors("invalid:::", "Xp_InvalidToken");
        }

        /// <summary>
        /// Pass in empty string
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_6()
        {
            CompileTestsErrors(string.Empty, "Xp_NodeSetExpected");
        }

        /// <summary>
        /// Pass in NULL
        /// Priority: 1
        /// </summary>
        [Fact]
        public static void Variation_7()
        {
            CompileTestsErrors(null, "Xp_ExprExpected");
        }
    }
}
