// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        // Continuations of IO completions are dispatched to the ThreadPool from IO completion poller threads. This avoids
        // continuations blocking/stalling the IO completion poller threads. Setting UnsafeInlineIOCompletionCallbacks allows
        // continuations to run directly on the IO completion poller thread, but is inherently unsafe due to the potential for
        // those threads to become stalled due to blocking. Sometimes, setting this config value may yield better latency. The
        // config value is named for consistency with SocketAsyncEngine.Unix.cs.
        private static readonly bool UnsafeInlineIOCompletionCallbacks =
            Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS") == "1";

        private static readonly int IOCompletionPollerCount = GetIOCompletionPollerCount();

        private static int GetIOCompletionPollerCount()
        {
            // Named for consistency with SocketAsyncEngine.Unix.cs, this environment variable is checked to override the exact
            // number of IO completion poller threads to use. See the comment in SocketAsyncEngine.Unix.cs about its potential
            // uses. For this implementation, the ProcessorsPerIOPollerThread config option below may be preferable as it may be
            // less machine-specific.
            if (uint.TryParse(Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_THREAD_COUNT"), out uint count))
            {
                return Math.Min((int)count, MaxPossibleThreadCount);
            }

            if (UnsafeInlineIOCompletionCallbacks)
            {
                // In this mode, default to ProcessorCount pollers to ensure that all processors can be utilized if more work
                // happens on the poller threads
                return Environment.ProcessorCount;
            }

            int processorsPerPoller =
                AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.ProcessorsPerIOPollerThread", 12, false);
            return (Environment.ProcessorCount - 1) / processorsPerPoller + 1;
        }

        private static nint CreateIOCompletionPort()
        {
            nint port =
                Interop.Kernel32.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, UIntPtr.Zero, IOCompletionPollerCount);
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

                IOCompletionPoller[] pollers = new IOCompletionPoller[IOCompletionPollerCount];
                for (int i = 0; i < IOCompletionPollerCount; ++i)
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
            private readonly ThreadPoolTypedWorkItemQueue<Event, Callback>? _events;
            private readonly Thread _thread;

            public IOCompletionPoller(nint port)
            {
                Debug.Assert(port != 0);
                _port = port;

                if (!UnsafeInlineIOCompletionCallbacks)
                {
                    _nativeEvents =
                        (Interop.Kernel32.OVERLAPPED_ENTRY*)
                        NativeMemory.Alloc(NativeEventCapacity, (nuint)sizeof(Interop.Kernel32.OVERLAPPED_ENTRY));
                    _events = new ThreadPoolTypedWorkItemQueue<Event, Callback>();

                    // These threads don't run user code, use a smaller stack size
                    _thread = new Thread(Poll, SmallStackSizeBytes);

                    // Poller threads are typically expected to be few in number and have to compete for time slices with all
                    // other threads that are scheduled to run. They do only a small amount of work and don't run any user code.
                    // In situations where frequently, a large number of threads are scheduled to run, a scheduled poller thread
                    // may be delayed artificially quite a bit. The poller threads are given higher priority than normal to
                    // mitigate that issue. It's unlikely that these threads would starve a system because in such a situation
                    // IO completions would stop occurring. Since the number of IO pollers is configurable, avoid having too
                    // many poller threads at higher priority.
                    if (IOCompletionPollerCount * 4 < Environment.ProcessorCount)
                    {
                        _thread.Priority = ThreadPriority.AboveNormal;
                    }
                }
                else
                {
                    // These threads may run user code, use the default stack size
                    _thread = new Thread(PollAndInlineCallbacks);
                }

                _thread.IsThreadPoolThread = true;
                _thread.IsBackground = true;
                _thread.Name = ".NET ThreadPool IO";

                // Thread pool threads must start in the default execution context without transferring the context, so
                // using UnsafeStart() instead of Start()
                _thread.UnsafeStart();
            }

            private void Poll()
            {
                Debug.Assert(_nativeEvents != null);
                Debug.Assert(_events != null);

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
                        if (nativeEvent->lpOverlapped != null) // shouldn't be null since null is not posted
                        {
                            _events.BatchEnqueue(new Event(nativeEvent->lpOverlapped, nativeEvent->dwNumberOfBytesTransferred));
                        }
                    }

                    _events.CompleteBatchEnqueue();
                }

                ThrowHelper.ThrowApplicationException(Marshal.GetHRForLastWin32Error());
            }

            private void PollAndInlineCallbacks()
            {
                Debug.Assert(_nativeEvents == null);
                Debug.Assert(_events == null);

                while (true)
                {
                    uint errorCode = Interop.Errors.ERROR_SUCCESS;
                    if (!Interop.Kernel32.GetQueuedCompletionStatus(
                            _port,
                            out uint bytesTransferred,
                            out _,
                            out nint nativeOverlappedPtr,
                            Timeout.Infinite))
                    {
                        errorCode = (uint)Marshal.GetLastPInvokeError();
                    }

                    var nativeOverlapped = (NativeOverlapped*)nativeOverlappedPtr;
                    if (nativeOverlapped == null) // shouldn't be null since null is not posted
                    {
                        continue;
                    }

                    if (NativeRuntimeEventSource.Log.IsEnabled())
                    {
                        NativeRuntimeEventSource.Log.ThreadPoolIODequeue(nativeOverlapped);
                    }

                    _IOCompletionCallback.PerformSingleIOCompletionCallback(errorCode, bytesTransferred, nativeOverlapped);
                }
            }

            private struct Callback : IThreadPoolTypedWorkItemQueueCallback<Event>
            {
                public static void Invoke(Event e)
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
