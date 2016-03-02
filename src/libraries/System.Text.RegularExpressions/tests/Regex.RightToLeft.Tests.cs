// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexRightToLeftTests
    {
        [Fact]
        public static void RightToLeft()
        {
            Regex regex = new Regex("aaa");
            Assert.False(regex.RightToLeft);

            regex = new Regex("aaa", RegexOptions.RightToLeft);
            Assert.True(regex.RightToLeft);
        }

        [Fact]
        public static void RightToLeft_MultipleMatches()
        {
            Regex regex = new Regex(@"foo\d+", RegexOptions.RightToLeft);
            string input = "0123456789foo4567890foo1foo  0987";

            MatchCollection collection = regex.Matches(input);
            Assert.Equal(2, collection.Count);
            Assert.Equal("foo1", collection[0].Value);
            Assert.Equal("foo4567890", collection[1].Value);
        }
    }
}
