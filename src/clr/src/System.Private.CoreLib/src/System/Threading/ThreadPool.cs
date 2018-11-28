// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Class for creating and managing a threadpool
**
**
=============================================================================*/

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Internal.Runtime.CompilerServices;
using Microsoft.Win32;

namespace System.Threading
{
    internal static class ThreadPoolGlobals
    {
        //Per-appDomain quantum (in ms) for which the thread keeps processing
        //requests in the current domain.
        public const uint TP_QUANTUM = 30U;

        public static readonly int processorCount = Environment.ProcessorCount;

        public static volatile bool vmTpInitialized;
        public static bool enableWorkerTracking;

        public static readonly ThreadPoolWorkQueue workQueue = new ThreadPoolWorkQueue();

        /// <summary>Shim used to invoke <see cref="IAsyncStateMachineBox.MoveNext"/> of the supplied <see cref="IAsyncStateMachineBox"/>.</summary>
        internal static readonly Action<object> s_invokeAsyncStateMachineBox = state =>
        {
            if (!(state is IAsyncStateMachineBox box))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.state);
                return;
            }

            box.MoveNext();
        };
    }

    [StructLayout(LayoutKind.Sequential)] // enforce layout so that padding reduces false sharing
    internal sealed class ThreadPoolWorkQueue
    {
        internal static class WorkStealingQueueList
        {
            private static volatile WorkStealingQueue[] _queues = new WorkStealingQueue[0];

            public static WorkStealingQueue[] Queues => _queues;

            public static void Add(WorkStealingQueue queue)
            {
                Debug.Assert(queue != null);
                while (true)
                {
                    WorkStealingQueue[] oldQueues = _queues;
                    Debug.Assert(Array.IndexOf(oldQueues, queue) == -1);

                    var newQueues = new WorkStealingQueue[oldQueues.Length + 1];
                    Array.Copy(oldQueues, 0, newQueues, 0, oldQueues.Length);
                    newQueues[newQueues.Length - 1] = queue;
                    if (Interlocked.CompareExchange(ref _queues, newQueues, oldQueues) == oldQueues)
                    {
                        break;
                    }
                }
            }

            public static void Remove(WorkStealingQueue queue)
            {
                Debug.Assert(queue != null);
                while (true)
                {
                    WorkStealingQueue[] oldQueues = _queues;
                    if (oldQueues.Length == 0)
                    {
                        return;
                    }

                    int pos = Array.IndexOf(oldQueues, queue);
                    if (pos == -1)
                    {
                        Debug.Fail("Should have found the queue");
                        return;
                    }

                    var newQueues = new WorkStealingQueue[oldQueues.Length - 1];
                    if (pos == 0)
                    {
                        Array.Copy(oldQueues, 1, newQueues, 0, newQueues.Length);
                    }
                    else if (pos == oldQueues.Length - 1)
                    {
                        Array.Copy(oldQueues, 0, newQueues, 0, newQueues.Length);
                    }
                    else
                    {
                        Array.Copy(oldQueues, 0, newQueues, 0, pos);
                        Array.Copy(oldQueues, pos + 1, newQueues, pos, newQueues.Length - pos);
                    }

                    if (Interlocked.CompareExchange(ref _queues, newQueues, oldQueues) == oldQueues)
                    {
                        break;
                    }
                }
            }
        }

        internal sealed class WorkStealingQueue
        {
            private const int INITIAL_SIZE = 32;
            internal volatile object[] m_array = new object[INITIAL_SIZE]; // SOS's ThreadPool command depends on this name
            private volatile int m_mask = INITIAL_SIZE - 1;

#if DEBUG
            // in debug builds, start at the end so we exercise the index reset logic.
            private const int START_INDEX = int.MaxValue;
#else
            private const int START_INDEX = 0;
#endif

            private volatile int m_headIndex = START_INDEX;
            private volatile int m_tailIndex = START_INDEX;

            private SpinLock m_foreignLock = new SpinLock(enableThreadOwnerTracking: false);

            public void LocalPush(object obj)
            {
                int tail = m_tailIndex;

                // We're going to increment the tail; if we'll overflow, then we need to reset our counts
                if (tail == int.MaxValue)
                {
                    bool lockTaken = false;
                    try
                    {
                        m_foreignLock.Enter(ref lockTaken);

                        if (m_tailIndex == int.MaxValue)
                        {
                            //
                            // Rather than resetting to zero, we'll just mask off the bits we don't care about.
                            // This way we don't need to rearrange the items already in the queue; they'll be found
                            // correctly exactly where they are.  One subtlety here is that we need to make sure that
                            // if head is currently < tail, it remains that way.  This happens to just fall out from
                            // the bit-masking, because we only do this if tail == int.MaxValue, meaning that all
                            // bits are set, so all of the bits we're keeping will also be set.  Thus it's impossible
                            // for the head to end up > than the tail, since you can't set any more bits than all of 
                            // them.
                            //
                            m_headIndex = m_headIndex & m_mask;
                            m_tailIndex = tail = m_tailIndex & m_mask;
                            Debug.Assert(m_headIndex <= m_tailIndex);
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                            m_foreignLock.Exit(useMemoryBarrier: true);
                    }
                }

                // When there are at least 2 elements' worth of space, we can take the fast path.
                if (tail < m_headIndex + m_mask)
                {
                    Volatile.Write(ref m_array[tail & m_mask], obj);
                    m_tailIndex = tail + 1;
                }
                else
                {
                    // We need to contend with foreign pops, so we lock.
                    bool lockTaken = false;
                    try
                    {
                        m_foreignLock.Enter(ref lockTaken);

                        int head = m_headIndex;
                        int count = m_tailIndex - m_headIndex;

                        // If there is still space (one left), just add the element.
                        if (count >= m_mask)
                        {
                            // We're full; expand the queue by doubling its size.
                            var newArray = new object[m_array.Length << 1];
                            for (int i = 0; i < m_array.Length; i++)
                                newArray[i] = m_array[(i + head) & m_mask];

                            // Reset the field values, incl. the mask.
                            m_array = newArray;
                            m_headIndex = 0;
                            m_tailIndex = tail = count;
                            m_mask = (m_mask << 1) | 1;
                        }

                        Volatile.Write(ref m_array[tail & m_mask], obj);
                        m_tailIndex = tail + 1;
                    }
                    finally
                    {
                        if (lockTaken)
                            m_foreignLock.Exit(useMemoryBarrier: false);
                    }
                }
            }

            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
            public bool LocalFindAndPop(object obj)
            {
                // Fast path: check the tail. If equal, we can skip the lock.
                if (m_array[(m_tailIndex - 1) & m_mask] == obj)
                {
                    object unused = LocalPop();
                    Debug.Assert(unused == null || unused == obj);
                    return unused != null;
                }

                // Else, do an O(N) search for the work item. The theory of work stealing and our
                // inlining logic is that most waits will happen on recently queued work.  And
                // since recently queued work will be close to the tail end (which is where we
                // begin our search), we will likely find it quickly.  In the worst case, we
                // will traverse the whole local queue; this is typically not going to be a
                // problem (although degenerate cases are clearly an issue) because local work
                // queues tend to be somewhat shallow in length, and because if we fail to find
                // the work item, we are about to block anyway (which is very expensive).
                for (int i = m_tailIndex - 2; i >= m_headIndex; i--)
                {
                    if (m_array[i & m_mask] == obj)
                    {
                        // If we found the element, block out steals to avoid interference.
                        bool lockTaken = false;
                        try
                        {
                            m_foreignLock.Enter(ref lockTaken);

                            // If we encountered a race condition, bail.
                            if (m_array[i & m_mask] == null)
                                return false;

                            // Otherwise, null out the element.
                            Volatile.Write(ref m_array[i & m_mask], null);

                            // And then check to see if we can fix up the indexes (if we're at
                            // the edge).  If we can't, we just leave nulls in the array and they'll
                            // get filtered out eventually (but may lead to superfluous resizing).
                            if (i == m_tailIndex)
                                m_tailIndex -= 1;
                            else if (i == m_headIndex)
                                m_headIndex += 1;

                            return true;
                        }
                        finally
                        {
                            if (lockTaken)
                                m_foreignLock.Exit(useMemoryBarrier: false);
                        }
                    }
                }

                return false;
            }

            public object LocalPop() => m_headIndex < m_tailIndex ? LocalPopCore() : null;

            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
            private object LocalPopCore()
            {
                while (true)
                {
                    int tail = m_tailIndex;
                    if (m_headIndex >= tail)
                    {
                        return null;
                    }

                    // Decrement the tail using a fence to ensure subsequent read doesn't come before.
                    tail -= 1;
                    Interlocked.Exchange(ref m_tailIndex, tail);

                    // If there is no interaction with a take, we can head down the fast path.
                    if (m_headIndex <= tail)
                    {
                        int idx = tail & m_mask;
                        object obj = Volatile.Read(ref m_array[idx]);

                        // Check for nulls in the array.
                        if (obj == null) continue;

                        m_array[idx] = null;
                        return obj;
                    }
                    else
                    {
                        // Interaction with takes: 0 or 1 elements left.
                        bool lockTaken = false;
                        try
                        {
                            m_foreignLock.Enter(ref lockTaken);

                            if (m_headIndex <= tail)
                            {
                                // Element still available. Take it.
                                int idx = tail & m_mask;
                                object obj = Volatile.Read(ref m_array[idx]);

                                // Check for nulls in the array.
                                if (obj == null) continue;

                                m_array[idx] = null;
                                return obj;
                            }
                            else
                            {
                                // If we encountered a race condition and element was stolen, restore the tail.
                                m_tailIndex = tail + 1;
                                return null;
                            }
                        }
                        finally
                        {
                            if (lockTaken)
                                m_foreignLock.Exit(useMemoryBarrier: false);
                        }
                    }
                }
            }

            public bool CanSteal => m_headIndex < m_tailIndex;

            public object TrySteal(ref bool missedSteal)
            {
                while (true)
                {
                    if (CanSteal)
                    {
                        bool taken = false;
                        try
                        {
                            m_foreignLock.TryEnter(ref taken);
                            if (taken)
                            {
                                // Increment head, and ensure read of tail doesn't move before it (fence).
                                int head = m_headIndex;
                                Interlocked.Exchange(ref m_headIndex, head + 1);

                                if (head < m_tailIndex)
                                {
                                    int idx = head & m_mask;
                                    object obj = Volatile.Read(ref m_array[idx]);

                                    // Check for nulls in the array.
                                    if (obj == null) continue;

                                    m_array[idx] = null;
                                    return obj;
                                }
                                else
                                {
                                    // Failed, restore head.
                                    m_headIndex = head;
                                }
                            }
                        }
                        finally
                        {
                            if (taken)
                                m_foreignLock.Exit(useMemoryBarrier: false);
                        }

                        missedSteal = true;
                    }

                    return null;
                }
            }
        }

        internal bool loggingEnabled;
        internal readonly ConcurrentQueue<object> workItems = new ConcurrentQueue<object>(); // SOS's ThreadPool command depends on this name

        private Internal.PaddingFor32 pad1;

        private volatile int numOutstandingThreadRequests = 0;

        private Internal.PaddingFor32 pad2;

        public ThreadPoolWorkQueue()
        {
            loggingEnabled = FrameworkEventSource.Log.IsEnabled(EventLevel.Verbose, FrameworkEventSource.Keywords.ThreadPool | FrameworkEventSource.Keywords.ThreadTransfer);
        }

        public ThreadPoolWorkQueueThreadLocals EnsureCurrentThreadHasQueue() =>
            ThreadPoolWorkQueueThreadLocals.threadLocals ??
            (ThreadPoolWorkQueueThreadLocals.threadLocals = new ThreadPoolWorkQueueThreadLocals(this));

        internal void EnsureThreadRequested()
        {
            //
            // If we have not yet requested #procs threads from the VM, then request a new thread
            // as needed
            //
            // Note that there is a separate count in the VM which will also be incremented in this case, 
            // which is handled by RequestWorkerThread.
            //
            int count = numOutstandingThreadRequests;
            while (count < ThreadPoolGlobals.processorCount)
            {
                int prev = Interlocked.CompareExchange(ref numOutstandingThreadRequests, count + 1, count);
                if (prev == count)
                {
                    ThreadPool.RequestWorkerThread();
                    break;
                }
                count = prev;
            }
        }

        internal void MarkThreadRequestSatisfied()
        {
            //
            // The VM has called us, so one of our outstanding thread requests has been satisfied.
            // Decrement the count so that future calls to EnsureThreadRequested will succeed.
            // Note that there is a separate count in the VM which has already been decremented by the VM
            // by the time we reach this point.
            //
            int count = numOutstandingThreadRequests;
            while (count > 0)
            {
                int prev = Interlocked.CompareExchange(ref numOutstandingThreadRequests, count - 1, count);
                if (prev == count)
                {
                    break;
                }
                count = prev;
            }
        }

        public void Enqueue(object callback, bool forceGlobal)
        {
            Debug.Assert((callback is IThreadPoolWorkItem) ^ (callback is Task));

            if (loggingEnabled)
                System.Diagnostics.Tracing.FrameworkEventSource.Log.ThreadPoolEnqueueWorkObject(callback);

            ThreadPoolWorkQueueThreadLocals tl = null;
            if (!forceGlobal)
                tl = ThreadPoolWorkQueueThreadLocals.threadLocals;

            if (null != tl)
            {
                tl.workStealingQueue.LocalPush(callback);
            }
            else
            {
                workItems.Enqueue(callback);
            }

            EnsureThreadRequested();
        }

        internal bool LocalFindAndPop(object callback)
        {
            ThreadPoolWorkQueueThreadLocals tl = ThreadPoolWorkQueueThreadLocals.threadLocals;
            return tl != null && tl.workStealingQueue.LocalFindAndPop(callback);
        }

        public object Dequeue(ThreadPoolWorkQueueThreadLocals tl, ref bool missedSteal)
        {
            WorkStealingQueue localWsq = tl.workStealingQueue;
            object callback;

            if ((callback = localWsq.LocalPop()) == null && // first try the local queue
                !workItems.TryDequeue(out callback)) // then try the global queue
            {
                // finally try to steal from another thread's local queue
                WorkStealingQueue[] queues = WorkStealingQueueList.Queues;
                int c = queues.Length;
                Debug.Assert(c > 0, "There must at least be a queue for this thread.");
                int maxIndex = c - 1;
                int i = tl.random.Next(c);
                while (c > 0)
                {
                    i = (i < maxIndex) ? i + 1 : 0;
                    WorkStealingQueue otherQueue = queues[i];
                    if (otherQueue != localWsq && otherQueue.CanSteal)
                    {
                        callback = otherQueue.TrySteal(ref missedSteal);
                        if (callback != null)
                        {
                            break;
                        }
                    }
                    c--;
                }
            }

            return callback;
        }

        internal static bool Dispatch()
        {
            ThreadPoolWorkQueue outerWorkQueue = ThreadPoolGlobals.workQueue;
            //
            // The clock is ticking!  We have ThreadPoolGlobals.TP_QUANTUM milliseconds to get some work done, and then
            // we need to return to the VM.
            //
            int quantumStartTime = Environment.TickCount;

            //
            // Update our records to indicate that an outstanding request for a thread has now been fulfilled.
            // From this point on, we are responsible for requesting another thread if we stop working for any
            // reason, and we believe there might still be work in the queue.
            //
            // Note that if this thread is aborted before we get a chance to request another one, the VM will
            // record a thread request on our behalf.  So we don't need to worry about getting aborted right here.
            //
            outerWorkQueue.MarkThreadRequestSatisfied();

            // Has the desire for logging changed since the last time we entered?
            outerWorkQueue.loggingEnabled = FrameworkEventSource.Log.IsEnabled(EventLevel.Verbose, FrameworkEventSource.Keywords.ThreadPool | FrameworkEventSource.Keywords.ThreadTransfer);

            //
            // Assume that we're going to need another thread if this one returns to the VM.  We'll set this to 
            // false later, but only if we're absolutely certain that the queue is empty.
            //
            bool needAnotherThread = true;
            object outerWorkItem = null;
            try
            {
                //
                // Set up our thread-local data
                //
                // Use operate on workQueue local to try block so it can be enregistered 
                ThreadPoolWorkQueue workQueue = outerWorkQueue;
                ThreadPoolWorkQueueThreadLocals tl = workQueue.EnsureCurrentThreadHasQueue();
                Thread currentThread = tl.currentThread;

                // Start on clean ExecutionContext and SynchronizationContext
                currentThread.ExecutionContext = null;
                currentThread.SynchronizationContext = null;

                //
                // Loop until our quantum expires.
                //
                while ((Environment.TickCount - quantumStartTime) < ThreadPoolGlobals.TP_QUANTUM)
                {
                    bool missedSteal = false;
                    // Use operate on workItem local to try block so it can be enregistered 
                    object workItem = outerWorkItem = workQueue.Dequeue(tl, ref missedSteal);

                    if (workItem == null)
                    {
                        //
                        // No work.  We're going to return to the VM once we leave this protected region.
                        // If we missed a steal, though, there may be more work in the queue.
                        // Instead of looping around and trying again, we'll just request another thread.  This way
                        // we won't starve other AppDomains while we spin trying to get locks, and hopefully the thread
                        // that owns the contended work-stealing queue will pick up its own workitems in the meantime, 
                        // which will be more efficient than this thread doing it anyway.
                        //
                        needAnotherThread = missedSteal;

                        // Tell the VM we're returning normally, not because Hill Climbing asked us to return.
                        return true;
                    }

                    if (workQueue.loggingEnabled)
                        System.Diagnostics.Tracing.FrameworkEventSource.Log.ThreadPoolDequeueWorkObject(workItem);

                    //
                    // If we found work, there may be more work.  Ask for another thread so that the other work can be processed
                    // in parallel.  Note that this will only ask for a max of #procs threads, so it's safe to call it for every dequeue.
                    //
                    workQueue.EnsureThreadRequested();

                    //
                    // Execute the workitem outside of any finally blocks, so that it can be aborted if needed.
                    //
                    if (ThreadPoolGlobals.enableWorkerTracking)
                    {
                        bool reportedStatus = false;
                        try
                        {
                            ThreadPool.ReportThreadStatus(isWorking: true);
                            reportedStatus = true;
                            if (workItem is Task task)
                            {
                                task.ExecuteFromThreadPool(currentThread);
                            }
                            else
                            {
                                Debug.Assert(workItem is IThreadPoolWorkItem);
                                Unsafe.As<IThreadPoolWorkItem>(workItem).Execute();
                            }
                        }
                        finally
                        {
                            if (reportedStatus)
                                ThreadPool.ReportThreadStatus(isWorking: false);
                        }
                    }
                    else if (workItem is Task task)
                    {
                        // Check for Task first as it's currently faster to type check
                        // for Task and then Unsafe.As for the interface, rather than
                        // vice versa, in particular when the object implements a bunch
                        // of interfaces.
                        task.ExecuteFromThreadPool(currentThread);
                    }
                    else
                    {
                        Debug.Assert(workItem is IThreadPoolWorkItem);
                        Unsafe.As<IThreadPoolWorkItem>(workItem).Execute();
                    }

                    // Release refs
                    outerWorkItem = workItem = null;

                    // Return to clean ExecutionContext and SynchronizationContext
                    ExecutionContext.ResetThreadPoolThread(currentThread);

                    // 
                    // Notify the VM that we executed this workitem.  This is also our opportunity to ask whether Hill Climbing wants
                    // us to return the thread to the pool or not.
                    //
                    if (!ThreadPool.NotifyWorkItemComplete())
                        return false;
                }

                // If we get here, it's because our quantum expired.  Tell the VM we're returning normally.
                return true;
            }
            catch (ThreadAbortException tae)
            {
                //
                // This is here to catch the case where this thread is aborted between the time we exit the finally block in the dispatch
                // loop, and the time we execute the work item.  QueueUserWorkItemCallback uses this to update its accounting of whether
                // it was executed or not (in debug builds only).  Task uses this to communicate the ThreadAbortException to anyone
                // who waits for the task to complete.
                //
                if (outerWorkItem is Task task)
                {
                    task.MarkAbortedFromThreadPool(tae);
                }

                //
                // In this case, the VM is going to request another thread on our behalf.  No need to do it twice.
                //
                needAnotherThread = false;
            }
            finally
            {
                //
                // If we are exiting for any reason other than that the queue is definitely empty, ask for another
                // thread to pick up where we left off.
                //
                if (needAnotherThread)
                    outerWorkQueue.EnsureThreadRequested();
            }

            // we can never reach this point, but the C# compiler doesn't know that, because it doesn't know the ThreadAbortException will be reraised above.
            Debug.Fail("Should never reach this point");
            return true;
        }
    }

    // Simple random number generator. We don't need great randomness, we just need a little and for it to be fast.
    internal struct FastRandom // xorshift prng
    {
        private uint _w, _x, _y, _z;

        public FastRandom(int seed)
        {
            _x = (uint)seed;
            _w = 88675123;
            _y = 362436069;
            _z = 521288629;
        }

        public int Next(int maxValue)
        {
            Debug.Assert(maxValue > 0);

            uint t = _x ^ (_x << 11);
            _x = _y; _y = _z; _z = _w;
            _w = _w ^ (_w >> 19) ^ (t ^ (t >> 8));

            return (int)(_w % (uint)maxValue);
        }
    }

    // Holds a WorkStealingQueue, and removes it from the list when this object is no longer referenced.
    internal sealed class ThreadPoolWorkQueueThreadLocals
    {
        [ThreadStatic]
        public static ThreadPoolWorkQueueThreadLocals threadLocals;

        public readonly ThreadPoolWorkQueue workQueue;
        public readonly ThreadPoolWorkQueue.WorkStealingQueue workStealingQueue;
        public readonly Thread currentThread;
        public FastRandom random = new FastRandom(Thread.CurrentThread.ManagedThreadId); // mutable struct, do not copy or make readonly

        public ThreadPoolWorkQueueThreadLocals(ThreadPoolWorkQueue tpq)
        {
            workQueue = tpq;
            workStealingQueue = new ThreadPoolWorkQueue.WorkStealingQueue();
            ThreadPoolWorkQueue.WorkStealingQueueList.Add(workStealingQueue);
            currentThread = Thread.CurrentThread;
        }

        private void CleanUp()
        {
            if (null != workStealingQueue)
            {
                if (null != workQueue)
                {
                    object cb;
                    while ((cb = workStealingQueue.LocalPop()) != null)
                    {
                        Debug.Assert(null != cb);
                        workQueue.Enqueue(cb, forceGlobal: true);
                    }
                }

                ThreadPoolWorkQueue.WorkStealingQueueList.Remove(workStealingQueue);
            }
        }

        ~ThreadPoolWorkQueueThreadLocals()
        {
            // Since the purpose of calling CleanUp is to transfer any pending workitems into the global
            // queue so that they will be executed by another thread, there's no point in doing this cleanup
            // if we're in the process of shutting down or unloading the AD.  In those cases, the work won't
            // execute anyway.  And there are subtle race conditions involved there that would lead us to do the wrong
            // thing anyway.  So we'll only clean up if this is a "normal" finalization.
            if (!Environment.HasShutdownStarted)
                CleanUp();
        }
    }

    internal sealed class RegisteredWaitHandleSafe : CriticalFinalizerObject
    {
        private static IntPtr InvalidHandle => Win32Native.INVALID_HANDLE_VALUE;
        private IntPtr registeredWaitHandle = InvalidHandle;
        private WaitHandle m_internalWaitObject;
        private bool bReleaseNeeded = false;
        private volatile int m_lock = 0;

        internal IntPtr GetHandle() => registeredWaitHandle;

        internal void SetHandle(IntPtr handle)
        {
            registeredWaitHandle = handle;
        }

        internal void SetWaitObject(WaitHandle waitObject)
        {
            // needed for DangerousAddRef
            RuntimeHelpers.PrepareConstrainedRegions();

            m_internalWaitObject = waitObject;
            if (waitObject != null)
            {
                m_internalWaitObject.SafeWaitHandle.DangerousAddRef(ref bReleaseNeeded);
            }
        }

        internal bool Unregister(
             WaitHandle waitObject          // object to be notified when all callbacks to delegates have completed
             )
        {
            bool result = false;
            // needed for DangerousRelease
            RuntimeHelpers.PrepareConstrainedRegions();

            // lock(this) cannot be used reliably in Cer since thin lock could be
            // promoted to syncblock and that is not a guaranteed operation
            bool bLockTaken = false;
            do
            {
                if (Interlocked.CompareExchange(ref m_lock, 1, 0) == 0)
                {
                    bLockTaken = true;
                    try
                    {
                        if (ValidHandle())
                        {
                            result = UnregisterWaitNative(GetHandle(), waitObject == null ? null : waitObject.SafeWaitHandle);
                            if (result == true)
                            {
                                if (bReleaseNeeded)
                                {
                                    m_internalWaitObject.SafeWaitHandle.DangerousRelease();
                                    bReleaseNeeded = false;
                                }
                                // if result not true don't release/suppress here so finalizer can make another attempt
                                SetHandle(InvalidHandle);
                                m_internalWaitObject = null;
                                GC.SuppressFinalize(this);
                            }
                        }
                    }
                    finally
                    {
                        m_lock = 0;
                    }
                }
                Thread.SpinWait(1);     // yield to processor
            }
            while (!bLockTaken);

            return result;
        }

        private bool ValidHandle() =>
            registeredWaitHandle != InvalidHandle && registeredWaitHandle != IntPtr.Zero;

        ~RegisteredWaitHandleSafe()
        {
            // if the app has already unregistered the wait, there is nothing to cleanup
            // we can detect this by checking the handle. Normally, there is no race condition here
            // so no need to protect reading of handle. However, if this object gets 
            // resurrected and then someone does an unregister, it would introduce a race condition
            //
            // PrepareConstrainedRegions call not needed since finalizer already in Cer
            //
            // lock(this) cannot be used reliably even in Cer since thin lock could be
            // promoted to syncblock and that is not a guaranteed operation
            //
            // Note that we will not "spin" to get this lock.  We make only a single attempt;
            // if we can't get the lock, it means some other thread is in the middle of a call
            // to Unregister, which will do the work of the finalizer anyway.
            //
            // Further, it's actually critical that we *not* wait for the lock here, because
            // the other thread that's in the middle of Unregister may be suspended for shutdown.
            // Then, during the live-object finalization phase of shutdown, this thread would
            // end up spinning forever, as the other thread would never release the lock.
            // This will result in a "leak" of sorts (since the handle will not be cleaned up)
            // but the process is exiting anyway.
            //
            // During AD-unload, we don�t finalize live objects until all threads have been 
            // aborted out of the AD.  Since these locked regions are CERs, we won�t abort them 
            // while the lock is held.  So there should be no leak on AD-unload.
            //
            if (Interlocked.CompareExchange(ref m_lock, 1, 0) == 0)
            {
                try
                {
                    if (ValidHandle())
                    {
                        WaitHandleCleanupNative(registeredWaitHandle);
                        if (bReleaseNeeded)
                        {
                            m_internalWaitObject.SafeWaitHandle.DangerousRelease();
                            bReleaseNeeded = false;
                        }
                        SetHandle(InvalidHandle);
                        m_internalWaitObject = null;
                    }
                }
                finally
                {
                    m_lock = 0;
                }
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void WaitHandleCleanupNative(IntPtr handle);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool UnregisterWaitNative(IntPtr handle, SafeHandle waitObject);
    }

    public sealed class RegisteredWaitHandle : MarshalByRefObject
    {
        private readonly RegisteredWaitHandleSafe internalRegisteredWait;

        internal RegisteredWaitHandle()
        {
            internalRegisteredWait = new RegisteredWaitHandleSafe();
        }

        internal void SetHandle(IntPtr handle)
        {
            internalRegisteredWait.SetHandle(handle);
        }

        internal void SetWaitObject(WaitHandle waitObject)
        {
            internalRegisteredWait.SetWaitObject(waitObject);
        }

        // This is the only public method on this class
        public bool Unregister(
             WaitHandle waitObject          // object to be notified when all callbacks to delegates have completed
             )
        {
            return internalRegisteredWait.Unregister(waitObject);
        }
    }

    public delegate void WaitCallback(object state);

    public delegate void WaitOrTimerCallback(object state, bool timedOut);  // signaled or timed out

    /// <summary>Represents a work item that can be executed by the ThreadPool.</summary>
    public interface IThreadPoolWorkItem
    {
        void Execute();
    }

    //
    // This type is necessary because VS 2010's debugger looks for a method named _ThreadPoolWaitCallbacck.PerformWaitCallback
    // on the stack to determine if a thread is a ThreadPool thread or not.  We have a better way to do this for .NET 4.5, but
    // still need to maintain compatibility with VS 2010.  When compat with VS 2010 is no longer an issue, this type may be
    // removed.
    //
    internal static class _ThreadPoolWaitCallback
    {
        internal static bool PerformWaitCallback() => ThreadPoolWorkQueue.Dispatch();
    }

    internal abstract class QueueUserWorkItemCallbackBase : IThreadPoolWorkItem
    {
#if DEBUG
        private volatile int executed;

        ~QueueUserWorkItemCallbackBase()
        {
            Debug.Assert(
                executed != 0 || Environment.HasShutdownStarted,
                "A QueueUserWorkItemCallback was never called!");
        }
#endif

        public virtual void Execute()
        {
#if DEBUG
            GC.SuppressFinalize(this);
            Debug.Assert(
                0 == Interlocked.Exchange(ref executed, 1),
                "A QueueUserWorkItemCallback was called twice!");
#endif
        }
    }

    internal sealed class QueueUserWorkItemCallback : QueueUserWorkItemCallbackBase
    {
        private WaitCallback _callback; // SOS's ThreadPool command depends on this name
        private readonly object _state;
        private readonly ExecutionContext _context;

        private static readonly Action<QueueUserWorkItemCallback> s_executionContextShim = quwi =>
        {
            WaitCallback callback = quwi._callback;
            quwi._callback = null;

            callback(quwi._state);
        };

        internal QueueUserWorkItemCallback(WaitCallback callback, object state, ExecutionContext context)
        {
            Debug.Assert(context != null);

            _callback = callback;
            _state = state;
            _context = context;
        }

        public override void Execute()
        {
            base.Execute();

            ExecutionContext.RunForThreadPoolUnsafe(_context, s_executionContextShim, this);
        }
    }

    internal sealed class QueueUserWorkItemCallback<TState> : QueueUserWorkItemCallbackBase
    {
        private Action<TState> _callback; // SOS's ThreadPool command depends on this name
        private readonly TState _state;
        private readonly ExecutionContext _context;

        internal QueueUserWorkItemCallback(Action<TState> callback, TState state, ExecutionContext context)
        {
            Debug.Assert(callback != null);

            _callback = callback;
            _state = state;
            _context = context;
        }

        public override void Execute()
        {
            base.Execute();

            Action<TState> callback = _callback;
            _callback = null;

            ExecutionContext.RunForThreadPoolUnsafe(_context, callback, in _state);
        }
    }

    internal sealed class QueueUserWorkItemCallbackDefaultContext : QueueUserWorkItemCallbackBase
    {
        private WaitCallback _callback; // SOS's ThreadPool command depends on this name
        private readonly object _state;

        internal QueueUserWorkItemCallbackDefaultContext(WaitCallback callback, object state)
        {
            Debug.Assert(callback != null);

            _callback = callback;
            _state = state;
        }

        public override void Execute()
        {
            ExecutionContext.CheckThreadPoolAndContextsAreDefault();
            base.Execute();

            WaitCallback callback = _callback;
            _callback = null;

            callback(_state);

            // ThreadPoolWorkQueue.Dispatch will handle notifications and reset EC and SyncCtx back to default
        }
    }

    internal sealed class QueueUserWorkItemCallbackDefaultContext<TState> : QueueUserWorkItemCallbackBase
    {
        private Action<TState> _callback; // SOS's ThreadPool command depends on this name
        private readonly TState _state;

        internal QueueUserWorkItemCallbackDefaultContext(Action<TState> callback, TState state)
        {
            Debug.Assert(callback != null);

            _callback = callback;
            _state = state;
        }

        public override void Execute()
        {
            ExecutionContext.CheckThreadPoolAndContextsAreDefault();
            base.Execute();

            Action<TState> callback = _callback;
            _callback = null;

            callback(_state);

            // ThreadPoolWorkQueue.Dispatch will handle notifications and reset EC and SyncCtx back to default
        }
    }

    internal class _ThreadPoolWaitOrTimerCallback
    {
        private WaitOrTimerCallback _waitOrTimerCallback;
        private ExecutionContext _executionContext;
        private object _state;
        private static readonly ContextCallback _ccbt = new ContextCallback(WaitOrTimerCallback_Context_t);
        private static readonly ContextCallback _ccbf = new ContextCallback(WaitOrTimerCallback_Context_f);

        internal _ThreadPoolWaitOrTimerCallback(WaitOrTimerCallback waitOrTimerCallback, object state, bool compressStack)
        {
            _waitOrTimerCallback = waitOrTimerCallback;
            _state = state;

            if (compressStack)
            {
                // capture the exection context
                _executionContext = ExecutionContext.Capture();
            }
        }

        private static void WaitOrTimerCallback_Context_t(object state) =>
            WaitOrTimerCallback_Context(state, timedOut: true);

        private static void WaitOrTimerCallback_Context_f(object state) =>
            WaitOrTimerCallback_Context(state, timedOut: false);

        private static void WaitOrTimerCallback_Context(object state, bool timedOut)
        {
            _ThreadPoolWaitOrTimerCallback helper = (_ThreadPoolWaitOrTimerCallback)state;
            helper._waitOrTimerCallback(helper._state, timedOut);
        }

        // call back helper
        internal static void PerformWaitOrTimerCallback(object state, bool timedOut)
        {
            _ThreadPoolWaitOrTimerCallback helper = (_ThreadPoolWaitOrTimerCallback)state;
            Debug.Assert(helper != null, "Null state passed to PerformWaitOrTimerCallback!");
            // call directly if it is an unsafe call OR EC flow is suppressed
            ExecutionContext context = helper._executionContext;
            if (context == null)
            {
                WaitOrTimerCallback callback = helper._waitOrTimerCallback;
                callback(helper._state, timedOut);
            }
            else
            {
                ExecutionContext.Run(context, timedOut ? _ccbt : _ccbf, helper);
            }
        }
    }

    [CLSCompliant(false)]
    public unsafe delegate void IOCompletionCallback(uint errorCode, // Error code
                                       uint numBytes, // No. of bytes transferred 
                                       NativeOverlapped* pOVERLAP // ptr to OVERLAP structure
                                       );

    public static class ThreadPool
    {
        public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
        {
            return SetMaxThreadsNative(workerThreads, completionPortThreads);
        }

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            GetMaxThreadsNative(out workerThreads, out completionPortThreads);
        }

        public static bool SetMinThreads(int workerThreads, int completionPortThreads)
        {
            return SetMinThreadsNative(workerThreads, completionPortThreads);
        }

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            GetMinThreadsNative(out workerThreads, out completionPortThreads);
        }

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            GetAvailableThreadsNative(out workerThreads, out completionPortThreads);
        }

        [CLSCompliant(false)]
        public static RegisteredWaitHandle RegisterWaitForSingleObject(  // throws RegisterWaitException
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce    // NOTE: we do not allow other options that allow the callback to be queued as an APC
             )
        {
            return RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, true);
        }

        [CLSCompliant(false)]
        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(  // throws RegisterWaitException
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce    // NOTE: we do not allow other options that allow the callback to be queued as an APC
             )
        {
            return RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, false);
        }


        private static RegisteredWaitHandle RegisterWaitForSingleObject(  // throws RegisterWaitException
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce,   // NOTE: we do not allow other options that allow the callback to be queued as an APC
             bool compressStack
             )
        {
            RegisteredWaitHandle registeredWaitHandle = new RegisteredWaitHandle();

            if (callBack != null)
            {
                _ThreadPoolWaitOrTimerCallback callBackHelper = new _ThreadPoolWaitOrTimerCallback(callBack, state, compressStack);
                state = (object)callBackHelper;
                // call SetWaitObject before native call so that waitObject won't be closed before threadpoolmgr registration
                // this could occur if callback were to fire before SetWaitObject does its addref
                registeredWaitHandle.SetWaitObject(waitObject);
                IntPtr nativeRegisteredWaitHandle = RegisterWaitForSingleObjectNative(waitObject,
                                                                               state,
                                                                               millisecondsTimeOutInterval,
                                                                               executeOnlyOnce,
                                                                               registeredWaitHandle);
                registeredWaitHandle.SetHandle(nativeRegisteredWaitHandle);
            }
            else
            {
                throw new ArgumentNullException(nameof(WaitOrTimerCallback));
            }
            return registeredWaitHandle;
        }


        public static RegisteredWaitHandle RegisterWaitForSingleObject(  // throws RegisterWaitException
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object state,
             int millisecondsTimeOutInterval,
             bool executeOnlyOnce    // NOTE: we do not allow other options that allow the callback to be queued as an APC
             )
        {
            if (millisecondsTimeOutInterval < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeOutInterval), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, true);
        }

        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(  // throws RegisterWaitException
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object state,
             int millisecondsTimeOutInterval,
             bool executeOnlyOnce    // NOTE: we do not allow other options that allow the callback to be queued as an APC
             )
        {
            if (millisecondsTimeOutInterval < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeOutInterval), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, false);
        }

        public static RegisteredWaitHandle RegisterWaitForSingleObject(  // throws RegisterWaitException
            WaitHandle waitObject,
            WaitOrTimerCallback callBack,
            object state,
            long millisecondsTimeOutInterval,
            bool executeOnlyOnce    // NOTE: we do not allow other options that allow the callback to be queued as an APC
        )
        {
            if (millisecondsTimeOutInterval < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeOutInterval), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, true);
        }

        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(  // throws RegisterWaitException
            WaitHandle waitObject,
            WaitOrTimerCallback callBack,
            object state,
            long millisecondsTimeOutInterval,
            bool executeOnlyOnce    // NOTE: we do not allow other options that allow the callback to be queued as an APC
        )
        {
            if (millisecondsTimeOutInterval < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeOutInterval), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, false);
        }

        public static RegisteredWaitHandle RegisterWaitForSingleObject(
                          WaitHandle waitObject,
                          WaitOrTimerCallback callBack,
                          object state,
                          TimeSpan timeout,
                          bool executeOnlyOnce
                          )
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1)
                throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (tm > (long)int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
            return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)tm, executeOnlyOnce, true);
        }

        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(
                          WaitHandle waitObject,
                          WaitOrTimerCallback callBack,
                          object state,
                          TimeSpan timeout,
                          bool executeOnlyOnce
                          )
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1)
                throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (tm > (long)int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
            return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)tm, executeOnlyOnce, false);
        }

        public static bool QueueUserWorkItem(WaitCallback callBack) =>
            QueueUserWorkItem(callBack, null);

        public static bool QueueUserWorkItem(WaitCallback callBack, object state)
        {
            if (callBack == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
            }

            EnsureVMInitialized();

            ExecutionContext context = ExecutionContext.Capture();

            object tpcallBack = (context == null || context.IsDefault) ?
                new QueueUserWorkItemCallbackDefaultContext(callBack, state) :
                (object)new QueueUserWorkItemCallback(callBack, state, context);

            ThreadPoolGlobals.workQueue.Enqueue(tpcallBack, forceGlobal: true);

            return true;
        }

        public static bool QueueUserWorkItem<TState>(Action<TState> callBack, TState state, bool preferLocal)
        {
            if (callBack == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
            }

            EnsureVMInitialized();

            ExecutionContext context = ExecutionContext.Capture();

            object tpcallBack = (context == null || context.IsDefault) ?
                new QueueUserWorkItemCallbackDefaultContext<TState>(callBack, state) :
                (object)new QueueUserWorkItemCallback<TState>(callBack, state, context);

            ThreadPoolGlobals.workQueue.Enqueue(tpcallBack, forceGlobal: !preferLocal);

            return true;
        }

        public static bool UnsafeQueueUserWorkItem<TState>(Action<TState> callBack, TState state, bool preferLocal)
        {
            if (callBack == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
            }

            // If the callback is the runtime-provided invocation of an IAsyncStateMachineBox,
            // then we can queue the Task state directly to the ThreadPool instead of 
            // wrapping it in a QueueUserWorkItemCallback.
            //
            // This occurs when user code queues its provided continuation to the ThreadPool;
            // internally we call UnsafeQueueUserWorkItemInternal directly for Tasks.
            if (ReferenceEquals(callBack, ThreadPoolGlobals.s_invokeAsyncStateMachineBox))
            {
                if (!(state is IAsyncStateMachineBox))
                {
                    // The provided state must be the internal IAsyncStateMachineBox (Task) type
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.state);
                }

                UnsafeQueueUserWorkItemInternal((object)state, preferLocal);
                return true;
            }

            EnsureVMInitialized();

            ThreadPoolGlobals.workQueue.Enqueue(
                new QueueUserWorkItemCallbackDefaultContext<TState>(callBack, state), forceGlobal: !preferLocal);

            return true;
        }

        public static bool UnsafeQueueUserWorkItem(WaitCallback callBack, object state)
        {
            if (callBack == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
            }

            EnsureVMInitialized();

            object tpcallBack = new QueueUserWorkItemCallbackDefaultContext(callBack, state);

            ThreadPoolGlobals.workQueue.Enqueue(tpcallBack, forceGlobal: true);

            return true;
        }

        public static bool UnsafeQueueUserWorkItem(IThreadPoolWorkItem callBack, bool preferLocal)
        {
            if (callBack == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
            }
            if (callBack is Task)
            {
                // Prevent code from queueing a derived Task that also implements the interface,
                // as that would bypass Task.Start and its safety checks.
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.callBack);
            }

            UnsafeQueueUserWorkItemInternal(callBack, preferLocal);
            return true;
        }

        internal static void UnsafeQueueUserWorkItemInternal(object callBack, bool preferLocal)
        {
            Debug.Assert((callBack is IThreadPoolWorkItem) ^ (callBack is Task));

            EnsureVMInitialized();

            ThreadPoolGlobals.workQueue.Enqueue(callBack, forceGlobal: !preferLocal);
        }

        // This method tries to take the target callback out of the current thread's queue.
        internal static bool TryPopCustomWorkItem(object workItem)
        {
            Debug.Assert(null != workItem);
            return
                ThreadPoolGlobals.vmTpInitialized && // if not initialized, so there's no way this workitem was ever queued.
                ThreadPoolGlobals.workQueue.LocalFindAndPop(workItem);
        }

        // Get all workitems.  Called by TaskScheduler in its debugger hooks.
        internal static IEnumerable<object> GetQueuedWorkItems()
        {
            // Enumerate global queue
            foreach (object workItem in ThreadPoolGlobals.workQueue.workItems)
            {
                yield return workItem;
            }

            // Enumerate each local queue
            foreach (ThreadPoolWorkQueue.WorkStealingQueue wsq in ThreadPoolWorkQueue.WorkStealingQueueList.Queues)
            {
                if (wsq != null && wsq.m_array != null)
                {
                    object[] items = wsq.m_array;
                    for (int i = 0; i < items.Length; i++)
                    {
                        object item = items[i];
                        if (item != null)
                        {
                            yield return item;
                        }
                    }
                }
            }
        }

        internal static IEnumerable<object> GetLocallyQueuedWorkItems()
        {
            ThreadPoolWorkQueue.WorkStealingQueue wsq = ThreadPoolWorkQueueThreadLocals.threadLocals.workStealingQueue;
            if (wsq != null && wsq.m_array != null)
            {
                object[] items = wsq.m_array;
                for (int i = 0; i < items.Length; i++)
                {
                    object item = items[i];
                    if (item != null)
                        yield return item;
                }
            }
        }

        internal static IEnumerable<object> GetGloballyQueuedWorkItems() => ThreadPoolGlobals.workQueue.workItems;

        private static object[] ToObjectArray(IEnumerable<object> workitems)
        {
            int i = 0;
            foreach (object item in workitems)
            {
                i++;
            }

            object[] result = new object[i];
            i = 0;
            foreach (object item in workitems)
            {
                if (i < result.Length) //just in case someone calls us while the queues are in motion
                    result[i] = item;
                i++;
            }

            return result;
        }

        // This is the method the debugger will actually call, if it ends up calling
        // into ThreadPool directly.  Tests can use this to simulate a debugger, as well.
        internal static object[] GetQueuedWorkItemsForDebugger() =>
            ToObjectArray(GetQueuedWorkItems());

        internal static object[] GetGloballyQueuedWorkItemsForDebugger() =>
            ToObjectArray(GetGloballyQueuedWorkItems());

        internal static object[] GetLocallyQueuedWorkItemsForDebugger() =>
            ToObjectArray(GetLocallyQueuedWorkItems());

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern bool RequestWorkerThread();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe bool PostQueuedCompletionStatus(NativeOverlapped* overlapped);

        [CLSCompliant(false)]
        public static unsafe bool UnsafeQueueNativeOverlapped(NativeOverlapped* overlapped) =>
            PostQueuedCompletionStatus(overlapped);

        // The thread pool maintains a per-appdomain managed work queue.
        // New thread pool entries are added in the managed queue.
        // The VM is responsible for the actual growing/shrinking of 
        // threads. 
        private static void EnsureVMInitialized()
        {
            if (!ThreadPoolGlobals.vmTpInitialized)
            {
                EnsureVMInitializedCore(); // separate out to help with inlining
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnsureVMInitializedCore()
        {
            InitializeVMTp(ref ThreadPoolGlobals.enableWorkerTracking);
            ThreadPoolGlobals.vmTpInitialized = true;
        }

        // Native methods: 

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool SetMinThreadsNative(int workerThreads, int completionPortThreads);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool SetMaxThreadsNative(int workerThreads, int completionPortThreads);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetMinThreadsNative(out int workerThreads, out int completionPortThreads);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetMaxThreadsNative(out int workerThreads, out int completionPortThreads);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetAvailableThreadsNative(out int workerThreads, out int completionPortThreads);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool NotifyWorkItemComplete();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ReportThreadStatus(bool isWorking);

        internal static void NotifyWorkItemProgress()
        {
            if (!ThreadPoolGlobals.vmTpInitialized)
                ThreadPool.InitializeVMTp(ref ThreadPoolGlobals.enableWorkerTracking);
            NotifyWorkItemProgressNative();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void NotifyWorkItemProgressNative();

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void InitializeVMTp(ref bool enableWorkerTracking);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr RegisterWaitForSingleObjectNative(
             WaitHandle waitHandle,
             object state,
             uint timeOutInterval,
             bool executeOnlyOnce,
             RegisteredWaitHandle registeredWaitHandle
             );


        [Obsolete("ThreadPool.BindHandle(IntPtr) has been deprecated.  Please use ThreadPool.BindHandle(SafeHandle) instead.", false)]
        public static bool BindHandle(IntPtr osHandle)
        {
            return BindIOCompletionCallbackNative(osHandle);
        }

        public static bool BindHandle(SafeHandle osHandle)
        {
            if (osHandle == null)
                throw new ArgumentNullException(nameof(osHandle));

            bool ret = false;
            bool mustReleaseSafeHandle = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                osHandle.DangerousAddRef(ref mustReleaseSafeHandle);
                ret = BindIOCompletionCallbackNative(osHandle.DangerousGetHandle());
            }
            finally
            {
                if (mustReleaseSafeHandle)
                    osHandle.DangerousRelease();
            }
            return ret;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool BindIOCompletionCallbackNative(IntPtr fileHandle);
    }
}
