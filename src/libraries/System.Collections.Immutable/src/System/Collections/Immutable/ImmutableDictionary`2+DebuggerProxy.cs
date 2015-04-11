// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Validation;

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
            Requires.NotNull(map, "map");
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

