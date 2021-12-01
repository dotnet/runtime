// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic
{
    // Base interface for all collections, defining enumerators, size, and
    // synchronization methods.
    public interface ICollection<T> : IEnumerable<T>
    {
        int Count
        {
#if MONO
            [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__ICollection_get_Count), typeof(Array))]
#endif
            get;
        }

        bool IsReadOnly
        {
#if MONO
            [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__ICollection_get_IsReadOnly), typeof(Array))]
#endif
            get;
        }

#if MONO
        [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__ICollection_Add) + "``1", typeof(Array))]
#endif
        void Add(T item);

#if MONO
        [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__ICollection_Clear), typeof(Array))]
#endif
        void Clear();

#if MONO
        [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__ICollection_Contains) + "``1", typeof(Array))]
#endif
        bool Contains(T item);

        // CopyTo copies a collection into an Array, starting at a particular
        // index into the array.
#if MONO
        [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__ICollection_CopyTo) + "``1", typeof(Array))]
#endif
        void CopyTo(T[] array, int arrayIndex);

#if MONO
        [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__ICollection_Remove) + "``1", typeof(Array))]
#endif
        bool Remove(T item);
    }
}
