// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Collections.Generic
{
    internal sealed class StackDebugView<T>
    {
        private readonly Stack<T> _stack;

        public StackDebugView(Stack<T> stack)
        {
            ArgumentNullException.ThrowIfNull(stack);

            _stack = stack;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                return _stack.ToArray();
            }
        }
    }
}
