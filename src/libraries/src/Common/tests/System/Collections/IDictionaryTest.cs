// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

namespace Tests.Collections
{
    public abstract class IDictionaryTest<TKey, TValue> : ICollectionTest<KeyValuePair<TKey, TValue>>
    {
        protected IDictionaryTest(bool isSynchronized)
            : base(isSynchronized)
        {
        }

        protected IDictionary<TKey, TValue> GetDictionary(object[] items)
        {
            return (IDictionary<TKey, TValue>)GetCollection(items);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(16)]
        [InlineData(100)]
        public void KeysShouldBeCorrect(int count)
        {
            object[] items = GenerateItems(count);
            var expectedKeys = new TKey[items.Length];
            for (int i = 0; i < items.Length; ++i)
            {
                expectedKeys[i] = ((KeyValuePair<TKey, TValue>)items[i]).Key;
            }

            IDictionary<TKey, TValue> dict = GetDictionary(items);

            CollectionAssert.Equal(expectedKeys, dict.Keys);

            IDictionary dict2 = dict as IDictionary;
            if (dict2 != null)
            {
                CollectionAssert.Equal(expectedKeys, dict2.Keys);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(16)]
        [InlineData(100)]
        public void ValuesShouldBeCorrect(int count)
        {
            object[] items = GenerateItems(count);
            var expectedValues = new TValue[items.Length];
            for (int i = 0; i < items.Length; ++i)
            {
                expectedValues[i] = ((KeyValuePair<TKey, TValue>)items[i]).Value;
            }

            IDictionary<TKey, TValue> dict = GetDictionary(items);

            CollectionAssert.Equal(expectedValues, dict.Values);

            IDictionary dict2 = dict as IDictionary;
            if (dict2 != null)
            {
                CollectionAssert.Equal(expectedValues, dict2.Values);
            }
        }

        [Fact]
        public void ItemShouldBeCorrect()
        {
            object[] items = GenerateItems(16);
            IDictionary<TKey, TValue> dict = GetDictionary(items);
            var isReadOnly = dict.IsReadOnly;
            for (int i = 0; i < items.Length; ++i)
            {
                var pair = (KeyValuePair<TKey, TValue>)items[i];
                Assert.Equal(pair.Value, dict[pair.Key]);
                if (isReadOnly)
                {
                    Assert.Throws<NotSupportedException>(() => dict[pair.Key] = pair.Value);
                }
                else
                {
                    dict[pair.Key] = pair.Value;
                }
            }

            IDictionary dict2 = dict as IDictionary;
            if (dict2 != null)
            {
                for (int i = 0; i < items.Length; ++i)
                {
                    var pair = (KeyValuePair<TKey, TValue>)items[i];
                    Assert.Equal(pair.Value, dict2[pair.Key]);
                    if (isReadOnly)
                    {
                        Assert.Throws<NotSupportedException>(() => dict2[pair.Key] = pair.Value);
                    }
                    else
                    {
                        dict2[pair.Key] = pair.Value;
                    }
                }
            }
        }

        [Fact]
        public void IDictionaryShouldContainAllKeys()
        {
            object[] items = GenerateItems(16);
            IDictionary dict = GetDictionary(items) as IDictionary;
            foreach (KeyValuePair<TKey, TValue> item in items)
            {
                Assert.True(dict.Contains(item.Key));
            }
        }

        [Fact]
        public void Contains_NullKey_ThrowsArgumentNullException()
        {
            IDictionary dict = GetDictionary(new object[0]) as IDictionary;
            AssertExtensions.Throws<ArgumentNullException>("key", () => dict.Contains(null));
        }

        [Fact]
        public void WhenDictionaryIsReadOnlyAddShouldThrow()
        {
            object[] items = GenerateItems(16);
            IDictionary dict = GetDictionary(items) as IDictionary;
            if (dict.IsReadOnly)
            {
                var pair = (KeyValuePair<TKey, TValue>)GenerateItem();
                Assert.Throws<NotSupportedException>(() => dict.Add(pair.Key, pair.Value));
            }
        }

        [Fact]
        public void WhenDictionaryIsReadOnlyClearShouldThrow()
        {
            object[] items = GenerateItems(16);
            IDictionary dict = GetDictionary(items) as IDictionary;
            if (dict.IsReadOnly)
            {
                Assert.Throws<NotSupportedException>(() => dict.Clear());
            }
        }

        [Fact]
        public void WhenDictionaryIsReadOnlyRemoveShouldThrow()
        {
            object[] items = GenerateItems(16);
            IDictionary dict = GetDictionary(items) as IDictionary;
            if (dict != null && dict.IsReadOnly)
            {
                var key = ((KeyValuePair<TKey, TValue>)GenerateItem()).Key;
                Assert.Throws<NotSupportedException>(() => dict.Remove(key));
            }
        }

        [Fact]
        public void IDictionaryGetEnumeratorShouldEnumerateSameItemsAsIEnumerableGetEnumerator()
        {
            object[] items = GenerateItems(16);
            IDictionary dict = GetDictionary(items) as IDictionary;
            if (dict != null)
            {
                IEnumerator enumerator = ((IEnumerable)dict).GetEnumerator();
                IDictionaryEnumerator dictEnumerator = dict.GetEnumerator();
                int i = 0;
                while (i++ < 2)
                {
                    while (enumerator.MoveNext())
                    {
                        Assert.True(dictEnumerator.MoveNext());
                        var pair = (KeyValuePair<TKey, TValue>) enumerator.Current;
                        Assert.Equal(dictEnumerator.Current, dictEnumerator.Entry);
                        var entry = dictEnumerator.Entry;
                        Assert.Equal(pair.Key, dictEnumerator.Key);
                        Assert.Equal(pair.Value, dictEnumerator.Value);
                        Assert.Equal(pair.Key, entry.Key);
                        Assert.Equal(pair.Value, entry.Value);
                    }
                    Assert.False(dictEnumerator.MoveNext());
                    dictEnumerator.Reset();
                    enumerator.Reset();
                }
            }
        }

        [Fact]
        public void KeyCollectionIsReadOnly()
        {
            object[] items = GenerateItems(16);
            IDictionary<TKey, TValue> dict = GetDictionary(items);
            var item = (KeyValuePair<TKey, TValue>)GenerateItem();
            var keys = dict.Keys;
            Assert.True(keys.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => keys.Add(item.Key));
            Assert.Throws<NotSupportedException>(() => keys.Clear());
            Assert.Throws<NotSupportedException>(() => keys.Remove(item.Key));
        }

        [Fact]
        public void KeyCollectionShouldContainAllKeys()
        {
            object[] items = GenerateItems(16);
            IDictionary<TKey, TValue> dict = GetDictionary(items);
            var keys = dict.Keys;
            foreach (KeyValuePair<TKey, TValue> item in items)
                Assert.True(keys.Contains(item.Key));
        }

        [Fact]
        public void KeyCollectionShouldNotBeSynchronized()
        {
            object[] items = GenerateItems(16);
            IDictionary<TKey, TValue> dict = GetDictionary(items);
            var keys = (ICollection)dict.Keys;
            Assert.False(keys.IsSynchronized);
        }

        [Fact]
        public void KeyCollectionSyncRootShouldNotBeNull()
        {
            object[] items = GenerateItems(16);
            IDictionary<TKey, TValue> dict = GetDictionary(items);
            var keys = (ICollection)dict.Keys;
            Assert.NotNull(keys.SyncRoot);
        }

        [Fact]
        public void ValueCollectionIsReadOnly()
        {
            object[] items = GenerateItems(16);
            IDictionary<TKey, TValue> dict = GetDictionary(items);
            var item = (KeyValuePair<TKey, TValue>)GenerateItem();
            var values = dict.Values;
            Assert.True(values.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => values.Add(item.Value));
            Assert.Throws<NotSupportedException>(() => values.Clear());
            Assert.Throws<NotSupportedException>(() => values.Remove(item.Value));
        }

        [Fact]
        public void ValueCollectionShouldContainAllValues()
        {
            object[] items = GenerateItems(16);
            IDictionary<TKey, TValue> dict = GetDictionary(items);
            var values = dict.Values;
            foreach (KeyValuePair<TKey, TValue> item in items)
                Assert.True(values.Contains(item.Value));
        }

        [Fact]
        public void ValueCollectionShouldNotBeSynchronized()
        {
            object[] items = GenerateItems(16);
            IDictionary<TKey, TValue> dict = GetDictionary(items);
            var values = (ICollection)dict.Values;
            Assert.False(values.IsSynchronized);
        }

        [Fact]
        public void ValueCollectionSyncRootShouldNotBeNull()
        {
            object[] items = GenerateItems(16);
            IDictionary<TKey, TValue> dict = GetDictionary(items);
            var values = (ICollection)dict.Values;
            Assert.NotNull(values.SyncRoot);
        }
    }
}
