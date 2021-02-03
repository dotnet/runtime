// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ElementAtOrDefaultTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = Repeat(_ => from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                where x > int.MinValue
                select x, 3);
            Assert.Equal(q[0].ElementAtOrDefault(3), q[0].ElementAtOrDefault(3));
            Assert.Equal(q[1].ElementAtOrDefault(new Index(3)), q[1].ElementAtOrDefault(new Index(3)));
            Assert.Equal(q[2].ElementAtOrDefault(^6), q[2].ElementAtOrDefault(^6));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = Repeat(_ => from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                where !string.IsNullOrEmpty(x)
                select x, 3);
            Assert.Equal(q[0].ElementAtOrDefault(4), q[0].ElementAtOrDefault(4));
            Assert.Equal(q[1].ElementAtOrDefault(new Index(4)), q[1].ElementAtOrDefault(new Index(4)));
            Assert.Equal(q[2].ElementAtOrDefault(^2), q[2].ElementAtOrDefault(^2));
        }

        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { NumberRangeGuaranteedNotCollectionType(9, 1), 0, 1, 9 };
            yield return new object[] { NumberRangeGuaranteedNotCollectionType(9, 10), 9, 1, 18 };
            yield return new object[] { NumberRangeGuaranteedNotCollectionType(-4, 10), 3, 7, -1 };

            yield return new object[] { new int[] { 1, 2, 3, 4 }, 4, 0, 0 };
            yield return new object[] { new int[0], 0, 0, 0 };
            yield return new object[] { new int[] { -4 }, 0, 1, -4 };
            yield return new object[] { new int[] { 9, 8, 0, -5, 10 }, 4, 1, 10 };

            yield return new object[] { NumberRangeGuaranteedNotCollectionType(-4, 5), -1, 6, 0 };
            yield return new object[] { NumberRangeGuaranteedNotCollectionType(5, 5), 5, 0, 0 };
            yield return new object[] { NumberRangeGuaranteedNotCollectionType(0, 0), 0, 0, 0 };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ElementAtOrDefault(IEnumerable<int> source, int index, int indexFromEnd, int expected)
        {
            Assert.Equal(expected, source.ElementAtOrDefault(index));

            if (index > 0)
            {
                Assert.Equal(expected, source.ElementAtOrDefault(new Index(index)));
            }

            Assert.Equal(expected, source.ElementAtOrDefault(^indexFromEnd));
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ElementAtOrDefaultRunOnce(IEnumerable<int> source, int index, int indexFromEnd, int expected)
        {
            Assert.Equal(expected, source.RunOnce().ElementAtOrDefault(index));

            if (index > 0)
            {
                Assert.Equal(expected, source.RunOnce().ElementAtOrDefault(new Index(index)));
            }

            Assert.Equal(expected, source.RunOnce().ElementAtOrDefault(^indexFromEnd));
        }

        [Fact]
        public void NullableArray_InvalidIndex_ReturnsNull()
        {
            int?[] source = { 9, 8 };
            Assert.Null(source.ElementAtOrDefault(-1));
            Assert.Null(source.ElementAtOrDefault(3));
            Assert.Null(source.ElementAtOrDefault(int.MaxValue));
            Assert.Null(source.ElementAtOrDefault(int.MinValue));

            Assert.Null(source.ElementAtOrDefault(^3));
            Assert.Null(source.ElementAtOrDefault(new Index(3)));
            Assert.Null(source.ElementAtOrDefault(new Index(int.MaxValue)));
            Assert.Null(source.ElementAtOrDefault(^int.MaxValue));
        }

        [Fact]
        public void NullableArray_ValidIndex_ReturnsCorrectObject()
        {
            int?[] source = { 9, 8, null, -5, 10 };

            Assert.Null(source.ElementAtOrDefault(2));
            Assert.Equal(-5, source.ElementAtOrDefault(3));

            Assert.Null(source.ElementAtOrDefault(new Index(2)));
            Assert.Equal(-5, source.ElementAtOrDefault(new Index(3)));

            Assert.Null(source.ElementAtOrDefault(^3));
            Assert.Equal(-5, source.ElementAtOrDefault(^2));
        }

        [Fact]
        public void NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).ElementAtOrDefault(2));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).ElementAtOrDefault(new Index(2)));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).ElementAtOrDefault(^2));
        }
    }
}
