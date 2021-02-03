// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ElementAtTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = Repeat(_ => from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                where x > int.MinValue
                select x, 3);
            Assert.Equal(q[0].ElementAt(3), q[0].ElementAt(3));
            Assert.Equal(q[1].ElementAt(new Index(3)), q[1].ElementAt(new Index(3)));
            Assert.Equal(q[2].ElementAt(^6), q[2].ElementAt(^6));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = Repeat(_ => from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                where !string.IsNullOrEmpty(x)
                select x,3);
            Assert.Equal(q[0].ElementAt(4), q[0].ElementAt(4));
            Assert.Equal(q[1].ElementAt(new Index(4)), q[1].ElementAt(new Index(4)));
            Assert.Equal(q[2].ElementAt(^2), q[2].ElementAt(^2));
        }

        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { NumberRangeGuaranteedNotCollectionType(9, 1), 0, 1, 9 };
            yield return new object[] { NumberRangeGuaranteedNotCollectionType(9, 10), 9, 1, 18 };
            yield return new object[] { NumberRangeGuaranteedNotCollectionType(-4, 10), 3, 7, -1 };

            yield return new object[] { new int[] { -4 }, 0, 1, -4 };
            yield return new object[] { new int[] { 9, 8, 0, -5, 10 }, 4, 1, 10 };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ElementAt(IEnumerable<int> source, int index, int indexFromEnd, int expected)
        {
            Assert.Equal(expected, source.ElementAt(index));
            Assert.Equal(expected, source.ElementAt(new Index(index)));
            Assert.Equal(expected, source.ElementAt(^indexFromEnd));
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ElementAtRunOnce(IEnumerable<int> source, int index, int indexFromEnd, int expected)
        {
            Assert.Equal(expected, source.RunOnce().ElementAt(index));
            Assert.Equal(expected, source.RunOnce().ElementAt(new Index(index)));
            Assert.Equal(expected, source.RunOnce().ElementAt(^indexFromEnd));
        }

        [Fact]
        public void InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int?[] { 9, 8 }.ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int?[] { 9, 8 }.ElementAt(^3));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int?[] { 9, 8 }.ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int?[] { 9, 8 }.ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int?[] { 9, 8 }.ElementAt(new Index(int.MaxValue)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int?[] { 9, 8 }.ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int[] { 1, 2, 3, 4 }.ElementAt(4));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int[] { 1, 2, 3, 4 }.ElementAt(new Index(4)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int[] { 1, 2, 3, 4 }.ElementAt(^0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int[] { 1, 2, 3, 4 }.ElementAt(^5));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int[0].ElementAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int[0].ElementAt(new Index(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int[0].ElementAt(^0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new int[0].ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(-4, 5).ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(-4, 5).ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(-4, 5).ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(-4, 5).ElementAt(^6));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(-4, 5).ElementAt(new Index(int.MaxValue)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(-4, 5).ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(5, 5).ElementAt(5));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(5, 5).ElementAt(new Index(5)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(5, 5).ElementAt(^0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(0, 0).ElementAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(0, 0).ElementAt(new Index(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(0, 0).ElementAt(^0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => NumberRangeGuaranteedNotCollectionType(0, 0).ElementAt(^1));
        }

        [Fact]
        public void NullableArray_ValidIndex_ReturnsCorrectObject()
        {
            int?[] source = { 9, 8, null, -5, 10 };

            Assert.Null(source.ElementAt(2));
            Assert.Equal(-5, source.ElementAt(3));

            Assert.Null(source.ElementAt(new Index(2)));
            Assert.Equal(-5, source.ElementAt(new Index(3)));

            Assert.Null(source.ElementAt(^3));
            Assert.Equal(-5, source.ElementAt(^2));
        }

        [Fact]
        public void NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).ElementAt(2));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).ElementAt(new Index(2)));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).ElementAt(^2));
        }
    }
}
