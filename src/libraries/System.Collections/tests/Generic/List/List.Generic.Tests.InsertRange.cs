// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the List class.
    /// </summary>
    public abstract partial class List_Generic_Tests<T> : IList_Generic_Tests<T>
    {
        [Fact]
        public void InsertRange_InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("list", () => CollectionExtensions.InsertRange(null, 0, ReadOnlySpan<int>.Empty));
            AssertExtensions.Throws<ArgumentNullException>("list", () => CollectionExtensions.InsertRange(null, 0, new ReadOnlySpan<int>(new int[1])));

            var list = new List<int>();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => CollectionExtensions.InsertRange(list, 1, new int[0]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => CollectionExtensions.InsertRange(list, -1, new int[0]));
        }

        [Fact]
        public void InsertRange_MatchesExpectedContents()
        {
            var list = new List<int>();

            list.InsertRange(0, ReadOnlySpan<int>.Empty);
            Assert.Equal(0, list.Count);

            list.InsertRange(0, (ReadOnlySpan<int>)new int[] { 3, 2, 1 });
            Assert.Equal(3, list.Count);
            Assert.Equal(new[] { 3, 2, 1 }, list);

            list.InsertRange(0, (ReadOnlySpan<int>)new int[] { 6, 5, 4 });
            Assert.Equal(6, list.Count);
            Assert.Equal(new[] { 6, 5, 4, 3, 2, 1 }, list);

            list.InsertRange(6, (ReadOnlySpan<int>)new int[] { 0, -1, -2 });
            Assert.Equal(9, list.Count);
            Assert.Equal(new[] { 6, 5, 4, 3, 2, 1, 0, -1, -2 }, list);

            list.InsertRange(3, (ReadOnlySpan<int>)new int[] { 100, 99, 98 });
            Assert.Equal(12, list.Count);
            Assert.Equal(new[] { 6, 5, 4, 100, 99, 98, 3, 2, 1, 0, -1, -2 }, list);
        }

        [Fact]
        public void InsertRange_CollectionWithLargeCount_ThrowsOverflowException()
        {
            List<T> list = GenericListFactory(count: 1);
            ICollection<T> collection = new CollectionWithLargeCount();

            Assert.Throws<OverflowException>(() => list.InsertRange(0, collection));
        }
    }
}
