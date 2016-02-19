// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tests.Collections;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    public class BadKey<T> : IComparable<BadKey<T>>,
                             IEquatable<BadKey<T>>
    {
        private readonly T _key;

        public BadKey(T key)
        {
            _key = key;
        }

        public T Key
        {
            get { return _key; }
        }

        /// <summary>
        ///     Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>
        ///     A value that indicates the relative order of the objects being compared. The return value has the following
        ///     meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This
        ///     object is equal to <paramref name="other" />. Greater than zero This object is greater than
        ///     <paramref name="other" />.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public int CompareTo(BadKey<T> other)
        {
            return 0;
        }

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        ///     true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(BadKey<T> other)
        {
            return true;
        }

        /// <summary>
        ///     Serves as the default hash function.
        /// </summary>
        /// <returns>
        ///     A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            return 0;
        }

        /// <summary>
        ///     Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        ///     true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            return true;
        }

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return Key == null ? "<null>" : Key.ToString();
        }
    }

    public class BadKeyComparer<T> : IEqualityComparer<BadKey<T>>
        where T : IEquatable<T>
    {
        /// <summary>
        ///     Determines whether the specified objects are equal.
        /// </summary>
        /// <returns>
        ///     true if the specified objects are equal; otherwise, false.
        /// </returns>
        /// <param name="x">The first object of type <paramref name="T" /> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T" /> to compare.</param>
        public bool Equals(BadKey<T> x, BadKey<T> y)
        {
            if (x == null)
            {
                return y == null;
            }
            if (y == null)
            {
                return false;
            }
            if (x.Key == null)
            {
                return y.Key == null;
            }
            return x.Key.Equals(y.Key);
        }

        /// <summary>
        ///     Returns a hash code for the specified object.
        /// </summary>
        /// <returns>
        ///     A hash code for the specified object.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///     The type of <paramref name="obj" /> is a reference type and
        ///     <paramref name="obj" /> is null.
        /// </exception>
        public int GetHashCode(BadKey<T> obj)
        {
            if (obj == null)
            {
                return 0;
            }
            if (obj.Key == null)
            {
                return 0;
            }
            return obj.Key.GetHashCode();
        }
    }

    public delegate void AddItemsFunc<TKey, TValue>(
        KeyedCollection<TKey, TValue> collection,
        Func<TValue> generateItem,
        Func<TValue, TKey> getKey,
        int numItems,
        out TKey[] keys,
        out TValue[] items,
        out TValue[] itemsWithKeys);

    public static class Helper
    {
        public static IDictionary<TKey, TValue> GetDictionary
            <TKey, TValue>(
            this KeyedCollection<TKey, TValue> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
            MethodInfo propGet =
                typeof (KeyedCollection<TKey, TValue>).GetTypeInfo()
                                                      .DeclaredProperties
                                                      .Where(
                                                          f =>
                                                          f.Name
                                                          == "Dictionary")
                                                      .Select(
                                                          f =>
                                                          f.GetMethod)
                                                      .Where(
                                                          gm =>
                                                          gm != null)
                                                      .FirstOrDefault(
                                                          gm =>
                                                          gm.IsFamily);
            if (propGet == null)
            {
                throw new InvalidOperationException(
                    "Could not get dictionary property from KeyedCollection");
            }
            object obj = propGet.Invoke(collection, new object[0]);
            return (IDictionary<TKey, TValue>) obj;
        }

        public static Func<IKeyedItem<T1, T2>> Bind<T1, T2>(
            this KeyedCollectionGetKeyedValue<T1, T2> function,
            Func<T2> val1,
            Func<T2, T1> val2)
        {
            return () => function(val1, val2);
        }

        public static void Verify<TKey, TValue>(
            this KeyedCollection<TKey, TValue> collection,
            TKey[] expectedKeys,
            TValue[] expectedItems,
            TValue[] expectedItemsWithKeys)
        {
            if (expectedItemsWithKeys.Length != expectedKeys.Length)
            {
                throw new ArgumentException(
                    "Expected Keys length and Expected Items length must be the same");
            }

            Assert.Equal(expectedItems.Length, collection.Count);
            // uses enumerator
            CollectionAssert.Equal(expectedItems, collection);
            // use int indexer
            for (var i = 0; i < expectedItems.Length; ++i)
            {
                Assert.Equal(expectedItems[i], collection[i]);
            }

            // use key indexer
            for (var i = 0; i < expectedItemsWithKeys.Length; ++i)
            {
                Assert.Equal(
                    expectedItemsWithKeys[i],
                    collection[expectedKeys[i]]);
            }

            // check that all keys are contained
            Assert.DoesNotContain(
                expectedKeys,
                key => !collection.Contains(key));

            // check that all values are contained
            Assert.DoesNotContain(
                expectedItems,
                item => !collection.Contains(item));
        }

        public static void AddItems<TKey, TValue>(
            this KeyedCollection<TKey, TValue> collection,
            Func<TValue> generateItem,
            Func<TValue, TKey> getKey,
            int numItems,
            out TKey[] keys,
            out TValue[] items,
            out TValue[] itemsWithKeys)
        {
            items = new TValue[numItems];
            keys = new TKey[numItems];
            itemsWithKeys = new TValue[numItems];
            var keyIndex = 0;

            for (var i = 0; i < numItems; ++i)
            {
                TValue item = generateItem();
                TKey key = getKey(item);

                collection.Add(item);
                items[i] = item;

                if (null != key)
                {
                    keys[keyIndex] = key;
                    itemsWithKeys[keyIndex] = item;
                    ++keyIndex;
                }
            }

            keys = keys.Slice(0, keyIndex);
            itemsWithKeys = itemsWithKeys.Slice(0, keyIndex);
        }

        public static void InsertItems<TKey, TValue>(
            this KeyedCollection<TKey, TValue> collection,
            Func<TValue> generateItem,
            Func<TValue, TKey> getKey,
            int numItems,
            out TKey[] keys,
            out TValue[] items,
            out TValue[] itemsWithKeys)
        {
            items = new TValue[numItems];
            keys = new TKey[numItems];
            itemsWithKeys = new TValue[numItems];
            var keyIndex = 0;

            for (var i = 0; i < numItems; ++i)
            {
                TValue item = generateItem();
                TKey key = getKey(item);

                collection.Insert(collection.Count, item);
                items[i] = item;

                if (null != key)
                {
                    keys[keyIndex] = key;
                    itemsWithKeys[keyIndex] = item;
                    ++keyIndex;
                }
            }

            keys = keys.Slice(0, keyIndex);
            itemsWithKeys = itemsWithKeys.Slice(0, keyIndex);
        }

        public static void InsertItemsObject<TKey, TValue>(
            this KeyedCollection<TKey, TValue> collection,
            Func<TValue> generateItem,
            Func<TValue, TKey> getKey,
            int numItems,
            out TKey[] keys,
            out TValue[] items,
            out TValue[] itemsWithKeys)
        {
            items = new TValue[numItems];
            keys = new TKey[numItems];
            itemsWithKeys = new TValue[numItems];
            var keyIndex = 0;

            for (var i = 0; i < numItems; ++i)
            {
                TValue item = generateItem();
                TKey key = getKey(item);

                ((IList) collection).Insert(collection.Count, item);
                items[i] = item;

                if (null != key)
                {
                    keys[keyIndex] = key;
                    itemsWithKeys[keyIndex] = item;
                    ++keyIndex;
                }
            }

            keys = keys.Slice(0, keyIndex);
            itemsWithKeys = itemsWithKeys.Slice(0, keyIndex);
        }

        public static void AddItemsObject<TKey, TValue>(
            this KeyedCollection<TKey, TValue> collection,
            Func<TValue> generateItem,
            Func<TValue, TKey> getKey,
            int numItems,
            out TKey[] keys,
            out TValue[] items,
            out TValue[] itemsWithKeys)
        {
            items = new TValue[numItems];
            keys = new TKey[numItems];
            itemsWithKeys = new TValue[numItems];
            var keyIndex = 0;

            for (var i = 0; i < numItems; ++i)
            {
                TValue item = generateItem();
                TKey key = getKey(item);

                ((IList) collection).Add(item);
                items[i] = item;

                if (null != key)
                {
                    keys[keyIndex] = key;
                    itemsWithKeys[keyIndex] = item;
                    ++keyIndex;
                }
            }

            keys = keys.Slice(0, keyIndex);
            itemsWithKeys = itemsWithKeys.Slice(0, keyIndex);
        }
    }

    public struct Named<T>
    {
        private readonly string _name;
        private readonly T _value;

        public Named(string name, T value) : this()
        {
            _name = name;
            _value = value;
        }

        public string Name
        {
            get { return _name; }
        }

        public T Value
        {
            get { return _value; }
        }

        /// <summary>
        ///     Returns the fully qualified type name of this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.String" /> containing a fully qualified type name.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }
    }

    public interface IKeyedItem<out TKey, out TValue>
    {
        TKey Key { get; }
        TValue Item { get; }
    }

    public class KeyedItem<TKey, TValue> :
        IComparable<KeyedItem<TKey, TValue>>, IKeyedItem<TKey, TValue>,
        IEquatable<KeyedItem<TKey, TValue>>
        where TValue : IComparable<TValue>
    {
        private readonly TValue _item;

        public KeyedItem(TKey key, TValue item)
        {
            Key = key;
            _item = item;
        }

        /// <summary>
        ///     Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>
        ///     A value that indicates the relative order of the objects being compared. The return value has the following
        ///     meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This
        ///     object is equal to <paramref name="other" />. Greater than zero This object is greater than
        ///     <paramref name="other" />.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public int CompareTo(KeyedItem<TKey, TValue> other)
        {
            if (other == null)
            {
                return -1;
            }
            if (Item == null)
            {
                return other.Item == null ? 0 : -1;
            }
            return Item.CompareTo(other.Item);
        }

        /// <summary>
        ///     Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        ///     true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(KeyedItem<TKey, TValue> other)
        {
            if (other == null)
            {
                return false;
            }
            if (Item == null)
            {
                return other.Item == null;
            }
            return Item.Equals(other.Item);
        }

        public TKey Key { get; set; }

        public TValue Item
        {
            get { return _item; }
        }

        /// <summary>
        ///     Serves as the default hash function.
        /// </summary>
        /// <returns>
        ///     A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            if (Item == null)
            {
                return 0;
            }
            return Item.GetHashCode();
        }

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            string value = Item == null ? "<null>" : Item.ToString();
            string key = Key == null ? "<null>" : Key.ToString();

            return "key=" + key + " value=" + value;
        }
    }

    public class TestKeyedCollection<TKey, TValue> :
        KeyedCollection<TKey, TValue>
    {
        private readonly Func<TValue, TKey> _getKey;

        public TestKeyedCollection(Func<TValue, TKey> getKey)
            : base(null, 32)
        {
            if (getKey == null)
            {
                throw new ArgumentNullException(nameof(getKey));
            }
            _getKey = getKey;
        }

        public TestKeyedCollection(
            Func<TValue, TKey> getKey,
            IEqualityComparer<TKey> comp) : base(comp, 32)
        {
            if (getKey == null)
            {
                throw new ArgumentNullException(nameof(getKey));
            }
            _getKey = getKey;
        }

        protected override TKey GetKeyForItem(TValue item)
        {
            return _getKey(item);
        }

        public void MyChangeItemKey(TValue item, TKey newKey)
        {
            ChangeItemKey(item, newKey);
        }
    }

    public class TestKeyedCollectionOfIKeyedItem<TKey, TValue> :
        KeyedCollection<TKey, IKeyedItem<TKey, TValue>>
        where TKey : IEquatable<TKey>
    {
        public TestKeyedCollectionOfIKeyedItem(
            int collectionDictionaryThreshold = 32)
            : base(null, collectionDictionaryThreshold)
        {
        }

        public TestKeyedCollectionOfIKeyedItem(
            IEqualityComparer<TKey> comp,
            int collectionDictionaryThreshold = 32)
            : base(comp, collectionDictionaryThreshold)
        {
        }

        protected override TKey GetKeyForItem(
            IKeyedItem<TKey, TValue> item)
        {
            return item.Key;
        }

        public void MyChangeItemKey(
            IKeyedItem<TKey, TValue> item,
            TKey newKey)
        {
            ChangeItemKey(item, newKey);
        }
    }

    public delegate IKeyedItem<TKey, TValue>
        KeyedCollectionGetKeyedValue<TKey, TValue>(
        Func<TValue> getValue,
        Func<TValue, TKey> getKeyForItem);
}
