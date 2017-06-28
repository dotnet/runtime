// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public abstract class ImmutableDictionaryTestBase : ImmutablesTestBase
    {
        [Fact]
        public virtual void EmptyTest()
        {
            this.EmptyTestHelper(Empty<int, bool>(), 5);
        }

        [Fact]
        public void EnumeratorTest()
        {
            this.EnumeratorTestHelper(this.Empty<int, GenericParameterHelper>());
        }

        [Fact]
        public void ContainsTest()
        {
            this.ContainsTestHelper(Empty<int, string>(), 5, "foo");
        }

        [Fact]
        public void RemoveTest()
        {
            this.RemoveTestHelper(Empty<int, GenericParameterHelper>(), 5);
        }

        [Fact]
        public void KeysTest()
        {
            this.KeysTestHelper(Empty<int, bool>(), 5);
        }

        [Fact]
        public void ValuesTest()
        {
            this.ValuesTestHelper(Empty<int, bool>(), 5);
        }

        [Fact]
        public void AddAscendingTest()
        {
            this.AddAscendingTestHelper(Empty<int, GenericParameterHelper>());
        }

        [Fact]
        public void AddRangeTest()
        {
            var map = Empty<int, GenericParameterHelper>();
            map = map.AddRange(Enumerable.Range(1, 100).Select(n => new KeyValuePair<int, GenericParameterHelper>(n, new GenericParameterHelper())));
            CollectionAssertAreEquivalent(map.Select(kv => kv.Key).ToList(), Enumerable.Range(1, 100).ToList());
            this.VerifyAvlTreeState(map);
            Assert.Equal(100, map.Count);

            // Test optimization for empty map.
            var map2 = Empty<int, GenericParameterHelper>();
            var jointMap = map2.AddRange(map);
            Assert.Same(map, jointMap);

            jointMap = map2.AddRange(map.ToReadOnlyDictionary());
            Assert.Same(map, jointMap);

            jointMap = map2.AddRange(map.ToBuilder());
            Assert.Same(map, jointMap);
        }

        [Fact]
        public void AddDescendingTest()
        {
            this.AddDescendingTestHelper(Empty<int, GenericParameterHelper>());
        }

        [Fact]
        public void AddRemoveRandomDataTest()
        {
            this.AddRemoveRandomDataTestHelper(Empty<double, GenericParameterHelper>());
        }

        [Fact]
        public void AddRemoveEnumerableTest()
        {
            this.AddRemoveEnumerableTestHelper(Empty<int, int>());
        }

        [Fact]
        public void SetItemTest()
        {
            var map = this.Empty<string, int>()
                .SetItem("Microsoft", 100)
                .SetItem("Corporation", 50);
            Assert.Equal(2, map.Count);

            map = map.SetItem("Microsoft", 200);
            Assert.Equal(2, map.Count);
            Assert.Equal(200, map["Microsoft"]);

            // Set it to the same thing again and make sure it's all good.
            var sameMap = map.SetItem("Microsoft", 200);
            Assert.Same(map, sameMap);
        }

        [Fact]
        public void SetItemsTest()
        {
            var template = new Dictionary<string, int>
            {
                { "Microsoft", 100 },
                { "Corporation", 50 },
            };
            var map = this.Empty<string, int>().SetItems(template);
            Assert.Equal(2, map.Count);

            var changes = new Dictionary<string, int>
            {
                { "Microsoft", 150 },
                { "Dogs", 90 },
            };
            map = map.SetItems(changes);
            Assert.Equal(3, map.Count);
            Assert.Equal(150, map["Microsoft"]);
            Assert.Equal(50, map["Corporation"]);
            Assert.Equal(90, map["Dogs"]);

            map = map.SetItems(
                new[] {
                    new KeyValuePair<string, int>("Microsoft", 80),
                    new KeyValuePair<string, int>("Microsoft", 70),
                });
            Assert.Equal(3, map.Count);
            Assert.Equal(70, map["Microsoft"]);
            Assert.Equal(50, map["Corporation"]);
            Assert.Equal(90, map["Dogs"]);

            map = this.Empty<string, int>().SetItems(new[] { // use an array for code coverage
                new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("b", 2),
                new KeyValuePair<string, int>("a", 3),
            });
            Assert.Equal(2, map.Count);
            Assert.Equal(3, map["a"]);
            Assert.Equal(2, map["b"]);
        }

        [Fact]
        public void ContainsKeyTest()
        {
            this.ContainsKeyTestHelper(Empty<int, GenericParameterHelper>(), 1, new GenericParameterHelper());
        }

        [Fact]
        public void IndexGetNonExistingKeyThrowsTest()
        {
            Assert.Throws<KeyNotFoundException>(() => this.Empty<int, int>()[3]);
        }

        [Fact]
        public void IndexGetTest()
        {
            var map = this.Empty<int, int>().Add(3, 5);
            Assert.Equal(5, map[3]);
        }

        [Fact]
        public void DictionaryRemoveThrowsTest()
        {
            IDictionary<int, int> map = this.Empty<int, int>().Add(5, 3).ToReadOnlyDictionary();
            Assert.Throws<NotSupportedException>(() => map.Remove(5));
        }

        [Fact]
        public void DictionaryAddThrowsTest()
        {
            IDictionary<int, int> map = this.Empty<int, int>().ToReadOnlyDictionary();
            Assert.Throws<NotSupportedException>(() => map.Add(5, 3));
        }

        [Fact]
        public void DictionaryIndexSetThrowsTest()
        {
            IDictionary<int, int> map = this.Empty<int, int>().ToReadOnlyDictionary();
            Assert.Throws<NotSupportedException>(() => map[3] = 5);
        }

        [Fact]
        public void EqualsTest()
        {
            Assert.False(Empty<int, int>().Equals(null));
            Assert.False(Empty<int, int>().Equals("hi"));
            Assert.True(Empty<int, int>().Equals(Empty<int, int>()));
            Assert.False(Empty<int, int>().Add(3, 2).Equals(Empty<int, int>().Add(3, 2)));
            Assert.False(Empty<int, int>().Add(3, 2).Equals(Empty<int, int>().Add(3, 1)));
            Assert.False(Empty<int, int>().Add(5, 1).Equals(Empty<int, int>().Add(3, 1)));
            Assert.False(Empty<int, int>().Add(3, 1).Add(5, 1).Equals(Empty<int, int>().Add(3, 1)));
            Assert.False(Empty<int, int>().Add(3, 1).Equals(Empty<int, int>().Add(3, 1).Add(5, 1)));

            Assert.True(Empty<int, int>().ToReadOnlyDictionary().Equals(Empty<int, int>()));
            Assert.True(Empty<int, int>().Equals(Empty<int, int>().ToReadOnlyDictionary()));
            Assert.True(Empty<int, int>().ToReadOnlyDictionary().Equals(Empty<int, int>().ToReadOnlyDictionary()));
            Assert.False(Empty<int, int>().Add(3, 1).ToReadOnlyDictionary().Equals(Empty<int, int>()));
            Assert.False(Empty<int, int>().Equals(Empty<int, int>().Add(3, 1).ToReadOnlyDictionary()));
            Assert.False(Empty<int, int>().ToReadOnlyDictionary().Equals(Empty<int, int>().Add(3, 1).ToReadOnlyDictionary()));
        }

        /// <summary>
        /// Verifies that the GetHashCode method returns the standard one.
        /// </summary>
        [Fact]
        public void GetHashCodeTest()
        {
            var dictionary = Empty<string, int>();
            Assert.Equal(EqualityComparer<object>.Default.GetHashCode(dictionary), dictionary.GetHashCode());
        }

        [Fact]
        public void ICollectionOfKVMembers()
        {
            var dictionary = (ICollection<KeyValuePair<string, int>>)Empty<string, int>();
            Assert.Throws<NotSupportedException>(() => dictionary.Add(new KeyValuePair<string, int>()));
            Assert.Throws<NotSupportedException>(() => dictionary.Remove(new KeyValuePair<string, int>()));
            Assert.Throws<NotSupportedException>(() => dictionary.Clear());
            Assert.True(dictionary.IsReadOnly);
        }

        [Fact]
        public void ICollectionMembers()
        {
            ((ICollection)Empty<string, int>()).CopyTo(new object[0], 0);

            var dictionary = (ICollection)Empty<string, int>().Add("a", 1);
            Assert.True(dictionary.IsSynchronized);
            Assert.NotNull(dictionary.SyncRoot);
            Assert.Same(dictionary.SyncRoot, dictionary.SyncRoot);

            var array = new object[2];
            dictionary.CopyTo(array, 1);
            Assert.Null(array[0]);
            Assert.Equal(new DictionaryEntry("a", 1), (DictionaryEntry)array[1]);
        }

        [Fact]
        public void IDictionaryOfKVMembers()
        {
            var dictionary = (IDictionary<string, int>)Empty<string, int>().Add("c", 3);
            Assert.Throws<NotSupportedException>(() => dictionary.Add("a", 1));
            Assert.Throws<NotSupportedException>(() => dictionary.Remove("a"));
            Assert.Throws<NotSupportedException>(() => dictionary["a"] = 2);
            Assert.Throws<KeyNotFoundException>(() => dictionary["a"]);
            Assert.Equal(3, dictionary["c"]);
        }

        [Fact]
        public void IDictionaryMembers()
        {
            var dictionary = (IDictionary)Empty<string, int>().Add("c", 3);
            Assert.Throws<NotSupportedException>(() => dictionary.Add("a", 1));
            Assert.Throws<NotSupportedException>(() => dictionary.Remove("a"));
            Assert.Throws<NotSupportedException>(() => dictionary["a"] = 2);
            Assert.Throws<NotSupportedException>(() => dictionary.Clear());
            Assert.False(dictionary.Contains("a"));
            Assert.True(dictionary.Contains("c"));
            Assert.Throws<KeyNotFoundException>(() => dictionary["a"]);
            Assert.Equal(3, dictionary["c"]);
            Assert.True(dictionary.IsFixedSize);
            Assert.True(dictionary.IsReadOnly);
            Assert.Equal(new[] { "c" }, dictionary.Keys.Cast<string>().ToArray());
            Assert.Equal(new[] { 3 }, dictionary.Values.Cast<int>().ToArray());
        }

        [Fact]
        public void IDictionaryEnumerator()
        {
            var dictionary = (IDictionary)Empty<string, int>().Add("a", 1);
            var enumerator = dictionary.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.Key);
            Assert.Throws<InvalidOperationException>(() => enumerator.Value);
            Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(enumerator.Entry, enumerator.Current);
            Assert.Equal(enumerator.Key, enumerator.Entry.Key);
            Assert.Equal(enumerator.Value, enumerator.Entry.Value);
            Assert.Equal("a", enumerator.Key);
            Assert.Equal(1, enumerator.Value);
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.Key);
            Assert.Throws<InvalidOperationException>(() => enumerator.Value);
            Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
            Assert.False(enumerator.MoveNext());

            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.Key);
            Assert.Throws<InvalidOperationException>(() => enumerator.Value);
            Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(enumerator.Key, ((DictionaryEntry)enumerator.Current).Key);
            Assert.Equal(enumerator.Value, ((DictionaryEntry)enumerator.Current).Value);
            Assert.Equal("a", enumerator.Key);
            Assert.Equal(1, enumerator.Value);
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.Key);
            Assert.Throws<InvalidOperationException>(() => enumerator.Value);
            Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void TryGetKey()
        {
            var dictionary = Empty<int>(StringComparer.OrdinalIgnoreCase)
                .Add("a", 1);
            string actualKey;
            Assert.True(dictionary.TryGetKey("a", out actualKey));
            Assert.Equal("a", actualKey);

            Assert.True(dictionary.TryGetKey("A", out actualKey));
            Assert.Equal("a", actualKey);

            Assert.False(dictionary.TryGetKey("b", out actualKey));
            Assert.Equal("b", actualKey);
        }

        protected void EmptyTestHelper<K, V>(IImmutableDictionary<K, V> empty, K someKey)
        {
            Assert.Same(empty, empty.Clear());
            Assert.Equal(0, empty.Count);
            Assert.Equal(0, empty.Count());
            Assert.Equal(0, empty.Keys.Count());
            Assert.Equal(0, empty.Values.Count());
            Assert.Same(EqualityComparer<V>.Default, GetValueComparer(empty));
            Assert.False(empty.ContainsKey(someKey));
            Assert.False(empty.Contains(new KeyValuePair<K, V>(someKey, default(V))));
            Assert.Equal(default(V), empty.GetValueOrDefault(someKey));

            V value;
            Assert.False(empty.TryGetValue(someKey, out value));
            Assert.Equal(default(V), value);
        }

        private IImmutableDictionary<TKey, TValue> AddTestHelper<TKey, TValue>(IImmutableDictionary<TKey, TValue> map, TKey key, TValue value) where TKey : IComparable<TKey>
        {
            Assert.NotNull(map);
            Assert.NotNull(key);

            IImmutableDictionary<TKey, TValue> addedMap = map.Add(key, value);
            Assert.NotSame(map, addedMap);
            ////Assert.Equal(map.Count + 1, addedMap.Count);
            Assert.False(map.ContainsKey(key));
            Assert.True(addedMap.ContainsKey(key));
            AssertAreSame(value, addedMap.GetValueOrDefault(key));

            this.VerifyAvlTreeState(addedMap);

            return addedMap;
        }

        protected void AddAscendingTestHelper(IImmutableDictionary<int, GenericParameterHelper> map)
        {
            Assert.NotNull(map);

            for (int i = 0; i < 10; i++)
            {
                map = this.AddTestHelper(map, i, new GenericParameterHelper(i));
            }

            Assert.Equal(10, map.Count);
            for (int i = 0; i < 10; i++)
            {
                Assert.True(map.ContainsKey(i));
            }
        }

        protected void AddDescendingTestHelper(IImmutableDictionary<int, GenericParameterHelper> map)
        {
            for (int i = 10; i > 0; i--)
            {
                map = this.AddTestHelper(map, i, new GenericParameterHelper(i));
            }

            Assert.Equal(10, map.Count);
            for (int i = 10; i > 0; i--)
            {
                Assert.True(map.ContainsKey(i));
            }
        }

        protected void AddRemoveRandomDataTestHelper(IImmutableDictionary<double, GenericParameterHelper> map)
        {
            Assert.NotNull(map);

            double[] inputs = GenerateDummyFillData();
            for (int i = 0; i < inputs.Length; i++)
            {
                map = this.AddTestHelper(map, inputs[i], new GenericParameterHelper());
            }

            Assert.Equal(inputs.Length, map.Count);
            for (int i = 0; i < inputs.Length; i++)
            {
                Assert.True(map.ContainsKey(inputs[i]));
            }

            for (int i = 0; i < inputs.Length; i++)
            {
                map = map.Remove(inputs[i]);
                this.VerifyAvlTreeState(map);
            }

            Assert.Equal(0, map.Count);
        }

        protected void AddRemoveEnumerableTestHelper(IImmutableDictionary<int, int> empty)
        {
            Assert.NotNull(empty);

            Assert.Same(empty, empty.RemoveRange(Enumerable.Empty<int>()));
            Assert.Same(empty, empty.AddRange(Enumerable.Empty<KeyValuePair<int, int>>()));
            var list = new List<KeyValuePair<int, int>> { new KeyValuePair<int, int>(3, 5), new KeyValuePair<int, int>(8, 10) };
            var nonEmpty = empty.AddRange(list);
            this.VerifyAvlTreeState(nonEmpty);
            var halfRemoved = nonEmpty.RemoveRange(Enumerable.Range(1, 5));
            Assert.Equal(1, halfRemoved.Count);
            Assert.True(halfRemoved.ContainsKey(8));
            this.VerifyAvlTreeState(halfRemoved);
        }

        protected void AddExistingKeySameValueTestHelper<TKey, TValue>(IImmutableDictionary<TKey, TValue> map, TKey key, TValue value1, TValue value2)
        {
            Assert.NotNull(map);
            Assert.NotNull(key);
            Assert.True(GetValueComparer(map).Equals(value1, value2));

            map = map.Add(key, value1);
            Assert.Same(map, map.Add(key, value2));
            Assert.Same(map, map.AddRange(new[] { new KeyValuePair<TKey, TValue>(key, value2) }));
        }

        /// <summary>
        /// Verifies that adding a key-value pair where the key already is in the map but with a different value throws.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the map.</typeparam>
        /// <typeparam name="TValue">The type of value in the map.</typeparam>
        /// <param name="map">The map to manipulate.</param>
        /// <param name="key">The key to add.</param>
        /// <param name="value1">The first value to add.</param>
        /// <param name="value2">The second value to add.</param>
        /// <remarks>
        /// Adding a key-value pair to a map where that key already exists, but with a different value, cannot fit the
        /// semantic of "adding", either by just returning or mutating the value on the existing key.  Throwing is the only reasonable response.
        /// </remarks>
        protected void AddExistingKeyDifferentValueTestHelper<TKey, TValue>(IImmutableDictionary<TKey, TValue> map, TKey key, TValue value1, TValue value2)
        {
            Assert.NotNull(map);
            Assert.NotNull(key);
            Assert.False(GetValueComparer(map).Equals(value1, value2));

            var map1 = map.Add(key, value1);
            var map2 = map.Add(key, value2);
            AssertExtensions.Throws<ArgumentException>(null, () => map1.Add(key, value2));
            AssertExtensions.Throws<ArgumentException>(null, () => map2.Add(key, value1));
        }

        protected void ContainsKeyTestHelper<TKey, TValue>(IImmutableDictionary<TKey, TValue> map, TKey key, TValue value)
        {
            Assert.False(map.ContainsKey(key));
            Assert.True(map.Add(key, value).ContainsKey(key));
        }

        protected void ContainsTestHelper<TKey, TValue>(IImmutableDictionary<TKey, TValue> map, TKey key, TValue value)
        {
            Assert.False(map.Contains(new KeyValuePair<TKey, TValue>(key, value)));
            Assert.False(map.Contains(key, value));
            Assert.True(map.Add(key, value).Contains(new KeyValuePair<TKey, TValue>(key, value)));
            Assert.True(map.Add(key, value).Contains(key, value));
        }

        protected void RemoveTestHelper<TKey, TValue>(IImmutableDictionary<TKey, TValue> map, TKey key)
        {
            // no-op remove
            Assert.Same(map, map.Remove(key));
            Assert.Same(map, map.RemoveRange(Enumerable.Empty<TKey>()));

            // substantial remove
            var addedMap = map.Add(key, default(TValue));
            var removedMap = addedMap.Remove(key);
            Assert.NotSame(addedMap, removedMap);
            Assert.False(removedMap.ContainsKey(key));
        }

        protected void KeysTestHelper<TKey, TValue>(IImmutableDictionary<TKey, TValue> map, TKey key)
        {
            Assert.Equal(0, map.Keys.Count());
            Assert.Equal(0, map.ToReadOnlyDictionary().Keys.Count());

            var nonEmpty = map.Add(key, default(TValue));
            Assert.Equal(1, nonEmpty.Keys.Count());
            Assert.Equal(1, nonEmpty.ToReadOnlyDictionary().Keys.Count());
            KeysOrValuesTestHelper(((IDictionary<TKey, TValue>)nonEmpty).Keys, key);
        }

        protected void ValuesTestHelper<TKey, TValue>(IImmutableDictionary<TKey, TValue> map, TKey key)
        {
            Assert.Equal(0, map.Values.Count());
            Assert.Equal(0, map.ToReadOnlyDictionary().Values.Count());

            var nonEmpty = map.Add(key, default(TValue));
            Assert.Equal(1, nonEmpty.Values.Count());
            Assert.Equal(1, nonEmpty.ToReadOnlyDictionary().Values.Count());
            KeysOrValuesTestHelper(((IDictionary<TKey, TValue>)nonEmpty).Values, default(TValue));
        }

        protected void EnumeratorTestHelper(IImmutableDictionary<int, GenericParameterHelper> map)
        {
            for (int i = 0; i < 10; i++)
            {
                map = this.AddTestHelper(map, i, new GenericParameterHelper(i));
            }

            int j = 0;
            foreach (KeyValuePair<int, GenericParameterHelper> pair in map)
            {
                Assert.Equal(j, pair.Key);
                Assert.Equal(j, pair.Value.Data);
                j++;
            }

            var list = map.ToList();
            Assert.Equal<KeyValuePair<int, GenericParameterHelper>>(list, ImmutableSetTest.ToListNonGeneric<KeyValuePair<int, GenericParameterHelper>>(map));

            // Apply some less common uses to the enumerator to test its metal.
            using (var enumerator = map.GetEnumerator())
            {
                enumerator.Reset(); // reset isn't usually called before MoveNext
                ManuallyEnumerateTest(list, enumerator);
                enumerator.Reset();
                ManuallyEnumerateTest(list, enumerator);

                // this time only partially enumerate
                enumerator.Reset();
                enumerator.MoveNext();
                enumerator.Reset();
                ManuallyEnumerateTest(list, enumerator);
            }

            var manualEnum = map.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => manualEnum.Current);
            while (manualEnum.MoveNext()) { }
            Assert.False(manualEnum.MoveNext());
            Assert.Throws<InvalidOperationException>(() => manualEnum.Current);
        }

        protected abstract IImmutableDictionary<TKey, TValue> Empty<TKey, TValue>();

        protected abstract IImmutableDictionary<string, TValue> Empty<TValue>(StringComparer comparer);

        protected abstract IEqualityComparer<TValue> GetValueComparer<TKey, TValue>(IImmutableDictionary<TKey, TValue> dictionary);

        internal abstract IBinaryTree GetRootNode<TKey, TValue>(IImmutableDictionary<TKey, TValue> dictionary);

        private static void KeysOrValuesTestHelper<T>(ICollection<T> collection, T containedValue)
        {
            Requires.NotNull(collection, nameof(collection));

            Assert.True(collection.Contains(containedValue));
            Assert.Throws<NotSupportedException>(() => collection.Add(default(T)));
            Assert.Throws<NotSupportedException>(() => collection.Clear());

            var nonGeneric = (ICollection)collection;
            Assert.NotNull(nonGeneric.SyncRoot);
            Assert.Same(nonGeneric.SyncRoot, nonGeneric.SyncRoot);
            Assert.True(nonGeneric.IsSynchronized);
            Assert.True(collection.IsReadOnly);

            AssertExtensions.Throws<ArgumentNullException>("array", () => nonGeneric.CopyTo(null, 0));
            var array = new T[collection.Count + 1];
            nonGeneric.CopyTo(array, 1);
            Assert.Equal(default(T), array[0]);
            Assert.Equal(array.Skip(1), nonGeneric.Cast<T>().ToArray());
        }

        private void VerifyAvlTreeState<TKey, TValue>(IImmutableDictionary<TKey, TValue> dictionary)
        {
            var rootNode = this.GetRootNode(dictionary);
            rootNode.VerifyBalanced();
            rootNode.VerifyHeightIsWithinTolerance(dictionary.Count);
        }
    }
}
