// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class FirstOrDefaultTests : EnumerableTests
    {
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
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.FirstOrDefault(), q.FirstOrDefault());
        }

        private static void TestEmptyIList<T>()
        {
            T[] source = { };
            T expected = default(T);

            Assert.IsAssignableFrom<IList<T>>(source);

            Assert.Equal(expected, source.RunOnce().FirstOrDefault());
        }

        private static void TestEmptyIListDefault<T>(T defaultValue)
        {
            T[] source = { };

            Assert.IsAssignableFrom<IList<T>>(source);

            Assert.Equal(defaultValue, source.RunOnce().FirstOrDefault(defaultValue));
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
        public void EmptyIListDefault()
        {
            TestEmptyIListDefault(5); // int
            TestEmptyIListDefault("Hello"); // string
            TestEmptyIListDefault(DateTime.UnixEpoch); //DateTime
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
        public void IListOneElementDefault()
        {
            int[] source = { 5 };
            int expected = 5;

            Assert.IsAssignableFrom<IList<int>>(source);

            Assert.Equal(expected, source.FirstOrDefault(3));
        }

        [Fact]
        public void IListTManyElementsFirstIsDefault()
        {
            int?[] source = { null, -10, 2, 4, 3, 0, 2 };
            int? expected = null;

            Assert.IsAssignableFrom<IList<int?>>(source);

            Assert.Equal(expected, source.FirstOrDefault());
        }

        [Fact]
        public void IListTManyElementsFirstIsNotDefault()
        {
            int?[] source = { 19, null, -10, 2, 4, 3, 0, 2 };
            int? expected = 19;

            Assert.IsAssignableFrom<IList<int?>>(source);

            Assert.Equal(expected, source.FirstOrDefault());
        }

        private static void TestEmptyNotIList<T>()
        {
            static IEnumerable<T1> EmptySource<T1>()
            {
                yield break;
            }

            var source = EmptySource<T>();
            T expected = default(T);

            Assert.Null(source as IList<T>);

            Assert.Equal(expected, source.RunOnce().FirstOrDefault());
        }

        private static void TestEmptyNotIListDefault<T>(T defaultValue)
        {
            static IEnumerable<T1> EmptySource<T1>()
            {
                yield break;
            }

            var source = EmptySource<T>();

            Assert.Null(source as IList<T>);

            Assert.Equal(defaultValue, source.RunOnce().FirstOrDefault(defaultValue));
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
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(-5, 1);
            int expected = -5;

            Assert.Null(source as IList<int>);

            Assert.Equal(expected, source.FirstOrDefault());
        }

        [Fact]
        public void ManyElementsNotIListT()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(3, 10);
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
        public void OneElementTruePredicateDefault()
        {
            int[] source = { 4 };
            Func<int, bool> predicate = IsEven;
            int expected = 4;

            Assert.Equal(expected, source.FirstOrDefault(predicate, 5));
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
        public void ManyElementsPredicateFalseForAllDefault()
        {
            int[] source = { 9, 5, 1, 3, 17, 21 };
            Func<int, bool> predicate = IsEven;
            int expected = 5;

            Assert.Equal(expected, source.FirstOrDefault(predicate, 5));
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
        public void PredicateTrueOnlyForLastDefault()
        {
            int[] source = { 9, 5, 1, 3, 17, 21, 50 };
            Func<int, bool> predicate = IsEven;
            int expected = 50;

            Assert.Equal(expected, source.FirstOrDefault(predicate, 5));
        }

        [Fact]
        public void PredicateTrueForSome()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 17, 13, 8 };
            Func<int, bool> predicate = IsEven;
            int expected = 10;

            Assert.Equal(expected, source.FirstOrDefault(predicate));
        }

        [Fact]
        public void PredicateTrueForSomeDefault()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 17, 13, 8 };
            Func<int, bool> predicate = IsEven;
            int expected = 10;

            Assert.Equal(expected, source.FirstOrDefault(predicate, 5));
        }

        [Fact]
        public void PredicateTrueForSomeRunOnce()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 17, 13, 8 };
            Func<int, bool> predicate = IsEven;
            int expected = 10;

            Assert.Equal(expected, source.RunOnce().FirstOrDefault(predicate));
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).FirstOrDefault());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).FirstOrDefault(5));
        }

        [Fact]
        public void NullSourcePredicateUsed()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).FirstOrDefault(i => i != 2));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).FirstOrDefault(i => i != 2, 5));
        }

        [Fact]
        public void NullPredicate()
        {
            Func<int, bool> predicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).FirstOrDefault(predicate));
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).FirstOrDefault(predicate, 5));
        }
    }
}
