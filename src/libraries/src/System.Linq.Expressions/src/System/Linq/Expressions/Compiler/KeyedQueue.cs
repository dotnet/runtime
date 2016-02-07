// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Compiler
{
    /// <summary>
    /// A simple dictionary of queues, keyed off a particular type
    /// This is useful for storing free lists of variables
    /// </summary>
    internal sealed class KeyedQueue<K, V>
    {
        private readonly Dictionary<K, Queue<V>> _data;

        internal KeyedQueue()
        {
            _data = new Dictionary<K, Queue<V>>();
        }

        internal void Enqueue(K key, V value)
        {
            Queue<V> queue;
            if (!_data.TryGetValue(key, out queue))
            {
                _data.Add(key, queue = new Queue<V>());
            }
            queue.Enqueue(value);
        }

        internal bool TryDequeue(K key, out V value)
        {
            Queue<V> queue;
            if (_data.TryGetValue(key, out queue) && queue.Count > 0)
            {
                value = queue.Dequeue();
                if (queue.Count == 0)
                {
                    _data.Remove(key);
                }
                return true;
            }
            value = default(V);
            return false;
        }
    }
}
