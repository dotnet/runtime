// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class GroupByTests
    {
        private const int GroupFactor = 8;

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (IGrouping<int, int> group in query.GroupBy(x => x % GroupFactor))
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

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_Unordered(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(Sources))]
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
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(Sources))]
        public static void GroupBy_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (IGrouping<int, int> group in query.GroupBy(x => x % GroupFactor).ToList())
            {
                groupsSeen.Add(group.Key);

                IntegerRangeSet elementsSeen = new IntegerRangeSet(0, 1 + (count - (group.Key + 1)) / GroupFactor);
                Assert.All(group, x => { Assert.Equal(group.Key, x % GroupFactor); elementsSeen.Add(x / GroupFactor); });
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_Unordered_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(Sources))]
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
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(Sources))]
        public static void GroupBy_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_CustomComparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (IGrouping<int, int> group in query.GroupBy(x => x, new ModularCongruenceComparer(GroupFactor)))
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

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_Unordered_CustomComparator(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(Sources))]
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
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(Sources))]
        public static void GroupBy_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_CustomComparator(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_ElementSelector(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (IGrouping<int, int> group in query.GroupBy(x => x % GroupFactor, x => -x))
            {
                groupsSeen.Add(group.Key);

                int expected = 1 + (count - (group.Key + 1)) / GroupFactor;
                IntegerRangeSet elementsSeen = new IntegerRangeSet(1 - expected, expected);
                Assert.All(group, x => { Assert.Equal(group.Key, -x % GroupFactor); elementsSeen.Add(x / GroupFactor); });
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_ElementSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_Unordered_ElementSelector(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(Sources))]
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
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(Sources))]
        public static void GroupBy_ElementSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_ElementSelector(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_ElementSelector_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (IGrouping<int, int> group in query.GroupBy(x => x % GroupFactor, y => -y).ToList())
            {
                groupsSeen.Add(group.Key);

                int expected = 1 + (count - (group.Key + 1)) / GroupFactor;
                IntegerRangeSet elementsSeen = new IntegerRangeSet(1 - expected, expected);
                Assert.All(group, x => { Assert.Equal(group.Key, -x % GroupFactor); elementsSeen.Add(x / GroupFactor); });
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_ElementSelector_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_Unordered_ElementSelector_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(Sources))]
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
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(Sources))]
        public static void GroupBy_ElementSelector_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_ElementSelector_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_ResultSelector(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (var group in query.GroupBy(x => x % GroupFactor, (key, elements) => KeyValuePair.Create(key, elements)))
            {
                groupsSeen.Add(group.Key);
                IntegerRangeSet elementsSeen = new IntegerRangeSet(0, 1 + (count - (group.Key + 1)) / GroupFactor);
                Assert.All(group.Value, x => { Assert.Equal(group.Key, x % GroupFactor); elementsSeen.Add(x / GroupFactor); });
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_ResultSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_Unordered_ResultSelector(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(Sources))]
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
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(Sources))]
        public static void GroupBy_ResultSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_ResultSelector(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 7, 8, 15, 16 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_ResultSelector_CustomComparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (var group in query.GroupBy(x => x, (key, elements) => KeyValuePair.Create(key, elements), new ModularCongruenceComparer(GroupFactor)))
            {
                groupsSeen.Add(group.Key % GroupFactor);
                IntegerRangeSet elementsSeen = new IntegerRangeSet(0, 1 + (count - (group.Key % GroupFactor + 1)) / GroupFactor);
                Assert.All(group.Value, x => { Assert.Equal(group.Key % GroupFactor, x % GroupFactor); elementsSeen.Add(x / GroupFactor); });
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_ResultSelector_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_Unordered_ResultSelector_CustomComparator(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(Sources))]
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
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(Sources))]
        public static void GroupBy_ResultSelector_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_ResultSelector_CustomComparator(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 7, 8, 15, 16 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_ElementSelector_ResultSelector(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet groupsSeen = new IntegerRangeSet(0, Math.Min(count, GroupFactor));
            foreach (var group in query.GroupBy(x => x % GroupFactor, x => -x, (key, elements) => KeyValuePair.Create(key, elements)))
            {
                groupsSeen.Add(group.Key);
                int expected = 1 + (count - (group.Key + 1)) / GroupFactor;
                IntegerRangeSet elementsSeen = new IntegerRangeSet(1 - expected, expected);
                Assert.All(group.Value, x => { Assert.Equal(group.Key, -x % GroupFactor); elementsSeen.Add(x / GroupFactor); });
                elementsSeen.AssertComplete();
            }
            groupsSeen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void GroupBy_Unordered_ElementSelector_ResultSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_Unordered_ElementSelector_ResultSelector(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, GroupFactor - 1, GroupFactor, GroupFactor * 2 - 1, GroupFactor * 2 }), MemberType = typeof(Sources))]
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
        [MemberData("Ranges", (object)(new int[] { 1024, 1024 * 16 }), MemberType = typeof(Sources))]
        public static void GroupBy_ElementSelector_ResultSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            GroupBy_ElementSelector_ResultSelector(labeled, count);
        }

        [Fact]
        public static void GroupBy_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).GroupBy(i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).GroupBy(i => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, EqualityComparer<int>.Default));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).GroupBy(i => i, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, i => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy(i => i, (Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).GroupBy(i => i, i => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, i => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy(i => i, (Func<int, int>)null, EqualityComparer<int>.Default));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).GroupBy(i => i, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy(i => i, (Func<int, IEnumerable<int>, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).GroupBy(i => i, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy(i => i, (Func<int, IEnumerable<int>, int>)null, EqualityComparer<int>.Default));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).GroupBy(i => i, i => i, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, i => i, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy(i => i, (Func<int, int>)null, (i, j) => i));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy(i => i, i => i, (Func<int, IEnumerable<int>, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).GroupBy(i => i, i => i, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy((Func<int, int>)null, i => i, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy(i => i, (Func<int, int>)null, (i, j) => i, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).GroupBy(i => i, i => i, (Func<int, IEnumerable<int>, int>)null, EqualityComparer<int>.Default));
        }
    }
}
