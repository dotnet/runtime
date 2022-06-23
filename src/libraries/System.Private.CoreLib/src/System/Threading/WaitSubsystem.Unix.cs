// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    /// # Wait subsystem
    ///
    /// ## Types
    ///
    /// <see cref="WaitSubsystem"/>
    ///   - Static API surface for dealing with synchronization objects that support multi-wait, and to put a thread into a wait
    ///     state, on Unix
    ///   - Any interaction with the wait subsystem from outside should go through APIs on this class, and should not directly
    ///     go through any of the nested classes
    ///
    /// <see cref="WaitableObject"/>
    ///   - An object that supports the features of <see cref="EventWaitHandle"/>, <see cref="Semaphore"/>, and
    ///     <see cref="Mutex"/>. The handle of each of those classes is associated with a <see cref="WaitableObject"/>.
    ///
    /// <see cref="ThreadWaitInfo"/>
    ///   - Keeps information about a thread's wait and provides functionlity to put a thread into a wait state and to take it
    ///     out of a wait state. Each thread has an instance available through <see cref="Thread.WaitInfo"/>.
    ///
    /// <see cref="HandleManager"/>
    ///   - Provides functionality to allocate a handle associated with a <see cref="WaitableObject"/>, to retrieve the object
    ///     from a handle, and to delete a handle.
    ///
    /// <see cref="LowLevelLock"/> and <see cref="LowLevelMonitor"/>
    ///   - These are "low level" in the sense they don't depend on this wait subsystem, and any waits done are not
    ///     interruptible
    ///   - <see cref="LowLevelLock"/> is used for the process-wide lock <see cref="s_lock"/>
    ///   - <see cref="LowLevelMonitor"/> is the main system dependency of the wait subsystem, and all waits are done through
    ///     it. It is backed by a C++ equivalent in CoreLib.Native's pal_threading.*, which wraps a pthread mutex/condition
    ///     pair. Each thread has an instance in <see cref="ThreadWaitInfo._waitMonitor"/>, which is used to synchronize the
    ///     thread's wait state and for waiting. <see cref="LowLevelLock"/> also uses an instance of
    ///     <see cref="LowLevelMonitor"/> for waiting.
    ///
    /// ## Design goals
    ///
    /// Behave similarly to wait operations on Windows
    ///   - The design is similar to the one used by CoreCLR's PAL, but much simpler due to there being no need for supporting
    ///     process/thread waits, or cross-process multi-waits (which CoreCLR also does not support but there are many design
    ///     elements specific to it)
    ///   - Waiting
    ///     - A waiter keeps an array of objects on which it is waiting (see <see cref="ThreadWaitInfo._waitedObjects"/>).
    ///     - The waiter registers a <see cref="ThreadWaitInfo.WaitedListNode"/> with each <see cref="WaitableObject"/>
    ///     - The waiter waits on its own <see cref="ThreadWaitInfo._waitMonitor"/> to go into a wait state
    ///     - Upon timeout, the waiter unregisters the wait and continues
    ///   - Sleeping
    ///     - Sleeping is just another way of waiting, only there would not be any waited objects
    ///   - Signaling
    ///     - A signaler iterates over waiters and tries to release waiters based on the signal count
    ///     - For each waiter, the signaler checks if the waiter's wait can be terminated
    ///     - When a waiter's wait can be terminated, the signaler does everything necesary before waking the waiter, such that
    ///       the waiter can simply continue after awakening, including unregistering the wait and assigning ownership if
    ///       applicable
    ///   - Interrupting
    ///     - Interrupting is just another way of signaling a waiting thread. The interrupter unregisters the wait and wakes the
    ///       waiter.
    ///   - Wait release fairness
    ///     - As mentioned above in how signaling works, waiters are released in fair order (first come, first served)
    ///     - This is mostly done to match the behavior of synchronization objects in Windows, which are also fair
    ///     - Events have an implicit requirement to be fair
    ///       - For a <see cref="ManualResetEvent"/>, Set/Reset in quick succession requires that it wakes up all waiters,
    ///         implying that the design cannot be to signal a thread to wake and have it check the state when it awakens some
    ///         time in the future
    ///       - For an <see cref="AutoResetEvent"/>, Set/Set in quick succession requires that it wakes up two threads, implying
    ///         that a Set/Wait in quick succession cannot have the calling thread accept its own signal if there is a waiter
    ///     - There is an advantage to being fair, as it guarantees that threads are only awakened when necessary. That is, a
    ///       thread will never wake up and find that it has to go back to sleep because the wait is not satisfied (except due
    ///       to spurious wakeups caused by external factors).
    ///   - Synchronization
    ///     - A process-wide lock <see cref="s_lock"/> is used to synchronize most operations and the signal state of all
    ///       <see cref="WaitableObject"/>s in the process. Given that it is recommended to use alternative synchronization
    ///       types (<see cref="ManualResetEventSlim"/>, <see cref="SemaphoreSlim"/>, <see cref="Monitor"/>) for single-wait
    ///       cases, it is probably not worth optimizing for the single-wait case. It is possible with a small design change to
    ///       bypass the lock and use interlocked operations for uncontended cases, but at the cost of making multi-waits more
    ///       complicated and slower.
    ///     - The wait state of a thread (<see cref="ThreadWaitInfo._waitSignalState"/>), among other things, is synchronized
    ///       using the thread's <see cref="ThreadWaitInfo._waitMonitor"/>, so signalers and interrupters acquire the monitor's
    ///       lock before checking the wait state of a thread and signaling the thread to wake up.
    ///
    /// Self-consistent in the event of any exception
    ///   - Try/finally is used extensively, including around any operation that could fail due to out-of-memory
    ///
    /// Decent balance between memory usage and performance
    ///   - <see cref="WaitableObject"/> is intended to be as small as possible while avoiding virtual calls and casts
    ///   - As <see cref="Mutex"/> is not commonly used and requires more state, some of its state is separated into
    ///     <see cref="WaitableObject._ownershipInfo"/>
    ///   - When support for cross-process objects is added, the current thought is to have an <see cref="object"/> field that
    ///     is used for both cross-process state and ownership state.
    ///
    /// No allocation in typical cases of any operation except where necessary
    ///   - Since the maximum number of wait handles for a multi-wait operation is limited to
    ///     <see cref="WaitHandle.MaxWaitHandles"/>, arrays necessary for holding information about a multi-wait, and list nodes
    ///     necessary for registering a wait, are precreated with a low initial capacity that covers most typical cases
    ///   - Threads track owned mutexes by linking the <see cref="WaitableObject.OwnershipInfo"/> instance into a linked list
    ///     <see cref="ThreadWaitInfo.LockedMutexesHead"/>. <see cref="WaitableObject.OwnershipInfo"/> is itself a list node,
    ///     and is created along with the mutex <see cref="WaitableObject"/>.
    ///
    /// Minimal p/invokes in typical uncontended cases
    ///   - <see cref="HandleManager"/> currently uses <see cref="Runtime.InteropServices.GCHandle"/> in the interest of
    ///     simplicity, which p/invokes and does a cast to get the <see cref="WaitableObject"/> from a handle
    ///   - Most of the wait subsystem is written in C#, so there is no initially required p/invoke
    ///   - <see cref="LowLevelLock"/>, used by the process-wide lock <see cref="s_lock"/>, uses interlocked operations to
    ///     acquire and release the lock when there is no need to wait or to release a waiter. This is significantly faster than
    ///     using <see cref="LowLevelMonitor"/> as a lock, which uses pthread mutex functionality through p/invoke. The lock is
    ///     typically not held for very long, especially since allocations inside the lock will be rare.
    ///   - Since <see cref="s_lock"/> provides mutual exclusion for the states of all <see cref="WaitableObject"/>s in the
    ///     process, any operation that does not involve waiting or releasing a wait can occur with minimal p/invokes
    ///
#if NATIVEAOT
    [EagerStaticClassConstruction] // the wait subsystem is used during lazy class construction
#endif
    internal static partial class WaitSubsystem
    {
        private static readonly LowLevelLock s_lock = new LowLevelLock();

        private static SafeWaitHandle NewHandle(WaitableObject waitableObject)
        {
            IntPtr handle = HandleManager.NewHandle(waitableObject);
            SafeWaitHandle? safeWaitHandle = null;
            try
            {
                safeWaitHandle = new SafeWaitHandle(handle, ownsHandle: true);
                return safeWaitHandle;
            }
            finally
            {
                if (safeWaitHandle == null)
                {
                    HandleManager.DeleteHandle(handle);
                }
            }
        }

        public static SafeWaitHandle NewEvent(bool initiallySignaled, EventResetMode resetMode)
        {
            return NewHandle(WaitableObject.NewEvent(initiallySignaled, resetMode));
        }

        public static SafeWaitHandle NewSemaphore(int initialSignalCount, int maximumSignalCount)
        {
            return NewHandle(WaitableObject.NewSemaphore(initialSignalCount, maximumSignalCount));
        }

        public static SafeWaitHandle NewMutex(bool initiallyOwned)
        {
            WaitableObject waitableObject = WaitableObject.NewMutex();
            SafeWaitHandle safeWaitHandle = NewHandle(waitableObject);
            if (!initiallyOwned)
            {
                return safeWaitHandle;
            }

            // Acquire the mutex. A thread's <see cref="ThreadWaitInfo"/> has a reference to all <see cref="Mutex"/>es locked
            // by the thread. See <see cref="ThreadWaitInfo.LockedMutexesHead"/>. So, acquire the lock only after all
            // possibilities for exceptions have been exhausted.
            ThreadWaitInfo waitInfo = Thread.CurrentThread.WaitInfo;
            bool acquiredLock = waitableObject.Wait(waitInfo, timeoutMilliseconds: 0, interruptible: false, prioritize: false) == 0;
            Debug.Assert(acquiredLock);
            return safeWaitHandle;
        }

        public static SafeWaitHandle? CreateNamedMutex(bool initiallyOwned, string name, out bool createdNew)
        {
            // For initially owned, newly created named mutexes, there is a potential race
            // between adding the mutex to the named object table and initially acquiring it.
            // To avoid the possibility of another thread retrieving the mutex via its name
            // before we managed to acquire it, we perform both steps while holding s_lock.
            s_lock.Acquire();
            bool holdingLock = true;
            try
            {
                WaitableObject? waitableObject = WaitableObject.CreateNamedMutex_Locked(name, out createdNew);
                if (waitableObject == null)
                {
                    return null;
                }
                SafeWaitHandle safeWaitHandle = NewHandle(waitableObject);
                if (!initiallyOwned || !createdNew)
                {
                    return safeWaitHandle;
                }

                // Acquire the mutex. A thread's <see cref="ThreadWaitInfo"/> has a reference to all <see cref="Mutex"/>es locked
                // by the thread. See <see cref="ThreadWaitInfo.LockedMutexesHead"/>. So, acquire the lock only after all
                // possibilities for exceptions have been exhausted.
                ThreadWaitInfo waitInfo = Thread.CurrentThread.WaitInfo;
                int status = waitableObject.Wait_Locked(waitInfo, timeoutMilliseconds: 0, interruptible: false, prioritize: false);
                Debug.Assert(status == 0);
                // Wait_Locked has already released s_lock, so we no longer hold it here.
                holdingLock = false;
                return safeWaitHandle;
            }
            finally
            {
                if (holdingLock)
                {
                    s_lock.Release();
                }
            }
        }

        public static OpenExistingResult OpenNamedMutex(string name, out SafeWaitHandle? result)
        {
            OpenExistingResult status = WaitableObject.OpenNamedMutex(name, out WaitableObject? mutex);
            result = status == OpenExistingResult.Success ? NewHandle(mutex!) : null;
            return status;
        }

        public static void DeleteHandle(IntPtr handle)
        {
            HandleManager.DeleteHandle(handle);
        }

        public static void SetEvent(IntPtr handle)
        {
            SetEvent(HandleManager.FromHandle(handle));
        }

        public static void SetEvent(WaitableObject waitableObject)
        {
            Debug.Assert(waitableObject != null);

            s_lock.Acquire();
            try
            {
                waitableObject.SignalEvent();
            }
            finally
            {
                s_lock.Release();
            }
        }

        public static void ResetEvent(IntPtr handle)
        {
            ResetEvent(HandleManager.FromHandle(handle));
        }

        public static void ResetEvent(WaitableObject waitableObject)
        {
            Debug.Assert(waitableObject != null);

            s_lock.Acquire();
            try
            {
                waitableObject.UnsignalEvent();
            }
            finally
            {
                s_lock.Release();
            }
        }

        public static int ReleaseSemaphore(IntPtr handle, int count)
        {
            Debug.Assert(count > 0);
            return ReleaseSemaphore(HandleManager.FromHandle(handle), count);
        }

        public static int ReleaseSemaphore(WaitableObject waitableObject, int count)
        {
            Debug.Assert(waitableObject != null);
            Debug.Assert(count > 0);

            s_lock.Acquire();
            try
            {
                return waitableObject.SignalSemaphore(count);
            }
            finally
            {
                s_lock.Release();
            }
        }

        public static void ReleaseMutex(IntPtr handle)
        {
            ReleaseMutex(HandleManager.FromHandle(handle));
        }

        public static void ReleaseMutex(WaitableObject waitableObject)
        {
            Debug.Assert(waitableObject != null);

            s_lock.Acquire();
            try
            {
                waitableObject.SignalMutex();
            }
            finally
            {
                s_lock.Release();
            }
        }

        public static int Wait(IntPtr handle, int timeoutMilliseconds, bool interruptible)
        {
            Debug.Assert(timeoutMilliseconds >= -1);
            return Wait(HandleManager.FromHandle(handle), timeoutMilliseconds, interruptible);
        }

        public static int Wait(
            WaitableObject waitableObject,
            int timeoutMilliseconds,
            bool interruptible = true,
            bool prioritize = false)
        {
            Debug.Assert(waitableObject != null);
            Debug.Assert(timeoutMilliseconds >= -1);

            return waitableObject.Wait(Thread.CurrentThread.WaitInfo, timeoutMilliseconds, interruptible, prioritize);
        }

        public static int Wait(
            Span<IntPtr> waitHandles,
            bool waitForAll,
            int timeoutMilliseconds)
        {
            Debug.Assert(waitHandles != null);
            Debug.Assert(waitHandles.Length > 0);
            Debug.Assert(waitHandles.Length <= WaitHandle.MaxWaitHandles);
            Debug.Assert(timeoutMilliseconds >= -1);

            ThreadWaitInfo waitInfo = Thread.CurrentThread.WaitInfo;
            WaitableObject?[] waitableObjects = waitInfo.GetWaitedObjectArray(waitHandles.Length);
            bool success = false;
            try
            {
                for (int i = 0; i < waitHandles.Length; ++i)
                {
                    Debug.Assert(waitHandles[i] != IntPtr.Zero);
                    WaitableObject waitableObject = HandleManager.FromHandle(waitHandles[i]);
                    if (waitForAll)
                    {
                        // Check if this is a duplicate, as wait-for-all does not support duplicates. Including the parent
                        // loop, this becomes a brute force O(n^2) search, which is intended since the typical array length is
                        // short enough that this would actually be faster than other alternatives. Also, the worst case is not
                        // so bad considering that the array length is limited by <see cref="WaitHandle.MaxWaitHandles"/>.
                        for (int j = 0; j < i; ++j)
                        {
                            if (waitableObject == waitableObjects[j])
                            {
                                throw new DuplicateWaitObjectException("waitHandles[" + i + ']');
                            }
                        }
                    }

                    waitableObjects[i] = waitableObject;
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    for (int i = 0; i < waitHandles.Length; ++i)
                    {
                        waitableObjects[i] = null;
                    }
                }
            }

            if (waitHandles.Length == 1)
            {
                WaitableObject waitableObject = waitableObjects[0]!;
                waitableObjects[0] = null;
                return
                    waitableObject.Wait(waitInfo, timeoutMilliseconds, interruptible: true, prioritize : false);
            }

            return
                WaitableObject.Wait(
                    waitableObjects,
                    waitHandles.Length,
                    waitForAll,
                    waitInfo,
                    timeoutMilliseconds,
                    interruptible: true,
                    prioritize: false);
        }

        public static int SignalAndWait(
            IntPtr handleToSignal,
            IntPtr handleToWaitOn,
            int timeoutMilliseconds)
        {
            Debug.Assert(timeoutMilliseconds >= -1);

            return
                SignalAndWait(
                    HandleManager.FromHandle(handleToSignal),
                    HandleManager.FromHandle(handleToWaitOn),
                    timeoutMilliseconds);
        }

        public static int SignalAndWait(
            WaitableObject waitableObjectToSignal,
            WaitableObject waitableObjectToWaitOn,
            int timeoutMilliseconds,
            bool interruptible = true,
            bool prioritize = false)
        {
            Debug.Assert(waitableObjectToSignal != null);
            Debug.Assert(waitableObjectToWaitOn != null);
            Debug.Assert(timeoutMilliseconds >= -1);

            ThreadWaitInfo waitInfo = Thread.CurrentThread.WaitInfo;
            bool waitCalled = false;
            s_lock.Acquire();
            try
            {
                // A pending interrupt does not signal the specified handle
                if (interruptible && waitInfo.CheckAndResetPendingInterrupt)
                {
                    throw new ThreadInterruptedException();
                }

                try
                {
                    waitableObjectToSignal.Signal(1);
                }
                catch (SemaphoreFullException ex)
                {
                    throw new InvalidOperationException(SR.Threading_WaitHandleTooManyPosts, ex);
                }
                waitCalled = true;
                return waitableObjectToWaitOn.Wait_Locked(waitInfo, timeoutMilliseconds, interruptible, prioritize);
            }
            finally
            {
                // Once the wait function is called, it will release the lock
                if (waitCalled)
                {
                    s_lock.VerifyIsNotLocked();
                }
                else
                {
                    s_lock.Release();
                }
            }
        }

        public static void UninterruptibleSleep0()
        {
            ThreadWaitInfo.UninterruptibleSleep0();
        }

        public static void Sleep(int timeoutMilliseconds, bool interruptible = true)
        {
            ThreadWaitInfo.Sleep(timeoutMilliseconds, interruptible);
        }

        public static void Interrupt(Thread thread)
        {
            Debug.Assert(thread != null);

            s_lock.Acquire();
            try
            {
                thread.WaitInfo.TrySignalToInterruptWaitOrRecordPendingInterrupt();
            }
            finally
            {
                s_lock.Release();
            }
        }
    }
}
