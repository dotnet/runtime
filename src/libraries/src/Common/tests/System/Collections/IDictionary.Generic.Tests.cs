// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of any class that implements the generic
    /// IDictionary interface
    /// </summary>
    public abstract partial class IDictionary_Generic_Tests<TKey, TValue> : ICollection_Generic_Tests<KeyValuePair<TKey, TValue>>
    {
        #region IDictionary<TKey, TValue> Helper Methods

        /// <summary>
        /// Creates an instance of an IDictionary{TKey, TValue} that can be used for testing.
        /// </summary>
        /// <returns>An instance of an IDictionary{TKey, TValue} that can be used for testing.</returns>
        protected abstract IDictionary<TKey, TValue> GenericIDictionaryFactory();

        /// <summary>
        /// Creates an instance of an IDictionary{TKey, TValue} that can be used for testing.
        /// </summary>
        /// <param name="count">The number of items that the returned IDictionary{TKey, TValue} contains.</param>
        /// <returns>An instance of an IDictionary{TKey, TValue} that can be used for testing.</returns>
        protected virtual IDictionary<TKey, TValue> GenericIDictionaryFactory(int count)
        {
            IDictionary<TKey, TValue> collection = GenericIDictionaryFactory();
            AddToCollection(collection, count);
            return collection;
        }

        /// <summary>
        /// To be implemented in the concrete Dictionary test classes. Creates an instance of TKey that
        /// is dependent only on the seed passed as input and will return the same key on repeated
        /// calls with the same seed.
        /// </summary>
        protected abstract TKey CreateTKey(int seed);

        /// <summary>
        /// To be implemented in the concrete Dictionary test classes. Creates an instance of TValue that
        /// is dependent only on the seed passed as input and will return the same value on repeated
        /// calls with the same seed.
        /// </summary>
        protected abstract TValue CreateTValue(int seed);

        /// <summary>
        /// Helper method to get a key that doesn't already exist within the dictionary given
        /// </summary>
        protected TKey GetNewKey(IDictionary<TKey, TValue> dictionary)
        {
            int seed = 840;
            TKey missingKey = CreateTKey(seed++);
            while (dictionary.ContainsKey(missingKey) || missingKey.Equals(default(TKey)))
                missingKey = CreateTKey(seed++);
            return missingKey;
        }

        /// <summary>
        /// For a Dictionary, the key comparer is primarily important rather than the KeyValuePair. For this
        /// reason, we rely only on the KeyComparer methods instead of the GetIComparer methods.
        /// </summary>
        public virtual IEqualityComparer<TKey> GetKeyIEqualityComparer()
        {
            return EqualityComparer<TKey>.Default;
        }

        /// <summary>
        /// For a Dictionary, the key comparer is primarily important rather than the KeyValuePair. For this
        /// reason, we rely only on the KeyComparer methods instead of the GetIComparer methods.
        /// </summary>
        public virtual IComparer<TKey> GetKeyIComparer()
        {
            return Comparer<TKey>.Default;
        }

        /// <summary>
        /// Class to provide an indirection around a Key comparer. Allows us to use a key comparer as a KeyValuePair comparer
        /// by only looking at the key of a KeyValuePair.
        /// </summary>
        public class KVPComparer : IEqualityComparer<KeyValuePair<TKey, TValue>>, IComparer<KeyValuePair<TKey, TValue>>
        {
            private IComparer<TKey> _comparer;
            private IEqualityComparer<TKey> _equalityComparer;

            public KVPComparer(IComparer<TKey> comparer, IEqualityComparer<TKey> eq)
            {
                _comparer = comparer;
                _equalityComparer = eq;
            }

            public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            {
                return _comparer.Compare(x.Key, y.Key);
            }

            public bool Equals(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            {
                return _equalityComparer.Equals(x.Key, y.Key);
            }

            public int GetHashCode(KeyValuePair<TKey, TValue> obj)
            {
                return _equalityComparer.GetHashCode(obj.Key);
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey, TValue>> Helper Methods

        protected override ICollection<KeyValuePair<TKey, TValue>> GenericICollectionFactory()
        {
            return GenericIDictionaryFactory();
        }

        protected override ICollection<KeyValuePair<TKey, TValue>> GenericICollectionFactory(int count)
        {
            return GenericIDictionaryFactory(count);
        }

        protected override bool DefaultValueAllowed => false;

        protected override bool DuplicateValuesAllowed => false;

        protected override void AddToCollection(ICollection<KeyValuePair<TKey, TValue>> collection, int numberOfItemsToAdd)
        {
            Assert.False(IsReadOnly);
            int seed = 12353;
            IDictionary<TKey, TValue> casted = (IDictionary<TKey, TValue>)collection;
            int initialCount = casted.Count;
            while ((casted.Count - initialCount) < numberOfItemsToAdd)
            {
                KeyValuePair<TKey, TValue> toAdd = CreateT(seed++);
                while (casted.ContainsKey(toAdd.Key) || Enumerable.Contains(InvalidValues, toAdd))
                    toAdd = CreateT(seed++);
                collection.Add(toAdd);
            }
        }

        protected override IEqualityComparer<KeyValuePair<TKey, TValue>> GetIEqualityComparer()
        {
            return new KVPComparer(GetKeyIComparer(), GetKeyIEqualityComparer());
        }

        protected override IComparer<KeyValuePair<TKey, TValue>> GetIComparer()
        {
            return new KVPComparer(GetKeyIComparer(), GetKeyIEqualityComparer());
        }

        /// <summary>
        /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
        /// </summary>
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
        {
            if ((operations & ModifyOperation.Add) == ModifyOperation.Add)
            {
                yield return (IEnumerable<KeyValuePair<TKey, TValue>> enumerable) =>
                {
                    IDictionary<TKey, TValue> casted = ((IDictionary<TKey, TValue>)enumerable);
                    casted.Add(CreateTKey(12), CreateTValue(5123));
                    return true;
                };
            }
            if ((operations & ModifyOperation.Insert) == ModifyOperation.Insert)
            {
                yield return (IEnumerable<KeyValuePair<TKey, TValue>> enumerable) =>
                {
                    IDictionary<TKey, TValue> casted = ((IDictionary<TKey, TValue>)enumerable);
                    casted[CreateTKey(541)] = CreateTValue(12);
                    return true;
                };
            }
            if ((operations & ModifyOperation.Remove) == ModifyOperation.Remove)
            {
                yield return (IEnumerable<KeyValuePair<TKey, TValue>> enumerable) =>
                {
                    IDictionary<TKey, TValue> casted = ((IDictionary<TKey, TValue>)enumerable);
                    if (casted.Count() > 0)
                    {
                        var keys = casted.Keys.GetEnumerator();
                        keys.MoveNext();
                        casted.Remove(keys.Current);
                        return true;
                    }
                    return false;
                };
            }
            if ((operations & ModifyOperation.Clear) == ModifyOperation.Clear)
            {
                yield return (IEnumerable<KeyValuePair<TKey, TValue>> enumerable) =>
                {
                    IDictionary<TKey, TValue> casted = ((IDictionary<TKey, TValue>)enumerable);
                    if (casted.Count() > 0)
                    {
                        casted.Clear();
                        return true;
                    }
                    return false;
                };
            }
            //throw new InvalidOperationException(string.Format("{0:G}", operations));
        }

        /// <summary>
        /// Used in IDictionary_Generic_Values_Enumeration_ParentDictionaryModifiedInvalidates and 
        /// IDictionary_Generic_Keys_Enumeration_ParentDictionaryModifiedInvalidates.
        /// Some collections (e.g. ConcurrentDictionary) do not throw an InvalidOperationException
        /// when enumerating the Keys or Values property and the parent is modified.
        /// </summary>
        protected virtual bool IDictionary_Generic_Keys_Values_Enumeration_ThrowsInvalidOperation_WhenParentModified => true;

        /// <summary>
        /// Used in IDictionary_Generic_Values_ModifyingTheDictionaryUpdatesTheCollection and 
        /// IDictionary_Generic_Keys_ModifyingTheDictionaryUpdatesTheCollection.
        /// Some collections (e.g ConcurrentDictionary) use iterators in the Keys and Values properties,
        /// and do not respond to updates in the base collection.
        /// </summary>
        protected virtual bool IDictionary_Generic_Keys_Values_ModifyingTheDictionaryUpdatesTheCollection => true;

        /// <summary>
        /// Used in IDictionary_Generic_Keys_Enumeration_Reset and IDictionary_Generic_Values_Enumeration_Reset.
        /// Typically, the support for Reset in enumerators for the Keys and Values depend on the support for it
        /// in the parent dictionary. However, some collections (e.g. ConcurrentDictionary) don't.
        /// </summary>
        protected virtual bool IDictionary_Generic_Keys_Values_Enumeration_ResetImplemented => ResetImplemented;

        #endregion

        #region Item Getter

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_ItemGet_DefaultKey(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            if (!DefaultValueAllowed)
            {
                Assert.Throws<ArgumentNullException>(() => dictionary[default(TKey)]);
            }
            else
            {
                TValue value = CreateTValue(3452);
                dictionary[default(TKey)] = value;
                Assert.Equal(value, dictionary[default(TKey)]);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_ItemGet_MissingNonDefaultKey_ThrowsKeyNotFoundException(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            TKey missingKey = GetNewKey(dictionary);
            Assert.Throws<KeyNotFoundException>(() => dictionary[missingKey]);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_ItemGet_MissingDefaultKey_ThrowsKeyNotFoundException(int count)
        {
            if (DefaultValueAllowed)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = default(TKey);
                while (dictionary.ContainsKey(missingKey))
                    dictionary.Remove(missingKey);
                Assert.Throws<KeyNotFoundException>(() => dictionary[missingKey]);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_ItemGet_PresentKeyReturnsCorrectValue(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                Assert.Equal(pair.Value, dictionary[pair.Key]);
            }
        }

        #endregion

        #region Item Setter

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_ItemSet_DefaultKey(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            if (!DefaultValueAllowed)
            {
                Assert.Throws<ArgumentNullException>(() => dictionary[default(TKey)] = CreateTValue(3));
            }
            else
            {
                TValue value = CreateTValue(3452);
                dictionary[default(TKey)] = value;
                Assert.Equal(value, dictionary[default(TKey)]);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_ItemSet_OnReadOnlyDictionary_ThrowsNotSupportedException(int count)
        {
            if (IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = GetNewKey(dictionary);
                Assert.Throws<NotSupportedException>(() => dictionary[missingKey] = CreateTValue(5312));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_ItemSet_AddsNewValueWhenNotPresent(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            TKey missingKey = GetNewKey(dictionary);
            dictionary[missingKey] = CreateTValue(543);
            Assert.Equal(count + 1, dictionary.Count);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_ItemSet_ReplacesExistingValueWhenPresent(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            TKey existingKey = GetNewKey(dictionary);
            dictionary.Add(existingKey, CreateTValue(5342));
            TValue newValue = CreateTValue(1234);
            dictionary[existingKey] = newValue;
            Assert.Equal(count + 1, dictionary.Count);
            Assert.Equal(newValue, dictionary[existingKey]);
        }

        #endregion

        #region Keys

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Keys_ContainsAllCorrectKeys(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            IEnumerable<TKey> expected = dictionary.Select((pair) => pair.Key);
            Assert.True(expected.SequenceEqual(dictionary.Keys));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Keys_ModifyingTheDictionaryUpdatesTheCollection(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            ICollection<TKey> keys = dictionary.Keys;
            int previousCount = keys.Count;
            if (count > 0)
                Assert.NotEmpty(keys);
            dictionary.Clear();
            if (IDictionary_Generic_Keys_Values_ModifyingTheDictionaryUpdatesTheCollection)
            {
                Assert.Empty(keys);
            }
            else
            {
                Assert.Equal(previousCount, keys.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Keys_Enumeration_ParentDictionaryModifiedInvalidates(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            ICollection<TKey> keys = dictionary.Keys;
            IEnumerator<TKey> keysEnum = keys.GetEnumerator();
            dictionary.Add(GetNewKey(dictionary), CreateTValue(3432));
            if (IDictionary_Generic_Keys_Values_Enumeration_ThrowsInvalidOperation_WhenParentModified)
            {
                Assert.Throws<InvalidOperationException>(() => keysEnum.MoveNext());
                Assert.Throws<InvalidOperationException>(() => keysEnum.Reset());
            }
            else
            {
                keysEnum.MoveNext();
                keysEnum.Reset();
            }
            var cur = keysEnum.Current;
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Keys_IsReadOnly(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            ICollection<TKey> keys = dictionary.Keys;
            Assert.True(keys.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => keys.Add(CreateTKey(11)));
            Assert.Throws<NotSupportedException>(() => keys.Clear());
            Assert.Throws<NotSupportedException>(() => keys.Remove(CreateTKey(11)));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Keys_Enumeration_Reset(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            ICollection<TKey> keys = dictionary.Keys;
            var enumerator = keys.GetEnumerator();
            if (IDictionary_Generic_Keys_Values_Enumeration_ResetImplemented)
                enumerator.Reset();
            else
                Assert.Throws<NotSupportedException>(() => enumerator.Reset());
        }

        #endregion

        #region Values

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Values_ContainsAllCorrectValues(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            IEnumerable<TValue> expected = dictionary.Select((pair) => pair.Value);
            Assert.True(expected.SequenceEqual(dictionary.Values));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Values_IncludeDuplicatesMultipleTimes(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            int seed = 431;
            foreach (KeyValuePair<TKey, TValue> pair in dictionary.ToList())
            {
                TKey missingKey = CreateTKey(seed++);
                while (dictionary.ContainsKey(missingKey))
                    missingKey = CreateTKey(seed++);
                dictionary.Add(missingKey, pair.Value);
            }
            Assert.Equal(count * 2, dictionary.Values.Count);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Values_ModifyingTheDictionaryUpdatesTheCollection(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            ICollection<TValue> values = dictionary.Values;
            int previousCount = values.Count;
            if (count > 0)
                Assert.NotEmpty(values);
            dictionary.Clear();
            if (IDictionary_Generic_Keys_Values_ModifyingTheDictionaryUpdatesTheCollection)
            {
                Assert.Empty(values);
            }
            else
            {
                Assert.Equal(previousCount, values.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Values_Enumeration_ParentDictionaryModifiedInvalidates(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            ICollection<TValue> values = dictionary.Values;
            IEnumerator<TValue> valuesEnum = values.GetEnumerator();
            dictionary.Add(GetNewKey(dictionary), CreateTValue(3432));
            if (IDictionary_Generic_Keys_Values_Enumeration_ThrowsInvalidOperation_WhenParentModified)
            {
                Assert.Throws<InvalidOperationException>(() => valuesEnum.MoveNext());
                Assert.Throws<InvalidOperationException>(() => valuesEnum.Reset());
            }
            else
            {
                valuesEnum.MoveNext();
                valuesEnum.Reset();
            }
            var cur = valuesEnum.Current;
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Values_IsReadOnly(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            ICollection<TValue> values = dictionary.Values;
            Assert.True(values.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => values.Add(CreateTValue(11)));
            Assert.Throws<NotSupportedException>(() => values.Clear());
            Assert.Throws<NotSupportedException>(() => values.Remove(CreateTValue(11)));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Values_Enumeration_Reset(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            ICollection<TValue> values = dictionary.Values;
            var enumerator = values.GetEnumerator();
            if (IDictionary_Generic_Keys_Values_Enumeration_ResetImplemented)
                enumerator.Reset();
            else
                Assert.Throws<NotSupportedException>(() => enumerator.Reset());
        }

        #endregion

        #region Add(TKey, TValue)

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Add_OnReadOnlyDictionary_ThrowsNotSupportedException(int count)
        {
            if (IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                Assert.Throws<NotSupportedException>(() => dictionary.Add(CreateTKey(0), CreateTValue(0)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Add_DefaultKey_DefaultValue(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            TKey missingKey = default(TKey);
            TValue value = default(TValue);
            if (DefaultValueAllowed && !IsReadOnly)
            {
                dictionary.Add(missingKey, value);
                Assert.Equal(count + 1, dictionary.Count);
                Assert.Equal(value, dictionary[missingKey]);
            }
            else if (!IsReadOnly)
            {
                Assert.Throws<ArgumentNullException>(() => dictionary.Add(missingKey, value));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Add_DefaultKey_NonDefaultValue(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            TKey missingKey = default(TKey);
            TValue value = CreateTValue(1456);
            if (DefaultValueAllowed && !IsReadOnly)
            {
                dictionary.Add(missingKey, value);
                Assert.Equal(count + 1, dictionary.Count);
                Assert.Equal(value, dictionary[missingKey]);
            }
            else if (!IsReadOnly)
            {
                Assert.Throws<ArgumentNullException>(() => dictionary.Add(missingKey, value));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Add_NonDefaultKey_DefaultValue(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = GetNewKey(dictionary);
                TValue value = default(TValue);
                dictionary.Add(missingKey, value);
                Assert.Equal(count + 1, dictionary.Count);
                Assert.Equal(value, dictionary[missingKey]);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Add_NonDefaultKey_NonDefaultValue(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = GetNewKey(dictionary);
                TValue value = CreateTValue(1342);
                dictionary.Add(missingKey, value);
                Assert.Equal(count + 1, dictionary.Count);
                Assert.Equal(value, dictionary[missingKey]);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Add_DuplicateValue(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                int seed = 321;
                TValue duplicate = CreateTValue(seed++);
                while (dictionary.Values.Contains(duplicate))
                    duplicate = CreateTValue(seed++);
                dictionary.Add(GetNewKey(dictionary), duplicate);
                dictionary.Add(GetNewKey(dictionary), duplicate);
                Assert.Equal(2, dictionary.Values.Count((value) => value.Equals(duplicate)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_Add_DuplicateKey(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = GetNewKey(dictionary);
                dictionary.Add(missingKey, CreateTValue(34251));
                Assert.Throws<ArgumentException>(() => dictionary.Add(missingKey, CreateTValue(134)));
            }
        }

        #endregion

        #region ContainsKey

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_ContainsKey_ValidKeyNotContainedInDictionary(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = GetNewKey(dictionary);
                Assert.False(dictionary.ContainsKey(missingKey));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_ContainsKey_ValidKeyContainedInDictionary(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = GetNewKey(dictionary);
                dictionary.Add(missingKey, CreateTValue(34251));
                Assert.True(dictionary.ContainsKey(missingKey));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_ContainsKey_DefaultKeyNotContainedInDictionary(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            if (DefaultValueAllowed)
            {
                // returns false
                TKey missingKey = default(TKey);
                while (dictionary.ContainsKey(missingKey))
                    dictionary.Remove(missingKey);
                Assert.False(dictionary.ContainsKey(missingKey));
            }
            else
            {
                // throws ArgumentNullException
                Assert.Throws<ArgumentNullException>(() => dictionary.ContainsKey(default(TKey)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_ContainsKey_DefaultKeyContainedInDictionary(int count)
        {
            if (DefaultValueAllowed && !IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = default(TKey);
                if (!dictionary.ContainsKey(missingKey))
                    dictionary.Add(missingKey, CreateTValue(5341));
                Assert.True(dictionary.ContainsKey(missingKey));
            }
        }

        #endregion

        #region Remove(TKey)

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_RemoveKey_OnReadOnlyDictionary_ThrowsNotSupportedException(int count)
        {
            if (IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                Assert.Throws<NotSupportedException>(() => dictionary.Remove(CreateTKey(0)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_RemoveKey_EveryKey(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                Assert.All(dictionary.Keys.ToList(), key =>
                {
                    Assert.True(dictionary.Remove(key));
                });
                Assert.Empty(dictionary);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_RemoveKey_ValidKeyNotContainedInDictionary(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = GetNewKey(dictionary);
                Assert.False(dictionary.Remove(missingKey));
                Assert.Equal(count, dictionary.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_RemoveKey_ValidKeyContainedInDictionary(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = GetNewKey(dictionary);
                dictionary.Add(missingKey, CreateTValue(34251));
                Assert.True(dictionary.Remove(missingKey));
                Assert.Equal(count, dictionary.Count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_RemoveKey_DefaultKeyNotContainedInDictionary(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                if (DefaultValueAllowed)
                {
                    TKey missingKey = default(TKey);
                    while (dictionary.ContainsKey(missingKey))
                        dictionary.Remove(missingKey);
                    Assert.False(dictionary.Remove(missingKey));
                }
                else
                {
                    Assert.Throws<ArgumentNullException>(() => dictionary.Remove(default(TKey)));
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_RemoveKey_DefaultKeyContainedInDictionary(int count)
        {
            if (DefaultValueAllowed && !IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = default(TKey);
                dictionary.TryAdd(missingKey, CreateTValue(5341));
                Assert.True(dictionary.Remove(missingKey));
            }
        }

        #endregion

        #region TryGetValue

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_TryGetValue_ValidKeyNotContainedInDictionary(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            TKey missingKey = GetNewKey(dictionary);
            TValue value = CreateTValue(5123);
            TValue outValue;
            Assert.False(dictionary.TryGetValue(missingKey, out outValue));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_TryGetValue_ValidKeyContainedInDictionary(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = GetNewKey(dictionary);
                TValue value = CreateTValue(5123);
                TValue outValue;
                dictionary.TryAdd(missingKey, value);
                Assert.True(dictionary.TryGetValue(missingKey, out outValue));
                Assert.Equal(value, outValue);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_TryGetValue_DefaultKeyNotContainedInDictionary(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            TValue outValue;
            if (DefaultValueAllowed)
            {
                TKey missingKey = default(TKey);
                while (dictionary.ContainsKey(missingKey))
                    dictionary.Remove(missingKey);
                Assert.False(dictionary.TryGetValue(missingKey, out outValue));
            }
            else
            {
                Assert.Throws<ArgumentNullException>(() => dictionary.TryGetValue(default(TKey), out outValue));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IDictionary_Generic_TryGetValue_DefaultKeyContainedInDictionary(int count)
        {
            if (DefaultValueAllowed && !IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = default(TKey);
                TValue value = CreateTValue(5123);
                TValue outValue;
                dictionary.TryAdd(missingKey, value);
                Assert.True(dictionary.TryGetValue(missingKey, out outValue));
                Assert.Equal(value, outValue);
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey, TValue>>

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Contains_ValidPresentKeyWithDefaultValue(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = GetNewKey(dictionary);
                dictionary.Add(missingKey, default(TValue));
                Assert.True(dictionary.Contains(new KeyValuePair<TKey, TValue>(missingKey, default(TValue))));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Remove_ValidPresentKeyWithDifferentValue(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = GetNewKey(dictionary);
                TValue present = CreateTValue(324);
                TValue missing = CreateTValue(5612);
                while (present.Equals(missing))
                    missing = CreateTValue(5612);
                dictionary.Add(missingKey, present);
                Assert.False(dictionary.Remove(new KeyValuePair<TKey, TValue>(missingKey, missing)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_Generic_Contains_ValidPresentKeyWithDifferentValue(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
                TKey missingKey = GetNewKey(dictionary);
                TValue present = CreateTValue(324);
                TValue missing = CreateTValue(5612);
                while (present.Equals(missing))
                    missing = CreateTValue(5612);
                dictionary.Add(missingKey, present);
                Assert.False(dictionary.Contains(new KeyValuePair<TKey, TValue>(missingKey, missing)));
            }
        }

        #endregion

        #region ICollection

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void ICollection_NonGeneric_CopyTo(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[count];
            object[] objarray = new object[count];
            dictionary.CopyTo(array, 0);
            ((ICollection)dictionary).CopyTo(objarray, 0);
            for (int i = 0; i < count; i++)
                Assert.Equal(array[i], (KeyValuePair<TKey, TValue>)(objarray[i]));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public override void ICollection_Generic_Contains_DefaultValueWhenNotAllowed(int count)
        {
            ICollection<KeyValuePair<TKey, TValue>> collection = GenericIDictionaryFactory(count);
            if (!DefaultValueAllowed && !IsReadOnly)
            {
                if (DefaultValueWhenNotAllowed_Throws)
                    Assert.Throws<ArgumentNullException>(() => collection.Contains(default(KeyValuePair<TKey, TValue>)));
                else
                    Assert.False(collection.Remove(default(KeyValuePair<TKey, TValue>)));
            }
        }

        #endregion
    }
}
