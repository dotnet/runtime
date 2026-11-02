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
        public sealed partial class WaitableObject : IWaitableObject
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
                        // The multithreaded implementation tracks owned mutexes on the thread.
                        // Abandon the mutex to remove ownership when the final handle is deleted.
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

            /// <summary>
            /// This function does not check for a pending thread interrupt. Callers are expected to do that soon after
            /// acquiring <see cref="s_lock"/>.
            /// </summary>
            public int Wait_Locked(ThreadWaitInfo waitInfo, int timeoutMilliseconds, bool interruptible, ref LockHolder lockHolder)
            {
                s_lock.VerifyIsLocked();
                Debug.Assert(waitInfo != null);
                Debug.Assert(waitInfo.Thread == Thread.CurrentThread);

                Debug.Assert(timeoutMilliseconds >= -1);
                Debug.Assert(!interruptible || !waitInfo.CheckAndResetPendingInterrupt);

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
                        lockHolder.Dispose();
                        throw new OverflowException(SR.Overflow_MutexReacquireCount);
                    }
                    _ownershipInfo.IncrementReacquireCount();
                    return WaitHandle.WaitSuccess;
                }

                if (timeoutMilliseconds == 0)
                {
                    return WaitHandle.WaitTimeout;
                }

                return WaitUnsignaled(waitInfo, timeoutMilliseconds, interruptible, ref lockHolder);
            }

            public static int Wait(
                WaitableObject?[]? waitableObjects,
                int count,
                bool waitForAll,
                ThreadWaitInfo waitInfo,
                int timeoutMilliseconds,
                bool interruptible)
            {
                s_lock.VerifyIsNotLocked();
                Debug.Assert(waitInfo != null);
                Debug.Assert(waitInfo.Thread == Thread.CurrentThread);

                Debug.Assert(waitableObjects != null);
                Debug.Assert(waitableObjects.Length >= count);
                Debug.Assert(count > 1);
                Debug.Assert(timeoutMilliseconds >= -1);

                var lockHolder = new LockHolder(s_lock);
                try
                {
                    if (interruptible && waitInfo.CheckAndResetPendingInterrupt)
                    {
                        lockHolder.Dispose();
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
                                        lockHolder.Dispose();
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
                                        lockHolder.Dispose();
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
                                lockHolder.Dispose();
                                throw new AbandonedMutexException();
                            }
                            return WaitHandle.WaitSuccess;
                        }
                    }

                    if (timeoutMilliseconds == 0)
                    {
                        return WaitHandle.WaitTimeout;
                    }

                    WaitableObject?[] waitedObjects = waitableObjects;
                    waitableObjects = null; // The threading-specific wait takes over clearing the array.
                    return WaitUnsignaled(waitedObjects, count, waitForAll, waitInfo, timeoutMilliseconds, interruptible, ref lockHolder);
                }
                finally
                {
                    lockHolder.Dispose();

                    if (waitableObjects != null)
                    {
                        Array.Clear(waitableObjects, 0, count);
                    }
                }
            }

            public void Signal(int count, ref LockHolder lockHolder)
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
                        SignalSemaphore(count, ref lockHolder);
                        break;

                    default:
                        Debug.Assert(count == 1);
                        SignalMutex(ref lockHolder);
                        break;
                }
            }

            public void SignalEvent(ref LockHolder lockHolder)
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
                        lockHolder.Dispose();
                        WaitHandle.ThrowInvalidHandleException();
                        break;
                }
            }

            public void UnsignalEvent(ref LockHolder lockHolder)
            {
                s_lock.VerifyIsLocked();

                if (!IsEvent)
                {
                    lockHolder.Dispose();
                    WaitHandle.ThrowInvalidHandleException();
                }

                if (IsSignaled)
                {
                    --_signalCount;
                }
            }

            public void SignalMutex(ref LockHolder lockHolder)
            {
                s_lock.VerifyIsLocked();

                if (!IsMutex)
                {
                    lockHolder.Dispose();
                    WaitHandle.ThrowInvalidHandleException();
                }

                if (IsSignaled || _ownershipInfo!.Thread != Thread.CurrentThread)
                {
                    lockHolder.Dispose();
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

            private sealed partial class OwnershipInfo
            {
                private int _reacquireCount;
                private bool _isAbandoned;

                public bool IsAbandoned
                {
                    get
                    {
                        s_lock.VerifyIsLocked();
                        return _isAbandoned;
                    }
                }

                public bool CanIncrementReacquireCount
                {
                    get
                    {
                        s_lock.VerifyIsLocked();

                        Debug.Assert(Thread is not null);
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

                    Debug.Assert(Thread == System.Threading.Thread.CurrentThread);
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
