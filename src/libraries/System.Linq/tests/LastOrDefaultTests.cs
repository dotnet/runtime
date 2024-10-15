// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class LastOrDefaultTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                             where x > int.MinValue
                             select x;

            Assert.Equal(q.LastOrDefault(), q.LastOrDefault());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                             where !string.IsNullOrEmpty(x)
                             select x;

            Assert.Equal(q.LastOrDefault(), q.LastOrDefault());
        }

        private static void TestEmptyIList<T>()
        {
            T[] source = { };
            T expected = default(T);

            Assert.IsAssignableFrom<IList<T>>(source);

            Assert.Equal(expected, source.RunOnce().LastOrDefault());
        }

        private static void TestEmptyIListDefault<T>(T defaultValue)
        {
            T[] source = { };

            Assert.IsAssignableFrom<IList<T>>(source);

            Assert.Equal(defaultValue, source.RunOnce().LastOrDefault(defaultValue));
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
        public void EmptyIList()
        {
            TestEmptyIListDefault(5); // int
            TestEmptyIListDefault("Hello"); // string
            TestEmptyIListDefault(DateTime.UnixEpoch);
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
        public void IListTOneElementDefault()
        {
            int[] source = { 5 };
            int expected = 5;

            Assert.IsAssignableFrom<IList<int>>(source);

            Assert.Equal(expected, source.LastOrDefault(4));
        }


        [Fact]
        public void IListTManyElementsLastIsDefault()
        {
            int?[] source = { -10, 2, 4, 3, 0, 2, null };
            int? expected = null;

            Assert.IsAssignableFrom<IList<int?>>(source);

            Assert.Equal(expected, source.LastOrDefault());
        }

        [Fact]
        public void IListTManyElementsLastIsNotDefault()
        {
            int?[] source = { -10, 2, 4, 3, 0, 2, null, 19 };
            int? expected = 19;

            Assert.IsAssignableFrom<IList<int?>>(source);

            Assert.Equal(expected, source.LastOrDefault());
        }

        [Fact]
        public void IListTManyElementsLastHasDefault()
        {
            int?[] source = { -10, 2, 4, 3, 0, 2, null };
            int? expected = null;

            Assert.IsAssignableFrom<IList<int?>>(source);

            Assert.Equal(expected, source.LastOrDefault(5));
        }

        [Fact]
        public void IListTManyElementsLastIsHasDefault()
        {
            int?[] source = { -10, 2, 4, 3, 0, 2, null, 19 };
            int? expected = 19;

            Assert.IsAssignableFrom<IList<int?>>(source);

            Assert.Equal(expected, source.LastOrDefault(5));
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

            Assert.Equal(expected, source.RunOnce().LastOrDefault());
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
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(-5, 1);
            int expected = -5;

            Assert.Null(source as IList<int>);

            Assert.Equal(expected, source.LastOrDefault());
        }

        [Fact]
        public void ManyElementsNotIListT()
        {
            IEnumerable<int> source = NumberRangeGuaranteedNotCollectionType(3, 10);
            int expected = 12;

            Assert.Null(source as IList<int>);

            Assert.Equal(expected, source.LastOrDefault());
        }

        [Fact]
        public void EmptySourcePredicate()
        {
            int?[] source = { };

            Assert.All(CreateSources(source), source =>
            {
                Assert.Null(source.LastOrDefault(x => true));
                Assert.Null(source.LastOrDefault(x => false));
            });
        }

        [Fact]
        public void OneElementTruePredicate()
        {
            int[] source = { 4 };
            Func<int, bool> predicate = IsEven;
            int expected = 4;

            Assert.All(CreateSources(source), source =>
            {
                Assert.Equal(expected, source.LastOrDefault(predicate));
            });
        }

        [Fact]
        public void OneElementTruePredicateDefault()
        {
            int[] source = { 4 };
            Func<int, bool> predicate = IsEven;
            int expected = 4;

            Assert.All(CreateSources(source), source =>
            {
                Assert.Equal(expected, source.LastOrDefault(predicate, 5));
            });
        }

        [Fact]
        public void ManyElementsPredicateFalseForAll()
        {
            int[] source = { 9, 5, 1, 3, 17, 21 };
            Func<int, bool> predicate = IsEven;
            int expected = default(int);

            Assert.All(CreateSources(source), source =>
            {
                Assert.Equal(expected, source.LastOrDefault(predicate));
            });
        }

        [Fact]
        public void ManyElementsPredicateFalseForAllDefault()
        {
            int[] source = { 9, 5, 1, 3, 17, 21 };
            Func<int, bool> predicate = IsEven;
            int expected = 5;

            Assert.All(CreateSources(source), source =>
            {
                Assert.Equal(expected, source.LastOrDefault(predicate, 5));
            });
        }

        [Fact]
        public void PredicateTrueOnlyForLast()
        {
            int[] source = { 9, 5, 1, 3, 17, 21, 50 };
            Func<int, bool> predicate = IsEven;
            int expected = 50;

            Assert.All(CreateSources(source), source =>
            {
                Assert.Equal(expected, source.LastOrDefault(predicate));
            });
        }

        [Fact]
        public void PredicateTrueForSome()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 18, 13, 9 };
            Func<int, bool> predicate = IsEven;
            int expected = 18;

            Assert.All(CreateSources(source), source =>
            {
                Assert.Equal(expected, source.LastOrDefault(predicate));
            });
        }

        [Fact]
        public void PredicateTrueForSomeRunOnce()
        {
            int[] source = { 3, 7, 10, 7, 9, 2, 11, 18, 13, 9 };
            Func<int, bool> predicate = IsEven;
            int expected = 18;

            Assert.All(CreateSources(source), source =>
            {
                Assert.Equal(expected, source.RunOnce().LastOrDefault(predicate));
            });
        }

        [Fact]
        public void NullSource()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).LastOrDefault());
        }

        [Fact]
        public void NullSourcePredicateUsed()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).LastOrDefault(i => i != 2));
        }

        [Fact]
        public void NullPredicate()
        {
            Func<int, bool> predicate = null;
            AssertExtensions.Throws<ArgumentNullException>("predicate", () => Enumerable.Range(0, 3).LastOrDefault(predicate));
        }
    }
}
