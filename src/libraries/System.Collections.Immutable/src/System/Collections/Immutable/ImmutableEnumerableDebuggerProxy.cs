// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Collections.Immutable
{
    /// <summary>
    /// Displays immutable dictionaries in the debugger.
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary's keys.</typeparam>
    /// <typeparam name="TValue">The type of the dictionary's values.</typeparam>
    internal sealed class ImmutableDictionaryDebuggerProxy<TKey, TValue> : ImmutableEnumerableDebuggerProxy<KeyValuePair<TKey, TValue>> where TKey : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableDictionaryDebuggerProxy{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="dictionary">The enumerable to show in the debugger.</param>
        public ImmutableDictionaryDebuggerProxy(IImmutableDictionary<TKey, TValue> dictionary)
            : base(enumerable: dictionary)
        {
        }
    }

    /// <summary>
    /// Displays immutable enumerables in the debugger.
    /// </summary>
    /// <typeparam name="T">The element type of the enumerable.</typeparam>
    /// <remarks>
    /// This class should only be used with immutable enumerables, since it
    /// caches the enumerable into an array for display in the debugger.
    /// </remarks>
    internal class ImmutableEnumerableDebuggerProxy<T>
    {
        /// <summary>
        /// The enumerable to show to the debugger.
        /// </summary>
        private readonly IEnumerable<T> _enumerable;

        /// <summary>
        /// The contents of the enumerable, cached into an array.
        /// </summary>
        private T[]? _cachedContents;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableEnumerableDebuggerProxy{T}"/> class.
        /// </summary>
        /// <param name="enumerable">The enumerable to show in the debugger.</param>
        public ImmutableEnumerableDebuggerProxy(IEnumerable<T> enumerable)
        {
            Requires.NotNull(enumerable, nameof(enumerable));
            _enumerable = enumerable;
        }

        /// <summary>
        /// Gets the contents of the enumerable for display in the debugger.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Contents => _cachedContents ?? (_cachedContents = _enumerable.ToArray());
    }
}
