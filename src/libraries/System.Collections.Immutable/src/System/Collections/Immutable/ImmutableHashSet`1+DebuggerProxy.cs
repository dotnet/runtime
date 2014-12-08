// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Validation;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A simple view of the immutable collection that the debugger can show to the developer.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class ImmutableHashSetDebuggerProxy<T>
    {
        /// <summary>
        /// The collection to be enumerated.
        /// </summary>
        private readonly ImmutableHashSet<T> set;

        /// <summary>
        /// The simple view of the collection.
        /// </summary>
        private T[] contents;

        /// <summary>   
        /// Initializes a new instance of the <see cref="ImmutableHashSetDebuggerProxy&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="set">The collection to display in the debugger</param>
        public ImmutableHashSetDebuggerProxy(ImmutableHashSet<T> set)
        {
            Requires.NotNull(set, "set");
            this.set = set;
        }

        /// <summary>
        /// Gets a simple debugger-viewable collection.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Contents
        {
            get
            {
                if (this.contents == null)
                {
                    this.contents = this.set.ToArray(this.set.Count);
                }

                return this.contents;
            }
        }
    }
}
