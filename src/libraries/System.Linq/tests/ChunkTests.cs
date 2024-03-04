// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ChunkTests : EnumerableTests
    {
        [Fact]
        public void Empty()
        {
            Assert.Equal(Enumerable.Empty<int[]>(), Enumerable.Empty<int>().Chunk(4));
        }

        [Fact]
        public void ThrowsOnNullSource()
        {
            int[] source = null;
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Chunk(5));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ThrowsWhenSizeIsNonPositive(int size)
        {
            int[] source = {1};
            AssertExtensions.Throws<ArgumentOutOfRangeException>("size", () => source.Chunk(size));
        }

        [Fact]
        public void ChunkSourceLazily()
        {
            using IEnumerator<int[]> chunks = new FastInfiniteEnumerator<int>().Chunk(5).GetEnumerator();
            chunks.MoveNext();
            Assert.Equal(new[] {0, 0, 0, 0, 0}, chunks.Current);
            Assert.True(chunks.MoveNext());
        }

        private static IEnumerable<T> ConvertToType<T>(T[] array, Type type)
        {
            return type switch
            {
                {} x when x == typeof(TestReadOnlyCollection<T>) => new TestReadOnlyCollection<T>(array),
                {} x when x == typeof(TestCollection<T>) => new TestCollection<T>(array),
                {} x when x == typeof(TestEnumerable<T>) => new TestEnumerable<T>(array),
                _ => throw new Exception()
            };
        }

        [Theory]
        [InlineData(new[] {9999, 0, 888, -1, 66, -777, 1, 2, -12345}, typeof(TestReadOnlyCollection<int>))]
        [InlineData(new[] {9999, 0, 888, -1, 66, -777, 1, 2, -12345}, typeof(TestCollection<int>))]
        [InlineData(new[] {9999, 0, 888, -1, 66, -777, 1, 2, -12345}, typeof(TestEnumerable<int>))]
        public void ChunkSourceRepeatCalls(int[] array, Type type)
        {
            IEnumerable<int> source = ConvertToType(array, type);

            Assert.Equal(source.Chunk(3), source.Chunk(3));
        }

        [Theory]
        [InlineData(new[] {9999, 0, 888, -1, 66, -777, 1, 2, -12345}, typeof(TestReadOnlyCollection<int>))]
        [InlineData(new[] {9999, 0, 888, -1, 66, -777, 1, 2, -12345}, typeof(TestCollection<int>))]
        [InlineData(new[] {9999, 0, 888, -1, 66, -777, 1, 2, -12345}, typeof(TestEnumerable<int>))]
        public void ChunkSourceEvenly(int[] array, Type type)
        {
            IEnumerable<int> source = ConvertToType(array, type);

            using IEnumerator<int[]> chunks = source.Chunk(3).GetEnumerator();
            chunks.MoveNext();
            Assert.Equal(new[] {9999, 0, 888}, chunks.Current);
            chunks.MoveNext();
            Assert.Equal(new[] {-1, 66, -777}, chunks.Current);
            chunks.MoveNext();
            Assert.Equal(new[] {1, 2, -12345}, chunks.Current);
            Assert.False(chunks.MoveNext());
        }

        [Theory]
        [InlineData(new[] {9999, 0, 888, -1, 66, -777, 1, 2}, typeof(TestReadOnlyCollection<int>))]
        [InlineData(new[] {9999, 0, 888, -1, 66, -777, 1, 2}, typeof(TestCollection<int>))]
        [InlineData(new[] {9999, 0, 888, -1, 66, -777, 1, 2}, typeof(TestEnumerable<int>))]
        public void ChunkSourceUnevenly(int[] array, Type type)
        {
            IEnumerable<int> source = ConvertToType(array, type);

            using IEnumerator<int[]> chunks = source.Chunk(3).GetEnumerator();
            chunks.MoveNext();
            Assert.Equal(new[] {9999, 0, 888}, chunks.Current);
            chunks.MoveNext();
            Assert.Equal(new[] {-1, 66, -777}, chunks.Current);
            chunks.MoveNext();
            Assert.Equal(new[] {1, 2}, chunks.Current);
            Assert.False(chunks.MoveNext());
        }

        [Theory]
        [InlineData(new[] {9999, 0}, typeof(TestReadOnlyCollection<int>))]
        [InlineData(new[] {9999, 0}, typeof(TestCollection<int>))]
        [InlineData(new[] {9999, 0}, typeof(TestEnumerable<int>))]
        public void ChunkSourceSmallerThanMaxSize(int[] array, Type type)
        {
            IEnumerable<int> source = ConvertToType(array, type);

            using IEnumerator<int[]> chunks = source.Chunk(3).GetEnumerator();
            chunks.MoveNext();
            Assert.Equal(new[] {9999, 0}, chunks.Current);
            Assert.False(chunks.MoveNext());
        }

        [Theory]
        [InlineData(new int[] {}, typeof(TestReadOnlyCollection<int>))]
        [InlineData(new int[] {}, typeof(TestCollection<int>))]
        [InlineData(new int[] {}, typeof(TestEnumerable<int>))]
        public void EmptySourceYieldsNoChunks(int[] array, Type type)
        {
            IEnumerable<int> source = ConvertToType(array, type);

            using IEnumerator<int[]> chunks = source.Chunk(3).GetEnumerator();
            Assert.False(chunks.MoveNext());
        }

        [Fact]
        public void RemovingFromSourceBeforeIterating()
        {
            var list = new List<int>
            {
                9999, 0, 888, -1, 66, -777, 1, 2, -12345
            };
            IEnumerable<int[]> chunks = list.Chunk(3);
            list.Remove(66);

            Assert.Equal(new[] {new[] {9999, 0, 888}, new[] {-1, -777, 1}, new[] {2, -12345}}, chunks);
        }

        [Fact]
        public void AddingToSourceBeforeIterating()
        {
            var list = new List<int>
            {
                9999, 0, 888, -1, 66, -777, 1, 2, -12345
            };
            IEnumerable<int[]> chunks = list.Chunk(3);
            list.Add(10);

            Assert.Equal(new[] {new[] {9999, 0, 888}, new[] {-1, 66, -777}, new[] {1, 2, -12345}, new[] {10}}, chunks);
        }

        // reproduces https://github.com/dotnet/runtime/issues/67132
        [Fact]
        public void DoesNotPrematurelyAllocateHugeArray()
        {
            int[][] chunks = Enumerable.Range(0, 10).Chunk(int.MaxValue).ToArray();

            Assert.Equal(new[] { Enumerable.Range(0, 10).ToArray() }, chunks);
        }
    }
}
