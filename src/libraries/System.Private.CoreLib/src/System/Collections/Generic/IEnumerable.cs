// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic
{
    // Implement this interface if you need to support foreach semantics.
    public interface IEnumerable<out T> : IEnumerable
    {
        // Returns an IEnumerator for this enumerable Object.  The enumerator provides
        // a simple way to access all the contents of a collection.
#if MONO
        [System.Diagnostics.CodeAnalysis.DynamicDependency(nameof(Array.InternalArray__IEnumerable_GetEnumerator) + "``1 ", typeof(Array))]
#endif
        new IEnumerator<T> GetEnumerator();
    }
}
