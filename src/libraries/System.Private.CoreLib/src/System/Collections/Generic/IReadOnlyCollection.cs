// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if MONO
using System.Diagnostics.CodeAnalysis;
#endif

namespace System.Collections.Generic
{
    // Provides a read-only, covariant view of a generic list.
    public interface IReadOnlyCollection<out T> : IEnumerable<T>
    {
        int Count
        {
#if MONO
            [DynamicDependency(nameof(Array.InternalArray__IReadOnlyCollection_get_Count), typeof(Array))]
#endif
            get;
        }
    }
}
