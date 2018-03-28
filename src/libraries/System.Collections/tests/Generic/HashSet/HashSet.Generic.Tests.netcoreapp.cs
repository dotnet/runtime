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
        public void HashSet_Generic_Constructor_Capacity_ToNextPrimeNumber()
        {
            // Highest pre-computed number + 1.
            const int Capacity = 7199370;
            var set = new HashSet<T>(Capacity);

            // Assert that the HashTable's capacity is set to the descendant prime number of the given one.
            const int NextPrime = 7199371;
            Assert.Equal(NextPrime, set.EnsureCapacity(0));
        }

        [Fact]
        public void HashSet_Generic_Constructor_int_Negative_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new HashSet<T>(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new HashSet<T>(int.MinValue));
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
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new HashSet<T>(-1, comparer));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new HashSet<T>(int.MinValue, comparer));
        }

        #region TryGetValue

        [Fact]
        public void HashSet_Generic_TryGetValue_Contains()
        {
            T value = CreateT(1);
            HashSet<T> set = new HashSet<T> { value };
            T equalValue = CreateT(1);
            T actualValue;
            Assert.True(set.TryGetValue(equalValue, out actualValue));
            Assert.Equal(value, actualValue);
            if (!typeof(T).IsValueType)
            {
                Assert.Same(value, actualValue);
            }
        }

        [Fact]
        public void HashSet_Generic_TryGetValue_Contains_OverwriteOutputParam()
        {
            T value = CreateT(1);
            HashSet<T> set = new HashSet<T> { value };
            T equalValue = CreateT(1);
            T actualValue = CreateT(2);
            Assert.True(set.TryGetValue(equalValue, out actualValue));
            Assert.Equal(value, actualValue);
            if (!typeof(T).IsValueType)
            {
                Assert.Same(value, actualValue);
            }
        }

        [Fact]
        public void HashSet_Generic_TryGetValue_NotContains()
        {
            T value = CreateT(1);
            HashSet<T> set = new HashSet<T> { value };
            T equalValue = CreateT(2);
            T actualValue;
            Assert.False(set.TryGetValue(equalValue, out actualValue));
            Assert.Equal(default(T), actualValue);
        }

        [Fact]
        public void HashSet_Generic_TryGetValue_NotContains_OverwriteOutputParam()
        {
            T value = CreateT(1);
            HashSet<T> set = new HashSet<T> { value };
            T equalValue = CreateT(2);
            T actualValue = equalValue;
            Assert.False(set.TryGetValue(equalValue, out actualValue));
            Assert.Equal(default(T), actualValue);
        }

        #endregion

        #region EnsureCapacity

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void EnsureCapacity_Generic_RequestingLargerCapacity_DoesNotInvalidateEnumeration(int setLength)
        {
            HashSet<T> set = (HashSet<T>)(GenericISetFactory(setLength));
            var capacity = set.EnsureCapacity(0);
            IEnumerator valuesEnum = set.GetEnumerator();
            IEnumerator valuesListEnum = new List<T>(set).GetEnumerator();

            set.EnsureCapacity(capacity + 1); // Verify EnsureCapacity does not invalidate enumeration

            while (valuesEnum.MoveNext())
            {
                valuesListEnum.MoveNext();
                Assert.Equal(valuesListEnum.Current, valuesEnum.Current);
            }
        }

        [Fact]
        public void EnsureCapacity_Generic_NegativeCapacityRequested_Throws()
        {
            var set = new HashSet<T>();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => set.EnsureCapacity(-1));
        }

        [Fact]
        public void EnsureCapacity_Generic_HashsetNotInitialized_RequestedZero_ReturnsZero()
        {
            var set = new HashSet<T>();
            Assert.Equal(0, set.EnsureCapacity(0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void EnsureCapacity_Generic_HashsetNotInitialized_RequestedNonZero_CapacityIsSetToAtLeastTheRequested(int requestedCapacity)
        {
            var set = new HashSet<T>();
            Assert.InRange(set.EnsureCapacity(requestedCapacity), requestedCapacity, int.MaxValue);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(7)]
        public void EnsureCapacity_Generic_RequestedCapacitySmallerThanCurrent_CapacityUnchanged(int currentCapacity)
        {
            HashSet<T> set;

            // assert capacity remains the same when ensuring a capacity smaller or equal than existing
            for (int i = 0; i <= currentCapacity; i++)
            {
                set = new HashSet<T>(currentCapacity);
                Assert.Equal(currentCapacity, set.EnsureCapacity(i));
            }
        }

        [Theory]
        [InlineData(7)]
        [InlineData(89)]
        public void EnsureCapacity_Generic_ExistingCapacityRequested_SameValueReturned(int capacity)
        {
            var set = new HashSet<T>(capacity);
            Assert.Equal(capacity, set.EnsureCapacity(capacity));

            set = (HashSet<T>)GenericISetFactory(capacity);
            Assert.Equal(capacity, set.EnsureCapacity(capacity));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void EnsureCapacity_Generic_EnsureCapacityCalledTwice_ReturnsSameValue(int setLength)
        {
            HashSet<T> set = (HashSet<T>)GenericISetFactory(setLength);
            int capacity = set.EnsureCapacity(0);
            Assert.Equal(capacity, set.EnsureCapacity(0));

            set = (HashSet<T>)GenericISetFactory(setLength);
            capacity = set.EnsureCapacity(setLength);
            Assert.Equal(capacity, set.EnsureCapacity(setLength));

            set = (HashSet<T>)GenericISetFactory(setLength);
            capacity = set.EnsureCapacity(setLength + 1);
            Assert.Equal(capacity, set.EnsureCapacity(setLength + 1));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(7)]
        [InlineData(8)]
        public void EnsureCapacity_Generic_HashsetNotEmpty_RequestedSmallerThanCount_ReturnsAtLeastSizeOfCount(int setLength)
        {
            HashSet<T> set = (HashSet<T>)GenericISetFactory(setLength);
            Assert.InRange(set.EnsureCapacity(setLength - 1), setLength, int.MaxValue);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(20)]
        public void EnsureCapacity_Generic_HashsetNotEmpty_SetsToAtLeastTheRequested(int setLength)
        {
            HashSet<T> set = (HashSet<T>)GenericISetFactory(setLength);

            // get current capacity
            int currentCapacity = set.EnsureCapacity(0);

            // assert we can update to a larger capacity
            int newCapacity = set.EnsureCapacity(currentCapacity * 2);
            Assert.InRange(newCapacity, currentCapacity * 2, int.MaxValue);
        }

        [Fact]
        public void EnsureCapacity_Generic_CapacityIsSetToPrimeNumberLargerOrEqualToRequested()
        {
            var set = new HashSet<T>();
            Assert.Equal(17, set.EnsureCapacity(17));

            set = new HashSet<T>();
            Assert.Equal(17, set.EnsureCapacity(15));

            set = new HashSet<T>();
            Assert.Equal(17, set.EnsureCapacity(13));
        }

        #endregion
    }
}
