// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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

        private protected enum OpenExistingResult
        {
            Success,
            NameNotFound,
            PathNotFound,
            NameInvalid
        }

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

        protected WaitHandle()
        {
        }

        [Obsolete("Use the SafeWaitHandle property instead.")]
        public virtual IntPtr Handle
        {
            get
            {
                return _waitHandle == null ? InvalidHandle : _waitHandle.DangerousGetHandle();
            }
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

        public SafeWaitHandle? SafeWaitHandle // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/2384
        {
            get
            {
                if (_waitHandle == null)
                {
                    _waitHandle = new SafeWaitHandle(InvalidHandle, false);
                }
                return _waitHandle;
            }
            set
            {
                _waitHandle = value;
            }
        }

        internal static int ToTimeoutMilliseconds(TimeSpan timeout)
        {
            var timeoutMilliseconds = (long)timeout.TotalMilliseconds;
            if (timeoutMilliseconds < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }
            if (timeoutMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), SR.ArgumentOutOfRange_LessEqualToIntegerMaxVal);
            }
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
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }

            return WaitOneNoCheck(millisecondsTimeout);
        }

        private bool WaitOneNoCheck(int millisecondsTimeout)
        {
            Debug.Assert(millisecondsTimeout >= -1);

            // The field value is modifiable via the public <see cref="WaitHandle.SafeWaitHandle"/> property, save it locally
            // to ensure that one instance is used in all places in this method
            SafeWaitHandle waitHandle = _waitHandle ?? throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);

            bool success = false;
            try
            {
                int waitResult;

                waitHandle.DangerousAddRef(ref success);

                SynchronizationContext? context = SynchronizationContext.Current;
                if (context != null && context.IsWaitNotificationRequired())
                {
                    waitResult = context.Wait(new[] { waitHandle.DangerousGetHandle() }, false, millisecondsTimeout);
                }
                else
                {
                    waitResult = WaitOneCore(waitHandle.DangerousGetHandle(), millisecondsTimeout);
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
            if (safeWaitHandles == null || safeWaitHandles.Length < capacity)
            {
                // Always allocate at least 4 slots to prevent unnecessary reallocations
                safeWaitHandles = new SafeWaitHandle[Math.Max(capacity, 4)];
            }

            return safeWaitHandles;
        }

        private static void ReturnSafeWaitHandleArray(SafeWaitHandle?[]? safeWaitHandles)
            => t_safeWaitHandlesForRent = safeWaitHandles;

        /// <summary>
        /// Obtains all of the corresponding safe wait handles and adds a ref to each. Since the <see cref="SafeWaitHandle"/>
        /// property is publically modifiable, this makes sure that we add and release refs one the same set of safe wait
        /// handles to keep them alive during a multi-wait operation.
        /// </summary>
        private static void ObtainSafeWaitHandles(
            ReadOnlySpan<WaitHandle> waitHandles,
            Span<SafeWaitHandle?> safeWaitHandles,
            Span<IntPtr> unsafeWaitHandles)
        {
            Debug.Assert(waitHandles != null);
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
                        throw new ArgumentNullException("waitHandles[" + i + ']', SR.ArgumentNull_ArrayElement);
                    }

                    SafeWaitHandle safeWaitHandle = waitHandle._waitHandle ??
                        // Throw ObjectDisposedException for backward compatibility even though it is not representative of the issue
                        throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);

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
            if (waitHandles == null)
            {
                throw new ArgumentNullException(nameof(waitHandles), SR.ArgumentNull_Waithandles);
            }

            return WaitMultiple(new ReadOnlySpan<WaitHandle>(waitHandles), waitAll, millisecondsTimeout);
        }

        private static int WaitMultiple(ReadOnlySpan<WaitHandle> waitHandles, bool waitAll, int millisecondsTimeout)
        {
            if (waitHandles.Length == 0)
            {
                //
                // Some history: in CLR 1.0 and 1.1, we threw ArgumentException in this case, which was correct.
                // Somehow, in 2.0, this became ArgumentNullException.  This was not fixed until Silverlight 2,
                // which went back to ArgumentException.
                //
                // Now we're in a bit of a bind.  Backward-compatibility requires us to keep throwing ArgumentException
                // in CoreCLR, and ArgumentNullException in the desktop CLR.  This is ugly, but so is breaking
                // user code.
                //
                throw new ArgumentException(SR.Argument_EmptyWaithandleArray, nameof(waitHandles));
            }
            if (waitHandles.Length > MaxWaitHandles)
            {
                throw new NotSupportedException(SR.NotSupported_MaxWaitHandles);
            }
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }

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
                    if (safeWaitHandles[i] != null)
                    {
                        safeWaitHandles[i]!.DangerousRelease(); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/34644
                        safeWaitHandles[i] = null;
                    }
                }

                ReturnSafeWaitHandleArray(safeWaitHandles);
            }
        }

        private static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn, int millisecondsTimeout)
        {
            if (toSignal == null)
            {
                throw new ArgumentNullException(nameof(toSignal));
            }
            if (toWaitOn == null)
            {
                throw new ArgumentNullException(nameof(toWaitOn));
            }
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            }

            // The field value is modifiable via the public <see cref="WaitHandle.SafeWaitHandle"/> property, save it locally
            // to ensure that one instance is used in all places in this method
            SafeWaitHandle? safeWaitHandleToSignal = toSignal._waitHandle;
            SafeWaitHandle? safeWaitHandleToWaitOn = toWaitOn._waitHandle;
            if (safeWaitHandleToSignal == null || safeWaitHandleToWaitOn == null)
            {
                // Throw ObjectDisposedException for backward compatibility even though it is not be representative of the issue
                throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);
            }

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
