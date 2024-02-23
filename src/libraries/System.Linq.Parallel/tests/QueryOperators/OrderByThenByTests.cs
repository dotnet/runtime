// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class OrderByThenByTests
    {
        private const int KeyFactor = 4;
        private const int GroupFactor = 8;
        private const int LongRunningCount = 16 * 1024;

        // Get ranges from 0 to each count.  The data is random, seeded from the size of the range.
        public static IEnumerable<object[]> OrderByRandomData(int[] counts)
        {
            foreach (int count in counts)
            {
                int[] randomInput = GetRandomInput(count);
                yield return new object[] { Labeled.Label("Array-Random", randomInput.AsParallel()), count };
                yield return new object[] { Labeled.Label("List-Random", randomInput.ToList().AsParallel()), count };
                yield return new object[] { Labeled.Label("Partitioner-Random", Partitioner.Create(randomInput).AsParallel()), count };
            }
        }

        private static int[] GetRandomInput(int count)
        {
            Random source = new Random(count);
            int[] data = new int[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = source.Next(count);
            }
            return data;
        }

        // Get a set of ranges, from 0 to each count, and an additional parameter denoting degree of parallelism.
        public static IEnumerable<object[]> OrderByThreadedData(int[] counts, int[] degrees)
        {
            foreach (object[] results in UnorderedSources.Ranges(counts, x => degrees.Cast<int>()))
            {
                yield return results;
            }
            foreach (object[] results in Sources.Ranges(counts, x => degrees.Cast<int>()))
            {
                yield return results;
            }

            foreach (object[] results in OrderByRandomData(counts))
            {
                foreach (int degree in degrees)
                {
                    yield return new object[] { results[0], results[1], degree };
                }
            }
        }

        //
        // OrderBy
        //
        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            int seen = 0;
            foreach (int i in labeled.Item.OrderBy(x => x))
            {
                Assert.InRange(i, prev, count - 1);
                prev = i;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderBy(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_Reversed(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            int seen = 0;
            foreach (int i in labeled.Item.OrderBy(x => -x))
            {
                Assert.InRange(i, 0, prev);
                prev = i;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_Reversed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderBy_Reversed(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            int seen = 0;
            foreach (int i in labeled.Item.OrderByDescending(x => x))
            {
                Assert.InRange(i, 0, prev);
                prev = i;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderByDescending(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_Reversed(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            int seen = 0;
            foreach (int i in labeled.Item.OrderByDescending(x => -x))
            {
                Assert.InRange(i, prev, count - 1);
                prev = i;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_Reversed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderByDescending_Reversed(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            int seen = 0;
            Assert.All(labeled.Item.OrderBy(x => x).ToList(), x => { Assert.InRange(x, prev, count - 1); prev = x; seen++; });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderBy_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_Reversed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            int seen = 0;
            Assert.All(labeled.Item.OrderBy(x => -x).ToList(), x => { Assert.InRange(x, 0, prev); prev = x; seen++; });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_Reversed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderBy_Reversed_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            int seen = 0;
            Assert.All(labeled.Item.OrderByDescending(x => x).ToList(), x => { Assert.InRange(x, 0, prev); prev = x; seen++; });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderByDescending_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_Reversed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            int seen = 0;
            Assert.All(labeled.Item.OrderByDescending(x => -x).ToList(), x => { Assert.InRange(x, prev, count - 1); prev = x; seen++; });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_Reversed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderByDescending_Reversed_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            int seen = 0;
            foreach (int i in labeled.Item.OrderBy(x => x, ReverseComparer.Instance))
            {
                Assert.InRange(i, 0, prev);
                prev = i;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderBy_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            int seen = 0;
            foreach (int i in labeled.Item.OrderByDescending(x => x, ReverseComparer.Instance))
            {
                Assert.InRange(i, prev, count - 1);
                prev = i;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderByDescending_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_NotPipelined_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            int seen = 0;
            Assert.All(labeled.Item.OrderBy(x => x, ReverseComparer.Instance).ToList(), x => { Assert.InRange(x, 0, prev); prev = x; seen++; });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_NotPipelined_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderBy_NotPipelined_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_NotPipelined_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            int seen = 0;
            Assert.All(labeled.Item.OrderByDescending(x => x, ReverseComparer.Instance).ToList(), x => { Assert.InRange(x, prev, count - 1); prev = x; seen++; });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_NotPipelined_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderByDescending_NotPipelined_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        // Regression test for the PLINQ version of https://github.com/dotnet/runtime/issues/14793 - comparer returning max/min value.
        public static void OrderBy_ExtremeComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            foreach (int i in labeled.Item.OrderBy(x => x, new ExtremeComparer<int>()))
            {
                Assert.InRange(i, prev, count - 1);
                prev = i;
            }
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        // Regression test for the PLINQ version of https://github.com/dotnet/runtime/issues/14793 - comparer returning max/min value.
        public static void OrderByDescending_ExtremeComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            foreach (int i in labeled.Item.OrderByDescending(x => x, new ExtremeComparer<int>()))
            {
                Assert.InRange(i, 0, prev);
                prev = i;
            }
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_NotPipelined_ExtremeComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            Assert.All(labeled.Item.OrderBy(x => x, new ExtremeComparer<int>()).ToList(), x => { Assert.InRange(x, prev, count - 1); prev = x; });
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_NotPipelined_ExtremeComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            Assert.All(labeled.Item.OrderByDescending(x => x, new ExtremeComparer<int>()).ToList(), x => { Assert.InRange(x, 0, prev); prev = x; });
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 2 }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_NotComparable(Labeled<ParallelQuery<int>> labeled, int count)
        {
            _ = count;
            ParallelQuery<int> query = labeled.Item.OrderBy(x => new NotComparable(x));
            AssertThrows.Wrapped<ArgumentException>(() => { foreach (int i in query) ; });
            AssertThrows.Wrapped<ArgumentException>(() => query.ToList());
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_NotComparable_Comparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            var comparer = Comparer<NotComparable>.Create((x, y) => ReverseComparer.Instance.Compare(x.Value, y.Value));
            foreach (int i in labeled.Item.OrderBy(x => new NotComparable(-x), comparer))
            {
                Assert.InRange(i, prev, count - 1);
                prev = i;
            }
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderBy_NotPipelined_NotComparable_Comparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            var comparer = Comparer<NotComparable>.Create((x, y) => ReverseComparer.Instance.Compare(x.Value, y.Value));
            Assert.All(labeled.Item.OrderBy(x => new NotComparable(-x), comparer).ToList(), x => { Assert.InRange(x, prev, count - 1); prev = x; });
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 2 }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_NotComparable(Labeled<ParallelQuery<int>> labeled, int count)
        {
            _ = count;
            ParallelQuery<int> query = labeled.Item.OrderByDescending(x => new NotComparable(x));
            AssertThrows.Wrapped<ArgumentException>(() => { foreach (int i in query) ; });
            AssertThrows.Wrapped<ArgumentException>(() => query.ToList());
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_NotComparable_Comparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            var comparer = Comparer<NotComparable>.Create((x, y) => ReverseComparer.Instance.Compare(x.Value, y.Value));
            foreach (int i in labeled.Item.OrderByDescending(x => new NotComparable(-x), comparer))
            {
                Assert.InRange(i, 0, prev);
                prev = i;
            }
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_NotPipelined_NotComparable_Comparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            var comparer = Comparer<NotComparable>.Create((x, y) => ReverseComparer.Instance.Compare(x.Value, y.Value));
            Assert.All(labeled.Item.OrderByDescending(x => new NotComparable(-x), comparer).ToList(), x => { Assert.InRange(x, 0, prev); prev = x; });
        }

        [Fact]
        public static void OrderBy_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).OrderBy(x => x));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).OrderBy((Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).OrderBy(x => x, Comparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).OrderBy((Func<int, int>)null, Comparer<int>.Default));
        }

        [Fact]
        public static void OrderByDescending_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).OrderByDescending(x => x));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).OrderByDescending((Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).OrderByDescending(x => x, Comparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).OrderByDescending((Func<int, int>)null, Comparer<int>.Default));
        }

        // Heavily exercises OrderBy in the face of user-delegate exceptions.
        // On CTP-M1, this would deadlock for DOP=7,9,11,... on 4-core, but works for DOP=1..6 and 8,10,12, ...
        //
        // In this test, every call to the key-selector delegate throws.
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        [MemberData(nameof(OrderByThreadedData), new[] { 1, 2, 16, 128, 1024 }, new[] { 1, 2, 4, 7, 8, 31, 32 })]
        public static void OrderBy_ThreadedDeadlock(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            _ = count;
            ParallelQuery<int> query = labeled.Item.WithDegreeOfParallelism(degree).OrderBy<int, int>(x => { throw new DeliberateTestException(); });

            AggregateException ae = Assert.Throws<AggregateException>(() => { foreach (int i in query) { } });
            Assert.All(ae.InnerExceptions, e => Assert.IsType<DeliberateTestException>(e));
        }

        // Heavily exercises OrderBy, but only throws one user delegate exception to simulate an occasional failure.
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedAndBlockingWait))]
        [MemberData(nameof(OrderByThreadedData), new[] { 1, 2, 16, 128, 1024 }, new[] { 1, 2, 4, 7, 8, 31, 32 })]
        public static void OrderBy_ThreadedDeadlock_SingleException(Labeled<ParallelQuery<int>> labeled, int count, int degree)
        {
            int countdown = Math.Min(count / 2, degree) + 1;

            ParallelQuery<int> query = labeled.Item.WithDegreeOfParallelism(degree).OrderBy(x => { if (Interlocked.Decrement(ref countdown) == 0) throw new DeliberateTestException(); return x; });

            AggregateException ae = Assert.Throws<AggregateException>(() => { foreach (int i in query) { } });
            Assert.Single(ae.InnerExceptions);
            Assert.All(ae.InnerExceptions, e => Assert.IsType<DeliberateTestException>(e));
        }

        //
        // Thenby
        //
        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, GroupFactor * 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = 0;
            int prevSecondary = count - 1;
            int seen = 0;
            foreach (int i in labeled.Item.OrderBy(x => x % GroupFactor).ThenBy(x => -x))
            {
                Assert.InRange(i % GroupFactor, prevPrimary, count - 1);
                if (i % GroupFactor != prevPrimary)
                {
                    prevPrimary = i % GroupFactor;
                    prevSecondary = count - 1;
                }
                Assert.InRange(i, 0, prevSecondary);
                prevSecondary = i;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, GroupFactor * 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_Reversed(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = GroupFactor - 1;
            int prevSecondary = 0;
            int seen = 0;
            foreach (int i in labeled.Item.OrderBy(x => -x % GroupFactor).ThenBy(x => x))
            {
                Assert.InRange(i % GroupFactor, 0, prevPrimary);
                if (i % GroupFactor != prevPrimary)
                {
                    prevPrimary = i % GroupFactor;
                    prevSecondary = 0;
                }
                Assert.InRange(i, prevSecondary, count - 1);
                prevSecondary = i;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_Reversed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_Reversed(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, GroupFactor * 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = GroupFactor - 1;
            int prevSecondary = 0;
            int seen = 0;
            foreach (int i in labeled.Item.OrderByDescending(x => x % GroupFactor).ThenByDescending(x => -x))
            {
                Assert.InRange(i % GroupFactor, 0, prevPrimary);
                if (i % GroupFactor != prevPrimary)
                {
                    prevPrimary = i % GroupFactor;
                    prevSecondary = 0;
                }
                Assert.InRange(i, prevSecondary, count - 1);
                prevSecondary = i;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, GroupFactor * 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_Reversed(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = 0;
            int prevSecondary = count - 1;
            int seen = 0;
            foreach (int i in labeled.Item.OrderByDescending(x => -x % GroupFactor).ThenByDescending(x => x))
            {
                Assert.InRange(i % GroupFactor, prevPrimary, count - 1);
                if (i % GroupFactor != prevPrimary)
                {
                    prevPrimary = i % GroupFactor;
                    prevSecondary = count - 1;
                }
                Assert.InRange(i, 0, prevSecondary);
                prevSecondary = i;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_Reversed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_Reversed(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, GroupFactor * 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = 0;
            int prevSecondary = count - 1;
            int seen = 0;
            Assert.All(labeled.Item.OrderBy(x => x % GroupFactor).ThenBy(x => -x).ToList(),
                x =>
                {
                    Assert.InRange(x % GroupFactor, prevPrimary, count - 1);
                    if (x % GroupFactor != prevPrimary)
                    {
                        prevPrimary = x % GroupFactor;
                        prevSecondary = count - 1;
                    }
                    Assert.InRange(x, 0, prevSecondary);
                    prevSecondary = x;
                    seen++;
                });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, GroupFactor * 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_Reversed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = GroupFactor - 1;
            int prevSecondary = 0;
            int seen = 0;
            Assert.All(labeled.Item.OrderBy(x => -x % GroupFactor).ThenBy(x => x).ToList(),
                x =>
                {
                    Assert.InRange(x % GroupFactor, 0, prevPrimary);
                    if (x % GroupFactor != prevPrimary)
                    {
                        prevPrimary = x % GroupFactor;
                        prevSecondary = 0;
                    }
                    Assert.InRange(x, prevSecondary, count - 1);
                    prevSecondary = x;
                    seen++;
                });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_Reversed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_Reversed_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, GroupFactor * 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = GroupFactor - 1;
            int prevSecondary = 0;
            int seen = 0;
            Assert.All(labeled.Item.OrderByDescending(x => x % GroupFactor).ThenByDescending(x => -x).ToList(),
                x =>
                {
                    Assert.InRange(x % GroupFactor, 0, prevPrimary);
                    if (x % GroupFactor != prevPrimary)
                    {
                        prevPrimary = x % GroupFactor;
                        prevSecondary = 0;
                    }
                    Assert.InRange(x, prevSecondary, count - 1);
                    prevSecondary = x;
                    seen++;
                });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, GroupFactor * 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_Reversed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = 0;
            int prevSecondary = count - 1;
            int seen = 0;
            Assert.All(labeled.Item.OrderByDescending(x => -x % GroupFactor).ThenByDescending(x => x).ToList(),
                x =>
                {
                    Assert.InRange(x % GroupFactor, prevPrimary, count - 1);
                    if (x % GroupFactor != prevPrimary)
                    {
                        prevPrimary = x % GroupFactor;
                        prevSecondary = count - 1;
                    }
                    Assert.InRange(x, 0, prevSecondary);
                    prevSecondary = x;
                    seen++;
                });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_Reversed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_Reversed_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, GroupFactor * 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = 0;
            int prevSecondary = count - 1;
            int seen = 0;
            foreach (int i in labeled.Item.OrderBy(x => x % GroupFactor).ThenBy(x => x, ReverseComparer.Instance))
            {
                Assert.InRange(i % GroupFactor, prevPrimary, count - 1);
                if (i % GroupFactor != prevPrimary)
                {
                    prevPrimary = i % GroupFactor;
                    prevSecondary = count - 1;
                }
                Assert.InRange(i, 0, prevSecondary);
                prevSecondary = i;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, GroupFactor * 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = GroupFactor - 1;
            int prevSecondary = 0;
            int seen = 0;
            foreach (int i in labeled.Item.OrderByDescending(x => x % GroupFactor).ThenByDescending(x => x, ReverseComparer.Instance))
            {
                Assert.InRange(i % GroupFactor, 0, prevPrimary);
                if (i % GroupFactor != prevPrimary)
                {
                    prevPrimary = i % GroupFactor;
                    prevSecondary = 0;
                }
                Assert.InRange(i, prevSecondary, count - 1);
                prevSecondary = i;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, GroupFactor * 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_NotPipelined_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = 0;
            int prevSecondary = count - 1;
            int seen = 0;
            Assert.All(labeled.Item.OrderBy(x => x % GroupFactor).ThenBy(x => x, ReverseComparer.Instance).ToList(),
                x =>
                {
                    Assert.InRange(x % GroupFactor, prevPrimary, count - 1);
                    if (x % GroupFactor != prevPrimary)
                    {
                        prevPrimary = x % GroupFactor;
                        prevSecondary = count - 1;
                    }
                    Assert.InRange(x, 0, prevSecondary);
                    prevSecondary = x;
                    seen++;
                });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_NotPipelined_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_NotPipelined_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, GroupFactor * 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, GroupFactor * 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_NotPipelined_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = GroupFactor - 1;
            int prevSecondary = 0;
            int seen = 0;
            Assert.All(labeled.Item.OrderByDescending(x => x % GroupFactor).ThenByDescending(x => x, ReverseComparer.Instance).ToList(),
                x =>
                {
                    Assert.InRange(x % GroupFactor, 0, prevPrimary);
                    if (x % GroupFactor != prevPrimary)
                    {
                        prevPrimary = x % GroupFactor;
                        prevSecondary = 0;
                    }
                    Assert.InRange(x, prevSecondary, count - 1);
                    prevSecondary = x;
                    seen++;
                });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_NotPipelined_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_NotPipelined_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        // Regression test for the PLINQ version of https://github.com/dotnet/runtime/issues/14793 - comparer returning max/min value.
        public static void ThenBy_ExtremeComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            foreach (int i in labeled.Item.OrderBy(x => 0).ThenBy(x => x, new ExtremeComparer<int>()))
            {
                Assert.InRange(i, prev, count - 1);
                prev = i;
            }
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        // Regression test for the PLINQ version of https://github.com/dotnet/runtime/issues/14793 - comparer returning max/min value.
        public static void ThenByDescending_ExtremeComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            foreach (int i in labeled.Item.OrderBy(x => 0).ThenByDescending(x => x, new ExtremeComparer<int>()))
            {
                Assert.InRange(i, 0, prev);
                prev = i;
            }
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_NotPipelined_ExtremeComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            Assert.All(labeled.Item.OrderBy(x => 0).ThenBy(x => x, new ExtremeComparer<int>()).ToList(), x => { Assert.InRange(x, prev, count - 1); prev = x; });
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_NotPipelined_ExtremeComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            Assert.All(labeled.Item.OrderBy(x => 0).ThenByDescending(x => x, new ExtremeComparer<int>()).ToList(), x => { Assert.InRange(x, 0, prev); prev = x; });
        }

        // Recursive sort with nested ThenBy...s
        // Due to the use of randomized input, cycles will not start with a known input (and may skip values, etc).

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_ThenBy(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(0, KeyValuePair.Create(0, 0));
            int seen = 0;
            foreach (var pOuter in labeled.Item.Select(x => KeyValuePair.Create(x % GroupFactor, KeyValuePair.Create((KeyFactor - 1) - x % KeyFactor, x)))
                .OrderBy(o => o.Key).ThenBy(o => o.Value.Key).ThenBy(o => o.Value.Value))
            {
                AssertLessThanOrEqual(prev, pOuter, count - 1);
                prev = pOuter;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_ThenBy_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_ThenBy(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_ThenByDescending(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(GroupFactor - 1, KeyValuePair.Create(KeyFactor - 1, count - 1));
            int seen = 0;
            foreach (var pOuter in labeled.Item.Select(x => KeyValuePair.Create(x % GroupFactor, KeyValuePair.Create((KeyFactor - 1) - x % KeyFactor, x)))
                .OrderByDescending(o => o.Key).ThenByDescending(o => o.Value.Key).ThenByDescending(o => o.Value.Value))
            {
                AssertGreaterOrEqual(prev, pOuter);
                prev = pOuter;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_ThenByDescending_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_ThenByDescending(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_ThenBy_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(0, KeyValuePair.Create(0, 0));
            int seen = 0;
            Assert.All(labeled.Item.Select(x => KeyValuePair.Create(x % GroupFactor, KeyValuePair.Create((KeyFactor - 1) - x % KeyFactor, x)))
                .OrderBy(o => o.Key).ThenBy(o => o.Value.Key).ThenBy(o => o.Value.Value).ToList(),
                pOuter =>
                {
                    AssertLessThanOrEqual(prev, pOuter, count - 1);
                    prev = pOuter;
                    seen++;
                });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_ThenBy_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_ThenBy_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_ThenByDescending_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(GroupFactor - 1, KeyValuePair.Create(KeyFactor - 1, count - 1));
            int seen = 0;
            Assert.All(labeled.Item.Select(x => KeyValuePair.Create(x % GroupFactor, KeyValuePair.Create((KeyFactor - 1) - x % KeyFactor, x)))
                .OrderByDescending(o => o.Key).ThenByDescending(o => o.Value.Key).ThenByDescending(o => o.Value.Value).ToList(),
                pOuter =>
                {
                    AssertGreaterOrEqual(prev, pOuter);
                    prev = pOuter;
                    seen++;
                });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { LongRunningCount })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_ThenByDescending_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_ThenByDescending_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_NotComparable(Labeled<ParallelQuery<int>> labeled, int count)
        {
            _ = count;
            ParallelQuery<int> query = labeled.Item.OrderBy(x => 0).ThenBy(x => new NotComparable(x));
            AssertThrows.Wrapped<ArgumentException>(() => { foreach (int i in query) ; });
            AssertThrows.Wrapped<ArgumentException>(() => query.ToList());
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_NotComparable_Comparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            var comparer = Comparer<NotComparable>.Create((x, y) => ReverseComparer.Instance.Compare(x.Value, y.Value));
            foreach (int i in labeled.Item.OrderBy(x => 0).ThenBy(x => new NotComparable(-x), comparer))
            {
                Assert.InRange(i, prev, count - 1);
                prev = i;
            }
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void ThenBy_NotPipelined_NotComparable_Comparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = 0;
            var comparer = Comparer<NotComparable>.Create((x, y) => ReverseComparer.Instance.Compare(x.Value, y.Value));
            Assert.All(labeled.Item.OrderBy(x => 0).ThenBy(x => new NotComparable(-x), comparer).ToList(), x => { Assert.InRange(x, prev, count - 1); prev = x; });
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 2 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 2 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 2 }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_NotComparable(Labeled<ParallelQuery<int>> labeled, int count)
        {
            _ = count;
            ParallelQuery<int> query = labeled.Item.OrderBy(x => 0).ThenByDescending(x => new NotComparable(x));
            AssertThrows.Wrapped<ArgumentException>(() => { foreach (int i in query) ; });
            AssertThrows.Wrapped<ArgumentException>(() => query.ToList());
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_NotComparable_Comparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            var comparer = Comparer<NotComparable>.Create((x, y) => ReverseComparer.Instance.Compare(x.Value, y.Value));
            foreach (int i in labeled.Item.OrderBy(x => 0).ThenByDescending(x => new NotComparable(-x), comparer))
            {
                Assert.InRange(i, 0, prev);
                prev = i;
            }
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(OrderByRandomData), new[] { 0, 1, 2, 16 })]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_NotPipelined_NotComparable_Comparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = count - 1;
            var comparer = Comparer<NotComparable>.Create((x, y) => ReverseComparer.Instance.Compare(x.Value, y.Value));
            Assert.All(labeled.Item.OrderBy(x => 0).ThenByDescending(x => new NotComparable(-x), comparer).ToList(), x => { Assert.InRange(x, 0, prev); prev = x; });
        }

        [Fact]
        public static void ThenBy_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((OrderedParallelQuery<int>)null).ThenBy(x => x));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).OrderBy(x => 0).ThenBy((Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((OrderedParallelQuery<int>)null).ThenBy(x => x, Comparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).OrderBy(x => 0).ThenBy((Func<int, int>)null, Comparer<int>.Default));
        }

        [Fact]
        public static void ThenByDescending_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((OrderedParallelQuery<int>)null).ThenByDescending(x => x));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).OrderBy(x => 0).ThenByDescending((Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((OrderedParallelQuery<int>)null).ThenByDescending(x => x, Comparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).OrderBy(x => 0).ThenByDescending((Func<int, int>)null, Comparer<int>.Default));
        }

        //
        // Stable Sort
        // Ensures that indices issued during a query are stable, **not** that OrderBy returns a stable result on its own (it does not).
        //
        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void StableSort(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(0, KeyValuePair.Create(0, 0));
            int seen = 0;
            foreach (var pOuter in labeled.Item.Select((x, index) => KeyValuePair.Create(index, KeyValuePair.Create(x, (count - x) / GroupFactor)))
                .OrderBy(p => p.Value.Value).ThenBy(p => p.Value.Key))
            {
                Assert.InRange(pOuter.Value.Value, prev.Value.Value, count / GroupFactor);
                if (pOuter.Value.Value == prev.Value.Value)
                {
                    Assert.InRange(pOuter.Key, prev.Key, count - 1);
                }
                prev = pOuter;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void StableSort_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            StableSort(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void StableSort_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(0, KeyValuePair.Create(0, 0));
            int seen = 0;
            Assert.All(labeled.Item.Select((x, index) => KeyValuePair.Create(index, KeyValuePair.Create(x, (count - x) / GroupFactor)))
                .OrderBy(p => p.Value.Value).ThenBy(p => p.Value.Key).ToList(),
                pOuter =>
                {
                    Assert.InRange(pOuter.Value.Value, prev.Value.Value, count / GroupFactor);
                    if (pOuter.Value.Value == prev.Value.Value)
                    {
                        Assert.InRange(pOuter.Key, prev.Key, count - 1);
                    }
                    prev = pOuter;
                    seen++;
                });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void StableSort_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            StableSort_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void StableSort_Descending(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(count, KeyValuePair.Create(count, count / GroupFactor));
            int seen = 0;
            foreach (var pOuter in labeled.Item.Select((x, index) => KeyValuePair.Create(index, KeyValuePair.Create(x, (count - x) / GroupFactor)))
                .OrderByDescending(p => p.Value.Value).ThenByDescending(p => p.Value.Key))
            {
                Assert.InRange(pOuter.Value.Value, 0, prev.Value.Value);
                if (pOuter.Value.Value == prev.Value.Value)
                {
                    Assert.InRange(pOuter.Key, 0, prev.Key);
                }

                prev = pOuter;
                seen++;
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void StableSort_Descending_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            StableSort_Descending(labeled, count);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(UnorderedSources))]
        public static void StableSort_Descending_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(count, KeyValuePair.Create(count, count / GroupFactor));
            int seen = 0;
            Assert.All(labeled.Item.Select((x, index) => KeyValuePair.Create(index, KeyValuePair.Create(x, (count - x) / GroupFactor)))
                .OrderByDescending(p => p.Value.Value).ThenByDescending(p => p.Value.Key).ToList(),
                pOuter =>
                {
                    Assert.InRange(pOuter.Value.Value, 0, prev.Value.Value);
                    if (pOuter.Value.Value == prev.Value.Value)
                    {
                        Assert.InRange(pOuter.Key, 0, prev.Key);
                    }
                    prev = pOuter;
                    seen++;
                });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { LongRunningCount }, MemberType = typeof(Sources))]
        [MemberData(nameof(UnorderedSources.Ranges), new[] { LongRunningCount }, MemberType = typeof(UnorderedSources))]
        public static void StableSort_Descending_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            StableSort_Descending_NotPipelined(labeled, count);
        }

        private static void AssertGreaterOrEqual(KeyValuePair<int, KeyValuePair<int, int>> greater, KeyValuePair<int, KeyValuePair<int, int>> actual)
        {
            Assert.InRange(actual.Key, 0, greater.Key);
            if (greater.Key == actual.Key)
            {
                Assert.InRange(actual.Value.Key, 0, greater.Value.Key);
                if (greater.Value.Key == actual.Value.Key)
                {
                    Assert.InRange(actual.Value.Value, 0, greater.Value.Value);
                }
            }
        }

        private static void AssertLessThanOrEqual(KeyValuePair<int, KeyValuePair<int, int>> lesser, KeyValuePair<int, KeyValuePair<int, int>> actual, int limit)
        {
            Assert.InRange(actual.Key, lesser.Key, GroupFactor - 1);
            if (lesser.Key == actual.Key)
            {
                Assert.InRange(actual.Value.Key, lesser.Value.Key, KeyFactor - 1);
                if (lesser.Value.Key == actual.Value.Key)
                {
                    Assert.InRange(actual.Value.Value, lesser.Value.Value, limit);
                }
            }
        }
    }
}
