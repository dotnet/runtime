// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic
{
    // An IList is an ordered collection of objects.  The exact ordering
    // is up to the implementation of the list, ranging from a sorted
    // order to insertion order.
    public interface IList<T> : ICollection<T>
    {
        // The Item property provides methods to read and edit entries in the List.
        T this[int index]
        {
#if MONO
            [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__get_Item) + "``1", typeof(Array))]
#endif
            get;
#if MONO
            [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__set_Item) + "``1", typeof(Array))]
#endif
            set;
        }

        // Returns the index of a particular item, if it is in the list.
        // Returns -1 if the item isn't in the list.
#if MONO
        [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__IndexOf) + "``1", typeof(Array))]
#endif
        int IndexOf(T item);

        // Inserts value into the list at position index.
        // index must be non-negative and less than or equal to the
        // number of elements in the list.  If index equals the number
        // of items in the list, then value is appended to the end.
#if MONO
        [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__Insert) + "``1", typeof(Array))]
#endif
        void Insert(int index, T item);

        // Removes the item at position index.
#if MONO
        [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__RemoveAt), typeof(Array))]
#endif
        void RemoveAt(int index);
    }
}
