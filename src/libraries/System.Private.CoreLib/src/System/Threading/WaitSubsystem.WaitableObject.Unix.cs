// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading
{
    internal static partial class WaitSubsystem
    {
        /// <summary>
        /// A synchronization object that can participate in <see cref="WaitSubsystem"/>'s wait operations.
        ///
        /// Used by the wait subsystem on Unix, so this class cannot have any dependencies on the wait subsystem.
        /// </summary>
        public sealed class WaitableObject
        {
            /// <summary>
            /// Dictionary to look up named waitable objects.  This implementation only supports in-process
            /// named waitable objects.  Currently only named mutexes are supported.
            /// </summary>
            private static Dictionary<string, WaitableObject>? s_namedObjects;

            private readonly WaitableObjectType _type;
            private int _signalCount;
            private readonly int _maximumSignalCount;
            private int _referenceCount;
            private readonly string? _name;

            /// <summary>
            /// Only <see cref="Mutex"/> has a thread ownership requirement, and it's a less common type to be used, so
            /// ownership info is separated to reduce the size. For other types, this is null.
            /// </summary>
            private readonly OwnershipInfo? _ownershipInfo;

            /// <summary>
            /// Linked list of information about waiting threads
            /// </summary>
            private ThreadWaitInfo.WaitedListNode? _waitersHead, _waitersTail;

            private WaitableObject(
                WaitableObjectType type,
                int initialSignalCount,
                int maximumSignalCount,
                string? name,
                OwnershipInfo? ownershipInfo)
            {
                Debug.Assert(initialSignalCount >= 0);
                Debug.Assert(maximumSignalCount > 0);
                Debug.Assert(initialSignalCount <= maximumSignalCount);

                _type = type;
                _signalCount = initialSignalCount;
                _maximumSignalCount = maximumSignalCount;
                _referenceCount = 1;
                _name = name;
                _ownershipInfo = ownershipInfo;
            }

            public static WaitableObject NewEvent(bool initiallySignaled, EventResetMode resetMode)
            {
                Debug.Assert((resetMode == EventResetMode.AutoReset) || (resetMode == EventResetMode.ManualReset));

                return
                    new WaitableObject(
                        resetMode == EventResetMode.ManualReset
                            ? WaitableObjectType.ManualResetEvent
                            : WaitableObjectType.AutoResetEvent,
                        initiallySignaled ? 1 : 0,
                        1,
                        null,
                        null);
            }

            public static WaitableObject NewSemaphore(int initialSignalCount, int maximumSignalCount)
            {
                return new WaitableObject(WaitableObjectType.Semaphore, initialSignalCount, maximumSignalCount, null, null);
            }

            public static WaitableObject NewMutex()
            {
                return new WaitableObject(WaitableObjectType.Mutex, 1, 1, null, new OwnershipInfo());
            }

            public static WaitableObject? CreateNamedMutex_Locked(string name, out bool createdNew)
            {
                s_lock.VerifyIsLocked();

                s_namedObjects ??= new Dictionary<string, WaitableObject>();

                if (s_namedObjects.TryGetValue(name, out WaitableObject? result))
                {
                    createdNew = false;
                    if (!result.IsMutex)
                    {
                        return null;
                    }
                    result._referenceCount++;
                }
                else
                {
                    createdNew = true;
                    result = new WaitableObject(WaitableObjectType.Mutex, 1, 1, name, new OwnershipInfo());
                    s_namedObjects.Add(name, result);
                }

                return result;
            }

            public static OpenExistingResult OpenNamedMutex(string name, out WaitableObject? result)
            {
                s_lock.Acquire();
                try
                {
                    if (s_namedObjects == null || !s_namedObjects.TryGetValue(name, out result))
                    {
                        result = null;
                        return OpenExistingResult.NameNotFound;
                    }
                    if (!result.IsMutex)
                    {
                        result = null;
                        return OpenExistingResult.NameInvalid;
                    }
                    result._referenceCount++;
                    return OpenExistingResult.Success;
                }
                finally
                {
                    s_lock.Release();
                }
            }

            public void OnDeleteHandle()
            {
                s_lock.Acquire();
                try
                {
                    // Multiple handles may refer to the same named object.  Make sure the object
                    // is only abandoned once the last handle to it is deleted.  Also, remove the
                    // object from the named objects dictionary at this point.
                    _referenceCount--;
                    if (_referenceCount > 0)
                    {
                        return;
                    }
                    if (_name != null)
                    {
                        s_namedObjects!.Remove(_name);
                    }

                    if (IsMutex && !IsSignaled)
                    {
                        // A thread has a reference to all <see cref="Mutex"/>es locked by it, see
                        // <see cref="ThreadWaitInfo.LockedMutexesHead"/>. Abandon the mutex to remove the reference to it.
                        AbandonMutex();
                    }
                }
                finally
                {
                    s_lock.Release();
                }
            }

            private bool IsEvent
            {
                get
                {
                    s_lock.VerifyIsLocked();

                    bool result = _type <= WaitableObjectType.AutoResetEvent;
                    Debug.Assert(!result || _maximumSignalCount == 1);
                    Debug.Assert(!result || _ownershipInfo == null);
                    return result;
                }
            }

            private bool IsManualResetEvent
            {
                get
                {
                    s_lock.VerifyIsLocked();

                    bool result = _type == WaitableObjectType.ManualResetEvent;
                    Debug.Assert(!result || _maximumSignalCount == 1);
                    Debug.Assert(!result || _ownershipInfo == null);
                    return result;
                }
            }

            private bool IsAutoResetEvent
            {
                get
                {
                    s_lock.VerifyIsLocked();

                    bool result = _type == WaitableObjectType.AutoResetEvent;
                    Debug.Assert(!result || _maximumSignalCount == 1);
                    Debug.Assert(!result || _ownershipInfo == null);
                    return result;
                }
            }

            private bool IsSemaphore
            {
                get
                {
                    s_lock.VerifyIsLocked();

                    bool result = _type == WaitableObjectType.Semaphore;
                    Debug.Assert(!result || _maximumSignalCount > 0);
                    Debug.Assert(!result || _ownershipInfo == null);
                    return result;
                }
            }

            private bool IsMutex
            {
                get
                {
                    s_lock.VerifyIsLocked();

                    bool result = _type == WaitableObjectType.Mutex;
                    Debug.Assert(!result || _maximumSignalCount == 1);
                    Debug.Assert(!result || _ownershipInfo != null);
                    return result;
                }
            }

            private bool IsAbandonedMutex
            {
                get
                {
                    s_lock.VerifyIsLocked();
                    return IsMutex && _ownershipInfo != null && _ownershipInfo.IsAbandoned;
                }
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

            private bool IsSignaled
            {
                get
                {
                    s_lock.VerifyIsLocked();

                    bool isSignaled = _signalCount != 0;
                    Debug.Assert(isSignaled || !IsMutex || (_ownershipInfo != null && _ownershipInfo.Thread != null));
                    return isSignaled;
                }
            }

            private void AcceptSignal(ThreadWaitInfo waitInfo)
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(IsSignaled);

                switch (_type)
                {
                    case WaitableObjectType.ManualResetEvent:
                        return;

                    case WaitableObjectType.AutoResetEvent:
                    case WaitableObjectType.Semaphore:
                        --_signalCount;
                        return;

                    default:
                        Debug.Assert(_type == WaitableObjectType.Mutex);
                        --_signalCount;

                        Debug.Assert(_ownershipInfo!.Thread == null);
                        _ownershipInfo.AssignOwnership(this, waitInfo);
                        return;
                }
            }

            public int Wait(ThreadWaitInfo waitInfo, int timeoutMilliseconds, bool interruptible, bool prioritize)
            {
                Debug.Assert(waitInfo != null);
                Debug.Assert(waitInfo.Thread == Thread.CurrentThread);

                Debug.Assert(timeoutMilliseconds >= -1);

                s_lock.Acquire();

                if (interruptible && waitInfo.CheckAndResetPendingInterrupt)
                {
                    s_lock.Release();
                    throw new ThreadInterruptedException();
                }

                return Wait_Locked(waitInfo, timeoutMilliseconds, interruptible, prioritize);
            }

            /// <summary>
            /// This function does not check for a pending thread interrupt. Callers are expected to do that soon after
            /// acquiring <see cref="s_lock"/>.
            /// </summary>
            public int Wait_Locked(ThreadWaitInfo waitInfo, int timeoutMilliseconds, bool interruptible, bool prioritize)
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(waitInfo != null);
                Debug.Assert(waitInfo.Thread == Thread.CurrentThread);

                Debug.Assert(timeoutMilliseconds >= -1);
                Debug.Assert(!interruptible || !waitInfo.CheckAndResetPendingInterrupt);

                bool needToWait = false;
                try
                {
                    if (IsSignaled)
                    {
                        bool isAbandoned = IsAbandonedMutex;
                        AcceptSignal(waitInfo);
                        return isAbandoned ? WaitHandle.WaitAbandoned : WaitHandle.WaitSuccess;
                    }

                    if (IsMutex && _ownershipInfo != null && _ownershipInfo.Thread == waitInfo.Thread)
                    {
                        if (!_ownershipInfo.CanIncrementReacquireCount)
                        {
                            throw new OverflowException(SR.Overflow_MutexReacquireCount);
                        }
                        _ownershipInfo.IncrementReacquireCount();
                        return WaitHandle.WaitSuccess;
                    }

                    if (timeoutMilliseconds == 0)
                    {
                        return WaitHandle.WaitTimeout;
                    }

                    WaitableObject?[] waitableObjects = waitInfo.GetWaitedObjectArray(1);
                    waitableObjects[0] = this;
                    waitInfo.RegisterWait(1, prioritize, isWaitForAll: false);
                    needToWait = true;
                }
                finally
                {
                    // Once the wait function is called, it will release the lock
                    if (!needToWait)
                    {
                        s_lock.Release();
                    }
                }

                return
                    waitInfo.Wait(
                        timeoutMilliseconds,
                        interruptible,
                        isSleep: false);
            }

            public static int Wait(
                WaitableObject?[]? waitableObjects,
                int count,
                bool waitForAll,
                ThreadWaitInfo waitInfo,
                int timeoutMilliseconds,
                bool interruptible,
                bool prioritize)
            {
                s_lock.VerifyIsNotLocked();
                Debug.Assert(waitInfo != null);
                Debug.Assert(waitInfo.Thread == Thread.CurrentThread);

                Debug.Assert(waitableObjects != null);
                Debug.Assert(waitableObjects.Length >= count);
                Debug.Assert(count > 1);
                Debug.Assert(timeoutMilliseconds >= -1);

                bool needToWait = false;
                s_lock.Acquire();
                try
                {
                    if (interruptible && waitInfo.CheckAndResetPendingInterrupt)
                    {
                        throw new ThreadInterruptedException();
                    }

                    if (!waitForAll)
                    {
                        // Check if any is already signaled
                        for (int i = 0; i < count; ++i)
                        {
                            WaitableObject waitableObject = waitableObjects[i]!;
                            Debug.Assert(waitableObject != null);

                            if (waitableObject.IsSignaled)
                            {
                                bool isAbandoned = waitableObject.IsAbandonedMutex;
                                waitableObject.AcceptSignal(waitInfo);
                                if (isAbandoned)
                                {
                                    return WaitHandle.WaitAbandoned + i;
                                }
                                return WaitHandle.WaitSuccess + i;
                            }

                            if (waitableObject.IsMutex)
                            {
                                OwnershipInfo ownershipInfo = waitableObject._ownershipInfo!;
                                if (ownershipInfo.Thread == waitInfo.Thread)
                                {
                                    if (!ownershipInfo.CanIncrementReacquireCount)
                                    {
                                        throw new OverflowException(SR.Overflow_MutexReacquireCount);
                                    }
                                    ownershipInfo.IncrementReacquireCount();
                                    return WaitHandle.WaitSuccess + i;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Check if all are already signaled
                        bool areAllSignaled = true;
                        bool isAnyAbandonedMutex = false;
                        for (int i = 0; i < count; ++i)
                        {
                            WaitableObject waitableObject = waitableObjects[i]!;
                            Debug.Assert(waitableObject != null);

                            if (waitableObject.IsSignaled)
                            {
                                if (!isAnyAbandonedMutex && waitableObject.IsAbandonedMutex)
                                {
                                    isAnyAbandonedMutex = true;
                                }
                                continue;
                            }

                            if (waitableObject.IsMutex)
                            {
                                OwnershipInfo ownershipInfo = waitableObject._ownershipInfo!;
                                if (ownershipInfo.Thread == waitInfo.Thread)
                                {
                                    if (!ownershipInfo.CanIncrementReacquireCount)
                                    {
                                        throw new OverflowException(SR.Overflow_MutexReacquireCount);
                                    }
                                    continue;
                                }
                            }

                            areAllSignaled = false;
                            break;
                        }

                        if (areAllSignaled)
                        {
                            for (int i = 0; i < count; ++i)
                            {
                                WaitableObject waitableObject = waitableObjects[i]!;
                                if (waitableObject.IsSignaled)
                                {
                                    waitableObject.AcceptSignal(waitInfo);
                                    continue;
                                }

                                Debug.Assert(waitableObject.IsMutex);
                                OwnershipInfo ownershipInfo = waitableObject._ownershipInfo!;
                                Debug.Assert(ownershipInfo.Thread == waitInfo.Thread);
                                ownershipInfo.IncrementReacquireCount();
                            }

                            if (isAnyAbandonedMutex)
                            {
                                throw new AbandonedMutexException();
                            }
                            return WaitHandle.WaitSuccess;
                        }
                    }

                    if (timeoutMilliseconds == 0)
                    {
                        return WaitHandle.WaitTimeout;
                    }

                    waitableObjects = null; // no need to clear this anymore, RegisterWait / Wait will take over from here
                    waitInfo.RegisterWait(count, prioritize, waitForAll);
                    needToWait = true;
                }
                finally
                {
                    if (waitableObjects != null)
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            waitableObjects[i] = null;
                        }
                    }

                    // Once the wait function is called, it will release the lock
                    if (!needToWait)
                    {
                        s_lock.Release();
                    }
                }

                return waitInfo.Wait(timeoutMilliseconds, interruptible, isSleep: false);
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

            public void Signal(int count)
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(count > 0);

                switch (_type)
                {
                    case WaitableObjectType.ManualResetEvent:
                        Debug.Assert(count == 1);
                        SignalManualResetEvent();
                        break;

                    case WaitableObjectType.AutoResetEvent:
                        Debug.Assert(count == 1);
                        SignalAutoResetEvent();
                        break;

                    case WaitableObjectType.Semaphore:
                        SignalSemaphore(count);
                        break;

                    default:
                        Debug.Assert(count == 1);
                        SignalMutex();
                        break;
                }
            }

            public void SignalEvent()
            {
                s_lock.VerifyIsLocked();

                switch (_type)
                {
                    case WaitableObjectType.ManualResetEvent:
                        SignalManualResetEvent();
                        break;

                    case WaitableObjectType.AutoResetEvent:
                        SignalAutoResetEvent();
                        break;

                    default:
                        WaitHandle.ThrowInvalidHandleException();
                        break;
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

            public void UnsignalEvent()
            {
                s_lock.VerifyIsLocked();

                if (!IsEvent)
                {
                    WaitHandle.ThrowInvalidHandleException();
                }

                if (IsSignaled)
                {
                    --_signalCount;
                }
            }

            public int SignalSemaphore(int count)
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(count > 0);

                if (!IsSemaphore)
                {
                    WaitHandle.ThrowInvalidHandleException();
                }

                int oldSignalCount = _signalCount;
                Debug.Assert(oldSignalCount <= _maximumSignalCount);
                if (count > _maximumSignalCount - oldSignalCount)
                {
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

            public void SignalMutex()
            {
                s_lock.VerifyIsLocked();

                if (!IsMutex)
                {
                    WaitHandle.ThrowInvalidHandleException();
                }

                if (IsSignaled || _ownershipInfo!.Thread != Thread.CurrentThread)
                {
                    throw new ApplicationException(SR.Arg_SynchronizationLockException);
                }

                if (!_ownershipInfo!.TryDecrementReacquireCount())
                {
                    SignalMutex(isAbandoned: false);
                }
            }

            public void AbandonMutex()
            {
                s_lock.VerifyIsLocked();

                Debug.Assert(IsMutex);
                Debug.Assert(!IsSignaled);

                // Typically, a mutex is abandoned before a thread that owns the mutex exits. However, any thread may
                // abandon a mutex since any thread may delete a mutex handle. See <see cref="OnDeleteHandle"/>. Although
                // Windows does not release waiters when a mutex is abandoned by deleting its handle, this implementation
                // treats that case as abandonment as well and releases waiters, as it's a bit more robust. As the handle
                // would be deleted shortly afterwards, it would otherwise not be possible to signal the mutex to release
                // waiters.
                SignalMutex(isAbandoned: true);
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

            private sealed class OwnershipInfo
            {
                private Thread? _thread;
                private int _reacquireCount;
                private bool _isAbandoned;

                /// <summary>
                /// Link in the <see cref="ThreadWaitInfo.LockedMutexesHead"/> linked list
                /// </summary>
                private WaitableObject? _previous;

                /// <summary>
                /// Link in the <see cref="ThreadWaitInfo.LockedMutexesHead"/> linked list
                /// </summary>
                private WaitableObject? _next;

                public Thread? Thread
                {
                    get
                    {
                        s_lock.VerifyIsLocked();
                        return _thread;
                    }
                }

                public bool IsAbandoned
                {
                    get
                    {
                        s_lock.VerifyIsLocked();
                        return _isAbandoned;
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

                public bool CanIncrementReacquireCount
                {
                    get
                    {
                        s_lock.VerifyIsLocked();

                        Debug.Assert(_thread != null);
                        Debug.Assert(_reacquireCount >= 0);
                        Debug.Assert(!_isAbandoned);

                        return _reacquireCount + 1 > _reacquireCount;
                    }
                }

                public void IncrementReacquireCount()
                {
                    Debug.Assert(CanIncrementReacquireCount);
                    ++_reacquireCount;
                }

                public bool TryDecrementReacquireCount()
                {
                    s_lock.VerifyIsLocked();

                    Debug.Assert(_thread == Thread.CurrentThread);
                    Debug.Assert(_reacquireCount >= 0);
                    Debug.Assert(!_isAbandoned);

                    if (_reacquireCount == 0)
                    {
                        return false;
                    }
                    --_reacquireCount;
                    return true;
                }
            }

            private enum WaitableObjectType : byte
            {
                ManualResetEvent,
                AutoResetEvent,
                Semaphore,
                Mutex
            }
        }
    }
}
