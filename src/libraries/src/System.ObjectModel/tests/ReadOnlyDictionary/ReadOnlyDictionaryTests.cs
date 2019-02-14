// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Tests.Collections;
using System.Reflection;
using System.Linq;

namespace System.Collections.ObjectModel.Tests
{
    public class ReadOnlyDictionaryTests
    {
        /// <summary>
        /// Current key that the m_generateItemFunc is at.
        /// </summary>
        private static int s_currentKey = int.MaxValue;

        /// <summary>
        /// Function to generate a KeyValuePair.
        /// </summary>
        private static Func<KeyValuePair<int, string>> s_generateItemFunc = () =>
        {
            var kvp = new KeyValuePair<int, string>(s_currentKey, s_currentKey.ToString());
            s_currentKey--;
            return kvp;
        };

        /// <summary>
        /// Tests that the ReadOnlyDictionary is constructed with the given
        /// Dictionary.
        /// </summary>
        [Fact]
        public static void CtorTests()
        {
            KeyValuePair<int, string>[] expectedArr = new KeyValuePair<int, string>[] {
                new KeyValuePair<int, string>(1, "one"),
                new KeyValuePair<int, string>(2, "two"),
                new KeyValuePair<int, string>(3, "three"),
                new KeyValuePair<int, string>(4, "four"),
                new KeyValuePair<int, string>(5, "five")
            };
            DummyDictionary<int, string> dummyExpectedDict = new DummyDictionary<int, string>(expectedArr);
            ReadOnlyDictionary<int, string> dictionary = new ReadOnlyDictionary<int, string>(dummyExpectedDict);
            IReadOnlyDictionary_T_Test<int, string> helper = new IReadOnlyDictionary_T_Test<int, string>(dictionary, expectedArr, s_generateItemFunc);
            helper.InitialItems_Tests();

            IDictionary<int, string> dictAsIDictionary = dictionary;
            Assert.True(dictAsIDictionary.IsReadOnly, "ReadonlyDictionary Should be readonly");

            IDictionary dictAsNonGenericIDictionary = dictionary;
            Assert.True(dictAsNonGenericIDictionary.IsFixedSize);
            Assert.True(dictAsNonGenericIDictionary.IsReadOnly);
        }

        /// <summary>
        /// Tests that an argument null exception is returned when given
        /// a null dictionary to initialise ReadOnlyDictionary.
        /// </summary>
        [Fact]
        public static void CtorTests_Negative()
        {
            AssertExtensions.Throws<ArgumentNullException>("dictionary", () => new ReadOnlyDictionary<int, string>(null));
        }

        /// <summary>
        /// Tests that true is returned when the key exists in the dictionary
        /// and false otherwise.
        /// </summary>
        [Fact]
        public static void ContainsKeyTests()
        {
            KeyValuePair<int, string>[] expectedArr = new KeyValuePair<int, string>[] {
                new KeyValuePair<int, string>(1, "one"),
                new KeyValuePair<int, string>(2, "two"),
                new KeyValuePair<int, string>(3, "three"),
                new KeyValuePair<int, string>(4, "four"),
                new KeyValuePair<int, string>(5, "five")
            };
            DummyDictionary<int, string> dummyExpectedDict = new DummyDictionary<int, string>(expectedArr);
            ReadOnlyDictionary<int, string> dictionary = new ReadOnlyDictionary<int, string>(dummyExpectedDict);
            IReadOnlyDictionary_T_Test<int, string> helper = new IReadOnlyDictionary_T_Test<int, string>(dictionary, expectedArr, s_generateItemFunc);
            helper.ContainsKey_Tests();
        }

        /// <summary>
        /// Tests that the value is retrieved from a dictionary when its
        /// key exists in the dictionary and false when it does not.
        /// </summary>
        [Fact]
        public static void TryGetValueTests()
        {
            KeyValuePair<int, string>[] expectedArr = new KeyValuePair<int, string>[] {
                new KeyValuePair<int, string>(1, "one"),
                new KeyValuePair<int, string>(2, "two"),
                new KeyValuePair<int, string>(3, "three"),
                new KeyValuePair<int, string>(4, "four"),
                new KeyValuePair<int, string>(5, "five")
            };
            DummyDictionary<int, string> dummyExpectedDict = new DummyDictionary<int, string>(expectedArr);
            ReadOnlyDictionary<int, string> dictionary = new ReadOnlyDictionary<int, string>(dummyExpectedDict);
            IReadOnlyDictionary_T_Test<int, string> helper = new IReadOnlyDictionary_T_Test<int, string>(dictionary, expectedArr, s_generateItemFunc);
            helper.TryGetValue_Tests();
        }

        /// <summary>
        /// Tests that the dictionary's keys can be retrieved.
        /// </summary>
        [Fact]
        public static void GetKeysTests()
        {
            KeyValuePair<int, string>[] expectedArr = new KeyValuePair<int, string>[] {
                new KeyValuePair<int, string>(1, "one"),
                new KeyValuePair<int, string>(2, "two"),
                new KeyValuePair<int, string>(3, "three"),
                new KeyValuePair<int, string>(4, "four"),
                new KeyValuePair<int, string>(5, "five")
            };
            DummyDictionary<int, string> dummyExpectedDict = new DummyDictionary<int, string>(expectedArr);
            ReadOnlyDictionary<int, string> dictionary = new ReadOnlyDictionary<int, string>(dummyExpectedDict);
            IReadOnlyDictionary_T_Test<int, string> helper = new IReadOnlyDictionary_T_Test<int, string>(dictionary, expectedArr, s_generateItemFunc);
            helper.Keys_get_Tests();
        }

        /// <summary>
        /// Tests that the dictionary's values can be retrieved.
        /// </summary>
        [Fact]
        public static void GetValuesTests()
        {
            KeyValuePair<int, string>[] expectedArr = new KeyValuePair<int, string>[] {
                new KeyValuePair<int, string>(1, "one"),
                new KeyValuePair<int, string>(2, "two"),
                new KeyValuePair<int, string>(3, "three"),
                new KeyValuePair<int, string>(4, "four"),
                new KeyValuePair<int, string>(5, "five")
            };
            DummyDictionary<int, string> dummyExpectedDict = new DummyDictionary<int, string>(expectedArr);
            ReadOnlyDictionary<int, string> dictionary = new ReadOnlyDictionary<int, string>(dummyExpectedDict);
            IReadOnlyDictionary_T_Test<int, string> helper = new IReadOnlyDictionary_T_Test<int, string>(dictionary, expectedArr, s_generateItemFunc);
            helper.Values_get_Tests();
        }

        /// <summary>
        /// Tests that items can be retrieved by key from the Dictionary.
        /// </summary>
        [Fact]
        public static void GetItemTests()
        {
            KeyValuePair<int, string>[] expectedArr = new KeyValuePair<int, string>[] {
                new KeyValuePair<int, string>(1, "one"),
                new KeyValuePair<int, string>(2, "two"),
                new KeyValuePair<int, string>(3, "three"),
                new KeyValuePair<int, string>(4, "four"),
                new KeyValuePair<int, string>(5, "five")
            };
            DummyDictionary<int, string> dummyExpectedDict = new DummyDictionary<int, string>(expectedArr);
            ReadOnlyDictionary<int, string> dictionary = new ReadOnlyDictionary<int, string>(dummyExpectedDict);
            IReadOnlyDictionary_T_Test<int, string> helper = new IReadOnlyDictionary_T_Test<int, string>(dictionary, expectedArr, s_generateItemFunc);
            helper.Item_get_Tests();
        }

        /// <summary>
        /// Tests that an KeyNotFoundException is thrown when retrieving the
        /// value of an item whose key is not in the dictionary.
        /// </summary>
        [Fact]
        public static void GetItemTests_Negative()
        {
            KeyValuePair<int, string>[] expectedArr = new KeyValuePair<int, string>[] {
                new KeyValuePair<int, string>(1, "one"),
                new KeyValuePair<int, string>(2, "two"),
                new KeyValuePair<int, string>(3, "three"),
                new KeyValuePair<int, string>(4, "four"),
                new KeyValuePair<int, string>(5, "five")
            };
            DummyDictionary<int, string> dummyExpectedDict = new DummyDictionary<int, string>(expectedArr);
            ReadOnlyDictionary<int, string> dictionary = new ReadOnlyDictionary<int, string>(dummyExpectedDict);
            IReadOnlyDictionary_T_Test<int, string> helper = new IReadOnlyDictionary_T_Test<int, string>(dictionary, expectedArr, s_generateItemFunc);
            helper.Item_get_Tests_Negative();
        }

        /// <summary> 
        /// Tests that a ReadOnlyDictionary cannot be modified. That is, that
        /// Add, Remove, Clear does not work.
        /// </summary>
        [Fact]
        public static void CannotModifyDictionaryTests_Negative()
        {
            KeyValuePair<int, string>[] expectedArr = new KeyValuePair<int, string>[] {
                new KeyValuePair<int, string>(1, "one"),
                new KeyValuePair<int, string>(2, "two"),
                new KeyValuePair<int, string>(3, "three"),
                new KeyValuePair<int, string>(4, "four"),
                new KeyValuePair<int, string>(5, "five")
            };
            DummyDictionary<int, string> dummyExpectedDict = new DummyDictionary<int, string>(expectedArr);
            ReadOnlyDictionary<int, string> dictionary = new ReadOnlyDictionary<int, string>(dummyExpectedDict);
            IReadOnlyDictionary_T_Test<int, string> helper = new IReadOnlyDictionary_T_Test<int, string>();
            IDictionary<int, string> dictAsIDictionary = dictionary;

            Assert.Throws<NotSupportedException>(() => dictAsIDictionary.Add(new KeyValuePair<int, string>(7, "seven")));
            Assert.Throws<NotSupportedException>(() => dictAsIDictionary.Add(7, "seven"));
            Assert.Throws<NotSupportedException>(() => dictAsIDictionary.Remove(new KeyValuePair<int, string>(1, "one")));
            Assert.Throws<NotSupportedException>(() => dictAsIDictionary.Remove(1));
            Assert.Throws<NotSupportedException>(() => dictAsIDictionary.Clear());

            helper.VerifyCollection(dictionary, expectedArr); //verifying that the collection has not changed.
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void DebuggerAttributeTests()
        {
            ReadOnlyDictionary<int, int> dict = new ReadOnlyDictionary<int, int>(new Dictionary<int, int>{{1, 2}, {2, 4}, {3, 6}});
            DebuggerAttributes.ValidateDebuggerDisplayReferences(dict);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(dict);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            KeyValuePair<int, int>[] pairs = itemProperty.GetValue(info.Instance) as KeyValuePair<int, int>[];
            Assert.Equal(dict, pairs);

            DebuggerAttributes.ValidateDebuggerDisplayReferences(dict.Keys);
            info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(ReadOnlyDictionary<int, int>.KeyCollection), new Type[] { typeof(int) }, dict.Keys);
            itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            int[] items = itemProperty.GetValue(info.Instance) as int[];
            Assert.Equal(dict.Keys, items);

            DebuggerAttributes.ValidateDebuggerDisplayReferences(dict.Values);
            info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(ReadOnlyDictionary<int, int>.KeyCollection), new Type[] { typeof(int) }, dict.Values);
            itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            items = itemProperty.GetValue(info.Instance) as int[];
            Assert.Equal(dict.Values, items);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void DebuggerAttribute_NullDictionary_ThrowsArgumentNullException()
        {
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>   DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(ReadOnlyDictionary<int, int>), null));
            ArgumentNullException argumentNullException = Assert.IsType<ArgumentNullException>(ex.InnerException);
            Assert.Equal("dictionary", argumentNullException.ParamName);
        }
        
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void DebuggerAttribute_NullDictionaryKeys_ThrowsArgumentNullException()
        {
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(ReadOnlyDictionary<int, int>.KeyCollection), new Type[] { typeof(int) }, null));
            ArgumentNullException argumentNullException = Assert.IsType<ArgumentNullException>(ex.InnerException);
            Assert.Equal("collection", argumentNullException.ParamName);
        }
    }

    public class TestReadOnlyDictionary<TKey, TValue> : ReadOnlyDictionary<TKey, TValue>
    {
        public TestReadOnlyDictionary(IDictionary<TKey, TValue> dict)
            : base(dict)
        {
        }

        public IDictionary<TKey, TValue> GetDictionary()
        {
            return Dictionary;
        }
    }

    public class DictionaryThatDoesntImplementNonGeneric<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _inner;
        
        public DictionaryThatDoesntImplementNonGeneric(IDictionary<TKey, TValue> inner)
        {
            _inner = inner;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _inner).GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _inner.Add(item);
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _inner.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _inner.Remove(item);
        }

        public int Count
        {
            get { return _inner.Count; }
        }

        public bool IsReadOnly
        {
            get { return _inner.IsReadOnly; }
        }

        public void Add(TKey key, TValue value)
        {
            _inner.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return _inner.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return _inner.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _inner.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get { return _inner[key]; }
            set { _inner[key] = value; }
        }

        public ICollection<TKey> Keys
        {
            get { return _inner.Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return _inner.Values; }
        }
    }

    public class ReadOnlyDictionaryOverNonGenericTests
        : IDictionaryTest<string, int>
    {
        public ReadOnlyDictionaryOverNonGenericTests()
            : base(false)
        {
        }

        private int m_next_item = 1;
        protected override bool IsResetNotSupported { get { return false; } }
        protected override bool IsGenericCompatibility { get { return false; } }
        protected override bool ItemsMustBeUnique { get { return true; } }
        protected override bool ItemsMustBeNonNull { get { return true; } }
        protected override object GenerateItem()
        {
            return new KeyValuePair<string, int>(m_next_item.ToString(), m_next_item++);
        }

        protected override IEnumerable GetEnumerable(object[] items)
        {
            var dict = new DictionaryThatDoesntImplementNonGeneric<string, int>(new Dictionary<string, int>());
            foreach (KeyValuePair<string, int> p in items)
                dict[p.Key] = p.Value;
            return new TestReadOnlyDictionary<string, int>(dict);
        }

        protected override object[] InvalidateEnumerator(IEnumerable enumerable)
        {
            var roDict = (TestReadOnlyDictionary<string, int>)enumerable;
            var dict = roDict.GetDictionary();
            var item = (KeyValuePair<string, int>)GenerateItem();
            dict.Add(item.Key, item.Value);
            var arr = new object[dict.Count];
            ((ICollection)roDict).CopyTo(arr, 0);
            return arr;
        }
    }

    public class ReadOnlyDictionaryTestsStringInt
        : IDictionaryTest<string, int>
    {
        public ReadOnlyDictionaryTestsStringInt()
            : base(false)
        {
        }

        private int m_next_item = 1;
        protected override bool IsResetNotSupported { get { return false; } }
        protected override bool IsGenericCompatibility { get { return false; } }
        protected override bool ItemsMustBeUnique { get { return true; } }
        protected override bool ItemsMustBeNonNull { get { return true; } }
        protected override object GenerateItem()
        {
            return new KeyValuePair<string, int>(m_next_item.ToString(), m_next_item++);
        }

        protected override IEnumerable GetEnumerable(object[] items)
        {
            var dict = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> p in items)
                dict[p.Key] = p.Value;
            return new TestReadOnlyDictionary<string, int>(dict);
        }

        protected override object[] InvalidateEnumerator(IEnumerable enumerable)
        {
            var roDict = (TestReadOnlyDictionary<string, int>)enumerable;
            var dict = roDict.GetDictionary();
            var item = (KeyValuePair<string, int>)GenerateItem();
            dict.Add(item.Key, item.Value);
            var arr = new object[dict.Count];
            ((ICollection)roDict).CopyTo(arr, 0);
            return arr;
        }
    }

    /// <summary>
    /// Helper class that performs all of the IReadOnlyDictionary
    /// verifications.
    /// </summary>
    public class IReadOnlyDictionary_T_Test<TKey, TValue>
    {
        private readonly IReadOnlyDictionary<TKey, TValue> _collection;
        private readonly KeyValuePair<TKey, TValue>[] _expectedItems;
        private readonly Func<KeyValuePair<TKey, TValue>> _generateItem;

        /// <summary>
        /// Initializes a new instance of the IReadOnlyDictionary_T_Test.
        /// </summary>
        public IReadOnlyDictionary_T_Test(
            IReadOnlyDictionary<TKey, TValue> collection, KeyValuePair<TKey, TValue>[] expectedItems,
            Func<KeyValuePair<TKey, TValue>> generateItem)
        {
            _collection = collection;
            _expectedItems = expectedItems;
            _generateItem = generateItem;
        }

        public IReadOnlyDictionary_T_Test()
        {

        }

        /// <summary>
        /// Tests that the initial items in the readonly collection
        /// are equivalent to the collection it was initialised with.
        /// </summary>
        public void InitialItems_Tests()
        {
            //[] Verify the initial items in the collection
            VerifyCollection(_collection, _expectedItems);
            //verifies the non-generic enumerator.
            VerifyEnumerator(_collection, _expectedItems);
        }

        /// <summary>
        /// Checks that the dictionary contains all keys of the expected items.
        /// And returns false when the dictionary does not contain a key.
        /// </summary>
        public void ContainsKey_Tests()
        {
            Assert.Equal(_expectedItems.Length, _collection.Count);

            for (int i = 0; i < _collection.Count; i++)
            {
                Assert.True(_collection.ContainsKey(_expectedItems[i].Key),
                    "Err_5983muqjl Verifying ContainsKey the item in the collection and the expected existing items(" + _expectedItems[i].Key + ")");
            }

            //Verify that the collection was not mutated 
            VerifyCollection(_collection, _expectedItems);

            KeyValuePair<TKey, TValue> nonExistingItem = _generateItem();
            while (!IsUniqueKey(_expectedItems, nonExistingItem))
            {
                nonExistingItem = _generateItem();
            }
            TKey nonExistingKey = nonExistingItem.Key;
            Assert.False(_collection.ContainsKey(nonExistingKey), "Err_4713ebda Verifying ContainsKey the non-existing item in the collection and the expected non-existing items key:" + nonExistingKey);

            //Verify that the collection was not mutated 
            VerifyCollection(_collection, _expectedItems);
        }

        /// <summary>
        /// Tests that you can get values that exist in the collection
        /// and not when the value is not in the collection.
        /// </summary>
        public void TryGetValue_Tests()
        {
            Assert.Equal(_expectedItems.Length, _collection.Count);

            for (int i = 0; i < _collection.Count; i++)
            {
                TValue itemValue;
                Assert.True(_collection.TryGetValue(_expectedItems[i].Key, out itemValue),
                    "Err_2621pnyan Verifying TryGetValue the item in the collection and the expected existing items(" + _expectedItems[i].Value + ")");
                Assert.Equal(_expectedItems[i].Value, itemValue);
            }

            //Verify that the collection was not mutated 
            VerifyCollection(_collection, _expectedItems);

            KeyValuePair<TKey, TValue> nonExistingItem = _generateItem();
            while (!IsUniqueKey(_expectedItems, nonExistingItem))
            {
                nonExistingItem = _generateItem();
            }
            TValue nonExistingItemValue;
            Assert.False(_collection.TryGetValue(nonExistingItem.Key, out nonExistingItemValue),
                "Err_4561rtio Verifying TryGetValue returns false when looking for a non-existing item in the collection (" + nonExistingItem.Key + ")");

            //Verify that the collection was not mutated 
            VerifyCollection(_collection, _expectedItems);
        }

        /// <summary>
        /// Tests that you can get all the keys in the collection.
        /// </summary>
        public void Keys_get_Tests()
        {
            // Verify Key get_Values
            int numItemsSeen = 0;
            foreach (TKey key in _collection.Keys)
            {
                numItemsSeen++;
                TValue value;
                Assert.True(_collection.TryGetValue(key, out value), "Items in the Keys collection should exist in the dictionary!");
            }
            Assert.Equal(_collection.Count, numItemsSeen);
        }

        /// <summary>
        /// Tests that you can get all the values in the collection.
        /// </summary>
        public void Values_get_Tests()
        {
            // Verify Values get_Values
            // Copy collection values to another collection, then compare them.
            List<TValue> knownValuesList = new List<TValue>();
            foreach (KeyValuePair<TKey, TValue> pair in _collection)
                knownValuesList.Add(pair.Value);

            int numItemsSeen = 0;
            foreach (TValue value in _collection.Values)
            {
                numItemsSeen++;
                Assert.True(knownValuesList.Contains(value), "Items in the Values collection should exist in the dictionary!");
            }
            Assert.Equal(_collection.Count, numItemsSeen);
        }

        /// <summary>
        /// Runs all of the tests on get Item.
        /// </summary>
        public void Item_get_Tests()
        {
            // Verify get_Item with existing item on Collection
            Assert.Equal(_expectedItems.Length, _collection.Count);
            for (int i = 0; i < _expectedItems.Length; ++i)
            {
                TKey expectedKey = _expectedItems[i].Key;
                TValue expectedValue = _expectedItems[i].Value;
                TValue actualValue = _collection[expectedKey];
                Assert.Equal(expectedValue, actualValue);
            }
            //Verify that the collection was not mutated 
            VerifyCollection(_collection, _expectedItems);
        }

        /// <summary>
        /// Tests that KeyNotFoundException is thrown when trying to get from
        /// a Dictionary whose key is not in the collection.
        /// </summary>
        public void Item_get_Tests_Negative()
        {
            // Verify get_Item with non-existing on Collection
            TKey nonExistingKey = _generateItem().Key;
            Assert.Throws<KeyNotFoundException>(() => { TValue itemValue = _collection[nonExistingKey]; });

            //Verify that the collection was not mutated 
            VerifyCollection(_collection, _expectedItems);
        }

        /// <summary>
        /// Verifies that the items in the given collection match the expected items.
        /// </summary>
        public void VerifyCollection(IReadOnlyDictionary<TKey, TValue> collection, KeyValuePair<TKey, TValue>[] expectedItems)
        {
            // verify that you can get all items in collection.
            Assert.Equal(expectedItems.Length, collection.Count);
            for (int i = 0; i < expectedItems.Length; ++i)
            {
                TKey expectedKey = expectedItems[i].Key;
                TValue expectedValue = expectedItems[i].Value;
                Assert.Equal(expectedValue, collection[expectedKey]);
            }

            VerifyGenericEnumerator(collection, expectedItems);
        }

        #region Helper Methods

        /// <summary>
        /// Verifies that the generic enumerator retrieves the correct items.
        /// </summary>
        private void VerifyGenericEnumerator(IReadOnlyDictionary<TKey, TValue> collection, KeyValuePair<TKey, TValue>[] expectedItems)
        {
            IEnumerator<KeyValuePair<TKey, TValue>> enumerator = collection.GetEnumerator();
            int iterations = 0;
            int expectedCount = expectedItems.Length;

            // There is no sequential order to the collection, so we're testing that all the items
            // in the readonlydictionary exist in the array.
            bool[] itemsVisited = new bool[expectedCount];
            bool itemFound;
            while ((iterations < expectedCount) && enumerator.MoveNext())
            {
                KeyValuePair<TKey, TValue> currentItem = enumerator.Current;
                KeyValuePair<TKey, TValue> tempItem;

                // Verify we have not gotten more items then we expected                
                Assert.True(iterations < expectedCount,
                    "Err_9844awpa More items have been returned from the enumerator(" + iterations + " items) then are in the expectedElements(" + expectedCount + " items)");

                // Verify Current returned the correct value
                itemFound = false;

                for (int i = 0; i < itemsVisited.Length; ++i)
                {
                    if (!itemsVisited[i] && currentItem.Equals(expectedItems[i]))
                    {
                        itemsVisited[i] = true;
                        itemFound = true;
                        break;
                    }
                }
                Assert.True(itemFound, "Err_1432pauy Current returned unexpected value=" + currentItem);

                // Verify Current always returns the same value every time it is called
                for (int i = 0; i < 3; i++)
                {
                    tempItem = enumerator.Current;
                    Assert.Equal(currentItem, tempItem);
                }

                iterations++;
            }

            for (int i = 0; i < expectedCount; ++i)
            {
                Assert.True(itemsVisited[i], "Err_052848ahiedoi Expected Current to return true for item: " + expectedItems[i] + "index: " + i);
            }

            Assert.Equal(expectedCount, iterations);

            for (int i = 0; i < 3; i++)
            {
                Assert.False(enumerator.MoveNext(), "Err_2929ahiea Expected MoveNext to return false after" + iterations + " iterations");
            }

            enumerator.Dispose();
        }

        /// <summary>
        /// Verifies that the non-generic enumerator retrieves the correct items.
        /// </summary>
        private void VerifyEnumerator(IReadOnlyDictionary<TKey, TValue> collection, KeyValuePair<TKey, TValue>[] expectedItems)
        {
            IEnumerator enumerator = collection.GetEnumerator();
            int iterations = 0;
            int expectedCount = expectedItems.Length;
            
            // There is no sequential order to the collection, so we're testing that all the items
            // in the readonlydictionary exist in the array.
            bool[] itemsVisited = new bool[expectedCount];
            bool itemFound;
            while ((iterations < expectedCount) && enumerator.MoveNext())
            {
                object currentItem = enumerator.Current;
                object tempItem;

                // Verify we have not gotten more items then we expected                
                Assert.True(iterations < expectedCount,
                    "Err_9844awpa More items have been returned from the enumerator(" + iterations + " items) then are in the expectedElements(" + expectedCount + " items)");

                // Verify Current returned the correct value
                itemFound = false;

                for (int i = 0; i < itemsVisited.Length; ++i)
                {
                    if (!itemsVisited[i] && expectedItems[i].Equals(currentItem))
                    {
                        itemsVisited[i] = true;
                        itemFound = true;
                        break;
                    }
                }
                Assert.True(itemFound, "Err_1432pauy Current returned unexpected value=" + currentItem);

                // Verify Current always returns the same value every time it is called
                for (int i = 0; i < 3; i++)
                {
                    tempItem = enumerator.Current;
                    Assert.Equal(currentItem, tempItem);
                }

                iterations++;
            }

            for (int i = 0; i < expectedCount; ++i)
            {
                Assert.True(itemsVisited[i], "Err_052848ahiedoi Expected Current to return true for item: " + expectedItems[i] + "index: " + i);
            }

            Assert.Equal(expectedCount, iterations);

            for (int i = 0; i < 3; i++)
            {
                Assert.False(enumerator.MoveNext(), "Err_2929ahiea Expected MoveNext to return false after" + iterations + " iterations");
            }
        }

        /// <summary>
        /// tests whether the given item's key is unique in a collection.  
        /// returns true if it is and false otherwise.
        /// </summary>
        private bool IsUniqueKey(KeyValuePair<TKey, TValue>[] items, KeyValuePair<TKey, TValue> item)
        {
            for (int i = 0; i < items.Length; ++i)
            {
                if (items[i].Key != null && items[i].Key.Equals(item.Key))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
    }

    /// <summary>
    /// Helper Dictionary class that implements basic IDictionary
    /// functionality but none of the modifier methods.
    /// </summary>
    public class DummyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly List<KeyValuePair<TKey, TValue>> _items;
        private readonly TKey[] _keys;
        private readonly TValue[] _values;

        public DummyDictionary(KeyValuePair<TKey, TValue>[] items)
        {
            _keys = new TKey[items.Length];
            _values = new TValue[items.Length];
            _items = new List<KeyValuePair<TKey, TValue>>(items);
            for (int i = 0; i < items.Length; i++)
            {
                _keys[i] = items[i].Key;
                _values[i] = items[i].Value;
            }
        }

        #region IDictionary<TKey, TValue> methods

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool ContainsKey(TKey key)
        {
            foreach (var item in _items)
            {
                if (item.Key.Equals(key))
                    return true;
            }
            return false;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            foreach (var i in _items)
            {
                if (i.Equals(item))
                    return true;
            }
            return false;
        }

        public ICollection<TKey> Keys
        {
            get { return _keys; }
        }

        public ICollection<TValue> Values
        {
            get { return _values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                foreach (var item in _items)
                {
                    if (item.Key.Equals(key))
                        return item.Value;
                }
                throw new KeyNotFoundException("key does not exist");
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default(TValue);

            if (!ContainsKey(key))
                return false;

            value = this[key];
            return true;
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region Not Implemented Methods

        public void Add(TKey key, TValue value)
        {
            throw new NotImplementedException("Should not have been able to add to the collection.");
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException("Should not have been able to add to the collection.");
        }

        public bool Remove(TKey key)
        {
            throw new NotImplementedException("Should not have been able remove items from the collection.");
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException("Should not have been able remove items from the collection.");
        }

        public void Clear()
        {
            throw new NotImplementedException("Should not have been able clear the collection.");
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
