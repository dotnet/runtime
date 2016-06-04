// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface:  ICollection
** 
** 
**
**
** Purpose: Base interface for all generic collections.
**
** 
===========================================================*/
namespace System.Collections.Generic {
    using System;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;

    // Base interface for all collections, defining enumerators, size, and 
    // synchronization methods.

    // Note that T[] : IList<T>, and we want to ensure that if you use
    // IList<YourValueType>, we ensure a YourValueType[] can be used 
    // without jitting.  Hence the TypeDependencyAttribute on SZArrayHelper.
    // This is a special workaround internally though - see VM\compile.cpp.
    // The same attribute is on IEnumerable<T> and ICollection<T>.
    [TypeDependencyAttribute("System.SZArrayHelper")]
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
        // 
        void CopyTo(T[] array, int arrayIndex);
                
        //void CopyTo(int sourceIndex, T[] destinationArray, int destinationIndex, int count);

        bool Remove(T item);
    }
}
