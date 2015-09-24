// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class ToDictionaryTests
    {
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(query.ToDictionary(x => x * 2),
                p => { seen.Add(p.Key / 2); Assert.Equal(p.Key, p.Value * 2); });
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ToDictionary(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_ElementSelector(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(query.ToDictionary(x => x, y => y * 2),
                p => { seen.Add(p.Key); Assert.Equal(p.Key * 2, p.Value); });
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_ElementSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ToDictionary_ElementSelector(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_CustomComparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(query.ToDictionary(x => x * 2, new ModularCongruenceComparer(count * 2)),
                p => { seen.Add(p.Key / 2); Assert.Equal(p.Key, p.Value * 2); });
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ToDictionary_CustomComparator(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_ElementSelector_CustomComparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(query.ToDictionary(x => x, y => y * 2, new ModularCongruenceComparer(count)),
                p => { seen.Add(p.Key); Assert.Equal(p.Key * 2, p.Value); });
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_ElementSelector_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ToDictionary_ElementSelector_CustomComparator(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_UniqueKeys_CustomComparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            if (count > 2)
            {
                AggregateException e = Assert.Throws<AggregateException>(() => query.ToDictionary(x => x, new ModularCongruenceComparer(2)));
                Assert.IsType<ArgumentException>(e.InnerException);
            }
            else if (count == 1 || count == 2)
            {
                IntegerRangeSet seen = new IntegerRangeSet(0, count);
                foreach (KeyValuePair<int, int> entry in query.ToDictionary(x => x, new ModularCongruenceComparer(2)))
                {
                    seen.Add(entry.Key);
                    Assert.Equal(entry.Key, entry.Value);
                }
                seen.AssertComplete();
            }
            else
            {
                Assert.Empty(query.ToDictionary(x => x, new ModularCongruenceComparer(2)));
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_UniqueKeys_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ToDictionary_UniqueKeys_CustomComparator(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_ElementSelector_UniqueKeys_CustomComparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            if (count > 2)
            {
                AggregateException e = Assert.Throws<AggregateException>(() => query.ToDictionary(x => x, y => y, new ModularCongruenceComparer(2)));
                Assert.IsType<ArgumentException>(e.InnerException);
            }
            else if (count == 1 || count == 2)
            {
                IntegerRangeSet seen = new IntegerRangeSet(0, count);
                foreach (KeyValuePair<int, int> entry in query.ToDictionary(x => x, y => y, new ModularCongruenceComparer(2)))
                {
                    seen.Add(entry.Key);
                    Assert.Equal(entry.Key, entry.Value);
                }
                seen.AssertComplete();
            }
            else
            {
                Assert.Empty(query.ToDictionary(x => x, y => y, new ModularCongruenceComparer(2)));
            }
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_ElementSelector_UniqueKeys_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ToDictionary_ElementSelector_UniqueKeys_CustomComparator(labeled, count);
        }

        [Fact]
        public static void ToDictionary_DuplicateKeys()
        {
            AggregateException e = Assert.Throws<AggregateException>(() => ParallelEnumerable.Repeat(0, 2).ToDictionary(x => x));
            Assert.IsType<ArgumentException>(e.InnerException);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_DuplicateKeys_ElementSelector(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            AggregateException e = Assert.Throws<AggregateException>(() => ParallelEnumerable.Repeat(0, 2).ToDictionary(x => x, y => y));
            Assert.IsType<ArgumentException>(e.InnerException);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_DuplicateKeys_ElementSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ToDictionary_DuplicateKeys_ElementSelector(labeled, count);
        }

        [Fact]
        public static void ToDictionary_DuplicateKeys_CustomComparator()
        {
            AggregateException e = Assert.Throws<AggregateException>(() => ParallelEnumerable.Repeat(0, 2).ToDictionary(x => x, new ModularCongruenceComparer(2)));
            Assert.IsType<ArgumentException>(e.InnerException);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0, 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_DuplicateKeys_ElementSelector_CustomComparator(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            AggregateException e = Assert.Throws<AggregateException>(() => ParallelEnumerable.Repeat(0, 2).ToDictionary(x => x, y => y, new ModularCongruenceComparer(2)));
            Assert.IsType<ArgumentException>(e.InnerException);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 16 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_DuplicateKeys_ElementSelector_CustomComparator_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ToDictionary_DuplicateKeys_ElementSelector_CustomComparator(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).ToDictionary(x => x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).ToDictionary(x => x, EqualityComparer<int>.Default));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).ToDictionary(x => x, y => y));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).ToDictionary(x => x, y => y, EqualityComparer<int>.Default));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void ToDictionary_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.ToDictionary((Func<int, int>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.ToDictionary((Func<int, int>)(x => { throw new DeliberateTestException(); }), y => y));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.ToDictionary(x => x, (Func<int, int>)(y => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.ToDictionary((Func<int, int>)(x => { throw new DeliberateTestException(); }), EqualityComparer<int>.Default));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.ToDictionary((Func<int, int>)(x => { throw new DeliberateTestException(); }), y => y, EqualityComparer<int>.Default));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.ToDictionary(x => x, (Func<int, int>)(y => { throw new DeliberateTestException(); }), EqualityComparer<int>.Default));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.ToDictionary(x => x, new FailingEqualityComparer<int>()));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.ToDictionary(x => x, y => y, new FailingEqualityComparer<int>()));
        }

        [Fact]
        public static void ToDictionary_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).ToDictionary(x => x));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).ToDictionary(x => x, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).ToDictionary(x => x, y => y));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).ToDictionary(x => x, y => y, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<int>().ToDictionary((Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<int>().ToDictionary((Func<int, int>)null, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<int>().ToDictionary((Func<int, int>)null, y => y));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<int>().ToDictionary((Func<int, int>)null, y => y, EqualityComparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<int>().ToDictionary(x => x, (Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Empty<int>().ToDictionary(x => x, (Func<int, int>)null, EqualityComparer<int>.Default));
        }
    }
}
