// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A simple view of the immutable collection that the debugger can show to the developer.
    /// </summary>
    internal class ImmutableHashSetDebuggerProxy<T>
    {
        /// <summary>
        /// The collection to be enumerated.
        /// </summary>
        private readonly ImmutableHashSet<T> _set;

        /// <summary>
        /// The simple view of the collection.
        /// </summary>
        private T[] _contents;

        /// <summary>   
        /// Initializes a new instance of the <see cref="ImmutableHashSetDebuggerProxy{T}"/> class.
        /// </summary>
        /// <param name="set">The collection to display in the debugger</param>
        public ImmutableHashSetDebuggerProxy(ImmutableHashSet<T> set)
        {
            Requires.NotNull(set, nameof(set));
            _set = set;
        }

        /// <summary>
        /// Gets a simple debugger-viewable collection.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Contents
        {
            get
            {
                if (_contents == null)
                {
                    _contents = _set.ToArray(_set.Count);
                }

                return _contents;
            }
        }
    }
}
