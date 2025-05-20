// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexBacktrackingTests
    {
        [Fact]
        public void PossessiveQuantifierWithLazyQuantifierShouldNotThrow()
        {
            // This specific pattern caused IndexOutOfRangeException before the fix
            var regex = new Regex(@"(?>(-*)+?-*)$");
            
            // Test with input that would previously throw
            Match match = regex.Match("test");
            
            // Verify it doesn't throw and correctly reports no match
            Assert.False(match.Success, "When backtracking stack is exhausted, match should be unsuccessful");
        }

        [Theory]
        [InlineData(@"(?>(-*)+?-*)$", "test", false)]
        [InlineData(@"(?>(-*)+?-*)$", "abcdef", false)]
        [InlineData(@"(?>(-+)+?-*)$", "test", false)]
        [InlineData(@"(?>a*)+?", "aaaaaaaa", true)]
        [InlineData(@"(?>(-{50000})+?-*)$", "test", false)]  // Large repeat count
        [InlineData(@"((?>a+)+b)+c", "aababc", true)]      // Complex nesting
        public void ComplexBacktrackingShouldNotThrow(string pattern, string input, bool expectedMatch)
        {
            // Tests various patterns that might stress the backtracking system
            var regex = new Regex(pattern);
            
            // Act - should not throw
            Match match = regex.Match(input);
            
            // Verify expected match result
            Assert.Equal(expectedMatch, match.Success);
        }
    }
}