// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System
{
    internal sealed class MemoryDebugView<T>
    {
        private readonly ReadOnlyMemory<T> _memory;

        public MemoryDebugView(Memory<T> memory)
        {
            _memory = memory;
        }

        public MemoryDebugView(ReadOnlyMemory<T> memory)
        {
            _memory = memory;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            // This is a work around since we cannot use _memory.ToArray() due to
            // https://devdiv.visualstudio.com/DevDiv/_workitems?id=286592
            get
            {
                if (_memory.DangerousTryGetArray(out ArraySegment<T> segment))
                {
                    T[] array = new T[_memory.Length];
                    Array.Copy(segment.Array, segment.Offset, array, 0, array.Length);
                    return array;
                }
                return SpanHelpers.PerTypeValues<T>.EmptyArray;
            }
        }
    }
}
