// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Principal;
using System.Runtime.Versioning;

namespace System.Threading
{
    public sealed partial class Thread : CriticalFinalizerObject
    {
        private static AsyncLocal<IPrincipal?>? s_asyncLocalPrincipal;

        [ThreadStatic]
        private static Thread? t_currentThread;

        // State associated with starting new thread
        private sealed class StartHelper
        {
            internal int _maxStackSize;
            internal Delegate _start;
            internal object? _startArg;
            internal CultureInfo? _culture;
            internal CultureInfo? _uiCulture;
            internal ExecutionContext? _executionContext;

            internal StartHelper(Delegate start)
            {
                _start = start;
            }

            internal static readonly ContextCallback s_threadStartContextCallback = new ContextCallback(Callback);

            private static void Callback(object? state)
            {
                Debug.Assert(state != null);
                ((StartHelper)state).RunWorker();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)] // avoid long-lived stack frame in many threads
            internal void Run()
            {
                if (_executionContext != null && !_executionContext.IsDefault)
                {
                    ExecutionContext.RunInternal(_executionContext, s_threadStartContextCallback, this);
                }
                else
                {
                    RunWorker();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)] // avoid long-lived stack frame in many threads
            private void RunWorker()
            {
                InitializeCulture();

                Delegate start = _start;
                _start = null!;

#if FEATURE_OBJCMARSHAL
                if (AutoreleasePool.EnableAutoreleasePool)
                    AutoreleasePool.CreateAutoreleasePool();
#endif

                if (start is ThreadStart threadStart)
                {
                    threadStart();
                }
                else
                {
                    ParameterizedThreadStart parameterizedThreadStart = (ParameterizedThreadStart)start;

                    object? startArg = _startArg;
                    _startArg = null;

                    parameterizedThreadStart(startArg);
                }

#if FEATURE_OBJCMARSHAL
                // There is no need to wrap this "clean up" code in a finally block since
                // if an exception is thrown above, the process is going to terminate.
                // Optimize for the most common case - no exceptions escape a thread.
                if (AutoreleasePool.EnableAutoreleasePool)
                    AutoreleasePool.DrainAutoreleasePool();
#endif
            }

            private void InitializeCulture()
            {
                if (_culture != null)
                {
                    CultureInfo.CurrentCulture = _culture;
                    _culture = null;
                }

                if (_uiCulture != null)
                {
                    CultureInfo.CurrentUICulture = _uiCulture;
                    _uiCulture = null;
                }
            }
        }

        public Thread(ThreadStart start)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            _startHelper = new StartHelper(start);

            Initialize();
        }

        public Thread(ThreadStart start, int maxStackSize)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            if (maxStackSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStackSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            _startHelper = new StartHelper(start) { _maxStackSize = maxStackSize };

            Initialize();
        }

        public Thread(ParameterizedThreadStart start)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            _startHelper = new StartHelper(start);

            Initialize();
        }

        public Thread(ParameterizedThreadStart start, int maxStackSize)
        {
            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            if (maxStackSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStackSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            _startHelper = new StartHelper(start) { _maxStackSize = maxStackSize };

            Initialize();
        }

#if !TARGET_BROWSER
        [UnsupportedOSPlatformGuard("browser")]
        internal static bool IsThreadStartSupported => true;

        /// <summary>Causes the operating system to change the state of the current instance to <see cref="ThreadState.Running"/>, and optionally supplies an object containing data to be used by the method the thread executes.</summary>
        /// <param name="parameter">An object that contains data to be used by the method the thread executes.</param>
        /// <exception cref="ThreadStateException">The thread has already been started.</exception>
        /// <exception cref="OutOfMemoryException">There is not enough memory available to start this thread.</exception>
        /// <exception cref="InvalidOperationException">This thread was created using a <see cref="ThreadStart"/> delegate instead of a <see cref="ParameterizedThreadStart"/> delegate.</exception>
        [UnsupportedOSPlatform("browser")]
        public void Start(object? parameter) => Start(parameter, captureContext: true);

        /// <summary>Causes the operating system to change the state of the current instance to <see cref="ThreadState.Running"/>, and optionally supplies an object containing data to be used by the method the thread executes.</summary>
        /// <param name="parameter">An object that contains data to be used by the method the thread executes.</param>
        /// <exception cref="ThreadStateException">The thread has already been started.</exception>
        /// <exception cref="OutOfMemoryException">There is not enough memory available to start this thread.</exception>
        /// <exception cref="InvalidOperationException">This thread was created using a <see cref="ThreadStart"/> delegate instead of a <see cref="ParameterizedThreadStart"/> delegate.</exception>
        /// <remarks>
        /// Unlike <see cref="Start"/>, which captures the current <see cref="ExecutionContext"/> and uses that context to invoke the thread's delegate,
        /// <see cref="UnsafeStart"/> explicitly avoids capturing the current context and flowing it to the invocation.
        /// </remarks>
        [UnsupportedOSPlatform("browser")]
        public void UnsafeStart(object? parameter) => Start(parameter, captureContext: false);

        private void Start(object? parameter, bool captureContext)
        {
            StartHelper? startHelper = _startHelper;

            // In the case of a null startHelper (second call to start on same thread)
            // StartCore method will take care of the error reporting.
            if (startHelper != null)
            {
                if (startHelper._start is ThreadStart)
                {
                    // We expect the thread to be setup with a ParameterizedThreadStart if this Start is called.
                    throw new InvalidOperationException(SR.InvalidOperation_ThreadWrongThreadStart);
                }

                startHelper._startArg = parameter;
                startHelper._executionContext = captureContext ? ExecutionContext.Capture() : null;
            }

            StartCore();
        }

        /// <summary>Causes the operating system to change the state of the current instance to <see cref="ThreadState.Running"/>.</summary>
        /// <exception cref="ThreadStateException">The thread has already been started.</exception>
        /// <exception cref="OutOfMemoryException">There is not enough memory available to start this thread.</exception>
        [UnsupportedOSPlatform("browser")]
        public void Start() => Start(captureContext: true);

        /// <summary>Causes the operating system to change the state of the current instance to <see cref="ThreadState.Running"/>.</summary>
        /// <exception cref="ThreadStateException">The thread has already been started.</exception>
        /// <exception cref="OutOfMemoryException">There is not enough memory available to start this thread.</exception>
        /// <remarks>
        /// Unlike <see cref="Start"/>, which captures the current <see cref="ExecutionContext"/> and uses that context to invoke the thread's delegate,
        /// <see cref="UnsafeStart"/> explicitly avoids capturing the current context and flowing it to the invocation.
        /// </remarks>
        [UnsupportedOSPlatform("browser")]
        public void UnsafeStart() => Start(captureContext: false);

        private void Start(bool captureContext)
        {
            StartHelper? startHelper = _startHelper;

            // In the case of a null startHelper (second call to start on same thread)
            // StartCore method will take care of the error reporting.
            if (startHelper != null)
            {
                startHelper._startArg = null;
                startHelper._executionContext = captureContext ? ExecutionContext.Capture() : null;
            }

            StartCore();
        }
#endif

        private void RequireCurrentThread()
        {
            if (this != CurrentThread)
            {
                throw new InvalidOperationException(SR.Thread_Operation_RequiresCurrentThread);
            }
        }

        private void SetCultureOnUnstartedThread(CultureInfo value, bool uiCulture)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            StartHelper? startHelper = _startHelper;

            // This check is best effort to catch common user errors only. It won't catch all posssible race
            // conditions between setting culture on unstarted thread and starting the thread.
            if ((ThreadState & ThreadState.Unstarted) == 0)
            {
                throw new InvalidOperationException(SR.Thread_Operation_RequiresCurrentThread);
            }

            Debug.Assert(startHelper != null);

            if (uiCulture)
            {
                startHelper._uiCulture = value;
            }
            else
            {
                startHelper._culture = value;
            }
        }

        partial void ThreadNameChanged(string? value);

        public CultureInfo CurrentCulture
        {
            get
            {
                RequireCurrentThread();
                return CultureInfo.CurrentCulture;
            }
            set
            {
                if (this != CurrentThread)
                {
                    SetCultureOnUnstartedThread(value, uiCulture: false);
                    return;
                }
                CultureInfo.CurrentCulture = value;
            }
        }

        public CultureInfo CurrentUICulture
        {
            get
            {
                RequireCurrentThread();
                return CultureInfo.CurrentUICulture;
            }
            set
            {
                if (this != CurrentThread)
                {
                    SetCultureOnUnstartedThread(value, uiCulture: true);
                    return;
                }
                CultureInfo.CurrentUICulture = value;
            }
        }

        public static IPrincipal? CurrentPrincipal
        {
            get
            {
                IPrincipal? principal = s_asyncLocalPrincipal?.Value;
                if (principal is null)
                {
                    CurrentPrincipal = (principal = AppDomain.CurrentDomain.GetThreadPrincipal());
                }
                return principal;
            }
            set
            {
                if (s_asyncLocalPrincipal is null)
                {
                    if (value is null)
                    {
                        return;
                    }
                    Interlocked.CompareExchange(ref s_asyncLocalPrincipal, new AsyncLocal<IPrincipal?>(), null);
                }
                s_asyncLocalPrincipal.Value = value;
            }
        }

        public static Thread CurrentThread
        {
            [Intrinsic]
            get
            {
                return t_currentThread ?? InitializeCurrentThread();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Slow path method. Make sure that the caller frame does not pay for PInvoke overhead.
        public static void Sleep(int millisecondsTimeout)
        {
            if (millisecondsTimeout < Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), millisecondsTimeout, SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            SleepInternal(millisecondsTimeout);
        }

#if !CORERT
        /// <summary>Returns the operating system identifier for the current thread.</summary>
        internal static ulong CurrentOSThreadId => GetCurrentOSThreadId();
#endif

        public ExecutionContext? ExecutionContext => ExecutionContext.Capture();

        public string? Name
        {
            get => _name;
            set
            {
                lock (this)
                {
                    if (_name != value)
                    {
                        _name = value;
                        ThreadNameChanged(value);
                        _mayNeedResetForThreadPool = true;
                    }
                }
            }
        }

        internal void SetThreadPoolWorkerThreadName()
        {
            Debug.Assert(this == CurrentThread);
            Debug.Assert(IsThreadPoolThread);

            lock (this)
            {
                _name = ThreadPool.WorkerThreadName;
                ThreadNameChanged(ThreadPool.WorkerThreadName);
            }
        }

#if !CORECLR
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResetThreadPoolThread()
        {
            Debug.Assert(this == CurrentThread);
            Debug.Assert(!IsThreadStartSupported || IsThreadPoolThread); // there are no dedicated threadpool threads on runtimes where we can't start threads

            if (_mayNeedResetForThreadPool)
            {
                ResetThreadPoolThreadSlow();
            }
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResetThreadPoolThreadSlow()
        {
            Debug.Assert(this == CurrentThread);
            Debug.Assert(!IsThreadStartSupported || IsThreadPoolThread); // there are no dedicated threadpool threads on runtimes where we can't start threads
            Debug.Assert(_mayNeedResetForThreadPool);

            _mayNeedResetForThreadPool = false;

            if (_name != ThreadPool.WorkerThreadName)
            {
                SetThreadPoolWorkerThreadName();
            }

            if (!IsBackground)
            {
                IsBackground = true;
            }

            if (Priority != ThreadPriority.Normal)
            {
                Priority = ThreadPriority.Normal;
            }
        }

        [Obsolete(Obsoletions.ThreadAbortMessage, DiagnosticId = Obsoletions.ThreadAbortDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void Abort()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadAbort);
        }

        [Obsolete(Obsoletions.ThreadAbortMessage, DiagnosticId = Obsoletions.ThreadAbortDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void Abort(object? stateInfo)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadAbort);
        }

        [Obsolete(Obsoletions.ThreadAbortMessage, DiagnosticId = Obsoletions.ThreadAbortDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void ResetAbort()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadAbort);
        }

        [Obsolete("Thread.Suspend has been deprecated. Use other classes in System.Threading, such as Monitor, Mutex, Event, and Semaphore, to synchronize Threads or protect resources.")]
        public void Suspend()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadSuspend);
        }

        [Obsolete("Thread.Resume has been deprecated. Use other classes in System.Threading, such as Monitor, Mutex, Event, and Semaphore, to synchronize Threads or protect resources.")]
        public void Resume()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ThreadSuspend);
        }

        // Currently, no special handling is done for critical regions, and no special handling is necessary to ensure thread
        // affinity. If that changes, the relevant functions would instead need to delegate to RuntimeThread.
        public static void BeginCriticalRegion() { }
        public static void EndCriticalRegion() { }
        public static void BeginThreadAffinity() { }
        public static void EndThreadAffinity() { }

        public static LocalDataStoreSlot AllocateDataSlot() => LocalDataStore.AllocateSlot();
        public static LocalDataStoreSlot AllocateNamedDataSlot(string name) => LocalDataStore.AllocateNamedSlot(name);
        public static LocalDataStoreSlot GetNamedDataSlot(string name) => LocalDataStore.GetNamedSlot(name);
        public static void FreeNamedDataSlot(string name) => LocalDataStore.FreeNamedSlot(name);
        public static object? GetData(LocalDataStoreSlot slot) => LocalDataStore.GetData(slot);
        public static void SetData(LocalDataStoreSlot slot, object? data) => LocalDataStore.SetData(slot, data);

        [Obsolete("The ApartmentState property has been deprecated. Use GetApartmentState, SetApartmentState or TrySetApartmentState.")]
        public ApartmentState ApartmentState
        {
            get => GetApartmentState();
            set => TrySetApartmentState(value);
        }

        [SupportedOSPlatform("windows")]
        public void SetApartmentState(ApartmentState state)
        {
            SetApartmentState(state, throwOnError:true);
        }

        public bool TrySetApartmentState(ApartmentState state)
        {
            return SetApartmentState(state, throwOnError:false);
        }

        private bool SetApartmentState(ApartmentState state, bool throwOnError)
        {
            switch (state)
            {
                case ApartmentState.STA:
                case ApartmentState.MTA:
                case ApartmentState.Unknown:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state), SR.ArgumentOutOfRange_Enum);
            }

            return SetApartmentStateUnchecked(state, throwOnError);
        }

        [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public CompressedStack GetCompressedStack()
        {
            throw new InvalidOperationException(SR.Thread_GetSetCompressedStack_NotSupported);
        }

        [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public void SetCompressedStack(CompressedStack stack)
        {
            throw new InvalidOperationException(SR.Thread_GetSetCompressedStack_NotSupported);
        }

        public static AppDomain GetDomain() => AppDomain.CurrentDomain;
        public static int GetDomainID() => 1;
        public override int GetHashCode() => ManagedThreadId;
        public void Join() => Join(-1);
        public bool Join(TimeSpan timeout) => Join(WaitHandle.ToTimeoutMilliseconds(timeout));
        public static void MemoryBarrier() => Interlocked.MemoryBarrier();
        public static void Sleep(TimeSpan timeout) => Sleep(WaitHandle.ToTimeoutMilliseconds(timeout));

        public static byte VolatileRead(ref byte address) => Volatile.Read(ref address);
        public static double VolatileRead(ref double address) => Volatile.Read(ref address);
        public static short VolatileRead(ref short address) => Volatile.Read(ref address);
        public static int VolatileRead(ref int address) => Volatile.Read(ref address);
        public static long VolatileRead(ref long address) => Volatile.Read(ref address);
        public static IntPtr VolatileRead(ref IntPtr address) => Volatile.Read(ref address);
        [return: NotNullIfNotNull("address")]
        public static object? VolatileRead([NotNullIfNotNull("address")] ref object? address) => Volatile.Read(ref address);
        [CLSCompliant(false)]
        public static sbyte VolatileRead(ref sbyte address) => Volatile.Read(ref address);
        public static float VolatileRead(ref float address) => Volatile.Read(ref address);
        [CLSCompliant(false)]
        public static ushort VolatileRead(ref ushort address) => Volatile.Read(ref address);
        [CLSCompliant(false)]
        public static uint VolatileRead(ref uint address) => Volatile.Read(ref address);
        [CLSCompliant(false)]
        public static ulong VolatileRead(ref ulong address) => Volatile.Read(ref address);
        [CLSCompliant(false)]
        public static UIntPtr VolatileRead(ref UIntPtr address) => Volatile.Read(ref address);
        public static void VolatileWrite(ref byte address, byte value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref double address, double value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref short address, short value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref int address, int value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref long address, long value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref IntPtr address, IntPtr value) => Volatile.Write(ref address, value);
        public static void VolatileWrite([NotNullIfNotNull("value")] ref object? address, object? value) => Volatile.Write(ref address, value);
        [CLSCompliant(false)]
        public static void VolatileWrite(ref sbyte address, sbyte value) => Volatile.Write(ref address, value);
        public static void VolatileWrite(ref float address, float value) => Volatile.Write(ref address, value);
        [CLSCompliant(false)]
        public static void VolatileWrite(ref ushort address, ushort value) => Volatile.Write(ref address, value);
        [CLSCompliant(false)]
        public static void VolatileWrite(ref uint address, uint value) => Volatile.Write(ref address, value);
        [CLSCompliant(false)]
        public static void VolatileWrite(ref ulong address, ulong value) => Volatile.Write(ref address, value);
        [CLSCompliant(false)]
        public static void VolatileWrite(ref UIntPtr address, UIntPtr value) => Volatile.Write(ref address, value);

        /// <summary>
        /// Manages functionality required to support members of <see cref="Thread"/> dealing with thread-local data
        /// </summary>
        private static class LocalDataStore
        {
            private static Dictionary<string, LocalDataStoreSlot>? s_nameToSlotMap;

            public static LocalDataStoreSlot AllocateSlot()
            {
                return new LocalDataStoreSlot(new ThreadLocal<object?>());
            }

            private static Dictionary<string, LocalDataStoreSlot> EnsureNameToSlotMap()
            {
                Dictionary<string, LocalDataStoreSlot>? nameToSlotMap = s_nameToSlotMap;
                if (nameToSlotMap != null)
                {
                    return nameToSlotMap;
                }

                nameToSlotMap = new Dictionary<string, LocalDataStoreSlot>();
                return Interlocked.CompareExchange(ref s_nameToSlotMap, nameToSlotMap, null) ?? nameToSlotMap;
            }

            public static LocalDataStoreSlot AllocateNamedSlot(string name)
            {
                LocalDataStoreSlot slot = AllocateSlot();
                Dictionary<string, LocalDataStoreSlot> nameToSlotMap = EnsureNameToSlotMap();
                lock (nameToSlotMap)
                {
                    nameToSlotMap.Add(name, slot);
                }
                return slot;
            }

            public static LocalDataStoreSlot GetNamedSlot(string name)
            {
                Dictionary<string, LocalDataStoreSlot> nameToSlotMap = EnsureNameToSlotMap();
                lock (nameToSlotMap)
                {
                    if (!nameToSlotMap.TryGetValue(name, out LocalDataStoreSlot? slot))
                    {
                        slot = AllocateSlot();
                        nameToSlotMap[name] = slot;
                    }
                    return slot;
                }
            }

            public static void FreeNamedSlot(string name)
            {
                Dictionary<string, LocalDataStoreSlot> nameToSlotMap = EnsureNameToSlotMap();
                lock (nameToSlotMap)
                {
                    nameToSlotMap.Remove(name);
                }
            }

            private static ThreadLocal<object?> GetThreadLocal(LocalDataStoreSlot slot)
            {
                if (slot == null)
                {
                    throw new ArgumentNullException(nameof(slot));
                }

                Debug.Assert(slot.Data != null);
                return slot.Data;
            }

            public static object? GetData(LocalDataStoreSlot slot)
            {
                return GetThreadLocal(slot).Value;
            }

            public static void SetData(LocalDataStoreSlot slot, object? value)
            {
                GetThreadLocal(slot).Value = value;
            }
        }
    }
}
