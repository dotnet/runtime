// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    /// <summary>
    /// The info for a completed wait on a specific <see cref="RegisteredWaitHandle"/>.
    /// </summary>
    internal sealed partial class CompleteWaitThreadPoolWorkItem : IThreadPoolWorkItem
    {
        private RegisteredWaitHandle _registeredWaitHandle;
        private bool _timedOut;

        public CompleteWaitThreadPoolWorkItem(RegisteredWaitHandle registeredWaitHandle, bool timedOut)
        {
            _registeredWaitHandle = registeredWaitHandle;
            _timedOut = timedOut;
        }
    }

    internal sealed partial class PortableThreadPool
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
            if (NativeRuntimeEventSource.Log.IsEnabled())
            {
                NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(handle);
            }

            _waitThreadLock.Acquire();
            try
            {
                WaitThreadNode? current = _waitThreadsHead ??= new WaitThreadNode(new WaitThread()); // Lazily create the first wait thread.

                // Register the wait handle on the first wait thread that is not at capacity.
                WaitThreadNode prev;
                do
                {
                    if (current.Thread.RegisterWaitHandle(handle))
                    {
                        return;
                    }
                    prev = current;
                    current = current.Next;
                } while (current != null);

                // If all wait threads are full, create a new one.
                prev.Next = new WaitThreadNode(new WaitThread());
                prev.Next.Thread.RegisterWaitHandle(handle);
                return;
            }
            finally
            {
                _waitThreadLock.Release();
            }
        }

        internal static void CompleteWait(RegisteredWaitHandle handle, bool timedOut)
        {
            if (NativeRuntimeEventSource.Log.IsEnabled())
            {
                NativeRuntimeEventSource.Log.ThreadPoolIODequeue(handle);
            }

            handle.PerformCallback(timedOut);
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
            WaitThreadNode? current = _waitThreadsHead!;
            if (current.Thread == thread)
            {
                _waitThreadsHead = current.Next;
                return;
            }

            WaitThreadNode prev;

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

        private sealed class WaitThreadNode
        {
            public WaitThread Thread { get; }
            public WaitThreadNode? Next { get; set; }

            public WaitThreadNode(WaitThread thread) => Thread = thread;
        }

        /// <summary>
        /// A thread pool wait thread.
        /// </summary>
        internal sealed class WaitThread
        {
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
            private readonly SafeWaitHandle[] _waitHandles = new SafeWaitHandle[WaitHandle.MaxWaitHandles];
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
                _waitHandles[0] = _changeHandlesEvent.SafeWaitHandle;

                // Thread pool threads must start in the default execution context without transferring the context, so
                // using UnsafeStart() instead of Start()
                Thread waitThread = new Thread(WaitThreadStart, SmallStackSizeBytes)
                {
                    IsThreadPoolThread = true,
                    IsBackground = true,
                    Name = ".NET ThreadPool Wait"
                };
                waitThread.UnsafeStart();
            }

            /// <summary>
            /// The main routine for the wait thread.
            /// </summary>
            private void WaitThreadStart()
            {
                while (true)
                {
                    // This value is taken inside the lock after processing removals. In this iteration these are the number of
                    // user waits that will be waited upon. Any new waits will wake the wait and the next iteration would
                    // consider them.
                    int numUserWaits = ProcessRemovals();

                    int currentTimeMs = Environment.TickCount;

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
                            RegisteredWaitHandle registeredWait = _registeredWaits[i];
                            Debug.Assert(registeredWait != null);
                            if (registeredWait.IsInfiniteTimeout)
                            {
                                continue;
                            }

                            int handleTimeoutDurationMs = Math.Max(0, registeredWait.TimeoutTimeMs - currentTimeMs);

                            if (timeoutDurationMs == Timeout.Infinite)
                            {
                                timeoutDurationMs = handleTimeoutDurationMs;
                            }
                            else
                            {
                                timeoutDurationMs = Math.Min(handleTimeoutDurationMs, timeoutDurationMs);
                            }

                            if (timeoutDurationMs == 0)
                            {
                                break;
                            }
                        }
                    }

                    int signaledHandleIndex = WaitHandle.WaitAny(new ReadOnlySpan<SafeWaitHandle>(_waitHandles, 0, numUserWaits + 1), timeoutDurationMs);

                    if (signaledHandleIndex >= WaitHandle.WaitAbandoned &&
                        signaledHandleIndex < WaitHandle.WaitAbandoned + 1 + numUserWaits)
                    {
                        // For compatibility, treat an abandoned mutex wait result as a success and ignore the abandonment
                        Debug.Assert(signaledHandleIndex != WaitHandle.WaitAbandoned); // the first wait handle is an event
                        signaledHandleIndex += WaitHandle.WaitSuccess - WaitHandle.WaitAbandoned;
                    }

                    if (signaledHandleIndex == 0) // If we were woken up for a change in our handles, continue.
                    {
                        continue;
                    }

                    if (signaledHandleIndex != WaitHandle.WaitTimeout)
                    {
                        RegisteredWaitHandle signaledHandle = _registeredWaits[signaledHandleIndex - 1];
                        Debug.Assert(signaledHandle != null);
                        QueueWaitCompletion(signaledHandle, false);
                        continue;
                    }

                    if (numUserWaits == 0 && ThreadPoolInstance.TryRemoveWaitThread(this))
                    {
                        return;
                    }

                    currentTimeMs = Environment.TickCount;
                    for (int i = 0; i < numUserWaits; i++)
                    {
                        RegisteredWaitHandle registeredHandle = _registeredWaits[i];
                        Debug.Assert(registeredHandle != null);
                        if (!registeredHandle.IsInfiniteTimeout && currentTimeMs - registeredHandle.TimeoutTimeMs >= 0)
                        {
                            QueueWaitCompletion(registeredHandle, true);
                        }
                    }
                }
            }

            /// <summary>
            /// Go through the <see cref="_pendingRemoves"/> array and remove those registered wait handles from the <see cref="_registeredWaits"/>
            /// and <see cref="_waitHandles"/> arrays, filling the holes along the way.
            /// </summary>
            private int ProcessRemovals()
            {
                PortableThreadPool threadPoolInstance = ThreadPoolInstance;
                threadPoolInstance._waitThreadLock.Acquire();
                try
                {
                    Debug.Assert(_numPendingRemoves >= 0);
                    Debug.Assert(_numPendingRemoves <= _pendingRemoves.Length);
                    Debug.Assert(_numUserWaits >= 0);
                    Debug.Assert(_numUserWaits <= _registeredWaits.Length);
                    Debug.Assert(_numPendingRemoves <= _numUserWaits, $"Num removals {_numPendingRemoves} should be less than or equal to num user waits {_numUserWaits}");

                    if (_numPendingRemoves == 0 || _numUserWaits == 0)
                    {
                        return _numUserWaits; // return the value taken inside the lock for the caller
                    }
                    int originalNumUserWaits = _numUserWaits;
                    int originalNumPendingRemoves = _numPendingRemoves;

                    // This is O(N^2), but max(N) = 63 and N will usually be very low
                    for (int i = 0; i < _numPendingRemoves; i++)
                    {
                        RegisteredWaitHandle waitHandleToRemove = _pendingRemoves[i]!;
                        int numUserWaits = _numUserWaits;
                        int j = 0;
                        for (; j < numUserWaits && waitHandleToRemove != _registeredWaits[j]; j++)
                        {
                        }
                        Debug.Assert(j < numUserWaits);

                        waitHandleToRemove.OnRemoveWait();

                        if (j + 1 < numUserWaits)
                        {
                            // Not removing the last element. Due to the possibility of there being duplicate system wait
                            // objects in the wait array, perhaps even with different handle values due to the use of
                            // DuplicateHandle(), don't reorder handles for fairness. When there are duplicate system wait
                            // objects in the wait array and the wait object gets signaled, the system may release the wait in
                            // in deterministic order based on the order in the wait array. Instead, shift the array.

                            int removeAt = j;
                            int count = numUserWaits;
                            Array.Copy(_registeredWaits, removeAt + 1, _registeredWaits, removeAt, count - (removeAt + 1));
                            _registeredWaits[count - 1] = null!;

                            // Corresponding elements in the wait handles array are shifted up by one
                            removeAt++;
                            count++;
                            Array.Copy(_waitHandles, removeAt + 1, _waitHandles, removeAt, count - (removeAt + 1));
                            _waitHandles[count - 1] = null!;
                        }
                        else
                        {
                            // Removing the last element
                            _registeredWaits[j] = null!;
                            _waitHandles[j + 1] = null!;
                        }

                        _numUserWaits = numUserWaits - 1;
                        _pendingRemoves[i] = null;

                        waitHandleToRemove.Handle.DangerousRelease();
                    }
                    _numPendingRemoves = 0;

                    Debug.Assert(originalNumUserWaits - originalNumPendingRemoves == _numUserWaits,
                        $"{originalNumUserWaits} - {originalNumPendingRemoves} == {_numUserWaits}");
                    return _numUserWaits; // return the value taken inside the lock for the caller
                }
                finally
                {
                    threadPoolInstance._waitThreadLock.Release();
                }
            }

            /// <summary>
            /// Queue a call to complete the wait on the ThreadPool.
            /// </summary>
            /// <param name="registeredHandle">The handle that completed.</param>
            /// <param name="timedOut">Whether or not the wait timed out.</param>
            private void QueueWaitCompletion(RegisteredWaitHandle registeredHandle, bool timedOut)
            {
                registeredHandle.RequestCallback();

                // If the handle is a repeating handle, set up the next call. Otherwise, remove it from the wait thread.
                if (registeredHandle.Repeating)
                {
                    if (!registeredHandle.IsInfiniteTimeout)
                    {
                        registeredHandle.RestartTimeout();
                    }
                }
                else
                {
                    UnregisterWait(registeredHandle, blocking: false); // We shouldn't block the wait thread on the unregistration.
                }

                ThreadPool.UnsafeQueueHighPriorityWorkItemInternal(
                    new CompleteWaitThreadPoolWorkItem(registeredHandle, timedOut));
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

                bool success = false;
                handle.Handle.DangerousAddRef(ref success);
                Debug.Assert(success);

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
                PortableThreadPool threadPoolInstance = ThreadPoolInstance;
                threadPoolInstance._waitThreadLock.Acquire();
                try
                {
                    // If this handle is not already pending removal and hasn't already been removed
                    if (Array.IndexOf(_registeredWaits, handle) >= 0)
                    {
                        if (Array.IndexOf(_pendingRemoves, handle) < 0)
                        {
                            _pendingRemoves[_numPendingRemoves++] = handle;
                            _changeHandlesEvent.Set(); // Tell the wait thread that there are changes pending.
                        }

                        pendingRemoval = true;
                    }
                }
                finally
                {
                    threadPoolInstance._waitThreadLock.Release();
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
