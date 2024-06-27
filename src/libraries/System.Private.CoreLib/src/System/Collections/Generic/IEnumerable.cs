// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if MONO
using System.Diagnostics.CodeAnalysis;
#endif

namespace System.Collections.Generic
{
    // Implement this interface if you need to support foreach semantics.
    public interface IEnumerable<out T> : IEnumerable
        where T : allows ref struct
    {
        // Returns an IEnumerator for this enumerable Object.  The enumerator provides
        // a simple way to access all the contents of a collection.
#if MONO
        [DynamicDependency(nameof(Array.InternalArray__IEnumerable_GetEnumerator) + "``1 ", typeof(Array))]
#endif
        new IEnumerator<T> GetEnumerator();
    }
}
