// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the Dictionary class.
    /// </summary>
    public abstract class SortedList_Generic_Tests<TKey, TValue> : IDictionary_Generic_Tests<TKey, TValue>
    {
        #region IDictionary<TKey, TValue> Helper Methods

        protected override IDictionary<TKey, TValue> GenericIDictionaryFactory()
        {
            return new SortedList<TKey, TValue>();
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public override void Enumerator_MoveNext_AfterDisposal(int count)
        {
            // Disposal of the enumerator is treated the same as a Reset call
            IEnumerator<KeyValuePair<TKey, TValue>> enumerator = GenericIEnumerableFactory(count).GetEnumerator();
            for (int i = 0; i < count; i++)
                enumerator.MoveNext();
            enumerator.Dispose();
            if (count > 0)
                Assert.True(enumerator.MoveNext());
        }

        protected override Type ICollection_Generic_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentOutOfRangeException);

        #endregion

        #region Constructor_IComparer

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_Constructor_IComparer(int count)
        {
            IComparer<TKey> comparer = GetKeyIComparer();
            IDictionary<TKey, TValue> source = GenericIDictionaryFactory(count);
            SortedList<TKey, TValue> copied = new SortedList<TKey, TValue>(source, comparer);
            Assert.Equal(source, copied);
            Assert.Equal(comparer, copied.Comparer);
        }

        #endregion

        #region Constructor_IDictionary

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_Constructor_IDictionary(int count)
        {
            IDictionary<TKey, TValue> source = GenericIDictionaryFactory(count);
            IDictionary<TKey, TValue> copied = new SortedList<TKey, TValue>(source);
            Assert.Equal(source, copied);
        }

        [Fact]
        public void SortedList_Generic_Constructor_NullIDictionary_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SortedList<TKey, TValue>((IDictionary<TKey, TValue>)null));
        }

        #endregion

        #region Constructor_IDictionary_IComparer

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_Constructor_IDictionary_IComparer(int count)
        {
            IComparer<TKey> comparer = GetKeyIComparer();
            IDictionary<TKey, TValue> source = GenericIDictionaryFactory(count);
            SortedList<TKey, TValue> copied = new SortedList<TKey, TValue>(source, comparer);
            Assert.Equal(source, copied);
            Assert.Equal(comparer, copied.Comparer);
        }

        #endregion

        #region Constructor_int

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_Constructor_int(int count)
        {
            SortedList<TKey, TValue> dictionary = new SortedList<TKey, TValue>(count);
            Assert.Equal(0, dictionary.Count);
            Assert.Equal(count, dictionary.Capacity);
        }

        [Fact]
        public void SortedList_Generic_Constructor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SortedList<TKey, TValue>(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SortedList<TKey, TValue>(int.MinValue));
        }

        #endregion

        #region Constructor_int_IComparer

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_Constructor_int_IComparer(int count)
        {
            IComparer<TKey> comparer = GetKeyIComparer();
            SortedList<TKey, TValue> dictionary = new SortedList<TKey, TValue>(count, comparer);
            Assert.Equal(0, dictionary.Count);
            Assert.Equal(comparer, dictionary.Comparer);
            Assert.Equal(count, dictionary.Capacity);
        }

        #endregion

        #region Capacity

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_Capacity_setRoundTrips(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            dictionary.Capacity = count * 2;
            Assert.Equal(count * 2, dictionary.Capacity);

            dictionary.Capacity = count * 2 + 16000;
            Assert.Equal(count * 2 + 16000, dictionary.Capacity);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_Capacity_NegativeValue_ThrowsArgumentOutOfRangeException(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            int capacityBefore = dictionary.Capacity;
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => dictionary.Capacity = -1);
            Assert.Equal(capacityBefore, dictionary.Capacity);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_Capacity_LessThanCount_ThrowsArgumentOutOfRangeException(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory();
            for (int i = 0; i < count; i++)
            {
                AddToCollection(dictionary, 1);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => dictionary.Capacity = i);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_Capacity_GrowsDuringAdds(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory();
            int capacity = 4;
            for (int i = 0; i < count; i++)
            {
                AddToCollection(dictionary, 1);

                //if the array needs to grow, it doubles the size
                if (i == capacity)
                    capacity *= 2;
                if (i <= capacity + 1)
                    Assert.Equal(capacity, dictionary.Capacity);
                else
                    Assert.Equal(i, dictionary.Capacity);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_Capacity_ClearDoesntTrim(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory();
            int capacity = 4;
            for (int i = 0; i < count; i++)
            {
                AddToCollection(dictionary, 1);

                //if the array needs to grow, it doubles the size
                if (i == capacity)
                    capacity *= 2;
            }
            dictionary.Clear();
            if (count == 0)
                Assert.Equal(0, dictionary.Capacity);
            else
                Assert.Equal(capacity, dictionary.Capacity);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_Capacity_ClearTrimsToInitialCapacity(int count)
        {
            SortedList<TKey, TValue> dictionary = new SortedList<TKey, TValue>(count);
            AddToCollection(dictionary, count);
            dictionary.Clear();
            Assert.Equal(count, dictionary.Capacity);
        }

        #endregion

        #region ContainsValue

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_ContainsValue_NotPresent(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            int seed = 4315;
            TValue notPresent = CreateTValue(seed++);
            while (dictionary.Values.Contains(notPresent))
                notPresent = CreateTValue(seed++);
            Assert.False(dictionary.ContainsValue(notPresent));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_ContainsValue_Present(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            int seed = 4315;
            KeyValuePair<TKey, TValue> notPresent = CreateT(seed++);
            while (dictionary.Contains(notPresent))
                notPresent = CreateT(seed++);
            dictionary.Add(notPresent.Key, notPresent.Value);
            Assert.True(dictionary.ContainsValue(notPresent.Value));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_ContainsValue_DefaultValueNotPresent(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            Assert.False(dictionary.ContainsValue(default(TValue)));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_ContainsValue_DefaultValuePresent(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            int seed = 4315;
            TKey notPresent = CreateTKey(seed++);
            while (dictionary.ContainsKey(notPresent))
                notPresent = CreateTKey(seed++);
            dictionary.Add(notPresent, default(TValue));
            Assert.True(dictionary.ContainsValue(default(TValue)));
        }

        #endregion

        #region GetKeyAtIndex

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_GetKeyAtIndex_EveryIndex(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            Assert.All(Enumerable.Range(0, count), index =>
            {
                Assert.Equal(index, dictionary.IndexOfKey(dictionary.GetKeyAtIndex(index)));
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_GetKeyAtIndex_OutOfRangeIndices(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.GetKeyAtIndex(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.GetKeyAtIndex(int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.GetKeyAtIndex(count));
            Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.GetKeyAtIndex(count + 1));
        }

        #endregion

        #region GetValueAtIndex

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_GetValueAtIndex_EveryIndex(int count)
        {
            // Assumes no duplicate elements contained in the dictionary returned by GenericIDictionaryFactory
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            Assert.All(Enumerable.Range(0, count), index =>
            {
                Assert.Equal(index, dictionary.IndexOfValue(dictionary.GetValueAtIndex(index)));
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_GetValueAtIndex_OutOfRangeIndices(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.GetValueAtIndex(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.GetValueAtIndex(int.MinValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.GetValueAtIndex(count));
            Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.GetValueAtIndex(count + 1));
        }

        #endregion

        #region IndexOfKey

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_IndexOf_DefaultKeyNotContainedInSortedList(int count)
        {
            if (DefaultValueAllowed)
            {
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
                TKey key = default(TKey);
                if (dictionary.ContainsKey(key))
                {
                    if (IsReadOnly)
                        return;
                    dictionary.Remove(key);
                }
                Assert.Equal(-1, dictionary.IndexOfKey(key));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_IndexOfKey_EachKey(int count)
        {
            // Assumes no duplicate elements contained in the dictionary returned by GenericIListFactory
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            IList<TKey> keys = dictionary.Keys;
            Assert.All(Enumerable.Range(0, count), index =>
            {
                Assert.Equal(index, dictionary.IndexOfKey(keys[index]));
            });
        }

        #endregion

        #region IndexOfValue

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_IndexOfValue_DefaultValueNotContainedInList(int count)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            TValue value = default(TValue);
            while (dictionary.ContainsValue(value))
            {
                if (IsReadOnly)
                    return;
                dictionary.RemoveAt(dictionary.IndexOfValue(value));
            }
            Assert.Equal(-1, dictionary.IndexOfValue(value));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_IndexOfValue_DefaultValueContainedInList(int count)
        {
            if (!IsReadOnly)
            {
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
                TKey key = GetNewKey(dictionary);
                TValue value = default(TValue);
                while (dictionary.ContainsValue(value))
                    dictionary.RemoveAt(dictionary.IndexOfValue(value));

                List<TKey> keys = dictionary.Keys.ToList();
                keys.Add(key);
                keys.Sort();
                int expectedIndex = keys.IndexOf(key);
                dictionary.Add(key, value);
                Assert.Equal(expectedIndex, dictionary.IndexOfValue(value));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_IndexOfValue_ValueInCollectionMultipleTimes(int count)
        {
            if (!IsReadOnly)
            {
                int seed = 53214;
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
                TKey key1 = CreateTKey(seed++);
                TKey key2 = CreateTKey(seed++);
                TValue value = CreateTValue(seed++);
                while (dictionary.ContainsKey(key1))
                    key1 = CreateTKey(seed++);
                while (key1.Equals(key2) || dictionary.ContainsKey(key2))
                    key2 = CreateTKey(seed++);
                while (dictionary.ContainsValue(value))
                    dictionary.RemoveAt(dictionary.IndexOfValue(value));

                List<TKey> keys = dictionary.Keys.ToList();
                keys.Add(key1);
                keys.Add(key2);
                keys.Sort();
                int expectedIndex = Math.Min(keys.IndexOf(key1), keys.IndexOf(key2));

                dictionary.Add(key1, value);
                dictionary.Add(key2, value);
                Assert.Equal(expectedIndex, dictionary.IndexOfValue(value));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_IndexOfValue_EachValue(int count)
        {
            // Assumes no duplicate elements contained in the dictionary returned by GenericIListFactory
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
            IList<TKey> keys = dictionary.Keys;
            Assert.All(Enumerable.Range(0, count), index =>
            {
                Assert.Equal(index, dictionary.IndexOfValue(dictionary[keys[index]]));
            });
        }

        #endregion

        #region SetValueAtIndex

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_SetValueAtIndex_OnReadOnlySortedList_ThrowsNotSupportedException(int count)
        {
            if (IsReadOnly)
            {
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
                Assert.Throws<NotSupportedException>(() => dictionary.SetValueAtIndex(0, CreateTValue(34543)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_SetValueAtIndex_NonDefaultValueContainedInCollection(int count)
        {
            if (!IsReadOnly)
            {
                int seed = count * 251;
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
                KeyValuePair<TKey, TValue> pair = CreateT(seed++);
                if (!dictionary.ContainsKey(pair.Key))
                {
                    dictionary.Add(pair.Key, pair.Value);
                    count++;
                }
                TValue newValue = CreateTValue(seed++);
                dictionary.SetValueAtIndex(dictionary.IndexOfKey(pair.Key), newValue);
                Assert.Equal(newValue, dictionary[pair.Key]);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_SetValueAtIndex_EveryIndex(int count)
        {
            if (!IsReadOnly)
            {
                int seed = count * 193;
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
                TValue newValue = CreateTValue(seed++);
                Assert.All(Enumerable.Range(0, count), index =>
                {
                    Assert.NotEqual(newValue, dictionary.GetValueAtIndex(index));
                    dictionary.SetValueAtIndex(index, newValue);
                    Assert.Equal(newValue, dictionary.GetValueAtIndex(index));
                });
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_SetValueAtIndex_OutOfRangeIndices(int count)
        {
            if (!IsReadOnly)
            {
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
                Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.SetValueAtIndex(-1, default));
                Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.SetValueAtIndex(int.MinValue, default));
                Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.SetValueAtIndex(count, default));
                Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.SetValueAtIndex(count + 1, default));
            }
        }

        #endregion

        #region RemoveAt

        private void RemoveAt(SortedList<TKey, TValue> dictionary, KeyValuePair<TKey, TValue> element)
        {
            dictionary.RemoveAt(dictionary.IndexOfKey(element.Key));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_RemoveAt_OnReadOnlySortedList_ThrowsNotSupportedException(int count)
        {
            if (IsReadOnly)
            {
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
                Assert.Throws<NotSupportedException>(() => RemoveAt(dictionary, CreateT(34543)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_RemoveAt_NonDefaultValueContainedInCollection(int count)
        {
            if (!IsReadOnly)
            {
                int seed = count * 251;
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
                KeyValuePair<TKey, TValue> pair = CreateT(seed++);
                if (!dictionary.ContainsKey(pair.Key))
                {
                    dictionary.Add(pair.Key, pair.Value);
                    count++;
                }
                RemoveAt(dictionary, pair);
                Assert.Equal(count - 1, dictionary.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_RemoveAt_EveryValue(int count)
        {
            if (!IsReadOnly)
            {
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
                Assert.All(dictionary.ToList(), value =>
                {
                    RemoveAt(dictionary, value);
                });
                Assert.Empty(dictionary);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_RemoveAt_OutOfRangeValues(int count)
        {
            if (!IsReadOnly)
            {
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(count);
                Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.RemoveAt(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.RemoveAt(int.MinValue));
                Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.RemoveAt(count));
                Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.RemoveAt(count + 1));
            }
        }

        #endregion

        #region TrimExcess

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_TrimExcess_OnValidSortedListThatHasntBeenRemovedFrom(int dictionaryLength)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(dictionaryLength);
            dictionary.TrimExcess();
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_TrimExcess_Repeatedly(int dictionaryLength)
        {
            SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(dictionaryLength);
            List<KeyValuePair<TKey, TValue>> expected = dictionary.ToList();
            dictionary.TrimExcess();
            dictionary.TrimExcess();
            dictionary.TrimExcess();
            Assert.True(dictionary.SequenceEqual(expected, GetIEqualityComparer()));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_TrimExcess_AfterRemovingOneElement(int dictionaryLength)
        {
            if (dictionaryLength > 0)
            {
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(dictionaryLength);
                List<KeyValuePair<TKey, TValue>> expected = dictionary.ToList();
                KeyValuePair<TKey, TValue> elementToRemove = dictionary.ElementAt(0);

                dictionary.TrimExcess();
                Assert.True(dictionary.Remove(elementToRemove.Key));
                expected.Remove(elementToRemove);
                dictionary.TrimExcess();

                Assert.True(dictionary.SequenceEqual(expected, GetIEqualityComparer()));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_TrimExcess_AfterClearingAndAddingSomeElementsBack(int dictionaryLength)
        {
            if (dictionaryLength > 0)
            {
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(dictionaryLength);
                dictionary.TrimExcess();
                dictionary.Clear();
                dictionary.TrimExcess();
                Assert.Equal(0, dictionary.Count);

                AddToCollection(dictionary, dictionaryLength / 10);
                dictionary.TrimExcess();
                Assert.Equal(dictionaryLength / 10, dictionary.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_TrimExcess_AfterClearingAndAddingAllElementsBack(int dictionaryLength)
        {
            if (dictionaryLength > 0)
            {
                SortedList<TKey, TValue> dictionary = (SortedList<TKey, TValue>)GenericIDictionaryFactory(dictionaryLength);
                dictionary.TrimExcess();
                dictionary.Clear();
                dictionary.TrimExcess();
                Assert.Equal(0, dictionary.Count);

                AddToCollection(dictionary, dictionaryLength);
                dictionary.TrimExcess();
                Assert.Equal(dictionaryLength, dictionary.Count);
            }
        }

        #endregion

        #region Ordering

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedList_Generic_DictionaryIsProperlySortedAccordingToComparer(int setLength)
        {
            SortedList<TKey, TValue> set = (SortedList<TKey, TValue>)GenericIDictionaryFactory(setLength);
            List<KeyValuePair<TKey, TValue>> expected = set.ToList();
            expected.Sort(GetIComparer());
            int expectedIndex = 0;
            foreach (KeyValuePair<TKey, TValue> value in set)
                Assert.Equal(expected[expectedIndex++], value);
        }

        #endregion

        #region IReadOnlyDictionary<TKey, TValue>.Keys

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IReadOnlyDictionary_Generic_Keys_ContainsAllCorrectKeys(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            IEnumerable<TKey> expected = dictionary.Select((pair) => pair.Key);
            IEnumerable<TKey> keys = ((IReadOnlyDictionary<TKey, TValue>)dictionary).Keys;
            Assert.True(expected.SequenceEqual(keys));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IReadOnlyDictionary_Generic_Values_ContainsAllCorrectValues(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            IEnumerable<TValue> expected = dictionary.Select((pair) => pair.Value);
            IEnumerable<TValue> values = ((IReadOnlyDictionary<TKey, TValue>)dictionary).Values;
            Assert.True(expected.SequenceEqual(values));
        }

        #endregion
    }
}
