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

        TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key] => this[key];

        // Returns a collections of the keys in this dictionary.
        new ICollection<TKey> Keys
        {
            get;
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        // Returns a collections of the values in this dictionary.
        new ICollection<TValue> Values
        {
            get;
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        // Returns whether this dictionary contains a particular key.
        //
        new bool ContainsKey(TKey key);

        bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key) => ContainsKey(key);

        // Adds a key-value pair to the dictionary.
        //
        void Add(TKey key, TValue value);

        // Removes a particular key from the dictionary.
        //
        bool Remove(TKey key);

        new bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);

        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => TryGetValue(key, out value);
    }
}
