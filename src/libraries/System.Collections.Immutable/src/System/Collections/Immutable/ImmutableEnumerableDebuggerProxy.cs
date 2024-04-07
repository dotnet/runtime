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
    /// <remarks>
    /// This class should only be used with immutable dictionaries, since it
    /// caches the dictionary into an array for display in the debugger.
    /// </remarks>
    internal sealed class ImmutableDictionaryDebuggerProxy<TKey, TValue> where TKey : notnull
    {
        /// <summary>
        /// The dictionary to show to the debugger.
        /// </summary>
        private readonly IReadOnlyDictionary<TKey, TValue> _dictionary;

        /// <summary>
        /// The contents of the dictionary, cached into an array.
        /// </summary>
        private DebugViewDictionaryItem<TKey, TValue>[]? _cachedContents;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableDictionaryDebuggerProxy{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary to show in the debugger.</param>
        public ImmutableDictionaryDebuggerProxy(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            Requires.NotNull(dictionary, nameof(dictionary));
            _dictionary = dictionary;
        }

        /// <summary>
        /// Gets the contents of the dictionary for display in the debugger.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DebugViewDictionaryItem<TKey, TValue>[] Contents => _cachedContents
            ??= _dictionary.Select(kv => new DebugViewDictionaryItem<TKey, TValue>(kv)).ToArray(_dictionary.Count);
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
        public T[] Contents => _cachedContents ??= _enumerable.ToArray();
    }
}
