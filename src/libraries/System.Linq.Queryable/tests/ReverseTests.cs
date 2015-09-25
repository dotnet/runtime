// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Linq.Tests
{
    public class ReverseTests : EnumerableBasedTests
    {
        [Fact]
        public void InvalidArguments()
        {
            Assert.Throws<ArgumentNullException>("source", () => ((IQueryable<string>)null).Reverse());
        }

        [Theory]
        [InlineData(new int[] { })]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 5 })]
        [InlineData(new int[] { 1, 3, 5 })]
        [InlineData(new int[] { 2, 4, 6, 8 })]
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
            int?[] source = new int?[] { -10, 0, 5, null, 0, 9, 100, null, 9 };
            int?[] expected = new int?[] { 9, null, 100, 9, 0, null, 5, 0, -10 };
            
            Assert.Equal(expected, source.AsQueryable().Reverse());
        }

        [Fact]
        public void Reverse()
        {
            var count = (new int[] { 0, 2, 1 }).AsQueryable().Reverse().Count();
            Assert.Equal(3, count);
        }
    }
}
