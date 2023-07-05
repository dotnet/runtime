// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the List class.
    /// </summary>
    public abstract partial class List_Generic_Tests<T> : IList_Generic_Tests<T>
    {
        [Fact]
        public void CopyTo_InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("list", () => CollectionExtensions.CopyTo(null, Span<int>.Empty));
            AssertExtensions.Throws<ArgumentNullException>("list", () => CollectionExtensions.CopyTo(null, new Span<int>(new int[1])));

            var list = new List<int>() { 1, 2, 3 };
            Assert.Throws<ArgumentException>(() => CollectionExtensions.CopyTo(list, (Span<int>)new int[2]));
        }

        [Fact]
        public void CopyTo_ItemsCopiedCorrectly()
        {
            List<int> list;
            Span<int> destination;

            list = new List<int>();
            destination = Span<int>.Empty;
            list.CopyTo(destination);

            list = new List<int>() { 1, 2, 3 };
            destination = new int[3];
            list.CopyTo(destination);
            Assert.Equal(new[] { 1, 2, 3 }, destination.ToArray());

            list = new List<int>() { 1, 2, 3 };
            destination = new int[4];
            list.CopyTo(destination);
            Assert.Equal(new[] { 1, 2, 3, 0 }, destination.ToArray());
        }
    }
}
