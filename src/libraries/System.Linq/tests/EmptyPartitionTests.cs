// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class EmptyPartitionTests
    {
        private static IEnumerable<T> GetEmptyPartition<T>()
        {
            return new T[0].Take(0);
        }

        [Fact]
        public void EmptyPartitionIsEmpty()
        {
            Assert.Empty(GetEmptyPartition<int>());
            Assert.Empty(GetEmptyPartition<string>());
        }

        [Fact]
        public void SingleInstance()
        {
            // .NET Core returns the instance as an optimization.
            // see https://github.com/dotnet/corefx/pull/2401.
            Assert.True(ReferenceEquals(GetEmptyPartition<int>(), GetEmptyPartition<int>()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        public void SkipSame()
        {
            IEnumerable<int> empty = GetEmptyPartition<int>();
            Assert.Same(empty, empty.Skip(2));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        public void TakeSame()
        {
            IEnumerable<int> empty = GetEmptyPartition<int>();
            Assert.Same(empty, empty.Take(2));
        }

        [Fact]
        public void ElementAtThrows()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => GetEmptyPartition<int>().ElementAt(0));
        }

        [Fact]
        public void ElementAtOrDefaultIsDefault()
        {
            Assert.Equal(0, GetEmptyPartition<int>().ElementAtOrDefault(0));
            Assert.Null(GetEmptyPartition<string>().ElementAtOrDefault(0));
        }

        [Fact]
        public void FirstThrows()
        {
            Assert.Throws<InvalidOperationException>(() => GetEmptyPartition<int>().First());
        }

        [Fact]
        public void FirstOrDefaultIsDefault()
        {
            Assert.Equal(0, GetEmptyPartition<int>().FirstOrDefault());
            Assert.Null(GetEmptyPartition<string>().FirstOrDefault());
        }

        [Fact]
        public void LastThrows()
        {
            Assert.Throws<InvalidOperationException>(() => GetEmptyPartition<int>().Last());
        }

        [Fact]
        public void LastOrDefaultIsDefault()
        {
            Assert.Equal(0, GetEmptyPartition<int>().LastOrDefault());
            Assert.Null(GetEmptyPartition<string>().LastOrDefault());
        }

        [Fact]
        public void ToArrayEmpty()
        {
            Assert.Empty(GetEmptyPartition<int>().ToArray());
        }

        [Fact]
        public void ToListEmpty()
        {
            Assert.Empty(GetEmptyPartition<int>().ToList());
        }

        [Fact]
        public void ResetIsNop()
        {
            IEnumerator<int> en = GetEmptyPartition<int>().GetEnumerator();
            en.Reset();
            en.Reset();
            en.Reset();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
        public void IListImplementationIsValid()
        {
            IList<int> list = Assert.IsAssignableFrom<IList<int>>(Enumerable.Empty<int>());
            IReadOnlyList<int> roList = Assert.IsAssignableFrom<IReadOnlyList<int>>(Enumerable.Empty<int>());

            Assert.Throws<NotSupportedException>(() => list.Add(42));
            Assert.Throws<NotSupportedException>(() => list.Insert(0, 42));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.Throws<NotSupportedException>(() => list.Remove(42));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list[0] = 42);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => list[0]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => roList[0]);

            Assert.True(list.IsReadOnly);
            Assert.Equal(0, list.Count);
            Assert.Equal(0, roList.Count);

            Assert.False(list.Contains(42));
            Assert.Equal(-1, list.IndexOf(42));

            list.CopyTo(Array.Empty<int>(), 0);
            list.CopyTo(Array.Empty<int>(), 1);
            int[] array = new int[1] { 42 };
            list.CopyTo(array, 0);
            Assert.Equal(42, array[0]);
        }
    }
}
