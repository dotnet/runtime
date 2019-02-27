// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal class ThreadHelper
    {
        private Delegate _start;
        internal CultureInfo _startCulture;
        internal CultureInfo _startUICulture;
        private object _startArg = null;
        private ExecutionContext _executionContext = null;

        internal ThreadHelper(Delegate start)
        {
            _start = start; 
        }

        internal void SetExecutionContextHelper(ExecutionContext ec)
        {
            _executionContext = ec;
        }

        internal static ContextCallback _ccb = new ContextCallback(ThreadStart_Context);

        private static void ThreadStart_Context(object state)
        {
            ThreadHelper t = (ThreadHelper)state;
            if (t._start is ThreadStart)
            {
                ((ThreadStart)t._start)();
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
        internal void ThreadStart(object obj)
        {
            _startArg = obj;

            InitializeCulture();

            ExecutionContext context = _executionContext;
            if (context != null)
            {
                ExecutionContext.RunInternal(context, _ccb, (object)this);
            }
            else
            {
                ((ParameterizedThreadStart)_start)(obj);
            }
        }

        // call back helper
        internal void ThreadStart()
        {
            InitializeCulture();

            ExecutionContext context = _executionContext;
            if (context != null)
            {
                ExecutionContext.RunInternal(context, _ccb, (object)this);
            }
            else
            {
                ((ThreadStart)_start)();
            }
        }
    }

    internal struct ThreadHandle
    {
        private IntPtr m_ptr;

        internal ThreadHandle(IntPtr pThread)
        {
            m_ptr = pThread;
        }
    }

    public sealed partial class Thread
    {
        /*=========================================================================
        ** Data accessed from managed code that needs to be defined in
        ** ThreadBaseObject to maintain alignment between the two classes.
        ** DON'T CHANGE THESE UNLESS YOU MODIFY ThreadBaseObject in vm\object.h
        =========================================================================*/
        private ExecutionContext m_ExecutionContext;    // this call context follows the logical thread
        private SynchronizationContext m_SynchronizationContext;    // On CoreCLR, this is maintained separately from ExecutionContext

        private string m_Name;
        private Delegate m_Delegate;             // Delegate

        private object m_ThreadStartArg;

        /*=========================================================================
        ** The base implementation of Thread is all native.  The following fields
        ** should never be used in the C# code.  They are here to define the proper
        ** space so the thread object may be allocated.  DON'T CHANGE THESE UNLESS
        ** YOU MODIFY ThreadBaseObject in vm\object.h
        =========================================================================*/
#pragma warning disable 169
#pragma warning disable 414  // These fields are not used from managed.
        // IntPtrs need to be together, and before ints, because IntPtrs are 64-bit
        //  fields on 64-bit platforms, where they will be sorted together.

        private IntPtr DONT_USE_InternalThread;        // Pointer
        private int m_Priority;                     // INT32

        // The following field is required for interop with the VS Debugger
        // Prior to making any changes to this field, please reach out to the VS Debugger 
        // team to make sure that your changes are not going to prevent the debugger
        // from working.
        private int _managedThreadId;              // INT32

#pragma warning restore 414
#pragma warning restore 169

        [ThreadStatic]
        private static Thread t_currentThread;

        // Adding an empty default ctor for annotation purposes
        internal Thread() { }

        /*=========================================================================
        ** Creates a new Thread object which will begin execution at
        ** start.ThreadStart on a new thread when the Start method is called.
        **
        ** Exceptions: ArgumentNullException if start == null.
        =========================================================================*/
        private void Create(ThreadStart start)
        {
            SetStartHelper((Delegate)start, 0);  //0 will setup Thread with default stackSize
        }

        private void Create(ThreadStart start, int maxStackSize)
        {
            SetStartHelper((Delegate)start, maxStackSize);
        }

        private void Create(ParameterizedThreadStart start)
        {
            SetStartHelper((Delegate)start, 0);
        }

        private void Create(ParameterizedThreadStart start, int maxStackSize)
        {
            SetStartHelper((Delegate)start, maxStackSize);
        }

        public extern int ManagedThreadId
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        // Returns handle for interop with EE. The handle is guaranteed to be non-null.
        internal ThreadHandle GetNativeHandle()
        {
            IntPtr thread = DONT_USE_InternalThread;

            // This should never happen under normal circumstances. m_assembly is always assigned before it is handed out to the user.
            // There are ways how to create an uninitialized objects through remoting, etc. Avoid AVing in the EE by throwing a nice
            // exception here.
            if (thread == IntPtr.Zero)
                throw new ArgumentException(null, SR.Argument_InvalidHandle);

            return new ThreadHandle(thread);
        }


        /*=========================================================================
        ** Spawns off a new thread which will begin executing at the ThreadStart
        ** method on the IThreadable interface passed in the constructor. Once the
        ** thread is dead, it cannot be restarted with another call to Start.
        **
        ** Exceptions: ThreadStateException if the thread has already been started.
        =========================================================================*/
        public void Start(object parameter)
        {
            //In the case of a null delegate (second call to start on same thread)
            //    StartInternal method will take care of the error reporting
            if (m_Delegate is ThreadStart)
            {
                //We expect the thread to be setup with a ParameterizedThreadStart
                //    if this constructor is called.
                //If we got here then that wasn't the case
                throw new InvalidOperationException(SR.InvalidOperation_ThreadWrongThreadStart);
            }
            m_ThreadStartArg = parameter;
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
            if (m_Delegate != null)
            {
                // If we reach here with a null delegate, something is broken. But we'll let the StartInternal method take care of
                // reporting an error. Just make sure we don't try to dereference a null delegate.
                ThreadHelper t = (ThreadHelper)(m_Delegate.Target);
                ExecutionContext ec = ExecutionContext.Capture();
                t.SetExecutionContextHelper(ec);
            }

            StartInternal();
        }

        private void SetCultureOnUnstartedThreadNoCheck(CultureInfo value, bool uiCulture)
        {
            Debug.Assert(m_Delegate != null);

            ThreadHelper t = (ThreadHelper)(m_Delegate.Target);
            if (uiCulture)
            {
                t._startUICulture = value;
            }
            else
            {
                t._startCulture = value;
            }
        }

        internal ExecutionContext ExecutionContext
        {
            get { return m_ExecutionContext; }
            set { m_ExecutionContext = value; }
        }

        internal SynchronizationContext SynchronizationContext
        {
            get { return m_SynchronizationContext; }
            set { m_SynchronizationContext = value; }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void StartInternal();


        // Helper method to get a logical thread ID for StringBuilder (for
        // correctness) and for FileStream's async code path (for perf, to
        // avoid creating a Thread instance).
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr InternalGetCurrentThread();

        /*=========================================================================
        ** Suspends the current thread for timeout milliseconds. If timeout == 0,
        ** forces the thread to give up the remainder of its timeslice.  If timeout
        ** == Timeout.Infinite, no timeout will occur.
        **
        ** Exceptions: ArgumentException if timeout < -1 (Timeout.Infinite).
        **             ThreadInterruptedException if the thread is interrupted while sleeping.
        =========================================================================*/
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SleepInternal(int millisecondsTimeout);

        public static void Sleep(int millisecondsTimeout)
        {
            SleepInternal(millisecondsTimeout);
        }

        /* wait for a length of time proportional to 'iterations'.  Each iteration is should
           only take a few machine instructions.  Calling this API is preferable to coding
           a explicit busy loop because the hardware can be informed that it is busy waiting. */

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SpinWaitInternal(int iterations);

        public static void SpinWait(int iterations)
        {
            SpinWaitInternal(iterations);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern bool YieldInternal();

        public static bool Yield()
        {
            return YieldInternal();
        }

        public static Thread CurrentThread => t_currentThread ?? InitializeCurrentThread();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Thread InitializeCurrentThread()
        {
            return (t_currentThread = GetCurrentThreadNative());
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall), ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern Thread GetCurrentThreadNative();

        private void SetStartHelper(Delegate start, int maxStackSize)
        {
            Debug.Assert(maxStackSize >= 0);

            ThreadHelper threadStartCallBack = new ThreadHelper(start);
            if (start is ThreadStart)
            {
                SetStart(new ThreadStart(threadStartCallBack.ThreadStart), maxStackSize);
            }
            else
            {
                SetStart(new ParameterizedThreadStart(threadStartCallBack.ThreadStart), maxStackSize);
            }
        }

        /*=========================================================================
        ** PRIVATE Sets the IThreadable interface for the thread. Assumes that
        ** start != null.
        =========================================================================*/
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void SetStart(Delegate start, int maxStackSize);

        /*=========================================================================
        ** Clean up the thread when it goes away.
        =========================================================================*/
        ~Thread()
        {
            // Delegate to the unmanaged portion.
            InternalFinalize();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void InternalFinalize();

#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void StartupSetApartmentStateInternal();
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

        // Retrieves the name of the thread.
        //
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                lock (this)
                {
                    if (m_Name != null)
                        throw new InvalidOperationException(SR.InvalidOperation_WriteOnce);
                    m_Name = value;

                    InformThreadNameChange(GetNativeHandle(), value, (value != null) ? value.Length : 0);
                }
            }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void InformThreadNameChange(ThreadHandle t, string name, int len);

        /*=========================================================================
        ** Returns true if the thread has been started and is not dead.
        =========================================================================*/
        public extern bool IsAlive
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /*=========================================================================
        ** Return whether or not this thread is a background thread.  Background
        ** threads do not affect when the Execution Engine shuts down.
        **
        ** Exceptions: ThreadStateException if the thread is dead.
        =========================================================================*/
        public bool IsBackground
        {
            get { return IsBackgroundNative(); }
            set { SetBackgroundNative(value); }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool IsBackgroundNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void SetBackgroundNative(bool isBackground);

        /*=========================================================================
        ** Returns true if the thread is a threadpool thread.
        =========================================================================*/
        public extern bool IsThreadPoolThread
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /*=========================================================================
        ** Returns the priority of the thread.
        **
        ** Exceptions: ThreadStateException if the thread is dead.
        =========================================================================*/
        public ThreadPriority Priority
        {
            get { return (ThreadPriority)GetPriorityNative(); }
            set { SetPriorityNative((int)value); }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int GetPriorityNative();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void SetPriorityNative(int priority);

        /*=========================================================================
        ** Returns the operating system identifier for the current thread.
        =========================================================================*/
        internal static ulong CurrentOSThreadId
        {
            get
            {
                return GetCurrentOSThreadId();
            }
        }

        [DllImport(JitHelpers.QCall)]
        private static extern ulong GetCurrentOSThreadId();

        /*=========================================================================
        ** Return the thread state as a consistent set of bits.  This is more
        ** general then IsAlive or IsBackground.
        =========================================================================*/
        public ThreadState ThreadState
        {
            get { return (ThreadState)GetThreadStateNative(); }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int GetThreadStateNative();

        public ApartmentState GetApartmentState()
        {
#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
            return (ApartmentState)GetApartmentStateNative();
#else // !FEATURE_COMINTEROP_APARTMENT_SUPPORT
            Debug.Assert(false); // the Thread class in CoreFX should have handled this case
            return ApartmentState.MTA;
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
        }

        /*=========================================================================
        ** An unstarted thread can be marked to indicate that it will host a
        ** single-threaded or multi-threaded apartment.
        =========================================================================*/
        public bool TrySetApartmentStateUnchecked(ApartmentState state)
        {
#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
            return SetApartmentStateHelper(state, false);
#else // !FEATURE_COMINTEROP_APARTMENT_SUPPORT
            Debug.Assert(false); // the Thread class in CoreFX should have handled this case
            return false;
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
        }

#if FEATURE_COMINTEROP_APARTMENT_SUPPORT
        internal bool SetApartmentStateHelper(ApartmentState state, bool fireMDAOnMismatch)
        {
            ApartmentState retState = (ApartmentState)SetApartmentStateNative((int)state, fireMDAOnMismatch);

            // Special case where we pass in Unknown and get back MTA.
            //  Once we CoUninitialize the thread, the OS will still
            //  report the thread as implicitly in the MTA if any
            //  other thread in the process is CoInitialized.
            if ((state == System.Threading.ApartmentState.Unknown) && (retState == System.Threading.ApartmentState.MTA))
                return true;

            if (retState != state)
                return false;

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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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
                return RefreshCurrentProcessorId();
            return (currentProcessorIdCache >> ProcessorIdCacheShift);
        }

        internal void ResetThreadPoolThread()
        {
            // Currently implemented in unmanaged method Thread::InternalReset and
            // called internally from the ThreadPool in NotifyWorkItemComplete.
        }
    } // End of class Thread

    // declaring a local var of this enum type and passing it by ref into a function that needs to do a
    // stack crawl will both prevent inlining of the callee and pass an ESP point to stack crawl to
    // Declaring these in EH clauses is illegal; they must declared in the main method body
    internal enum StackCrawlMark
    {
        LookForMe = 0,
        LookForMyCaller = 1,
        LookForMyCallersCaller = 2,
        LookForThread = 3
    }
}
