// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Threading
{
    internal sealed class ThreadInt64PersistentCounter
    {
        private static readonly LowLevelLock s_lock = new LowLevelLock();

        [ThreadStatic]
        private static List<ThreadLocalNodeFinalizationHelper>? t_nodeFinalizationHelpers;

        private long _overflowCount;
        private HashSet<ThreadLocalNode> _nodes = new HashSet<ThreadLocalNode>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Increment(object threadLocalCountObject)
        {
            Debug.Assert(threadLocalCountObject is ThreadLocalNode);
            Unsafe.As<ThreadLocalNode>(threadLocalCountObject).Increment();
        }

        public object CreateThreadLocalCountObject()
        {
            var node = new ThreadLocalNode(this);

            List<ThreadLocalNodeFinalizationHelper>? nodeFinalizationHelpers = t_nodeFinalizationHelpers ??= new List<ThreadLocalNodeFinalizationHelper>(1);
            nodeFinalizationHelpers.Add(new ThreadLocalNodeFinalizationHelper(node));

            s_lock.Acquire();
            try
            {
                _nodes.Add(node);
            }
            finally
            {
                s_lock.Release();
            }

            return node;
        }

        public long Count
        {
            get
            {
                s_lock.Acquire();
                long count = _overflowCount;
                try
                {
                    foreach (ThreadLocalNode node in _nodes)
                    {
                        count += node.Count;
                    }
                }
                catch (OutOfMemoryException)
                {
                    // Some allocation occurs above and it may be a bit awkward to get an OOM from this property getter
                }
                finally
                {
                    s_lock.Release();
                }

                return count;
            }
        }

        private sealed class ThreadLocalNode
        {
            private uint _count;
            private readonly ThreadInt64PersistentCounter _counter;

            public ThreadLocalNode(ThreadInt64PersistentCounter counter)
            {
                Debug.Assert(counter != null);
                _counter = counter;
            }

            public void Dispose()
            {
                ThreadInt64PersistentCounter counter = _counter;
                s_lock.Acquire();
                try
                {
                    counter._overflowCount += _count;
                    counter._nodes.Remove(this);
                }
                finally
                {
                    s_lock.Release();
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

                OnIncrementOverflow();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void OnIncrementOverflow()
            {
                // Accumulate the count for this increment into the overflow count and reset the thread-local count

                // The lock, in coordination with other places that read these values, ensures that both changes below become
                // visible together
                ThreadInt64PersistentCounter counter = _counter;
                s_lock.Acquire();
                try
                {
                    _count = 0;
                    counter._overflowCount += (long)uint.MaxValue + 1;
                }
                finally
                {
                    s_lock.Release();
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
