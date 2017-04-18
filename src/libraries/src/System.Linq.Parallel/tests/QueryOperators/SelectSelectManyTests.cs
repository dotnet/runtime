// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class SelectSelectManyTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Select_Unordered(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (var p in UnorderedSources.Default(count).Select(x => KeyValuePair.Create(x, x * x)))
            {
                seen.Add(p.Key);
                Assert.Equal(p.Key * p.Key, p.Value);
            }
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Select_Unordered_Longrunning()
        {
            Select_Unordered(Sources.OuterLoopCount / 64);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void Select(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (var p in query.Select(x => KeyValuePair.Create(x, x * x)))
            {
                Assert.Equal(seen++, p.Key);
                Assert.Equal(p.Key * p.Key, p.Value);
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 4 }, MemberType = typeof(Sources))]
        public static void Select_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Select(labeled, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Select_Unordered_NotPipelined(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(UnorderedSources.Default(count).Select(x => KeyValuePair.Create(x, x * x)).ToList(),
                p => { seen.Add(p.Key); Assert.Equal(p.Key * p.Key, p.Value); });
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Select_Unordered_NotPipelined_Longrunning()
        {
            Select_Unordered_NotPipelined(Sources.OuterLoopCount / 64);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void Select_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.Select(x => KeyValuePair.Create(x, x * x)).ToList(),
                p =>
                {
                    Assert.Equal(seen++, p.Key);
                    Assert.Equal(p.Key * p.Key, p.Value);
                });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 4 }, MemberType = typeof(Sources))]
        public static void Select_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Select_NotPipelined(labeled, count);
        }

        // Uses an element's index to calculate an output value.  If order preservation isn't
        // working, this would PROBABLY fail.  Unfortunately, this isn't deterministic.  But choosing
        // larger input sizes increases the probability that it will.

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Select_Indexed_Unordered(int count)
        {
            // For unordered collections, which element is at which index isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (var p in UnorderedSources.Default(count).Select((x, index) => KeyValuePair.Create(x, index)))
            {
                seen.Add(p.Key);
                Assert.Equal(p.Key, p.Value);
            }
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Select_Indexed_Unordered_Longrunning()
        {
            Select_Indexed_Unordered(Sources.OuterLoopCount / 64);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void Select_Indexed(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (var p in query.Select((x, index) => KeyValuePair.Create(x, index)))
            {
                Assert.Equal(seen++, p.Key);
                Assert.Equal(p.Key, p.Value);
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 4 }, MemberType = typeof(Sources))]
        public static void Select_Indexed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Select_Indexed(labeled, count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Select_Indexed_Unordered_NotPipelined(int count)
        {
            // For unordered collections, which element is at which index isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(UnorderedSources.Default(count).Select((x, index) => KeyValuePair.Create(x, index)).ToList(),
                p =>
                {
                    seen.Add(p.Key);
                    Assert.Equal(p.Key, p.Value);
                });
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Select_Indexed_Unordered_NotPipelined_Longrunning()
        {
            Select_Indexed_Unordered_NotPipelined(Sources.OuterLoopCount / 64);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0, 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void Select_Indexed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.Select((x, index) => KeyValuePair.Create(x, index)).ToList(),
                p =>
                {
                    Assert.Equal(seen++, p.Key);
                    Assert.Equal(p.Key, p.Value);
                });
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.Ranges), new[] { 1024 * 4 }, MemberType = typeof(Sources))]
        public static void Select_Indexed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Select_Indexed_NotPipelined(labeled, count);
        }

        [Fact]
        public static void Select_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).Select(x => x));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).Select((x, index) => x));
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Empty<bool>().Select((Func<bool, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Empty<bool>().Select((Func<bool, int, bool>)null));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public static void Select_OrderablePartitionerWithOutOfOrderInputs_AsOrdered_CorrectOrder(bool keysOrderedInEachPartition, bool keysNormalized)
        {
            var range = new RangeOrderablePartitioner(0, 1024, keysOrderedInEachPartition, keysNormalized);
            int next = 0;
            foreach (int i in range.AsParallel().AsOrdered().Select(i => i))
            {
                Assert.Equal(next++, i);
            }
        }

        //
        // SelectMany
        //
        // [Regression Test]
        //   An issue occurred because the QuerySettings structure was not being deep-cloned during
        //   query-opening.  As a result, the concurrent inner-enumerators (for the RHS operators)
        //   that occur in SelectMany were sharing CancellationState that they should not have.
        //   The result was that enumerators could falsely believe they had been canceled when
        //   another inner-enumerator was disposed.
        //
        //   Note: the failure was intermittent.  this test would fail about 1 in 2 times on mikelid1 (4-core).

        public static IEnumerable<object[]> SelectManyUnorderedData(int[] counts)
        {
            foreach (int count in counts.DefaultIfEmpty(Sources.OuterLoopCount / 64))
            {
                foreach (Labeled<Func<int, int, IEnumerable<int>>> expander in Expanders())
                {
                    foreach (int expandCount in new[] { 0, 1, 2, 8 })
                    {
                        yield return new object[] { count, expander, expandCount };
                    }
                }
            }
        }

        public static IEnumerable<object[]> SelectManyData(int[] counts)
        {
            foreach (object[] results in Sources.Ranges(counts.DefaultIfEmpty(Sources.OuterLoopCount / 64)))
            {
                foreach (Labeled<Func<int, int, IEnumerable<int>>> expander in Expanders())
                {
                    foreach (int count in new[] { 0, 1, 2, 8 })
                    {
                        yield return new object[] { results[0], results[1], expander, count };
                    }
                }
            }
        }

        private static IEnumerable<Labeled<Func<int, int, IEnumerable<int>>>> Expanders()
        {
            yield return Labeled.Label("Array", (Func<int, int, IEnumerable<int>>)((start, count) => Enumerable.Range(start * count, count).ToArray()));
            yield return Labeled.Label("Enumerable.Range", (Func<int, int, IEnumerable<int>>)((start, count) => Enumerable.Range(start * count, count)));
        }

        [Theory]
        [MemberData(nameof(SelectManyUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_Unordered(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count * expansion);
            foreach (int i in UnorderedSources.Default(count).SelectMany(x => expand(x, expansion)))
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Unordered_Longrunning(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_Unordered(count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            ParallelQuery<int> query = labeled.Item;
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            int seen = 0;
            foreach (int i in query.SelectMany(x => expand(x, expansion)))
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(count * expansion, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany(labeled, count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_Unordered_NotPipelined(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count * expansion);
            Assert.All(UnorderedSources.Default(count).SelectMany(x => expand(x, expansion)).ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Unordered_NotPipelined_Longrunning(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_Unordered_NotPipelined(count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            ParallelQuery<int> query = labeled.Item;
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            int seen = 0;
            Assert.All(query.SelectMany(x => expand(x, expansion)).ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(count * expansion, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_NotPipelined(labeled, count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_Unordered_ResultSelector(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count * expansion);
            foreach (var p in UnorderedSources.Default(count).SelectMany(x => expand(x, expansion), (original, expanded) => KeyValuePair.Create(original, expanded)))
            {
                seen.Add(p.Value);
                Assert.Equal(p.Key, p.Value / expansion);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Unordered_ResultSelector_Longrunning(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_Unordered_ResultSelector(count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_Unordered_ResultSelector_NotPipelined(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count * expansion);
            Assert.All(UnorderedSources.Default(count).SelectMany(x => expand(x, expansion), (original, expanded) => KeyValuePair.Create(original, expanded)).ToList(),
                p =>
                {
                    seen.Add(p.Value);
                    Assert.Equal(p.Key, p.Value / expansion);
                });
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Unordered_ResultSelector_NotPipelined_Longrunning(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_Unordered_ResultSelector_NotPipelined(count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_ResultSelector(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            ParallelQuery<int> query = labeled.Item;
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            int seen = 0;
            foreach (var p in query.SelectMany(x => expand(x, expansion), (original, expanded) => KeyValuePair.Create(original, expanded)))
            {
                Assert.Equal(seen++, p.Value);
                Assert.Equal(p.Key, p.Value / expansion);
            }
            Assert.Equal(count * expansion, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_ResultSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_ResultSelector(labeled, count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_ResultSelector_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            ParallelQuery<int> query = labeled.Item;
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            int seen = 0;
            Assert.All(query.SelectMany(x => expand(x, expansion), (original, expanded) => KeyValuePair.Create(original, expanded)).ToList(),
               p =>
               {
                   Assert.Equal(seen++, p.Value);
                   Assert.Equal(p.Key, p.Value / expansion);
               });
            Assert.Equal(count * expansion, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_ResultSelector_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_ResultSelector_NotPipelined(labeled, count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_Indexed_Unordered(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            // For unordered collections, which element is at which index isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count * expansion);
            foreach (var pIndex in UnorderedSources.Default(count).SelectMany((x, index) => expand(x, expansion).Select(y => KeyValuePair.Create(index, y))))
            {
                seen.Add(pIndex.Value);
                Assert.Equal(pIndex.Key, pIndex.Value / expansion);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Indexed_Unordered_Longrunning(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_Indexed_Unordered(count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_Indexed_Unordered_NotPipelined(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            // For unordered collections, which element is at which index isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count * expansion);
            Assert.All(UnorderedSources.Default(count).SelectMany((x, index) => expand(x, expansion).Select(y => KeyValuePair.Create(index, y))).ToList(),
                pIndex =>
                {
                    seen.Add(pIndex.Value);
                    Assert.Equal(pIndex.Key, pIndex.Value / expansion);
                });
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Indexed_Unordered_NotPipelined_Longrunning(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_Indexed_Unordered_NotPipelined(count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_Indexed(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            ParallelQuery<int> query = labeled.Item;
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            int seen = 0;
            foreach (var pIndex in query.SelectMany((x, index) => expand(x, expansion).Select(y => KeyValuePair.Create(index, y))))
            {
                Assert.Equal(seen++, pIndex.Value);
                Assert.Equal(pIndex.Key, pIndex.Value / expansion);
            }
            Assert.Equal(count * expansion, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Indexed_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_Indexed(labeled, count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_Indexed_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            ParallelQuery<int> query = labeled.Item;
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            int seen = 0;
            Assert.All(query.SelectMany((x, index) => expand(x, expansion).Select(y => KeyValuePair.Create(index, y))).ToList(),
                pIndex =>
                {
                    Assert.Equal(seen++, pIndex.Value);
                    Assert.Equal(pIndex.Key, pIndex.Value / expansion);
                });
            Assert.Equal(count * expansion, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Indexed_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_Indexed_NotPipelined(labeled, count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_Indexed_Unordered_ResultSelector(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            // For unordered collections, which element is at which index isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count * expansion);
            foreach (var pOuter in UnorderedSources.Default(count).SelectMany((x, index) => expand(x, expansion).Select(y => KeyValuePair.Create(index, y)), (original, expanded) => KeyValuePair.Create(original, expanded)))
            {
                var pInner = pOuter.Value;
                Assert.Equal(pOuter.Key, pInner.Key);
                seen.Add(pInner.Value);
                Assert.Equal(pOuter.Key, pInner.Value / expansion);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Indexed_Unordered_ResultSelector_Longrunning(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_Indexed_Unordered_ResultSelector(count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyUnorderedData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_Indexed_Unordered_ResultSelector_NotPipelined(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            // For unordered collections, which element is at which index isn't actually guaranteed, but an effect of the implementation.
            // If this test starts failing it should be updated, and possibly mentioned in release notes.
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count * expansion);
            Assert.All(UnorderedSources.Default(count).SelectMany((x, index) => expand(x, expansion).Select(y => KeyValuePair.Create(index, y)), (original, expanded) => KeyValuePair.Create(original, expanded)).ToList(),
                pOuter =>
                {
                    var pInner = pOuter.Value;
                    Assert.Equal(pOuter.Key, pInner.Key);
                    seen.Add(pInner.Value);
                    Assert.Equal(pOuter.Key, pInner.Value / expansion);
                });
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyUnorderedData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Indexed_Unordered_ResultSelector_NotPipelined_Longrunning(int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_Indexed_Unordered_ResultSelector_NotPipelined(count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_Indexed_ResultSelector(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            ParallelQuery<int> query = labeled.Item;
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            int seen = 0;
            foreach (var pOuter in query.SelectMany((x, index) => expand(x, expansion).Select(y => KeyValuePair.Create(index, y)), (original, expanded) => KeyValuePair.Create(original, expanded)))
            {
                var pInner = pOuter.Value;
                Assert.Equal(pOuter.Key, pInner.Key);
                Assert.Equal(seen++, pInner.Value);
                Assert.Equal(pOuter.Key, pInner.Value / expansion);
            }
            Assert.Equal(count * expansion, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Indexed_ResultSelector_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_Indexed_ResultSelector(labeled, count, expander, expansion);
        }

        [Theory]
        [MemberData(nameof(SelectManyData), new[] { 0, 1, 2, 16 })]
        public static void SelectMany_Indexed_ResultSelector_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            ParallelQuery<int> query = labeled.Item;
            Func<int, int, IEnumerable<int>> expand = expander.Item;
            int seen = 0;
            Assert.All(query.SelectMany((x, index) => expand(x, expansion).Select(y => KeyValuePair.Create(index, y)), (original, expanded) => KeyValuePair.Create(original, expanded)).ToList(),
                pOuter =>
                {
                    var pInner = pOuter.Value;
                    Assert.Equal(pOuter.Key, pInner.Key);
                    Assert.Equal(seen++, pInner.Value);
                    Assert.Equal(pOuter.Key, pInner.Value / expansion);
                });
            Assert.Equal(count * expansion, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(SelectManyData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void SelectMany_Indexed_ResultSelector_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, Labeled<Func<int, int, IEnumerable<int>>> expander, int expansion)
        {
            SelectMany_Indexed_ResultSelector_NotPipelined(labeled, count, expander, expansion);
        }

        [Fact]
        public static void SelectMany_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).SelectMany(x => new[] { x }));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).SelectMany((x, index) => new[] { x }));
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Empty<bool>().SelectMany((Func<bool, IEnumerable<bool>>)null));
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Empty<bool>().SelectMany((Func<bool, int, IEnumerable<bool>>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).SelectMany(x => new[] { x }, (x, y) => x));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<bool>)null).SelectMany((x, index) => new[] { x }, (x, y) => x));
            AssertExtensions.Throws<ArgumentNullException>("collectionSelector", () => ParallelEnumerable.Empty<bool>().SelectMany((Func<bool, IEnumerable<bool>>)null, (x, y) => x));
            AssertExtensions.Throws<ArgumentNullException>("collectionSelector", () => ParallelEnumerable.Empty<bool>().SelectMany((Func<bool, int, IEnumerable<bool>>)null, (x, y) => x));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => ParallelEnumerable.Empty<bool>().SelectMany(x => new[] { x }, (Func<bool, bool, bool>)null));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => ParallelEnumerable.Empty<bool>().SelectMany((x, index) => new[] { x }, (Func<bool, bool, bool>)null));
        }
    }
}
