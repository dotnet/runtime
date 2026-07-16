// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        private readonly nint[] _ioPorts = new nint[IOCompletionPortCount];
        private uint _ioPortSelectorForRegister = unchecked((uint)-1);
        private uint _ioPortSelectorForQueue = unchecked((uint)-1);
        private IOCompletionPoller[]? _ioCompletionPollers;

        private static short DetermineIOCompletionPortCount()
        {
            const short DefaultIOPortCount = 1;
            const short MaxIOPortCount = 1 << 10;

            short ioPortCount =
                AppContextConfigHelper.GetInt16Config(
                    "System.Threading.ThreadPool.IOCompletionPortCount",
                    "DOTNET_ThreadPool_IOCompletionPortCount",
                    DefaultIOPortCount,
                    allowNegative: false);
            return ioPortCount == 0 ? DefaultIOPortCount : Math.Min(ioPortCount, MaxIOPortCount);
        }

        private static int DetermineIOCompletionPollerCount()
        {
            // Named for consistency with SocketAsyncEngine.Unix.cs, this environment variable is checked to override the exact
            // number of IO completion poller threads to use. See the comment in SocketAsyncEngine.Unix.cs about its potential
            // uses. For this implementation, the ProcessorsPerIOPollerThread config option below may be preferable as it may be
            // less machine-specific.
            int ioPollerCount;
            if (uint.TryParse(Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_THREAD_COUNT"), out uint count) &&
                count != 0)
            {
                ioPollerCount = (int)Math.Min(count, (uint)MaxPossibleThreadCount);
            }
            else if (UnsafeInlineIOCompletionCallbacks)
            {
                // In this mode, default to ProcessorCount pollers to ensure that all processors can be utilized if more work
                // happens on the poller threads
                ioPollerCount = Environment.ProcessorCount;
            }
            else
            {
                int processorsPerPoller =
                    AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.ProcessorsPerIOPollerThread", 12, false);
                ioPollerCount = (Environment.ProcessorCount - 1) / processorsPerPoller + 1;
            }

            if (IOCompletionPortCount == 1)
            {
                return ioPollerCount;
            }

            // Use at least one IO poller per port
            if (ioPollerCount <= IOCompletionPortCount)
            {
                return IOCompletionPortCount;
            }

            // Use the same number of IO pollers per port, align up if necessary to make it even
            int rem = ioPollerCount % IOCompletionPortCount;
            if (rem != 0)
            {
                ioPollerCount += IOCompletionPortCount - rem;
            }

            return ioPollerCount;
        }

        private void InitializeIOOnWindows()
        {
            Debug.Assert(IOCompletionPollerCount % IOCompletionPortCount == 0);
            int numConcurrentThreads = IOCompletionPollerCount / IOCompletionPortCount;
            for (int i = 0; i < IOCompletionPortCount; i++)
            {
                _ioPorts[i] = CreateIOCompletionPort(numConcurrentThreads);
            }
        }

        private static nint CreateIOCompletionPort(int numConcurrentThreads)
        {
            nint port =
                Interop.Kernel32.CreateIoCompletionPort(new IntPtr(-1), IntPtr.Zero, UIntPtr.Zero, numConcurrentThreads);
            if (port == 0)
            {
                int hr = Marshal.GetHRForLastWin32Error();
                Environment.FailFast($"Failed to create an IO completion port. HR: {hr}");
            }

            return port;
        }

        public void RegisterForIOCompletionNotifications(nint handle)
        {
            Debug.Assert(_ioPorts != null);

            if (_ioCompletionPollers == null)
            {
                EnsureIOCompletionPollers();
            }

            uint selectedPortIndex =
                IOCompletionPortCount == 1
                    ? 0
                    : Interlocked.Increment(ref _ioPortSelectorForRegister) % (uint)IOCompletionPortCount;
            nint selectedPort = _ioPorts[selectedPortIndex];
            Debug.Assert(selectedPort != 0);
            nint port = Interop.Kernel32.CreateIoCompletionPort(handle, selectedPort, UIntPtr.Zero, 0);
            if (port == 0)
            {
                ThrowHelper.ThrowApplicationException(Marshal.GetHRForLastWin32Error());
            }

            Debug.Assert(port == selectedPort);
        }

        public unsafe void QueueNativeOverlapped(NativeOverlapped* nativeOverlapped)
        {
            Debug.Assert(nativeOverlapped != null);
            Debug.Assert(_ioPorts != null);

            if (_ioCompletionPollers == null)
            {
                EnsureIOCompletionPollers();
            }

            if (NativeRuntimeEventSource.Log.IsEnabled())
            {
                NativeRuntimeEventSource.Log.ThreadPoolIOEnqueue(nativeOverlapped);
            }

            uint selectedPortIndex =
                IOCompletionPortCount == 1
                    ? 0
                    : Interlocked.Increment(ref _ioPortSelectorForQueue) % (uint)IOCompletionPortCount;
            nint selectedPort = _ioPorts[selectedPortIndex];
            Debug.Assert(selectedPort != 0);
            if (!Interop.Kernel32.PostQueuedCompletionStatus(selectedPort, 0, UIntPtr.Zero, (IntPtr)nativeOverlapped))
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
                    pollers[i] = new IOCompletionPoller(_ioPorts[i % IOCompletionPortCount]);
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

        internal sealed unsafe class IOCompletionPoller
        {
            private const int NativeEventCapacity =
#if DEBUG
                32;
#else
                1024;
#endif

            private readonly nint _port;
            private readonly Interop.Kernel32.OVERLAPPED_ENTRY* _nativeEvents;

            // Pool of reusable Event objects to avoid allocating one per completion.
            private readonly ConcurrentQueue<Event>? _eventPool;

            // Reusable, preallocated scratch buffer to collect the events produced by a single
            // GetQueuedCompletionStatusEx call before packing them into a balanced binary tree.
            // The number of events can never exceed NativeEventCapacity, so this array never needs to grow.
            private readonly Event[]? _events;
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
                    _eventPool = new ConcurrentQueue<Event>();
                    _events = new Event[NativeEventCapacity];

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
                _thread.Name = ".NET TP I/O";

                // Thread pool threads must start in the default execution context without transferring the context, so
                // using UnsafeStart() instead of Start()
                _thread.UnsafeStart();
            }

            private void Poll()
            {
                Debug.Assert(_nativeEvents != null);
                Debug.Assert(_eventPool != null);
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

                    Event[] events = _events;
                    int count = 0;
                    for (int i = 0; i < nativeEventCount; ++i)
                    {
                        Interop.Kernel32.OVERLAPPED_ENTRY* nativeEvent = &_nativeEvents[i];
                        if (nativeEvent->lpOverlapped != null) // shouldn't be null since null is not posted
                        {
                            Event newEvent = _eventPool.TryDequeue(out Event? existingEvent) ?
                                existingEvent :
                                new Event(_eventPool);

                            events[count++] = newEvent.With(nativeEvent->lpOverlapped, nativeEvent->dwNumberOfBytesTransferred);
                        }
                    }

                    if (count > 0)
                    {
                        // Pack all the events into a single balanced binary tree and post it to the
                        // thread pool queue as one item. The tree is unpacked into the local
                        // queues as the items execute.
                        Event root = BuildTree(new ReadOnlySpan<Event>(events, 0, count));

                        // Clear the references so the scratch buffer doesn't keep the events alive.
                        Array.Clear(events, 0, count);

                        ThreadPool.UnsafeQueueUserWorkItemInternal(root, preferLocal: false);
                    }
                }

                ThrowHelper.ThrowApplicationException(Marshal.GetHRForLastWin32Error());
            }

            // Builds a balanced binary tree out of the events in the span, returning its root.
            private static Event BuildTree(ReadOnlySpan<Event> events)
            {
                Debug.Assert(!events.IsEmpty);

                Event root = events[0];
                ReadOnlySpan<Event> rest = events.Slice(1);

                // Give the left side the extra element when the count is odd.
                int leftCount = (rest.Length + 1) / 2;

                root._left = leftCount > 0 ? BuildTree(rest.Slice(0, leftCount)) : null;
                root._right = rest.Length > leftCount ? BuildTree(rest.Slice(leftCount)) : null;

                return root;
            }

            private void PollAndInlineCallbacks()
            {
                Debug.Assert(_nativeEvents == null);
                Debug.Assert(_eventPool == null);
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

                    IOCompletionCallbackHelper.PerformSingleIOCompletionCallback(errorCode, bytesTransferred, nativeOverlapped);
                }
            }

            internal sealed partial class Event : IThreadPoolWorkItem
            {
                private readonly ConcurrentQueue<Event> _pool;
                public Event? _left;
                public Event? _right;

                public NativeOverlapped* nativeOverlapped;
                public uint bytesTransferred;

                // Assuming that Event + overhead of a queue slot takes ~ 64 bytes,
                // we will limit the number of events in the pool to 1MB / 64bytes = 16k items
                // to prevent unlimited growth in edge cases.
                // The count of events in flight per poller should normally be much less than this.
                private const int MaxEventPoolCount = 1024 * 1024 / 64;

                public Event(ConcurrentQueue<Event> pool)
                {
                    _pool = pool;
                }

                public Event With(NativeOverlapped* nativeOverlapped, uint bytesTransferred)
                {
                    this.nativeOverlapped = nativeOverlapped;
                    this.bytesTransferred = bytesTransferred;
                    return this;
                }

                void IThreadPoolWorkItem.Execute()
                {
                    // Unpack the child subtrees into the local queue. Each of them will in turn
                    // unpack its own children when it executes.
                    Event? left = _left;
                    Event? right = _right;

                    if (left is not null)
                    {
                        ThreadPool.UnsafeQueueUserWorkItemInternal(left, preferLocal: true);
                    }
                    if (right is not null)
                    {
                        ThreadPool.UnsafeQueueUserWorkItemInternal(right, preferLocal: true);
                    }

                    NativeOverlapped* nativeOverlapped = this.nativeOverlapped;
                    uint bytesTransferred = this.bytesTransferred;

                    if (_pool.Count < MaxEventPoolCount)
                    {
                        this.nativeOverlapped = null;
                        this.bytesTransferred = 0;
                        _left = null;
                        _right = null;
                        _pool.Enqueue(this);
                    }

                    Invoke(nativeOverlapped, bytesTransferred);
                }

                private static void Invoke(NativeOverlapped* nativeOverlapped, uint bytesTransferred)
                {
                    if (NativeRuntimeEventSource.Log.IsEnabled())
                    {
                        NativeRuntimeEventSource.Log.ThreadPoolIODequeue(nativeOverlapped);
                    }

                    // The NtStatus code for the operation is in the InternalLow field
                    uint ntStatus = (uint)(nint)nativeOverlapped->InternalLow;
                    uint errorCode = Interop.Errors.ERROR_SUCCESS;
                    if (!Interop.StatusOptions.NT_SUCCESS(ntStatus))
                    {
                        errorCode = Interop.NtDll.RtlNtStatusToDosError((int)ntStatus);
                    }

                    IOCompletionCallbackHelper.PerformSingleIOCompletionCallback(errorCode, bytesTransferred, nativeOverlapped);
                }
            }
        }
    }
}
