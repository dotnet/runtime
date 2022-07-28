// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Collections.Generic
{
    // LargeArrayBuilder.netcoreapp.cs provides a "LargeArrayBuilder" that's meant to help
    // avoid allocations and copying while building up an array.  But in doing so, it utilizes
    // T[][] to store T[]s, which results in significant size increases for AOT builds. To
    // address that, this minimal wrapper for ArrayBuilder<T> may be used instead; it's a simple
    // passthrough to ArrayBuilder<T>, and thus doesn't incur the size increase due to the T[][]s.

    internal struct LargeArrayBuilder<T>
    {
        private ArrayBuilder<T> _builder; // mutable struct; do not make this readonly

        /// <summary>
        /// Constructs a new builder.
        /// </summary>
        public LargeArrayBuilder() => this = default;

        public int Count => _builder.Count;

        public void Add(T item) => _builder.Add(item);

        public void AddRange(IEnumerable<T> items)
        {
            Debug.Assert(items != null);
            foreach (T item in items)
            {
                _builder.Add(item);
            }
        }

        public T[] ToArray() => _builder.ToArray();

        public CopyPosition CopyTo(CopyPosition position, T[] array, int arrayIndex, int count)
        {
            Array.Copy(_builder.Buffer!, position.Column, array, arrayIndex, count);
            return new CopyPosition(0, position.Column + count);
        }
    }
}
