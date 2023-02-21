// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Linq.Tests
{
    public class ReverseTests : EnumerableBasedTests
    {
        [Fact]
        public void InvalidArguments()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IQueryable<string>)null).Reverse());
        }

        [Theory]
        [InlineData(new int[] { })]
        [InlineData(new[] { 1 })]
        [InlineData(new[] { 5 })]
        [InlineData(new[] { 1, 3, 5 })]
        [InlineData(new[] { 2, 4, 6, 8 })]
        public void ReverseMatches(int[] input)
        {
            int[] expectedResults = new int[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                expectedResults[i] = input[input.Length - 1 - i];
            }

            Assert.Equal(expectedResults, input.AsQueryable().Reverse());
        }

        [Fact]
        public void SomeRepeatedElements()
        {
            int?[] source = { -10, 0, 5, null, 0, 9, 100, null, 9 };
            int?[] expected = { 9, null, 100, 9, 0, null, 5, 0, -10 };

            Assert.Equal(expected, source.AsQueryable().Reverse());
        }

        [Fact]
        public void Reverse()
        {
            var count = new[] { 0, 2, 1 }.AsQueryable().Reverse().Count();
            Assert.Equal(3, count);
        }
    }
}
