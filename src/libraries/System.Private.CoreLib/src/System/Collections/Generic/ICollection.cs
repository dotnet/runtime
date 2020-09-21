// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic
{
    // Base interface for all collections, defining enumerators, size, and
    // synchronization methods.
    public interface ICollection<T> : IEnumerable<T>
    {
        // Number of items in the collections.
        int Count { get; }

        bool IsReadOnly { get; }

        void Add(T item);

        void Clear();

        bool Contains(T item);

        // CopyTo copies a collection into an Array, starting at a particular
        // index into the array.
        void CopyTo(T[] array, int arrayIndex);

        bool Remove(T item);
    }
}
