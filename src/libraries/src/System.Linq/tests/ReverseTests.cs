// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ReverseTests : EnumerableTests
    {
        [Fact]
        public void InvalidArguments()
        {
            Assert.Throws<ArgumentNullException>("source", () => Enumerable.Reverse<string>(null));
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

            Assert.NotSame(input, Enumerable.Reverse(input));

            Assert.Equal(expectedResults, input.Reverse());
            Assert.Equal(expectedResults, new TestCollection<int>(input).Reverse());
            Assert.Equal(expectedResults, new TestEnumerable<int>(input).Reverse());
            Assert.Equal(expectedResults, new TestReadOnlyCollection<int>(input).Reverse());

            Assert.Equal(expectedResults.Select(i => i * 2), input.Select(i => i * 2).Reverse());
            Assert.Equal(expectedResults.Where(i => true).Select(i => i * 2), input.Where(i => true).Select(i => i * 2).Reverse());
            Assert.Equal(expectedResults.Where(i => false).Select(i => i * 2), input.Where(i => false).Select(i => i * 2).Reverse());
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > Int32.MinValue
                    select x;

            Assert.Equal(q.Reverse(), q.Reverse());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Reverse(), q.Reverse());
        }
        
        [Fact]
        public void SomeRepeatedElements()
        {
            int?[] source = new int?[] { -10, 0, 5, null, 0, 9, 100, null, 9 };
            int?[] expected = new int?[] { 9, null, 100, 9, 0, null, 5, 0, -10 };
            
            Assert.Equal(expected, source.Reverse());
        }

        [Fact]
        public void ForcedToEnumeratorDoesntEnumerate()
        {
            var iterator = NumberRangeGuaranteedNotCollectionType(0, 3).Reverse();
            // Don't insist on this behaviour, but check its correct if it happens
            var en = iterator as IEnumerator<int>;
            Assert.False(en != null && en.MoveNext());
        }
    }
}
