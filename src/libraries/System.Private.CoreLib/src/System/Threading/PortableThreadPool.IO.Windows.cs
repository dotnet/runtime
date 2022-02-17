// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        private static readonly int ProcessorsPerPoller =
            AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.ProcessorsPerIOPollerThread", 12, false);

        private static nint CreateIOCompletionPort()
        {
            nint port = Interop.Kernel32.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, UIntPtr.Zero, 0);
            if (port == 0)
            {
                int hr = Marshal.GetHRForLastWin32Error();
                Environment.FailFast($"Failed to create an IO completion port. HR: {hr}");
            }

            return port;
        }

        public void RegisterForIOCompletionNotifications(nint handle)
        {
            Debug.Assert(_ioPort != 0);

            if (_ioCompletionPollers == null)
            {
                EnsureIOCompletionPollers();
            }

            nint port = Interop.Kernel32.CreateIoCompletionPort(handle, _ioPort, UIntPtr.Zero, 0);
            if (port == 0)
            {
                ThrowHelper.ThrowApplicationException(Marshal.GetHRForLastWin32Error());
            }

            Debug.Assert(port == _ioPort);
        }

        public unsafe void QueueNativeOverlapped(NativeOverlapped* nativeOverlapped)
        {
            Debug.Assert(nativeOverlapped != null);
            Debug.Assert(_ioPort != 0);

            if (_ioCompletionPollers == null)
            {
                EnsureIOCompletionPollers();
            }

            if (NativeRuntimeEventSource.Log.IsEnabled())
            {
                NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(nativeOverlapped);
            }

            if (!Interop.Kernel32.PostQueuedCompletionStatus(_ioPort, 0, UIntPtr.Zero, (IntPtr)nativeOverlapped))
            {
                ThrowHelper.ThrowApplicationException(Marshal.GetHRForLastWin32Error());
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnsureIOCompletionPollers()
        {
            _threadAdjustmentLock.Acquire();
            try
            {
                if (_ioCompletionPollers != null)
                {
                    return;
                }

                int pollerCount = (Environment.ProcessorCount - 1) / ProcessorsPerPoller + 1;
                IOCompletionPoller[] pollers = new IOCompletionPoller[pollerCount];
                for (int i = 0; i < pollerCount; ++i)
                {
                    pollers[i] = new IOCompletionPoller(_ioPort);
                }

                _ioCompletionPollers = pollers;
            }
            catch (Exception ex)
            {
                Environment.FailFast("Failed to initialize IO completion pollers.", ex);
            }
            finally
            {
                _threadAdjustmentLock.Release();
            }
        }

        private sealed unsafe class IOCompletionPoller
        {
            private const int NativeEventCapacity =
#if DEBUG
                32;
#else
                1024;
#endif

            private readonly nint _port;
            private readonly Interop.Kernel32.OVERLAPPED_ENTRY* _nativeEvents;
            private readonly ThreadPoolTypedWorkItemQueue<Event, Callback> _events;
            private readonly Thread _thread;

            public IOCompletionPoller(nint port)
            {
                Debug.Assert(port != 0);
                _port = port;

                _nativeEvents =
                    (Interop.Kernel32.OVERLAPPED_ENTRY*)
                    NativeMemory.Alloc(NativeEventCapacity, (nuint)sizeof(Interop.Kernel32.OVERLAPPED_ENTRY));
                _events = new(default);

                // Thread pool threads must start in the default execution context without transferring the context, so
                // using UnsafeStart() instead of Start()
                _thread = new Thread(Poll, SmallStackSizeBytes)
                {
                    IsThreadPoolThread = true,
                    IsBackground = true,
                    Name = ".NET ThreadPool IO"
                };

                // Poller threads are typically expected to be few in number and have to compete for time slices with all other
                // threads that are scheduled to run. They do only a small amount of work and don't run any user code. In
                // situations where frequently, a large number of threads are scheduled to run, a scheduled poller thread may be
                // delayed artificially quite a bit. The poller threads are given higher priority than normal to mitigate that
                // issue. It's unlikely that these threads would starve a system because in such a situation IO completions
                // would stop occurring. Since the number of IO pollers is configurable, avoid having too many poller threads at
                // higher priority.
                if (ProcessorsPerPoller >= 4)
                {
                    _thread.Priority = ThreadPriority.AboveNormal;
                }

                _thread.UnsafeStart();
            }

            private void Poll()
            {
                while (
                    Interop.Kernel32.GetQueuedCompletionStatusEx(
                        _port,
                        _nativeEvents,
                        NativeEventCapacity,
                        out int nativeEventCount,
                        Timeout.Infinite,
                        false))
                {
                    Debug.Assert(nativeEventCount > 0);
                    Debug.Assert(nativeEventCount <= NativeEventCapacity);

                    for (int i = 0; i < nativeEventCount; ++i)
                    {
                        Interop.Kernel32.OVERLAPPED_ENTRY* nativeEvent = &_nativeEvents[i];
                        _events.BatchEnqueue(new Event(nativeEvent->lpOverlapped, nativeEvent->dwNumberOfBytesTransferred));
                    }

                    _events.CompleteBatchEnqueue();
                }

                ThrowHelper.ThrowApplicationException(Marshal.GetHRForLastWin32Error());
            }

            private struct Callback : IThreadPoolTypedWorkItemQueueCallback<Event>
            {
                public void Invoke(Event e)
                {
                    if (NativeRuntimeEventSource.Log.IsEnabled())
                    {
                        NativeRuntimeEventSource.Log.ThreadPoolIODequeue(e.nativeOverlapped);
                    }

                    // The NtStatus code for the operation is in the InternalLow field
                    uint ntStatus = (uint)(nint)e.nativeOverlapped->InternalLow;
                    uint errorCode = Interop.Errors.ERROR_SUCCESS;
                    if (ntStatus != Interop.StatusOptions.STATUS_SUCCESS)
                    {
                        errorCode = Interop.NtDll.RtlNtStatusToDosError((int)ntStatus);
                    }

                    _IOCompletionCallback.PerformSingleIOCompletionCallback(errorCode, e.bytesTransferred, e.nativeOverlapped);
                }
            }

            private readonly struct Event
            {
                public readonly NativeOverlapped* nativeOverlapped;
                public readonly uint bytesTransferred;

                public Event(NativeOverlapped* nativeOverlapped, uint bytesTransferred)
                {
                    this.nativeOverlapped = nativeOverlapped;
                    this.bytesTransferred = bytesTransferred;
                }
            }
        }
    }
}
