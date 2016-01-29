// Copyright (c) Justin Van Patten. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public class Dictionary_IDictionary_NonGeneric_Tests : IDictionary_NonGeneric_Tests
    {
        protected override IDictionary NonGenericIDictionaryFactory()
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Creates an object that is dependent on the seed given. The object may be either
        /// a value type or a reference type, chosen based on the value of the seed.
        /// </summary>
        protected override object CreateTKey(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Creates an object that is dependent on the seed given. The object may be either
        /// a value type or a reference type, chosen based on the value of the seed.
        /// </summary>
        protected override object CreateTValue(int seed)
        {
            return CreateTKey(seed);
        }

        #region IDictionary tests

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemSet_NullValueWhenDefaultValueIsNonNull(int count)
        {
            IDictionary dictionary = new Dictionary<string, int>();
            Assert.Throws<ArgumentNullException>(() => dictionary[GetNewKey(dictionary)] = null);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemSet_KeyOfWrongType(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = new Dictionary<string, string>();
                Assert.Throws<ArgumentException>(() => dictionary[23] = CreateTValue(12345));
                Assert.Empty(dictionary);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemSet_ValueOfWrongType(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = new Dictionary<string, string>();
                object missingKey = GetNewKey(dictionary);
                Assert.Throws<ArgumentException>(() => dictionary[missingKey] = 324);
                Assert.Empty(dictionary);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Add_KeyOfWrongType(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = new Dictionary<string, string>();
                object missingKey = 23;
                Assert.Throws<ArgumentException>(() => dictionary.Add(missingKey, CreateTValue(12345)));
                Assert.Empty(dictionary);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Add_ValueOfWrongType(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = new Dictionary<string, string>();
                object missingKey = GetNewKey(dictionary);
                Assert.Throws<ArgumentException>(() => dictionary.Add(missingKey, 324));
                Assert.Empty(dictionary);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Add_NullValueWhenDefaultTValueIsNonNull(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = new Dictionary<string, int>();
                object missingKey = GetNewKey(dictionary);
                Assert.Throws<ArgumentNullException>(() => dictionary.Add(missingKey, null));
                Assert.Empty(dictionary);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Contains_KeyOfWrongType(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = new Dictionary<string, int>();
                Assert.False(dictionary.Contains(1));
            }
        }

        #endregion

        #region ICollection tests

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_ArrayOfIncorrectKeyValuePairType(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            KeyValuePair<string, int>[] array = new KeyValuePair<string, int>[count * 3 / 2];
            Assert.Throws<ArgumentException>(() => collection.CopyTo(array, 0));
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_ArrayOfCorrectKeyValuePairType(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[count];
            collection.CopyTo(array, 0);
            int i = 0;
            foreach (object obj in collection)
                Assert.Equal(array[i++], obj);
        }

        #endregion
    }

    public class Dictionary_Tests
    {
        [Fact]
        public void CopyConstructorExceptions()
        {
            Assert.Throws<ArgumentNullException>("dictionary", () => new Dictionary<int, int>((IDictionary<int, int>)null));
            Assert.Throws<ArgumentNullException>("dictionary", () => new Dictionary<int, int>((IDictionary<int, int>)null, null));
            Assert.Throws<ArgumentNullException>("dictionary", () => new Dictionary<int, int>((IDictionary<int, int>)null, EqualityComparer<int>.Default));

            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => new Dictionary<int, int>(new NegativeCountDictionary<int, int>()));
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => new Dictionary<int, int>(new NegativeCountDictionary<int, int>(), null));
            Assert.Throws<ArgumentOutOfRangeException>("capacity", () => new Dictionary<int, int>(new NegativeCountDictionary<int, int>(), EqualityComparer<int>.Default));
        }

        [Theory]
        [MemberData("CopyConstructorInt32Data")]
        public void CopyConstructorInt32(int size, Func<int, int> keyValueSelector, Func<IDictionary<int, int>, IDictionary<int, int>> dictionarySelector)
        {
            TestCopyConstructor(size, keyValueSelector, dictionarySelector);
        }

        public static IEnumerable<object[]> CopyConstructorInt32Data
        {
            get { return GetCopyConstructorData(i => i); }
        }

        [Theory]
        [MemberData("CopyConstructorStringData")]
        public void CopyConstructorString(int size, Func<int, string> keyValueSelector, Func<IDictionary<string, string>, IDictionary<string, string>> dictionarySelector)
        {
            TestCopyConstructor(size, keyValueSelector, dictionarySelector);
        }

        public static IEnumerable<object[]> CopyConstructorStringData
        {
            get { return GetCopyConstructorData(i => i.ToString()); }
        }

        private static void TestCopyConstructor<T>(int size, Func<int, T> keyValueSelector, Func<IDictionary<T, T>, IDictionary<T, T>> dictionarySelector)
        {
            IDictionary<T, T> expected = CreateDictionary(size, keyValueSelector);
            IDictionary<T, T> input = dictionarySelector(CreateDictionary(size, keyValueSelector));

            Assert.Equal(expected, new Dictionary<T, T>(input));
        }

        [Theory]
        [MemberData("CopyConstructorInt32ComparerData")]
        public void CopyConstructorInt32Comparer(int size, Func<int, int> keyValueSelector, Func<IDictionary<int, int>, IDictionary<int, int>> dictionarySelector, IEqualityComparer<int> comparer)
        {
            TestCopyConstructor(size, keyValueSelector, dictionarySelector, comparer);
        }

        public static IEnumerable<object[]> CopyConstructorInt32ComparerData
        {
            get
            {
                var comparers = new IEqualityComparer<int>[]
                {
                    null,
                    EqualityComparer<int>.Default
                };

                return GetCopyConstructorData(i => i, comparers);
            }
        }

        [Theory]
        [MemberData("CopyConstructorStringComparerData")]
        public void CopyConstructorStringComparer(int size, Func<int, string> keyValueSelector, Func<IDictionary<string, string>, IDictionary<string, string>> dictionarySelector, IEqualityComparer<string> comparer)
        {
            TestCopyConstructor(size, keyValueSelector, dictionarySelector, comparer);
        }

        public static IEnumerable<object[]> CopyConstructorStringComparerData
        {
            get
            {
                var comparers = new IEqualityComparer<string>[]
                {
                    null,
                    EqualityComparer<string>.Default,
                    StringComparer.Ordinal,
                    StringComparer.OrdinalIgnoreCase
                };

                return GetCopyConstructorData(i => i.ToString(), comparers);
            }
        }

        private static void TestCopyConstructor<T>(int size, Func<int, T> keyValueSelector, Func<IDictionary<T, T>, IDictionary<T, T>> dictionarySelector, IEqualityComparer<T> comparer)
        {
            IDictionary<T, T> expected = CreateDictionary(size, keyValueSelector, comparer);
            IDictionary<T, T> input = dictionarySelector(CreateDictionary(size, keyValueSelector, comparer));

            Assert.Equal(expected, new Dictionary<T, T>(input, comparer));
        }

        private static IEnumerable<object[]> GetCopyConstructorData<T>(Func<int, T> keyValueSelector, IEqualityComparer<T>[] comparers = null)
        {
            var dictionarySelectors = new Func<IDictionary<T, T>, IDictionary<T, T>>[]
            {
                d => d,
                d => new DictionarySubclass<T, T>(d),
                d => new ReadOnlyDictionary<T, T>(d)
            };

            var sizes = new int[] { 0, 1, 2, 3 };

            foreach (Func<IDictionary<T, T>, IDictionary<T, T>> dictionarySelector in dictionarySelectors)
            {
                foreach (int size in sizes)
                {
                    if (comparers != null)
                    {
                        foreach (IEqualityComparer<T> comparer in comparers)
                        {
                            yield return new object[] { size, keyValueSelector, dictionarySelector, comparer };
                        }
                    }
                    else
                    {
                        yield return new object[] { size, keyValueSelector, dictionarySelector };
                    }
                }
            }
        }

        private static IDictionary<T, T> CreateDictionary<T>(int size, Func<int, T> keyValueSelector, IEqualityComparer<T> comparer = null)
        {
            return Enumerable.Range(1, size).ToDictionary(keyValueSelector, keyValueSelector, comparer);
        }

        private sealed class DictionarySubclass<TKey, TValue> : Dictionary<TKey, TValue>
        {
            public DictionarySubclass(IDictionary<TKey, TValue> dictionary)
            {
                foreach (var pair in dictionary)
                {
                    Add(pair.Key, pair.Value);
                }
            }
        }

        /// <summary>
        /// An incorrectly implemented dictionary that returns -1 from Count.
        /// </summary>
        private sealed class NegativeCountDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        {
            public int Count { get { return -1; } }

            public TValue this[TKey key] { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
            public bool IsReadOnly { get { throw new NotImplementedException(); } }
            public ICollection<TKey> Keys { get { throw new NotImplementedException(); } }
            public ICollection<TValue> Values { get { throw new NotImplementedException(); } }
            public void Add(KeyValuePair<TKey, TValue> item) { throw new NotImplementedException(); }
            public void Add(TKey key, TValue value) { throw new NotImplementedException(); }
            public void Clear() { throw new NotImplementedException(); }
            public bool Contains(KeyValuePair<TKey, TValue> item) { throw new NotImplementedException(); }
            public bool ContainsKey(TKey key) { throw new NotImplementedException(); }
            public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) { throw new NotImplementedException(); }
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() { throw new NotImplementedException(); }
            public bool Remove(KeyValuePair<TKey, TValue> item) { throw new NotImplementedException(); }
            public bool Remove(TKey key) { throw new NotImplementedException(); }
            public bool TryGetValue(TKey key, out TValue value) { throw new NotImplementedException(); }
            IEnumerator IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
        }
    }
}
