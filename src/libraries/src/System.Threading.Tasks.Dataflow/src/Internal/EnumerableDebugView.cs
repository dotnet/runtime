// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Debug.Assert(enumerable != null, "Expected a non-null enumerable.");
            _enumerable = enumerable;
        }

        /// <summary>Gets the contents of the list.</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue>[] Items { get { return _enumerable.ToArray(); } }
    }
}
