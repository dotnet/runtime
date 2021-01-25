// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public class SimpleRegexTests
    {
        [Theory]
        [InlineData("", "", true)]
        [InlineData("", "*", true)]
        [InlineData("a", "", false)]
        [InlineData("a", "*", true)]
        [InlineData("a", "a", true)]
        [InlineData("A", "a", true)]
        [InlineData("a", "A", true)]
        [InlineData("a", "b", false)]
        [InlineData(" a", "a", false)]
        [InlineData("a ", "a", false)]
        [InlineData("aaa", "*", true)]
        [InlineData("aaa", "*****", true)]
        [InlineData("example.com", "*.com", true)]
        [InlineData("example.com", "*.net", false)]
        [InlineData("sub.example.com", "*.com", true)]
        [InlineData("sub.example.com", "*.example.com", true)]
        [InlineData("SuB.eXaMpLe.COm", "*.example.com", true)]
        [InlineData("sub2.sub1.example.com", "*.example.com", true)]
        [InlineData("sub2.sub1.example.com", "*.*.example.com", true)]
        [InlineData("sub.example.com", "*.*.example.com", false)]
        [InlineData("sub.example.com", "*.*.*", true)]
        [InlineData("sub.example.com", "*", true)]
        [InlineData("abcdefg", "*a*b*c*d**e***f****g*****", true)]
        [InlineData("abcdefg", "*a*b*c*de**e***f****g*****", false)]
        [InlineData(".", "*.*", true)]
        [InlineData("ab.cde", "*.*", true)]
        [InlineData(".cde", "*.*", true)]
        [InlineData("cde", "*.*", false)]
        [InlineData("cde", "cd*", true)]
        [InlineData("192.168.1.123", "192.168.1.*", true)]
        [InlineData("192.168.2.123", "192.168.1.*", false)]
        public void InputMatchesStarWildcardPattern(string input, string pattern, bool expected)
        {
            Assert.Equal(expected, SimpleRegex.IsMatchWithStarWildcard(input, pattern));
        }
    }
}
