// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the Dictionary class.
    /// </summary>
    public abstract partial class Dictionary_Generic_Tests<TKey, TValue> : IDictionary_Generic_Tests<TKey, TValue>
    {
        #region Remove(TKey)

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Dictionary_Generic_RemoveKey_ValidKeyNotContainedInDictionary(int count)
        {
            Dictionary<TKey, TValue> dictionary = (Dictionary<TKey, TValue>)GenericIDictionaryFactory(count);
            TValue value;
            TKey missingKey = GetNewKey(dictionary);

            Assert.False(dictionary.Remove(missingKey, out value));
            Assert.Equal(count, dictionary.Count);
            Assert.Equal(default(TValue), value);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Dictionary_Generic_RemoveKey_ValidKeyContainedInDictionary(int count)
        {
            Dictionary<TKey, TValue> dictionary = (Dictionary<TKey, TValue>)GenericIDictionaryFactory(count);
            TKey missingKey = GetNewKey(dictionary);
            TValue outValue;
            TValue inValue = CreateTValue(count);

            dictionary.Add(missingKey, inValue);
            Assert.True(dictionary.Remove(missingKey, out outValue));
            Assert.Equal(count, dictionary.Count);
            Assert.Equal(inValue, outValue);
            Assert.False(dictionary.TryGetValue(missingKey, out outValue));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Dictionary_Generic_RemoveKey_DefaultKeyNotContainedInDictionary(int count)
        {
            Dictionary<TKey, TValue> dictionary = (Dictionary<TKey, TValue>)GenericIDictionaryFactory(count);
            TValue outValue;

            if (DefaultValueAllowed)
            {
                TKey missingKey = default(TKey);
                while (dictionary.ContainsKey(missingKey))
                    dictionary.Remove(missingKey);
                Assert.False(dictionary.Remove(missingKey, out outValue));
                Assert.Equal(default(TValue), outValue);
            }
            else
            {
                TValue initValue = CreateTValue(count);
                outValue = initValue;
                Assert.Throws<ArgumentNullException>(() => dictionary.Remove(default(TKey), out outValue));
                Assert.Equal(initValue, outValue);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Dictionary_Generic_RemoveKey_DefaultKeyContainedInDictionary(int count)
        {
            if (DefaultValueAllowed)
            {
                Dictionary<TKey, TValue> dictionary = (Dictionary<TKey, TValue>)(GenericIDictionaryFactory(count));
                TKey missingKey = default(TKey);
                TValue value;

                dictionary.TryAdd(missingKey, default(TValue));
                Assert.True(dictionary.Remove(missingKey, out value));
            }
        }

        #endregion

        #region EnsureCapacity

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void EnsureCapacity_RequestingLargerCapacity_DoesNotInvalidateEnumeration(int count)
        {
            Dictionary<TKey, TValue> dictionary = (Dictionary<TKey, TValue>)(GenericIDictionaryFactory(count));
            var capacity = dictionary.EnsureCapacity(0);
            ICollection<TValue> values = dictionary.Values;
            IEnumerator<TValue> valuesEnum = values.GetEnumerator();
            dictionary.EnsureCapacity(capacity + 1); // Verify EnsureCapacity does not invalidate enumeration
            while (valuesEnum.MoveNext());
        }

        [Fact]
        public void EnsureCapacity_NegativeCapacityRequested_Throws()
        {
            var dictionary = new Dictionary<string, string>();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => dictionary.EnsureCapacity(-1));
        }

        [Fact]
        [ActiveIssue("dotnet/corefx #24445")] 
        public void EnsureCapacity_DictionaryNotInitialized_RequestedZero_ReturnsZero()
        {
            var dictionary = new Dictionary<string, string>();
            Assert.Equal(0, dictionary.EnsureCapacity(0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void EnsureCapacity_DictionaryNotInitialized_RequestedNonZero_CapacityIsSetToAtLeastTheRequested(int requestedCapacity)
        {
            var dictionary = new Dictionary<string, string>();
            Assert.InRange(dictionary.EnsureCapacity(requestedCapacity), requestedCapacity, int.MaxValue);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(7)]
        public void EnsureCapacity_RequestedCapacitySmallerThanCurrent_CapacityUnchanged(int currentCapacity)
        {
            Dictionary<string, string> dictionary;

            // assert capacity remains the same when ensuring a capacity smaller or equal to existing
            for (int i = 0; i <= currentCapacity; i++)
            {
                dictionary = new Dictionary<string, string>(currentCapacity);
                Assert.Equal(currentCapacity, dictionary.EnsureCapacity(i));
            }
        }

        [Theory]
        [InlineData(7)]
        public void EnsureCapacity_ExistingCapacityRequested_SameValueReturned(int capacity)
        {
            var dictionary = new Dictionary<int, int>(capacity);
            Assert.Equal(capacity, dictionary.EnsureCapacity(capacity));

            dictionary = new Dictionary<int, int>();
            for (int i = 0; i < capacity; i++)
            {
                dictionary.Add(i, 0);
            }
            Assert.Equal(capacity, dictionary.EnsureCapacity(capacity));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(7)]
        public void EnsureCapacity_EnsureCapacityCalledTwice_ReturnsSameValue(int count)
        {
            var dictionary = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
            {
                dictionary.Add(i, 0);
            }
            int capacity = dictionary.EnsureCapacity(count * 2);
            Assert.Equal(capacity, dictionary.EnsureCapacity(count * 2));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(7)]
        public void EnsureCapacity_DictionaryNotEmpty_RequestedSmallerThanCount_ReturnsAtLeastSizeOfCount(int count)
        {
            var dictionary = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
            {
                dictionary.Add(i, 0);
            }
            Assert.InRange(dictionary.EnsureCapacity(count - 1), count, int.MaxValue);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(20)]
        public void EnsureCapacity_DictionaryNotEmpty_SetsToAtLeastTheRequested(int count)
        {
            var dictionary = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
            {
                dictionary.Add(i, 0);
            }

            // get current capacity
            int currentCapacity = dictionary.EnsureCapacity(0);

            // assert we can update to a larger capacity
            int newCapacity = dictionary.EnsureCapacity(currentCapacity * 2);
            Assert.InRange(newCapacity, currentCapacity * 2, int.MaxValue);
        }

        [Fact]
        public void EnsureCapacity_CapacityIsSetToPrimeNumberLargerOrEqualToRequested()
        {
            var dictionary = new Dictionary<int, int>();
            Assert.Equal(17, dictionary.EnsureCapacity(17));

            dictionary = new Dictionary<int, int>();
            Assert.Equal(17, dictionary.EnsureCapacity(15));

            dictionary = new Dictionary<int, int>();
            Assert.Equal(17, dictionary.EnsureCapacity(13));
        }

        #endregion
    }
}
