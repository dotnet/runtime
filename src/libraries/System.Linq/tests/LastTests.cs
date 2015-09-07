// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class LastTests
    {
        private static IEnumerable<int> NumList(int start, int count)
        {
            for (int i = 0; i < count; i++)
                yield return start + i;
        }

        private static IEnumerable<T> ForceNotCollection<T>(IEnumerable<T> source)
        {
            foreach (T item in source) yield return item;
        }

        private static bool IsEven(int num)
        {
            return num % 2 == 0;
        }

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
            
            Assert.Throws<InvalidOperationException>(() => source.Last());
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
        public void IListTManyELementsLastIsDefault()
        {
            int?[] source = { -10, 2, 4, 3, 0, 2, null };
            int? expected = null;

            Assert.IsAssignableFrom<IList<int?>>(source);
            
            Assert.Equal(expected, source.Last());
        }

        [Fact]
        public void IListTManyELementsLastIsNotDefault()
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
            
            Assert.Throws<InvalidOperationException>(() => source.Last());
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
            IEnumerable<int> source = NumList(-5, 1);
            int expected = -5;

            Assert.Null(source as IList<int>);
            
            Assert.Equal(expected, source.Last());
        }

        [Fact]
        public void ManyElementsNotIListT()
        {
            IEnumerable<int> source = NumList(3, 10);
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
        public void NotIListIListEmptySourcePredicate()
        {
            IEnumerable<int> source = Enumerable.Range(1, 0);

            Assert.Throws<InvalidOperationException>(() => source.Last(x => true));
            Assert.Throws<InvalidOperationException>(() => source.Last(x => false));
        }

        [Fact]
        public void OneElementNotIListTruePredicate()
        {
            IEnumerable<int> source = NumList(4, 1);
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
    }
}
