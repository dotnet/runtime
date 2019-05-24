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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Internal.Runtime.CompilerServices;

namespace System.Threading
{
    internal static class ThreadPoolGlobals
    {
        public static readonly int processorCount = Environment.ProcessorCount;

        public static volatile bool threadPoolInitialized;
        public static bool enableWorkerTracking;

        public static readonly ThreadPoolWorkQueue workQueue = new ThreadPoolWorkQueue();

        /// <summary>Shim used to invoke <see cref="IAsyncStateMachineBox.MoveNext"/> of the supplied <see cref="IAsyncStateMachineBox"/>.</summary>
        internal static readonly Action<object?> s_invokeAsyncStateMachineBox = state =>
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
            internal volatile object?[] m_array = new object[INITIAL_SIZE]; // SOS's ThreadPool command depends on this name
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
                            var newArray = new object?[m_array.Length << 1];
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
                    object? unused = LocalPop();
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

            public object? LocalPop() => m_headIndex < m_tailIndex ? LocalPopCore() : null;

            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
            private object? LocalPopCore()
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
                        object? obj = Volatile.Read(ref m_array[idx]);

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
                                object? obj = Volatile.Read(ref m_array[idx]);

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

            public object? TrySteal(ref bool missedSteal)
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
                                    object? obj = Volatile.Read(ref m_array[idx]);

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

            public int Count
            {
                get
                {
                    bool lockTaken = false;
                    try
                    {
                        m_foreignLock.Enter(ref lockTaken);
                        return Math.Max(0, m_tailIndex - m_headIndex);
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            m_foreignLock.Exit(useMemoryBarrier: false);
                        }
                    }
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

        public ThreadPoolWorkQueueThreadLocals GetOrCreateThreadLocals() =>
            ThreadPoolWorkQueueThreadLocals.threadLocals ?? CreateThreadLocals();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ThreadPoolWorkQueueThreadLocals CreateThreadLocals()
        {
            Debug.Assert(ThreadPoolWorkQueueThreadLocals.threadLocals == null);

            return (ThreadPoolWorkQueueThreadLocals.threadLocals = new ThreadPoolWorkQueueThreadLocals(this));
        }

        internal void EnsureThreadRequested()
        {
            //
            // If we have not yet requested #procs threads, then request a new thread.
            //
            // CoreCLR: Note that there is a separate count in the VM which has already been incremented
            // by the VM by the time we reach this point.
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
            // One of our outstanding thread requests has been satisfied.
            // Decrement the count so that future calls to EnsureThreadRequested will succeed.
            //
            // CoreCLR: Note that there is a separate count in the VM which has already been decremented
            // by the VM by the time we reach this point.
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

            ThreadPoolWorkQueueThreadLocals? tl = null;
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

        public object? Dequeue(ThreadPoolWorkQueueThreadLocals tl, ref bool missedSteal)
        {
            WorkStealingQueue localWsq = tl.workStealingQueue;
            object? callback;

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

        public long LocalCount
        {
            get
            {
                long count = 0;
                foreach (WorkStealingQueue workStealingQueue in WorkStealingQueueList.Queues)
                {
                    count += workStealingQueue.Count;
                }
                return count;
            }
        }

        public long GlobalCount => workItems.Count;

        /// <summary>
        /// Dispatches work items to this thread.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this thread did as much work as was available or its quantum expired.
        /// <c>false</c> if this thread stopped working early.
        /// </returns>
        internal static bool Dispatch()
        {
            ThreadPoolWorkQueue outerWorkQueue = ThreadPoolGlobals.workQueue;

            //
            // Save the start time
            //
            int startTickCount = Environment.TickCount;

            //
            // Update our records to indicate that an outstanding request for a thread has now been fulfilled.
            // From this point on, we are responsible for requesting another thread if we stop working for any
            // reason, and we believe there might still be work in the queue.
            //
            // CoreCLR: Note that if this thread is aborted before we get a chance to request another one, the VM will
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
            object? outerWorkItem = null;
            try
            {
                //
                // Set up our thread-local data
                //
                // Use operate on workQueue local to try block so it can be enregistered 
                ThreadPoolWorkQueue workQueue = outerWorkQueue;
                ThreadPoolWorkQueueThreadLocals tl = workQueue.GetOrCreateThreadLocals();
                Thread currentThread = tl.currentThread;

                // Start on clean ExecutionContext and SynchronizationContext
                currentThread._executionContext = null;
                currentThread._synchronizationContext = null;

                //
                // Loop until our quantum expires or there is no work.
                //
                while (ThreadPool.KeepDispatching(startTickCount))
                {
                    bool missedSteal = false;
                    // Use operate on workItem local to try block so it can be enregistered 
                    object? workItem = outerWorkItem = workQueue.Dequeue(tl, ref missedSteal);

                    if (workItem == null)
                    {
                        //
                        // No work.
                        // If we missed a steal, though, there may be more work in the queue.
                        // Instead of looping around and trying again, we'll just request another thread.  Hopefully the thread
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

                    currentThread.ResetThreadPoolThread();

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
            finally
            {
                //
                // If we are exiting for any reason other than that the queue is definitely empty, ask for another
                // thread to pick up where we left off.
                //
                if (needAnotherThread)
                    outerWorkQueue.EnsureThreadRequested();
            }
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

        ~ThreadPoolWorkQueueThreadLocals()
        {
            // Transfer any pending workitems into the global queue so that they will be executed by another thread
            if (null != workStealingQueue)
            {
                if (null != workQueue)
                {
                    object? cb;
                    while ((cb = workStealingQueue.LocalPop()) != null)
                    {
                        Debug.Assert(null != cb);
                        workQueue.Enqueue(cb, forceGlobal: true);
                    }
                }

                ThreadPoolWorkQueue.WorkStealingQueueList.Remove(workStealingQueue);
            }
        }
    }

    public delegate void WaitCallback(object? state);

    public delegate void WaitOrTimerCallback(object? state, bool timedOut);  // signaled or timed out

    internal abstract class QueueUserWorkItemCallbackBase : IThreadPoolWorkItem
    {
#if DEBUG
        private volatile int executed;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1821:RemoveEmptyFinalizers")]
        ~QueueUserWorkItemCallbackBase()
        {
            Debug.Assert(
                executed != 0, "A QueueUserWorkItemCallback was never called!");
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
        private WaitCallback? _callback; // SOS's ThreadPool command depends on this name
        private readonly object? _state;
        private readonly ExecutionContext _context;

        private static readonly Action<QueueUserWorkItemCallback> s_executionContextShim = quwi =>
        {
            Debug.Assert(quwi._callback != null);
            WaitCallback callback = quwi._callback;
            quwi._callback = null;

            callback(quwi._state);
        };

        internal QueueUserWorkItemCallback(WaitCallback callback, object? state, ExecutionContext context)
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
        private Action<TState>? _callback; // SOS's ThreadPool command depends on this name
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

            Debug.Assert(_callback != null);
            Action<TState> callback = _callback;
            _callback = null;

            ExecutionContext.RunForThreadPoolUnsafe(_context, callback, in _state);
        }
    }

    internal sealed class QueueUserWorkItemCallbackDefaultContext : QueueUserWorkItemCallbackBase
    {
        private WaitCallback? _callback; // SOS's ThreadPool command depends on this name
        private readonly object? _state;

        internal QueueUserWorkItemCallbackDefaultContext(WaitCallback callback, object? state)
        {
            Debug.Assert(callback != null);

            _callback = callback;
            _state = state;
        }

        public override void Execute()
        {
            ExecutionContext.CheckThreadPoolAndContextsAreDefault();
            base.Execute();

            Debug.Assert(_callback != null);
            WaitCallback callback = _callback;
            _callback = null;

            callback(_state);

            // ThreadPoolWorkQueue.Dispatch will handle notifications and reset EC and SyncCtx back to default
        }
    }

    internal sealed class QueueUserWorkItemCallbackDefaultContext<TState> : QueueUserWorkItemCallbackBase
    {
        private Action<TState>? _callback; // SOS's ThreadPool command depends on this name
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

            Debug.Assert(_callback != null);
            Action<TState> callback = _callback;
            _callback = null;

            callback(_state);

            // ThreadPoolWorkQueue.Dispatch will handle notifications and reset EC and SyncCtx back to default
        }
    }

    internal sealed class _ThreadPoolWaitOrTimerCallback
    {
        private WaitOrTimerCallback _waitOrTimerCallback;
        private ExecutionContext? _executionContext;
        private object? _state;
        private static readonly ContextCallback _ccbt = new ContextCallback(WaitOrTimerCallback_Context_t);
        private static readonly ContextCallback _ccbf = new ContextCallback(WaitOrTimerCallback_Context_f);

        internal _ThreadPoolWaitOrTimerCallback(WaitOrTimerCallback waitOrTimerCallback, object? state, bool flowExecutionContext)
        {
            _waitOrTimerCallback = waitOrTimerCallback;
            _state = state;

            if (flowExecutionContext)
            {
                // capture the exection context
                _executionContext = ExecutionContext.Capture();
            }
        }

        private static void WaitOrTimerCallback_Context_t(object? state) =>
            WaitOrTimerCallback_Context(state, timedOut: true);

        private static void WaitOrTimerCallback_Context_f(object? state) =>
            WaitOrTimerCallback_Context(state, timedOut: false);

        private static void WaitOrTimerCallback_Context(object? state, bool timedOut)
        {
            _ThreadPoolWaitOrTimerCallback helper = (_ThreadPoolWaitOrTimerCallback)state!;
            helper._waitOrTimerCallback(helper._state, timedOut);
        }

        // call back helper
        internal static void PerformWaitOrTimerCallback(_ThreadPoolWaitOrTimerCallback helper, bool timedOut)
        {
            Debug.Assert(helper != null, "Null state passed to PerformWaitOrTimerCallback!");
            // call directly if it is an unsafe call OR EC flow is suppressed
            ExecutionContext? context = helper._executionContext;
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

    public static partial class ThreadPool
    {
        [CLSCompliant(false)]
        public static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object? state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce    // NOTE: we do not allow other options that allow the callback to be queued as an APC
             )
        {
            if (millisecondsTimeOutInterval > (uint)int.MaxValue && millisecondsTimeOutInterval != uint.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeOutInterval), SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
            return RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, true);
        }

        [CLSCompliant(false)]
        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object? state,
             uint millisecondsTimeOutInterval,
             bool executeOnlyOnce    // NOTE: we do not allow other options that allow the callback to be queued as an APC
             )
        {
            if (millisecondsTimeOutInterval > (uint)int.MaxValue && millisecondsTimeOutInterval != uint.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeOutInterval), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            return RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, false);
        }

        public static RegisteredWaitHandle RegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object? state,
             int millisecondsTimeOutInterval,
             bool executeOnlyOnce    // NOTE: we do not allow other options that allow the callback to be queued as an APC
             )
        {
            if (millisecondsTimeOutInterval < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeOutInterval), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, true);
        }

        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(
             WaitHandle waitObject,
             WaitOrTimerCallback callBack,
             object? state,
             int millisecondsTimeOutInterval,
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
            object? state,
            long millisecondsTimeOutInterval,
            bool executeOnlyOnce    // NOTE: we do not allow other options that allow the callback to be queued as an APC
        )
        {
            if (millisecondsTimeOutInterval < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeOutInterval), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (millisecondsTimeOutInterval > (uint)int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeOutInterval), SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
            return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, true);
        }

        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(
            WaitHandle waitObject,
            WaitOrTimerCallback callBack,
            object? state,
            long millisecondsTimeOutInterval,
            bool executeOnlyOnce    // NOTE: we do not allow other options that allow the callback to be queued as an APC
        )
        {
            if (millisecondsTimeOutInterval < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeOutInterval), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            if (millisecondsTimeOutInterval > (uint)int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeOutInterval), SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
            return RegisterWaitForSingleObject(waitObject, callBack, state, (uint)millisecondsTimeOutInterval, executeOnlyOnce, false);
        }

        public static RegisteredWaitHandle RegisterWaitForSingleObject(
                          WaitHandle waitObject,
                          WaitOrTimerCallback callBack,
                          object? state,
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
                          object? state,
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

        public static bool QueueUserWorkItem(WaitCallback callBack, object? state)
        {
            if (callBack == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
            }

            EnsureInitialized();

            ExecutionContext? context = ExecutionContext.Capture();

            object tpcallBack = (context == null || context.IsDefault) ?
                new QueueUserWorkItemCallbackDefaultContext(callBack!, state) :
                (object)new QueueUserWorkItemCallback(callBack!, state, context);

            ThreadPoolGlobals.workQueue.Enqueue(tpcallBack, forceGlobal: true);

            return true;
        }

        public static bool QueueUserWorkItem<TState>(Action<TState> callBack, TState state, bool preferLocal)
        {
            if (callBack == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
            }

            EnsureInitialized();

            ExecutionContext? context = ExecutionContext.Capture();

            object tpcallBack = (context == null || context.IsDefault) ?
                new QueueUserWorkItemCallbackDefaultContext<TState>(callBack!, state) :
                (object)new QueueUserWorkItemCallback<TState>(callBack!, state, context);

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

                UnsafeQueueUserWorkItemInternal((object)state!, preferLocal);
                return true;
            }

            EnsureInitialized();

            ThreadPoolGlobals.workQueue.Enqueue(
                new QueueUserWorkItemCallbackDefaultContext<TState>(callBack!, state), forceGlobal: !preferLocal);

            return true;
        }

        public static bool UnsafeQueueUserWorkItem(WaitCallback callBack, object? state)
        {
            if (callBack == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.callBack);
            }

            EnsureInitialized();

            object tpcallBack = new QueueUserWorkItemCallbackDefaultContext(callBack!, state);

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

            UnsafeQueueUserWorkItemInternal(callBack!, preferLocal);
            return true;
        }

        internal static void UnsafeQueueUserWorkItemInternal(object callBack, bool preferLocal)
        {
            Debug.Assert((callBack is IThreadPoolWorkItem) ^ (callBack is Task));

            EnsureInitialized();

            ThreadPoolGlobals.workQueue.Enqueue(callBack, forceGlobal: !preferLocal);
        }

        // This method tries to take the target callback out of the current thread's queue.
        internal static bool TryPopCustomWorkItem(object workItem)
        {
            Debug.Assert(null != workItem);
            return
                ThreadPoolGlobals.threadPoolInitialized && // if not initialized, so there's no way this workitem was ever queued.
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
                    object?[] items = wsq.m_array;
                    for (int i = 0; i < items.Length; i++)
                    {
                        object? item = items[i];
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
                object?[] items = wsq.m_array;
                for (int i = 0; i < items.Length; i++)
                {
                    object? item = items[i];
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

        /// <summary>
        /// Gets the number of work items that are currently queued to be processed.
        /// </summary>
        /// <remarks>
        /// For a thread pool implementation that may have different types of work items, the count includes all types that can
        /// be tracked, which may only be the user work items including tasks. Some implementations may also include queued
        /// timer and wait callbacks in the count. On Windows, the count is unlikely to include the number of pending IO
        /// completions, as they get posted directly to an IO completion port.
        /// </remarks>
        public static long PendingWorkItemCount
        {
            get
            {
                ThreadPoolWorkQueue workQueue = ThreadPoolGlobals.workQueue;
                return workQueue.LocalCount + workQueue.GlobalCount + PendingUnmanagedWorkItemCount;
            }
        }
    }
}
