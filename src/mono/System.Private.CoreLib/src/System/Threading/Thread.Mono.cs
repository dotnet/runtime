// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    //
    // Under netcore, there is only one thread object per thread
    //
    [StructLayout(LayoutKind.Sequential)]
    public partial class Thread
    {
#pragma warning disable 169, 414, 649
        #region Sync with metadata/object-internals.h
        private int lock_thread_id;
        // stores a thread handle
        private IntPtr handle;
        private IntPtr native_handle; // used only on Win32
        /* accessed only from unmanaged code */
        private IntPtr name;
        private int name_free; // bool
        private int name_length;
        private ThreadState state;
        private object? abort_exc;
        private int abort_state_handle;
        /* thread_id is only accessed from unmanaged code */
        internal long thread_id;
        private IntPtr debugger_thread; // FIXME switch to bool as soon as CI testing with corlib version bump works
        private UIntPtr static_data; /* GC-tracked */
        private IntPtr runtime_thread_info;
        private int interruption_requested;
        private IntPtr longlived;
        internal bool threadpool_thread;
        /* These are used from managed code */
        internal byte apartment_state;
        internal int managed_id;
        private int small_id;
        private IntPtr manage_callback;
        private IntPtr flags;
        private IntPtr thread_pinning_ref;
        private int priority;
        private IntPtr owned_mutex;
        private IntPtr suspended_event;
        private int self_suspended;
        private IntPtr thread_state;

        private Thread self = null!;
        private object? pending_exception;

        /* This is used only to check that we are in sync between the representation
         * of MonoInternalThread in native and InternalThread in managed
         *
         * DO NOT RENAME! DO NOT ADD FIELDS AFTER! */
        private IntPtr last;
        #endregion
#pragma warning restore 169, 414, 649

        private string? _name;
        private StartHelper? _startHelper;
        internal ExecutionContext? _executionContext;
        internal SynchronizationContext? _synchronizationContext;
#if TARGET_UNIX || TARGET_BROWSER
        internal WaitSubsystem.ThreadWaitInfo? _waitInfo;
#endif

        // This is used for a quick check on thread pool threads after running a work item to determine if the name, background
        // state, or priority were changed by the work item, and if so to reset it. Other threads may also change some of those,
        // but those types of changes may race with the reset anyway, so this field doesn't need to be synchronized.
        private bool _mayNeedResetForThreadPool;

        private Thread()
        {
            Initialize();
        }

        ~Thread()
        {
            FreeInternal();
        }

        public bool IsAlive
        {
            get
            {
                ThreadState state = GetState(this);
                return (state & (ThreadState.Unstarted | ThreadState.Stopped | ThreadState.Aborted)) == 0;
            }
        }

        public bool IsBackground
        {
            get
            {
                ThreadState state = ValidateThreadState();
                return (state & ThreadState.Background) != 0;
            }
            set
            {
                ValidateThreadState();
                if (value)
                {
                    SetState(this, ThreadState.Background);
                }
                else
                {
                    ClrState(this, ThreadState.Background);
                    _mayNeedResetForThreadPool = true;
                }
            }
        }

        public bool IsThreadPoolThread
        {
            get
            {
                ValidateThreadState();
                return threadpool_thread;
            }
            internal set
            {
                threadpool_thread = value;
            }
        }

        public int ManagedThreadId => managed_id;

        internal static int OptimalMaxSpinWaitsPerSpinIteration
        {
            get
            {
                // Default from coreclr (src/utilcode/yieldprocessornormalized.cpp)
                return 7;
            }
        }
#if TARGET_UNIX || TARGET_BROWSER
        internal WaitSubsystem.ThreadWaitInfo WaitInfo
        {
            get
            {
                return Volatile.Read(ref _waitInfo) ?? AllocateWaitInfo();

                WaitSubsystem.ThreadWaitInfo AllocateWaitInfo()
                {
                    Interlocked.CompareExchange(ref _waitInfo, new WaitSubsystem.ThreadWaitInfo(this), null!);
                    return _waitInfo;
                }
            }
        }
#endif

        public ThreadPriority Priority
        {
            get
            {
                ValidateThreadState();
                return (ThreadPriority)priority;
            }
            set
            {
                // TODO: arguments check
                SetPriority(this, (int)value);
                if (value != ThreadPriority.Normal)
                {
                    _mayNeedResetForThreadPool = true;
                }
            }
        }

        public ThreadState ThreadState => GetState(this);

        public ApartmentState GetApartmentState() => ApartmentState.Unknown;

        public void DisableComObjectEagerCleanup()
        {
            // no-op
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetCurrentProcessorNumber();

        public static int GetCurrentProcessorId()
        {
            int id = GetCurrentProcessorNumber();

            if (id < 0)
                id = Environment.CurrentManagedThreadId;

            return id;
        }

        public void Interrupt()
        {
#if TARGET_UNIX || TARGET_BROWSER // TODO: https://github.com/dotnet/runtime/issues/49521
            WaitSubsystem.Interrupt(this);
#endif
            InterruptInternal(this);
        }

        public bool Join(int millisecondsTimeout)
        {
            if (millisecondsTimeout < Timeout.Infinite)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), millisecondsTimeout, SR.ArgumentOutOfRange_NeedNonNegOrNegative1);
            return JoinInternal(this, millisecondsTimeout);
        }

#if TARGET_UNIX || TARGET_BROWSER
        [DynamicDependency(nameof(OnThreadExiting))]
#endif
        private void Initialize()
        {
            InitInternal(this);
        }

        public static void SpinWait(int iterations)
        {
            if (iterations < 0)
                return;

            while (iterations-- > 0)
                SpinWait_nop();
        }

        // Called from the runtime
        internal void StartCallback()
        {
            StartHelper? startHelper = _startHelper;
            Debug.Assert(startHelper != null);
            _startHelper = null;

            startHelper.Run();
        }

        // Called from the runtime
        internal static void ThrowThreadStartException(Exception ex) => throw new ThreadStartException(ex);

        private void StartCore()
        {
             StartInternal(this, _startHelper?._maxStackSize ?? 0);
        }

        [DynamicDependency(nameof(StartCallback))]
        [DynamicDependency(nameof(ThrowThreadStartException))]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void StartInternal(Thread runtimeThread, int stackSize);

        partial void ThreadNameChanged(string? value)
        {
            // TODO: Should only raise the events
            SetName(this, value);
        }

        public static bool Yield()
        {
            return YieldInternal();
        }

        private static bool SetApartmentStateUnchecked(ApartmentState state, bool throwOnError)
        {
             if (state != ApartmentState.Unknown)
             {
                if (throwOnError)
                {
                    throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
                }

                return false;
             }

             return true;
        }

        private ThreadState ValidateThreadState()
        {
            ThreadState state = GetState(this);
            if ((state & ThreadState.Stopped) != 0)
                throw new ThreadStateException(SR.ThreadState_Dead_State);
            return state;
        }

        internal void SetWaitSleepJoinState()
        {
            Debug.Assert(this == CurrentThread);
            Debug.Assert((GetState(this) & ThreadState.WaitSleepJoin) == 0);

            SetState(this, ThreadState.WaitSleepJoin);
        }

        internal void ClearWaitSleepJoinState()
        {
            Debug.Assert(this == CurrentThread);
            Debug.Assert((GetState(this) & ThreadState.WaitSleepJoin) != 0);

            ClrState(this, ThreadState.WaitSleepJoin);
        }

        private static void OnThreadExiting(Thread thread)
        {
#if TARGET_UNIX || TARGET_BROWSER
            thread.WaitInfo.OnThreadExiting();
#endif
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern ulong GetCurrentOSThreadId();

        [MemberNotNull(nameof(self))]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void InitInternal(Thread thread);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Thread GetCurrentThread();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Thread InitializeCurrentThread()
        {
            var current = GetCurrentThread();
            t_currentThread = current;
            return t_currentThread;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void FreeInternal();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern ThreadState GetState(Thread thread);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SetState(Thread thread, ThreadState set);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void ClrState(Thread thread, ThreadState clr);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern string GetName(Thread thread);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void SetName_icall(Thread thread, char* name, int nameLength);

        private static unsafe void SetName(Thread thread, string? name)
        {
            fixed (char* fixed_name = name)
                SetName_icall(thread, fixed_name, name?.Length ?? 0);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool YieldInternal();

        [Intrinsic]
        private static void SpinWait_nop()
        {
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool JoinInternal(Thread thread, int millisecondsTimeout);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void InterruptInternal(Thread thread);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SetPriority(Thread thread, int priority);
    }
}
