// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class LastTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                             where x > Int32.MinValue
                             select x;

            Assert.Equal(q.Last(), q.Last());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                             where !String.IsNullOrEmpty(x)
                             select x;

            Assert.Equal(q.Last(), q.Last());
        }

        public void TestEmptyIList<T>()
        {
            T[] source = { };
            
            Assert.NotNull(source as IList<T>);
            
            Assert.Throws<InvalidOperationException>(() => source.RunOnce().Last());
        }

        [Fact]
        public void EmptyIListT()
        {
            TestEmptyIList<int>();
            TestEmptyIList<string>();
            TestEmptyIList<DateTime>();
            TestEmptyIList<LastTests>();
        }

        [Fact]
        public void IListTOneElement()
        {
            int[] source = { 5 };
            int expected = 5;

            Assert.NotNull(source as IList<int>);
            
            Assert.Equal(expected, source.Last());
        }

        [Fact]
        public void IListTManyElementsLastIsDefault()
        {
            int?[] source = { -10, 2, 4, 3, 0, 2, null };
            int? expected = null;

            Assert.IsAssignableFrom<IList<int?>>(source);
            
            Assert.Equal(expected, source.Last());
        }

        [Fact]
        public void IListTManyElementsLastIsNotDefault()
        {
            int?[] source = { -10, 2, 4, 3, 0, 2, null, 19 };
            int? expected = 19;

            Assert.IsAssignableFrom<IList<int?>>(source);
            
            Assert.Equal(expected, source.Last());
        }

        private static IEnumerable<T> EmptySource<T>()
        {
            yield break;
        }

        private static void TestEmptyNotIList<T>()
        {
            var source = EmptySource<T>();

            Assert.Null(source as IList<T>);
            
            Assert.Throws<InvalidOperationException>(() => source.RunOnce().Last());
        }

        [Fact]
        public void EmptyNotIListT()
        {
            TestEmptyNotIList<int>();
            TestEmptyNotIList<string>();
            TestEmptyNotIList<DateTime>();
            TestEmptyNotIList<LastTests>();
        }

        [Fact]
        public void OneElementNotIListT()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(-5, 1);
            int expected = -5;

            Assert.Null(source as IList<int>);
            
            Assert.Equal(expected, source.Last());
        }

        [Fact]
        public void ManyElementsNotIListT()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(3, 10);
            int expected = 12;

            Assert.Null(source as IList<int>);
            
            Assert.Equal(expected, source.Last());
        }

        [Fact]
        public void IListEmptySourcePredicate()
        {
            int[] source = { };

            Assert.Throws<InvalidOperationException>(() => source.Last(x => true));
            Assert.Throws<InvalidOperationException>(() => source.Last(x => false));
        }

        [Fact]
        public void OneElementIListTruePredicate()
        {
            int[] source = { 4 };
            Func<int, bool> predicate = IsEven;
            int expected = 4;
            
            Assert.Equal(expected, source.Last(predicate));
        }

        [Fact]
        public void ManyElementsIListPredicateFalseForAll()
        {
            int[] source = { 9, 5, 1, 3, 17, 21 };
            Func<int, bool> predicate = IsEven;

            Assert.Throws<InvalidOperationException>(() => source.Last(predicate));
        }

        [Fact]
        public void IListPredicateTrueOnlyForLast()
        {
            int[] source = { 9, 5, 1, 3, 17, 21, 50 };
            Func<int, bool> predicate = IsEven;
            int expected = 50;

            Assert.Equal(expected, source.Last(predicate));
        }

        [Fact]
        public void IListPredicateTrueForSome()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 18, 13, 9 };
            Func<int, bool> predicate = IsEven;
            int expected = 18;

            Assert.Equal(expected, source.Last(predicate));
        }

        [Fact]
        public void IListPredicateTrueForSomeRunOnce()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 18, 13, 9 };
            Func<int, bool> predicate = IsEven;
            int expected = 18;

            Assert.Equal(expected, source.RunOnce().Last(predicate));
        }

        [Fact]
        public void NotIListIListEmptySourcePredicate()
        {
            IEnumerable<int> source = Enumerable.Range(1, 0);

            Assert.Throws<InvalidOperationException>(() => source.Last(x => true));
            Assert.Throws<InvalidOperationException>(() => source.Last(x => false));
        }

        [Fact]
        public void OneElementNotIListTruePredicate()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(4, 1);
            Func<int, bool> predicate = IsEven;
            int expected = 4;
            
            Assert.Equal(expected, source.Last(predicate));
        }

        [Fact]
        public void ManyElementsNotIListPredicateFalseForAll()
        {
            IEnumerable<int> source = ForceNotCollection(new int[] { 9, 5, 1, 3, 17, 21 });
            Func<int, bool> predicate = IsEven;

            Assert.Throws<InvalidOperationException>(() => source.Last(predicate));
        }

        [Fact]
        public void NotIListPredicateTrueOnlyForLast()
        {
            IEnumerable<int> source = ForceNotCollection(new int[] { 9, 5, 1, 3, 17, 21, 50 });
            Func<int, bool> predicate = IsEven;
            int expected = 50;

            Assert.Equal(expected, source.Last(predicate));
        }

        [Fact]
        public void NotIListPredicateTrueForSome()
        {
            IEnumerable<int> source = ForceNotCollection(new int[] { 3, 7, 10, 7, 9, 2, 11, 18, 13, 9 });
            Func<int, bool> predicate = IsEven;
            int expected = 18;

            Assert.Equal(expected, source.Last(predicate));
        }

        [Fact]
        public void NotIListPredicateTrueForSomeRunOnce()
        {
            IEnumerable<int> source = ForceNotCollection(new int[] { 3, 7, 10, 7, 9, 2, 11, 18, 13, 9 });
            Func<int, bool> predicate = IsEven;
            int expected = 18;

            Assert.Equal(expected, source.RunOnce().Last(predicate));
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Last());
        }

        [Fact]
        public void NullSourcePredicateUsed()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Last(i => i != 2));
        }

        [Fact]
        public void NullPredicate()
        {
            Func<int, bool> predicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).Last(predicate));
        }
    }
}
