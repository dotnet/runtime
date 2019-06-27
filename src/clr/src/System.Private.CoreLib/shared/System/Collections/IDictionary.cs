// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Collections
{
    // An IDictionary is a possibly unordered set of key-value pairs.
    // Keys can be any non-null object.  Values can be any object.
    // You can look up a value in an IDictionary via the default indexed
    // property, Items.
    public interface IDictionary : ICollection
    {
        // Interfaces are not serializable
        // The Item property provides methods to read and edit entries 
        // in the Dictionary.
        object? this[object key]
        {
            get;
            set;
        }

        // Returns a collections of the keys in this dictionary.
        ICollection Keys {get; }

        // Returns a collections of the values in this dictionary.
        ICollection Values { get; }

        // Returns whether this dictionary contains a particular key.
        bool Contains(object key);

        // Adds a key-value pair to the dictionary.
        void Add(object key, object? value);

        // Removes all pairs from the dictionary.
        void Clear();

        bool IsReadOnly { get; }

        bool IsFixedSize { get; }

        // Returns an IDictionaryEnumerator for this dictionary.
        new IDictionaryEnumerator GetEnumerator();

        // Removes a particular key from the dictionary.
        void Remove(object key);
    }
}
