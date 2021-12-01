// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic
{
    // Provides a read-only, covariant view of a generic list.
    public interface IReadOnlyList<out T> : IReadOnlyCollection<T>
    {
        T this[int index]
        {
#if MONO
            [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__IReadOnlyList_get_Item) + "``1", typeof(Array))]
#endif
            get;
        }
    }
}
