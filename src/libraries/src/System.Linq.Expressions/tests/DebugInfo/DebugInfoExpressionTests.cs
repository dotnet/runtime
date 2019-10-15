// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class DebugInfoExpressionTests
    {
        [Theory]
        [InlineData(1, 1, 1, 1, false)]
        [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, false)]
        [InlineData(1, 1, int.MaxValue, int.MaxValue, false)]
        [InlineData(5, 10, 15, 20, false)]
        [InlineData(5, 25, 15, 20, false)]
        [InlineData(0xfeefee, 0, 0xfeefee, 0, true)]
        public static void DebugInfo(int startLine, int startColumn, int endLine, int endColumn, bool isClear)
        {
            SymbolDocumentInfo document = Expression.SymbolDocument("AFile");
            DebugInfoExpression ex = Expression.DebugInfo(document, startLine, startColumn, endLine, endColumn);
            VerifyDebugInfoExpression(ex, document, startLine, startColumn, endLine, endColumn, isClear);
        }

        [Fact]
        public static void DebugInfo_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("document", () => Expression.DebugInfo(null, 1, 1, 1, 1));

            SymbolDocumentInfo document = Expression.SymbolDocument("AFile");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startLine", () => Expression.DebugInfo(document, 0, 1, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startColumn", () => Expression.DebugInfo(document, 1, 0, 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("endLine", () => Expression.DebugInfo(document, 1, 1, 0, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("endColumn", () => Expression.DebugInfo(document, 1, 1, 1, 0));

            AssertExtensions.Throws<ArgumentException>(null, () => Expression.DebugInfo(document, 10, 1, 1, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.DebugInfo(document, 1, 10, 1, 1));
        }

        [Fact]
        public static void ClearDebugInfo()
        {
            SymbolDocumentInfo document = Expression.SymbolDocument("AFile");
            DebugInfoExpression ex = Expression.ClearDebugInfo(document);
            VerifyDebugInfoExpression(ex, document, 0xfeefee, 0, 0xfeefee, 0, true);
        }

        [Fact]
        public static void ToStringTest()
        {
            DebugInfoExpression e = Expression.DebugInfo(Expression.SymbolDocument("foo.cs"), 12, 23, 34, 45);
            Assert.Equal("<DebugInfo(foo.cs: 12, 23, 34, 45)>", e.ToString());
        }

        private static void VerifyDebugInfoExpression(DebugInfoExpression ex, SymbolDocumentInfo document, int startLine, int startColumn, int endLine, int endColumn, bool isClear)
        {
            Assert.Same(document, ex.Document);
            Assert.Equal(startLine, ex.StartLine);
            Assert.Equal(startColumn, ex.StartColumn);
            Assert.Equal(endLine, ex.EndLine);
            Assert.Equal(endColumn, ex.EndColumn);

            Assert.Equal(ExpressionType.DebugInfo, ex.NodeType);
            Assert.Equal(typeof(void), ex.Type);
            Assert.Equal(isClear, ex.IsClear);
        }
    }
}
