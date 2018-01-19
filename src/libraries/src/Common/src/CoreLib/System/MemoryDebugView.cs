// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

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
                if (MemoryMarshal.TryGetArray(_memory, out ArraySegment<T> segment))
                {
                    T[] array = new T[_memory.Length];
                    Array.Copy(segment.Array, segment.Offset, array, 0, array.Length);
                    return array;
                }

                if (typeof(T) == typeof(char) &&
                    ((ReadOnlyMemory<char>)(object)_memory).TryGetString(out string text, out int start, out int length))
                {
                    return (T[])(object)text.Substring(start, length).ToCharArray();
                }

#if FEATURE_PORTABLE_SPAN
                return SpanHelpers.PerTypeValues<T>.EmptyArray;
#else
                return Array.Empty<T>();
#endif // FEATURE_PORTABLE_SPAN
            }
        }
    }
}
