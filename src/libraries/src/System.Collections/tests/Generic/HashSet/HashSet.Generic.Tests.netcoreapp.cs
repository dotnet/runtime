// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public abstract partial class HashSet_Generic_Tests<T> : ISet_Generic_Tests<T>
    {
        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_Constructor_int(int capacity)
        {
            HashSet<T> set = new HashSet<T>(capacity);
            Assert.Equal(0, set.Count);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_Constructor_int_AddUpToAndBeyondCapacity(int capacity)
        {
            HashSet<T> set = new HashSet<T>(capacity);

            AddToCollection(set, capacity);
            Assert.Equal(capacity, set.Count);

            AddToCollection(set, capacity + 1);
            Assert.Equal(capacity + 1, set.Count);
        }

        [Fact]
        public void HashSet_Generic_Constructor_int_Negative_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => new HashSet<T>(-1));
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => new HashSet<T>(int.MinValue));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_Constructor_int_IEqualityComparer(int capacity)
        {
            IEqualityComparer<T> comparer = GetIEqualityComparer();
            HashSet<T> set = new HashSet<T>(capacity, comparer);
            Assert.Equal(0, set.Count);
            if (comparer == null)
                Assert.Equal(EqualityComparer<T>.Default, set.Comparer);
            else
                Assert.Equal(comparer, set.Comparer);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void HashSet_Generic_Constructor_int_IEqualityComparer_AddUpToAndBeyondCapacity(int capacity)
        {
            IEqualityComparer<T> comparer = GetIEqualityComparer();
            HashSet<T> set = new HashSet<T>(capacity, comparer);

            AddToCollection(set, capacity);
            Assert.Equal(capacity, set.Count);

            AddToCollection(set, capacity + 1);
            Assert.Equal(capacity + 1, set.Count);
        }

        [Fact]
        public void HashSet_Generic_Constructor_int_IEqualityComparer_Negative_ThrowsArgumentOutOfRangeException()
        {
            IEqualityComparer<T> comparer = GetIEqualityComparer();
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => new HashSet<T>(-1, comparer));
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => new HashSet<T>(int.MinValue, comparer));
        }
    }
}
