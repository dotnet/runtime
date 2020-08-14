// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public class InterlockedTests
    {
        [Fact]
        public void InterlockedAdd_Int32()
        {
            int value = 42;
            Assert.Equal(12387, Interlocked.Add(ref value, 12345));
            Assert.Equal(12387, Interlocked.Add(ref value, 0));
            Assert.Equal(12386, Interlocked.Add(ref value, -1));

            value = int.MaxValue;
            Assert.Equal(int.MinValue, Interlocked.Add(ref value, 1));
        }

        [Fact]
        public void InterlockedAdd_UInt32()
        {
            uint value = 42;
            Assert.Equal(12387u, Interlocked.Add(ref value, 12345u));
            Assert.Equal(12387u, Interlocked.Add(ref value, 0u));
            Assert.Equal(9386u, Interlocked.Add(ref value, 4294964295u));

            value = uint.MaxValue;
            Assert.Equal(0u, Interlocked.Add(ref value, 1));
        }

        [Fact]
        public void InterlockedAdd_Int64()
        {
            long value = 42;
            Assert.Equal(12387, Interlocked.Add(ref value, 12345));
            Assert.Equal(12387, Interlocked.Add(ref value, 0));
            Assert.Equal(12386, Interlocked.Add(ref value, -1));

            value = long.MaxValue;
            Assert.Equal(long.MinValue, Interlocked.Add(ref value, 1));
        }

        [Fact]
        public void InterlockedAdd_UInt64()
        {
            ulong value = 42;
            Assert.Equal(12387u, Interlocked.Add(ref value, 12345));
            Assert.Equal(12387u, Interlocked.Add(ref value, 0));
            Assert.Equal(10771u, Interlocked.Add(ref value, 18446744073709550000));

            value = ulong.MaxValue;
            Assert.Equal(0u, Interlocked.Add(ref value, 1));
        }

        [Fact]
        public void InterlockedIncrement_Int32()
        {
            int value = 42;
            Assert.Equal(43, Interlocked.Increment(ref value));
            Assert.Equal(43, value);
        }

        [Fact]
        public void InterlockedIncrement_UInt32()
        {
            uint value = 42u;
            Assert.Equal(43u, Interlocked.Increment(ref value));
            Assert.Equal(43u, value);
        }

        [Fact]
        public void InterlockedIncrement_Int64()
        {
            long value = 42;
            Assert.Equal(43, Interlocked.Increment(ref value));
            Assert.Equal(43, value);
        }

        [Fact]
        public void InterlockedIncrement_UInt64()
        {
            ulong value = 42u;
            Assert.Equal(43u, Interlocked.Increment(ref value));
            Assert.Equal(43u, value);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void InterlockedDecrement_Int32()
        {
            int value = 42;
            Assert.Equal(41, Interlocked.Decrement(ref value));
            Assert.Equal(41, value);

            List<Task> threads = new List<Task>();
            int count = 0;
            for (int i = 0; i < 10000; i++)
            {
                threads.Add(Task.Run(() => Interlocked.Increment(ref count)));
                threads.Add(Task.Run(() => Interlocked.Decrement(ref count)));
            }
            Task.WaitAll(threads.ToArray());
            Assert.Equal(0, count);
        }

        [Fact]
        public void InterlockedDecrement_UInt32()
        {
            uint value = 42u;
            Assert.Equal(41u, Interlocked.Decrement(ref value));
            Assert.Equal(41u, value);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void InterlockedDecrement_Int64()
        {
            long value = 42;
            Assert.Equal(41, Interlocked.Decrement(ref value));
            Assert.Equal(41, value);

            List<Task> threads = new List<Task>();
            long count = 0;
            for (int i = 0; i < 10000; i++)
            {
                threads.Add(Task.Run(() => Interlocked.Increment(ref count)));
                threads.Add(Task.Run(() => Interlocked.Decrement(ref count)));
            }
            Task.WaitAll(threads.ToArray());
            Assert.Equal(0, count);
        }

        [Fact]
        public void InterlockedDecrement_UInt64()
        {
            ulong value = 42u;
            Assert.Equal(41u, Interlocked.Decrement(ref value));
            Assert.Equal(41u, value);
        }

        [Fact]
        public void InterlockedExchange_Int32()
        {
            int value = 42;
            Assert.Equal(42, Interlocked.Exchange(ref value, 12345));
            Assert.Equal(12345, value);
        }

        [Fact]
        public void InterlockedExchange_UInt32()
        {
            uint value = 42;
            Assert.Equal(42u, Interlocked.Exchange(ref value, 12345u));
            Assert.Equal(12345u, value);
        }

        [Fact]
        public void InterlockedExchange_Int64()
        {
            long value = 42;
            Assert.Equal(42, Interlocked.Exchange(ref value, 12345));
            Assert.Equal(12345, value);
        }

        [Fact]
        public void InterlockedExchange_UInt64()
        {
            ulong value = 42;
            Assert.Equal(42u, Interlocked.Exchange(ref value, 12345u));
            Assert.Equal(12345u, value);
        }

        [Fact]
        public void InterlockedCompareExchange_Int32()
        {
            int value = 42;

            Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 41));
            Assert.Equal(42, value);

            Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 42));
            Assert.Equal(12345, value);
        }

        [Fact]
        public void InterlockedCompareExchange_UInt32()
        {
            uint value = 42;

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 41u));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 42u));
            Assert.Equal(12345u, value);
        }

        [Fact]
        public void InterlockedCompareExchange_Int64()
        {
            long value = 42;

            Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 41));
            Assert.Equal(42, value);

            Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 42));
            Assert.Equal(12345, value);
        }

        [Fact]
        public void InterlockedCompareExchange_UInt64()
        {
            ulong value = 42;

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 41u));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 42u));
            Assert.Equal(12345u, value);
        }

        [Fact]
        public void InterlockedRead_Int64()
        {
            long value = long.MaxValue - 42;
            Assert.Equal(long.MaxValue - 42, Interlocked.Read(ref value));
        }

        [Fact]
        public void InterlockedRead_UInt64()
        {
            ulong value = ulong.MaxValue - 42;
            Assert.Equal(ulong.MaxValue - 42, Interlocked.Read(ref value));
        }

        [Fact]
        public void InterlockedAnd_Int32()
        {
            int value = 0x12345670;
            Assert.Equal(0x12345670, Interlocked.And(ref value, 0x7654321));
            Assert.Equal(0x02244220, value);
        }

        [Fact]
        public void InterlockedAnd_UInt32()
        {
            uint value = 0x12345670u;
            Assert.Equal(0x12345670u, Interlocked.And(ref value, 0x7654321));
            Assert.Equal(0x02244220u, value);
        }

        [Fact]
        public void InterlockedAnd_Int64()
        {
            long value = 0x12345670;
            Assert.Equal(0x12345670, Interlocked.And(ref value, 0x7654321));
            Assert.Equal(0x02244220, value);
        }

        [Fact]
        public void InterlockedAnd_UInt64()
        {
            ulong value = 0x12345670u;
            Assert.Equal(0x12345670u, Interlocked.And(ref value, 0x7654321));
            Assert.Equal(0x02244220u, value);
        }

        [Fact]
        public void InterlockedOr_Int32()
        {
            int value = 0x12345670;
            Assert.Equal(0x12345670, Interlocked.Or(ref value, 0x7654321));
            Assert.Equal(0x17755771, value);
        }

        [Fact]
        public void InterlockedOr_UInt32()
        {
            uint value = 0x12345670u;
            Assert.Equal(0x12345670u, Interlocked.Or(ref value, 0x7654321));
            Assert.Equal(0x17755771u, value);
        }

        [Fact]
        public void InterlockedOr_Int64()
        {
            long value = 0x12345670;
            Assert.Equal(0x12345670, Interlocked.Or(ref value, 0x7654321));
            Assert.Equal(0x17755771, value);
        }

        [Fact]
        public void InterlockedOr_UInt64()
        {
            ulong value = 0x12345670u;
            Assert.Equal(0x12345670u, Interlocked.Or(ref value, 0x7654321));
            Assert.Equal(0x17755771u, value);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void MemoryBarrierProcessWide()
        {
            // Stress MemoryBarrierProcessWide correctness using a simple AsymmetricLock

            AsymmetricLock asymmetricLock = new AsymmetricLock();
            List<Task> threads = new List<Task>();
            int count = 0;
            for (int i = 0; i < 1000; i++)
            {
                threads.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        var cookie = asymmetricLock.Enter();
                        count++;
                        cookie.Exit();
                    }
                }));
            }
            Task.WaitAll(threads.ToArray());
            Assert.Equal(1000*1000, count);
        }

        // Taking this lock on the same thread repeatedly is very fast because it has no interlocked operations.
        // Switching the thread where the lock is taken is expensive because of allocation and FlushProcessWriteBuffers.
        private class AsymmetricLock
        {
            public class LockCookie
            {
                internal LockCookie(int threadId)
                {
                    ThreadId = threadId;
                    Taken = false;
                }

                public void Exit()
                {
                    Debug.Assert(ThreadId == Environment.CurrentManagedThreadId);
                    Taken = false;
                }

                internal readonly int ThreadId;
                internal bool Taken;
            }

            private LockCookie _current = new LockCookie(-1);

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static T VolatileReadWithoutBarrier<T>(ref T location)
            {
                return location;
            }

            // Returning LockCookie to call Exit on is the fastest implementation because of it works naturally with the RCU pattern.
            // The traditional Enter/Exit lock interface would require thread local storage or some other scheme to reclaim the cookie.
            // Returning LockCookie to call Exit on is the fastest implementation because of it works naturally with the RCU pattern.
            // The traditional Enter/Exit lock interface would require thread local storage or some other scheme to reclaim the cookie
            public LockCookie Enter()
            {
                int currentThreadId = Environment.CurrentManagedThreadId;

                LockCookie entry = _current;

                if (entry.ThreadId == currentThreadId)
                {
                    entry.Taken = true;

                    //
                    // If other thread started stealing the ownership, we need to take slow path.
                    //
                    // Make sure that the compiler won't reorder the read with the above write by wrapping the read in no-inline method.
                    // RyuJIT won't reorder them today, but more advanced optimizers might. Regular Volatile.Read would be too big of
                    // a hammer because of it will result into memory barrier on ARM that we do not need here.
                    //
                    //
                    if (VolatileReadWithoutBarrier(ref _current) == entry)
                    {
                        return entry;
                    }

                    entry.Taken = false;
                }

                return EnterSlow();
            }

            private LockCookie EnterSlow()
            {
                // Attempt to steal the ownership. Take a regular lock to ensure that only one thread is trying to steal it at a time.
                lock (this)
                {
                    // We are the new fast thread now!
                    var oldEntry = _current;
                    _current = new LockCookie(Environment.CurrentManagedThreadId);

                    // After MemoryBarrierProcessWide, we can be sure that the Volatile.Read done by the fast thread will see that it is not a fast
                    // thread anymore, and thus it will not attempt to enter the lock.
                    Interlocked.MemoryBarrierProcessWide();

                    // Keep looping as long as the lock is taken by other thread
                    SpinWait sw = new SpinWait();
                    while (oldEntry.Taken)
                        sw.SpinOnce();

                    // We have seen that the other thread released the lock by setting Taken to false.
                    // However, on platforms with weak memory ordering (ex: ARM32, ARM64) observing that does not guarantee that the writes executed by that
                    // thread prior to releasing the lock are all committed to the shared memory.
                    // We could fix that by doing the release via Volatile.Write, but we do not want to add expense to every release on the fast path.
                    // Instead we will do another MemoryBarrierProcessWide here.

                    // NOTE: not needed on x86/x64
                    Interlocked.MemoryBarrierProcessWide();

                    _current.Taken = true;
                    return _current;
                }
            }
        }
    }
}
