// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading
{
    internal static partial class WaitSubsystem
    {
        public sealed partial class WaitableObject : IWaitableObject
        {
            /// <summary>
            /// Linked list of information about waiting threads
            /// </summary>
            private ThreadWaitInfo.WaitedListNode? _waitersHead, _waitersTail;

            private int WaitUnsignaled(ThreadWaitInfo waitInfo, int timeoutMilliseconds, bool interruptible, ref LockHolder lockHolder)
            {
                WaitableObject?[] waitableObjects = waitInfo.GetWaitedObjectArray(1);
                waitableObjects[0] = this;
                waitInfo.RegisterWait(1, isWaitForAll: false);
                return waitInfo.Wait(timeoutMilliseconds, interruptible, isSleep: false, ref lockHolder);
            }

            private static int WaitUnsignaled(
                WaitableObject?[] waitableObjects,
                int count,
                bool waitForAll,
                ThreadWaitInfo waitInfo,
                int timeoutMilliseconds,
                bool interruptible,
                ref LockHolder lockHolder)
            {
                Debug.Assert(waitableObjects.Length >= count);
                waitInfo.RegisterWait(count, waitForAll);
                return waitInfo.Wait(timeoutMilliseconds, interruptible, isSleep: false, ref lockHolder);
            }
            public ThreadWaitInfo.WaitedListNode? WaitersHead
            {
                get
                {
                    s_lock.VerifyIsLocked();
                    return _waitersHead;
                }
                set
                {
                    s_lock.VerifyIsLocked();
                    _waitersHead = value;
                }
            }

            public ThreadWaitInfo.WaitedListNode? WaitersTail
            {
                get
                {
                    s_lock.VerifyIsLocked();
                    return _waitersTail;
                }
                set
                {
                    s_lock.VerifyIsLocked();
                    _waitersTail = value;
                }
            }

            public static bool WouldWaitForAllBeSatisfiedOrAborted(
                Thread waitingThread,
                WaitableObject?[] waitedObjects,
                int waitedCount,
                int signaledWaitedObjectIndex,
                ref bool wouldAnyMutexReacquireCountOverflow,
                ref bool isAnyAbandonedMutex)
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(waitingThread != null);
                Debug.Assert(waitingThread != Thread.CurrentThread);
                Debug.Assert(waitedObjects != null);
                Debug.Assert(waitedObjects.Length >= waitedCount);
                Debug.Assert(waitedCount > 1);
                Debug.Assert(signaledWaitedObjectIndex >= 0);
                Debug.Assert(signaledWaitedObjectIndex <= WaitHandle.MaxWaitHandles);
                Debug.Assert(!wouldAnyMutexReacquireCountOverflow);

                for (int i = 0; i < waitedCount; ++i)
                {
                    Debug.Assert(waitedObjects[i] != null);
                    if (i == signaledWaitedObjectIndex)
                    {
                        continue;
                    }

                    WaitableObject waitedObject = waitedObjects[i]!;
                    if (waitedObject.IsSignaled)
                    {
                        if (!isAnyAbandonedMutex && waitedObject.IsAbandonedMutex)
                        {
                            isAnyAbandonedMutex = true;
                        }
                        continue;
                    }

                    if (waitedObject.IsMutex)
                    {
                        OwnershipInfo ownershipInfo = waitedObject._ownershipInfo!;
                        if (ownershipInfo.Thread == waitingThread)
                        {
                            if (!ownershipInfo.CanIncrementReacquireCount)
                            {
                                // This will cause the wait to be aborted without accepting any signals
                                wouldAnyMutexReacquireCountOverflow = true;
                                return true;
                            }
                            continue;
                        }
                    }

                    return false;
                }

                return true;
            }

            public static void SatisfyWaitForAll(
                ThreadWaitInfo waitInfo,
                WaitableObject?[] waitedObjects,
                int waitedCount,
                int signaledWaitedObjectIndex)
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(waitInfo != null);
                Debug.Assert(waitInfo.Thread != Thread.CurrentThread);
                Debug.Assert(waitedObjects != null);
                Debug.Assert(waitedObjects.Length >= waitedCount);
                Debug.Assert(waitedCount > 1);
                Debug.Assert(signaledWaitedObjectIndex >= 0);
                Debug.Assert(signaledWaitedObjectIndex <= WaitHandle.MaxWaitHandles);

                for (int i = 0; i < waitedCount; ++i)
                {
                    Debug.Assert(waitedObjects[i] != null);
                    if (i == signaledWaitedObjectIndex)
                    {
                        continue;
                    }

                    WaitableObject waitedObject = waitedObjects[i]!;
                    if (waitedObject.IsSignaled)
                    {
                        waitedObject.AcceptSignal(waitInfo);
                        continue;
                    }

                    Debug.Assert(waitedObject.IsMutex);
                    OwnershipInfo ownershipInfo = waitedObject._ownershipInfo!;
                    Debug.Assert(ownershipInfo.Thread == waitInfo.Thread);
                    ownershipInfo.IncrementReacquireCount();
                }
            }

            private void SignalManualResetEvent()
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(IsManualResetEvent);

                if (IsSignaled)
                {
                    return;
                }

                for (ThreadWaitInfo.WaitedListNode? waiterNode = _waitersHead, nextWaiterNode;
                    waiterNode != null;
                    waiterNode = nextWaiterNode)
                {
                    // Signaling a waiter will unregister the waiter node, so keep the next node before trying
                    nextWaiterNode = waiterNode.NextThread;

                    waiterNode.WaitInfo.TrySignalToSatisfyWait(waiterNode, isAbandonedMutex: false);
                }

                _signalCount = 1;
            }

            private void SignalAutoResetEvent()
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(IsAutoResetEvent);

                if (IsSignaled)
                {
                    return;
                }

                for (ThreadWaitInfo.WaitedListNode? waiterNode = _waitersHead, nextWaiterNode;
                    waiterNode != null;
                    waiterNode = nextWaiterNode)
                {
                    // Signaling a waiter will unregister the waiter node, but it may only abort the wait without satisfying the
                    // wait, in which case we would try to signal another waiter. So, keep the next node before trying.
                    nextWaiterNode = waiterNode.NextThread;

                    if (waiterNode.WaitInfo.TrySignalToSatisfyWait(waiterNode, isAbandonedMutex: false))
                    {
                        return;
                    }
                }

                _signalCount = 1;
            }

            public int SignalSemaphore(int count, ref LockHolder lockHolder)
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(count > 0);

                if (!IsSemaphore)
                {
                    lockHolder.Dispose();
                    WaitHandle.ThrowInvalidHandleException();
                }

                int oldSignalCount = _signalCount;
                Debug.Assert(oldSignalCount <= _maximumSignalCount);
                if (count > _maximumSignalCount - oldSignalCount)
                {
                    lockHolder.Dispose();
                    throw new SemaphoreFullException();
                }

                if (oldSignalCount != 0)
                {
                    _signalCount = oldSignalCount + count;
                    return oldSignalCount;
                }

                for (ThreadWaitInfo.WaitedListNode? waiterNode = _waitersHead, nextWaiterNode;
                    waiterNode != null;
                    waiterNode = nextWaiterNode)
                {
                    // Signaling the waiter will unregister the waiter node, so keep the next node before trying
                    nextWaiterNode = waiterNode.NextThread;

                    if (waiterNode.WaitInfo.TrySignalToSatisfyWait(waiterNode, isAbandonedMutex: false) && --count == 0)
                    {
                        return oldSignalCount;
                    }
                }

                _signalCount = count;
                return oldSignalCount;
            }

            private void SignalMutex(bool isAbandoned)
            {
                s_lock.VerifyIsLocked();

                Debug.Assert(IsMutex);
                Debug.Assert(!IsSignaled);
                Debug.Assert(_ownershipInfo != null);

                _ownershipInfo.RelinquishOwnership(this, isAbandoned);

                for (ThreadWaitInfo.WaitedListNode? waiterNode = _waitersHead, nextWaiterNode;
                    waiterNode != null;
                    waiterNode = nextWaiterNode)
                {
                    // Signaling a waiter will unregister the waiter node, but it may only abort the wait without satisfying the
                    // wait, in which case we would try to signal another waiter. So, keep the next node before trying.
                    nextWaiterNode = waiterNode.NextThread;

                    ThreadWaitInfo waitInfo = waiterNode.WaitInfo;
                    if (waitInfo.TrySignalToSatisfyWait(waiterNode, isAbandoned))
                    {
                        _ownershipInfo.AssignOwnership(this, waitInfo);
                        return;
                    }
                }

                _signalCount = 1;
            }

            private sealed partial class OwnershipInfo
            {
                private Thread? _thread;
                private WaitableObject? _previous;
                private WaitableObject? _next;

                public Thread? Thread
                {
                    get
                    {
                        s_lock.VerifyIsLocked();
                        return _thread;
                    }
                }

                public void AssignOwnership(WaitableObject waitableObject, ThreadWaitInfo waitInfo)
                {
                    s_lock.VerifyIsLocked();

                    Debug.Assert(waitableObject != null);
                    Debug.Assert(waitableObject.IsMutex);
                    Debug.Assert(waitableObject._ownershipInfo == this);

                    Debug.Assert(_thread == null);
                    Debug.Assert(_reacquireCount == 0);
                    Debug.Assert(_previous == null);
                    Debug.Assert(_next == null);

                    _thread = waitInfo.Thread;
                    _isAbandoned = false;

                    WaitableObject? head = waitInfo.LockedMutexesHead;
                    if (head != null)
                    {
                        _next = head;
                        head._ownershipInfo!._previous = waitableObject;
                    }
                    waitInfo.LockedMutexesHead = waitableObject;
                }

                public void RelinquishOwnership(WaitableObject waitableObject, bool isAbandoned)
                {
                    s_lock.VerifyIsLocked();

                    Debug.Assert(waitableObject != null);
                    Debug.Assert(waitableObject.IsMutex);
                    Debug.Assert(waitableObject._ownershipInfo == this);

                    Debug.Assert(_thread != null);
                    ThreadWaitInfo waitInfo = _thread.WaitInfo;
                    Debug.Assert(isAbandoned ? _reacquireCount >= 0 : _reacquireCount == 0);
                    Debug.Assert(!_isAbandoned);

                    _thread = null;
                    if (isAbandoned)
                    {
                        _reacquireCount = 0;
                        _isAbandoned = isAbandoned;
                    }

                    WaitableObject? previous = _previous;
                    WaitableObject? next = _next;

                    if (previous != null)
                    {
                        previous._ownershipInfo!._next = next;
                        _previous = null;
                    }
                    else
                    {
                        Debug.Assert(waitInfo.LockedMutexesHead == waitableObject);
                        waitInfo.LockedMutexesHead = next;
                    }

                    if (next != null)
                    {
                        next._ownershipInfo!._previous = previous;
                        _next = null;
                    }
                }
            }
        }
    }
}
