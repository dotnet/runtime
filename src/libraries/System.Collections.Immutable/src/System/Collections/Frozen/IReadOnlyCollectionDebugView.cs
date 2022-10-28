// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    internal sealed class IReadOnlyCollectionDebugView<T>
    {
        private readonly IReadOnlyCollection<T> _collection;

        public IReadOnlyCollectionDebugView(IReadOnlyCollection<T> collection)
        {
            _collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => new List<T>(_collection).ToArray();
    }
}
