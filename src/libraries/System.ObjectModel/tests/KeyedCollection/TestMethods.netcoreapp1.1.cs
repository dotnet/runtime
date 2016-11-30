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
    {
        [Theory]
        [MemberData(nameof(ContainsKeyData))]
        public void TryGetValue(
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

            TKey keyNotIn = itemNotIn.Key;
            if (keyNotIn == null)
            {
                IKeyedItem<TKey, TValue> item;
                Assert.Throws<ArgumentNullException>(
                    () => collection.TryGetValue(keyNotIn, out item));
            }
            else
            {
                IKeyedItem<TKey, TValue> item;
                Assert.False(collection.TryGetValue(keyNotIn, out item));
            }
            foreach (TKey k in keys)
            {
                IKeyedItem<TKey, TValue> item;
                TKey key = k;
                if (key == null)
                {
                    Assert.Throws<ArgumentNullException>(
                        () => collection.TryGetValue(key, out item));
                    continue;
                }
                Assert.True(collection.TryGetValue(key, out item));
                Assert.Equal(item.Key, key);
            }
        }
    }
}
