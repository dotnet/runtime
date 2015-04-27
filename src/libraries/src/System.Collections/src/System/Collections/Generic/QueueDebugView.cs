// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Collections.Generic
{
    internal sealed class QueueDebugView<T>
    {
        private readonly Queue<T> _queue;

        public QueueDebugView(Queue<T> queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            this._queue = queue;
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
