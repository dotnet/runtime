// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    internal static partial class WaitSubsystem
    {
        /// <summary>
        /// Contains thread-specific information for the wait subsystem. There is one instance per thread that is registered
        /// using <see cref="WaitedListNode"/>s with each <see cref="WaitableObject"/> that the thread is waiting upon.
        ///
        /// Used by the wait subsystem on Unix, so this class cannot have any dependencies on the wait subsystem.
        /// </summary>
        public sealed class ThreadWaitInfo
        {
            private readonly Thread _thread;

            /// <summary>
            /// The monitor the thread would wait upon when the wait needs to be interruptible
            /// </summary>
            private LowLevelMonitor _waitMonitor;


            /// <summary>
            /// Thread wait state. The following members indicate the waiting state of the thread, and convery information from
            /// a signaler to the waiter. They are synchronized with <see cref="_waitMonitor"/>.
            /// </summary>
            private WaitSignalState _waitSignalState;

            /// <summary>
            /// Index of the waitable object in <see cref="_waitedObjects"/>, which got signaled and satisfied the wait. -1 if
            /// the wait has not yet been satisfied.
            /// </summary>
            private int _waitedObjectIndexThatSatisfiedWait;

            /// <summary>
            /// Information about the current wait, including the type of wait, the <see cref="WaitableObject"/>s involved in
            /// the wait, etc. They are synchronized with <see cref="s_lock"/>.
            /// </summary>
            private bool _isWaitForAll;

            /// <summary>
            /// Number of <see cref="WaitableObject"/>s the thread is waiting upon
            /// </summary>
            private int _waitedCount;

            /// <summary>
            /// - <see cref="WaitableObject"/>s that are waited upon by the thread. This array is also used for temporarily
            ///   storing <see cref="WaitableObject"/>s corresponding to <see cref="WaitHandle"/>s when the thread is not
            ///   waiting.
            /// - The filled count is <see cref="_waitedCount"/>
            /// - Indexes in all arrays that use <see cref="_waitedCount"/> correspond
            /// </summary>
            private WaitableObject?[] _waitedObjects;

            /// <summary>
            /// - Nodes used for registering a thread's wait on each <see cref="WaitableObject"/>, in the
            ///   <see cref="WaitableObject.WaitersHead"/> linked list
            /// - The filled count is <see cref="_waitedCount"/>
            /// - Indexes in all arrays that use <see cref="_waitedCount"/> correspond
            /// </summary>
            private WaitedListNode[] _waitedListNodes;

            /// <summary>
            /// Indicates whether the next wait should be interrupted.
            ///
            /// Synchronization:
            /// - In most cases, reads and writes are synchronized with <see cref="s_lock"/>
            /// - Sleep(nonzero) intentionally does not acquire <see cref="s_lock"/>, but it must acquire
            ///   <see cref="_waitMonitor"/> to do the wait. To support this case, a pending interrupt is recorded while
            ///   <see cref="s_lock"/> and <see cref="_waitMonitor"/> are locked, and the read and reset for Sleep(nonzero) are
            ///   done while <see cref="_waitMonitor"/> is locked.
            /// - Sleep(0) intentionally does not acquire any lock, so it uses an interlocked compare-exchange for the read and
            ///   reset, see <see cref="CheckAndResetPendingInterrupt_NotLocked"/>
            /// </summary>
            private int _isPendingInterrupt;

            ////////////////////////////////////////////////////////////////

            /// <summary>
            /// Linked list of mutex <see cref="WaitableObject"/>s that are owned by the thread and need to be abandoned before
            /// the thread exits. The linked list has only a head and no tail, which means acquired mutexes are prepended and
            /// mutexes are abandoned in reverse order.
            /// </summary>
            private WaitableObject? _lockedMutexesHead;

            public ThreadWaitInfo(Thread thread)
            {
                Debug.Assert(thread != null);

                _thread = thread;
                _waitMonitor.Initialize();
                _waitSignalState = WaitSignalState.NotWaiting;
                _waitedObjectIndexThatSatisfiedWait = -1;

                // Preallocate to make waiting for single handle fault-free
                _waitedObjects = new WaitableObject[1];
                _waitedListNodes = new WaitedListNode[1] { new WaitedListNode(this, 0) };
            }

            ~ThreadWaitInfo()
            {
                _waitMonitor.Dispose();
            }

            public Thread Thread => _thread;

            private bool IsWaiting
            {
                get
                {
                    _waitMonitor.VerifyIsLocked();
                    return _waitSignalState < WaitSignalState.NotWaiting;
                }
            }

            /// <summary>
            /// Callers must ensure to clear the array after use. Once <see cref="RegisterWait(int, bool, bool)"/> is called (followed
            /// by a call to <see cref="Wait(int, bool, bool)"/>, the array will be cleared automatically.
            /// </summary>
            public WaitableObject?[] GetWaitedObjectArray(int requiredCapacity)
            {
                Debug.Assert(_thread == Thread.CurrentThread);
                Debug.Assert(_waitedCount == 0);

#if DEBUG
                for (int i = 0; i < _waitedObjects.Length; ++i)
                {
                    Debug.Assert(_waitedObjects[i] == null);
                }
#endif

                int currentLength = _waitedObjects.Length;
                if (currentLength < requiredCapacity)
                    _waitedObjects = new WaitableObject[Math.Max(requiredCapacity,
                        Math.Min(WaitHandle.MaxWaitHandles, 2 * currentLength))];

                return _waitedObjects;
            }

            private WaitedListNode[] GetWaitedListNodeArray(int requiredCapacity)
            {
                Debug.Assert(_thread == Thread.CurrentThread);
                Debug.Assert(_waitedCount == 0);

                int currentLength = _waitedListNodes.Length;
                if (currentLength < requiredCapacity)
                {
                    WaitedListNode[] newItems = new WaitedListNode[Math.Max(requiredCapacity,
                        Math.Min(WaitHandle.MaxWaitHandles, 2 * currentLength))];

                    Array.Copy(_waitedListNodes, 0, newItems, 0, currentLength);
                    for (int i = currentLength; i < newItems.Length; i++)
                        newItems[i] = new WaitedListNode(this, i);

                    _waitedListNodes = newItems;
                }

                return _waitedListNodes;
            }

            /// <summary>
            /// The caller is expected to populate <see cref="GetWaitedObjectArray"/> and pass in the number of objects filled
            /// </summary>
            public void RegisterWait(int waitedCount, bool prioritize, bool isWaitForAll)
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(_thread == Thread.CurrentThread);

                Debug.Assert(waitedCount > (isWaitForAll ? 1 : 0));
                Debug.Assert(waitedCount <= _waitedObjects.Length);

                Debug.Assert(_waitedCount == 0);

                WaitableObject?[] waitedObjects = _waitedObjects;
#if DEBUG
                for (int i = 0; i < waitedCount; ++i)
                {
                    Debug.Assert(waitedObjects[i] != null);
                }
                for (int i = waitedCount; i < waitedObjects.Length; ++i)
                {
                    Debug.Assert(waitedObjects[i] == null);
                }
#endif

                bool success = false;
                WaitedListNode[] waitedListNodes;
                try
                {
                    waitedListNodes = GetWaitedListNodeArray(waitedCount);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        // Once this function is called, the caller is effectively transferring ownership of the waited objects
                        // to this and the wait functions. On exception, clear the array.
                        for (int i = 0; i < waitedCount; ++i)
                        {
                            waitedObjects[i] = null;
                        }
                    }
                }

                _isWaitForAll = isWaitForAll;
                _waitedCount = waitedCount;
                if (prioritize)
                {
                    for (int i = 0; i < waitedCount; ++i)
                    {
                        waitedListNodes[i].RegisterPrioritizedWait(waitedObjects[i]!);
                    }
                }
                else
                {
                    for (int i = 0; i < waitedCount; ++i)
                    {
                        waitedListNodes[i].RegisterWait(waitedObjects[i]!);
                    }
                }
            }

            public void UnregisterWait()
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(_waitedCount > (_isWaitForAll ? 1 : 0));

                for (int i = 0; i < _waitedCount; ++i)
                {
                    _waitedListNodes[i].UnregisterWait(_waitedObjects[i]!);
                    _waitedObjects[i] = null;
                }
                _waitedCount = 0;
            }

            private int ProcessSignaledWaitState()
            {
                s_lock.VerifyIsNotLocked();
                _waitMonitor.VerifyIsLocked();
                Debug.Assert(_thread == Thread.CurrentThread);

                switch (_waitSignalState)
                {
                    case WaitSignalState.Waiting:
                    case WaitSignalState.Waiting_Interruptible:
                        return WaitHandle.WaitTimeout;

                    case WaitSignalState.NotWaiting_SignaledToSatisfyWait:
                        {
                            Debug.Assert(_waitedObjectIndexThatSatisfiedWait >= 0);
                            int waitedObjectIndexThatSatisfiedWait = _waitedObjectIndexThatSatisfiedWait;
                            _waitedObjectIndexThatSatisfiedWait = -1;
                            return waitedObjectIndexThatSatisfiedWait;
                        }

                    case WaitSignalState.NotWaiting_SignaledToSatisfyWaitWithAbandonedMutex:
                        {
                            Debug.Assert(_waitedObjectIndexThatSatisfiedWait >= 0);
                            int waitedObjectIndexThatSatisfiedWait = _waitedObjectIndexThatSatisfiedWait;
                            _waitedObjectIndexThatSatisfiedWait = -1;
                            return WaitHandle.WaitAbandoned + waitedObjectIndexThatSatisfiedWait;
                        }

                    case WaitSignalState.NotWaiting_SignaledToAbortWaitDueToMaximumMutexReacquireCount:
                        Debug.Assert(_waitedObjectIndexThatSatisfiedWait < 0);
                        throw new OverflowException(SR.Overflow_MutexReacquireCount);

                    default:
                        Debug.Assert(_waitSignalState == WaitSignalState.NotWaiting_SignaledToInterruptWait);
                        Debug.Assert(_waitedObjectIndexThatSatisfiedWait < 0);
                        throw new ThreadInterruptedException();
                }
            }

            public int Wait(int timeoutMilliseconds, bool interruptible, bool isSleep)
            {
                if (isSleep)
                {
                    s_lock.VerifyIsNotLocked();
                }
                else
                {
                    s_lock.VerifyIsLocked();
                }
                Debug.Assert(_thread == Thread.CurrentThread);

                Debug.Assert(timeoutMilliseconds >= -1);
                Debug.Assert(timeoutMilliseconds != 0); // caller should have taken care of it

                _thread.SetWaitSleepJoinState();

                // <see cref="_waitMonitor"/> must be acquired before <see cref="s_lock"/> is released, to ensure that there is
                // no gap during which a waited object may be signaled to satisfy the wait but the thread may not yet be in a
                // wait state to accept the signal
                _waitMonitor.Acquire();
                if (!isSleep)
                {
                    s_lock.Release();
                }

                Debug.Assert(_waitedObjectIndexThatSatisfiedWait < 0);
                Debug.Assert(_waitSignalState == WaitSignalState.NotWaiting);

                // A signaled state may be set only when the thread is in one of the following states
                _waitSignalState = interruptible ? WaitSignalState.Waiting_Interruptible : WaitSignalState.Waiting;

                try
                {
                    if (isSleep && interruptible && CheckAndResetPendingInterrupt)
                    {
                        throw new ThreadInterruptedException();
                    }

                    if (timeoutMilliseconds < 0)
                    {
                        do
                        {
                            _waitMonitor.Wait();
                        } while (IsWaiting);

                        int waitResult = ProcessSignaledWaitState();
                        Debug.Assert(waitResult != WaitHandle.WaitTimeout);
                        return waitResult;
                    }

                    int elapsedMilliseconds = 0;
                    int startTimeMilliseconds = Environment.TickCount;
                    while (true)
                    {
                        bool monitorWaitResult = _waitMonitor.Wait(timeoutMilliseconds - elapsedMilliseconds);

                        // It's possible for the wait to have timed out, but before the monitor could reacquire the lock, a
                        // signaler could have acquired it and signaled to satisfy the wait or interrupt the thread. Accept the
                        // signal and ignore the wait timeout.
                        int waitResult = ProcessSignaledWaitState();
                        if (waitResult != WaitHandle.WaitTimeout)
                        {
                            return waitResult;
                        }

                        if (monitorWaitResult)
                        {
                            elapsedMilliseconds = Environment.TickCount - startTimeMilliseconds;
                            if (elapsedMilliseconds < timeoutMilliseconds)
                            {
                                continue;
                            }
                        }

                        // Timeout
                        Debug.Assert(_waitedObjectIndexThatSatisfiedWait < 0);
                        break;
                    }
                }
                finally
                {
                    _waitSignalState = WaitSignalState.NotWaiting;
                    _waitMonitor.Release();

                    _thread.ClearWaitSleepJoinState();
                }

                // Timeout. It's ok to read <see cref="_waitedCount"/> without acquiring <see cref="s_lock"/> here, because it
                // is initially set by this thread, and another thread cannot unregister this thread's wait without first
                // signaling this thread, in which case this thread wouldn't be timing out.
                Debug.Assert(isSleep == (_waitedCount == 0));
                if (!isSleep)
                {
                    s_lock.Acquire();
                    UnregisterWait();
                    s_lock.Release();
                }
                return WaitHandle.WaitTimeout;
            }

            public static void UninterruptibleSleep0()
            {
                s_lock.VerifyIsNotLocked();

                // On Unix, a thread waits on a condition variable. The timeout time will have already elapsed at the time
                // of the call. The documentation does not state whether the thread yields or does nothing before returning
                // an error, and in some cases, suggests that doing nothing is acceptable. The behavior could also be
                // different between distributions. Yield directly here.
                Thread.Yield();
            }

            public static void Sleep(int timeoutMilliseconds, bool interruptible)
            {
                s_lock.VerifyIsNotLocked();
                Debug.Assert(timeoutMilliseconds >= -1);

                if (timeoutMilliseconds == 0)
                {
                    if (interruptible && Thread.CurrentThread.WaitInfo.CheckAndResetPendingInterrupt_NotLocked)
                    {
                        throw new ThreadInterruptedException();
                    }

                    UninterruptibleSleep0();
                    return;
                }

                int waitResult =
                    Thread
                        .CurrentThread
                        .WaitInfo
                        .Wait(timeoutMilliseconds, interruptible, isSleep: true);
                Debug.Assert(waitResult == WaitHandle.WaitTimeout);
            }

            public bool TrySignalToSatisfyWait(WaitedListNode registeredListNode, bool isAbandonedMutex)
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(_thread != Thread.CurrentThread);

                Debug.Assert(registeredListNode != null);
                Debug.Assert(registeredListNode.WaitInfo == this);
                Debug.Assert(registeredListNode.WaitedObjectIndex >= 0);
                Debug.Assert(registeredListNode.WaitedObjectIndex < _waitedCount);

                Debug.Assert(_waitedCount > (_isWaitForAll ? 1 : 0));

                int signaledWaitedObjectIndex = registeredListNode.WaitedObjectIndex;
                bool isWaitForAll = _isWaitForAll;
                bool wouldAnyMutexReacquireCountOverflow = false;
                if (isWaitForAll)
                {
                    // Determine if all waits would be satisfied
                    if (!WaitableObject.WouldWaitForAllBeSatisfiedOrAborted(
                            _thread,
                            _waitedObjects,
                            _waitedCount,
                            signaledWaitedObjectIndex,
                            ref wouldAnyMutexReacquireCountOverflow,
                            ref isAbandonedMutex))
                    {
                        return false;
                    }
                }

                // The wait would be satisfied. Before making changes to satisfy the wait, acquire the monitor and verify that
                // the thread can accept a signal.
                _waitMonitor.Acquire();

                if (!IsWaiting)
                {
                    _waitMonitor.Release();
                    return false;
                }

                if (isWaitForAll && !wouldAnyMutexReacquireCountOverflow)
                {
                    // All waits would be satisfied, accept the signals
                    WaitableObject.SatisfyWaitForAll(this, _waitedObjects, _waitedCount, signaledWaitedObjectIndex);
                }

                UnregisterWait();

                Debug.Assert(_waitedObjectIndexThatSatisfiedWait < 0);
                if (wouldAnyMutexReacquireCountOverflow)
                {
                    _waitSignalState = WaitSignalState.NotWaiting_SignaledToAbortWaitDueToMaximumMutexReacquireCount;
                }
                else
                {
                    _waitedObjectIndexThatSatisfiedWait = signaledWaitedObjectIndex;
                    _waitSignalState =
                        isAbandonedMutex
                            ? WaitSignalState.NotWaiting_SignaledToSatisfyWaitWithAbandonedMutex
                            : WaitSignalState.NotWaiting_SignaledToSatisfyWait;
                }

                _waitMonitor.Signal_Release();
                return !wouldAnyMutexReacquireCountOverflow;
            }

            public void TrySignalToInterruptWaitOrRecordPendingInterrupt()
            {
                s_lock.VerifyIsLocked();

                _waitMonitor.Acquire();

                if (_waitSignalState != WaitSignalState.Waiting_Interruptible)
                {
                    RecordPendingInterrupt();
                    _waitMonitor.Release();
                    return;
                }

                if (_waitedCount != 0)
                {
                    UnregisterWait();
                }

                Debug.Assert(_waitedObjectIndexThatSatisfiedWait < 0);
                _waitSignalState = WaitSignalState.NotWaiting_SignaledToInterruptWait;

                _waitMonitor.Signal_Release();
            }

            private void RecordPendingInterrupt()
            {
                s_lock.VerifyIsLocked();
                _waitMonitor.VerifyIsLocked();

                _isPendingInterrupt = 1;
            }

            public bool CheckAndResetPendingInterrupt
            {
                get
                {
#if DEBUG
                    Debug.Assert(s_lock.IsLocked || _waitMonitor.IsLocked);
#endif

                    if (_isPendingInterrupt == 0)
                    {
                        return false;
                    }
                    _isPendingInterrupt = 0;
                    return true;
                }
            }

            private bool CheckAndResetPendingInterrupt_NotLocked
            {
                get
                {
                    s_lock.VerifyIsNotLocked();
                    _waitMonitor.VerifyIsNotLocked();

                    return Interlocked.CompareExchange(ref _isPendingInterrupt, 0, 1) != 0;
                }
            }

            public WaitableObject? LockedMutexesHead
            {
                get
                {
                    s_lock.VerifyIsLocked();
                    return _lockedMutexesHead;
                }
                set
                {
                    s_lock.VerifyIsLocked();
                    _lockedMutexesHead = value;
                }
            }

            public void OnThreadExiting()
            {
                // Abandon locked mutexes. Acquired mutexes are prepended to the linked list, so the mutexes are abandoned in
                // last-acquired-first-abandoned order.
                s_lock.Acquire();
                try
                {
                    while (true)
                    {
                        WaitableObject? waitableObject = LockedMutexesHead;
                        if (waitableObject == null)
                        {
                            break;
                        }

                        waitableObject.AbandonMutex();
                        Debug.Assert(LockedMutexesHead != waitableObject);
                    }
                }
                finally
                {
                    s_lock.Release();
                }
            }

            public sealed class WaitedListNode
            {
                /// <summary>
                /// For <see cref="WaitedListNode"/>s registered with <see cref="WaitableObject"/>s, this provides information
                /// about the thread that is waiting and the <see cref="WaitableObject"/>s it is waiting upon
                /// </summary>
                private readonly ThreadWaitInfo _waitInfo;

                /// <summary>
                /// Index of the waited object corresponding to this node
                /// </summary>
                private readonly int _waitedObjectIndex;

                /// <summary>
                /// Link in the <see cref="WaitableObject.WaitersHead"/> linked list
                /// </summary>
                private WaitedListNode? _previous, _next;

                public WaitedListNode(ThreadWaitInfo waitInfo, int waitedObjectIndex)
                {
                    Debug.Assert(waitInfo != null);
                    Debug.Assert(waitedObjectIndex >= 0);
                    Debug.Assert(waitedObjectIndex < WaitHandle.MaxWaitHandles);

                    _waitInfo = waitInfo;
                    _waitedObjectIndex = waitedObjectIndex;
                }

                public ThreadWaitInfo WaitInfo
                {
                    get
                    {
                        s_lock.VerifyIsLocked();
                        return _waitInfo;
                    }
                }

                public int WaitedObjectIndex
                {
                    get
                    {
                        s_lock.VerifyIsLocked();
                        return _waitedObjectIndex;
                    }
                }

                public WaitedListNode? Previous
                {
                    get
                    {
                        s_lock.VerifyIsLocked();
                        return _previous;
                    }
                }

                public WaitedListNode? Next
                {
                    get
                    {
                        s_lock.VerifyIsLocked();
                        return _next;
                    }
                }

                public void RegisterWait(WaitableObject waitableObject)
                {
                    s_lock.VerifyIsLocked();
                    Debug.Assert(_waitInfo.Thread == Thread.CurrentThread);

                    Debug.Assert(waitableObject != null);

                    Debug.Assert(_previous == null);
                    Debug.Assert(_next == null);

                    WaitedListNode? tail = waitableObject.WaitersTail;
                    if (tail != null)
                    {
                        _previous = tail;
                        tail._next = this;
                    }
                    else
                    {
                        waitableObject.WaitersHead = this;
                    }
                    waitableObject.WaitersTail = this;
                }

                public void RegisterPrioritizedWait(WaitableObject waitableObject)
                {
                    s_lock.VerifyIsLocked();
                    Debug.Assert(_waitInfo.Thread == Thread.CurrentThread);

                    Debug.Assert(waitableObject != null);

                    Debug.Assert(_previous == null);
                    Debug.Assert(_next == null);

                    WaitedListNode? head = waitableObject.WaitersHead;
                    if (head != null)
                    {
                        _next = head;
                        head._previous = this;
                    }
                    else
                    {
                        waitableObject.WaitersTail = this;
                    }
                    waitableObject.WaitersHead = this;
                }

                public void UnregisterWait(WaitableObject waitableObject)
                {
                    s_lock.VerifyIsLocked();
                    Debug.Assert(waitableObject != null);

                    WaitedListNode? previous = _previous;
                    WaitedListNode? next = _next;

                    if (previous != null)
                    {
                        previous._next = next;
                        _previous = null;
                    }
                    else
                    {
                        Debug.Assert(waitableObject.WaitersHead == this);
                        waitableObject.WaitersHead = next;
                    }

                    if (next != null)
                    {
                        next._previous = previous;
                        _next = null;
                    }
                    else
                    {
                        Debug.Assert(waitableObject.WaitersTail == this);
                        waitableObject.WaitersTail = previous;
                    }
                }
            }

            private enum WaitSignalState : byte
            {
                Waiting,
                Waiting_Interruptible,
                NotWaiting,
                NotWaiting_SignaledToSatisfyWait,
                NotWaiting_SignaledToSatisfyWaitWithAbandonedMutex,
                NotWaiting_SignaledToAbortWaitDueToMaximumMutexReacquireCount,
                NotWaiting_SignaledToInterruptWait
            }
        }
    }
}
