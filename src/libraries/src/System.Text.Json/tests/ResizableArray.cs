// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Tests
{
    // a List<T> like type designed to be embeded in other types
    internal struct ResizableArray<T>
    {
        public ResizableArray(T[] array, int count = 0)
        {
            Array = array;
            Count = count;
        }

        public T[] Array { get; set; }

        public int Count { get; set; }

        public int Capacity => Array.Length;

        public T[] Resize(T[] newArray)
        {
            T[] oldArray = Array;
            Array.AsSpan(0, Count).CopyTo(newArray);  // CopyTo will throw if newArray.Length < _count
            Array = newArray;
            return oldArray;
        }

        public ArraySegment<T> Full => new ArraySegment<T>(Array, 0, Count);

        public ArraySegment<T> Free => new ArraySegment<T>(Array, Count, Array.Length - Count);

        public Span<T> FreeSpan => new Span<T>(Array, Count, Array.Length - Count);

        public Memory<T> FreeMemory => new Memory<T>(Array, Count, Array.Length - Count);

        public int FreeCount => Array.Length - Count;
    }
}
