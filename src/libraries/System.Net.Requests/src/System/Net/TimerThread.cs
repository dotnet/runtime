// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net
{
    /// <summary>
    /// <para>Acts as countdown timer, used to measure elapsed time over a sync operation.</para>
    /// </summary>
    internal static class TimerThread
    {
        /// <summary>
        /// <para>Represents a queue of timers, which all have the same duration.</para>
        /// </summary>
        internal abstract class Queue
        {
            private readonly int _durationMilliseconds;

            internal Queue(int durationMilliseconds)
            {
                _durationMilliseconds = durationMilliseconds;
            }

            /// <summary>
            /// <para>The duration in milliseconds of timers in this queue.</para>
            /// </summary>
            internal int Duration => _durationMilliseconds;

            /// <summary>
            /// <para>Creates and returns a handle to a new timer with attached context.</para>
            /// </summary>
            internal abstract Timer CreateTimer(Callback callback, object? context);
        }

        /// <summary>
        /// <para>Represents a timer and provides a mechanism to cancel.</para>
        /// </summary>
        internal abstract class Timer : IDisposable
        {
            private readonly int _startTimeMilliseconds;
            private readonly int _durationMilliseconds;

            internal Timer(int durationMilliseconds)
            {
                _durationMilliseconds = durationMilliseconds;
                _startTimeMilliseconds = Environment.TickCount;
            }

            /// <summary>
            /// <para>The time (relative to Environment.TickCount) when the timer started.</para>
            /// </summary>
            internal int StartTime => _startTimeMilliseconds;

            /// <summary>
            /// <para>The time (relative to Environment.TickCount) when the timer will expire.</para>
            /// </summary>
            internal int Expiration => unchecked(_startTimeMilliseconds + _durationMilliseconds);

            /// <summary>
            /// <para>Cancels the timer.  Returns true if the timer hasn't and won't fire; false if it has or will.</para>
            /// </summary>
            internal abstract bool Cancel();

            /// <summary>
            /// <para>Whether or not the timer has expired.</para>
            /// </summary>
            internal abstract bool HasExpired { get; }

            public void Dispose() => Cancel();
        }

        /// <summary>
        /// <para>Prototype for the callback that is called when a timer expires.</para>
        /// </summary>
        internal delegate void Callback(Timer timer, int timeNoticed, object? context);

        private const int ThreadIdleTimeoutMilliseconds = 30 * 1000;
        private const int CacheScanPerIterations = 32;
        private const int TickCountResolution = 15;

        private static readonly LinkedList<WeakReference> s_queues = new LinkedList<WeakReference>();
        private static readonly LinkedList<WeakReference> s_newQueues = new LinkedList<WeakReference>();
        private static int s_threadState = (int)TimerThreadState.Idle;  // Really a TimerThreadState, but need an int for Interlocked.
        private static readonly AutoResetEvent s_threadReadyEvent = new AutoResetEvent(false);
        private static readonly ManualResetEvent s_threadShutdownEvent = new ManualResetEvent(false);
        private static readonly WaitHandle[] s_threadEvents = { s_threadShutdownEvent, s_threadReadyEvent };
        private static int s_cacheScanIteration;
        private static readonly Hashtable s_queuesCache = new Hashtable();

        /// <summary>
        /// <para>The possible states of the timer thread.</para>
        /// </summary>
        private enum TimerThreadState
        {
            Idle,
            Running,
            Stopped
        }

        /// <summary>
        /// <para>Queue factory.  Always synchronized.</para>
        /// </summary>
        internal static Queue GetOrCreateQueue(int durationMilliseconds)
        {
            if (durationMilliseconds == Timeout.Infinite)
            {
                return new InfiniteTimerQueue();
            }

            if (durationMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
            }

            TimerQueue? queue;
            object key = durationMilliseconds; // Box once.
            WeakReference? weakQueue = (WeakReference?)s_queuesCache[key];
            if (weakQueue == null || (queue = (TimerQueue?)weakQueue.Target) == null)
            {
                lock (s_newQueues)
                {
                    weakQueue = (WeakReference?)s_queuesCache[key];
                    if (weakQueue == null || (queue = (TimerQueue?)weakQueue.Target) == null)
                    {
                        queue = new TimerQueue(durationMilliseconds);
                        weakQueue = new WeakReference(queue);
                        s_newQueues.AddLast(weakQueue);
                        s_queuesCache[key] = weakQueue;

                        // Take advantage of this lock to periodically scan the table for garbage.
                        if (++s_cacheScanIteration % CacheScanPerIterations == 0)
                        {
                            var garbage = new List<object>();
                            // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                            IDictionaryEnumerator e = s_queuesCache.GetEnumerator();
                            while (e.MoveNext())
                            {
                                DictionaryEntry pair = e.Entry;
                                if (((WeakReference)pair.Value!).Target == null)
                                {
                                    garbage.Add(pair.Key);
                                }
                            }
                            for (int i = 0; i < garbage.Count; i++)
                            {
                                s_queuesCache.Remove(garbage[i]);
                            }
                        }
                    }
                }
            }

            return queue;
        }

        /// <summary>
        /// <para>Represents a queue of timers of fixed duration.</para>
        /// </summary>
        private sealed class TimerQueue : Queue
        {
            // This is a GCHandle that holds onto the TimerQueue when active timers are in it.
            // The TimerThread only holds WeakReferences to it so that it can be collected when the user lets go of it.
            // But we don't want the user to HAVE to keep a reference to it when timers are active in it.
            // It gets created when the first timer gets added, and cleaned up when the TimerThread notices it's empty.
            // The TimerThread will always notice it's empty eventually, since the TimerThread will always wake up and
            // try to fire the timer, even if it was cancelled and removed prematurely.
            private IntPtr _thisHandle;

            // This sentinel TimerNode acts as both the head and the tail, allowing nodes to go in and out of the list without updating
            // any TimerQueue members.  _timers.Next is the true head, and .Prev the true tail.  This also serves as the list's lock.
            private readonly TimerNode _timers;

            /// <summary>
            /// <para>Create a new TimerQueue.  TimerQueues must be created while s_NewQueues is locked in
            /// order to synchronize with Shutdown().</para>
            /// </summary>
            /// <param name="durationMilliseconds"></param>
            internal TimerQueue(int durationMilliseconds) :
                base(durationMilliseconds)
            {
                // Create the doubly-linked list with a sentinel head and tail so that this member never needs updating.
                _timers = new TimerNode();
                _timers.Next = _timers;
                _timers.Prev = _timers;
            }

            /// <summary>
            /// <para>Creates new timers.  This method is thread-safe.</para>
            /// </summary>
            internal override Timer CreateTimer(Callback callback, object? context)
            {
                TimerNode timer = new TimerNode(callback, context, Duration, _timers);

                // Add this on the tail.  (Actually, one before the tail - _timers is the sentinel tail.)
                bool needProd = false;
                lock (_timers)
                {
                    Debug.Assert(_timers.Prev!.Next == _timers, $"Tail corruption.");

                    // If this is the first timer in the list, we need to create a queue handle and prod the timer thread.
                    if (_timers.Next == _timers)
                    {
                        if (_thisHandle == IntPtr.Zero)
                        {
                            _thisHandle = (IntPtr)GCHandle.Alloc(this);
                        }
                        needProd = true;
                    }

                    timer.Next = _timers;
                    timer.Prev = _timers.Prev;
                    _timers.Prev.Next = timer;
                    _timers.Prev = timer;
                }

                // If, after we add the new tail, there is a chance that the tail is the next
                // node to be processed, we need to wake up the timer thread.
                if (needProd)
                {
                    TimerThread.Prod();
                }

                return timer;
            }

            /// <summary>
            /// <para>Called by the timer thread to fire the expired timers.  Returns true if there are future timers
            /// in the queue, and if so, also sets nextExpiration.</para>
            /// </summary>
            internal bool Fire(out int nextExpiration)
            {
                while (true)
                {
                    // Check if we got to the end.  If so, free the handle.
                    TimerNode timer = _timers.Next!;
                    if (timer == _timers)
                    {
                        lock (_timers)
                        {
                            timer = _timers.Next!;
                            if (timer == _timers)
                            {
                                if (_thisHandle != IntPtr.Zero)
                                {
                                    ((GCHandle)_thisHandle).Free();
                                    _thisHandle = IntPtr.Zero;
                                }

                                nextExpiration = 0;
                                return false;
                            }
                        }
                    }

                    if (!timer.Fire())
                    {
                        nextExpiration = timer.Expiration;
                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// <para>A special dummy implementation for a queue of timers of infinite duration.</para>
        /// </summary>
        private sealed class InfiniteTimerQueue : Queue
        {
            internal InfiniteTimerQueue() : base(Timeout.Infinite) { }

            /// <summary>
            /// <para>Always returns a dummy infinite timer.</para>
            /// </summary>
            internal override Timer CreateTimer(Callback callback, object? context) => new InfiniteTimer();
        }

        /// <summary>
        /// <para>Internal representation of an individual timer.</para>
        /// </summary>
        private sealed class TimerNode : Timer
        {
            private TimerState _timerState;
            private Callback? _callback;
            private object? _context;
            private readonly object _queueLock = null!;
            private TimerNode? _next;
            private TimerNode? _prev;

            /// <summary>
            /// <para>Status of the timer.</para>
            /// </summary>
            private enum TimerState
            {
                Ready,
                Fired,
                Cancelled,
                Sentinel
            }

            internal TimerNode(Callback callback, object? context, int durationMilliseconds, object queueLock) : base(durationMilliseconds)
            {
                if (callback != null)
                {
                    _callback = callback;
                    _context = context;
                }
                _timerState = TimerState.Ready;
                _queueLock = queueLock;
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"TimerThreadTimer#{StartTime}");
            }

            // A sentinel node - both the head and tail are one, which prevent the head and tail from ever having to be updated.
            internal TimerNode() : base(0)
            {
                _timerState = TimerState.Sentinel;
            }

            internal override bool HasExpired => _timerState == TimerState.Fired;

            internal TimerNode? Next
            {
                get { return _next; }
                set { _next = value; }
            }

            internal TimerNode? Prev
            {
                get { return _prev; }
                set { _prev = value; }
            }

            /// <summary>
            /// <para>Cancels the timer.  Returns true if it hasn't and won't fire; false if it has or will, or has already been cancelled.</para>
            /// </summary>
            internal override bool Cancel()
            {
                if (_timerState == TimerState.Ready)
                {
                    lock (_queueLock)
                    {
                        if (_timerState == TimerState.Ready)
                        {
                            // Remove it from the list.  This keeps the list from getting too big when there are a lot of rapid creations
                            // and cancellations.  This is done before setting it to Cancelled to try to prevent the Fire() loop from
                            // seeing it, or if it does, of having to take a lock to synchronize with the state of the list.
                            Next!.Prev = Prev;
                            Prev!.Next = Next;

                            // Just cleanup.  Doesn't need to be in the lock but is easier to have here.
                            Next = null;
                            Prev = null;
                            _callback = null;
                            _context = null;

                            _timerState = TimerState.Cancelled;

                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"TimerThreadTimer#{StartTime} Cancel (success)");
                            return true;
                        }
                    }
                }

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"TimerThreadTimer#{StartTime} Cancel (failure)");
                return false;
            }

            /// <summary>
            /// <para>Fires the timer if it is still active and has expired.  Returns
            /// true if it can be deleted, or false if it is still timing.</para>
            /// </summary>
            internal bool Fire()
            {
                if (_timerState == TimerState.Sentinel)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "TimerQueue tried to Fire a Sentinel.");
                }

                if (_timerState != TimerState.Ready)
                {
                    return true;
                }

                // Must get the current tick count within this method so it is guaranteed not to be before
                // StartTime, which is set in the constructor.
                int nowMilliseconds = Environment.TickCount;
                if (IsTickBetween(StartTime, Expiration, nowMilliseconds))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"TimerThreadTimer#{StartTime}::Fire() Not firing ({StartTime} <= {nowMilliseconds} < {Expiration})");
                    return false;
                }

                bool needCallback = false;
                lock (_queueLock)
                {
                    if (_timerState == TimerState.Ready)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"TimerThreadTimer#{StartTime}::Fire() Firing ({StartTime} <= {nowMilliseconds} >= " + Expiration + ")");
                        _timerState = TimerState.Fired;

                        // Remove it from the list.
                        Next!.Prev = Prev;
                        Prev!.Next = Next;

                        Next = null;
                        Prev = null;
                        needCallback = _callback != null;
                    }
                }

                if (needCallback)
                {
                    try
                    {
                        Callback callback = _callback!;
                        object? context = _context;
                        _callback = null;
                        _context = null;
                        callback(this, nowMilliseconds, context);
                    }
                    catch (Exception exception)
                    {
                        if (ExceptionCheck.IsFatal(exception))
                            throw;

                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"exception in callback: {exception}");

                        // This thread is not allowed to go into user code, so we should never get an exception here.
                        // So, in debug, throw it up, killing the AppDomain.  In release, we'll just ignore it.
#if DEBUG
                        throw;
#endif
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// <para>A dummy infinite timer.</para>
        /// </summary>
        private sealed class InfiniteTimer : Timer
        {
            internal InfiniteTimer() : base(Timeout.Infinite) { }

            private int _cancelled;

            internal override bool HasExpired => false;

            /// <summary>
            /// <para>Cancels the timer.  Returns true the first time, false after that.</para>
            /// </summary>
            internal override bool Cancel() => Interlocked.Exchange(ref _cancelled, 1) == 0;
        }

        /// <summary>
        /// <para>Internal mechanism used when timers are added to wake up / create the thread.</para>
        /// </summary>
        private static void Prod()
        {
            s_threadReadyEvent.Set();
            TimerThreadState oldState = (TimerThreadState)Interlocked.CompareExchange(
                ref s_threadState,
                (int)TimerThreadState.Running,
                (int)TimerThreadState.Idle);

            if (oldState == TimerThreadState.Idle)
            {
                new Thread(new ThreadStart(ThreadProc))
                {
                    IsBackground = true,
                    Name = ".NET Network Timer"
                }.Start();
            }
        }

        /// <summary>
        /// <para>Thread for the timer.  Ignores all exceptions.  If no activity occurs for a while,
        /// the thread will shut down.</para>
        /// </summary>
        private static void ThreadProc()
        {
            // Keep a permanent lock on s_Queues.  This lets for example Shutdown() know when this thread isn't running.
            lock (s_queues)
            {
                // If shutdown was recently called, abort here.
                if (Interlocked.CompareExchange(ref s_threadState, (int)TimerThreadState.Running, (int)TimerThreadState.Running) !=
                    (int)TimerThreadState.Running)
                {
                    return;
                }

                bool running = true;
                while (running)
                {
                    try
                    {
                        s_threadReadyEvent.Reset();

                        while (true)
                        {
                            // Copy all the new queues to the real queues.  Since only this thread modifies the real queues, it doesn't have to lock it.
                            if (s_newQueues.Count > 0)
                            {
                                lock (s_newQueues)
                                {
                                    for (LinkedListNode<WeakReference>? node = s_newQueues.First; node != null; node = s_newQueues.First)
                                    {
                                        s_newQueues.Remove(node);
                                        s_queues.AddLast(node);
                                    }
                                }
                            }

                            int now = Environment.TickCount;
                            int nextTick = 0;
                            bool haveNextTick = false;
                            for (LinkedListNode<WeakReference>? node = s_queues.First; node != null; /* node = node.Next must be done in the body */)
                            {
                                TimerQueue? queue = (TimerQueue?)node.Value.Target;
                                if (queue == null)
                                {
                                    LinkedListNode<WeakReference>? next = node.Next;
                                    s_queues.Remove(node);
                                    node = next;
                                    continue;
                                }

                                // Fire() will always return values that should be interpreted as later than 'now' (that is, even if 'now' is
                                // returned, it is 0x100000000 milliseconds in the future).  There's also a chance that Fire() will return a value
                                // intended as > 0x100000000 milliseconds from 'now'.  Either case will just cause an extra scan through the timers.
                                int nextTickInstance;
                                if (queue.Fire(out nextTickInstance) && (!haveNextTick || IsTickBetween(now, nextTick, nextTickInstance)))
                                {
                                    nextTick = nextTickInstance;
                                    haveNextTick = true;
                                }

                                node = node.Next;
                            }

                            // Figure out how long to wait, taking into account how long the loop took.
                            // Add 15 ms to compensate for poor TickCount resolution (want to guarantee a firing).
                            int newNow = Environment.TickCount;
                            int waitDuration = haveNextTick ?
                                (int)(IsTickBetween(now, nextTick, newNow) ?
                                    Math.Min(unchecked((uint)(nextTick - newNow)), (uint)(int.MaxValue - TickCountResolution)) + TickCountResolution :
                                    0) :
                                ThreadIdleTimeoutMilliseconds;

                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Waiting for {waitDuration}ms");

                            int waitResult = WaitHandle.WaitAny(s_threadEvents, waitDuration, false);

                            // 0 is s_ThreadShutdownEvent - die.
                            if (waitResult == 0)
                            {
                                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "Awoke, cause: Shutdown");
                                running = false;
                                break;
                            }

                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Awoke, cause {(waitResult == WaitHandle.WaitTimeout ? "Timeout" : "Prod")}");

                            // If we timed out with nothing to do, shut down.
                            if (waitResult == WaitHandle.WaitTimeout && !haveNextTick)
                            {
                                Interlocked.CompareExchange(ref s_threadState, (int)TimerThreadState.Idle, (int)TimerThreadState.Running);
                                // There could have been one more prod between the wait and the exchange.  Check, and abort if necessary.
                                if (s_threadReadyEvent.WaitOne(0, false))
                                {
                                    if (Interlocked.CompareExchange(ref s_threadState, (int)TimerThreadState.Running, (int)TimerThreadState.Idle) ==
                                        (int)TimerThreadState.Idle)
                                    {
                                        continue;
                                    }
                                }

                                running = false;
                                break;
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (ExceptionCheck.IsFatal(exception))
                            throw;

                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, exception);

                        // The only options are to continue processing and likely enter an error-loop,
                        // shut down timers for this AppDomain, or shut down the AppDomain.  Go with shutting
                        // down the AppDomain in debug, and going into a loop in retail, but try to make the
                        // loop somewhat slow.  Note that in retail, this can only be triggered by OutOfMemory or StackOverflow,
                        // or an exception thrown within TimerThread - the rest are caught in Fire().
#if !DEBUG
                        Thread.Sleep(1000);
#else
                        throw;
#endif
                    }
                }
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, "Stop");
        }

        /// <summary>
        /// <para>Helper for deciding whether a given TickCount is before or after a given expiration
        /// tick count assuming that it can't be before a given starting TickCount.</para>
        /// </summary>
        private static bool IsTickBetween(int start, int end, int comparand)
        {
            // Assumes that if start and end are equal, they are the same time.
            // Assumes that if the comparand and start are equal, no time has passed,
            // and that if the comparand and end are equal, end has occurred.
            return ((start <= comparand) == (end <= comparand)) != (start <= end);
        }
    }
}
