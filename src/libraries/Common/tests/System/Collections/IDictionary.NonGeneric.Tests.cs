// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of any class that implements the nongeneric
    /// IDictionary interface
    /// </summary>
    public abstract class IDictionary_NonGeneric_Tests : ICollection_NonGeneric_Tests
    {
        #region IDictionary Helper Methods

        /// <summary>
        /// To be implemented in the concrete Dictionary test classes. Creates an instance of TValue that
        /// is dependent only on the seed passed as input and will return the same value on repeated
        /// calls with the same seed.
        /// </summary>
        protected virtual object CreateTKey(int seed)
        {
            if (seed % 2 == 0)
            {
                int stringLength = seed % 10 + 5;
                Random rand = new Random(seed);
                byte[] bytes = new byte[stringLength];
                rand.NextBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
            else
            {
                Random rand = new Random(seed);
                return rand.Next();
            }
        }

        /// <summary>
        /// To be implemented in the concrete Dictionary test classes. Creates an instance of TValue that
        /// is dependent only on the seed passed as input and will return the same value on repeated
        /// calls with the same seed.
        /// </summary>
        protected virtual object CreateTValue(int seed)
        {
            return CreateTKey(seed);
        }

        /// <summary>
        /// Creates an instance of an IDictionary that can be used for testing.
        /// </summary>
        /// <returns>An instance of an IDictionary that can be used for testing.</returns>
        protected abstract IDictionary NonGenericIDictionaryFactory();

        /// <summary>
        /// Creates an instance of an IDictionary that can be used for testing.
        /// </summary>
        /// <param name="count">The number of items that the returned IDictionary contains.</param>
        /// <returns>An instance of an IDictionary that can be used for testing.</returns>
        protected virtual IDictionary NonGenericIDictionaryFactory(int count)
        {
            IDictionary collection = NonGenericIDictionaryFactory();
            AddToCollection(collection, count);
            return collection;
        }

        /// <summary>
        /// Helper method to get a key that doesn't already exist within the dictionary
        /// </summary>
        /// <param name="dictionary"></param>
        protected object GetNewKey(IDictionary dictionary)
        {
            int seed = 840;
            object missingKey = CreateTKey(seed++);
            while (dictionary.Contains(missingKey) || missingKey.Equals(null))
                missingKey = CreateTKey(seed++);
            return missingKey;
        }

        #endregion

        #region ICollection Helper Methods

        protected override ICollection NonGenericICollectionFactory()
        {
            return NonGenericIDictionaryFactory();
        }

        protected override bool DuplicateValuesAllowed { get { return false; } }
        protected override bool NullAllowed { get { return false; } }
        protected override bool Enumerator_Current_UndefinedOperation_Throws { get { return true; } }
        protected override bool ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowsArgumentException { get { return true; } }
        protected override void AddToCollection(ICollection collection, int numberOfItemsToAdd)
        {
            Assert.False(IsReadOnly);
            int seed = 12353;
            IDictionary casted = (IDictionary)collection;
            int initialCount = casted.Count;
            while ((casted.Count - initialCount) < numberOfItemsToAdd)
            {
                object key = CreateTKey(seed++);
                object value = CreateTValue(seed++);
                while (casted.Contains(key) || Enumerable.Contains(InvalidValues, key))
                    key = CreateTKey(seed++);
                casted.Add(key, value);
            }
        }

        /// <summary>
        /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
        /// </summary>
        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables
        {
            get
            {
                yield return (IEnumerable enumerable) => {
                    IDictionary casted = ((IDictionary)enumerable);
                    casted.Add(CreateTKey(12), CreateTValue(5123));
                    return true;
                };
                yield return (IEnumerable enumerable) => {
                    IDictionary casted = ((IDictionary)enumerable);
                    casted[CreateTKey(541)] = CreateTValue(12);
                    return true;
                };
                yield return (IEnumerable enumerable) => {
                    IDictionary casted = ((IDictionary)enumerable);
                    if (casted.Count > 0)
                    {
                        var keys = casted.Keys.GetEnumerator();
                        keys.MoveNext();
                        casted.Remove(keys.Current); return true;
                    }
                    return false;
                };
                yield return (IEnumerable enumerable) => {
                    IDictionary casted = ((IDictionary)enumerable);
                    if (casted.Count > 0)
                    {
                        casted.Clear();
                        return true;
                    }
                    return false;
                };
            }
        }

        #endregion

        #region IsFixedSize

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_IsFixedSize_Validity(int count)
        {
            IDictionary collection = NonGenericIDictionaryFactory(count);
            Assert.False(collection.IsFixedSize);
        }

        #endregion

        #region IsReadOnly

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_IsReadOnly_Validity(int count)
        {
            IDictionary collection = NonGenericIDictionaryFactory(count);
            Assert.Equal(IsReadOnly, collection.IsReadOnly);
        }

        #endregion

        #region Item Getter

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemGet_NullKey(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            if (!NullAllowed)
            {
                Assert.Throws<ArgumentNullException>(() => dictionary[null]);
            }
            else
            {
                object value = CreateTValue(3452);
                dictionary[null] = value;
                Assert.Equal(value, dictionary[null]);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemGet_MissingNonNullKey_ThrowsKeyNotFoundException(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            object missingKey = GetNewKey(dictionary);
            Assert.Equal(null, dictionary[missingKey]);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemGet_MissingNullKey_ThrowsKeyNotFoundException(int count)
        {
            if (NullAllowed)
            {
                IDictionary dictionary = NonGenericIDictionaryFactory(count);
                object missingKey = null;
                if (dictionary.Contains(missingKey))
                    dictionary.Remove(missingKey);
                Assert.Equal(null, dictionary[missingKey]);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemGet_PresenobjectReturnsCorrecobject(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            foreach (DictionaryEntry pair in dictionary)
            {
                Assert.Equal(pair.Value, dictionary[pair.Key]);
            }
        }

        #endregion

        #region Item Setter

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemSet_NullKey(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            if (!NullAllowed)
            {
                Assert.Throws<ArgumentNullException>(() => dictionary[null] = CreateTValue(3));
            }
            else
            {
                object value = CreateTValue(3452);
                dictionary[null] = value;
                Assert.Equal(value, dictionary[null]);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemSet_OnReadOnlyDictionary_ThrowsNotSupportedException(int count)
        {
            if (IsReadOnly)
            {
                IDictionary dictionary = NonGenericIDictionaryFactory(count);
                object missingKey = GetNewKey(dictionary);
                Assert.Throws<NotSupportedException>(() => dictionary[missingKey] = CreateTValue(5312));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemSet_AddsNewValueWhenNotPresent(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            object missingKey = GetNewKey(dictionary);
            dictionary[missingKey] = CreateTValue(543);
            Assert.Equal(count + 1, dictionary.Count);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemSet_ReplacesExistingValueWhenPresent(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            object existingKey = GetNewKey(dictionary);
            dictionary.Add(existingKey, CreateTValue(5342));
            object newValue = CreateTValue(1234);
            dictionary[existingKey] = newValue;
            Assert.Equal(count + 1, dictionary.Count);
            Assert.Equal(newValue, dictionary[existingKey]);
        }

        #endregion

        #region Keys

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Keys_ContainsAllCorrectobjects(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            object[] expected = new object[count];
            dictionary.Keys.CopyTo(expected, 0);
            int i = 0;
            foreach (object key in dictionary.Keys)
                Assert.Equal(expected[i++], key);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Keys_ModifyingTheDictionaryUpdatesTheCollection(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            ICollection keys = dictionary.Keys;
            dictionary.Clear();
            Assert.Empty(keys);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Keys_Enumeration_ParentDictionaryModifiedInvalidatesEnumerator(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            ICollection keys = dictionary.Keys;
            IEnumerator keysEnum = keys.GetEnumerator();
            dictionary.Add(GetNewKey(dictionary), CreateTValue(3432));
            Assert.Throws<InvalidOperationException>(() => keysEnum.MoveNext());
            Assert.Throws<InvalidOperationException>(() => keysEnum.Reset());
            Assert.Throws<InvalidOperationException>(() => keysEnum.Current);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Keys_Enumeration_Reset(int count)
        {
            IEnumerator enumerator = NonGenericIDictionaryFactory(count).Keys.GetEnumerator();
            if (ResetImplemented)
                enumerator.Reset();
            else
                Assert.Throws<NotSupportedException>(() => enumerator.Reset());
        }

        #endregion

        #region Values

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Values_ContainsAllCorrecobjects(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            object[] expected = new object[count];
            dictionary.Values.CopyTo(expected, 0);
            int i = 0;
            foreach (object value in dictionary.Values)
                Assert.Equal(expected[i++], value);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Values_IncludeDuplicatesMultipleTimes(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            List<DictionaryEntry> entries = new List<DictionaryEntry>();

            foreach (DictionaryEntry pair in dictionary)
                entries.Add(pair);
            foreach (DictionaryEntry pair in entries)
            {  
                object missingKey = GetNewKey(dictionary);
                dictionary.Add(missingKey, (pair.Value));
            }
            Assert.Equal(count * 2, dictionary.Values.Count);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Values_ModifyingTheDictionaryUpdatesTheCollection(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            ICollection values = dictionary.Values;
            dictionary.Clear();
            Assert.Empty(values);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public virtual void IDictionary_NonGeneric_Values_Enumeration_ParentDictionaryModifiedInvalidatesEnumerator(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            ICollection values = dictionary.Values;
            IEnumerator valuesEnum = values.GetEnumerator();
            dictionary.Add(GetNewKey(dictionary), CreateTValue(3432));
            Assert.Throws<InvalidOperationException>(() => valuesEnum.MoveNext());
            Assert.Throws<InvalidOperationException>(() => valuesEnum.Reset());
            Assert.Throws<InvalidOperationException>(() => valuesEnum.Current);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Values_Enumeration_Reset(int count)
        {
            IEnumerator enumerator = NonGenericIDictionaryFactory(count).Values.GetEnumerator();
            if (ResetImplemented)
                enumerator.Reset();
            else
                Assert.Throws<NotSupportedException>(() => enumerator.Reset());
        }

        #endregion

        #region Add

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Add_OnReadOnlyDictionary_ThrowsNotSupportedException(int count)
        {
            if (IsReadOnly)
            {
                IDictionary dictionary = NonGenericIDictionaryFactory(count);
                Assert.Throws<NotSupportedException>(() => dictionary.Add(CreateTKey(0), CreateTValue(0)));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Add_NullKey_NullValue(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            object missingKey = null;
            object value = null;
            if (NullAllowed && !IsReadOnly)
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
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Add_NullKey_NonNullValue(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            object missingKey = null;
            object value = CreateTValue(1456);
            if (NullAllowed && !IsReadOnly)
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
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Add_NonNullKey_NullValue(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = NonGenericIDictionaryFactory(count);
                object missingKey = GetNewKey(dictionary);
                object value = null;
                dictionary.Add(missingKey, value);
                Assert.Equal(count + 1, dictionary.Count);
                Assert.Equal(value, dictionary[missingKey]);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Add_NonNullKey_NonNulValue(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = NonGenericIDictionaryFactory(count);
                object missingKey = GetNewKey(dictionary);
                object value = CreateTValue(1342);
                dictionary.Add(missingKey, value);
                Assert.Equal(count + 1, dictionary.Count);
                Assert.Equal(value, dictionary[missingKey]);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Add_DuplicateKey(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = NonGenericIDictionaryFactory(count);
                object missingKey = GetNewKey(dictionary);
                dictionary.Add(missingKey, CreateTValue(34251));
                Assert.Throws<ArgumentException>(() => dictionary.Add(missingKey, CreateTValue(134)));
            }
        }

        #endregion

        #region Clear

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Clear(int count)
        {
            IDictionary collection = NonGenericIDictionaryFactory(count);
            if (IsReadOnly)
            {
                Assert.Throws<NotSupportedException>(() => collection.Clear());
                Assert.Equal(count, collection.Count);
            }
            else
            {
                collection.Clear();
                Assert.Equal(0, collection.Count);
            }
        }

        #endregion

        #region Contains

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Contains_ValidKeyNotContainedInDictionary(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = NonGenericIDictionaryFactory(count);
                object missingKey = GetNewKey(dictionary);
                Assert.False(dictionary.Contains(missingKey));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Contains_ValidKeyContainedInDictionary(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = NonGenericIDictionaryFactory(count);
                object missingKey = GetNewKey(dictionary);
                dictionary.Add(missingKey, CreateTValue(34251));
                Assert.True(dictionary.Contains(missingKey));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Contains_NullKeyNotContainedInDictionary(int count)
        {
            IDictionary dictionary = NonGenericIDictionaryFactory(count);
            if (NullAllowed)
            {
                // returns false
                object missingKey = null;
                while (dictionary.Contains(missingKey))
                    dictionary.Remove(missingKey);
                Assert.False(dictionary.Contains(missingKey));
            }
            else
            {
                // throws ArgumentNullException
                Assert.Throws<ArgumentNullException>(() => dictionary.Contains(null));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Contains_NullKeyContainedInDictionary(int count)
        {
            if (NullAllowed && !IsReadOnly)
            {
                IDictionary dictionary = NonGenericIDictionaryFactory(count);
                object missingKey = null;
                if (!dictionary.Contains(missingKey))
                    dictionary.Add(missingKey, CreateTValue(5341));
                Assert.True(dictionary.Contains(missingKey));
            }
        }

        #endregion

        #region IDictionaryEnumerator

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_IDictionaryEnumerator_Current_FromStartToFinish(int count)
        {
            IDictionaryEnumerator enumerator = NonGenericIDictionaryFactory(count).GetEnumerator();
            object current, key, value, entry;
            while (enumerator.MoveNext())
            {
                current = enumerator.Current;
                key = enumerator.Key;
                value = enumerator.Value;
                entry = enumerator.Entry;
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_IDictionaryEnumerator_Current_ReturnsSameValueOnRepeatedCalls(int count)
        {
            IDictionaryEnumerator enumerator = NonGenericIDictionaryFactory(count).GetEnumerator();
            while (enumerator.MoveNext())
            {
                object current = enumerator.Current;
                Assert.Equal(current, enumerator.Current);
                Assert.Equal(current, enumerator.Current);
                Assert.Equal(current, enumerator.Current);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public virtual void IDictionary_NonGeneric_IDictionaryEnumerator_Current_BeforeFirstMoveNext_UndefinedBehavior(int count)
        {
            object current, key, value, entry;
            IDictionaryEnumerator enumerator = NonGenericIDictionaryFactory(count).GetEnumerator();
            if (Enumerator_Current_UndefinedOperation_Throws)
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                Assert.Throws<InvalidOperationException>(() => enumerator.Key);
                Assert.Throws<InvalidOperationException>(() => enumerator.Value);
                Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
            }
            else
            {
                current = enumerator.Current;
                key = enumerator.Key;
                value = enumerator.Value;
                entry = enumerator.Entry;
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public virtual void IDictionary_NonGeneric_IDictionaryEnumerator_Current_AfterEndOfEnumerable_UndefinedBehavior(int count)
        {
            object current, key, value, entry;
            IDictionaryEnumerator enumerator = NonGenericIDictionaryFactory(count).GetEnumerator();
            while (enumerator.MoveNext()) ;
            if (Enumerator_Current_UndefinedOperation_Throws)
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                Assert.Throws<InvalidOperationException>(() => enumerator.Key);
                Assert.Throws<InvalidOperationException>(() => enumerator.Value);
                Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
            }
            else
            {
                current = enumerator.Current;
                key = enumerator.Key;
                value = enumerator.Value;
                entry = enumerator.Entry;
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public virtual void IDictionary_NonGeneric_IDictionaryEnumerator_Current_ModifiedDuringEnumeration_UndefinedBehavior(int count)
        {
            Assert.All(ModifyEnumerables, ModifyEnumerable =>
            {
                object current, key, value, entry;
                IDictionary enumerable = NonGenericIDictionaryFactory(count);
                IDictionaryEnumerator enumerator = enumerable.GetEnumerator();
                if (ModifyEnumerable(enumerable))
                {
                    if (Enumerator_Current_UndefinedOperation_Throws)
                    {
                        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                        Assert.Throws<InvalidOperationException>(() => enumerator.Key);
                        Assert.Throws<InvalidOperationException>(() => enumerator.Value);
                        Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
                    }
                    else
                    {
                        current = enumerator.Current;
                        key = enumerator.Key;
                        value = enumerator.Value;
                        entry = enumerator.Entry;
                    }
                }
            });
        }

        #endregion
    }
}