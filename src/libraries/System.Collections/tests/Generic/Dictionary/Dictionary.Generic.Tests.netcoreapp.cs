// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
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
        
        [Fact]
        public void EnsureCapacity_NegativeCapacityRequested_Throws()
        {
            var dictionary = new Dictionary<string, string>();
            Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.EnsureCapacity(-1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void EnsureCapacity_DefaultCapacityOnEmptyDictionary_ReturnsCapacityRequestedLargerOrEqual(int requestedCapacity)
        {
            var dictionary = new Dictionary<string, string>();
            Assert.InRange(dictionary.EnsureCapacity(requestedCapacity), requestedCapacity, int.MaxValue);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void EnsureCapacity_CapacityRequestedSmallerThanCurrentCapacity_CapacityUnchanged(int requestedCapacity)
        {
            var dictionary = new Dictionary<string, string>();
            var capacity = dictionary.EnsureCapacity(requestedCapacity);
            Assert.Equal(capacity, dictionary.EnsureCapacity(requestedCapacity - 1));
        }
        
        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public void EnsureCapacity_CapacityRequestedSmallerThanCount_SetsCapacityToLargerOrEqualToExistingCount(int count)
        {
            var dictionary = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
            {
                dictionary.Add(i, i);
            }
            Assert.InRange(dictionary.EnsureCapacity(count - 1), dictionary.Count, int.MaxValue);
        }

        [Fact]
        public void EnsureCapacity_GivenNonEmptyDictionary_CapacityRemainsTheSameUnlessCalledWithValueLargerThanExisting()
        {
            int count = 20;
            var dictionary = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
            {
                dictionary.Add(i, i);
            }

            // assert capacity won't change when ensuring a capacity smaller or equal than existing
            int currentCapacity = dictionary.EnsureCapacity(0);
            Assert.Equal(currentCapacity, dictionary.EnsureCapacity(currentCapacity));
            Assert.Equal(currentCapacity, dictionary.EnsureCapacity(currentCapacity - 2));

            // assert we can update to a larger capacity
            int newCapacity = dictionary.EnsureCapacity(currentCapacity * 2);
            Assert.InRange(newCapacity, currentCapacity * 2, int.MaxValue);

            // assert new capacity remains the newly updated one
            Assert.Equal(newCapacity, dictionary.EnsureCapacity(0));
        }

        [Fact]
        public void EnsureCapacity_SetsToNextPrimeNumber()
        {
            var dictionary = new Dictionary<int, int>();
            Assert.Equal(17, dictionary.EnsureCapacity(17));

            dictionary = new Dictionary<int, int>();
            Assert.Equal(17, dictionary.EnsureCapacity(15));
        }

        #endregion
    }
}
