// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using OSThreadPriority = Interop.Kernel32.ThreadPriority;

namespace System.Threading
{
    public sealed partial class Thread
    {
        [ThreadStatic]
        private static ComState t_comState;

        private SafeWaitHandle _osHandle;

        private ApartmentState _initialApartmentState = ApartmentState.Unknown;

        partial void PlatformSpecificInitialize();

        internal static void SleepInternal(int millisecondsTimeout)
        {
            Debug.Assert(millisecondsTimeout >= Timeout.Infinite);

            CheckForPendingInterrupt();

            Thread currentThread = CurrentThread;
            if (millisecondsTimeout == Timeout.Infinite)
            {
                // Infinite wait - use alertable wait
                currentThread.SetWaitSleepJoinState();
                uint result;
                while (true)
                {
                    result = Interop.Kernel32.SleepEx(Timeout.UnsignedInfinite, true);
                    if (result != Interop.Kernel32.WAIT_IO_COMPLETION)
                    {
                        break;
                    }
                    CheckForPendingInterrupt();
                }

                currentThread.ClearWaitSleepJoinState();
            }
            else
            {
                // Timed wait - use alertable wait
                currentThread.SetWaitSleepJoinState();
                long startTime = Environment.TickCount64;
                while (true)
                {
                    uint result = Interop.Kernel32.SleepEx((uint)millisecondsTimeout, true);
                    if (result != Interop.Kernel32.WAIT_IO_COMPLETION)
                    {
                        break;
                    }
                    // Check if this was our interrupt APC
                    CheckForPendingInterrupt();
                    // Handle APC completion by adjusting timeout and retrying
                    long currentTime = Environment.TickCount64;
                    long elapsed = currentTime - startTime;
                    if (elapsed >= millisecondsTimeout)
                    {
                        break;
                    }
                    millisecondsTimeout -= (int)elapsed;
                    startTime = currentTime;
                }

                currentThread.ClearWaitSleepJoinState();
            }
        }

        // Platform-specific initialization of foreign threads, i.e. threads not created by Thread.Start
        private void PlatformSpecificInitializeExistingThread()
        {
            _osHandle = GetOSHandleForCurrentThread();
        }

        private static SafeWaitHandle GetOSHandleForCurrentThread()
        {
            IntPtr currentProcHandle = Interop.Kernel32.GetCurrentProcess();
            IntPtr currentThreadHandle = Interop.Kernel32.GetCurrentThread();
            SafeWaitHandle threadHandle;

            if (Interop.Kernel32.DuplicateHandle(currentProcHandle, currentThreadHandle, currentProcHandle,
                out threadHandle, 0, false, Interop.Kernel32.DUPLICATE_SAME_ACCESS))
            {
                return threadHandle;
            }

            // Throw an ApplicationException for compatibility with CoreCLR. First save the error code.
            int errorCode = Marshal.GetLastWin32Error();
            var ex = new ApplicationException();
            ex.HResult = errorCode;
            throw ex;
        }

        private static ThreadPriority MapFromOSPriority(OSThreadPriority priority)
        {
            if (priority <= OSThreadPriority.Lowest)
            {
                // OS thread priorities in the [Idle,Lowest] range are mapped to ThreadPriority.Lowest
                return ThreadPriority.Lowest;
            }
            switch (priority)
            {
                case OSThreadPriority.BelowNormal:
                    return ThreadPriority.BelowNormal;

                case OSThreadPriority.Normal:
                    return ThreadPriority.Normal;

                case OSThreadPriority.AboveNormal:
                    return ThreadPriority.AboveNormal;

                case OSThreadPriority.ErrorReturn:
                    Debug.Fail("GetThreadPriority failed");
                    return ThreadPriority.Normal;
            }
            // Handle OSThreadPriority.ErrorReturn value before this check!
            if (priority >= OSThreadPriority.Highest)
            {
                // OS thread priorities in the [Highest,TimeCritical] range are mapped to ThreadPriority.Highest
                return ThreadPriority.Highest;
            }
            Debug.Fail("Unreachable");
            return ThreadPriority.Normal;
        }

        private static OSThreadPriority MapToOSPriority(ThreadPriority priority)
        {
            switch (priority)
            {
                case ThreadPriority.Lowest:
                    return OSThreadPriority.Lowest;

                case ThreadPriority.BelowNormal:
                    return OSThreadPriority.BelowNormal;

                case ThreadPriority.Normal:
                    return OSThreadPriority.Normal;

                case ThreadPriority.AboveNormal:
                    return OSThreadPriority.AboveNormal;

                case ThreadPriority.Highest:
                    return OSThreadPriority.Highest;

                default:
                    Debug.Fail("Unreachable");
                    return OSThreadPriority.Normal;
            }
        }

        private ThreadPriority GetPriorityLive()
        {
            Debug.Assert(!_osHandle.IsInvalid);
            return MapFromOSPriority(Interop.Kernel32.GetThreadPriority(_osHandle));
        }

        private bool SetPriorityLive(ThreadPriority priority)
        {
            Debug.Assert(!_osHandle.IsInvalid);
            return Interop.Kernel32.SetThreadPriority(_osHandle, (int)MapToOSPriority(priority));
        }

        [UnmanagedCallersOnly]
        private static void OnThreadExit()
        {
            Thread? currentThread = t_currentThread;
            if (currentThread != null)
            {
                StopThread(currentThread);
            }
        }

        private bool JoinInternal(int millisecondsTimeout)
        {
            // This method assumes the thread has been started
            Debug.Assert(!GetThreadStateBit(ThreadState.Unstarted) || (millisecondsTimeout == 0));
            SafeWaitHandle waitHandle = _osHandle;

            // If an OS thread is terminated and its Thread object is resurrected, _osHandle may be finalized and closed
            if (waitHandle.IsClosed)
            {
                return true;
            }

            // Handle race condition with the finalizer
            try
            {
                waitHandle.DangerousAddRef();
            }
            catch (ObjectDisposedException)
            {
                return true;
            }

            try
            {
                if (millisecondsTimeout == 0)
                {
                    int result = (int)Interop.Kernel32.WaitForSingleObject(waitHandle.DangerousGetHandle(), 0);
                    return result == (int)Interop.Kernel32.WAIT_OBJECT_0;
                }
                else
                {
                    Thread currentThread = CurrentThread;
                    currentThread.SetWaitSleepJoinState();
                    uint result;
                    if (millisecondsTimeout == Timeout.Infinite)
                    {
                        // Infinite wait
                        while (true)
                        {
                            result = Interop.Kernel32.WaitForSingleObjectEx(waitHandle.DangerousGetHandle(), Timeout.UnsignedInfinite, Interop.BOOL.TRUE);
                            if (result != Interop.Kernel32.WAIT_IO_COMPLETION)
                            {
                                break;
                            }
                            // Check if this was our interrupt APC
                            CheckForPendingInterrupt();
                        }
                    }
                    else
                    {
                        long startTime = Environment.TickCount64;
                        while (true)
                        {
                            result = Interop.Kernel32.WaitForSingleObjectEx(waitHandle.DangerousGetHandle(), (uint)millisecondsTimeout, Interop.BOOL.TRUE);
                            if (result != Interop.Kernel32.WAIT_IO_COMPLETION)
                            {
                                break;
                            }
                            // Check if this was our interrupt APC
                            CheckForPendingInterrupt();
                            // Handle APC completion by adjusting timeout and retrying
                            long currentTime = Environment.TickCount64;
                            long elapsed = currentTime - startTime;
                            if (elapsed >= millisecondsTimeout)
                            {
                                result = Interop.Kernel32.WAIT_TIMEOUT;
                                break;
                            }
                            millisecondsTimeout -= (int)elapsed;
                            startTime = currentTime;
                        }
                    }
                    currentThread.ClearWaitSleepJoinState();
                    return result == (int)Interop.Kernel32.WAIT_OBJECT_0;
                }
            }
            finally
            {
                waitHandle.DangerousRelease();
            }
        }

        private unsafe bool CreateThread(GCHandle<Thread> thisThreadHandle)
        {
            const int AllocationGranularity = 0x10000;  // 64 KiB
            int stackSize = _startHelper._maxStackSize;

            if (stackSize <= 0)
            {
                stackSize = (int)RuntimeImports.RhGetDefaultStackSize();
            }

            if ((0 < stackSize) && (stackSize < AllocationGranularity))
            {
                // If StackSizeParamIsAReservation flag is set and the reserve size specified by CreateThread's
                // dwStackSize parameter is less than or equal to the initially committed stack size specified in
                // the executable header, the reserve size will be set to the initially committed size rounded up
                // to the nearest multiple of 1 MiB. In all cases the reserve size is rounded up to the nearest
                // multiple of the system's allocation granularity (typically 64 KiB).
                //
                // To prevent overreservation of stack memory for small stackSize values, we increase stackSize to
                // the allocation granularity. We assume that the SizeOfStackCommit field of IMAGE_OPTIONAL_HEADER
                // is strictly smaller than the allocation granularity (the field's default value is 4 KiB);
                // otherwise, at least 1 MiB of memory will be reserved. Note that the desktop CLR increases
                // stackSize to 256 KiB if it is smaller than that.
                stackSize = AllocationGranularity;
            }

            _osHandle = Interop.Kernel32.CreateThread(IntPtr.Zero, (IntPtr)stackSize,
                RuntimeImports.RhGetThreadEntryPointAddress(), GCHandle<Thread>.ToIntPtr(thisThreadHandle),
                Interop.Kernel32.CREATE_SUSPENDED | Interop.Kernel32.STACK_SIZE_PARAM_IS_A_RESERVATION,
                out _);

            if (_osHandle.IsInvalid)
            {
                return false;
            }

            // CoreCLR ignores OS errors while setting the priority, so do we
            SetPriorityLive(_priority);

            // If the thread was interrupted before it was started, queue the interruption now
            if (GetThreadStateBit(Interrupted))
            {
                ClearThreadStateBit(Interrupted);
                Interrupt();
            }

            Interop.Kernel32.ResumeThread(_osHandle);
            return true;
        }

        /// <summary>
        /// This is an entry point for managed threads created by application
        /// </summary>
        [UnmanagedCallersOnly(EntryPoint = "ThreadEntryPoint")]
        private static uint ThreadEntryPoint(IntPtr parameter)
        {
            StartThread(parameter);
            return 0;
        }

        public ApartmentState GetApartmentState()
        {
            if (this != CurrentThread)
            {
                if (HasStarted())
                    throw new ThreadStateException();
                return _initialApartmentState;
            }

            switch (GetCurrentApartmentState())
            {
                case ApartmentState.STA:
                    return ApartmentState.STA;
                case ApartmentState.MTA:
                    return ApartmentState.MTA;
                default:
                    // If COM is uninitialized on the current thread, it is assumed to be implicit MTA.
                    return ApartmentState.MTA;
            }
        }

        private bool SetApartmentStateUnchecked(ApartmentState state, bool throwOnError)
        {
            ApartmentState retState;

            if (this != CurrentThread)
            {
                using (_lock.EnterScope())
                {
                    if (HasStarted())
                        throw new ThreadStateException();

                    // Compat: Disallow resetting the initial apartment state
                    if (_initialApartmentState == ApartmentState.Unknown)
                        _initialApartmentState = state;

                    retState = _initialApartmentState;
                }
            }
            else
            {

                if ((t_comState & ComState.Locked) == 0)
                {
                    if (state != ApartmentState.Unknown)
                    {
                        InitializeCom(state);
                    }
                    else
                    {
                        // Compat: Setting ApartmentState to Unknown uninitializes COM
                        UninitializeCom();
                    }

                    // Clear the cache and check whether new state matches the desired state
                    t_comState &= ~(ComState.STA | ComState.MTA);

                    retState = GetCurrentApartmentState();
                }
                else
                {
                    Debug.Assert((t_comState & ComState.MTA) != 0);
                    retState = ApartmentState.MTA;
                }
            }

            // Special case where we pass in Unknown and get back MTA.
            //  Once we CoUninitialize the thread, the OS will still
            //  report the thread as implicitly in the MTA if any
            //  other thread in the process is CoInitialized.
            if ((state == ApartmentState.Unknown) && (retState == ApartmentState.MTA))
            {
                return true;
            }

            if (retState != state)
            {
                if (throwOnError)
                {
                    // NOTE: We do the enum stringification manually to avoid introducing a dependency
                    // on enum stringification in small apps. We set apartment state in the startup path.
                    string msg = SR.Format(SR.Thread_ApartmentState_ChangeFailed, retState switch
                    {
                        ApartmentState.MTA => "MTA",
                        ApartmentState.STA => "STA",
                        _ => "Unknown"
                    });
                    throw new InvalidOperationException(msg);
                }

                return false;
            }

            return true;
        }

        private void InitializeComOnNewThread()
        {
            InitializeCom(_initialApartmentState);
        }

        private static void InitializeComForThreadPoolThread()
        {
            // Process-wide COM is initialized very early before any managed code can run.
            // Assume it is done.
            // Prevent re-initialization of COM model on threadpool threads from the default one.
            t_comState |= ComState.Locked | ComState.MTA;
        }

        private static void InitializeCom(ApartmentState state = ApartmentState.MTA)
        {
            if ((t_comState & ComState.InitializedByUs) != 0)
                return;

#if ENABLE_WINRT
            int hr = Interop.WinRT.RoInitialize(
                (state == ApartmentState.STA) ? Interop.WinRT.RO_INIT_SINGLETHREADED
                    : Interop.WinRT.RO_INIT_MULTITHREADED);
#else
            int hr = Interop.Ole32.CoInitializeEx(IntPtr.Zero,
                (state == ApartmentState.STA) ? Interop.Ole32.COINIT_APARTMENTTHREADED
                    : Interop.Ole32.COINIT_MULTITHREADED);
#endif
            if (hr < 0)
            {
                // RPC_E_CHANGED_MODE indicates this thread has been already initialized with a different
                // concurrency model. We stay away and let whoever else initialized the COM to be in control.
                if (hr == HResults.RPC_E_CHANGED_MODE)
                    return;

                // CoInitializeEx returns E_NOTIMPL on Windows Nano Server for STA
                if (hr == HResults.E_NOTIMPL)
                    throw new PlatformNotSupportedException();

                throw new OutOfMemoryException();
            }

            t_comState |= ComState.InitializedByUs;

            // If the thread has already been CoInitialized to the proper mode, then
            // we don't want to leave an outstanding CoInit so we CoUninit.
            if (hr > 0)
                UninitializeCom();
        }

        private static void UninitializeCom()
        {
            if ((t_comState & ComState.InitializedByUs) == 0)
                return;

#if ENABLE_WINRT
            Interop.WinRT.RoUninitialize();
#else
            Interop.Ole32.CoUninitialize();
#endif

            t_comState &= ~ComState.InitializedByUs;
        }

        // TODO: https://github.com/dotnet/runtime/issues/22161
        public void DisableComObjectEagerCleanup() { }

        private static Thread InitializeExistingThreadPoolThread()
        {
            ThreadPool.InitializeForThreadPoolThread();

            InitializeComForThreadPoolThread();

            Thread thread = CurrentThread;
            thread.SetThreadStateBit(ThreadPoolThread);
            return thread;
        }

        // Use ThreadPoolCallbackWrapper instead of calling this function directly
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Thread EnsureThreadPoolThreadInitialized()
        {
            Thread? thread = t_currentThread;
            if (thread != null && thread.GetThreadStateBit(ThreadPoolThread))
                return thread;
            return InitializeExistingThreadPoolThread();
        }

        public void Interrupt()
        {
            using (_lock.EnterScope())
            {
                // Thread.Interrupt for dead thread should do nothing
                if (IsDead())
                {
                    return;
                }

                // Thread.Interrupt for thread that has not been started yet should queue a pending interrupt
                // for when we actually create the thread.
                if (_osHandle?.IsInvalid ?? true)
                {
                    SetThreadStateBit(Interrupted);
                    return;
                }

                unsafe
                {
                    Interop.Kernel32.QueueUserAPC(RuntimeImports.RhGetInterruptApcCallback(), _osHandle, 0);
                }
            }
        }

        internal static void CheckForPendingInterrupt()
        {
            if (RuntimeImports.RhCheckAndClearPendingInterrupt())
            {
                CurrentThread.ClearWaitSleepJoinState();
                throw new ThreadInterruptedException();
            }
        }

        internal static bool ReentrantWaitsEnabled =>
            GetCurrentApartmentState() == ApartmentState.STA;

        // Unlike the public API, this returns ApartmentState.Unknown when the COM is uninitialized on current thread
        internal static ApartmentState GetCurrentApartmentState()
        {
            if ((t_comState & (ComState.MTA | ComState.STA)) != 0)
                return ((t_comState & ComState.STA) != 0) ? ApartmentState.STA : ApartmentState.MTA;

            Interop.APTTYPE aptType;
            Interop.APTTYPEQUALIFIER aptTypeQualifier;
            int result = Interop.Ole32.CoGetApartmentType(out aptType, out aptTypeQualifier);

            ApartmentState state = ApartmentState.Unknown;

            switch (result)
            {
                case HResults.CO_E_NOTINITIALIZED:
                    Debug.Fail("COM is not initialized");
                    state = ApartmentState.Unknown;
                    break;

                case HResults.S_OK:
                    switch (aptType)
                    {
                        case Interop.APTTYPE.APTTYPE_STA:
                        case Interop.APTTYPE.APTTYPE_MAINSTA:
                            state = ApartmentState.STA;
                            break;

                        case Interop.APTTYPE.APTTYPE_MTA:
                            state = ApartmentState.MTA;
                            break;

                        case Interop.APTTYPE.APTTYPE_NA:
                            switch (aptTypeQualifier)
                            {
                                case Interop.APTTYPEQUALIFIER.APTTYPEQUALIFIER_NA_ON_MTA:
                                    state = ApartmentState.MTA;
                                    break;

                                case Interop.APTTYPEQUALIFIER.APTTYPEQUALIFIER_NA_ON_IMPLICIT_MTA:
                                    state = ApartmentState.Unknown;
                                    break;

                                case Interop.APTTYPEQUALIFIER.APTTYPEQUALIFIER_NA_ON_STA:
                                case Interop.APTTYPEQUALIFIER.APTTYPEQUALIFIER_NA_ON_MAINSTA:
                                    state = ApartmentState.STA;
                                    break;

                                default:
                                    Debug.Fail("NA apartment without NA qualifier");
                                    break;
                            }
                            break;
                    }
                    break;

                default:
                    Debug.Fail("bad return from CoGetApartmentType");
                    break;
            }

            if (state != ApartmentState.Unknown)
                t_comState |= (state == ApartmentState.STA) ? ComState.STA : ComState.MTA;
            return state;
        }

        [Flags]
        internal enum ComState : byte
        {
            InitializedByUs = 1,
            Locked = 2,
            MTA = 4,
            STA = 8
        }
    }
}
