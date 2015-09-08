// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class LastOrDefaultTests
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

            Assert.Equal(q.LastOrDefault(), q.LastOrDefault());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                             where !String.IsNullOrEmpty(x)
                             select x;

            Assert.Equal(q.LastOrDefault(), q.LastOrDefault());
        }

        private static void TestEmptyIList<T>()
        {
            T[] source = { };
            T expected = default(T);
            
            Assert.IsAssignableFrom<IList<T>>(source);
            
            Assert.Equal(expected, source.LastOrDefault());
        }

        [Fact]
        public void EmptyIListT()
        {
            TestEmptyIList<int>();
            TestEmptyIList<string>();
            TestEmptyIList<DateTime>();
            TestEmptyIList<LastOrDefaultTests>();
        }

        [Fact]
        public void IListTOneElement()
        {
            int[] source = { 5 };
            int expected = 5;

            Assert.IsAssignableFrom<IList<int>>(source);
            
            Assert.Equal(expected, source.LastOrDefault());
        }


        [Fact]
        public void IListTManyELementsLastIsDefault()
        {
            int?[] source = { -10, 2, 4, 3, 0, 2, null };
            int? expected = null;

            Assert.IsAssignableFrom<IList<int?>>(source);
            
            Assert.Equal(expected, source.LastOrDefault());
        }

        [Fact]
        public void IListTManyELementsLastIsNotDefault()
        {
            int?[] source = { -10, 2, 4, 3, 0, 2, null, 19 };
            int? expected = 19;

            Assert.IsAssignableFrom<IList<int?>>(source);
            
            Assert.Equal(expected, source.LastOrDefault());
        }

        private static IEnumerable<T> EmptySource<T>()
        {
            yield break;
        }

        private static void TestEmptyNotIList<T>()
        {
            var source = EmptySource<T>();
            T expected = default(T);
            
            Assert.Null(source as IList<T>);
            
            Assert.Equal(expected, source.LastOrDefault());
        }

        [Fact]
        public void EmptyNotIListT()
        {
            TestEmptyNotIList<int>();
            TestEmptyNotIList<string>();
            TestEmptyNotIList<DateTime>();
            TestEmptyNotIList<LastOrDefaultTests>();
        }

        [Fact]
        public void OneElementNotIListT()
        {
            IEnumerable<int> source = NumList(-5, 1);
            int expected = -5;

            Assert.Null(source as IList<int>);
            
            Assert.Equal(expected, source.LastOrDefault());
        }

        [Fact]
        public void ManyElementsNotIListT()
        {
            IEnumerable<int> source = NumList(3, 10);
            int expected = 12;

            Assert.Null(source as IList<int>);
            
            Assert.Equal(expected, source.LastOrDefault());
        }

        [Fact]
        public void EmptyIListSource()
        {
            int?[] source = { };

            Assert.Null(source.LastOrDefault(x => true));
            Assert.Null(source.LastOrDefault(x => false));
        }

        [Fact]
        public void OneElementIListTruePredicate()
        {
            int[] source = { 4 };
            Func<int, bool> predicate = IsEven;
            int expected = 4;
            
            Assert.Equal(expected, source.LastOrDefault(predicate));
        }

        [Fact]
        public void ManyElementsIListPredicateFalseForAll()
        {
            int[] source = { 9, 5, 1, 3, 17, 21 };
            Func<int, bool> predicate = IsEven;
            int expected = default(int);

            Assert.Equal(expected, source.LastOrDefault(predicate));
        }

        [Fact]
        public void IListPredicateTrueOnlyForLast()
        {
            int[] source = { 9, 5, 1, 3, 17, 21, 50 };
            Func<int, bool> predicate = IsEven;
            int expected = 50;

            Assert.Equal(expected, source.LastOrDefault(predicate));
        }

        [Fact]
        public void IListPredicateTrueForSome()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 18, 13, 9 };
            Func<int, bool> predicate = IsEven;
            int expected = 18;

            Assert.Equal(expected, source.LastOrDefault(predicate));
        }

        [Fact]
        public void EmptyNotIListSource()
        {
            IEnumerable<int?> source = Enumerable.Repeat((int?)4, 0);

            Assert.Null(source.LastOrDefault(x => true));
            Assert.Null(source.LastOrDefault(x => false));
        }

        [Fact]
        public void OneElementNotIListTruePredicate()
        {
            IEnumerable<int> source = ForceNotCollection(new[] { 4 });
            Func<int, bool> predicate = IsEven;
            int expected = 4;

            Assert.Equal(expected, source.LastOrDefault(predicate));
        }

        [Fact]
        public void ManyElementsNotIListPredicateFalseForAll()
        {
            IEnumerable<int> source = ForceNotCollection(new int[] { 9, 5, 1, 3, 17, 21 });
            Func<int, bool> predicate = IsEven;
            int expected = default(int);

            Assert.Equal(expected, source.LastOrDefault(predicate));
        }

        [Fact]
        public void NotIListPredicateTrueOnlyForLast()
        {
            IEnumerable<int> source = ForceNotCollection(new int[] { 9, 5, 1, 3, 17, 21, 50 });
            Func<int, bool> predicate = IsEven;
            int expected = 50;

            Assert.Equal(expected, source.LastOrDefault(predicate));
        }

        [Fact]
        public void NotIListPredicateTrueForSome()
        {
            IEnumerable<int> source = ForceNotCollection(new int[] { 3, 7, 10, 7, 9, 2, 11, 18, 13, 9 });
            Func<int, bool> predicate = IsEven;
            int expected = 18;

            Assert.Equal(expected, source.LastOrDefault(predicate));
        }
    }
}
