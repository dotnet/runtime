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

            if (index >= 0)
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

            if (index >= 0)
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
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).ElementAtOrDefault(2));
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).ElementAtOrDefault(new Index(2)));
            Assert.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).ElementAtOrDefault(^2));
        }

        [Fact]
        public void MutableSource()
        {
            var source = new List<int>() { 0, 1, 2, 3, 4 };
            Assert.Equal(2, source.ElementAtOrDefault(2));
            Assert.Equal(2, source.ElementAtOrDefault(new Index(2)));
            Assert.Equal(2, source.ElementAtOrDefault(^3));

            source.InsertRange(3, new[] { -1, -2 });
            source.RemoveAt(0);
            Assert.Equal(-1, source.ElementAtOrDefault(2));
            Assert.Equal(-1, source.ElementAtOrDefault(new Index(2)));
            Assert.Equal(-1, source.ElementAtOrDefault(^4));
        }

        [Fact]
        public void MutableSourceNotList()
        {
            var source = new List<int>() { 0, 1, 2, 3, 4 };
            var query1 = Repeat(_ => ForceNotCollection(source).Select(i => i), 3);
            Assert.Equal(2, query1[0].ElementAtOrDefault(2));
            Assert.Equal(2, query1[1].ElementAtOrDefault(new Index(2)));
            Assert.Equal(2, query1[2].ElementAtOrDefault(^3));

            var query2 = Repeat(_ => ForceNotCollection(source).Select(i => i), 3);
            source.InsertRange(3, new[] { -1, -2 });
            source.RemoveAt(0);
            Assert.Equal(-1, query2[0].ElementAtOrDefault(2));
            Assert.Equal(-1, query2[1].ElementAtOrDefault(new Index(2)));
            Assert.Equal(-1, query2[2].ElementAtOrDefault(^4));
        }

        [Fact]
        public void EnumerateElements()
        {
            const int ElementCount = 10;
            int state = -1;
            int moveNextCallCount = 0;
            Func<DelegateIterator<int?>> source = () =>
            {
                state = -1;
                moveNextCallCount = 0;
                return new DelegateIterator<int?>(
                    moveNext: () => { moveNextCallCount++; return ++state < ElementCount; },
                    current: () => state,
                    dispose: () => state = -1);
            };

            Assert.Equal(0, source().ElementAtOrDefault(0));
            Assert.Equal(1, moveNextCallCount);
            Assert.Equal(0, source().ElementAtOrDefault(new Index(0)));
            Assert.Equal(1, moveNextCallCount);

            Assert.Equal(5, source().ElementAtOrDefault(5));
            Assert.Equal(6, moveNextCallCount);
            Assert.Equal(5, source().ElementAtOrDefault(new Index(5)));
            Assert.Equal(6, moveNextCallCount);

            Assert.Equal(0, source().ElementAtOrDefault(^ElementCount));
            Assert.Equal(ElementCount + 1, moveNextCallCount);
            Assert.Equal(5, source().ElementAtOrDefault(^5));
            Assert.Equal(ElementCount + 1, moveNextCallCount);

            Assert.Null(source().ElementAtOrDefault(ElementCount));
            Assert.Equal(ElementCount + 1, moveNextCallCount);
            Assert.Null(source().ElementAtOrDefault(new Index(ElementCount)));
            Assert.Equal(ElementCount + 1, moveNextCallCount);
            Assert.Null(source().ElementAtOrDefault(^0));
            Assert.Equal(0, moveNextCallCount);
        }

        [Fact]
        public void NonEmptySource_Consistency()
        {
            int?[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Equal(5, source.ElementAtOrDefault(5));
            Assert.Equal(5, source.ElementAtOrDefault(new Index(5)));
            Assert.Equal(5, source.ElementAtOrDefault(^5));

            Assert.Equal(0, source.ElementAtOrDefault(0));
            Assert.Equal(0, source.ElementAtOrDefault(new Index(0)));
            Assert.Equal(0, source.ElementAtOrDefault(^10));

            Assert.Equal(9, source.ElementAtOrDefault(9));
            Assert.Equal(9, source.ElementAtOrDefault(new Index(9)));
            Assert.Equal(9, source.ElementAtOrDefault(^1));

            Assert.Null(source.ElementAtOrDefault(-1));
            Assert.Null(source.ElementAtOrDefault(^11));

            Assert.Null(source.ElementAtOrDefault(10));
            Assert.Null(source.ElementAtOrDefault(new Index(10)));
            Assert.Null(source.ElementAtOrDefault(^0));

            Assert.Null(source.ElementAtOrDefault(int.MinValue));
            Assert.Null(source.ElementAtOrDefault(^int.MaxValue));

            Assert.Null(source.ElementAtOrDefault(int.MaxValue));
            Assert.Null(source.ElementAtOrDefault(new Index(int.MaxValue)));
        }

        [Fact]
        public void NonEmptySource_Consistency_NotList()
        {
            int?[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Equal(5, ForceNotCollection(source).ElementAtOrDefault(5));
            Assert.Equal(5, ForceNotCollection(source).ElementAtOrDefault(new Index(5)));
            Assert.Equal(5, ForceNotCollection(source).ElementAtOrDefault(^5));

            Assert.Equal(0, ForceNotCollection(source).ElementAtOrDefault(0));
            Assert.Equal(0, ForceNotCollection(source).ElementAtOrDefault(new Index(0)));
            Assert.Equal(0, ForceNotCollection(source).ElementAtOrDefault(^10));

            Assert.Equal(9, ForceNotCollection(source).ElementAtOrDefault(9));
            Assert.Equal(9, ForceNotCollection(source).ElementAtOrDefault(new Index(9)));
            Assert.Equal(9, ForceNotCollection(source).ElementAtOrDefault(^1));

            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(-1));
            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(^11));

            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(10));
            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(new Index(10)));
            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(^0));

            const int ElementCount = 10;
            int state = -1;
            int moveNextCallCount = 0;
            Func<DelegateIterator<int?>> getSource = () =>
            {
                state = -1;
                moveNextCallCount = 0;
                return new DelegateIterator<int?>(
                    moveNext: () => { moveNextCallCount++; return ++state < ElementCount; },
                    current: () => state,
                    dispose: () => state = -1);
            };

            Assert.Null(getSource().ElementAtOrDefault(10));
            Assert.Equal(ElementCount + 1, moveNextCallCount);
            Assert.Null(getSource().ElementAtOrDefault(new Index(10)));
            Assert.Equal(ElementCount + 1, moveNextCallCount);
            Assert.Null(getSource().ElementAtOrDefault(^0));
            Assert.Equal(0, moveNextCallCount);

            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(int.MinValue));
            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(^int.MaxValue));

            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(int.MaxValue));
            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(new Index(int.MaxValue)));
        }

        [Fact]
        public void NonEmptySource_Consistency_ListPartition()
        {
            int?[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Equal(5, ListPartitionOrEmpty(source).ElementAtOrDefault(5));
            Assert.Equal(5, ListPartitionOrEmpty(source).ElementAtOrDefault(new Index(5)));
            Assert.Equal(5, ListPartitionOrEmpty(source).ElementAtOrDefault(^5));

            Assert.Equal(0, ListPartitionOrEmpty(source).ElementAtOrDefault(0));
            Assert.Equal(0, ListPartitionOrEmpty(source).ElementAtOrDefault(new Index(0)));
            Assert.Equal(0, ListPartitionOrEmpty(source).ElementAtOrDefault(^10));

            Assert.Equal(9, ListPartitionOrEmpty(source).ElementAtOrDefault(9));
            Assert.Equal(9, ListPartitionOrEmpty(source).ElementAtOrDefault(new Index(9)));
            Assert.Equal(9, ListPartitionOrEmpty(source).ElementAtOrDefault(^1));

            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(-1));
            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(^11));

            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(10));
            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(new Index(10)));
            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(^0));

            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(int.MinValue));
            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(^int.MaxValue));

            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(int.MaxValue));
            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(new Index(int.MaxValue)));
        }

        [Fact]
        public void NonEmptySource_Consistency_EnumerablePartition()
        {
            int?[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Equal(5, EnumerablePartitionOrEmpty(source).ElementAtOrDefault(5));
            Assert.Equal(5, EnumerablePartitionOrEmpty(source).ElementAtOrDefault(new Index(5)));
            Assert.Equal(5, EnumerablePartitionOrEmpty(source).ElementAtOrDefault(^5));

            Assert.Equal(0, EnumerablePartitionOrEmpty(source).ElementAtOrDefault(0));
            Assert.Equal(0, EnumerablePartitionOrEmpty(source).ElementAtOrDefault(new Index(0)));
            Assert.Equal(0, EnumerablePartitionOrEmpty(source).ElementAtOrDefault(^10));

            Assert.Equal(9, EnumerablePartitionOrEmpty(source).ElementAtOrDefault(9));
            Assert.Equal(9, EnumerablePartitionOrEmpty(source).ElementAtOrDefault(new Index(9)));
            Assert.Equal(9, EnumerablePartitionOrEmpty(source).ElementAtOrDefault(^1));

            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(-1));
            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(^11));

            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(10));
            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(new Index(10)));
            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(^0));

            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(int.MinValue));
            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(^int.MaxValue));

            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(int.MaxValue));
            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(new Index(int.MaxValue)));
        }

        [Fact]
        public void EmptySource_Consistency()
        {
            int?[] source = { };

            Assert.Null(source.ElementAtOrDefault(1));
            Assert.Null(source.ElementAtOrDefault(-1));
            Assert.Null(source.ElementAtOrDefault(new Index(1)));
            Assert.Null(source.ElementAtOrDefault(^1));

            Assert.Null(source.ElementAtOrDefault(0));
            Assert.Null(source.ElementAtOrDefault(new Index(0)));
            Assert.Null(source.ElementAtOrDefault(^0));

            Assert.Null(source.ElementAtOrDefault(int.MinValue));
            Assert.Null(source.ElementAtOrDefault(^int.MaxValue));

            Assert.Null(source.ElementAtOrDefault(int.MaxValue));
            Assert.Null(source.ElementAtOrDefault(new Index(int.MaxValue)));
        }

        [Fact]
        public void EmptySource_Consistency_NotList()
        {
            int?[] source = { };

            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(1));
            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(-1));
            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(new Index(1)));
            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(^1));

            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(0));
            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(new Index(0)));
            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(^0));

            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(int.MinValue));
            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(^int.MaxValue));

            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(int.MaxValue));
            Assert.Null(ForceNotCollection(source).ElementAtOrDefault(new Index(int.MaxValue)));
        }

        [Fact]
        public void EmptySource_Consistency_ListPartition()
        {
            int?[] source = { };

            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(1));
            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(-1));
            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(new Index(1)));
            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(^1));

            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(0));
            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(new Index(0)));
            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(^0));

            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(int.MinValue));
            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(^int.MaxValue));

            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(int.MaxValue));
            Assert.Null(ListPartitionOrEmpty(source).ElementAtOrDefault(new Index(int.MaxValue)));
        }

        [Fact]
        public void EmptySource_Consistency_EnumerablePartition()
        {
            int?[] source = { };

            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(1));
            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(-1));
            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(new Index(1)));
            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(^1));

            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(0));
            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(new Index(0)));
            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(^0));

            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(int.MinValue));
            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(^int.MaxValue));

            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(int.MaxValue));
            Assert.Null(EnumerablePartitionOrEmpty(source).ElementAtOrDefault(new Index(int.MaxValue)));
        }
    }
}
