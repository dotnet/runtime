// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class ParallelEnumerableTests
    {
        //
        // Null query
        //
        [Fact]
        public static void NullQuery()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).AsParallel());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable)null).AsParallel());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((Partitioner<int>)null).AsParallel());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((int[])null).AsParallel());

            AssertExtensions.Throws<ArgumentNullException>("source", () => ParallelEnumerable.AsOrdered((ParallelQuery<int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ParallelEnumerable.AsOrdered((ParallelQuery)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ParallelEnumerable.AsUnordered<int>((ParallelQuery<int>)null));
        }

        //
        // Range
        //
        public static IEnumerable<object[]> RangeData()
        {
            int[] datapoints = { 0, 1, 2, 16, };
            foreach (int sign in new[] { -1, 1 })
            {
                foreach (int start in datapoints)
                {
                    foreach (int count in datapoints)
                    {
                        yield return new object[] { start * sign, count };
                    }
                }
            }
            yield return new object[] { int.MaxValue, 0 };
            yield return new object[] { int.MaxValue, 1 };
            yield return new object[] { int.MaxValue - 8, 8 + 1 };
            yield return new object[] { int.MinValue, 0 };
            yield return new object[] { int.MinValue, 1 };
        }

        [Theory]
        [MemberData(nameof(RangeData))]
        public static void Range_UndefinedOrder(int start, int count)
        {
            ParallelQuery<int> query = ParallelEnumerable.Range(start, count);

            IntegerRangeSet seen = new IntegerRangeSet(start, count);
            Assert.All(query, x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(RangeData))]
        public static void Range_AsOrdered(int start, int count)
        {
            ParallelQuery<int> query = ParallelEnumerable.Range(start, count).AsOrdered();

            int current = start;
            Assert.All(query, x => Assert.Equal(unchecked(current++), x));
            Assert.Equal(count, unchecked(current - start));
        }

        [Theory]
        [MemberData(nameof(RangeData))]
        public static void Range_AsSequential(int start, int count)
        {
            IEnumerable<int> query = ParallelEnumerable.Range(start, count).AsSequential();

            int current = start;
            Assert.All(query, x => Assert.Equal(unchecked(current++), x));
            Assert.Equal(count, unchecked(current - start));
        }

        [Theory]
        [MemberData(nameof(RangeData))]
        public static void Range_First(int start, int count)
        {
            ParallelQuery<int> query = ParallelEnumerable.Range(start, count);

            if (count == 0)
            {
                Assert.Throws<InvalidOperationException>(() => query.First());
            }
            else
            {
                Assert.Equal(start, query.First());
            }
        }

        [Theory]
        [MemberData(nameof(RangeData))]
        public static void Range_FirstOrDefault(int start, int count)
        {
            ParallelQuery<int> query = ParallelEnumerable.Range(start, count);

            Assert.Equal(count == 0 ? 0 : start, query.FirstOrDefault());
        }

        [Theory]
        [MemberData(nameof(RangeData))]
        public static void Range_Last(int start, int count)
        {
            ParallelQuery<int> query = ParallelEnumerable.Range(start, count);

            if (count == 0)
            {
                Assert.Throws<InvalidOperationException>(() => query.Last());
            }
            else
            {
                Assert.Equal(start + (count - 1), query.Last());
            }
        }

        [Theory]
        [MemberData(nameof(RangeData))]
        public static void Range_LastOrDefault(int start, int count)
        {
            ParallelQuery<int> query = ParallelEnumerable.Range(start, count);

            Assert.Equal(count == 0 ? 0 : start + (count - 1), query.LastOrDefault());
        }

        [Theory]
        [MemberData(nameof(RangeData))]
        public static void Range_Take(int start, int count)
        {
            ParallelQuery<int> query = ParallelEnumerable.Range(start, count).Take(count / 2);

            // Elements are taken from the first half of the list, but order is indeterminate.
            IntegerRangeSet seen = new IntegerRangeSet(start, count / 2);
            Assert.All(query, x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [MemberData(nameof(RangeData))]
        public static void Range_Skip(int start, int count)
        {
            ParallelQuery<int> query = ParallelEnumerable.Range(start, count).Skip(count / 2);

            // Skips over the first half of the list, but order is indeterminate.
            IntegerRangeSet seen = new IntegerRangeSet(start + count / 2, (count + 1) / 2);
            Assert.All(query, x => seen.Add(x));
            seen.AssertComplete();
            Assert.Empty(ParallelEnumerable.Range(start, count).Skip(count + 1));
        }

        [Fact]
        public static void Range_Exception()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ParallelEnumerable.Range(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ParallelEnumerable.Range(-8, -8));
            Assert.Throws<ArgumentOutOfRangeException>(() => ParallelEnumerable.Range(int.MaxValue, 2));
        }

        //
        // Repeat
        //
        public static IEnumerable<object[]> RepeatData()
        {
            int[] datapoints = new[] { 0, 1, 2, 16, 128, 1024 };
            foreach (int count in datapoints)
            {
                foreach (int element in datapoints)
                {
                    yield return new object[] { element, count };
                    yield return new object[] { (long)element, count };
                    yield return new object[] { (double)element, count };
                    yield return new object[] { (decimal)element, count };
                    yield return new object[] { "" + element, count };
                }
                yield return new object[] { (object)null, count };
                yield return new object[] { (string)null, count };
            }
        }

        [Theory]
        [MemberData(nameof(RepeatData))]
        public static void Repeat<T>(T element, int count)
        {
            ParallelQuery<T> query = ParallelEnumerable.Repeat(element, count);

            int counted = 0;
            Assert.All(query, e => { counted++; Assert.Equal(element, e); });
            Assert.Equal(count, counted);
        }

        [Theory]
        [MemberData(nameof(RepeatData))]
        public static void Repeat_Select<T>(T element, int count)
        {
            ParallelQuery<T> query = ParallelEnumerable.Repeat(element, count).Select(i => i);

            int counted = 0;
            Assert.All(query, e => { counted++; Assert.Equal(element, e); });
            Assert.Equal(count, counted);
        }

        [Fact]
        public static void Repeat_Exception()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ParallelEnumerable.Repeat(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ParallelEnumerable.Repeat((long)1024, -1024));
            Assert.Throws<ArgumentOutOfRangeException>(() => ParallelEnumerable.Repeat(2.0, -2));
            Assert.Throws<ArgumentOutOfRangeException>(() => ParallelEnumerable.Repeat((decimal)8, -8));
            Assert.Throws<ArgumentOutOfRangeException>(() => ParallelEnumerable.Repeat("fail", -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ParallelEnumerable.Repeat((string)null, -1));
        }

        [Fact]
        public static void Repeat_Reset()
        {
            const int Value = 42;
            const int Iterations = 3;

            ParallelQuery<int> q = ParallelEnumerable.Repeat(Value, Iterations);

            IEnumerator<int> e = q.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int count = 0;
                while (e.MoveNext())
                {
                    Assert.Equal(Value, e.Current);
                    count++;
                }
                Assert.False(e.MoveNext());
                Assert.Equal(Iterations, count);

                e.Reset();
            }
        }

        //
        // Empty
        //
        public static IEnumerable<object[]> EmptyData()
        {
            yield return new object[] { default(int) };
            yield return new object[] { default(long) };
            yield return new object[] { default(double) };
            yield return new object[] { default(decimal) };
            yield return new object[] { default(string) };
            yield return new object[] { default(object) };
        }

        [Theory]
        [MemberData(nameof(EmptyData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "This causes assertion failure on UAPAoT")]
        public static void Empty<T>(T def)
        {
            Assert.Empty(ParallelEnumerable.Empty<T>());
            Assert.False(ParallelEnumerable.Empty<T>().Any(x => true));
            Assert.False(ParallelEnumerable.Empty<T>().Contains(default(T)));
            Assert.Equal(0, ParallelEnumerable.Empty<T>().Count());
            Assert.Equal(0, ParallelEnumerable.Empty<T>().LongCount());
            Assert.Equal(new T[0], ParallelEnumerable.Empty<T>().ToArray());
            Assert.Equal(new Dictionary<T, T>(), ParallelEnumerable.Empty<T>().ToDictionary(x => x));
            Assert.Equal(new List<T>(), ParallelEnumerable.Empty<T>().ToList());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<T>().First());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<T>().Last());
        }
    }
}
