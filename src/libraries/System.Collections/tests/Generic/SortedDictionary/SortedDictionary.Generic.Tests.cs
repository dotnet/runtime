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
    public abstract class SortedDictionary_Generic_Tests<TKey, TValue> : IDictionary_Generic_Tests<TKey, TValue>
    {
        #region IDictionary<TKey, TValue> Helper Methods

        protected override bool DefaultValueWhenNotAllowed_Throws { get { return false; } }

        protected override IDictionary<TKey, TValue> GenericIDictionaryFactory()
        {
            return new SortedDictionary<TKey, TValue>();
        }

        #endregion

        #region Constructors

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedDictionary_Generic_Constructor_IComparer(int count)
        {
            IComparer<TKey> comparer = GetKeyIComparer();
            IDictionary<TKey, TValue> source = GenericIDictionaryFactory(count);
            SortedDictionary<TKey, TValue> copied = new SortedDictionary<TKey, TValue>(source, comparer);
            Assert.Equal(source, copied);
            Assert.Equal(comparer, copied.Comparer);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedDictionary_Generic_Constructor_IDictionary(int count)
        {
            IDictionary<TKey, TValue> source = GenericIDictionaryFactory(count);
            IDictionary<TKey, TValue> copied = new SortedDictionary<TKey, TValue>(source);
            Assert.Equal(source, copied);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedDictionary_Generic_Constructor_NullIDictionary_ThrowsArgumentNullException(int count)
        {
            Assert.Throws<ArgumentNullException>(() => new SortedDictionary<TKey, TValue>((IDictionary<TKey, TValue>)null));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedDictionary_Generic_Constructor_IDictionary_IComparer(int count)
        {
            IComparer<TKey> comparer = GetKeyIComparer();
            IDictionary<TKey, TValue> source = GenericIDictionaryFactory(count);
            SortedDictionary<TKey, TValue> copied = new SortedDictionary<TKey, TValue>(source, comparer);
            Assert.Equal(source, copied);
            Assert.Equal(comparer, copied.Comparer);
        }

        #endregion

        #region ContainsValue

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedDictionary_Generic_ContainsValue_NotPresent(int count)
        {
            SortedDictionary<TKey, TValue> dictionary = (SortedDictionary<TKey, TValue>)GenericIDictionaryFactory(count);
            int seed = 4315;
            TValue notPresent = CreateTValue(seed++);
            while (dictionary.Values.Contains(notPresent))
                notPresent = CreateTValue(seed++);
            Assert.False(dictionary.ContainsValue(notPresent));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedDictionary_Generic_ContainsValue_Present(int count)
        {
            SortedDictionary<TKey, TValue> dictionary = (SortedDictionary<TKey, TValue>)GenericIDictionaryFactory(count);
            int seed = 4315;
            KeyValuePair<TKey, TValue> notPresent = CreateT(seed++);
            while (dictionary.Contains(notPresent))
                notPresent = CreateT(seed++);
            dictionary.Add(notPresent.Key, notPresent.Value);
            Assert.True(dictionary.ContainsValue(notPresent.Value));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedDictionary_Generic_ContainsValue_DefaultValueNotPresent(int count)
        {
            SortedDictionary<TKey, TValue> dictionary = (SortedDictionary<TKey, TValue>)GenericIDictionaryFactory(count);
            Assert.False(dictionary.ContainsValue(default(TValue)));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedDictionary_Generic_ContainsValue_DefaultValuePresent(int count)
        {
            SortedDictionary<TKey, TValue> dictionary = (SortedDictionary<TKey, TValue>)GenericIDictionaryFactory(count);
            int seed = 4315;
            TKey notPresent = CreateTKey(seed++);
            while (dictionary.ContainsKey(notPresent))
                notPresent = CreateTKey(seed++);
            dictionary.Add(notPresent, default(TValue));
            Assert.True(dictionary.ContainsValue(default(TValue)));
        }

        #endregion

        #region Ordering

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedDictionary_Generic_DictionaryIsProperlySortedAccordingToComparer(int setLength)
        {
            SortedDictionary<TKey, TValue> set = (SortedDictionary<TKey, TValue>)GenericIDictionaryFactory(setLength);
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
