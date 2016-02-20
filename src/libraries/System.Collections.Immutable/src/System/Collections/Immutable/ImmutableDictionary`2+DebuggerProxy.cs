// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A simple view of the immutable collection that the debugger can show to the developer.
    /// </summary>
    internal class ImmutableDictionaryDebuggerProxy<TKey, TValue>
    {
        /// <summary>
        /// The collection to be enumerated.
        /// </summary>
        private readonly ImmutableDictionary<TKey, TValue> _map;

        /// <summary>
        /// The simple view of the collection.
        /// </summary>
        private KeyValuePair<TKey, TValue>[] _contents;

        /// <summary>   
        /// Initializes a new instance of the <see cref="ImmutableDictionaryDebuggerProxy{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="map">The collection to display in the debugger</param>
        public ImmutableDictionaryDebuggerProxy(ImmutableDictionary<TKey, TValue> map)
        {
            Requires.NotNull(map, nameof(map));
            _map = map;
        }

        /// <summary>
        /// Gets a simple debugger-viewable collection.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue>[] Contents
        {
            get
            {
                if (_contents == null)
                {
                    _contents = _map.ToArray(_map.Count);
                }

                return _contents;
            }
        }
    }
}

