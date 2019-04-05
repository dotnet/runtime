// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System
{
    internal sealed class SpanDebugView<T>
    {
        private readonly T[] _array;

        public SpanDebugView(Span<T> span)
        {
            _array = span.ToArray();
        }

        public SpanDebugView(ReadOnlySpan<T> span)
        {
            _array = span.ToArray();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _array;
    }
}
