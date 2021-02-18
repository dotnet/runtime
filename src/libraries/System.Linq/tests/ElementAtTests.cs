// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
                                select x, 3);
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

        [Fact]
        public void MutableSource()
        {
            var source = new List<int>() { 0, 1, 2, 3, 4 };
            Assert.Equal(2, source.ElementAt(2));
            Assert.Equal(2, source.ElementAt(new Index(2)));
            Assert.Equal(2, source.ElementAt(^3));

            source.InsertRange(3, new[] { -1, -2 });
            source.RemoveAt(0);
            Assert.Equal(-1, source.ElementAt(2));
            Assert.Equal(-1, source.ElementAt(new Index(2)));
            Assert.Equal(-1, source.ElementAt(^4));
        }

        [Fact]
        public void MutableSourceNotList()
        {
            var source = new List<int>() { 0, 1, 2, 3, 4 };
            var query1 = Repeat(_ => ForceNotCollection(source).Select(i => i), 3);
            Assert.Equal(2, query1[0].ElementAt(2));
            Assert.Equal(2, query1[1].ElementAt(new Index(2)));
            Assert.Equal(2, query1[2].ElementAt(^3));

            var query2 = Repeat(_ => ForceNotCollection(source).Select(i => i), 3);
            source.InsertRange(3, new[] { -1, -2 });
            source.RemoveAt(0);
            Assert.Equal(-1, query2[0].ElementAt(2));
            Assert.Equal(-1, query2[1].ElementAt(new Index(2)));
            Assert.Equal(-1, query2[2].ElementAt(^4));
        }

        private class ArrayWrapper<T> : IList<T>
        {
            private readonly T[] _list;

            internal ArrayWrapper(T[] list) => _list = list;

            public int Count => _list.Length;

            public T this[int index]
            {
                get => _list[index];
                set => _list[index] = value;
            }

            public IEnumerator<T> GetEnumerator() => ((IList<T>)_list).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

            public bool Contains(T item) => _list.Contains(item);

            public void Add(T item) => throw new NotImplementedException();

            public void Clear() => throw new NotImplementedException();

            public bool Remove(T item) => throw new NotImplementedException();

            public bool IsReadOnly => throw new NotImplementedException();

            public int IndexOf(T item) => throw new NotImplementedException();

            public void Insert(int index, T item) => throw new NotImplementedException();

            public void RemoveAt(int index) => throw new NotImplementedException();
        }

        [Fact]
        public void NonEmptySource_Consistency()
        {
            int[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Equal(5, source.ElementAt(5));
            Assert.Equal(5, source.ElementAt(new Index(5)));
            Assert.Equal(5, source.ElementAt(^5));

            Assert.Equal(0, source.ElementAt(0));
            Assert.Equal(0, source.ElementAt(new Index(0)));
            Assert.Equal(0, source.ElementAt(^10));

            Assert.Equal(9, source.ElementAt(9));
            Assert.Equal(9, source.ElementAt(new Index(9)));
            Assert.Equal(9, source.ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(^11));
            AssertExtensions.Throws<IndexOutOfRangeException>(() => new ArrayWrapper<int>(source).ElementAt(-1));
            AssertExtensions.Throws<IndexOutOfRangeException>(() => new ArrayWrapper<int>(source).ElementAt(^11));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(10));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(new Index(10)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(^0));
            AssertExtensions.Throws<IndexOutOfRangeException>(() => new ArrayWrapper<int>(source).ElementAt(10));
            AssertExtensions.Throws<IndexOutOfRangeException>(() => new ArrayWrapper<int>(source).ElementAt(new Index(10)));
            AssertExtensions.Throws<IndexOutOfRangeException>(() => new ArrayWrapper<int>(source).ElementAt(^0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(new Index(int.MaxValue)));
        }

        [Fact]
        public void NonEmptySource_Consistency_NotList()
        {
            int[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Equal(5, ForceNotCollection(source).ElementAt(5));
            Assert.Equal(5, ForceNotCollection(source).ElementAt(new Index(5)));
            Assert.Equal(5, ForceNotCollection(source).ElementAt(^5));

            Assert.Equal(0, ForceNotCollection(source).ElementAt(0));
            Assert.Equal(0, ForceNotCollection(source).ElementAt(new Index(0)));
            Assert.Equal(0, ForceNotCollection(source).ElementAt(^10));

            Assert.Equal(9, ForceNotCollection(source).ElementAt(9));
            Assert.Equal(9, ForceNotCollection(source).ElementAt(new Index(9)));
            Assert.Equal(9, ForceNotCollection(source).ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(^11));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(10));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(new Index(10)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(^0));

            const int ElementCount = 10;
            int state = -1;
            int moveNextCallCount = 0;
            Func<DelegateIterator<int>> getSource = () =>
            {
                state = -1;
                moveNextCallCount = 0;
                return new DelegateIterator<int>(
                    moveNext: () => { moveNextCallCount++; return ++state < ElementCount; },
                    current: () => state,
                    dispose: () => state = -1);
            };

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => getSource().ElementAt(10));
            Assert.Equal(ElementCount + 1, moveNextCallCount);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => getSource().ElementAt(new Index(10)));
            Assert.Equal(ElementCount + 1, moveNextCallCount);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => getSource().ElementAt(^0));
            Assert.Equal(0, moveNextCallCount);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(new Index(int.MaxValue)));
        }

        [Fact]
        public void NonEmptySource_Consistency_ListPartition()
        {
            int[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Equal(5, ListPartitionOrEmpty(source).ElementAt(5));
            Assert.Equal(5, ListPartitionOrEmpty(source).ElementAt(new Index(5)));
            Assert.Equal(5, ListPartitionOrEmpty(source).ElementAt(^5));

            Assert.Equal(0, ListPartitionOrEmpty(source).ElementAt(0));
            Assert.Equal(0, ListPartitionOrEmpty(source).ElementAt(new Index(0)));
            Assert.Equal(0, ListPartitionOrEmpty(source).ElementAt(^10));

            Assert.Equal(9, ListPartitionOrEmpty(source).ElementAt(9));
            Assert.Equal(9, ListPartitionOrEmpty(source).ElementAt(new Index(9)));
            Assert.Equal(9, ListPartitionOrEmpty(source).ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(^11));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(10));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(new Index(10)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(^0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(new Index(int.MaxValue)));
        }

        [Fact]
        public void NonEmptySource_Consistency_EnumerablePartition()
        {
            int[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Equal(5, EnumerablePartitionOrEmpty(source).ElementAt(5));
            Assert.Equal(5, EnumerablePartitionOrEmpty(source).ElementAt(new Index(5)));
            Assert.Equal(5, EnumerablePartitionOrEmpty(source).ElementAt(^5));

            Assert.Equal(0, EnumerablePartitionOrEmpty(source).ElementAt(0));
            Assert.Equal(0, EnumerablePartitionOrEmpty(source).ElementAt(new Index(0)));
            Assert.Equal(0, EnumerablePartitionOrEmpty(source).ElementAt(^10));

            Assert.Equal(9, EnumerablePartitionOrEmpty(source).ElementAt(9));
            Assert.Equal(9, EnumerablePartitionOrEmpty(source).ElementAt(new Index(9)));
            Assert.Equal(9, EnumerablePartitionOrEmpty(source).ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(^11));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(10));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(new Index(10)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(^0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(new Index(int.MaxValue)));
        }

        [Fact]
        public void NonEmptySource_Consistency_Collection()
        {
            int[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Equal(5, new TestCollection<int>(source).ElementAt(5));
            Assert.Equal(5, new TestCollection<int>(source).ElementAt(new Index(5)));
            Assert.Equal(5, new TestCollection<int>(source).ElementAt(^5));

            Assert.Equal(0, new TestCollection<int>(source).ElementAt(0));
            Assert.Equal(0, new TestCollection<int>(source).ElementAt(new Index(0)));
            Assert.Equal(0, new TestCollection<int>(source).ElementAt(^10));

            Assert.Equal(9, new TestCollection<int>(source).ElementAt(9));
            Assert.Equal(9, new TestCollection<int>(source).ElementAt(new Index(9)));
            Assert.Equal(9, new TestCollection<int>(source).ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(^11));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(10));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(new Index(10)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(^0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(new Index(int.MaxValue)));
        }

        [Fact]
        public void NonEmptySource_Consistency_NonGenericCollection()
        {
            int[] source = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Assert.Equal(5, new TestNonGenericCollection<int>(source.ToArray()).ElementAt(5));
            Assert.Equal(5, new TestNonGenericCollection<int>(source.ToArray()).ElementAt(new Index(5)));
            Assert.Equal(5, new TestNonGenericCollection<int>(source.ToArray()).ElementAt(^5));

            Assert.Equal(0, new TestNonGenericCollection<int>(source.ToArray()).ElementAt(0));
            Assert.Equal(0, new TestNonGenericCollection<int>(source.ToArray()).ElementAt(new Index(0)));
            Assert.Equal(0, new TestNonGenericCollection<int>(source.ToArray()).ElementAt(^10));

            Assert.Equal(9, new TestNonGenericCollection<int>(source.ToArray()).ElementAt(9));
            Assert.Equal(9, new TestNonGenericCollection<int>(source.ToArray()).ElementAt(new Index(9)));
            Assert.Equal(9, new TestNonGenericCollection<int>(source.ToArray()).ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(^11));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(10));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(new Index(10)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(^0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(new Index(int.MaxValue)));
        }

        [Fact]
        public void EmptySource_Consistency()
        {
            int[] source = { };

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(new Index(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(new Index(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(^0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => source.ElementAt(new Index(int.MaxValue)));
        }

        [Fact]
        public void EmptySource_Consistency_NotList()
        {
            int[] source = { };

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(new Index(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(new Index(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(^0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ForceNotCollection(source).ElementAt(new Index(int.MaxValue)));
        }

        [Fact]
        public void EmptySource_Consistency_ListPartition()
        {
            int[] source = { };

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(new Index(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(new Index(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(^0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ListPartitionOrEmpty(source).ElementAt(new Index(int.MaxValue)));
        }

        [Fact]
        public void EmptySource_Consistency_EnumerablePartition()
        {
            int[] source = { };

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(new Index(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(new Index(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(^0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => EnumerablePartitionOrEmpty(source).ElementAt(new Index(int.MaxValue)));
        }

        [Fact]
        public void EmptySource_Consistency_Collection()
        {
            int[] source = { };

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(new Index(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(new Index(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(^0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestCollection<int>(source).ElementAt(new Index(int.MaxValue)));
        }

        [Fact]
        public void EmptySource_Consistency_NonGenericCollection()
        {
            int[] source = { };

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(new Index(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(^1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(new Index(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(^0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(int.MinValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(^int.MaxValue));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => new TestNonGenericCollection<int>(source.ToArray()).ElementAt(new Index(int.MaxValue)));
        }
    }
}
