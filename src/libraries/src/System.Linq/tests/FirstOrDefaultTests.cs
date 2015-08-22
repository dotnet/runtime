// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class FirstOrDefaultTests
    {
        private static IEnumerable<int> NumList(int start, int count)
        {
            for (int i = 0; i < count; i++)
                yield return start + i;
        }

        private static bool IsEven(int num)
        {
            return num % 2 == 0;
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            IEnumerable<int> ieInt = Enumerable.Range(0, 0);
            var q = from x in ieInt
                    select x;

            Assert.Equal(q.FirstOrDefault(), q.FirstOrDefault());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                    where !String.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.FirstOrDefault(), q.FirstOrDefault());
        }
        
        private static void TestEmptyIList<T>()
        {
            T[] source = { };
            T expected = default(T);
            
            Assert.IsAssignableFrom<IList<T>>(source);
            
            Assert.Equal(expected, source.FirstOrDefault());
        }

        [Fact]
        public void EmptyIListT()
        {
            TestEmptyIList<int>();
            TestEmptyIList<string>();
            TestEmptyIList<DateTime>();
            TestEmptyIList<FirstOrDefaultTests>();
        }

        [Fact]
        public void IListTOneElement()
        {
            int[] source = { 5 };
            int expected = 5;

            Assert.IsAssignableFrom<IList<int>>(source);
            
            Assert.Equal(expected, source.FirstOrDefault());
        }

        [Fact]
        public void IListTManyELementsFirstIsDefault()
        {
            int?[] source = { null, -10, 2, 4, 3, 0, 2 };
            int? expected = null;

            Assert.IsAssignableFrom<IList<int?>>(source);
            
            Assert.Equal(expected, source.FirstOrDefault());
        }

        [Fact]
        public void IListTManyELementsFirstIsNotDefault()
        {
            int?[] source = { 19, null, -10, 2, 4, 3, 0, 2 };
            int? expected = 19;

            Assert.IsAssignableFrom<IList<int?>>(source);
            
            Assert.Equal(expected, source.FirstOrDefault());
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
            
            Assert.Equal(expected, source.FirstOrDefault());
        }

        [Fact]
        public void EmptyNotIListT()
        {
            TestEmptyNotIList<int>();
            TestEmptyNotIList<string>();
            TestEmptyNotIList<DateTime>();
            TestEmptyNotIList<FirstOrDefaultTests>();
        }

        [Fact]
        public void OneElementNotIListT()
        {
            IEnumerable<int> source = NumList(-5, 1);
            int expected = -5;

            Assert.Null(source as IList<int>);
            
            Assert.Equal(expected, source.FirstOrDefault());
        }

        [Fact]
        public void ManyElementsNotIListT()
        {
            IEnumerable<int> source = NumList(3, 10);
            int expected = 3;

            Assert.Null(source as IList<int>);
            
            Assert.Equal(expected, source.FirstOrDefault());
        }

        [Fact]
        public void EmptySource()
        {
            int?[] source = { };

            Assert.Null(source.FirstOrDefault(x => true));
            Assert.Null(source.FirstOrDefault(x => false));
        }

        [Fact]
        public void OneElementTruePredicate()
        {
            int[] source = { 4 };
            Func<int, bool> predicate = IsEven;
            int expected = 4;
            
            Assert.Equal(expected, source.FirstOrDefault(predicate));
        }

        [Fact]
        public void ManyElementsPredicateFalseForAll()
        {
            int[] source = { 9, 5, 1, 3, 17, 21 };
            Func<int, bool> predicate = IsEven;
            int expected = default(int);

            Assert.Equal(expected, source.FirstOrDefault(predicate));
        }

        [Fact]
        public void PredicateTrueOnlyForLast()
        {
            int[] source = { 9, 5, 1, 3, 17, 21, 50 };
            Func<int, bool> predicate = IsEven;
            int expected = 50;

            Assert.Equal(expected, source.FirstOrDefault(predicate));
        }

        [Fact]
        public void PredicateTrueForSome()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 17, 13, 8 };
            Func<int, bool> predicate = IsEven;
            int expected = 10;

            Assert.Equal(expected, source.FirstOrDefault(predicate));
        }
    }
}
