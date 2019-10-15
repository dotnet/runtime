// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class GroupByTests
    {
        private const int GroupFactor = 8;

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(GroupFactor - 1)]
        [InlineData(GroupFactor)]
        [InlineData(GroupFactor * 2 - 1)]
        [InlineData(GroupFactor * 2)]
        public static void GroupBy_Unordered(int count)
        {
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (IGrouping<int, int> group in UnorderedSources.Default(count).GroupBy(x => x % GroupFactor))
            {
                groupsSeen.Add(group.Key);

                IntegerRangeSet elementsSeen = new IntegerRangeSet(0, 1 + (count - (group.Key + 1)) / GroupFactor);
                foreach (int i in group)
                {
                    Assert.Equal(group.Key, i % GroupFactor);
                    elementsSeen.Add(i / GroupFactor);
                }
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void GroupBy_Unordered_Longrunning()
        {
            GroupBy_Unordered(Sources.OuterLoopCount / 4);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }, MemberType = typeof(Sources))]
        public static void GroupBy(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int groupsSeen = 0;
            foreach (IGrouping<int, int> group in query.GroupBy(x => x % GroupFactor))
            {
                Assert.Equal(groupsSeen++, group.Key);

                int elementsSeen = group.Key;
                foreach (int i in group)
                {
                    Assert.Equal(elementsSeen, i);
                    elementsSeen += GroupFactor;
                }
                Assert.Equal(group.Key + (1 + (count - (group.Key + 1)) / GroupFactor) * GroupFactor, elementsSeen);
            }
            Assert.Equal(Math.Min(count, GroupFactor), groupsSeen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 64 }, MemberType = typeof(Sources))]
        public static void GroupBy_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy(labeled, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(GroupFactor - 1)]
        [InlineData(GroupFactor)]
        [InlineData(GroupFactor * 2 - 1)]
        [InlineData(GroupFactor * 2)]
        public static void GroupBy_Unordered_NotPipelined(int count)
        {
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (IGrouping<int, int> group in UnorderedSources.Default(count).GroupBy(x => x % GroupFactor).ToList())
            {
                groupsSeen.Add(group.Key);

                IntegerRangeSet elementsSeen = new IntegerRangeSet(0, 1 + (count - (group.Key + 1)) / GroupFactor);
                Assert.All(group, x => { Assert.Equal(group.Key, x % GroupFactor); elementsSeen.Add(x / GroupFactor); });
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void GroupBy_Unordered_NotPipelined_Longrunning()
        {
            GroupBy_Unordered_NotPipelined(Sources.OuterLoopCount / 4);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }, MemberType = typeof(Sources))]
        public static void GroupBy_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int groupsSeen = 0;
            foreach (IGrouping<int, int> group in query.GroupBy(x => x % GroupFactor).ToList())
            {
                Assert.Equal(groupsSeen++, group.Key);

                int elementsSeen = group.Key;
                Assert.All(group, x => { Assert.Equal(elementsSeen, x); elementsSeen += GroupFactor; });
                Assert.Equal(group.Key + (1 + (count - (group.Key + 1)) / GroupFactor) * GroupFactor, elementsSeen);
            }
            Assert.Equal(Math.Min(count, GroupFactor), groupsSeen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 64 }, MemberType = typeof(Sources))]
        public static void GroupBy_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_NotPipelined(labeled, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(GroupFactor - 1)]
        [InlineData(GroupFactor)]
        [InlineData(GroupFactor * 2 - 1)]
        [InlineData(GroupFactor * 2)]
        public static void GroupBy_Unordered_CustomComparator(int count)
        {
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (IGrouping<int, int> group in UnorderedSources.Default(count).GroupBy(x => x, new ModularCongruenceComparer(GroupFactor)))
            {
                groupsSeen.Add(group.Key % GroupFactor);

                IntegerRangeSet elementsSeen = new IntegerRangeSet(0, 1 + (count - (group.Key % GroupFactor + 1)) / GroupFactor);
                foreach (int i in group)
                {
                    Assert.Equal(group.Key % GroupFactor, i % GroupFactor);
                    elementsSeen.Add(i / GroupFactor);
                }
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void GroupBy_Unordered_CustomComparator_Longrunning()
        {
            GroupBy_Unordered_CustomComparator(Sources.OuterLoopCount / 4);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }, MemberType = typeof(Sources))]
        // GroupBy doesn't select the first 'identical' key.  Issue #1490
        public static void GroupBy_CustomComparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int groupsSeen = 0;
            foreach (IGrouping<int, int> group in query.GroupBy(x => x, new ModularCongruenceComparer(GroupFactor)))
            {
                int elementsSeen = groupsSeen;
                Assert.Equal(groupsSeen++, group.Key % GroupFactor);

                foreach (int i in group)
                {
                    Assert.Equal(elementsSeen, i);
                    elementsSeen += GroupFactor;
                }
                Assert.Equal(group.Key + (1 + (count - (group.Key + 1)) / GroupFactor) * GroupFactor, elementsSeen);
            }
            Assert.Equal(Math.Min(count, GroupFactor), groupsSeen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 64 }, MemberType = typeof(Sources))]
        public static void GroupBy_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_CustomComparator(labeled, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(GroupFactor - 1)]
        [InlineData(GroupFactor)]
        [InlineData(GroupFactor * 2 - 1)]
        [InlineData(GroupFactor * 2)]
        public static void GroupBy_Unordered_ElementSelector(int count)
        {
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (IGrouping<int, int> group in UnorderedSources.Default(count).GroupBy(x => x % GroupFactor, x => -x))
            {
                groupsSeen.Add(group.Key);

                int expected = 1 + (count - (group.Key + 1)) / GroupFactor;
                IntegerRangeSet elementsSeen = new IntegerRangeSet(1 - expected, expected);
                Assert.All(group, x => { Assert.Equal(group.Key, -x % GroupFactor); elementsSeen.Add(x / GroupFactor); });
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void GroupBy_Unordered_ElementSelector_Longrunning()
        {
            GroupBy_Unordered_ElementSelector(Sources.OuterLoopCount / 4);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }, MemberType = typeof(Sources))]
        public static void GroupBy_ElementSelector(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int groupsSeen = 0;
            foreach (IGrouping<int, int> group in query.GroupBy(x => x % GroupFactor, x => -x))
            {
                Assert.Equal(groupsSeen++, group.Key);

                int elementsSeen = -group.Key;
                Assert.All(group, x => { Assert.Equal(elementsSeen, x); elementsSeen -= GroupFactor; });
                Assert.Equal(-group.Key - (1 + (count - (group.Key + 1)) / GroupFactor) * GroupFactor, elementsSeen);
            }
            Assert.Equal(Math.Min(count, GroupFactor), groupsSeen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 64 }, MemberType = typeof(Sources))]
        public static void GroupBy_ElementSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_ElementSelector(labeled, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(GroupFactor - 1)]
        [InlineData(GroupFactor)]
        [InlineData(GroupFactor * 2 - 1)]
        [InlineData(GroupFactor * 2)]
        public static void GroupBy_Unordered_ElementSelector_NotPipelined(int count)
        {
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (IGrouping<int, int> group in UnorderedSources.Default(count).GroupBy(x => x % GroupFactor, y => -y).ToList())
            {
                groupsSeen.Add(group.Key);

                int expected = 1 + (count - (group.Key + 1)) / GroupFactor;
                IntegerRangeSet elementsSeen = new IntegerRangeSet(1 - expected, expected);
                Assert.All(group, x => { Assert.Equal(group.Key, -x % GroupFactor); elementsSeen.Add(x / GroupFactor); });
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void GroupBy_Unordered_ElementSelector_NotPipelined_Longrunning()
        {
            GroupBy_Unordered_ElementSelector_NotPipelined(Sources.OuterLoopCount / 4);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }, MemberType = typeof(Sources))]
        public static void GroupBy_ElementSelector_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int groupsSeen = 0;
            foreach (IGrouping<int, int> group in query.GroupBy(x => x % GroupFactor, y => -y).ToList())
            {
                Assert.Equal(groupsSeen++, group.Key);

                int elementsSeen = -group.Key;
                Assert.All(group, x => { Assert.Equal(elementsSeen, x); elementsSeen -= GroupFactor; });
                Assert.Equal(-group.Key - (1 + (count - (group.Key + 1)) / GroupFactor) * GroupFactor, elementsSeen);
            }
            Assert.Equal(Math.Min(count, GroupFactor), groupsSeen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 64 }, MemberType = typeof(Sources))]
        public static void GroupBy_ElementSelector_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_ElementSelector_NotPipelined(labeled, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(GroupFactor - 1)]
        [InlineData(GroupFactor)]
        [InlineData(GroupFactor * 2 - 1)]
        [InlineData(GroupFactor * 2)]
        public static void GroupBy_Unordered_ResultSelector(int count)
        {
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (var group in UnorderedSources.Default(count).GroupBy(x => x % GroupFactor, (key, elements) => KeyValuePair.Create(key, elements)))
            {
                groupsSeen.Add(group.Key);
                IntegerRangeSet elementsSeen = new IntegerRangeSet(0, 1 + (count - (group.Key + 1)) / GroupFactor);
                Assert.All(group.Value, x => { Assert.Equal(group.Key, x % GroupFactor); elementsSeen.Add(x / GroupFactor); });
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void GroupBy_Unordered_ResultSelector_Longrunning()
        {
            GroupBy_Unordered_ResultSelector(Sources.OuterLoopCount / 4);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }, MemberType = typeof(Sources))]
        public static void GroupBy_ResultSelector(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int groupsSeen = 0;
            foreach (var group in query.GroupBy(x => x % GroupFactor, (key, elements) => KeyValuePair.Create(key, elements)))
            {
                Assert.Equal(groupsSeen++, group.Key);
                int elementsSeen = group.Key;
                Assert.All(group.Value, x => { Assert.Equal(elementsSeen, x); elementsSeen += GroupFactor; });
                Assert.Equal(group.Key + (1 + (count - (group.Key + 1)) / GroupFactor) * GroupFactor, elementsSeen);
            }
            Assert.Equal(Math.Min(count, GroupFactor), groupsSeen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 64 }, MemberType = typeof(Sources))]
        public static void GroupBy_ResultSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_ResultSelector(labeled, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(GroupFactor - 1)]
        [InlineData(GroupFactor)]
        [InlineData(GroupFactor * 2 - 1)]
        [InlineData(GroupFactor * 2)]
        public static void GroupBy_Unordered_ResultSelector_CustomComparator(int count)
        {
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (var group in UnorderedSources.Default(count).GroupBy(x => x, (key, elements) => KeyValuePair.Create(key, elements), new ModularCongruenceComparer(GroupFactor)))
            {
                groupsSeen.Add(group.Key % GroupFactor);
                IntegerRangeSet elementsSeen = new IntegerRangeSet(0, 1 + (count - (group.Key % GroupFactor + 1)) / GroupFactor);
                Assert.All(group.Value, x => { Assert.Equal(group.Key % GroupFactor, x % GroupFactor); elementsSeen.Add(x / GroupFactor); });
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void GroupBy_Unordered_ResultSelector_CustomComparator_Longrunning()
        {
            GroupBy_Unordered_ResultSelector_CustomComparator(Sources.OuterLoopCount / 4);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }, MemberType = typeof(Sources))]
        public static void GroupBy_ResultSelector_CustomComparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int groupsSeen = 0;
            foreach (var group in query.GroupBy(x => x, (key, elements) => KeyValuePair.Create(key, elements), new ModularCongruenceComparer(GroupFactor)))
            {
                int elementsSeen = groupsSeen;
                Assert.Equal(groupsSeen++, group.Key % GroupFactor);
                Assert.All(group.Value, x => { Assert.Equal(elementsSeen, x); elementsSeen += GroupFactor; });
                Assert.Equal(group.Key + (1 + (count - (group.Key + 1)) / GroupFactor) * GroupFactor, elementsSeen);
            }
            Assert.Equal(Math.Min(count, GroupFactor), groupsSeen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 64 }, MemberType = typeof(Sources))]
        public static void GroupBy_ResultSelector_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_ResultSelector_CustomComparator(labeled, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(GroupFactor - 1)]
        [InlineData(GroupFactor)]
        [InlineData(GroupFactor * 2 - 1)]
        [InlineData(GroupFactor * 2)]
        public static void GroupBy_Unordered_ElementSelector_ResultSelector(int count)
        {
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (var group in UnorderedSources.Default(count).GroupBy(x => x % GroupFactor, x => -x, (key, elements) => KeyValuePair.Create(key, elements)))
            {
                groupsSeen.Add(group.Key);
                int expected = 1 + (count - (group.Key + 1)) / GroupFactor;
                IntegerRangeSet elementsSeen = new IntegerRangeSet(1 - expected, expected);
                Assert.All(group.Value, x => { Assert.Equal(group.Key, -x % GroupFactor); elementsSeen.Add(x / GroupFactor); });
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void GroupBy_Unordered_ElementSelector_ResultSelector_Longrunning()
        {
            GroupBy_Unordered_ElementSelector_ResultSelector(Sources.OuterLoopCount / 4);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }, MemberType = typeof(Sources))]
        public static void GroupBy_ElementSelector_ResultSelector(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int groupsSeen = 0;
            foreach (var group in query.GroupBy(x => x % GroupFactor, x => -x, (key, elements) => KeyValuePair.Create(key, elements)))
            {
                Assert.Equal(groupsSeen++, group.Key);
                int elementsSeen = -group.Key;
                Assert.All(group.Value, x => { Assert.Equal(elementsSeen, x); elementsSeen -= GroupFactor; });
                Assert.Equal(-group.Key - (1 + (count - (group.Key + 1)) / GroupFactor) * GroupFactor, elementsSeen);
            }
            Assert.Equal(Math.Min(count, GroupFactor), groupsSeen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 64 }, MemberType = typeof(Sources))]
        public static void GroupBy_ElementSelector_ResultSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_ElementSelector_ResultSelector(labeled, count);
        }

        [Fact]
        public static void GroupBy_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).GroupBy(i => i));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).GroupBy(i => i, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, EqualityComparer<int>.Default));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).GroupBy(i => i, i => i));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, i => i));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => ParallelEnumerable.Range(0, 1).GroupBy(i => i, (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).GroupBy(i => i, i => i, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, i => i, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => ParallelEnumerable.Range(0, 1).GroupBy(i => i, (Func<int, int>)null, EqualityComparer<int>.Default));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).GroupBy(i => i, (i, j) => i));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, (i, j) => i));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => ParallelEnumerable.Range(0, 1).GroupBy(i => i, (Func<int, IEnumerable<int>, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).GroupBy(i => i, (i, j) => i, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, (i, j) => i, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => ParallelEnumerable.Range(0, 1).GroupBy(i => i, (Func<int, IEnumerable<int>, int>)null, EqualityComparer<int>.Default));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).GroupBy(i => i, i => i, (i, j) => i));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, i => i, (i, j) => i));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => ParallelEnumerable.Range(0, 1).GroupBy(i => i, (Func<int, int>)null, (i, j) => i));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => ParallelEnumerable.Range(0, 1).GroupBy(i => i, i => i, (Func<int, IEnumerable<int>, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).GroupBy(i => i, i => i, (i, j) => i, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, i => i, (i, j) => i, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => ParallelEnumerable.Range(0, 1).GroupBy(i => i, (Func<int, int>)null, (i, j) => i, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => ParallelEnumerable.Range(0, 1).GroupBy(i => i, i => i, (Func<int, IEnumerable<int>, int>)null, EqualityComparer<int>.Default));
        }

        [Fact]
        public static void GroupBy_LargeGroup_ForceInternalArrayToGrow()
        {
            const int Key = 42;
            const int LargeSize = 8192; // larger than GrowingArray's internal default array size

            IGrouping<int, int>[] result = ParallelEnumerable
                .Range(0, LargeSize)
                .AsOrdered()
                .GroupBy(i => Key)
                .ToArray();

            Assert.NotNull(result);
            Assert.Equal(1, result.Length);

            Assert.Equal(Key, result[0].Key);
            Assert.Equal(LargeSize, result[0].Count());
        }
    }
}
