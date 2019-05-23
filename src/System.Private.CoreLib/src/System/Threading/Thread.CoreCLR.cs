// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Threading
{
    internal sealed class ThreadHelper
    {
        private Delegate _start;
        internal CultureInfo? _startCulture;
        internal CultureInfo? _startUICulture;
        private object? _startArg = null;
        private ExecutionContext? _executionContext = null;

        internal ThreadHelper(Delegate start)
        {
            _start = start; 
        }

        internal void SetExecutionContextHelper(ExecutionContext? ec)
        {
            _executionContext = ec;
        }

        internal static readonly ContextCallback s_threadStartContextCallback = new ContextCallback(ThreadStart_Context);

        private static void ThreadStart_Context(object? state)
        {
            Debug.Assert(state is ThreadHelper);
            ThreadHelper t = (ThreadHelper)state;

            t.InitializeCulture();

            Debug.Assert(t._start is ThreadStart || t._start is ParameterizedThreadStart);
            if (t._start is ThreadStart threadStart)
            {
                threadStart();
            }
            else
            {
                ((ParameterizedThreadStart)t._start)(t._startArg);
            }
        }

        private void InitializeCulture()
        {
            if (_startCulture != null)
            {
                CultureInfo.CurrentCulture = _startCulture;
                _startCulture = null;
            }

            if (_startUICulture != null)
            {
                CultureInfo.CurrentUICulture = _startUICulture;
                _startUICulture = null;
            }
        }

        // call back helper
        internal void ThreadStart(object? obj)
        {
            Debug.Assert(_start is ParameterizedThreadStart);
            _startArg = obj;

            ExecutionContext? context = _executionContext;
            if (context != null)
            {
                ExecutionContext.RunInternal(context, s_threadStartContextCallback, this);
            }
            else
            {
                InitializeCulture();
                ((ParameterizedThreadStart)_start)(obj);
            }
        }

        // call back helper
        internal void ThreadStart()
        {
            Debug.Assert(_start is ThreadStart);

            ExecutionContext? context = _executionContext;
            if (context != null)
            {
                ExecutionContext.RunInternal(context, s_threadStartContextCallback, this);
            }
            else
            {
                InitializeCulture();
                ((ThreadStart)_start)();
            }
        }
    }

    internal readonly struct ThreadHandle
    {
        private readonly IntPtr _ptr;

        internal ThreadHandle(IntPtr pThread)
        {
            _ptr = pThread;
        }
    }

    public sealed partial class Thread
    {
        /*=========================================================================
        ** Data accessed from managed code that needs to be defined in
        ** ThreadBaseObject to maintain alignment between the two classes.
        ** DON'T CHANGE THESE UNLESS YOU MODIFY ThreadBaseObject in vm\object.h
        =========================================================================*/
        internal ExecutionContext? _executionContext; // this call context follows the logical thread
        internal SynchronizationContext? _synchronizationContext; // maintained separately from ExecutionContext

        private string? _name;
        private Delegate? _delegate; // Delegate

        private object? _threadStartArg;

        /*=========================================================================
        ** The base implementation of Thread is all native.  The following fields
        ** should never be used in the C# code.  They are here to define the proper
        ** space so the thread object may be allocated.  DON'T CHANGE THESE UNLESS
        ** YOU MODIFY ThreadBaseObject in vm\object.h
        =========================================================================*/
#pragma warning disable 169 // These fields are not used from managed.
        // IntPtrs need to be together, and before ints, because IntPtrs are 64-bit
        // fields on 64-bit platforms, where they will be sorted together.

        private IntPtr _DONT_USE_InternalThread; // Pointer
        private int _priority; // INT32

        // The following field is required for interop with the VS Debugger
        // Prior to making any changes to this field, please reach out to the VS Debugger 
        // team to make sure that your changes are not going to prevent the debugger
        // from working.
        private int _managedThreadId; // INT32
#pragma warning restore 169

        private Thread() { }

        private void Create(ThreadStart start) =>
            SetStartHelper((Delegate)start, 0); // 0 will setup Thread with default stackSize

        private void Create(ThreadStart start, int maxStackSize) =>
            SetStartHelper((Delegate)start, maxStackSize);

        private void Create(ParameterizedThreadStart start) =>
            SetStartHelper((Delegate)start, 0);

        private void Create(ParameterizedThreadStart start, int maxStackSize) => 
            SetStartHelper((Delegate)start, maxStackSize);

        public extern int ManagedThreadId
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>Returns handle for interop with EE. The handle is guaranteed to be non-null.</summary>
        internal ThreadHandle GetNativeHandle()
        {
            IntPtr thread = _DONT_USE_InternalThread;

            // This should never happen under normal circumstances.
            if (thread == IntPtr.Zero)
            {
                throw new ArgumentException(null, SR.Argument_InvalidHandle);
            }

            return new ThreadHandle(thread);
        }

        /// <summary>
        /// Spawns off a new thread which will begin executing at the ThreadStart
        /// method on the IThreadable interface passed in the constructor. Once the
        /// thread is dead, it cannot be restarted with another call to Start.
        /// </summary>
        public void Start(object? parameter)
        {
            // In the case of a null delegate (second call to start on same thread)
            // StartInternal method will take care of the error reporting.
            if (_delegate is ThreadStart)
            {
                // We expect the thread to be setup with a ParameterizedThreadStart if this Start is called.
                throw new InvalidOperationException(SR.InvalidOperation_ThreadWrongThreadStart);
            }

            _threadStartArg = parameter;
            Start();
        }

        public void Start()
        {
#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
            // Eagerly initialize the COM Apartment state of the thread if we're allowed to.
            StartupSetApartmentStateInternal();
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

            // Attach current thread's security principal object to the new
            // thread. Be careful not to bind the current thread to a principal
            // if it's not already bound.
            if (_delegate != null)
            {
                // If we reach here with a null delegate, something is broken. But we'll let the StartInternal method take care of
                // reporting an error. Just make sure we don't try to dereference a null delegate.
                Debug.Assert(_delegate.Target is ThreadHelper);
                var t = (ThreadHelper)_delegate.Target;

                ExecutionContext? ec = ExecutionContext.Capture();
                t.SetExecutionContextHelper(ec);
            }

            StartInternal();
        }

        private void SetCultureOnUnstartedThreadNoCheck(CultureInfo value, bool uiCulture)
        {
            Debug.Assert(_delegate != null);
            Debug.Assert(_delegate.Target is ThreadHelper);

            var t = (ThreadHelper)(_delegate.Target);

            if (uiCulture)
            {
                t._startUICulture = value;
            }
            else
            {
                t._startCulture = value;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void StartInternal();

        // Invoked by VM. Helper method to get a logical thread ID for StringBuilder (for
        // correctness) and for FileStream's async code path (for perf, to avoid creating
        // a Thread instance).
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr InternalGetCurrentThread();

        /// <summary>
        /// Suspends the current thread for timeout milliseconds. If timeout == 0,
        /// forces the thread to give up the remainder of its timeslice.  If timeout
        /// == Timeout.Infinite, no timeout will occur.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void SleepInternal(int millisecondsTimeout);

        public static void Sleep(int millisecondsTimeout) => SleepInternal(millisecondsTimeout);

        /// <summary>
        /// Wait for a length of time proportional to 'iterations'.  Each iteration is should
        /// only take a few machine instructions.  Calling this API is preferable to coding
        /// a explicit busy loop because the hardware can be informed that it is busy waiting.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void SpinWaitInternal(int iterations);

        public static void SpinWait(int iterations) => SpinWaitInternal(iterations);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern Interop.BOOL YieldInternal();

        public static bool Yield() => YieldInternal() != Interop.BOOL.FALSE;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Thread InitializeCurrentThread() => (t_currentThread = GetCurrentThreadNative());

        [MethodImpl(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern Thread GetCurrentThreadNative();

        private void SetStartHelper(Delegate start, int maxStackSize)
        {
            Debug.Assert(maxStackSize >= 0);

            var helper = new ThreadHelper(start);
            if (start is ThreadStart)
            {
                SetStart(new ThreadStart(helper.ThreadStart), maxStackSize);
            }
            else
            {
                SetStart(new ParameterizedThreadStart(helper.ThreadStart), maxStackSize);
            }
        }

        /// <summary>Sets the IThreadable interface for the thread. Assumes that start != null.</summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void SetStart(Delegate start, int maxStackSize);

        /// <summary>Clean up the thread when it goes away.</summary>
        ~Thread() => InternalFinalize(); // Delegate to the unmanaged portion.

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void InternalFinalize();

#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void StartupSetApartmentStateInternal();
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

        partial void ThreadNameChanged(string? value)
        {
            InformThreadNameChange(GetNativeHandle(), value, value?.Length ?? 0);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void InformThreadNameChange(ThreadHandle t, string? name, int len);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern DeserializationTracker GetThreadDeserializationTracker(ref StackCrawlMark stackMark);

        /// <summary>Returns true if the thread has been started and is not dead.</summary>
        public extern bool IsAlive
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Return whether or not this thread is a background thread.  Background
        /// threads do not affect when the Execution Engine shuts down.
        /// </summary>
        public bool IsBackground
        {
            get => IsBackgroundNative();
            set => SetBackgroundNative(value);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool IsBackgroundNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void SetBackgroundNative(bool isBackground);

        /// <summary>Returns true if the thread is a threadpool thread.</summary>
        public extern bool IsThreadPoolThread
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>Returns the priority of the thread.</summary>
        public ThreadPriority Priority
        {
            get => (ThreadPriority)GetPriorityNative();
            set => SetPriorityNative((int)value);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int GetPriorityNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void SetPriorityNative(int priority);

        /// <summary>Returns the operating system identifier for the current thread.</summary>
        internal static ulong CurrentOSThreadId => GetCurrentOSThreadId();

        [DllImport(JitHelpers.QCall)]
        private static extern ulong GetCurrentOSThreadId();

        /// <summary>
        /// Return the thread state as a consistent set of bits.  This is more
        /// general then IsAlive or IsBackground.
        /// </summary>
        public ThreadState ThreadState => (ThreadState)GetThreadStateNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int GetThreadStateNative();

        public ApartmentState GetApartmentState() =>
#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
            (ApartmentState)GetApartmentStateNative();
#else // !FEATURE_COMINTEROP_APARTMENT_SUPPORT
            ApartmentState.Unknown;
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

        /// <summary>
        /// An unstarted thread can be marked to indicate that it will host a
        /// single-threaded or multi-threaded apartment.
        /// </summary>
        private bool TrySetApartmentStateUnchecked(ApartmentState state) =>
#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
            SetApartmentStateHelper(state, false);
#else // !FEATURE_COMINTEROP_APARTMENT_SUPPORT
            state == ApartmentState.Unknown;
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
        internal bool SetApartmentStateHelper(ApartmentState state, bool fireMDAOnMismatch)
        {
            ApartmentState retState = (ApartmentState)SetApartmentStateNative((int)state, fireMDAOnMismatch);

            // Special case where we pass in Unknown and get back MTA.
            //  Once we CoUninitialize the thread, the OS will still
            //  report the thread as implicitly in the MTA if any
            //  other thread in the process is CoInitialized.
            if ((state == System.Threading.ApartmentState.Unknown) && (retState == System.Threading.ApartmentState.MTA))
            {
                return true;
            }

            if (retState != state)
            {
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern int GetApartmentStateNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern int SetApartmentStateNative(int state, bool fireMDAOnMismatch);
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#if FEATURE_COMINTEROP
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void DisableComObjectEagerCleanup();
#else // !FEATURE_COMINTEROP
        public void DisableComObjectEagerCleanup()
        {
        }
#endif // FEATURE_COMINTEROP

        /// <summary>
        /// Interrupts a thread that is inside a Wait(), Sleep() or Join().  If that
        /// thread is not currently blocked in that manner, it will be interrupted
        /// when it next begins to block.
        /// </summary>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void Interrupt();

        /// <summary>
        /// Waits for the thread to die or for timeout milliseconds to elapse.
        /// </summary>
        /// <returns>
        /// Returns true if the thread died, or false if the wait timed out. If
        /// -1 is given as the parameter, no timeout will occur.
        /// </returns>
        /// <exception cref="ArgumentException">if timeout &lt; -1 (Timeout.Infinite)</exception>
        /// <exception cref="ThreadInterruptedException">if the thread is interrupted while waiting</exception>
        /// <exception cref="ThreadStateException">if the thread has not been started yet</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool Join(int millisecondsTimeout);

        private static int s_optimalMaxSpinWaitsPerSpinIteration;

        [DllImport(JitHelpers.QCall)]
        private static extern int GetOptimalMaxSpinWaitsPerSpinIterationInternal();

        /// <summary>
        /// Max value to be passed into <see cref="SpinWait(int)"/> for optimal delaying. This value is normalized to be
        /// appropriate for the processor.
        /// </summary>
        internal static int OptimalMaxSpinWaitsPerSpinIteration
        {
            get
            {
                if (s_optimalMaxSpinWaitsPerSpinIteration != 0)
                {
                    return s_optimalMaxSpinWaitsPerSpinIteration;
                }

                // This is done lazily because the first call to the function below in the process triggers a measurement that
                // takes a nontrivial amount of time if the measurement has not already been done in the backgorund.
                // See Thread::InitializeYieldProcessorNormalized(), which describes and calculates this value.
                s_optimalMaxSpinWaitsPerSpinIteration = GetOptimalMaxSpinWaitsPerSpinIterationInternal();
                Debug.Assert(s_optimalMaxSpinWaitsPerSpinIteration > 0);
                return s_optimalMaxSpinWaitsPerSpinIteration;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GetCurrentProcessorNumber();

        // The upper bits of t_currentProcessorIdCache are the currentProcessorId. The lower bits of
        // the t_currentProcessorIdCache are counting down to get it periodically refreshed.
        // TODO: Consider flushing the currentProcessorIdCache on Wait operations or similar 
        // actions that are likely to result in changing the executing core
        [ThreadStatic]
        private static int t_currentProcessorIdCache;

        private const int ProcessorIdCacheShift = 16;
        private const int ProcessorIdCacheCountDownMask = (1 << ProcessorIdCacheShift) - 1;
        private const int ProcessorIdRefreshRate = 5000;

        private static int RefreshCurrentProcessorId()
        {
            int currentProcessorId = GetCurrentProcessorNumber();

            // On Unix, GetCurrentProcessorNumber() is implemented in terms of sched_getcpu, which
            // doesn't exist on all platforms.  On those it doesn't exist on, GetCurrentProcessorNumber()
            // returns -1.  As a fallback in that case and to spread the threads across the buckets
            // by default, we use the current managed thread ID as a proxy.
            if (currentProcessorId < 0) currentProcessorId = Environment.CurrentManagedThreadId;

            // Add offset to make it clear that it is not guaranteed to be 0-based processor number
            currentProcessorId += 100;

            Debug.Assert(ProcessorIdRefreshRate <= ProcessorIdCacheCountDownMask);

            // Mask with int.MaxValue to ensure the execution Id is not negative
            t_currentProcessorIdCache = ((currentProcessorId << ProcessorIdCacheShift) & int.MaxValue) | ProcessorIdRefreshRate;

            return currentProcessorId;
        }

        // Cached processor id used as a hint for which per-core stack to access. It is periodically
        // refreshed to trail the actual thread core affinity.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCurrentProcessorId()
        {
            int currentProcessorIdCache = t_currentProcessorIdCache--;
            if ((currentProcessorIdCache & ProcessorIdCacheCountDownMask) == 0)
            {
                return RefreshCurrentProcessorId();
            }

            return (currentProcessorIdCache >> ProcessorIdCacheShift);
        }

        internal void ResetThreadPoolThread()
        {
            // Currently implemented in unmanaged method Thread::InternalReset and
            // called internally from the ThreadPool in NotifyWorkItemComplete.
        }
    } // End of class Thread
}
