// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// Provides a debugger view for types implementing <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the enumerable.</typeparam>
    public sealed class EnumerableDebugView<T>
    {
        private readonly IEnumerable<T> _source;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableDebugView{T}"/> class.
        /// </summary>
        /// <param name="source">The enumerable to provide a debug view for.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public EnumerableDebugView(IEnumerable<T> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            _source = source;
        }

        /// <summary>
        /// Gets the items in the enumerable as an array.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                T[] array = EnumerableHelpers.ToArray(_source, out int length);
                if (length < array.Length)
                {
                    Array.Resize(ref array, length);
                }

                return array;
            }
        }
    }
}
