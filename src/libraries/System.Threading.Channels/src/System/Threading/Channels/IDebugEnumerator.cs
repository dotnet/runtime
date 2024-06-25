// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading.Channels
{
    internal interface IDebugEnumerable<T>
    {
        IEnumerator<T> GetEnumerator();
    }

    internal sealed class DebugEnumeratorDebugView<T>
    {
        public DebugEnumeratorDebugView(IDebugEnumerable<T> enumerable)
        {
            var list = new List<T>();
            foreach (T item in enumerable)
            {
                list.Add(item);
            }
            Items = list.ToArray();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items { get; }
    }
}
