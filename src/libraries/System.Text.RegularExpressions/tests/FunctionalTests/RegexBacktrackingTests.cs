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
            
            // No assertion on match result, just verifying it doesn't throw
            Assert.True(true, "No exception was thrown");
        }

        [Theory]
        [InlineData(@"(?>(-*)+?-*)$", "test")]
        [InlineData(@"(?>(-*)+?-*)$", "abcdef")]
        [InlineData(@"(?>(-+)+?-*)$", "test")]
        [InlineData(@"(?>a*)+?", "aaaaaaaa")]
        [InlineData(@"(?>(-{50000})+?-*)$", "test")]  // Large repeat count
        [InlineData(@"((?>a+)+b)+c", "aababc")]      // Complex nesting
        public void ComplexBacktrackingShouldNotThrow(string pattern, string input)
        {
            // Tests various patterns that might stress the backtracking system
            var regex = new Regex(pattern);
            
            // Act - should not throw
            Match match = regex.Match(input);
        }
    }
}