// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    internal sealed class ThreadInt64PersistentCounter
    {
        private readonly LowLevelLock _lock = new LowLevelLock();

        [ThreadStatic]
        private static List<ThreadLocalNodeFinalizationHelper>? t_nodeFinalizationHelpers;

        private long _overflowCount;

        // dummy node serving as a start and end of the ring list
        private ThreadLocalNode _nodes;

        public ThreadInt64PersistentCounter()
        {
            _nodes = new ThreadLocalNode(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Increment(object threadLocalCountObject)
        {
            Debug.Assert(threadLocalCountObject is ThreadLocalNode);
            Unsafe.As<ThreadLocalNode>(threadLocalCountObject).Increment();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(object threadLocalCountObject, uint count)
        {
            Debug.Assert(threadLocalCountObject is ThreadLocalNode);
            Unsafe.As<ThreadLocalNode>(threadLocalCountObject).Add(count);
        }

        public object CreateThreadLocalCountObject()
        {
            var node = new ThreadLocalNode(this);

            List<ThreadLocalNodeFinalizationHelper>? nodeFinalizationHelpers = t_nodeFinalizationHelpers ??= new List<ThreadLocalNodeFinalizationHelper>(1);
            nodeFinalizationHelpers.Add(new ThreadLocalNodeFinalizationHelper(node));

            _lock.Acquire();
            try
            {
                node._next = _nodes._next;
                node._prev = _nodes;
                _nodes._next._prev = node;
                _nodes._next = node;
            }
            finally
            {
                _lock.Release();
            }

            return node;
        }

        public long Count
        {
            get
            {
                _lock.Acquire();
                long count = _overflowCount;
                try
                {
                    ThreadLocalNode first = _nodes;
                    ThreadLocalNode node = first._next;
                    while (node != first)
                    {
                        count += node.Count;
                        node = node._next;
                    }
                }
                finally
                {
                    _lock.Release();
                }

                return count;
            }
        }

        private sealed class ThreadLocalNode
        {
            private uint _count;
            private readonly ThreadInt64PersistentCounter _counter;

            internal ThreadLocalNode _prev;
            internal ThreadLocalNode _next;

            public ThreadLocalNode(ThreadInt64PersistentCounter counter)
            {
                Debug.Assert(counter != null);
                _counter = counter;
                _prev = this;
                _next = this;
            }

            public void Dispose()
            {
                ThreadInt64PersistentCounter counter = _counter;
                counter._lock.Acquire();
                try
                {
                    counter._overflowCount += _count;

                    _prev._next = _next;
                    _next._prev = _prev;
                }
                finally
                {
                    counter._lock.Release();
                }
            }

            public uint Count => _count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Increment()
            {
                uint newCount = _count + 1;
                if (newCount != 0)
                {
                    _count = newCount;
                    return;
                }

                OnAddOverflow(1);
            }

            public void Add(uint count)
            {
                Debug.Assert(count != 0);

                uint newCount = _count + count;
                if (newCount >= count)
                {
                    _count = newCount;
                    return;
                }

                OnAddOverflow(count);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void OnAddOverflow(uint count)
            {
                Debug.Assert(count != 0);

                // Accumulate the count for this add into the overflow count and reset the thread-local count

                // The lock, in coordination with other places that read these values, ensures that both changes below become
                // visible together
                ThreadInt64PersistentCounter counter = _counter;
                counter._lock.Acquire();
                try
                {
                    counter._overflowCount += (long)_count + count;
                    _count = 0;
                }
                finally
                {
                    counter._lock.Release();
                }
            }
        }

        private sealed class ThreadLocalNodeFinalizationHelper
        {
            private readonly ThreadLocalNode _node;

            public ThreadLocalNodeFinalizationHelper(ThreadLocalNode node)
            {
                Debug.Assert(node != null);
                _node = node;
            }

            ~ThreadLocalNodeFinalizationHelper() => _node.Dispose();
        }
    }
}
