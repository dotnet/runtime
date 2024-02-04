// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public abstract partial class WaitHandle : MarshalByRefObject, IDisposable
    {
        internal const int MaxWaitHandles = 64;

        protected static readonly IntPtr InvalidHandle = new IntPtr(-1);

        // IMPORTANT:
        // - Do not add or rearrange fields as the EE depends on this layout.

        private SafeWaitHandle? _waitHandle;

        [ThreadStatic]
        private static SafeWaitHandle?[]? t_safeWaitHandlesForRent;

        // The wait result values below match Win32 wait result codes (WAIT_OBJECT_0,
        // WAIT_ABANDONED, WAIT_TIMEOUT).

        // Successful wait on first object. When waiting for multiple objects the
        // return value is (WaitSuccess + waitIndex).
        internal const int WaitSuccess = 0;

        // The specified object is a mutex object that was not released by the
        // thread that owned the mutex object before the owning thread terminated.
        // When waiting for multiple objects the return value is (WaitAbandoned +
        // waitIndex).
        internal const int WaitAbandoned = 0x80;

        public const int WaitTimeout = 0x102;
        internal const int WaitFailed = unchecked((int)0xffffffff);

        protected WaitHandle()
        {
        }

        [Obsolete("WaitHandle.Handle has been deprecated. Use the SafeWaitHandle property instead.")]
        public virtual IntPtr Handle
        {
            get => _waitHandle == null ? InvalidHandle : _waitHandle.DangerousGetHandle();
            set
            {
                if (value == InvalidHandle)
                {
                    // This line leaks a handle.  However, it's currently
                    // not perfectly clear what the right behavior is here
                    // anyways.  This preserves Everett behavior.  We should
                    // ideally do these things:
                    // *) Expose a settable SafeHandle property on WaitHandle.
                    // *) Expose a settable OwnsHandle property on SafeHandle.
                    if (_waitHandle != null)
                    {
                        _waitHandle.SetHandleAsInvalid();
                        _waitHandle = null;
                    }
                }
                else
                {
                    _waitHandle = new SafeWaitHandle(value, true);
                }
            }
        }

        [AllowNull]
        public SafeWaitHandle SafeWaitHandle
        {
            get => _waitHandle ??= new SafeWaitHandle(InvalidHandle, false);
            set => _waitHandle = value;
        }

        internal static int ToTimeoutMilliseconds(TimeSpan timeout)
        {
            long timeoutMilliseconds = (long)timeout.TotalMilliseconds;
            ArgumentOutOfRangeException.ThrowIfLessThan(timeoutMilliseconds, -1, nameof(timeout));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(timeoutMilliseconds, int.MaxValue, nameof(timeout));
            return (int)timeoutMilliseconds;
        }

        public virtual void Close() => Dispose();

        protected virtual void Dispose(bool explicitDisposing)
        {
            _waitHandle?.Close();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual bool WaitOne(int millisecondsTimeout)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            return WaitOneNoCheck(millisecondsTimeout);
        }

        internal bool WaitOneNoCheck(
            int millisecondsTimeout,
            bool useTrivialWaits = false,
            object? associatedObject = null,
            NativeRuntimeEventSource.WaitHandleWaitSourceMap waitSource = NativeRuntimeEventSource.WaitHandleWaitSourceMap.Unknown)
        {
            Debug.Assert(millisecondsTimeout >= -1);

            // The field value is modifiable via the public <see cref="WaitHandle.SafeWaitHandle"/> property, save it locally
            // to ensure that one instance is used in all places in this method
            SafeWaitHandle? waitHandle = _waitHandle;
            ObjectDisposedException.ThrowIf(waitHandle is null, this);

            bool success = false;
            try
            {
                waitHandle.DangerousAddRef(ref success);

                int waitResult = WaitFailed;

                // Check if the wait should be forwarded to a SynchronizationContext wait override. Trivial waits don't allow
                // reentrance or interruption, and are not forwarded.
                bool usedSyncContextWait = false;
                if (!useTrivialWaits)
                {
                    SynchronizationContext? context = SynchronizationContext.Current;
                    if (context != null && context.IsWaitNotificationRequired())
                    {
                        usedSyncContextWait = true;
                        waitResult = context.Wait(new[] { waitHandle.DangerousGetHandle() }, false, millisecondsTimeout);
                    }
                }

                if (!usedSyncContextWait)
                {
#if !CORECLR // CoreCLR sends the wait events from the native side
                    bool sendWaitEvents =
                        millisecondsTimeout != 0 &&
                        !useTrivialWaits &&
                        NativeRuntimeEventSource.Log.IsEnabled(
                            EventLevel.Verbose,
                            NativeRuntimeEventSource.Keywords.WaitHandleKeyword);

                    // Monitor.Wait is typically a blocking wait. For other waits, when sending the wait events try a
                    // nonblocking wait first such that the events sent are more likely to represent blocking waits.
                    bool tryNonblockingWaitFirst =
                        sendWaitEvents &&
                        waitSource != NativeRuntimeEventSource.WaitHandleWaitSourceMap.MonitorWait;
                    if (tryNonblockingWaitFirst)
                    {
                        waitResult = WaitOneCore(waitHandle.DangerousGetHandle(), 0 /* millisecondsTimeout */, useTrivialWaits);
                        if (waitResult == WaitTimeout)
                        {
                            // Do a full wait and send the wait events
                            tryNonblockingWaitFirst = false;
                        }
                        else
                        {
                            // The nonblocking wait was successful, don't send the wait events
                            sendWaitEvents = false;
                        }
                    }

                    if (sendWaitEvents)
                    {
                        NativeRuntimeEventSource.Log.WaitHandleWaitStart(waitSource, associatedObject ?? this);
                    }

                    // When tryNonblockingWaitFirst is true, we have a final wait result from the nonblocking wait above
                    if (!tryNonblockingWaitFirst)
#endif
                    {
                        waitResult = WaitOneCore(waitHandle.DangerousGetHandle(), millisecondsTimeout, useTrivialWaits);
                    }

#if !CORECLR // CoreCLR sends the wait events from the native side
                    if (sendWaitEvents)
                    {
                        NativeRuntimeEventSource.Log.WaitHandleWaitStop();
                    }
#endif
                }

                if (waitResult == WaitAbandoned)
                {
                    throw new AbandonedMutexException();
                }

                return waitResult != WaitTimeout;
            }
            finally
            {
                if (success)
                    waitHandle.DangerousRelease();
            }
        }

        // Returns an array for storing SafeWaitHandles in WaitMultiple calls. The array
        // is reused for subsequent calls to reduce GC pressure.
        private static SafeWaitHandle?[] RentSafeWaitHandleArray(int capacity)
        {
            SafeWaitHandle?[]? safeWaitHandles = t_safeWaitHandlesForRent;

            t_safeWaitHandlesForRent = null;

            // t_safeWaitHandlesForRent can be null when it was not initialized yet or
            // if a re-entrant wait is performed and the array is already rented. In
            // that case we just allocate a new one and reuse it as necessary.
            int currentLength = (safeWaitHandles != null) ? safeWaitHandles.Length : 0;
            if (currentLength < capacity)
            {
                safeWaitHandles = new SafeWaitHandle[Math.Max(capacity,
                    Math.Min(MaxWaitHandles, 2 * currentLength))];
            }

            return safeWaitHandles!;
        }

        private static void ReturnSafeWaitHandleArray(SafeWaitHandle?[]? safeWaitHandles)
            => t_safeWaitHandlesForRent = safeWaitHandles;

        /// <summary>
        /// Obtains all of the corresponding safe wait handles and adds a ref to each. Since the <see cref="SafeWaitHandle"/>
        /// property is publicly modifiable, this makes sure that we add and release refs one the same set of safe wait
        /// handles to keep them alive during a multi-wait operation.
        /// </summary>
        private static void ObtainSafeWaitHandles(
            ReadOnlySpan<WaitHandle> waitHandles,
            Span<SafeWaitHandle?> safeWaitHandles,
            Span<IntPtr> unsafeWaitHandles)
        {
            Debug.Assert(waitHandles.Length > 0);
            Debug.Assert(waitHandles.Length <= MaxWaitHandles);

            bool lastSuccess = true;
            SafeWaitHandle? lastSafeWaitHandle = null;
            try
            {
                for (int i = 0; i < waitHandles.Length; ++i)
                {
                    WaitHandle waitHandle = waitHandles[i];
                    if (waitHandle == null)
                    {
                        throw new ArgumentNullException($"waitHandles[{i}]", SR.ArgumentNull_ArrayElement);
                    }

                    SafeWaitHandle? safeWaitHandle = waitHandle._waitHandle;
                    ObjectDisposedException.ThrowIf(safeWaitHandle is null, waitHandle); // throw ObjectDisposedException for backward compatibility even though it is not representative of the issue

                    lastSafeWaitHandle = safeWaitHandle;
                    lastSuccess = false;
                    safeWaitHandle.DangerousAddRef(ref lastSuccess);
                    safeWaitHandles[i] = safeWaitHandle;
                    unsafeWaitHandles[i] = safeWaitHandle.DangerousGetHandle();
                }
            }
            catch
            {
                for (int i = 0; i < waitHandles.Length; ++i)
                {
                    SafeWaitHandle? safeWaitHandle = safeWaitHandles[i];
                    if (safeWaitHandle == null)
                    {
                        break;
                    }
                    safeWaitHandle.DangerousRelease();
                    safeWaitHandles[i] = null;
                    if (safeWaitHandle == lastSafeWaitHandle)
                    {
                        lastSafeWaitHandle = null;
                        lastSuccess = true;
                    }
                }

                if (!lastSuccess)
                {
                    Debug.Assert(lastSafeWaitHandle != null);
                    lastSafeWaitHandle.DangerousRelease();
                }

                throw;
            }
        }

        private static int WaitMultiple(WaitHandle[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            ArgumentNullException.ThrowIfNull(waitHandles);

            return WaitMultiple(new ReadOnlySpan<WaitHandle>(waitHandles), waitAll, millisecondsTimeout);
        }

        private static int WaitMultiple(ReadOnlySpan<WaitHandle> waitHandles, bool waitAll, int millisecondsTimeout)
        {
            if (waitHandles.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyWaithandleArray, nameof(waitHandles));
            }
            if (waitHandles.Length > MaxWaitHandles)
            {
                throw new NotSupportedException(SR.NotSupported_MaxWaitHandles);
            }
            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            SynchronizationContext? context = SynchronizationContext.Current;
            bool useWaitContext = context != null && context.IsWaitNotificationRequired();
            SafeWaitHandle?[]? safeWaitHandles = RentSafeWaitHandleArray(waitHandles.Length);

            try
            {
                int waitResult;

                if (useWaitContext)
                {
                    IntPtr[] unsafeWaitHandles = new IntPtr[waitHandles.Length];
                    ObtainSafeWaitHandles(waitHandles, safeWaitHandles, unsafeWaitHandles);
                    waitResult = context!.Wait(unsafeWaitHandles, waitAll, millisecondsTimeout);
                }
                else
                {
                    Span<IntPtr> unsafeWaitHandles = stackalloc IntPtr[waitHandles.Length];
                    ObtainSafeWaitHandles(waitHandles, safeWaitHandles, unsafeWaitHandles);
                    waitResult = WaitMultipleIgnoringSyncContext(unsafeWaitHandles, waitAll, millisecondsTimeout);
                }

                if (waitResult >= WaitAbandoned && waitResult < WaitAbandoned + waitHandles.Length)
                {
                    if (waitAll)
                    {
                        // In the case of WaitAll the OS will only provide the information that mutex was abandoned.
                        // It won't tell us which one.  So we can't set the Index or provide access to the Mutex
                        throw new AbandonedMutexException();
                    }

                    waitResult -= WaitAbandoned;
                    throw new AbandonedMutexException(waitResult, waitHandles[waitResult]);
                }

                return waitResult;
            }
            finally
            {
                for (int i = 0; i < waitHandles.Length; ++i)
                {
                    if (safeWaitHandles[i] is SafeWaitHandle swh)
                    {
                        swh.DangerousRelease();
                        safeWaitHandles[i] = null;
                    }
                }

                ReturnSafeWaitHandleArray(safeWaitHandles);
            }
        }

        private static int WaitAnyMultiple(ReadOnlySpan<SafeWaitHandle> safeWaitHandles, int millisecondsTimeout)
        {
            // - Callers are expected to manage the lifetimes of the safe wait handles such that they would not expire during
            //   this wait
            // - If the safe wait handle that satisfies the wait is an abandoned mutex, the wait result would reflect that and
            //   handling of that is left up to the caller

            Debug.Assert(safeWaitHandles.Length != 0);
            Debug.Assert(safeWaitHandles.Length <= MaxWaitHandles);
            Debug.Assert(millisecondsTimeout >= -1);

            SynchronizationContext? context = SynchronizationContext.Current;
            bool useWaitContext = context != null && context.IsWaitNotificationRequired();

            int waitResult;
            if (useWaitContext)
            {
                IntPtr[] unsafeWaitHandles = new IntPtr[safeWaitHandles.Length];
                for (int i = 0; i < safeWaitHandles.Length; ++i)
                {
                    Debug.Assert(safeWaitHandles[i] != null);
                    unsafeWaitHandles[i] = safeWaitHandles[i].DangerousGetHandle();
                }
                waitResult = context!.Wait(unsafeWaitHandles, false, millisecondsTimeout);
            }
            else
            {
                Span<IntPtr> unsafeWaitHandles = stackalloc IntPtr[safeWaitHandles.Length];
                for (int i = 0; i < safeWaitHandles.Length; ++i)
                {
                    Debug.Assert(safeWaitHandles[i] != null);
                    unsafeWaitHandles[i] = safeWaitHandles[i].DangerousGetHandle();
                }
                waitResult = WaitMultipleIgnoringSyncContext(unsafeWaitHandles, false, millisecondsTimeout);
            }

            return waitResult;
        }

        internal static int WaitMultipleIgnoringSyncContext(Span<IntPtr> handles, bool waitAll, int millisecondsTimeout)
        {
            int waitResult = WaitFailed;

#if !CORECLR // CoreCLR sends the wait events from the native side
            bool sendWaitEvents =
                millisecondsTimeout != 0 &&
                NativeRuntimeEventSource.Log.IsEnabled(
                    EventLevel.Verbose,
                    NativeRuntimeEventSource.Keywords.WaitHandleKeyword);

            // When sending the wait events try a nonblocking wait first such that the events sent are more likely to
            // represent blocking waits
            bool tryNonblockingWaitFirst = sendWaitEvents;
            if (tryNonblockingWaitFirst)
            {
                waitResult = WaitMultipleIgnoringSyncContextCore(handles, waitAll, millisecondsTimeout: 0);
                if (waitResult == WaitTimeout)
                {
                    // Do a full wait and send the wait events
                    tryNonblockingWaitFirst = false;
                }
                else
                {
                    // The nonblocking wait was successful, don't send the wait events
                    sendWaitEvents = false;
                }
            }

            if (sendWaitEvents)
            {
                NativeRuntimeEventSource.Log.WaitHandleWaitStart();
            }

            // When tryNonblockingWaitFirst is true, we have a final wait result from the nonblocking wait above
            if (!tryNonblockingWaitFirst)
#endif
            {
                waitResult = WaitMultipleIgnoringSyncContextCore(handles, waitAll, millisecondsTimeout);
            }

#if !CORECLR // CoreCLR sends the wait events from the native side
            if (sendWaitEvents)
            {
                NativeRuntimeEventSource.Log.WaitHandleWaitStop();
            }
#endif

            return waitResult;
        }

        private static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn, int millisecondsTimeout)
        {
            ArgumentNullException.ThrowIfNull(toSignal);
            ArgumentNullException.ThrowIfNull(toWaitOn);

            ArgumentOutOfRangeException.ThrowIfLessThan(millisecondsTimeout, -1);

            // The field value is modifiable via the public <see cref="WaitHandle.SafeWaitHandle"/> property, save it locally
            // to ensure that one instance is used in all places in this method
            SafeWaitHandle? safeWaitHandleToSignal = toSignal._waitHandle;
            SafeWaitHandle? safeWaitHandleToWaitOn = toWaitOn._waitHandle;
            ObjectDisposedException.ThrowIf(safeWaitHandleToSignal is null, toSignal); // throw ObjectDisposedException for backward compatibility even though it is not representative of the issue
            ObjectDisposedException.ThrowIf(safeWaitHandleToWaitOn is null, toWaitOn);

            bool successSignal = false, successWait = false;
            try
            {
                safeWaitHandleToSignal.DangerousAddRef(ref successSignal);
                safeWaitHandleToWaitOn.DangerousAddRef(ref successWait);

                int ret = SignalAndWaitCore(
                    safeWaitHandleToSignal.DangerousGetHandle(),
                    safeWaitHandleToWaitOn.DangerousGetHandle(),
                    millisecondsTimeout);

                if (ret == WaitAbandoned)
                {
                    throw new AbandonedMutexException();
                }

                return ret != WaitTimeout;
            }
            finally
            {
                if (successWait)
                {
                    safeWaitHandleToWaitOn.DangerousRelease();
                }
                if (successSignal)
                {
                    safeWaitHandleToSignal.DangerousRelease();
                }
            }
        }

        internal static void ThrowInvalidHandleException()
        {
            var ex = new InvalidOperationException(SR.InvalidOperation_InvalidHandle);
            ex.HResult = HResults.E_HANDLE;
            throw ex;
        }

        public virtual bool WaitOne(TimeSpan timeout) => WaitOneNoCheck(ToTimeoutMilliseconds(timeout));
        public virtual bool WaitOne() => WaitOneNoCheck(-1);
        public virtual bool WaitOne(int millisecondsTimeout, bool exitContext) => WaitOne(millisecondsTimeout);
        public virtual bool WaitOne(TimeSpan timeout, bool exitContext) => WaitOneNoCheck(ToTimeoutMilliseconds(timeout));

        public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout) =>
            WaitMultiple(waitHandles, true, millisecondsTimeout) != WaitTimeout;
        public static bool WaitAll(WaitHandle[] waitHandles, TimeSpan timeout) =>
            WaitMultiple(waitHandles, true, ToTimeoutMilliseconds(timeout)) != WaitTimeout;
        public static bool WaitAll(WaitHandle[] waitHandles) =>
            WaitMultiple(waitHandles, true, -1) != WaitTimeout;
        public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext) =>
            WaitMultiple(waitHandles, true, millisecondsTimeout) != WaitTimeout;
        public static bool WaitAll(WaitHandle[] waitHandles, TimeSpan timeout, bool exitContext) =>
            WaitMultiple(waitHandles, true, ToTimeoutMilliseconds(timeout)) != WaitTimeout;

        public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout) =>
            WaitMultiple(waitHandles, false, millisecondsTimeout);
        internal static int WaitAny(ReadOnlySpan<SafeWaitHandle> safeWaitHandles, int millisecondsTimeout) =>
            WaitAnyMultiple(safeWaitHandles, millisecondsTimeout);
        internal static int WaitAny(ReadOnlySpan<WaitHandle> waitHandles, int millisecondsTimeout) =>
            WaitMultiple(waitHandles, false, millisecondsTimeout);
        public static int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout) =>
            WaitMultiple(waitHandles, false, ToTimeoutMilliseconds(timeout));
        public static int WaitAny(WaitHandle[] waitHandles) =>
            WaitMultiple(waitHandles, false, -1);
        public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext) =>
            WaitMultiple(waitHandles, false, millisecondsTimeout);
        public static int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout, bool exitContext) =>
            WaitMultiple(waitHandles, false, ToTimeoutMilliseconds(timeout));

        public static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn) =>
            SignalAndWait(toSignal, toWaitOn, -1);
        public static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn, TimeSpan timeout, bool exitContext) =>
            SignalAndWait(toSignal, toWaitOn, ToTimeoutMilliseconds(timeout));
        public static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn, int millisecondsTimeout, bool exitContext) =>
            SignalAndWait(toSignal, toWaitOn, millisecondsTimeout);
    }
}
