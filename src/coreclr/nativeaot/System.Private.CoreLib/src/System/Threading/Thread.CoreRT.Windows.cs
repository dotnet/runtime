// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    using OSThreadPriority = Interop.Kernel32.ThreadPriority;

    public sealed partial class Thread
    {
        [ThreadStatic]
        private static int t_reentrantWaitSuppressionCount;

        [ThreadStatic]
        private static ApartmentType t_apartmentType;

        [ThreadStatic]
        private static ComState t_comState;

        private SafeWaitHandle _osHandle;

        private ApartmentState _initialApartmentState = ApartmentState.Unknown;

        private static volatile bool s_comInitializedOnFinalizerThread;

        partial void PlatformSpecificInitialize();

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
                int result;

                if (millisecondsTimeout == 0)
                {
                    result = (int)Interop.Kernel32.WaitForSingleObject(waitHandle.DangerousGetHandle(), 0);
                }
                else
                {
                    result = WaitHandle.WaitOneCore(waitHandle.DangerousGetHandle(), millisecondsTimeout);
                }

                return result == (int)Interop.Kernel32.WAIT_OBJECT_0;
            }
            finally
            {
                waitHandle.DangerousRelease();
            }
        }

        private unsafe bool CreateThread(GCHandle thisThreadHandle)
        {
            const int AllocationGranularity = 0x10000;  // 64 KiB

            int stackSize = _startHelper._maxStackSize;
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
                &ThreadEntryPoint, (IntPtr)thisThreadHandle,
                Interop.Kernel32.CREATE_SUSPENDED | Interop.Kernel32.STACK_SIZE_PARAM_IS_A_RESERVATION,
                out _);

            if (_osHandle.IsInvalid)
            {
                return false;
            }

            // CoreCLR ignores OS errors while setting the priority, so do we
            SetPriorityLive(_priority);

            Interop.Kernel32.ResumeThread(_osHandle);
            return true;
        }

        /// <summary>
        /// This is an entry point for managed threads created by application
        /// </summary>
        [UnmanagedCallersOnly]
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

            switch (GetCurrentApartmentType())
            {
                case ApartmentType.STA:
                    return ApartmentState.STA;
                case ApartmentType.MTA:
                    return ApartmentState.MTA;
                default:
                    return ApartmentState.Unknown;
            }
        }

        private bool SetApartmentStateUnchecked(ApartmentState state, bool throwOnError)
        {
            if (this != CurrentThread)
            {
                using (LockHolder.Hold(_lock))
                {
                    if (HasStarted())
                        throw new ThreadStateException();
                    _initialApartmentState = state;
                    return true;
                }
            }

            if ((t_comState & ComState.Locked) == 0)
            {
                if (state != ApartmentState.Unknown)
                {
                    InitializeCom(state);
                }
                else
                {
                    UninitializeCom();
                }
            }

            // Clear the cache and check whether new state matches the desired state
            t_apartmentType = ApartmentType.Unknown;

            ApartmentState retState = GetApartmentState();

            if (retState != state)
            {
                if (throwOnError)
                {
                    string msg = SR.Format(SR.Thread_ApartmentState_ChangeFailed, retState);
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

        internal static void InitializeComForFinalizerThread()
        {
            InitializeCom();

            // Prevent re-initialization of COM model on finalizer thread
            t_comState |= ComState.Locked;

            s_comInitializedOnFinalizerThread = true;
        }

        private static void InitializeComForThreadPoolThread()
        {
            // Initialized COM - take advantage of implicit MTA initialized by the finalizer thread
            SpinWait sw = new SpinWait();
            while (!s_comInitializedOnFinalizerThread)
            {
                RuntimeImports.RhInitializeFinalizerThread();
                sw.SpinOnce(0);
            }

            // Prevent re-initialization of COM model on threadpool threads
            t_comState |= ComState.Locked;
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

        // TODO: https://github.com/dotnet/corefx/issues/20766
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

        public void Interrupt() { throw new PlatformNotSupportedException(); }

        //
        // Suppresses reentrant waits on the current thread, until a matching call to RestoreReentrantWaits.
        // This should be used by code that's expected to be called inside the STA message pump, so that it won't
        // reenter itself.  In an ASTA, this should only be the CCW implementations of IUnknown and IInspectable.
        //
        internal static void SuppressReentrantWaits()
        {
            t_reentrantWaitSuppressionCount++;
        }

        internal static void RestoreReentrantWaits()
        {
            Debug.Assert(t_reentrantWaitSuppressionCount > 0);
            t_reentrantWaitSuppressionCount--;
        }

        internal static bool ReentrantWaitsEnabled =>
            GetCurrentApartmentType() == ApartmentType.STA && t_reentrantWaitSuppressionCount == 0;

        internal static ApartmentType GetCurrentApartmentType()
        {
            ApartmentType currentThreadType = t_apartmentType;
            if (currentThreadType != ApartmentType.Unknown)
                return currentThreadType;

            Interop.APTTYPE aptType;
            Interop.APTTYPEQUALIFIER aptTypeQualifier;
            int result = Interop.Ole32.CoGetApartmentType(out aptType, out aptTypeQualifier);

            ApartmentType type = ApartmentType.Unknown;

            switch (result)
            {
                case HResults.CO_E_NOTINITIALIZED:
                    type = ApartmentType.None;
                    break;

                case HResults.S_OK:
                    switch (aptType)
                    {
                        case Interop.APTTYPE.APTTYPE_STA:
                        case Interop.APTTYPE.APTTYPE_MAINSTA:
                            type = ApartmentType.STA;
                            break;

                        case Interop.APTTYPE.APTTYPE_MTA:
                            type = ApartmentType.MTA;
                            break;

                        case Interop.APTTYPE.APTTYPE_NA:
                            switch (aptTypeQualifier)
                            {
                                case Interop.APTTYPEQUALIFIER.APTTYPEQUALIFIER_NA_ON_MTA:
                                case Interop.APTTYPEQUALIFIER.APTTYPEQUALIFIER_NA_ON_IMPLICIT_MTA:
                                    type = ApartmentType.MTA;
                                    break;

                                case Interop.APTTYPEQUALIFIER.APTTYPEQUALIFIER_NA_ON_STA:
                                case Interop.APTTYPEQUALIFIER.APTTYPEQUALIFIER_NA_ON_MAINSTA:
                                    type = ApartmentType.STA;
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

            if (type != ApartmentType.Unknown)
                t_apartmentType = type;
            return type;
        }

        internal enum ApartmentType : byte
        {
            Unknown = 0,
            None,
            STA,
            MTA
        }

        [Flags]
        internal enum ComState : byte
        {
            InitializedByUs = 1,
            Locked = 2,
        }

        // TODO: Use GetCurrentProcessorNumberEx for NUMA
        private static int ComputeCurrentProcessorId() => (int)Interop.Kernel32.GetCurrentProcessorNumber();
    }
}
