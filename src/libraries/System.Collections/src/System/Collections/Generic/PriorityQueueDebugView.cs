// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Collections.Generic
{
    internal sealed class PriorityQueueDebugView<TElement, TPriority>
    {
        private readonly PriorityQueue<TElement, TPriority> _queue;
        private readonly bool _sort;

        public PriorityQueueDebugView(PriorityQueue<TElement, TPriority> queue)
        {
            ArgumentNullException.ThrowIfNull(queue);

            _queue = queue;
            _sort = true;
        }

        public PriorityQueueDebugView(PriorityQueue<TElement, TPriority>.UnorderedItemsCollection collection)
        {
            _queue = collection?._queue ?? throw new ArgumentNullException(nameof(collection));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public (TElement Element, TPriority Priority)[] Items
        {
            get
            {
                var list = new List<(TElement Element, TPriority Priority)>(_queue.UnorderedItems);
                if (_sort)
                {
                    list.Sort((i1, i2) => _queue.Comparer.Compare(i1.Priority, i2.Priority));
                }
                return list.ToArray();
            }
        }
    }
}
