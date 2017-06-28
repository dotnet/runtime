// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Tests.Collections;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    public abstract partial class KeyedCollectionTests<TKey, TValue>
        where TValue : IComparable<TValue> where TKey : IEquatable<TKey>
    {
        private static readonly bool s_keyNullable = default(TKey)
                                                     == null;

        private static int s_sometimesNullIndex;

        public static Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
            GetNeverNullKeyMethod
        {
            get
            {
                return
                    new Named
                        <KeyedCollectionGetKeyedValue<TKey, TValue>>(
                        "GetNeverNullKey",
                        GetNeverNullKey);
            }
        }

        public static Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
            GetSometimesNullKeyMethod
        {
            get
            {
                return
                    new Named
                        <KeyedCollectionGetKeyedValue<TKey, TValue>>(
                        "GetSometimesNullKey",
                        GetSometimesNullKey);
            }
        }

        public static Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
            GetAlwaysNullKeyMethod
        {
            get
            {
                return
                    new Named
                        <KeyedCollectionGetKeyedValue<TKey, TValue>>(
                        "GetAlwaysNullKey",
                        GetAlwaysNullKey);
            }
        }

        public static IEnumerable<object[]> CollectionSizes
        {
            get
            {
                yield return new object[] {0};
                yield return new object[] {33};
            }
        }

        public static IEnumerable<object[]> ClassData2
        {
            get
            {
                yield return new object[] {0, GetNeverNullKeyMethod};
                yield return new object[] {33, GetNeverNullKeyMethod};
            }
        }

        public static IEnumerable<object[]> ClassData
        {
            get
            {
                yield return new object[] {0, GetNeverNullKeyMethod};
                yield return new object[] {33, GetNeverNullKeyMethod};
                if (s_keyNullable)
                {
                    yield return
                        new object[] {0, GetSometimesNullKeyMethod};
                    yield return
                        new object[] {33, GetSometimesNullKeyMethod};
                    yield return
                        new object[] {0, GetAlwaysNullKeyMethod};
                    yield return
                        new object[] {33, GetAlwaysNullKeyMethod};
                }
            }
        }

        public static IEnumerable<object[]> ThresholdData
        {
            get
            {
                yield return
                    new object[]
                    {
                        32,
                        new Named
                            <
                                AddItemsFunc
                                    <TKey, IKeyedItem<TKey, TValue>>>(
                            "Add<T>",
                            Helper.AddItems)
                    };
                yield return
                    new object[]
                    {
                        -1,
                        new Named
                            <
                                AddItemsFunc
                                    <TKey, IKeyedItem<TKey, TValue>>>(
                            "Add<T>",
                            Helper.AddItems)
                    };
                yield return
                    new object[]
                    {
                        32,
                        new Named
                            <
                                AddItemsFunc
                                    <TKey, IKeyedItem<TKey, TValue>>>(
                            "Insert<T>",
                            Helper.InsertItems)
                    };
                yield return
                    new object[]
                    {
                        -1,
                        new Named
                            <
                                AddItemsFunc
                                    <TKey, IKeyedItem<TKey, TValue>>>(
                            "Insert<T>",
                            Helper.InsertItems)
                    };
                yield return
                    new object[]
                    {
                        32,
                        new Named
                            <
                                AddItemsFunc
                                    <TKey, IKeyedItem<TKey, TValue>>>(
                            "Add",
                            Helper.AddItemsObject)
                    };
                yield return
                    new object[]
                    {
                        -1,
                        new Named
                            <
                                AddItemsFunc
                                    <TKey, IKeyedItem<TKey, TValue>>>(
                            "Add",
                            Helper.AddItemsObject)
                    };
                yield return
                    new object[]
                    {
                        32,
                        new Named
                            <
                                AddItemsFunc
                                    <TKey, IKeyedItem<TKey, TValue>>>(
                            "Add",
                            Helper.InsertItemsObject)
                    };
                yield return
                    new object[]
                    {
                        -1,
                        new Named
                            <
                                AddItemsFunc
                                    <TKey, IKeyedItem<TKey, TValue>>>(
                            "Add",
                            Helper.InsertItemsObject)
                    };
            }
        }

        public static IEnumerable<object[]> ContainsKeyData
        {
            get
            {
                var sizes = new[]
                {
                    new object[] {0},
                    new object[] {1},
                    new object[] {16},
                    new object[] {33}
                };
                object[][] generatorMethods;
                if (s_keyNullable)
                {
                    generatorMethods = new[]
                    {
                        new object[] {GetNeverNullKeyMethod},
                        new object[] {GetSometimesNullKeyMethod},
                        new object[] {GetAlwaysNullKeyMethod}
                    };
                }
                else
                {
                    generatorMethods = new[]
                    {
                        new object[] {GetNeverNullKeyMethod}
                    };
                }
                return from size in sizes
                       from method in generatorMethods
                       select size.Push(method);
            }
        }

        public static IEnumerable<object[]> DictionaryData
        {
            get
            {
                yield return new object[] {10, 0, 0, 0, 0};
                yield return new object[] {0, 10, 0, 0, 0};
                yield return new object[] {10, 0, 5, 0, 0};
                yield return new object[] {0, 10, 5, 0, 0};
                yield return new object[] {10, 10, 10, 0, 0};
                yield return new object[] {10, 0, 0, 5, 0};
                yield return new object[] {0, 10, 0, 5, 0};
                yield return new object[] {10, 10, 0, 10, 0};
                yield return new object[] {10, 0, 3, 3, 0};
                yield return new object[] {0, 10, 3, 3, 0};
                yield return new object[] {10, 10, 5, 5, 0};
                yield return new object[] {10, 0, 0, 0, 32};
                yield return new object[] {0, 10, 0, 0, 32};
                yield return new object[] {10, 0, 5, 0, 32};
                yield return new object[] {0, 10, 5, 0, 32};
                yield return new object[] {10, 10, 10, 0, 32};
                yield return new object[] {10, 0, 0, 5, 32};
                yield return new object[] {0, 10, 0, 5, 32};
                yield return new object[] {10, 10, 0, 10, 32};
                yield return new object[] {10, 0, 3, 3, 32};
                yield return new object[] {0, 10, 3, 3, 32};
                yield return new object[] {10, 10, 5, 5, 32};
            }
        }

        public abstract TKey GetKeyForItem(TValue item);
        public abstract TValue GenerateValue();

        public object GenerateValueObject()
        {
            return GenerateValue();
        }

        private static IKeyedItem<TKey, TValue> GetNeverNullKey(
            Func<TValue> getValue,
            Func<TValue, TKey> getKeyForItem)
        {
            TValue item = getValue();
            return new KeyedItem<TKey, TValue>(
                getKeyForItem(item),
                item);
        }

        private static IKeyedItem<TKey, TValue> GetSometimesNullKey(
            Func<TValue> getValue,
            Func<TValue, TKey> getKeyForItem)
        {
            TValue item = getValue();
            return
                new KeyedItem<TKey, TValue>(
                    (s_sometimesNullIndex++ & 1) == 0
                        ? default(TKey)
                        : getKeyForItem(item),
                    item);
        }

        private static IKeyedItem<TKey, TValue> GetAlwaysNullKey(
            Func<TValue> getValue,
            Func<TValue, TKey> getKeyForItem)
        {
            return new KeyedItem<TKey, TValue>(
                default(TKey),
                getValue());
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void AddNullKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TValue item1 = GenerateValue();
            TValue item3 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);

            // Verify Adding a value where the key is null
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);

            var tmpKeyedItem = new KeyedItem<TKey, TValue>(
                default(TKey),
                item3);
            keys = keys.Push(key1);
            items = items.Push(keyedItem1, tmpKeyedItem);
            itemsWithKeys = itemsWithKeys.Push(keyedItem1);

            collection.Add(keyedItem1);
            collection.Add(tmpKeyedItem);

            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void AddExistingKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TValue item1 = GenerateValue();
            TValue item3 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);

            //[] Verify setting a value where the key already exists in the collection
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);

            var tmpKeyedItem = new KeyedItem<TKey, TValue>(key1, item3);
            keys = keys.Push(key1);
            items = items.Push(keyedItem1);
            itemsWithKeys = itemsWithKeys.Push(keyedItem1);

            collection.Add(keyedItem1);

            AssertExtensions.Throws<ArgumentException>(null, () => collection.Add(tmpKeyedItem));

            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void AddUniqueKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TValue item1 = GenerateValue();
            TValue item3 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            TKey key3 = GetKeyForItem(item3);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);

            //[] Verify setting a value where the key is unique
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);

            var tmpKeyedItem = new KeyedItem<TKey, TValue>(key3, item3);
            keys = keys.Push(key1, key3);
            items = items.Push(keyedItem1, tmpKeyedItem);
            itemsWithKeys = itemsWithKeys.Push(keyedItem1, tmpKeyedItem);

            collection.Add(keyedItem1);
            collection.Add(tmpKeyedItem);

            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void NonGenericAddNullKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TValue item1 = GenerateValue();
            TValue item3 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            IList nonGenericCollection = collection;
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            var tmpKeyedItem = new KeyedItem<TKey, TValue>(
                default(TKey),
                item3);
            keys = keys.Push(key1);
            items = items.Push(keyedItem1, tmpKeyedItem);
            itemsWithKeys = itemsWithKeys.Push(keyedItem1);

            collection.Add(keyedItem1);
            nonGenericCollection.Add(tmpKeyedItem);

            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void NonGenericAddExistingKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TValue item1 = GenerateValue();
            TValue item3 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            IList nonGenericCollection = collection;
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            var tmpKeyedItem = new KeyedItem<TKey, TValue>(key1, item3);
            keys = keys.Push(key1);
            items = items.Push(keyedItem1);
            itemsWithKeys = itemsWithKeys.Push(keyedItem1);

            collection.Add(keyedItem1);

            AssertExtensions.Throws<ArgumentException>(null, () => nonGenericCollection.Add(tmpKeyedItem));
            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void NonGenericAddUniqueKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TValue item1 = GenerateValue();
            TValue item3 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            TKey key3 = GetKeyForItem(item3);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            IList nonGenericCollection = collection;
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            var tmpKeyedItem = new KeyedItem<TKey, TValue>(key3, item3);
            keys = keys.Push(key1, key3);
            items = items.Push(keyedItem1, tmpKeyedItem);
            itemsWithKeys = itemsWithKeys.Push(keyedItem1, tmpKeyedItem);

            collection.Add(keyedItem1);
            nonGenericCollection.Add(tmpKeyedItem);
            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void ChangeItemKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            TValue item1 = GenerateValue();
            TValue item2 = GenerateValue();
            TValue item3 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            TKey key2 = GetKeyForItem(item2);
            TKey key3 = GetKeyForItem(item3);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);
            var keyedItem2 = new KeyedItem<TKey, TValue>(key2, item2);
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            collection.Add(keyedItem1);
            collection.Add(keyedItem2);
            keys = keys.Push(key1, key2);
            items = items.Push(keyedItem1, keyedItem2);
            itemsWithKeys =
                itemsWithKeys.Push(
                    new[] {keyedItem1, keyedItem2}.Where(
                        ki => ki.Key != null)
                                                  .ToArray
                        <IKeyedItem<TKey, TValue>>());

            collection.MyChangeItemKey(keyedItem2, key3);
            keyedItem2.Key = key3;
            keys[keys.Length - 1] = key3;
            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void ChangeItemKeyThrowsPreexistingKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            TValue item1 = GenerateValue();
            TValue item2 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            TKey key2 = GetKeyForItem(item2);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);
            var keyedItem2 = new KeyedItem<TKey, TValue>(key2, item2);
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            collection.Add(keyedItem1);
            collection.Add(keyedItem2);
            keys = keys.Push(key1, key2);
            items = items.Push(keyedItem1, keyedItem2);
            itemsWithKeys =
                itemsWithKeys.Push(
                    new[] {keyedItem1, keyedItem2}.Where(
                        ki => ki.Key != null)
                                                  .ToArray
                        <IKeyedItem<TKey, TValue>>());

            AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(keyedItem2, key1));
            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void ChangeItemKeySameKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            TValue item1 = GenerateValue();
            TValue item2 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            TKey key2 = GetKeyForItem(item2);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);
            var keyedItem2 = new KeyedItem<TKey, TValue>(key2, item2);
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            collection.Add(keyedItem1);
            collection.Add(keyedItem2);
            keys = keys.Push(key1, key2);
            items = items.Push(keyedItem1, keyedItem2);
            itemsWithKeys =
                itemsWithKeys.Push(
                    new[] {keyedItem1, keyedItem2}.Where(
                        ki => ki.Key != null)
                                                  .ToArray
                        <IKeyedItem<TKey, TValue>>());

            collection.MyChangeItemKey(keyedItem2, key2);

            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void ChangeItemDoesNotExistThrows(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            TValue item1 = GenerateValue();
            TValue item2 = GenerateValue();
            TValue item3 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            TKey key2 = GetKeyForItem(item2);
            TKey key3 = GetKeyForItem(item3);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);
            var keyedItem2 = new KeyedItem<TKey, TValue>(key2, item2);
            var keyedItem3 = new KeyedItem<TKey, TValue>(key3, item3);
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            collection.Add(keyedItem1);
            collection.Add(keyedItem2);
            keys = keys.Push(key1, key2);
            items = items.Push(keyedItem1, keyedItem2);
            itemsWithKeys =
                itemsWithKeys.Push(
                    new[] {keyedItem1, keyedItem2}.Where(
                        ki => ki.Key != null)
                                                  .ToArray
                        <IKeyedItem<TKey, TValue>>());
            AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(keyedItem3, key3));
            AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(keyedItem3, key2));
            var tempKeyedItem = new KeyedItem<TKey, TValue>(key1, item2);
            AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(tempKeyedItem, key2));
            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void ChangeItemKeyNullToNull(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                IKeyedItem<TKey, TValue>[] items;
                IKeyedItem<TKey, TValue>[] itemsWithKeys;
                TValue item1 = GenerateValue();
                TValue item2 = GenerateValue();
                TKey key1 = GetKeyForItem(item1);
                var keyedItem1 = new KeyedItem<TKey, TValue>(
                    key1,
                    item1);
                var collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
                collection.AddItems(
                    generateKeyedItem.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);
                collection.Add(keyedItem1);
                var tempKeyedItem =
                    new KeyedItem<TKey, TValue>(default(TKey), item2);
                collection.Add(tempKeyedItem);
                keys = keys.Push(key1);
                items = items.Push(keyedItem1, tempKeyedItem);
                itemsWithKeys =
                    itemsWithKeys.Push(
                        new[] {keyedItem1}.Where(ki => ki.Key != null)
                                          .ToArray
                            <IKeyedItem<TKey, TValue>>());

                collection.MyChangeItemKey(tempKeyedItem, default(TKey));
                collection.Verify(keys, items, itemsWithKeys);
            }
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void ChangeItemKeyNullToNonNull(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                IKeyedItem<TKey, TValue>[] items;
                IKeyedItem<TKey, TValue>[] itemsWithKeys;
                TValue item1 = GenerateValue();
                TValue item2 = GenerateValue();
                TKey key1 = GetKeyForItem(item1);
                TKey key2 = GetKeyForItem(item2);
                var keyedItem1 = new KeyedItem<TKey, TValue>(
                    key1,
                    item1);
                var collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
                collection.AddItems(
                    generateKeyedItem.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);
                collection.Add(keyedItem1);
                var tempKeyedItem =
                    new KeyedItem<TKey, TValue>(default(TKey), item2);
                collection.Add(tempKeyedItem);
                keys = keys.Push(key1);
                items = items.Push(keyedItem1, tempKeyedItem);
                itemsWithKeys =
                    itemsWithKeys.Push(
                        new[] {keyedItem1, tempKeyedItem}.Where(
                            ki => ki.Key != null)
                                                         .ToArray
                            <IKeyedItem<TKey, TValue>>());

                collection.MyChangeItemKey(tempKeyedItem, key2);
                tempKeyedItem.Key = key2;
                collection.Verify(keys, items, itemsWithKeys);
            }
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void ChangeItemKeyNonNullToNull(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                IKeyedItem<TKey, TValue>[] items;
                IKeyedItem<TKey, TValue>[] itemsWithKeys;
                TValue item1 = GenerateValue();
                TValue item2 = GenerateValue();
                TKey key1 = GetKeyForItem(item1);
                TKey key2 = GetKeyForItem(item2);
                var keyedItem1 = new KeyedItem<TKey, TValue>(
                    key1,
                    item1);
                var keyedItem2 = new KeyedItem<TKey, TValue>(
                    key2,
                    item2);
                var collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
                collection.AddItems(
                    generateKeyedItem.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);
                collection.Add(keyedItem1);
                collection.Add(keyedItem2);
                keys = keys.Push(key1);
                items = items.Push(keyedItem1, keyedItem2);
                itemsWithKeys =
                    itemsWithKeys.Push(
                        new[] {keyedItem1}.Where(ki => ki.Key != null)
                                          .ToArray
                            <IKeyedItem<TKey, TValue>>());

                collection.MyChangeItemKey(keyedItem2, default(TKey));
                collection.Verify(keys, items, itemsWithKeys);
            }
        }

        [Theory]
        [MemberData(nameof(CollectionSizes))]
        public void ChangeItemKeyNullItemNotPresent(int collectionSize)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                TValue[] items;
                TValue[] itemsWithKeys;
                TValue item1 = GenerateValue();
                TValue item2 = GenerateValue();
                TKey key1 = GetKeyForItem(item1);
                TKey key2 = GetKeyForItem(item2);
                var collection =
                    new TestKeyedCollection<TKey, TValue>(GetKeyForItem);
                collection.AddItems(
                    GenerateValue,
                    GetKeyForItem,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);
                collection.Add(item1);
                AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(default(TValue), key2));
                collection.Verify(
                    keys.Push(key1),
                    items.Push(item1),
                    itemsWithKeys.Push(item1));
            }
        }

        [Theory]
        [MemberData(nameof(CollectionSizes))]
        public void ChangeItemKeyNullItemPresent(int collectionSize)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                TValue[] items;
                TValue[] itemsWithKeys;
                TValue item1 = GenerateValue();
                TValue item2 = GenerateValue();
                TKey key1 = GetKeyForItem(item1);
                TKey key2 = GetKeyForItem(item2);
                var collection =
                    new TestKeyedCollection<TKey, TValue>(GetKeyForItem);
                collection.AddItems(
                    GenerateValue,
                    GetKeyForItem,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);
                collection.Add(item1);
                collection.Add(default(TValue));
                collection.MyChangeItemKey(default(TValue), key2);
                collection.Verify(
                    keys.Push(key1),
                    items.Push(item1, default(TValue)),
                    itemsWithKeys.Push(item1));
            }
        }

        [Theory]
        [MemberData(nameof(CollectionSizes))]
        public void ChangeItemKeyNullKeyNotPresent(int collectionSize)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                TValue[] items;
                TValue[] itemsWithKeys;
                TValue item1 = GenerateValue();
                var collection =
                    new TestKeyedCollection<TKey, TValue>(GetKeyForItem);
                collection.AddItems(
                    GenerateValue,
                    GetKeyForItem,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);
                collection.Add(item1);
                collection.MyChangeItemKey(item1, default(TKey));
                collection.Verify(
                    keys,
                    items.Push(item1),
                    itemsWithKeys);
            }
        }

        [Theory]
        [MemberData(nameof(CollectionSizes))]
        public void ChangeItemKeyNullKeyPresent(int collectionSize)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                TValue[] items;
                TValue[] itemsWithKeys;
                TValue item1 = GenerateValue();
                var collection =
                    new TestKeyedCollection<TKey, TValue>(GetKeyForItem);
                collection.AddItems(
                    GenerateValue,
                    GetKeyForItem,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);
                collection.Add(item1);
                collection.Add(default(TValue));
                collection.MyChangeItemKey(item1, default(TKey));
                collection.Verify(
                    keys,
                    items.Push(item1, default(TValue)),
                    itemsWithKeys);
            }
        }

        [Theory]
        [MemberData(nameof(CollectionSizes))]
        public void ChangeItemKeyNullItemNullKeyPresent(
            int collectionSize)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                TValue[] items;
                TValue[] itemsWithKeys;
                TValue item1 = GenerateValue();
                var collection =
                    new TestKeyedCollection<TKey, TValue>(GetKeyForItem);
                collection.AddItems(
                    GenerateValue,
                    GetKeyForItem,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);
                collection.Add(item1);
                collection.Add(default(TValue));
                collection.MyChangeItemKey(
                    default(TValue),
                    default(TKey));
                collection.Verify(
                    keys,
                    items.Push(item1, default(TValue)),
                    itemsWithKeys);
            }
        }

        [Theory]
        [MemberData(nameof(ClassData2))]
        public void ChangeItemKeyKeyAlreadyChanged(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            TValue item1 = GenerateValue();
            TValue item2 = GenerateValue();
            TValue item3 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            TKey key2 = GetKeyForItem(item2);
            TKey key3 = GetKeyForItem(item3);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);
            var keyedItem2 = new KeyedItem<TKey, TValue>(key2, item2);
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);

            collection.Add(keyedItem1);
            collection.Add(keyedItem2);
            keys = keys.Push(key1, collectionSize >= 32 ? key2 : key3);
            items = items.Push(keyedItem1, keyedItem2);
            itemsWithKeys = itemsWithKeys.Push(keyedItem1, keyedItem2);
            keyedItem2.Key = key3;
            if (collectionSize >= 32)
            {
                AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(keyedItem2, key3));
            }
            else
            {
                collection.MyChangeItemKey(keyedItem2, key3);
            }
            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData2))]
        public void ChangeItemKeyKeyAlreadyChangedNewKeyIsOldKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            TValue item1 = GenerateValue();
            TValue item2 = GenerateValue();
            TValue item3 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            TKey key2 = GetKeyForItem(item2);
            TKey key3 = GetKeyForItem(item3);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);
            var keyedItem2 = new KeyedItem<TKey, TValue>(key2, item2);
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);

            collection.Add(keyedItem1);
            collection.Add(keyedItem2);
            keys = keys.Push(key1, collectionSize >= 32 ? key2 : key3);
            items = items.Push(keyedItem1, keyedItem2);
            itemsWithKeys = itemsWithKeys.Push(keyedItem1, keyedItem2);
            keyedItem2.Key = key3;
            if (collectionSize >= 32)
            {
                AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(keyedItem2, key2));
            }
            else
            {
                collection.MyChangeItemKey(keyedItem2, key2);
            }
            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData2))]
        public void ChangeItemKeyKeyAlreadyChangedNewKeyIsDifferent(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            TValue item1 = GenerateValue();
            TValue item2 = GenerateValue();
            TValue item3 = GenerateValue();
            TValue item4 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            TKey key2 = GetKeyForItem(item2);
            TKey key3 = GetKeyForItem(item3);
            TKey key4 = GetKeyForItem(item4);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);
            var keyedItem2 = new KeyedItem<TKey, TValue>(key2, item2);
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);

            collection.Add(keyedItem1);
            collection.Add(keyedItem2);
            keys = keys.Push(key1, collectionSize >= 32 ? key2 : key3);
            items = items.Push(keyedItem1, keyedItem2);
            itemsWithKeys = itemsWithKeys.Push(keyedItem1, keyedItem2);
            keyedItem2.Key = key3;
            if (collectionSize >= 32)
            {
                AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(keyedItem2, key4));
            }
            else
            {
                collection.MyChangeItemKey(keyedItem2, key4);
            }
            collection.Verify(keys, items, itemsWithKeys);
        }

        [Theory]
        [MemberData(nameof(ClassData2))]
        public void ChangeItemKeyNullToNewKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                IKeyedItem<TKey, TValue>[] items;
                IKeyedItem<TKey, TValue>[] itemsWithKeys;
                TValue item1 = GenerateValue();
                TValue item2 = GenerateValue();
                TValue item3 = GenerateValue();
                TKey key1 = GetKeyForItem(item1);
                TKey key3 = GetKeyForItem(item3);
                var keyedItem1 = new KeyedItem<TKey, TValue>(
                    key1,
                    item1);
                var collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
                collection.AddItems(
                    generateKeyedItem.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);

                collection.Add(keyedItem1);
                var tempKeyedItem =
                    new KeyedItem<TKey, TValue>(default(TKey), item2);
                collection.Add(tempKeyedItem);
                keys = keys.Push(key1);
                if (collectionSize < 32)
                {
                    keys = keys.Push(key3);
                }
                items = items.Push(keyedItem1, tempKeyedItem);
                itemsWithKeys = itemsWithKeys.Push(keyedItem1);
                if (collectionSize < 32)
                {
                    itemsWithKeys = itemsWithKeys.Push(tempKeyedItem);
                }
                tempKeyedItem.Key = key3;
                if (collectionSize >= 32)
                {
                    AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(tempKeyedItem, key3));
                }
                else
                {
                    collection.MyChangeItemKey(tempKeyedItem, key3);
                }
                collection.Verify(keys, items, itemsWithKeys);
            }
        }

        [Theory]
        [MemberData(nameof(ClassData2))]
        public void ChangeItemKeyNullToOldKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                IKeyedItem<TKey, TValue>[] items;
                IKeyedItem<TKey, TValue>[] itemsWithKeys;
                TValue item1 = GenerateValue();
                TValue item2 = GenerateValue();
                TValue item3 = GenerateValue();
                TKey key1 = GetKeyForItem(item1);
                TKey key3 = GetKeyForItem(item3);
                var keyedItem1 = new KeyedItem<TKey, TValue>(
                    key1,
                    item1);
                var collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
                collection.AddItems(
                    generateKeyedItem.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);

                collection.Add(keyedItem1);
                var tempKeyedItem =
                    new KeyedItem<TKey, TValue>(default(TKey), item2);
                collection.Add(tempKeyedItem);
                keys = keys.Push(key1);
                if (collectionSize < 32)
                {
                    keys = keys.Push(key3);
                }
                items = items.Push(keyedItem1, tempKeyedItem);
                itemsWithKeys = itemsWithKeys.Push(keyedItem1);
                if (collectionSize < 32)
                {
                    itemsWithKeys = itemsWithKeys.Push(tempKeyedItem);
                }
                tempKeyedItem.Key = key3;
                if (collectionSize >= 32)
                {
                    AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(tempKeyedItem, default(TKey)));
                }
                else
                {
                    collection.MyChangeItemKey(
                        tempKeyedItem,
                        default(TKey));
                }
                collection.Verify(keys, items, itemsWithKeys);
            }
        }

        [Theory]
        [MemberData(nameof(ClassData2))]
        public void ChangeItemKeyNullToOtherKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                IKeyedItem<TKey, TValue>[] items;
                IKeyedItem<TKey, TValue>[] itemsWithKeys;
                TValue item1 = GenerateValue();
                TValue item2 = GenerateValue();
                TValue item3 = GenerateValue();
                TValue item4 = GenerateValue();
                TKey key1 = GetKeyForItem(item1);
                TKey key3 = GetKeyForItem(item3);
                TKey key4 = GetKeyForItem(item4);
                var keyedItem1 = new KeyedItem<TKey, TValue>(
                    key1,
                    item1);
                var collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
                collection.AddItems(
                    generateKeyedItem.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);

                collection.Add(keyedItem1);
                var tempKeyedItem =
                    new KeyedItem<TKey, TValue>(default(TKey), item2);
                collection.Add(tempKeyedItem);
                keys = keys.Push(key1);
                if (collectionSize < 32)
                {
                    keys = keys.Push(key3);
                }
                items = items.Push(keyedItem1, tempKeyedItem);
                itemsWithKeys = itemsWithKeys.Push(keyedItem1);
                if (collectionSize < 32)
                {
                    itemsWithKeys = itemsWithKeys.Push(tempKeyedItem);
                }
                tempKeyedItem.Key = key3;
                if (collectionSize >= 32)
                {
                    AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(tempKeyedItem, key4));
                }
                else
                {
                    collection.MyChangeItemKey(tempKeyedItem, key4);
                }
                collection.Verify(keys, items, itemsWithKeys);
            }
        }

        [Theory]
        [MemberData(nameof(ClassData2))]
        public void ChangeItemKeySetKeyNonNullToNull(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                IKeyedItem<TKey, TValue>[] items;
                IKeyedItem<TKey, TValue>[] itemsWithKeys;
                TValue item1 = GenerateValue();
                TValue item2 = GenerateValue();
                TKey key1 = GetKeyForItem(item1);
                TKey key2 = GetKeyForItem(item2);
                var keyedItem1 = new KeyedItem<TKey, TValue>(
                    key1,
                    item1);
                var keyedItem2 = new KeyedItem<TKey, TValue>(
                    key2,
                    item2);
                var collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
                collection.AddItems(
                    generateKeyedItem.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);

                collection.Add(keyedItem1);
                collection.Add(keyedItem2);
                keys = keys.Push(key1);
                items = items.Push(keyedItem1, keyedItem2);
                itemsWithKeys = itemsWithKeys.Push(keyedItem1);
                keyedItem2.Key = default(TKey);
                collection.MyChangeItemKey(keyedItem2, default(TKey));
                collection.Verify(keys, items, itemsWithKeys);
            }
        }

        [Theory]
        [MemberData(nameof(ClassData2))]
        public void ChangeItemKeySetKeyNonNullToNullChangeKeyNonNull(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                IKeyedItem<TKey, TValue>[] items;
                IKeyedItem<TKey, TValue>[] itemsWithKeys;
                TValue item1 = GenerateValue();
                TValue item2 = GenerateValue();
                TKey key1 = GetKeyForItem(item1);
                TKey key2 = GetKeyForItem(item2);
                var keyedItem1 = new KeyedItem<TKey, TValue>(
                    key1,
                    item1);
                var keyedItem2 = new KeyedItem<TKey, TValue>(
                    key2,
                    item2);
                var collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
                collection.AddItems(
                    generateKeyedItem.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);

                collection.Add(keyedItem1);
                collection.Add(keyedItem2);
                keys = keys.Push(key1);
                items = items.Push(keyedItem1, keyedItem2);
                itemsWithKeys = itemsWithKeys.Push(keyedItem1);
                keyedItem2.Key = default(TKey);
                if (collectionSize >= 32)
                {
                    AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(keyedItem2, key2));
                }
                else
                {
                    collection.MyChangeItemKey(keyedItem2, key2);
                }
                collection.Verify(keys, items, itemsWithKeys);
            }
        }

        [Theory]
        [MemberData(nameof(ClassData2))]
        public void
            ChangeItemKeySetKeyNonNullToNullChangeKeySomethingElse(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            if (default(TKey) == null)
            {
                TKey[] keys;
                IKeyedItem<TKey, TValue>[] items;
                IKeyedItem<TKey, TValue>[] itemsWithKeys;
                TValue item1 = GenerateValue();
                TValue item2 = GenerateValue();
                TValue item4 = GenerateValue();
                TKey key1 = GetKeyForItem(item1);
                TKey key2 = GetKeyForItem(item2);
                TKey key4 = GetKeyForItem(item4);
                var keyedItem1 = new KeyedItem<TKey, TValue>(
                    key1,
                    item1);
                var keyedItem2 = new KeyedItem<TKey, TValue>(
                    key2,
                    item2);
                var collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
                collection.AddItems(
                    generateKeyedItem.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    collectionSize,
                    out keys,
                    out items,
                    out itemsWithKeys);

                collection.Add(keyedItem1);
                collection.Add(keyedItem2);
                keys = keys.Push(key1);
                items = items.Push(keyedItem1, keyedItem2);
                itemsWithKeys = itemsWithKeys.Push(keyedItem1);
                keyedItem2.Key = default(TKey);
                if (collectionSize >= 32 && keyedItem2.Key != null)
                {
                    AssertExtensions.Throws<ArgumentException>(null, () => collection.MyChangeItemKey(keyedItem2, key4));
                }
                else
                {
                    collection.MyChangeItemKey(keyedItem2, key4);
                }
                collection.Verify(keys, items, itemsWithKeys);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(25)]
        [InlineData(33)]
        public void Clear(int collectionSize)
        {
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            collection.AddItems(
                GetNeverNullKeyMethod.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            bool haveDict = collection.GetDictionary() != null;
            collection.Clear();
            collection.Verify(
                new TKey[0],
                new IKeyedItem<TKey, TValue>[0],
                new IKeyedItem<TKey, TValue>[0]);
            Assert.Equal(haveDict, collection.GetDictionary() != null);
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void Contains(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            if (s_keyNullable)
            {
                Assert.Throws<ArgumentNullException>(
                    () => collection.Contains(default(TKey)));
            }
            else
            {
                Assert.False(collection.Contains(default(TKey)));
            }
        }

        private void VerifyDictionary(
            KeyedCollection<TKey, IKeyedItem<TKey, TValue>> dictionary,
            TKey[] expectedKeys,
            IKeyedItem<TKey, TValue>[] expectedItems)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }
            if (expectedKeys.Length != expectedItems.Length)
            {
                throw new ArgumentException(
                    "Expected keys length and expected items length must be the same.");
            }
            Assert.Equal(expectedItems.Length, dictionary.Count);

            for (var i = 0; i < expectedKeys.Length; ++i)
            {
                Assert.Equal(
                    expectedItems[i],
                    dictionary[expectedKeys[i]]);
            }
        }

        [Theory]
        [MemberData(nameof(ThresholdData))]
        public void Threshold(
            int collectionDictionaryThreshold,
            Named<AddItemsFunc<TKey, IKeyedItem<TKey, TValue>>> addItems)
        {
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            if (collectionDictionaryThreshold >= 0)
            {
                var collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>(
                        collectionDictionaryThreshold);
                // dictionary is created when the threshold is exceeded
                addItems.Value(
                    collection,
                    GetNeverNullKeyMethod.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    collectionDictionaryThreshold,
                    out keys,
                    out items,
                    out itemsWithKeys);
                Assert.Null(collection.GetDictionary());

                collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>(
                        collectionDictionaryThreshold);
                addItems.Value(
                    collection,
                    GetNeverNullKeyMethod.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    collectionDictionaryThreshold + 1,
                    out keys,
                    out items,
                    out itemsWithKeys);
                Assert.NotNull(collection.GetDictionary());
                VerifyDictionary(collection, keys, itemsWithKeys);
            }
            else
            {
                var collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>(
                        collectionDictionaryThreshold);
                // dictionary is created when the threshold is exceeded
                addItems.Value(
                    collection,
                    GetNeverNullKeyMethod.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    1024,
                    out keys,
                    out items,
                    out itemsWithKeys);
                Assert.Null(collection.GetDictionary());

                collection =
                    new TestKeyedCollectionOfIKeyedItem<TKey, TValue>(
                        collectionDictionaryThreshold);
                addItems.Value(
                    collection,
                    GetNeverNullKeyMethod.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    2048,
                    out keys,
                    out items,
                    out itemsWithKeys);
                Assert.Null(collection.GetDictionary());
            }
        }

        [Theory]
        [MemberData(nameof(ContainsKeyData))]
        public void ContainsKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            IKeyedItem<TKey, TValue> itemNotIn =
                generateKeyedItem.Value(GenerateValue, GetKeyForItem);
            // this is to make overload resolution pick the correct Contains function. replacing keyNotIn with null causes the Contains<TValue> overload to be used. We want the Contains<TKey> version.
            TKey keyNotIn = itemNotIn.Key;
            if (keyNotIn == null)
            {
                Assert.Throws<ArgumentNullException>(
                    () => collection.Contains(keyNotIn));
            }
            else
            {
                Assert.False(collection.Contains(keyNotIn));
            }
            foreach (TKey k in keys)
            {
                TKey key = k;
                if (key == null)
                {
                    Assert.Throws<ArgumentNullException>(
                        () => collection.Contains(key));
                    continue;
                }
                Assert.True(collection.Contains(key));
            }
        }

        [Theory]
        [MemberData(nameof(ContainsKeyData))]
        public void RemoveKey(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            collection.Verify(keys, items, itemsWithKeys);

            IKeyedItem<TKey, TValue> itemNotIn =
                generateKeyedItem.Value(GenerateValue, GetKeyForItem);
            // this is to make overload resolution pick the correct Contains function. replacing keyNotIn with null causes the Contains<TValue> overload to be used. We want the Contains<TKey> version.
            TKey keyNotIn = itemNotIn.Key;
            if (keyNotIn == null)
            {
                Assert.Throws<ArgumentNullException>(
                    () => collection.Remove(keyNotIn));
            }
            else
            {
                Assert.False(collection.Remove(keyNotIn));
            }
            collection.Verify(keys, items, itemsWithKeys);
            var tempKeys = (TKey[]) keys.Clone();
            var tempItems = (IKeyedItem<TKey, TValue>[]) items.Clone();
            var tempItemsWithKeys =
                (IKeyedItem<TKey, TValue>[]) itemsWithKeys.Clone();

            for (var i = 0; i < itemsWithKeys.Length; i++)
            {
                TKey key = keys[i];
                if (key == null)
                {
                    Assert.Throws<ArgumentNullException>(
                        () => collection.Remove(key));
                }
                else
                {
                    Assert.True(collection.Remove(key));
                    tempItems =
                        tempItems.RemoveAt(
                            Array.IndexOf(tempItems, itemsWithKeys[i]));
                    tempItemsWithKeys = itemsWithKeys.Slice(
                        i + 1,
                        itemsWithKeys.Length - i - 1);
                    tempKeys = keys.Slice(i + 1, keys.Length - i - 1);
                }
                collection.Verify(
                    tempKeys,
                    tempItems,
                    tempItemsWithKeys);
            }
        }

        [Theory]
        [MemberData(nameof(ContainsKeyData))]
        public void KeyIndexer(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TKey[] keys;
            IKeyedItem<TKey, TValue>[] items;
            IKeyedItem<TKey, TValue>[] itemsWithKeys;
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>();
            collection.AddItems(
                generateKeyedItem.Value.Bind(
                    GenerateValue,
                    GetKeyForItem),
                ki => ki.Key,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            IKeyedItem<TKey, TValue> itemNotIn =
                generateKeyedItem.Value(GenerateValue, GetKeyForItem);
            // this is to make overload resolution pick the correct Contains function. replacing keyNotIn with null causes the Contains<TValue> overload to be used. We want the Contains<TKey> version.
            TKey keyNotIn = itemNotIn.Key;
            if (keyNotIn == null)
            {
                Assert.Throws<ArgumentNullException>(
                    () => collection[keyNotIn]);
            }
            else
            {
                Assert.Throws<KeyNotFoundException>(
                    () => collection[keyNotIn]);
            }
            foreach (TKey k in keys)
            {
                TKey key = k;
                if (key == null)
                {
                    Assert.Throws<ArgumentNullException>(
                        () => collection[key]);
                    continue;
                }
                IKeyedItem<TKey, TValue> tmp = collection[key];
            }
        }

        [Theory]
        [MemberData(nameof(CollectionSizes))]
        public void KeyIndexerSet(int collectionSize)
        {
            TKey[] keys;
            TValue[] items;
            TValue[] itemsWithKeys;
            var collection =
                new TestKeyedCollection<TKey, TValue>(GetKeyForItem);
            collection.AddItems(
                GenerateValue,
                GetKeyForItem,
                collectionSize,
                out keys,
                out items,
                out itemsWithKeys);
            foreach (TValue item in itemsWithKeys)
            {
                collection[collection.IndexOf(item)] = item;
            }
        }

        [Theory]
        [MemberData(nameof(DictionaryData))]
        public void Dictionary(
            int addCount,
            int insertCount,
            int removeCount,
            int removeKeyCount,
            int collectionDictionaryThreshold)
        {
            var collection =
                new TestKeyedCollectionOfIKeyedItem<TKey, TValue>(
                    collectionDictionaryThreshold);
            TKey[] tempKeys;
            IKeyedItem<TKey, TValue>[] tempItems;
            IKeyedItem<TKey, TValue>[] tempItemsWithKeys;
            var keys = new TKey[0];
            var itemsWithKeys = new IKeyedItem<TKey, TValue>[0];

            if (addCount > 0)
            {
                collection.AddItems(
                    GetNeverNullKeyMethod.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    addCount,
                    out tempKeys,
                    out tempItems,
                    out tempItemsWithKeys);
                keys = keys.Push(tempKeys);
                itemsWithKeys = itemsWithKeys.Push(tempItemsWithKeys);
                VerifyDictionary(collection, keys, itemsWithKeys);
            }
            if (insertCount > 0)
            {
                collection.InsertItems(
                    GetNeverNullKeyMethod.Value.Bind(
                        GenerateValue,
                        GetKeyForItem),
                    ki => ki.Key,
                    insertCount,
                    out tempKeys,
                    out tempItems,
                    out tempItemsWithKeys);
                keys = keys.Push(tempKeys);
                itemsWithKeys = itemsWithKeys.Push(tempItemsWithKeys);
                VerifyDictionary(collection, keys, itemsWithKeys);
            }

            if (removeCount > 0)
            {
                for (var i = 0; i < removeCount; i++)
                {
                    int index = (((i*43691 << 2)/7 >> 1)*5039)
                                %collection.Count;
                    collection.RemoveAt(index);
                    keys = keys.RemoveAt(index);
                    itemsWithKeys = itemsWithKeys.RemoveAt(index);
                    VerifyDictionary(collection, keys, itemsWithKeys);
                }
            }

            if (removeKeyCount > 0)
            {
                for (var i = 0; i < removeCount; i++)
                {
                    int index = (((i*127 << 2)/7 >> 1)*5039)
                                %collection.Count;
                    IKeyedItem<TKey, TValue> item = collection[index];
                    collection.Remove(item.Key);
                    keys = keys.RemoveAt(index);
                    itemsWithKeys = itemsWithKeys.RemoveAt(index);
                    VerifyDictionary(collection, keys, itemsWithKeys);
                }
            }
        }

        [Theory]
        [MemberData(nameof(ClassData))]
        public void Insert(
            int collectionSize,
            Named<KeyedCollectionGetKeyedValue<TKey, TValue>>
                generateKeyedItem)
        {
            TValue item1 = GenerateValue();
            TValue item3 = GenerateValue();
            TKey key1 = GetKeyForItem(item1);
            TKey key3 = GetKeyForItem(item3);
            var keyedItem1 = new KeyedItem<TKey, TValue>(key1, item1);

            var inserts =
                new Action
                    <KeyedCollection<TKey, IKeyedItem<TKey, TValue>>,
                        int, IKeyedItem<TKey, TValue>>[]
                {
                    (c, i, item) => c.Insert(i, item),
                    (c, i, item) => ((IList) c).Insert(i, item)
                };

            foreach (
                Action
                    <KeyedCollection<TKey, IKeyedItem<TKey, TValue>>,
                        int, IKeyedItem<TKey, TValue>> i in inserts)
            {
                Action
                    <KeyedCollection<TKey, IKeyedItem<TKey, TValue>>,
                        int, IKeyedItem<TKey, TValue>> insert = i;
                {
                    // Insert key is null
                    TKey[] keys;
                    IKeyedItem<TKey, TValue>[] items;
                    IKeyedItem<TKey, TValue>[] itemsWithKeys;
                    var collection =
                        new TestKeyedCollectionOfIKeyedItem
                            <TKey, TValue>();
                    collection.AddItems(
                        generateKeyedItem.Value.Bind(
                            GenerateValue,
                            GetKeyForItem),
                        ki => ki.Key,
                        collectionSize,
                        out keys,
                        out items,
                        out itemsWithKeys);
                    var tempKeyedItem =
                        new KeyedItem<TKey, TValue>(
                            default(TKey),
                            item3);
                    keys = keys.Push(key1);
                    items = items.Push(keyedItem1, tempKeyedItem);
                    itemsWithKeys = itemsWithKeys.Push(keyedItem1);
                    insert(collection, collection.Count, keyedItem1);
                    insert(collection, collection.Count, tempKeyedItem);
                    collection.Verify(keys, items, itemsWithKeys);
                }

                {
                    // Insert key already exists
                    TKey[] keys;
                    IKeyedItem<TKey, TValue>[] items;
                    IKeyedItem<TKey, TValue>[] itemsWithKeys;
                    var collection =
                        new TestKeyedCollectionOfIKeyedItem
                            <TKey, TValue>();
                    collection.AddItems(
                        generateKeyedItem.Value.Bind(
                            GenerateValue,
                            GetKeyForItem),
                        ki => ki.Key,
                        collectionSize,
                        out keys,
                        out items,
                        out itemsWithKeys);
                    var tempKeyedItem = new KeyedItem<TKey, TValue>(
                        key1,
                        item3);
                    keys = keys.Push(key1);
                    items = items.Push(keyedItem1);
                    itemsWithKeys = itemsWithKeys.Push(keyedItem1);
                    insert(collection, collection.Count, keyedItem1);
                    AssertExtensions.Throws<ArgumentException>(null, () => insert(collection, collection.Count, tempKeyedItem));
                    collection.Verify(keys, items, itemsWithKeys);
                }

                {
                    // Insert key is unique
                    TKey[] keys;
                    IKeyedItem<TKey, TValue>[] items;
                    IKeyedItem<TKey, TValue>[] itemsWithKeys;
                    var collection =
                        new TestKeyedCollectionOfIKeyedItem
                            <TKey, TValue>();
                    collection.AddItems(
                        generateKeyedItem.Value.Bind(
                            GenerateValue,
                            GetKeyForItem),
                        ki => ki.Key,
                        collectionSize,
                        out keys,
                        out items,
                        out itemsWithKeys);
                    var tempKeyedItem = new KeyedItem<TKey, TValue>(
                        key3,
                        item3);
                    keys = keys.Push(key1, key3);
                    items = items.Push(keyedItem1, tempKeyedItem);
                    itemsWithKeys = itemsWithKeys.Push(
                        keyedItem1,
                        tempKeyedItem);
                    insert(collection, collection.Count, keyedItem1);
                    insert(collection, collection.Count, tempKeyedItem);
                    collection.Verify(keys, items, itemsWithKeys);
                }
            }
        }
    }

    public abstract class IListTestKeyedCollection<TKey, TValue> :
        IListTest<KeyedCollection<TKey, TValue>, TValue>
    {
        protected IListTestKeyedCollection()
            : base(false, false, false, false, true, true)
        {
        }

        protected abstract TKey GetKeyForItem(TValue item);

        /// <summary>
        ///     When overridden in a derived class, Gets an instance of the list under test containing the given items.
        /// </summary>
        /// <param name="items">The items to initialize the list with.</param>
        /// <returns>An instance of the list under test containing the given items.</returns>
        protected override KeyedCollection<TKey, TValue> CreateList(
            IEnumerable<TValue> items)
        {
            var ret =
                new TestKeyedCollection<TKey, TValue>(GetKeyForItem);
            if (items == null)
            {
                return ret;
            }
            foreach (TValue item in items)
            {
                ret.Add(item);
            }
            return ret;
        }

        /// <summary>
        ///     When overridden in a derived class, invalidates any enumerators for the given list.
        /// </summary>
        /// <param name="list">The list to invalidate enumerators for.</param>
        /// <returns>The new contents of the list.</returns>
        protected override IEnumerable<TValue> InvalidateEnumerator(
            KeyedCollection<TKey, TValue> list)
        {
            TValue item = CreateItem();
            list.Add(item);
            return list;
        }
    }

    public abstract class IListTestKeyedCollectionBadKey<TKey, TValue> :
        IListTest<KeyedCollection<BadKey<TKey>, TValue>, TValue>
        where TKey : IEquatable<TKey>
    {
        protected IListTestKeyedCollectionBadKey()
            : base(false, false, false, false, true, true)
        {
        }

        /// <summary>
        ///     When overridden in a derived class, Gets an instance of the list under test containing the given items.
        /// </summary>
        /// <param name="items">The items to initialize the list with.</param>
        /// <returns>An instance of the list under test containing the given items.</returns>
        protected override KeyedCollection<BadKey<TKey>, TValue>
            CreateList(IEnumerable<TValue> items)
        {
            var ret =
                new TestKeyedCollection<BadKey<TKey>, TValue>(
                    item => new BadKey<TKey>(GetKeyForItem(item)),
                    new BadKeyComparer<TKey>());
            if (items == null)
            {
                return ret;
            }
            foreach (TValue item in items)
            {
                ret.Add(item);
            }
            return ret;
        }

        /// <summary>
        ///     When overridden in a derived class, invalidates any enumerators for the given list.
        /// </summary>
        /// <param name="list">The list to invalidate enumerators for.</param>
        /// <returns>The new contents of the list.</returns>
        protected override IEnumerable<TValue> InvalidateEnumerator(
            KeyedCollection<BadKey<TKey>, TValue> list)
        {
            TValue item = CreateItem();
            list.Add(item);
            return list;
        }

        protected abstract TKey GetKeyForItem(TValue item);
    }
}
