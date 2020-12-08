// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// An unsafe class that provides a set of methods to access the underlying data representations of collections.
    /// </summary>
    public static class CollectionsMarshal
    {
        /// <summary>
        /// Get a <see cref="Span{T}"/> view over a <see cref="List{T}"/>'s data.
        /// Items should not be added or removed from the <see cref="List{T}"/> while the <see cref="Span{T}"/> is in use.
        /// </summary>
        public static Span<T> AsSpan<T>(List<T>? list)
            => list is null ? default : new Span<T>(list._items, 0, list._size);
    }
}
