// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace Test
{
    public class OrderByThenByTests
    {
        private const int KeyFactor = 4;
        private const int GroupFactor = 8;

        public static IEnumerable<object[]> OrderByRandomData(object[] counts)
        {
            foreach (int count in counts.Cast<int>())
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

        //
        // OrderBy
        //
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OrderBy(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = int.MinValue;
            foreach (int i in labeled.Item.OrderBy(x => x))
            {
                Assert.True(i >= prev);
                prev = i;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void OrderBy_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderBy(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OrderBy_Reversed(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = int.MaxValue;
            foreach (int i in labeled.Item.OrderBy(x => -x))
            {
                Assert.True(i <= prev);
                prev = i;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void OrderBy_Reversed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderBy_Reversed(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = int.MaxValue;
            foreach (int i in labeled.Item.OrderByDescending(x => x))
            {
                Assert.True(i <= prev);
                prev = i;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderByDescending(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_Reversed(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = int.MinValue;
            foreach (int i in labeled.Item.OrderByDescending(x => -x))
            {
                Assert.True(i >= prev);
                prev = i;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_Reversed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderByDescending_Reversed(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OrderBy_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = int.MinValue;
            Assert.All(labeled.Item.OrderBy(x => x).ToList(), x => { Assert.True(x >= prev); prev = x; });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void OrderBy_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderBy_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OrderBy_Reversed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = int.MaxValue;
            Assert.All(labeled.Item.OrderBy(x => -x).ToList(), x => { Assert.True(x <= prev); prev = x; });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void OrderBy_Reversed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderBy_Reversed_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = int.MaxValue;
            Assert.All(labeled.Item.OrderByDescending(x => x).ToList(), x => { Assert.True(x <= prev); prev = x; });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderByDescending_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_Reversed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = int.MinValue;
            Assert.All(labeled.Item.OrderByDescending(x => -x).ToList(), x => { Assert.True(x >= prev); prev = x; });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_Reversed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderByDescending_Reversed_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OrderBy_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = int.MaxValue;
            foreach (int i in labeled.Item.OrderBy(x => x, ReverseComparer.Instance))
            {
                Assert.True(i <= prev);
                prev = i;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void OrderBy_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderBy_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = int.MinValue;
            foreach (int i in labeled.Item.OrderByDescending(x => x, ReverseComparer.Instance))
            {
                Assert.True(i >= prev);
                prev = i;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderByDescending_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OrderBy_NotPipelined_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = int.MaxValue;
            Assert.All(labeled.Item.OrderBy(x => x, ReverseComparer.Instance).ToList(), x => { Assert.True(x <= prev); prev = x; });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void OrderBy_NotPipelined_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderBy_NotPipelined_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_NotPipelined_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prev = int.MinValue;
            Assert.All(labeled.Item.OrderByDescending(x => x, ReverseComparer.Instance).ToList(), x => { Assert.True(x >= prev); prev = x; });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void OrderByDescending_NotPipelined_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            OrderByDescending_NotPipelined_CustomComparer(labeled, count);
        }

        [Fact]
        public static void OrderBy_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).OrderBy(x => x));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).OrderBy((Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).OrderBy(x => x, Comparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).OrderBy((Func<int, int>)null, Comparer<int>.Default));
        }

        [Fact]
        public static void OrderByDescending_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).OrderByDescending(x => x));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).OrderByDescending((Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).OrderByDescending(x => x, Comparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).OrderByDescending((Func<int, int>)null, Comparer<int>.Default));
        }

        //
        // Thenby
        //
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = 0;
            int prevSecondary = int.MaxValue;
            foreach (int i in labeled.Item.OrderBy(x => x % GroupFactor).ThenBy(x => -x))
            {
                Assert.True(i % GroupFactor >= prevPrimary);
                if (i % GroupFactor != prevPrimary)
                {
                    prevPrimary = i % GroupFactor;
                    prevSecondary = int.MaxValue;
                }
                Assert.True(i <= prevSecondary);
                prevSecondary = i;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_Reversed(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = GroupFactor - 1;
            int prevSecondary = 0;
            foreach (int i in labeled.Item.OrderBy(x => -x % GroupFactor).ThenBy(x => x))
            {
                Assert.True(i % GroupFactor <= prevPrimary);
                if (i % GroupFactor != prevPrimary)
                {
                    prevPrimary = i % GroupFactor;
                    prevSecondary = 0;
                }
                Assert.True(i >= prevSecondary);
                prevSecondary = i;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_Reversed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_Reversed(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = GroupFactor - 1;
            int prevSecondary = 0;
            foreach (int i in labeled.Item.OrderByDescending(x => x % GroupFactor).ThenByDescending(x => -x))
            {
                Assert.True(i % GroupFactor <= prevPrimary);
                if (i % GroupFactor != prevPrimary)
                {
                    prevPrimary = i % GroupFactor;
                    prevSecondary = 0;
                }
                Assert.True(i >= prevSecondary);
                prevSecondary = i;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_Reversed(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = 0;
            int prevSecondary = int.MaxValue;
            foreach (int i in labeled.Item.OrderByDescending(x => -x % GroupFactor).ThenByDescending(x => x))
            {
                Assert.True(i % GroupFactor >= prevPrimary);
                if (i % GroupFactor != prevPrimary)
                {
                    prevPrimary = i % GroupFactor;
                    prevSecondary = int.MaxValue;
                }
                Assert.True(i <= prevSecondary);
                prevSecondary = i;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_Reversed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_Reversed(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = 0;
            int prevSecondary = int.MaxValue;
            Assert.All(labeled.Item.OrderBy(x => x % GroupFactor).ThenBy(x => -x).ToList(),
                x =>
                {
                    Assert.True(x % GroupFactor >= prevPrimary);
                    if (x % GroupFactor != prevPrimary)
                    {
                        prevPrimary = x % GroupFactor;
                        prevSecondary = int.MaxValue;
                    }
                    Assert.True(x <= prevSecondary);
                    prevSecondary = x;
                });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_Reversed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = GroupFactor - 1;
            int prevSecondary = 0;
            Assert.All(labeled.Item.OrderBy(x => -x % GroupFactor).ThenBy(x => x).ToList(),
                x =>
                {
                    Assert.True(x % GroupFactor <= prevPrimary);
                    if (x % GroupFactor != prevPrimary)
                    {
                        prevPrimary = x % GroupFactor;
                        prevSecondary = 0;
                    }
                    Assert.True(x >= prevSecondary);
                    prevSecondary = x;
                });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_Reversed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_Reversed_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = GroupFactor - 1;
            int prevSecondary = 0;
            Assert.All(labeled.Item.OrderByDescending(x => x % GroupFactor).ThenByDescending(x => -x).ToList(),
                x =>
                {
                    Assert.True(x % GroupFactor <= prevPrimary);
                    if (x % GroupFactor != prevPrimary)
                    {
                        prevPrimary = x % GroupFactor;
                        prevSecondary = 0;
                    }
                    Assert.True(x >= prevSecondary);
                    prevSecondary = x;
                });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_Reversed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = 0;
            int prevSecondary = int.MaxValue;
            Assert.All(labeled.Item.OrderByDescending(x => -x % GroupFactor).ThenByDescending(x => x).ToList(),
                x =>
                {
                    Assert.True(x % GroupFactor >= prevPrimary);
                    if (x % GroupFactor != prevPrimary)
                    {
                        prevPrimary = x % GroupFactor;
                        prevSecondary = int.MaxValue;
                    }
                    Assert.True(x <= prevSecondary);
                    prevSecondary = x;
                });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_Reversed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_Reversed_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = 0;
            int prevSecondary = int.MaxValue;
            foreach (int i in labeled.Item.OrderBy(x => x % GroupFactor).ThenBy(x => x, ReverseComparer.Instance))
            {
                Assert.True(i % GroupFactor >= prevPrimary);
                if (i % GroupFactor != prevPrimary)
                {
                    prevPrimary = i % GroupFactor;
                    prevSecondary = int.MaxValue;
                }
                Assert.True(i <= prevSecondary);
                prevSecondary = i;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = GroupFactor - 1;
            int prevSecondary = 0;
            foreach (int i in labeled.Item.OrderByDescending(x => x % GroupFactor).ThenByDescending(x => x, ReverseComparer.Instance))
            {
                Assert.True(i % GroupFactor <= prevPrimary);
                if (i % GroupFactor != prevPrimary)
                {
                    prevPrimary = i % GroupFactor;
                    prevSecondary = 0;
                }
                Assert.True(i >= prevSecondary);
                prevSecondary = i;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_NotPipelined_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = 0;
            int prevSecondary = int.MaxValue;
            Assert.All(labeled.Item.OrderBy(x => x % GroupFactor).ThenBy(x => x, ReverseComparer.Instance).ToList(),
                x =>
                {
                    Assert.True(x % GroupFactor >= prevPrimary);
                    if (x % GroupFactor != prevPrimary)
                    {
                        prevPrimary = x % GroupFactor;
                        prevSecondary = int.MaxValue;
                    }
                    Assert.True(x <= prevSecondary);
                    prevSecondary = x;
                });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_NotPipelined_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_NotPipelined_CustomComparer(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_NotPipelined_CustomComparer(Labeled<ParallelQuery<int>> labeled, int count)
        {
            int prevPrimary = GroupFactor - 1;
            int prevSecondary = 0;
            Assert.All(labeled.Item.OrderByDescending(x => x % GroupFactor).ThenByDescending(x => x, ReverseComparer.Instance).ToList(),
                x =>
                {
                    Assert.True(x % GroupFactor <= prevPrimary);
                    if (x % GroupFactor != prevPrimary)
                    {
                        prevPrimary = x % GroupFactor;
                        prevSecondary = 0;
                    }
                    Assert.True(x >= prevSecondary);
                    prevSecondary = x;
                });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_NotPipelined_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_NotPipelined_CustomComparer(labeled, count);
        }

        // Recursive sort with nested ThenBy...s
        // Due to the use of randomized input, cycles will not start with a known input (and may skip values, etc).

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_ThenBy(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(0, KeyValuePair.Create(0, 0));
            foreach (var pOuter in labeled.Item.Select(x => KeyValuePair.Create(x % GroupFactor, KeyValuePair.Create((KeyFactor - 1) - x % KeyFactor, x)))
                .OrderBy(o => o.Key).ThenBy(o => o.Value.Key).ThenBy(o => o.Value.Value))
            {
                Assert.True(pOuter.Key >= prev.Key);
                Assert.True(pOuter.Value.Key >= prev.Value.Key || pOuter.Key > prev.Key);
                Assert.True(pOuter.Value.Value >= prev.Value.Value || pOuter.Value.Key > prev.Value.Key || pOuter.Key > prev.Key);
                prev = pOuter;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_ThenBy_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_ThenBy(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_ThenByDescending(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(GroupFactor - 1, KeyValuePair.Create(KeyFactor - 1, int.MaxValue));
            foreach (var pOuter in labeled.Item.Select(x => KeyValuePair.Create(x % GroupFactor, KeyValuePair.Create((KeyFactor - 1) - x % KeyFactor, x)))
                .OrderByDescending(o => o.Key).ThenByDescending(o => o.Value.Key).ThenByDescending(o => o.Value.Value))
            {
                Assert.True(pOuter.Key <= prev.Key);
                Assert.True(pOuter.Value.Key <= prev.Value.Key || pOuter.Key < prev.Key);
                Assert.True(pOuter.Value.Value <= prev.Value.Value || pOuter.Value.Key < prev.Value.Key || pOuter.Key < prev.Key);
                prev = pOuter;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_ThenByDescending_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_ThenByDescending(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_ThenBy_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(0, KeyValuePair.Create(0, 0));
            Assert.All(labeled.Item.Select(x => KeyValuePair.Create(x % GroupFactor, KeyValuePair.Create((KeyFactor - 1) - x % KeyFactor, x)))
                .OrderBy(o => o.Key).ThenBy(o => o.Value.Key).ThenBy(o => o.Value.Value).ToList(),
                pOuter =>
                {
                    Assert.True(pOuter.Key >= prev.Key);
                    Assert.True(pOuter.Value.Key >= prev.Value.Key || pOuter.Key > prev.Key);
                    Assert.True(pOuter.Value.Value >= prev.Value.Value || pOuter.Value.Key > prev.Value.Key || pOuter.Key > prev.Key);
                    prev = pOuter;
                });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenBy_ThenBy_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenBy_ThenBy_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 0, 1, 2, 16 }))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_ThenByDescending_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(GroupFactor - 1, KeyValuePair.Create(KeyFactor - 1, int.MaxValue));
            Assert.All(labeled.Item.Select(x => KeyValuePair.Create(x % GroupFactor, KeyValuePair.Create((KeyFactor - 1) - x % KeyFactor, x)))
                .OrderByDescending(o => o.Key).ThenByDescending(o => o.Value.Key).ThenByDescending(o => o.Value.Value).ToList(),
                pOuter =>
                {
                    Assert.True(pOuter.Key <= prev.Key);
                    Assert.True(pOuter.Value.Key <= prev.Value.Key || pOuter.Key < prev.Key);
                    Assert.True(pOuter.Value.Value <= prev.Value.Value || pOuter.Value.Key < prev.Value.Key || pOuter.Key < prev.Key);
                    prev = pOuter;
                });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("OrderByRandomData", (object)(new[] { 1024, 1024 * 32 }))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void ThenByDescending_ThenByDescending_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ThenByDescending_ThenByDescending_NotPipelined(labeled, count);
        }

        [Fact]
        public static void ThenBy_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((OrderedParallelQuery<int>)null).ThenBy(x => x));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).OrderBy(x => 0).ThenBy((Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((OrderedParallelQuery<int>)null).ThenBy(x => x, Comparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).OrderBy(x => 0).ThenBy((Func<int, int>)null, Comparer<int>.Default));
        }

        [Fact]
        public static void ThenByDescending_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((OrderedParallelQuery<int>)null).ThenByDescending(x => x));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).OrderBy(x => 0).ThenByDescending((Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((OrderedParallelQuery<int>)null).ThenByDescending(x => x, Comparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).OrderBy(x => 0).ThenByDescending((Func<int, int>)null, Comparer<int>.Default));
        }

        //
        // Stable Sort
        //
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void StableSort(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(0, KeyValuePair.Create(0, 0));
            foreach (var pOuter in labeled.Item.Select((x, index) => KeyValuePair.Create(index, KeyValuePair.Create(x, (count - x) / Math.Max(count / GroupFactor, 1))))
                .OrderBy(p => p.Value.Value).ThenBy(p => p.Value.Key))
            {
                Assert.False(pOuter.Value.Value < prev.Value.Value);
                Assert.False(pOuter.Value.Value == prev.Value.Value && pOuter.Key < prev.Key, "" + prev + "_" + pOuter);
                prev = pOuter;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void StableSort_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            StableSort(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void StableSort_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(0, KeyValuePair.Create(0, 0));
            Assert.All(labeled.Item.Select((x, index) => KeyValuePair.Create(index, KeyValuePair.Create(x, (count - x) / Math.Max(count / GroupFactor, 1))))
                .OrderBy(p => p.Value.Value).ThenBy(p => p.Value.Key).ToList(),
                pOuter =>
                {
                    Assert.False(pOuter.Value.Value < prev.Value.Value);
                    Assert.False(pOuter.Value.Value == prev.Value.Value && pOuter.Key < prev.Key, "" + prev + "_" + pOuter);
                    prev = pOuter;
                });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void StableSort_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            StableSort_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void StableSort_Descending(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(int.MaxValue, KeyValuePair.Create(int.MaxValue, int.MaxValue));
            foreach (var pOuter in labeled.Item.Select((x, index) => KeyValuePair.Create(index, KeyValuePair.Create(x, (count - x) / Math.Max(count / GroupFactor, 1))))
                .OrderByDescending(p => p.Value.Value).ThenByDescending(p => p.Value.Key))
            {
                Assert.False(pOuter.Value.Value > prev.Value.Value);
                Assert.False(pOuter.Value.Value == prev.Value.Value && pOuter.Key > prev.Key, "" + prev + "_" + pOuter);
                prev = pOuter;
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void StableSort_Descending_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            StableSort_Descending(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void StableSort_Descending_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            var prev = KeyValuePair.Create(int.MaxValue, KeyValuePair.Create(int.MaxValue, int.MaxValue));
            Assert.All(labeled.Item.Select((x, index) => KeyValuePair.Create(index, KeyValuePair.Create(x, (count - x) / Math.Max(count / GroupFactor, 1))))
                .OrderByDescending(p => p.Value.Value).ThenByDescending(p => p.Value.Key).ToList(),
                pOuter =>
                {
                    Assert.False(pOuter.Value.Value > prev.Value.Value);
                    Assert.False(pOuter.Value.Value == prev.Value.Value && pOuter.Key > prev.Key, "" + prev + "_" + pOuter);
                    prev = pOuter;
                });
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(Sources))]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 32 }), MemberType = typeof(UnorderedSources))]
        public static void StableSort_Descending_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            StableSort_Descending_NotPipelined(labeled, count);
        }
    }
}
