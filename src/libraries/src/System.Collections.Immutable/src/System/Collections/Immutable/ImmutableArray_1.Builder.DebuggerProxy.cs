// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A simple view of the immutable collection that the debugger can show to the developer.
    /// </summary>
    internal sealed class ImmutableArrayBuilderDebuggerProxy<T>
    {
        /// <summary>
        /// The collection to be enumerated.
        /// </summary>
        private readonly ImmutableArray<T>.Builder _builder;
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArrayBuilderDebuggerProxy{T}"/> class.
        /// </summary>
        /// <param name="builder">The collection to display in the debugger</param>
        public ImmutableArrayBuilderDebuggerProxy(ImmutableArray<T>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));
            _builder = builder;
        }

        /// <summary>
        /// Gets a simple debugger-viewable collection.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] A
        {
            get
            {
                return _builder.ToArray();
            }
        }
    }
}
