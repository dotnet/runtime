// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;

namespace System.Collections.Generic
{
    // An IDictionary is a possibly unordered set of key-value pairs.
    // Keys can be any non-null object.  Values can be any object.
    // You can look up a value in an IDictionary via the default indexed
    // property, Items.
    public interface IDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>> where TKey : object
    {
        // Interfaces are not serializable
        // The Item property provides methods to read and edit entries 
        // in the Dictionary.
        TValue this[TKey key]
        {
            get;
            set;
        }

        // Returns a collections of the keys in this dictionary.
        ICollection<TKey> Keys
        {
            get;
        }

        // Returns a collections of the values in this dictionary.
        ICollection<TValue> Values
        {
            get;
        }

        // Returns whether this dictionary contains a particular key.
        //
        bool ContainsKey(TKey key);

        // Adds a key-value pair to the dictionary.
        // 
        void Add(TKey key, TValue value);

        // Removes a particular key from the dictionary.
        //
        bool Remove(TKey key);

        bool TryGetValue(TKey key, out TValue value); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
    }
}
