// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class AggregateTests
    {
        private const int ResultFuncModifier = 17;

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Sum(int count)
        {
            // The operation will overflow for long-running sizes, but that's okay:
            // The helper is overflowing too!
            Assert.Equal(Functions.SumRange(0, count), UnorderedSources.Default(count).Aggregate((x, y) => unchecked(x + y)));
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Sum_Longrunning()
        {
            Aggregate_Sum(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Sum_Seed(int count)
        {
            Assert.Equal(Functions.SumRange(0, count), UnorderedSources.Default(count).Aggregate(0, (x, y) => unchecked(x + y)));
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Sum_Seed_Longrunning()
        {
            Aggregate_Sum_Seed(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Product_Seed(int count)
        {
            // The operation will overflow for long-running sizes, but that's okay:
            // The helper is overflowing too!
            Assert.Equal(Functions.ProductRange(1, count), ParallelEnumerable.Range(1, count).Aggregate(1L, (x, y) => unchecked(x * y)));
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Product_Seed_Longrunning()
        {
            Aggregate_Product_Seed(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Collection_Seed(int count)
        {
            Assert.Equal(Enumerable.Range(0, count), UnorderedSources.Default(count).Aggregate(ImmutableList<int>.Empty, (l, x) => l.Add(x)).OrderBy(x => x));
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Collection_Seed_Longrunning()
        {
            // Given the cost of using an object, reduce count.
            Aggregate_Collection_Seed(Sources.OuterLoopCount / 2);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Sum_Result(int count)
        {
            Assert.Equal(Functions.SumRange(0, count) + ResultFuncModifier,
                         UnorderedSources.Default(count).Aggregate(0, (x, y) => unchecked(x + y), result => result + ResultFuncModifier));
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Sum_Result_Longrunning()
        {
            Aggregate_Sum_Result(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Product_Result(int count)
        {
            Assert.Equal(Functions.ProductRange(1, count) + ResultFuncModifier,
                         ParallelEnumerable.Range(1, count).Aggregate(1L, (x, y) => unchecked(x * y), result => result + ResultFuncModifier));
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Product_Results_Longrunning()
        {
            Aggregate_Product_Result(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Collection_Results(int count)
        {
            Assert.Equal(Enumerable.Range(0, count), UnorderedSources.Default(count).Aggregate(ImmutableList<int>.Empty, (l, x) => l.Add(x), l => l.OrderBy(x => x)));
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Collection_Results_Longrunning()
        {
            Aggregate_Collection_Results(Sources.OuterLoopCount / 2);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Sum_Accumulator(int count)
        {
            ParallelQuery<int> query = UnorderedSources.Default(count);
            int actual = query.Aggregate(
                0,
                (accumulator, x) => accumulator + x,
                (left, right) => unchecked(left + right),
                result => result + ResultFuncModifier);
            Assert.Equal(Functions.SumRange(0, count) + ResultFuncModifier, actual);
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Sum_Accumulator_Longrunning()
        {
            Aggregate_Sum_Accumulator(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Product_Accumulator(int count)
        {
            ParallelQuery<int> query = ParallelEnumerable.Range(1, count);
            long actual = query.Aggregate(
                1L,
                (accumulator, x) => unchecked(accumulator * x),
                (left, right) => left * right,
                result => result + ResultFuncModifier);
            Assert.Equal(Functions.ProductRange(1, count) + ResultFuncModifier, actual);
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Product_Accumulator_Longrunning()
        {
            Aggregate_Product_Accumulator(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Collection_Accumulator(int count)
        {
            ParallelQuery<int> query = UnorderedSources.Default(count);
            IList<int> actual = query.Aggregate(
                ImmutableList<int>.Empty,
                (accumulator, x) => accumulator.Add(x),
                (left, right) => left.AddRange(right),
                result => result.OrderBy(x => x).ToList());
            Assert.Equal(Enumerable.Range(0, count), actual);
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Collection_Accumulator_Longrunning()
        {
            Aggregate_Collection_Accumulator(Sources.OuterLoopCount / 2);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Sum_SeedFunction(int count)
        {
            ParallelQuery<int> query = UnorderedSources.Default(count);
            int actual = query.Aggregate(
                () => 0,
                (accumulator, x) => accumulator + x,
                (left, right) => unchecked(left + right),
                result => result + ResultFuncModifier);
            Assert.Equal(Functions.SumRange(0, count) + ResultFuncModifier, actual);
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Sum_SeedFunction_Longrunning()
        {
            Aggregate_Sum_SeedFunction(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Product_SeedFunction(int count)
        {
            ParallelQuery<int> query = ParallelEnumerable.Range(1, count);
            long actual = query.Aggregate(
                () => 1L,
                (accumulator, x) => unchecked(accumulator * x),
                (left, right) => left * right,
                result => result + ResultFuncModifier);
            Assert.Equal(Functions.ProductRange(1, count) + ResultFuncModifier, actual);
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Product_SeedFunction_Longrunning()
        {
            Aggregate_Product_SeedFunction(Sources.OuterLoopCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Aggregate_Collection_SeedFunction(int count)
        {
            ParallelQuery<int> query = UnorderedSources.Default(count);
            IList<int> actual = query.Aggregate(
                () => ImmutableList<int>.Empty,
                (accumulator, x) => accumulator.Add(x),
                (left, right) => left.AddRange(right),
                result => result.OrderBy(x => x).ToList());
            Assert.Equal(Enumerable.Range(0, count), actual);
        }

        [Fact]
        [OuterLoop]
        public static void Aggregate_Collection_SeedFunction_Longrunning()
        {
            Aggregate_Collection_SeedFunction(Sources.OuterLoopCount / 2);
        }

        [Fact]
        public static void Aggregate_InvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Aggregate((i, j) => i));
            // All other invocations return the seed value.
            Assert.Equal(-1, ParallelEnumerable.Empty<int>().Aggregate(-1, (i, j) => i + j));
            Assert.Equal(-1, ParallelEnumerable.Empty<int>().Aggregate(-1, (i, j) => i + j, i => i));
            Assert.Equal(-1, ParallelEnumerable.Empty<int>().Aggregate(-1, (i, j) => i + j, (i, j) => i + j, i => i));
            Assert.Equal(-1, ParallelEnumerable.Empty<int>().Aggregate(() => -1, (i, j) => i + j, (i, j) => i + j, i => i));
        }

        [Fact]
        public static void Aggregate_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.Aggregate((i, j) => { canceler(); return j; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Aggregate(0, (i, j) => { canceler(); return j; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Aggregate(0, (i, j) => { canceler(); return j; }, i => i));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Aggregate(0, (i, j) => { canceler(); return j; }, (i, j) => i, i => i));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Aggregate(() => 0, (i, j) => { canceler(); ; return j; }, (i, j) => i, i => i));
        }

        [Fact]
        public static void Aggregate_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Aggregate((i, j) => { canceler(); return j; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Aggregate(0, (i, j) => { canceler(); return j; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Aggregate(0, (i, j) => { canceler(); return j; }, i => i));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Aggregate(0, (i, j) => { canceler(); return j; }, (i, j) => i, i => i));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Aggregate(() => 0, (i, j) => { canceler(); ; return j; }, (i, j) => i, i => i));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Aggregate((i, j) => { canceler(); return j; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Aggregate(0, (i, j) => { canceler(); return j; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Aggregate(0, (i, j) => { canceler(); return j; }, i => i));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Aggregate(0, (i, j) => { canceler(); return j; }, (i, j) => i, i => i));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Aggregate(() => 0, (i, j) => { canceler(); ; return j; }, (i, j) => i, i => i));
        }

        [Fact]
        public static void Aggregate_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.Aggregate((i, j) => i));
            AssertThrows.AlreadyCanceled(source => source.Aggregate(0, (i, j) => i));
            AssertThrows.AlreadyCanceled(source => source.Aggregate(0, (i, j) => i, i => i));
            AssertThrows.AlreadyCanceled(source => source.Aggregate(0, (i, j) => i, (i, j) => i, i => i));
            AssertThrows.AlreadyCanceled(source => source.Aggregate(() => 0, (i, j) => i, (i, j) => i, i => i));
        }

        [Fact]
        public static void Aggregate_AggregateException()
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(2).Aggregate((i, j) => { throw new DeliberateTestException(); }));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(2).Aggregate(0, (i, j) => { throw new DeliberateTestException(); }));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(2).Aggregate(0, (i, j) => { throw new DeliberateTestException(); }, i => i));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(2).Aggregate<int, int, int>(0, (i, j) => i, i => { throw new DeliberateTestException(); }));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(2).Aggregate(0, (i, j) => { throw new DeliberateTestException(); }, (i, j) => i, i => i));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(2).Aggregate<int, int, int>(0, (i, j) => i, (i, j) => i, i => { throw new DeliberateTestException(); }));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(2).Aggregate<int, int, int>(() => { throw new DeliberateTestException(); }, (i, j) => i, (i, j) => i, i => i));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(2).Aggregate(() => 0, (i, j) => { throw new DeliberateTestException(); }, (i, j) => i, i => i));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(2).Aggregate<int, int, int>(() => 0, (i, j) => i, (i, j) => i, i => { throw new DeliberateTestException(); }));
            if (Environment.ProcessorCount >= 2)
            {
                AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(2).Aggregate(0, (i, j) => i, (i, j) => { throw new DeliberateTestException(); }, i => i));
                AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(2).Aggregate(() => 0, (i, j) => i, (i, j) => { throw new DeliberateTestException(); }, i => i));
            }
        }

        [Fact]
        public static void Aggregate_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).Aggregate((i, j) => i));
            AssertExtensions.Throws<ArgumentNullException>("func", () => UnorderedSources.Default(1).Aggregate(null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).Aggregate(0, (i, j) => i));
            AssertExtensions.Throws<ArgumentNullException>("func", () => UnorderedSources.Default(1).Aggregate(0, null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).Aggregate(0, (i, j) => i, i => i));
            AssertExtensions.Throws<ArgumentNullException>("func", () => UnorderedSources.Default(1).Aggregate(0, null, i => i));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => UnorderedSources.Default(1).Aggregate<int, int, int>(0, (i, j) => i, null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).Aggregate(0, (i, j) => i, (i, j) => i, i => i));
            AssertExtensions.Throws<ArgumentNullException>("updateAccumulatorFunc", () => UnorderedSources.Default(1).Aggregate(0, null, (i, j) => i, i => i));
            AssertExtensions.Throws<ArgumentNullException>("combineAccumulatorsFunc", () => UnorderedSources.Default(1).Aggregate(0, (i, j) => i, null, i => i));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => UnorderedSources.Default(1).Aggregate<int, int, int>(0, (i, j) => i, (i, j) => i, null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).Aggregate(() => 0, (i, j) => i, (i, j) => i, i => i));
            AssertExtensions.Throws<ArgumentNullException>("seedFactory", () => UnorderedSources.Default(1).Aggregate<int, int, int>(null, (i, j) => i, (i, j) => i, i => i));
            AssertExtensions.Throws<ArgumentNullException>("updateAccumulatorFunc", () => UnorderedSources.Default(1).Aggregate(() => 0, null, (i, j) => i, i => i));
            AssertExtensions.Throws<ArgumentNullException>("combineAccumulatorsFunc", () => UnorderedSources.Default(1).Aggregate(() => 0, (i, j) => i, null, i => i));
            AssertExtensions.Throws<ArgumentNullException>("resultSelector", () => UnorderedSources.Default(1).Aggregate<int, int, int>(() => 0, (i, j) => i, (i, j) => i, null));
        }
    }
}
