// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Collections.Generic
{
    internal sealed class QueueDebugView<T>
    {
        private readonly Queue<T> _queue;

        public QueueDebugView(Queue<T> queue)
        {
            ArgumentNullException.ThrowIfNull(queue);

            _queue = queue;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                return _queue.ToArray();
            }
        }
    }
}
