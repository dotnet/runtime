// Copyright (c) Jon Hanna. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public void NotSingleInstance()
        {
            Assert.NotSame(GetEmptyPartition<int>(), GetEmptyPartition<int>());
        }

        [Fact]
        public void SkipNotSame()
        {
            var empty = GetEmptyPartition<int>();
            Assert.NotSame(empty, empty.Skip(2));
        }

        [Fact]
        public void TakeNotSame()
        {
            var empty = GetEmptyPartition<int>();
            Assert.NotSame(empty, empty.Take(2));
        }

        [Fact]
        public void ElementAtThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>("index", () => GetEmptyPartition<int>().ElementAt(0));
        }

        [Fact]
        public void ElementAtOrDefaultIsDefault()
        {
            Assert.Equal(0, GetEmptyPartition<int>().ElementAtOrDefault(0));
            Assert.Equal(null, GetEmptyPartition<string>().ElementAtOrDefault(0));
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
            Assert.Equal(null, GetEmptyPartition<string>().FirstOrDefault());
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
            Assert.Equal(null, GetEmptyPartition<string>().LastOrDefault());
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
        public void CantResetEnumerator()
        {
            IEnumerator<int> en = GetEmptyPartition<int>().GetEnumerator();
            Assert.Throws<NotSupportedException>(() => en.Reset());
        }
    }
}
