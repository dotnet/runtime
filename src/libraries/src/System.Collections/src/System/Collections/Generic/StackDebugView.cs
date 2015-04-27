// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Collections.Generic
{
    internal sealed class StackDebugView<T>
    {
        private readonly Stack<T> _stack;

        public StackDebugView(Stack<T> stack)
        {
            if (stack == null)
            {
                throw new ArgumentNullException("stack");
            }

            this._stack = stack;
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
