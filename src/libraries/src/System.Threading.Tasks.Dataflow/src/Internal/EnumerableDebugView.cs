// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// EnumerableDebugView.cs
//
//
// Debugger type proxy for enumerables.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace System.Threading.Tasks.Dataflow.Internal
{
    /// <summary>Debugger type proxy for an enumerable of T.</summary>
    internal sealed class EnumerableDebugView<TKey, TValue>
    {
        /// <summary>The enumerable being visualized.</summary>
        private readonly IEnumerable<KeyValuePair<TKey, TValue>> _enumerable;

        /// <summary>Initializes the debug view.</summary>
        /// <param name="enumerable">The enumerable being debugged.</param>
        public EnumerableDebugView(IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        {
            Contract.Requires(enumerable != null, "Expected a non-null enumerable.");
            _enumerable = enumerable;
        }

        /// <summary>Gets the contents of the list.</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue>[] Items { get { return _enumerable.ToArray(); } }
    }

    /// <summary>Debugger type proxy for an enumerable of T.</summary>
    internal sealed class EnumerableDebugView<T>
    {
        /// <summary>The enumerable being visualized.</summary>
        private readonly IEnumerable<T> _enumerable;

        /// <summary>Initializes the debug view.</summary>
        /// <param name="enumerable">The enumerable being debugged.</param>
        public EnumerableDebugView(IEnumerable<T> enumerable)
        {
            Contract.Requires(enumerable != null, "Expected a non-null enumerable.");
            _enumerable = enumerable;
        }

        /// <summary>Gets the contents of the list.</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items { get { return _enumerable.ToArray(); } }
    }
}
