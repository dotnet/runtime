// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    internal partial class PortableThreadPool
    {
        /// <summary>
        /// A linked list of <see cref="WaitThread"/>s.
        /// </summary>
        private WaitThreadNode? _waitThreadsHead;

        private readonly LowLevelLock _waitThreadLock = new LowLevelLock();

        /// <summary>
        /// Register a wait handle on a <see cref="WaitThread"/>.
        /// </summary>
        /// <param name="handle">A description of the requested registration.</param>
        internal void RegisterWaitHandle(RegisteredWaitHandle handle)
        {
            _waitThreadLock.Acquire();
            try
            {
                if (_waitThreadsHead == null) // Lazily create the first wait thread.
                {
                    _waitThreadsHead = new WaitThreadNode
                    {
                        Thread = new WaitThread()
                    };
                }

                // Register the wait handle on the first wait thread that is not at capacity.
                WaitThreadNode prev;
                WaitThreadNode? current = _waitThreadsHead;
                do
                {
                    if (current.Thread!.RegisterWaitHandle(handle))
                    {
                        return;
                    }
                    prev = current;
                    current = current.Next;
                } while (current != null);

                // If all wait threads are full, create a new one.
                prev.Next = new WaitThreadNode
                {
                    Thread = new WaitThread()
                };
                prev.Next.Thread.RegisterWaitHandle(handle);
                return;
            }
            finally
            {
                _waitThreadLock.Release();
            }
        }

        /// <summary>
        /// Attempt to remove the given wait thread from the list. It is only removed if there are no user-provided waits on the thread.
        /// </summary>
        /// <param name="thread">The thread to remove.</param>
        /// <returns><c>true</c> if the thread was successfully removed; otherwise, <c>false</c></returns>
        private bool TryRemoveWaitThread(WaitThread thread)
        {
            _waitThreadLock.Acquire();
            try
            {
                if (thread.AnyUserWaits)
                {
                    return false;
                }
                RemoveWaitThread(thread);
            }
            finally
            {
                _waitThreadLock.Release();
            }
            return true;
        }

        /// <summary>
        /// Removes the wait thread from the list.
        /// </summary>
        /// <param name="thread">The wait thread to remove from the list.</param>
        private void RemoveWaitThread(WaitThread thread)
        {
            if (_waitThreadsHead!.Thread == thread)
            {
                _waitThreadsHead = _waitThreadsHead.Next;
                return;
            }

            WaitThreadNode prev;
            WaitThreadNode? current = _waitThreadsHead;

            do
            {
                prev = current;
                current = current.Next;
            } while (current != null && current.Thread != thread);

            Debug.Assert(current != null, "The wait thread to remove was not found in the list of thread pool wait threads.");

            if (current != null)
            {
                prev.Next = current.Next;
            }
        }

        private class WaitThreadNode
        {
            public WaitThread? Thread { get; set; }
            public WaitThreadNode? Next { get; set; }
        }

        /// <summary>
        /// A thread pool wait thread.
        /// </summary>
        internal class WaitThread
        {
            /// <summary>
            /// The info for a completed wait on a specific <see cref="RegisteredWaitHandle"/>.
            /// </summary>
            private struct CompletedWaitHandle
            {
                public CompletedWaitHandle(RegisteredWaitHandle completedHandle, bool timedOut)
                {
                    CompletedHandle = completedHandle;
                    TimedOut = timedOut;
                }

                public RegisteredWaitHandle CompletedHandle { get; }
                public bool TimedOut { get; }
            }

            /// <summary>
            /// The wait handles registered on this wait thread.
            /// </summary>
            private readonly RegisteredWaitHandle[] _registeredWaits = new RegisteredWaitHandle[WaitHandle.MaxWaitHandles - 1];
            /// <summary>
            /// The raw wait handles to wait on.
            /// </summary>
            /// <remarks>
            /// The zeroth element of this array is always <see cref="_changeHandlesEvent"/>.
            /// </remarks>
            private readonly WaitHandle[] _waitHandles = new WaitHandle[WaitHandle.MaxWaitHandles];
            /// <summary>
            /// The number of user-registered waits on this wait thread.
            /// </summary>
            private int _numUserWaits;

            /// <summary>
            /// A list of removals of wait handles that are waiting for the wait thread to process.
            /// </summary>
            private readonly RegisteredWaitHandle?[] _pendingRemoves = new RegisteredWaitHandle[WaitHandle.MaxWaitHandles - 1];
            /// <summary>
            /// The number of pending removals.
            /// </summary>
            private int _numPendingRemoves;

            /// <summary>
            /// An event to notify the wait thread that there are pending adds or removals of wait handles so it needs to wake up.
            /// </summary>
            private readonly AutoResetEvent _changeHandlesEvent = new AutoResetEvent(false);

            internal bool AnyUserWaits => _numUserWaits != 0;

            public WaitThread()
            {
                _waitHandles[0] = _changeHandlesEvent;
                Thread waitThread = new Thread(WaitThreadStart);
                waitThread.IsBackground = true;
                waitThread.Start();
            }

            /// <summary>
            /// The main routine for the wait thread.
            /// </summary>
            private void WaitThreadStart()
            {
                while (true)
                {
                    ProcessRemovals();
                    int numUserWaits = _numUserWaits;
                    int preWaitTimeMs = Environment.TickCount;

                    // Recalculate Timeout
                    int timeoutDurationMs = Timeout.Infinite;
                    if (numUserWaits == 0)
                    {
                        timeoutDurationMs = ThreadPoolThreadTimeoutMs;
                    }
                    else
                    {
                        for (int i = 0; i < numUserWaits; i++)
                        {
                            if (_registeredWaits[i].IsInfiniteTimeout)
                            {
                                continue;
                            }

                            int handleTimeoutDurationMs = _registeredWaits[i].TimeoutTimeMs - preWaitTimeMs;

                            if (timeoutDurationMs == Timeout.Infinite)
                            {
                                timeoutDurationMs = handleTimeoutDurationMs > 0 ? handleTimeoutDurationMs : 0;
                            }
                            else
                            {
                                timeoutDurationMs = Math.Min(handleTimeoutDurationMs > 0 ? handleTimeoutDurationMs : 0, timeoutDurationMs);
                            }

                            if (timeoutDurationMs == 0)
                            {
                                break;
                            }
                        }
                    }

                    int signaledHandleIndex = WaitHandle.WaitAny(new ReadOnlySpan<WaitHandle>(_waitHandles, 0, numUserWaits + 1), timeoutDurationMs);

                    if (signaledHandleIndex == 0) // If we were woken up for a change in our handles, continue.
                    {
                        continue;
                    }

                    RegisteredWaitHandle? signaledHandle = signaledHandleIndex != WaitHandle.WaitTimeout ? _registeredWaits[signaledHandleIndex - 1] : null;

                    if (signaledHandle != null)
                    {
                        QueueWaitCompletion(signaledHandle, false);
                    }
                    else
                    {
                        if (numUserWaits == 0)
                        {
                            if (ThreadPoolInstance.TryRemoveWaitThread(this))
                            {
                                return;
                            }
                        }

                        int elapsedDurationMs = Environment.TickCount - preWaitTimeMs; // Calculate using relative time to ensure we don't have issues with overflow wraparound
                        for (int i = 0; i < numUserWaits; i++)
                        {
                            RegisteredWaitHandle registeredHandle = _registeredWaits[i];
                            int handleTimeoutDurationMs = registeredHandle.TimeoutTimeMs - preWaitTimeMs;
                            if (elapsedDurationMs >= handleTimeoutDurationMs)
                            {
                                QueueWaitCompletion(registeredHandle, true);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Go through the <see cref="_pendingRemoves"/> array and remove those registered wait handles from the <see cref="_registeredWaits"/>
            /// and <see cref="_waitHandles"/> arrays, filling the holes along the way.
            /// </summary>
            private void ProcessRemovals()
            {
                ThreadPoolInstance._waitThreadLock.Acquire();
                try
                {
                    Debug.Assert(_numPendingRemoves >= 0);
                    Debug.Assert(_numPendingRemoves <= _pendingRemoves.Length);
                    Debug.Assert(_numUserWaits >= 0);
                    Debug.Assert(_numUserWaits <= _registeredWaits.Length);
                    Debug.Assert(_numPendingRemoves <= _numUserWaits, $"Num removals {_numPendingRemoves} should be less than or equal to num user waits {_numUserWaits}");

                    if (_numPendingRemoves == 0 || _numUserWaits == 0)
                    {
                        return;
                    }
                    int originalNumUserWaits = _numUserWaits;
                    int originalNumPendingRemoves = _numPendingRemoves;

                    // This is O(N^2), but max(N) = 63 and N will usually be very low
                    for (int i = 0; i < _numPendingRemoves; i++)
                    {
                        for (int j = 0; j < _numUserWaits; j++)
                        {
                            if (_pendingRemoves[i] == _registeredWaits[j])
                            {
                                _registeredWaits[j].OnRemoveWait();
                                _registeredWaits[j] = _registeredWaits[_numUserWaits - 1];
                                _waitHandles[j + 1] = _waitHandles[_numUserWaits];
                                _registeredWaits[_numUserWaits - 1] = null!;
                                _waitHandles[_numUserWaits] = null!;
                                --_numUserWaits;
                                _pendingRemoves[i] = null;
                                break;
                            }
                        }
                        Debug.Assert(_pendingRemoves[i] == null);
                    }
                    _numPendingRemoves = 0;

                    Debug.Assert(originalNumUserWaits - originalNumPendingRemoves == _numUserWaits,
                        $"{originalNumUserWaits} - {originalNumPendingRemoves} == {_numUserWaits}");
                }
                finally
                {
                    ThreadPoolInstance._waitThreadLock.Release();
                }
            }

            /// <summary>
            /// Queue a call to <see cref="CompleteWait(object)"/> on the ThreadPool.
            /// </summary>
            /// <param name="registeredHandle">The handle that completed.</param>
            /// <param name="timedOut">Whether or not the wait timed out.</param>
            private void QueueWaitCompletion(RegisteredWaitHandle registeredHandle, bool timedOut)
            {
                registeredHandle.RequestCallback();
                // If the handle is a repeating handle, set up the next call. Otherwise, remove it from the wait thread.
                if (registeredHandle.Repeating)
                {
                    registeredHandle.RestartTimeout(Environment.TickCount);
                }
                else
                {
                    UnregisterWait(registeredHandle, blocking: false); // We shouldn't block the wait thread on the unregistration.
                }
                ThreadPool.QueueUserWorkItem(CompleteWait, new CompletedWaitHandle(registeredHandle, timedOut));
            }

            /// <summary>
            /// Process the completion of a user-registered wait (call the callback).
            /// </summary>
            /// <param name="state">A <see cref="CompletedWaitHandle"/> object representing the wait completion.</param>
            private void CompleteWait(object? state)
            {
                CompletedWaitHandle handle = (CompletedWaitHandle)state!;
                handle.CompletedHandle.PerformCallback(handle.TimedOut);
            }

            /// <summary>
            /// Register a wait handle on this <see cref="WaitThread"/>.
            /// </summary>
            /// <param name="handle">The handle to register.</param>
            /// <returns>If the handle was successfully registered on this wait thread.</returns>
            public bool RegisterWaitHandle(RegisteredWaitHandle handle)
            {
                ThreadPoolInstance._waitThreadLock.VerifyIsLocked();
                if (_numUserWaits == WaitHandle.MaxWaitHandles - 1)
                {
                    return false;
                }

                _registeredWaits[_numUserWaits] = handle;
                _waitHandles[_numUserWaits + 1] = handle.Handle;
                _numUserWaits++;

                handle.WaitThread = this;

                _changeHandlesEvent.Set();
                return true;
            }

            /// <summary>
            /// Unregisters a wait handle.
            /// </summary>
            /// <param name="handle">The handle to unregister.</param>
            /// <remarks>
            /// As per CoreCLR's behavior, if the user passes in an invalid <see cref="WaitHandle"/>
            /// into <see cref="RegisteredWaitHandle.Unregister(WaitHandle)"/>, then the unregistration of the wait handle is blocking.
            /// Otherwise, the unregistration of the wait handle is queued on the wait thread.
            /// </remarks>
            public void UnregisterWait(RegisteredWaitHandle handle)
            {
                UnregisterWait(handle, true);
            }

            /// <summary>
            /// Unregister a wait handle.
            /// </summary>
            /// <param name="handle">The wait handle to unregister.</param>
            /// <param name="blocking">Should the unregistration block at all.</param>
            private void UnregisterWait(RegisteredWaitHandle handle, bool blocking)
            {
                bool pendingRemoval = false;
                // TODO: Optimization: Try to unregister wait directly if it isn't being waited on.
                ThreadPoolInstance._waitThreadLock.Acquire();
                try
                {
                    // If this handle is not already pending removal and hasn't already been removed
                    if (Array.IndexOf(_registeredWaits, handle) != -1 && Array.IndexOf(_pendingRemoves, handle) == -1)
                    {
                        _pendingRemoves[_numPendingRemoves++] = handle;
                        _changeHandlesEvent.Set(); // Tell the wait thread that there are changes pending.
                        pendingRemoval = true;
                    }
                }
                finally
                {
                    ThreadPoolInstance._waitThreadLock.Release();
                }

                if (blocking)
                {
                    if (handle.IsBlocking)
                    {
                        handle.WaitForCallbacks();
                    }
                    else if (pendingRemoval)
                    {
                        handle.WaitForRemoval();
                    }
                }
            }
        }
    }
}
