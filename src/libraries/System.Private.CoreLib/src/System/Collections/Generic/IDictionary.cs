// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    // An IDictionary is a possibly unordered set of key-value pairs.
    // Keys can be any non-null object.  Values can be any object.
    // You can look up a value in an IDictionary via the default indexed
    // property, Items.
    public interface IDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    {
        // Interfaces are not serializable
        // The Item property provides methods to read and edit entries
        // in the Dictionary.
        new TValue this[TKey key]
        {
            get;
            set;
        }

        // Returns a collections of the keys in this dictionary.
        new ICollection<TKey> Keys
        {
            get;
        }

        // Returns a collections of the values in this dictionary.
        new ICollection<TValue> Values
        {
            get;
        }

        // Returns whether this dictionary contains a particular key.
        //
        new bool ContainsKey(TKey key);

        // Adds a key-value pair to the dictionary.
        //
        void Add(TKey key, TValue value);

        // Removes a particular key from the dictionary.
        //
        bool Remove(TKey key);

        new bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);

        TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key] => this[key];

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key) => ContainsKey(key);

        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => TryGetValue(key, out value);
    }
}
