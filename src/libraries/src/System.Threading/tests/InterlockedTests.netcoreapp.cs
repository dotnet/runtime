// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Threading.Tests
{
    public partial class InterlockedTests
    {
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

                    _current.Taken = true;
                    return _current;
                }
            }
        }

        [Fact]
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
    }
}
