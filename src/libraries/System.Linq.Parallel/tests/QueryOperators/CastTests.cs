// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class CastTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Cast_Unordered_Valid(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (int i in UnorderedSources.Default(count).Select(x => (object)x).Cast<int>())
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Cast_Unordered_Valid_Longrunning()
        {
            Cast_Unordered_Valid(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void Cast_Valid(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            foreach (int i in query.Select(x => (object)x).Cast<int>())
            {
                Assert.Equal(seen++, i);
            }
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.OuterLoopRanges), MemberType = typeof(Sources))]
        public static void Cast_Valid_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Cast_Valid(labeled, count);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Cast_Unordered_Valid_NotPipelined(int count)
        {
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(UnorderedSources.Default(count).Select(x => (object)x).Cast<int>().ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Fact]
        [OuterLoop]
        public static void Cast_Unordered_Valid_NotPipelined_Longrunning()
        {
            Cast_Unordered_Valid_NotPipelined(Sources.OuterLoopCount);
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void Cast_Valid_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.Select(x => (object)x).Cast<int>().ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(Sources.OuterLoopRanges), MemberType = typeof(Sources))]
        public static void Cast_Valid_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Cast_Valid(labeled, count);
        }

        [Fact]
        public static void Cast_Unordered_Empty()
        {
            ParallelQuery<int> empty = UnorderedSources.Default(0);

            Assert.IsAssignableFrom<ParallelQuery<int>>(empty.Cast<string>().Cast<int>());
            Assert.Empty(empty.Cast<string>().Cast<int>());
            Assert.Empty(empty.Cast<string>().Cast<int>().ToList());
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 0 }, MemberType = typeof(Sources))]
        public static void Cast_Empty(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> empty = labeled.Item;

            Assert.IsAssignableFrom<ParallelQuery<int>>(empty.Cast<string>().Cast<int>());
            Assert.Empty(empty.Cast<string>().Cast<int>());
            Assert.Empty(empty.Cast<string>().Cast<int>().ToList());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Cast_Unordered_InvalidCastException(int count)
        {
            AssertThrows.Wrapped<InvalidCastException>(() => UnorderedSources.Default(count).Cast<double>().ForAll(x => {; }));
            AssertThrows.Wrapped<InvalidCastException>(() => UnorderedSources.Default(count).Cast<double>().ToList());
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void Cast_InvalidCastException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            AssertThrows.Wrapped<InvalidCastException>(() => labeled.Item.Cast<double>().ForAll(x => {; }));
            AssertThrows.Wrapped<InvalidCastException>(() => labeled.Item.Cast<double>().ToList());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Cast_Unordered_Assignable_InvalidCastException(int count)
        {
            AssertThrows.Wrapped<InvalidCastException>(() => UnorderedSources.Default(count).Select(x => (Int32)x).Cast<Castable>().ForAll(x => {; }));
            AssertThrows.Wrapped<InvalidCastException>(() => UnorderedSources.Default(count).Select(x => (Int32)x).Cast<Castable>().ToList());
        }

        [Theory]
        [MemberData(nameof(Sources.Ranges), new[] { 1, 2, 16 }, MemberType = typeof(Sources))]
        public static void Cast_Assignable_InvalidCastException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            AssertThrows.Wrapped<InvalidCastException>(() => labeled.Item.Select(x => (Int32)x).Cast<Castable>().ForAll(x => {; }));
            AssertThrows.Wrapped<InvalidCastException>(() => labeled.Item.Select(x => (Int32)x).Cast<Castable>().ToList());
        }

        [Fact]
        public static void Cast_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<object>)null).Cast<int>());
        }

        private class Castable
        {
            private readonly int _value;

            private Castable(int value)
            {
                _value = value;
            }

            public int Value { get { return _value; } }

            public static explicit operator Castable(int value)
            {
                return new Castable(value);
            }
        }
    }
}
