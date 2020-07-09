// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    internal sealed class ThreadInt64PersistentCounter
    {
        // This type is used by Monitor for lock contention counting, so can't use an object for a lock. Also it's preferable
        // (though currently not required) to disallow/ignore thread interrupt for uses of this lock here. Using Lock directly
        // is a possibility but maybe less compatible with other runtimes. Lock cases are relatively rare, static instance
        // should be ok.
        private static readonly LowLevelLock s_lock = new LowLevelLock();

        private readonly ThreadLocal<ThreadLocalNode> _threadLocalNode = new ThreadLocal<ThreadLocalNode>(trackAllValues: true);
        private long _overflowCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Increment()
        {
            ThreadLocalNode? node = _threadLocalNode.Value;
            if (node != null)
            {
                node.Increment();
                return;
            }

            TryCreateNode();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void TryCreateNode()
        {
            Debug.Assert(_threadLocalNode.Value == null);

            try
            {
                _threadLocalNode.Value = new ThreadLocalNode(this);
            }
            catch (OutOfMemoryException)
            {
            }
        }

        public long Count
        {
            get
            {
                long count = 0;
                try
                {
                    s_lock.Acquire();
                    try
                    {
                        count = _overflowCount;
                        foreach (ThreadLocalNode node in _threadLocalNode.ValuesAsEnumerable)
                        {
                            if (node != null)
                            {
                                count += node.Count;
                            }
                        }
                        return count;
                    }
                    finally
                    {
                        s_lock.Release();
                    }
                }
                catch (OutOfMemoryException)
                {
                    // Some allocation occurs above and it may be a bit awkward to get an OOM from this property getter
                    return count;
                }
            }
        }

        private sealed class ThreadLocalNode
        {
            private uint _count;
            private readonly ThreadInt64PersistentCounter _counter;

            public ThreadLocalNode(ThreadInt64PersistentCounter counter)
            {
                Debug.Assert(counter != null);

                _count = 1;
                _counter = counter;
            }

            ~ThreadLocalNode()
            {
                ThreadInt64PersistentCounter counter = _counter;
                s_lock.Acquire();
                try
                {
                    counter._overflowCount += _count;
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
    }
}
