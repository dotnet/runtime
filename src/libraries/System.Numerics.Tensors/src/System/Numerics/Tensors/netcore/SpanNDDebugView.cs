// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Numerics.Tensors
{
    internal sealed class SpanNDDebugView<T>
    {
        private readonly T[] _array;

        public SpanNDDebugView(SpanND<T> span)
        {
            _array = span.ToArray();
        }

        public SpanNDDebugView(ReadOnlySpanND<T> span)
        {
            _array = span.ToArray();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _array;
    }
}
