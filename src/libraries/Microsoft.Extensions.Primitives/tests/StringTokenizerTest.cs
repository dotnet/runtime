﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace Microsoft.Extensions.Primitives
{
    public class StringTokenizerTest
    {
        [Fact]
        public void TokenizerReturnsEmptySequenceForNullValues()
        {
            // Arrange
            var stringTokenizer = new StringTokenizer();
            var enumerator = stringTokenizer.GetEnumerator();

            // Act
            var next = enumerator.MoveNext();

            // Assert
            Assert.False(next);
        }

        [Theory]
        [InlineData("", new[] { "" })]
        [InlineData("a", new[] { "a" })]
        [InlineData("abc", new[] { "abc" })]
        [InlineData("a,b", new[] { "a", "b" })]
        [InlineData("a,,b", new[] { "a", "", "b" })]
        [InlineData(",a,b", new[] { "", "a", "b" })]
        [InlineData(",,a,b", new[] { "", "", "a", "b" })]
        [InlineData("a,b,", new[] { "a", "b", "" })]
        [InlineData("a,b,,", new[] { "a", "b", "", "" })]
        [InlineData("ab,cde,efgh", new[] { "ab", "cde", "efgh" })]
        public void Tokenizer_ReturnsSequenceOfValues(string value, string[] expected)
        {
            // Arrange
            var tokenizer = new StringTokenizer(value, new [] { ',' });

            // Act
            var result = tokenizer.Select(t => t.Value).ToArray();

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("", new[] { "" })]
        [InlineData("a", new[] { "a" })]
        [InlineData("abc", new[] { "abc" })]
        [InlineData("a.b", new[] { "a", "b" })]
        [InlineData("a,b", new[] { "a", "b" })]
        [InlineData("a.b,c", new[] { "a", "b", "c" })]
        [InlineData("a,b.c", new[] { "a", "b", "c" })]
        [InlineData("ab.cd,ef", new[] { "ab", "cd", "ef" })]
        [InlineData("ab,cd.ef", new[] { "ab", "cd", "ef" })]
        [InlineData(",a.b", new[] { "", "a", "b" })]
        [InlineData(".a,b", new[] { "", "a", "b" })]
        [InlineData(".,a.b", new[] { "", "", "a", "b" })]
        [InlineData(",.a,b", new[] { "", "", "a", "b" })]
        [InlineData("a.b,", new[] { "a", "b", "" })]
        [InlineData("a,b.", new[] { "a", "b", "" })]
        [InlineData("a.b,.", new[] { "a", "b", "", "" })]
        [InlineData("a,b.,", new[] { "a", "b", "", "" })]
        public void Tokenizer_SupportsMultipleSeparators(string value, string[] expected)
        {
            // Arrange
            var tokenizer = new StringTokenizer(value, new[] { '.', ',' });

            // Act
            var result = tokenizer.Select(t => t.Value).ToArray();

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
