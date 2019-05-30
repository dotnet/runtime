// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Assert.Equal(true, ReferenceEquals(GetEmptyPartition<int>(), GetEmptyPartition<int>()));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Netcoreapp, ".NET Core returns the instance as an optimization")]
        public void SkipSame()
        {
            IEnumerable<int> empty = GetEmptyPartition<int>();
            Assert.Same(empty, empty.Skip(2));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Netcoreapp, ".NET Core returns the instance as an optimization")]
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
        public void ResetIsNop()
        {
            IEnumerator<int> en = GetEmptyPartition<int>().GetEnumerator();
            en.Reset();
            en.Reset();
            en.Reset();
        }
    }
}
