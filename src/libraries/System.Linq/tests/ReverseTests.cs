// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Tests.Helpers;
using Xunit;

namespace System.Linq.Tests
{
    public class ReverseTests
    {
        [Fact]
        public void InvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => Enumerable.Reverse<string>(null));
        }

        [Theory]
        [InlineData(new int[] { })]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 1, 3, 5 })]
        [InlineData(new int[] { 2, 4, 6, 8 })]
        public void ReverseMatches(int[] input)
        {
            int[] expectedResults = new int[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                expectedResults[i] = input[input.Length - 1 - i];
            }

            Assert.NotSame(input, Enumerable.Reverse(input));

            Assert.Equal(expectedResults, input.Reverse());
            Assert.Equal(expectedResults, new TestCollection<int>(input).Reverse());
            Assert.Equal(expectedResults, new TestEnumerable<int>(input).Reverse());
            Assert.Equal(expectedResults, new TestReadOnlyCollection<int>(input).Reverse());

            Assert.Equal(expectedResults.Select(i => i * 2), input.Select(i => i * 2).Reverse());
            Assert.Equal(expectedResults.Where(i => true).Select(i => i * 2), input.Where(i => true).Select(i => i * 2).Reverse());
            Assert.Equal(expectedResults.Where(i => false).Select(i => i * 2), input.Where(i => false).Select(i => i * 2).Reverse());
        }
    }
}
