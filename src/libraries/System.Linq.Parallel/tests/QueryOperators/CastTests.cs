// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class CastTests
    {
        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Cast_Unordered_Valid(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            foreach (int i in query.Select(x => (object)x).Cast<int>())
            {
                seen.Add(i);
            }
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 1024 }), MemberType = typeof(UnorderedSources))]
        public static void Cast_Unordered_Valid_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Cast_Unordered_Valid(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(Sources))]
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
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 1024 }), MemberType = typeof(Sources))]
        public static void Cast_Valid_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Cast_Valid(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        public static void Cast_Unordered_Valid_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            IntegerRangeSet seen = new IntegerRangeSet(0, count);
            Assert.All(query.Select(x => (object)x).Cast<int>().ToList(), x => seen.Add(x));
            seen.AssertComplete();
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 1024 }), MemberType = typeof(UnorderedSources))]
        public static void Cast_Unordered_Valid_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Cast_Unordered_Valid_NotPipelined(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void Cast_Valid_NotPipelined(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> query = labeled.Item;
            int seen = 0;
            Assert.All(query.Select(x => (object)x).Cast<int>().ToList(), x => Assert.Equal(seen++, x));
            Assert.Equal(count, seen);
        }

        [Theory]
        [OuterLoop]
        [MemberData("Ranges", (object)(new int[] { 1024 * 4, 1024 * 1024 }), MemberType = typeof(Sources))]
        public static void Cast_Valid_NotPipelined_Longrunning(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Cast_Valid(labeled, count);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 0 }), MemberType = typeof(UnorderedSources))]
        [MemberData("Ranges", (object)(new int[] { 0 }), MemberType = typeof(Sources))]
        public static void Cast_Empty(Labeled<ParallelQuery<int>> labeled, int count)
        {
            ParallelQuery<int> empty = labeled.Item;

            Assert.IsAssignableFrom<ParallelQuery<int>>(empty.Cast<string>().Cast<int>());
            Assert.Empty(empty.Cast<string>().Cast<int>());
            Assert.Empty(empty.Cast<string>().Cast<int>().ToList());
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void Cast_InvalidCastException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<InvalidCastException>(() => labeled.Item.Cast<double>().ForAll(x => {; }));
            Functions.AssertThrowsWrapped<InvalidCastException>(() => labeled.Item.Cast<double>().ToList());
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(UnorderedSources))]
        [MemberData("Ranges", (object)(new int[] { 1, 2, 16 }), MemberType = typeof(Sources))]
        public static void Cast_Assignable_InvalidCastException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<InvalidCastException>(() => labeled.Item.Select(x => (Int32)x).Cast<Castable>().ForAll(x => {; }));
            Functions.AssertThrowsWrapped<InvalidCastException>(() => labeled.Item.Select(x => (Int32)x).Cast<Castable>().ToList());
        }

        [Fact]
        public static void Cast_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<object>)null).Cast<int>());
        }

        private class Castable
        {
            private readonly int _value;

            private Castable(int value)
            {
                _value = value;
            }

            public int Value { get { return _value; } }

            public static explicit operator Castable(Int32 value)
            {
                return new Castable(value);
            }
        }
    }
}
