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
    public abstract class OrderedDictionary_Generic_Tests<TKey, TValue> : IDictionary_Generic_Tests<TKey, TValue>
    {
        #region IDictionary<TKey, TValue> Helper Methods
        protected override bool Enumerator_Empty_UsesSingletonInstance => true;
        protected override bool Enumerator_Empty_Current_UndefinedOperation_Throws => true;
        protected override bool Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;
        protected override bool DefaultValueWhenNotAllowed_Throws => true;
        protected override ModifyOperation ModifyEnumeratorThrows => ModifyOperation.Add | ModifyOperation.Insert | ModifyOperation.Remove | ModifyOperation.Clear;

        protected override IDictionary<TKey, TValue> GenericIDictionaryFactory() => new OrderedDictionary<TKey, TValue>();

        #endregion

        #region Constructors

        [Fact]
        public void OrderedDictionary_Generic_Constructor()
        {
            OrderedDictionary<TKey, TValue> instance;
            IEqualityComparer<TKey> comparer = GetKeyIEqualityComparer();

            instance = new OrderedDictionary<TKey, TValue>();
            Assert.Empty(instance);
            Assert.Empty(instance.Keys);
            Assert.Empty(instance.Values);
            Assert.Same(EqualityComparer<TKey>.Default, instance.Comparer);
            Assert.Equal(0, instance.Capacity);

            instance = new OrderedDictionary<TKey, TValue>(42);
            Assert.Empty(instance);
            Assert.Empty(instance.Keys);
            Assert.Empty(instance.Values);
            Assert.Same(EqualityComparer<TKey>.Default, instance.Comparer);
            Assert.InRange(instance.Capacity, 42, int.MaxValue);

            instance = new OrderedDictionary<TKey, TValue>(comparer);
            Assert.Empty(instance);
            Assert.Empty(instance.Keys);
            Assert.Empty(instance.Values);
            Assert.Same(comparer, instance.Comparer);
            Assert.Equal(0, instance.Capacity);

            instance = new OrderedDictionary<TKey, TValue>(42, comparer);
            Assert.Empty(instance);
            Assert.Empty(instance.Keys);
            Assert.Empty(instance.Values);
            Assert.Same(comparer, instance.Comparer);
            Assert.InRange(instance.Capacity, 42, int.MaxValue);

            IEqualityComparer<TKey> customComparer = EqualityComparer<TKey>.Create(comparer.Equals, comparer.GetHashCode);
            instance = new OrderedDictionary<TKey, TValue>(42, customComparer);
            Assert.Empty(instance);
            Assert.Empty(instance.Keys);
            Assert.Empty(instance.Values);
            Assert.Same(customComparer, instance.Comparer);
            Assert.InRange(instance.Capacity, 42, int.MaxValue);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void OrderedDictionary_Generic_Constructor_IDictionary(int count)
        {
            IDictionary<TKey, TValue> source = GenericIDictionaryFactory(count);
            IEqualityComparer<TKey> comparer = GetKeyIEqualityComparer();
            OrderedDictionary<TKey, TValue> copied;

            copied = new OrderedDictionary<TKey, TValue>(source);
            Assert.Equal(source, copied);
            Assert.Same(comparer, EqualityComparer<TKey>.Default);

            copied = new OrderedDictionary<TKey, TValue>(source, comparer);
            Assert.Equal(source, copied);
            Assert.Same(comparer, copied.Comparer);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void OrderedDictionary_Generic_Constructor_IEnumerable(int count)
        {
            IEnumerable<KeyValuePair<TKey, TValue>> initial = GenericIDictionaryFactory(count);

            foreach (IEnumerable<KeyValuePair<TKey, TValue>> source in new[] { initial, initial.ToArray(), initial.Where(i => true) })
            {
                IEqualityComparer<TKey> comparer = GetKeyIEqualityComparer();
                OrderedDictionary<TKey, TValue> copied;

                copied = new OrderedDictionary<TKey, TValue>(source);
                Assert.Equal(source, copied);
                Assert.Same(comparer, EqualityComparer<TKey>.Default);
                Assert.InRange(copied.Capacity, copied.Count, int.MaxValue);

                copied = new OrderedDictionary<TKey, TValue>(source, comparer);
                Assert.Equal(source, copied);
                Assert.Same(comparer, copied.Comparer);
                Assert.InRange(copied.Capacity, copied.Count, int.MaxValue);
            }
        }

        [Fact]
        public void OrderedDictionary_Generic_Constructor_NullIDictionary_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("dictionary", () => new OrderedDictionary<TKey, TValue>((IDictionary<TKey, TValue>)null));
            AssertExtensions.Throws<ArgumentNullException>("dictionary", () => new OrderedDictionary<TKey, TValue>((IDictionary<TKey, TValue>)null, null));
            AssertExtensions.Throws<ArgumentNullException>("dictionary", () => new OrderedDictionary<TKey, TValue>((IDictionary<TKey, TValue>)null, EqualityComparer<TKey>.Default));

            AssertExtensions.Throws<ArgumentNullException>("collection", () => new OrderedDictionary<TKey, TValue>((IEnumerable<KeyValuePair<TKey, TValue>>)null));
            AssertExtensions.Throws<ArgumentNullException>("collection", () => new OrderedDictionary<TKey, TValue>((IEnumerable<KeyValuePair<TKey, TValue>>)null, null));
            AssertExtensions.Throws<ArgumentNullException>("collection", () => new OrderedDictionary<TKey, TValue>((IEnumerable<KeyValuePair<TKey, TValue>>)null, EqualityComparer<TKey>.Default));
        }

        [Fact]
        public void OrderedDictionary_Generic_Constructor_AllKeysEqualComparer()
        {
            var dictionary = new OrderedDictionary<TKey, TValue>(EqualityComparer<TKey>.Create((x, y) => true, x => 1));
            Assert.Equal(0, dictionary.Count);

            Assert.True(dictionary.TryAdd(CreateTKey(0), CreateTValue(0)));
            Assert.Equal(1, dictionary.Count);

            Assert.False(dictionary.TryAdd(CreateTKey(1), CreateTValue(0)));
            Assert.Equal(1, dictionary.Count);

            dictionary.Remove(CreateTKey(2));
            Assert.Equal(0, dictionary.Count);
        }

        #endregion

        #region TryAdd
        [Fact]
        public void TryAdd_NullKeyThrows()
        {
            if (default(TKey) is not null)
            {
                return;
            }

            var dictionary = new OrderedDictionary<TKey, TValue>();
            AssertExtensions.Throws<ArgumentNullException>("key", () => dictionary.TryAdd(default(TKey), CreateTValue(0)));
            Assert.True(dictionary.TryAdd(CreateTKey(0), default));
            Assert.Equal(1, dictionary.Count);
        }

        [Fact]
        public void TryAdd_AppendsItemToEndOfDictionary()
        {
            var dictionary = new OrderedDictionary<TKey, TValue>();
            AddToCollection(dictionary, 10);
            foreach (var entry in dictionary)
            {
                Assert.False(dictionary.TryAdd(entry.Key, entry.Value));
            }

            TKey newKey;
            int i = 0;
            do
            {
                newKey = CreateTKey(i);
            }
            while (dictionary.ContainsKey(newKey));

            Assert.True(dictionary.TryAdd(newKey, CreateTValue(42)));
            Assert.Equal(dictionary.Count - 1, dictionary.IndexOf(newKey));
        }

        [Fact]
        public void TryAdd_ItemAlreadyExists_DoesNotInvalidateEnumerator()
        {
            TKey key1 = CreateTKey(1);

            var dictionary = new OrderedDictionary<TKey, TValue>() { [key1] = CreateTValue(2) };

            IEnumerator valuesEnum = dictionary.GetEnumerator();
            Assert.False(dictionary.TryAdd(key1, CreateTValue(3)));

            Assert.True(valuesEnum.MoveNext());
        }
        #endregion

        #region ContainsValue

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void OrderedDictionary_Generic_ContainsValue_NotPresent(int count)
        {
            OrderedDictionary<TKey, TValue> dictionary = (OrderedDictionary<TKey, TValue>)GenericIDictionaryFactory(count);
            int seed = 4315;
            TValue notPresent = CreateTValue(seed++);
            while (dictionary.Values.Contains(notPresent))
            {
                notPresent = CreateTValue(seed++);
            }

            Assert.False(dictionary.ContainsValue(notPresent));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void OrderedDictionary_Generic_ContainsValue_Present(int count)
        {
            OrderedDictionary<TKey, TValue> dictionary = (OrderedDictionary<TKey, TValue>)GenericIDictionaryFactory(count);
            int seed = 4315;
            KeyValuePair<TKey, TValue> notPresent = CreateT(seed++);
            while (dictionary.Contains(notPresent))
            {
                notPresent = CreateT(seed++);
            }

            dictionary.Add(notPresent.Key, notPresent.Value);
            Assert.True(dictionary.ContainsValue(notPresent.Value));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void OrderedDictionary_Generic_ContainsValue_DefaultValueNotPresent(int count)
        {
            OrderedDictionary<TKey, TValue> dictionary = (OrderedDictionary<TKey, TValue>)GenericIDictionaryFactory(count);
            Assert.False(dictionary.ContainsValue(default(TValue)));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void OrderedDictionary_Generic_ContainsValue_DefaultValuePresent(int count)
        {
            OrderedDictionary<TKey, TValue> dictionary = (OrderedDictionary<TKey, TValue>)GenericIDictionaryFactory(count);
            int seed = 4315;
            TKey notPresent = CreateTKey(seed++);
            while (dictionary.ContainsKey(notPresent))
            {
                notPresent = CreateTKey(seed++);
            }

            dictionary.Add(notPresent, default(TValue));
            Assert.True(dictionary.ContainsValue(default(TValue)));
        }

        #endregion

        #region GetAt / SetAt

        [Fact]
        public void OrderedDictionary_Generic_SetAt_GetAt_InvalidInputs()
        {
            OrderedDictionary<TKey, TValue> dictionary = (OrderedDictionary<TKey, TValue>)GenericIDictionaryFactory();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => dictionary.GetAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => dictionary.GetAt(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => dictionary.SetAt(-1, CreateTValue(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => dictionary.SetAt(0, CreateTValue(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => dictionary.SetAt(-1, CreateTKey(0), CreateTValue(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => dictionary.SetAt(0, CreateTKey(0), CreateTValue(0)));

            dictionary.Add(CreateTKey(0), CreateTValue(0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => dictionary.GetAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => dictionary.GetAt(1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => dictionary.SetAt(-1, CreateTValue(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => dictionary.SetAt(1, CreateTValue(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => dictionary.SetAt(-1, CreateTKey(0), CreateTValue(0)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => dictionary.SetAt(1, CreateTKey(0), CreateTValue(0)));

            if (default(TKey) is null)
            {
                AssertExtensions.Throws<ArgumentNullException>("key", () => dictionary.SetAt(0, default, CreateTValue(0)));
            }

            dictionary.Add(CreateTKey(1), CreateTValue(1));

            TKey firstKey = dictionary.GetAt(0).Key;
            dictionary.SetAt(0, firstKey, CreateTValue(0));
            dictionary.SetAt(0, CreateTKey(2), CreateTValue(0));
            dictionary.SetAt(0, firstKey, CreateTValue(0));

            AssertExtensions.Throws<ArgumentException>("key", () => dictionary.SetAt(1, firstKey, CreateTValue(0)));
        }

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void OrderedDictionary_Generic_SetAt_GetAt_Roundtrip(int count)
        {
            OrderedDictionary<TKey, TValue> dictionary = (OrderedDictionary<TKey, TValue>)GenericIDictionaryFactory(count);
            KeyValuePair<TKey, TValue> pair;

            for (int i = 0; i < dictionary.Count; i++)
            {
                pair = dictionary.GetAt(i);
                Assert.Equal(pair, ((IList<KeyValuePair<TKey, TValue>>)dictionary)[i]);

                dictionary.SetAt(i, CreateTValue(i + 500));
                pair = dictionary.GetAt(i);
                Assert.Equal(pair, ((IList<KeyValuePair<TKey, TValue>>)dictionary)[i]);

                dictionary.SetAt(i, CreateTKey(i + 1000), CreateTValue(i + 1000));
                pair = dictionary.GetAt(i);
                Assert.Equal(pair, ((IList<KeyValuePair<TKey, TValue>>)dictionary)[i]);
            }
        }

        [Fact]
        public void OrderedDictionary_SetAt_KeyValuePairSubsequentlyAvailable()
        {
            TKey key0 = CreateTKey(0), key1 = CreateTKey(1);
            TValue value0 = CreateTValue(0), value1 = CreateTValue(1);

            var dict = new OrderedDictionary<TKey, TValue>
            {
                [key0] = value0,
            };

            dict.SetAt(index: 0, key1, value1);

            Assert.Equal(1, dict.Count);
            Assert.Equal([new(key1, value1)], dict);
            Assert.False(dict.ContainsKey(key0));
            Assert.True(dict.ContainsKey(key1));
        }

        #endregion

        #region Remove(..., out TValue)

        [Theory]
        [MemberData(nameof(ValidPositiveCollectionSizes))]
        public void OrderedDictionary_Generic_Remove(int count)
        {
            OrderedDictionary<TKey, TValue> dictionary = (OrderedDictionary<TKey, TValue>)GenericIDictionaryFactory(count);

            KeyValuePair<TKey, TValue> pair = default;
            while (dictionary.Count > 0)
            {
                pair = dictionary.GetAt(0);
                Assert.True(dictionary.Remove(pair.Key, out TValue value));
                Assert.Equal(pair.Value, value);
            }

            Assert.False(dictionary.Remove(pair.Key, out _));
        }

        #endregion

        #region TrimExcess

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void OrderedDictionary_Generic_TrimExcess(int count)
        {
            OrderedDictionary<TKey, TValue> dictionary = (OrderedDictionary<TKey, TValue>)GenericIDictionaryFactory(count);

            int dictCount = dictionary.Count;
            dictionary.TrimExcess();
            Assert.Equal(dictCount, dictionary.Count);
            Assert.InRange(dictionary.Capacity, dictCount, int.MaxValue);

            if (count > 0)
            {
                int oldCapacity = dictionary.Capacity;
                int newCapacity = dictionary.EnsureCapacity(count * 10);
                Assert.Equal(newCapacity, dictionary.Capacity);
                Assert.InRange(newCapacity, oldCapacity + 1, int.MaxValue);
                dictionary.TrimExcess(dictCount);
                Assert.Equal(oldCapacity, dictionary.Capacity);
            }
        }

        #endregion

        #region EnsureCapacity

        [Fact]
        public void OrderedDictionary_Generic_EnsureCapacity()
        {
            OrderedDictionary<TKey, TValue> dictionary = (OrderedDictionary<TKey, TValue>)GenericIDictionaryFactory();
            Assert.Equal(0, dictionary.Capacity);

            dictionary.EnsureCapacity(1);
            Assert.InRange(dictionary.Capacity, 1, int.MaxValue);

            for (int i = 0; i < 30; i++)
            {
                dictionary.TryAdd(CreateTKey(i), CreateTValue(i));
            }
            int count = dictionary.Count;
            Assert.InRange(count, 1, 30);
            Assert.InRange(dictionary.Capacity, dictionary.Count, int.MaxValue);
            Assert.Equal(dictionary.Capacity, dictionary.EnsureCapacity(dictionary.Capacity));
            Assert.Equal(dictionary.Capacity, dictionary.EnsureCapacity(dictionary.Capacity - 1));
            Assert.Equal(dictionary.Capacity, dictionary.EnsureCapacity(0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(() => dictionary.EnsureCapacity(-1));

            int oldCapacity = dictionary.Capacity;
            int newCapacity = dictionary.EnsureCapacity(oldCapacity * 2);
            Assert.Equal(newCapacity, dictionary.Capacity);
            Assert.InRange(newCapacity, oldCapacity * 2, int.MaxValue);

            for (int i = 0; i < 30; i++)
            {
                Assert.True(dictionary.ContainsKey(CreateTKey(i)));
            }
            Assert.Equal(count, dictionary.Count);
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
