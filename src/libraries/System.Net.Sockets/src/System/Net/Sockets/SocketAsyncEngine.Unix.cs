// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed unsafe partial class SocketAsyncEngine : IThreadPoolWorkItem
    {
        private const int DefaultEventBufferCount =
#if DEBUG
            32;
#else
            1024;
#endif
        private static readonly int s_eventBufferCount = GetEventBufferCount();

        // Socket continuations are dispatched to the ThreadPool from the event thread.
        // This avoids continuations blocking the event handling.
        // Setting PreferInlineCompletions allows continuations to run directly on the event thread.
        // PreferInlineCompletions defaults to false and can be set to true using the DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS envvar.
        internal static readonly bool InlineSocketCompletionsEnabled = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS") == "1";

#if DEBUG
        /// <summary>
        /// Central registry of DEBUG-only io_uring test environment variables.
        /// These switches are intentionally unsupported for production tuning.
        /// </summary>
        private static class IoUringTestEnvironmentVariables
        {
            internal const string EventBufferCount = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_EVENT_BUFFER_COUNT";
            internal const string QueueEntries = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_QUEUE_ENTRIES";
            internal const string PrepareQueueCapacity = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_PREPARE_QUEUE_CAPACITY";
            internal const string DirectSqe = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_DIRECT_SQE";
            internal const string ZeroCopySend = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_ZERO_COPY_SEND";
            internal const string ForceEagainOnceMask = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_EAGAIN_ONCE_MASK";
            internal const string ForceEcanceledOnceMask = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_ECANCELED_ONCE_MASK";
            internal const string ForceSubmitEpermOnce = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_SUBMIT_EPERM_ONCE";
            internal const string ForceEnterEintrRetryLimitOnce = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_ENTER_EINTR_RETRY_LIMIT_ONCE";
            internal const string ForceKernelVersionUnsupported = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_KERNEL_VERSION_UNSUPPORTED";
            internal const string ForceProvidedBufferRingOomOnce = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_PROVIDED_BUFFER_RING_OOM_ONCE";
            internal const string ProvidedBufferSize = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_PROVIDED_BUFFER_SIZE";
            internal const string AdaptiveBufferSizing = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_ADAPTIVE_BUFFER_SIZING";
            internal const string RegisterBuffers = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_REGISTER_BUFFERS";
        }
#endif

        private static int GetEventBufferCount()
        {
#if DEBUG
            // Test-only knob to make wait-buffer saturation deterministic for io_uring diagnostics coverage.
            // Only available in DEBUG builds so production code never reads test env vars.
            if (OperatingSystem.IsLinux())
            {
                string? configuredValue = Environment.GetEnvironmentVariable(IoUringTestEnvironmentVariables.EventBufferCount);
                if (configuredValue is not null &&
                    int.TryParse(configuredValue, out int parsedValue) &&
                    parsedValue >= 1 &&
                    parsedValue <= DefaultEventBufferCount)
                {
                    return parsedValue;
                }
            }
#endif

            return DefaultEventBufferCount;
        }

        private static int GetEngineCount()
        {
            // The responsibility of SocketAsyncEngine is to get notifications from epoll|kqueue
            // (or io_uring on Linux when enabled in the native shim)
            // and schedule corresponding work items to ThreadPool (socket reads and writes).
            //
            // Using TechEmpower benchmarks that generate a LOT of SMALL socket reads and writes under a VERY HIGH load
            // we have observed that a single engine is capable of keeping busy up to thirty x64 and eight ARM64 CPU Cores.
            //
            // The vast majority of real-life scenarios is never going to generate such a huge load (hundreds of thousands of requests per second)
            // and having a single producer should be almost always enough.
            //
            // We want to be sure that we can handle extreme loads and that's why we have decided to use these values.
            //
            // It's impossible to predict all possible scenarios so we have added a possibility to configure this value using environment variables.
            if (uint.TryParse(Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_THREAD_COUNT"), out uint count))
            {
                return (int)count;
            }

            // When inlining continuations, we default to ProcessorCount to make sure event threads cannot be a bottleneck.
            if (InlineSocketCompletionsEnabled)
            {
                return Environment.ProcessorCount;
            }

            Architecture architecture = RuntimeInformation.ProcessArchitecture;
            int coresPerEngine = architecture == Architecture.Arm64 || architecture == Architecture.Arm
                ? 8
                : 30;

            return Math.Max(1, (int)Math.Round(Environment.ProcessorCount / (double)coresPerEngine));
        }

        private static readonly SocketAsyncEngine[] s_engines = CreateEngines();
        private static int s_allocateFromEngine = -1;
        private static volatile int[]? s_fdEngineAffinity;
        private static int[]? s_cpuToEngineIndex;

        internal static int EngineCount => s_engines.Length;

        internal static SocketAsyncEngine GetEngineByIndex(int index) => s_engines[index];

        internal static int GetEngineIndexForCpu(int cpuIndex)
        {
            int[]? cpuToEngineIndex = s_cpuToEngineIndex;
            if (cpuToEngineIndex is null || (uint)cpuIndex >= (uint)cpuToEngineIndex.Length)
            {
                return -1;
            }

            return Volatile.Read(ref cpuToEngineIndex[cpuIndex]);
        }

        private static SocketAsyncEngine[] CreateEngines()
        {
            int engineCount = GetEngineCount();
            int[]? pinnedCpuIndices = null;
            int[]? cpuToEngineIndex = null;
            LinuxInitializeEngineAffinityTopology(ref engineCount, ref pinnedCpuIndices, ref cpuToEngineIndex);
            if (cpuToEngineIndex is not null)
            {
                Interlocked.Exchange(ref s_cpuToEngineIndex, cpuToEngineIndex);
            }

            var engines = new SocketAsyncEngine[engineCount];

            for (int i = 0; i < engineCount; i++)
            {
                int pinnedCpuIndex = -1;
                if (pinnedCpuIndices is not null && (uint)i < (uint)pinnedCpuIndices.Length)
                {
                    pinnedCpuIndex = pinnedCpuIndices[i];
                }

                engines[i] = new SocketAsyncEngine(i, pinnedCpuIndex);
            }

            return engines;
        }

        /// <summary>
        /// Each <see cref="SocketAsyncContext"/> is assigned an index into this table while registered with a <see cref="SocketAsyncEngine"/>.
        /// <para>The index is used as the <see cref="Interop.Sys.SocketEvent.Data"/> to quickly map events to <see cref="SocketAsyncContext"/>s.</para>
        /// <para>It is also stored in <see cref="SocketAsyncContext.GlobalContextIndex"/> so that we can efficiently remove it when unregistering the socket.</para>
        /// </summary>
        private static SocketAsyncContext?[] s_registeredContexts = [];
        private static readonly Queue<int> s_registeredContextsFreeList = [];

        private readonly IntPtr _port;
        private readonly Interop.Sys.SocketEvent* _buffer;
        private readonly int _engineIndex;
        private readonly int _pinnedCpuIndex;
        private int _eventLoopManagedThreadId;
        private readonly ManualResetEventSlim _ioUringInitSignal = new ManualResetEventSlim(false);
        internal int EngineIndex => _engineIndex;
        internal int PinnedCpuIndex => _pinnedCpuIndex;

        //
        // Queue of events generated by EventLoop() that would be processed by the thread pool
        //
        private readonly SocketIOEventQueue _eventQueue = new SocketIOEventQueue();

        // This flag is used for communication between item enqueuing and workers that process the items.
        // There are two states of this flag:
        // 0: has no guarantees
        // 1: means a worker will check work queues and ensure that
        //    any work items inserted in work queue before setting the flag
        //    are picked up.
        //    Note: The state must be cleared by the worker thread _before_
        //       checking. Otherwise there is a window between finding no work
        //       and resetting the flag, when the flag is in a wrong state.
        //       A new work item may be added right before the flag is reset
        //       without asking for a worker, while the last worker is quitting.
        private int _hasOutstandingThreadRequest;

        //
        // Registers the Socket with a SocketAsyncEngine, and returns the associated engine.
        //
        public static bool TryRegisterSocket(IntPtr socketHandle, SocketAsyncContext context, out SocketAsyncEngine? engine, out Interop.Error error)
        {
            int engineIndex;
            int[]? affinity = s_fdEngineAffinity;
            int fd = checked((int)socketHandle);
            if (affinity is not null && (uint)fd < (uint)affinity.Length)
            {
                int value = Interlocked.Exchange(ref affinity[fd], 0);
                if (value > 0)
                    engineIndex = value - 1;
                else
                    engineIndex = Math.Abs(Interlocked.Increment(ref s_allocateFromEngine) % s_engines.Length);
            }
            else
            {
                engineIndex = Math.Abs(Interlocked.Increment(ref s_allocateFromEngine) % s_engines.Length);
            }

            SocketAsyncEngine nextEngine = s_engines[engineIndex];
            nextEngine._ioUringInitSignal.Wait();
            bool registered = nextEngine.TryRegisterCore(socketHandle, context, out error);
            engine = registered ? nextEngine : null;
            return registered;
        }

        internal static bool TryRegisterSocketWithEngine(
            IntPtr socketHandle,
            SocketAsyncContext context,
            SocketAsyncEngine engine,
            out Interop.Error error)
        {
            engine._ioUringInitSignal.Wait();
            return engine.TryRegisterCore(socketHandle, context, out error);
        }

        private bool TryRegisterCore(IntPtr socketHandle, SocketAsyncContext context, out Interop.Error error)
        {
            Debug.Assert(context.GlobalContextIndex == -1);

            lock (s_registeredContextsFreeList)
            {
                if (!s_registeredContextsFreeList.TryDequeue(out int index))
                {
                    int previousLength = s_registeredContexts.Length;
                    int newLength = Math.Max(4, 2 * previousLength);

                    Array.Resize(ref s_registeredContexts, newLength);

                    for (int i = previousLength + 1; i < newLength; i++)
                    {
                        s_registeredContextsFreeList.Enqueue(i);
                    }

                    index = previousLength;
                }

                Debug.Assert(s_registeredContexts[index] is null);

                s_registeredContexts[index] = context;
                context.GlobalContextIndex = index;
            }

            Interop.Error managedError = default;
            bool managedHandled = false;
            LinuxTryChangeSocketEventRegistration(socketHandle, Interop.Sys.SocketEvents.None,
                Interop.Sys.SocketEvents.Read | Interop.Sys.SocketEvents.Write,
                context.GlobalContextIndex, ref managedError, ref managedHandled);
            if (managedHandled)
            {
                error = managedError;
            }
            else
            {
                error = Interop.Sys.TryChangeSocketEventRegistration(_port, socketHandle, Interop.Sys.SocketEvents.None,
                    Interop.Sys.SocketEvents.Read | Interop.Sys.SocketEvents.Write, context.GlobalContextIndex);
            }
            if (error == Interop.Error.SUCCESS)
            {
                return true;
            }

            UnregisterSocket(context);
            return false;
        }

        public static void UnregisterSocket(SocketAsyncContext context)
        {
            Debug.Assert(context.GlobalContextIndex >= 0);
            Debug.Assert(ReferenceEquals(s_registeredContexts[context.GlobalContextIndex], context));

            lock (s_registeredContextsFreeList)
            {
                s_registeredContexts[context.GlobalContextIndex] = null;
                s_registeredContextsFreeList.Enqueue(context.GlobalContextIndex);
            }

            context.GlobalContextIndex = -1;
        }

        private SocketAsyncEngine(int engineIndex, int pinnedCpuIndex)
        {
            _engineIndex = engineIndex;
            _pinnedCpuIndex = pinnedCpuIndex;
            _port = (IntPtr)(-1);
            try
            {
                //
                // Create the event port and buffer
                //
                Interop.Error err;
                fixed (IntPtr* portPtr = &_port)
                {
                    err = Interop.Sys.CreateSocketEventPort(portPtr);
                    if (err != Interop.Error.SUCCESS)
                    {
                        ThrowInternalException(err);
                    }
                }

                fixed (Interop.Sys.SocketEvent** bufferPtr = &_buffer)
                {
                    err = Interop.Sys.CreateSocketEventBuffer(s_eventBufferCount, bufferPtr);
                    if (err != Interop.Error.SUCCESS)
                    {
                        ThrowInternalException(err);
                    }
                }

                // io_uring initialization is deferred to the event loop thread so that
                // io_uring_setup sets submitter_task to the event loop thread, which is
                // required by DEFER_TASKRUN. TryRegisterSocket waits on _ioUringInitSignal
                // before handing sockets to an engine, so no socket can register before
                // init completes. This wait cannot deadlock because it runs after the
                // static initializer finishes (s_engines must be assigned for
                // TryRegisterSocket to access it).

                var thread = new Thread(static s => ((SocketAsyncEngine)s!).EventLoop())
                {
                    IsBackground = true,
                    Name = ".NET Sockets"
                };
                thread.UnsafeStart(this);
            }
            catch
            {
                // Constructor failure path only: if construction throws, clean up immediately.
                // This path is the sole caller of FreeNativeResources().
                FreeNativeResources();
                throw;
            }
        }

        partial void LinuxDetectAndInitializeIoUring();
        static partial void LinuxInitializeEngineAffinityTopology(ref int engineCount, ref int[]? pinnedCpuIndices, ref int[]? cpuToEngineIndex);
        partial void LinuxPinEventLoopThreadIfConfigured();
        partial void LinuxEventLoopBeforeWait();
        partial void LinuxEventLoopTryCompletionWait(SocketEventHandler handler, ref int numEvents, ref int numCompletions, ref Interop.Error err, ref bool waitHandled);
        partial void LinuxEventLoopAfterIteration();
        partial void LinuxBeforeFreeNativeResources(ref bool closeSocketEventPort);
        partial void LinuxFreeIoUringResources();
        partial void LinuxTryChangeSocketEventRegistration(IntPtr socketHandle, Interop.Sys.SocketEvents currentEvents, Interop.Sys.SocketEvents newEvents, int data, ref Interop.Error error, ref bool handled);
        partial void LinuxWakeIoUringEventLoopForSocketClose();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void WakeIoUringEventLoopForSocketClose() => LinuxWakeIoUringEventLoopForSocketClose();

        [DoesNotReturn]
        [StackTraceHidden]
        private static void ThrowInternalException(Interop.Error error) =>
            throw new InternalException(error);

        [DoesNotReturn]
        [StackTraceHidden]
        private static void ThrowInternalException(string message) =>
            throw new InternalException(message);

        [DoesNotReturn]
        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FailFastEventLoop(Exception exception) =>
            Environment.FailFast($"Exception thrown from SocketAsyncEngine event loop: {exception}", exception);

        private void RecordAndAssertEventLoopThreadIdentity()
        {
            int currentThreadId = Environment.CurrentManagedThreadId;
#if DEBUG
            int previousThreadId = Interlocked.CompareExchange(ref _eventLoopManagedThreadId, currentThreadId, 0);
            Debug.Assert(
                previousThreadId == 0 || previousThreadId == currentThreadId,
                $"SocketAsyncEngine event loop thread changed: previous={previousThreadId}, current={currentThreadId}");
#else
            Interlocked.CompareExchange(ref _eventLoopManagedThreadId, currentThreadId, 0);
#endif
        }

        private void EventLoop()
        {
            try
            {
                RecordAndAssertEventLoopThreadIdentity();
                LinuxPinEventLoopThreadIfConfigured();
                try
                {
                    LinuxDetectAndInitializeIoUring();
                }
                finally
                {
                    _ioUringInitSignal.Set();
                }

                SocketEventHandler handler = new SocketEventHandler(this);
                while (true)
                {
                    LinuxEventLoopBeforeWait();

                    int numEvents = s_eventBufferCount;
                    int numCompletions = 0;
                    Interop.Error err = default;
                    bool waitHandled = false;
                    LinuxEventLoopTryCompletionWait(handler, ref numEvents, ref numCompletions, ref err, ref waitHandled);
                    if (!waitHandled)
                    {
                        err = Interop.Sys.WaitForSocketEvents(_port, handler.Buffer, &numEvents);
                    }

                    if (err != Interop.Error.SUCCESS)
                    {
                        ThrowInternalException(err);
                    }

                    // io_uring completion-mode wait can return with zero surfaced events/completions
                    // when woken only to flush managed prepare/cancel queues.
                    Debug.Assert(waitHandled || numEvents > 0 || numCompletions > 0, $"Unexpected wait result: events={numEvents}, completions={numCompletions}");

                    if (numEvents > 0 && handler.HandleSocketEvents(numEvents))
                    {
                        EnsureWorkerScheduled();
                    }

                    LinuxEventLoopAfterIteration();
                }
            }
            catch (Exception e)
            {
                FailFastEventLoop(e);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureWorkerScheduled()
        {
            // Only one worker is requested at a time to mitigate Thundering Herd problem.
            if (Interlocked.Exchange(ref _hasOutstandingThreadRequest, 1) == 0)
            {
                ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
            }
        }

        void IThreadPoolWorkItem.Execute()
        {
            // We are asking for one worker at a time, thus the state should be 1.
            Debug.Assert(_hasOutstandingThreadRequest == 1);
            _hasOutstandingThreadRequest = 0;

            // Checking for items must happen after resetting the processing state.
            Interlocked.MemoryBarrier();

            SocketIOEventQueue eventQueue = _eventQueue;
            if (!eventQueue.TryDequeue(out SocketIOEvent ev))
            {
                return;
            }

            // The batch that is currently in the queue could have asked only for one worker.
            // We are going to process a workitem, which may take unknown time or even block.
            // In a worst case the current workitem will indirectly depend on progress of other
            // items and that would lead to a deadlock if no one else checks the queue.
            // We must ensure at least one more worker is coming if the queue is not empty.
            if (!eventQueue.IsEmpty)
            {
                EnsureWorkerScheduled();
            }

            int startTimeMs = Environment.TickCount;
            do
            {
                ev.Context.HandleEvents(ev.Events);

                // If there is a constant stream of new events, and/or if user callbacks take long to process an event, this
                // work item may run for a long time. If work items of this type are using up all of the thread pool threads,
                // collectively they may starve other types of work items from running. Before dequeuing and processing another
                // event, check the elapsed time since the start of the work item and yield the thread after some time has
                // elapsed to allow the thread pool to run other work items.
                //
                // The threshold chosen below was based on trying various thresholds and in trying to keep the latency of
                // running another work item low when these work items are using up all of the thread pool worker threads. In
                // such cases, the latency would be something like threshold / proc count. Smaller thresholds were tried and
                // using Stopwatch instead (like 1 ms, 5 ms, etc.), from quick tests they appeared to have a slightly greater
                // impact on throughput compared to the threshold chosen below, though it is slight enough that it may not
                // matter much. Higher thresholds didn't seem to have any noticeable effect.
            } while (Environment.TickCount - startTimeMs < 15 && eventQueue.TryDequeue(out ev));
        }

        private void FreeNativeResources()
        {
            Debug.Assert(
                Volatile.Read(ref _eventLoopManagedThreadId) == 0,
                "FreeNativeResources is only used by constructor-failure cleanup; event loop thread must not have started.");
            bool closeSocketEventPort = true;
            // Linux io_uring teardown may need to close the port first to ensure native
            // ownership is detached before managed operation resources are released.
            LinuxBeforeFreeNativeResources(ref closeSocketEventPort);

            LinuxFreeIoUringResources();

            if (_buffer != null)
            {
                Interop.Sys.FreeSocketEventBuffer(_buffer);
            }

            if (closeSocketEventPort && _port != (IntPtr)(-1))
            {
                Interop.Sys.CloseSocketEventPort(_port);
            }
        }

        // The JIT is allowed to arbitrarily extend the lifetime of locals, which may retain SocketAsyncContext references,
        // indirectly preventing Socket instances to be finalized, despite being no longer referenced by user code.
        // To avoid this, the event handling logic is delegated to a non-inlined processing method.
        // See discussion: https://github.com/dotnet/runtime/issues/37064
        // SocketEventHandler holds an on-stack cache of SocketAsyncEngine members needed by the handler method.
        private readonly partial struct SocketEventHandler
        {
            public Interop.Sys.SocketEvent* Buffer { get; }

            private readonly SocketIOEventQueue _eventQueue;
            private readonly SocketAsyncEngine _engine;

            public SocketEventHandler(SocketAsyncEngine engine)
            {
                _engine = engine;
                Buffer = engine._buffer;
                _eventQueue = engine._eventQueue;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public bool HandleSocketEvents(int numEvents)
            {
                bool enqueuedEvent = false;
                foreach (var socketEvent in new ReadOnlySpan<Interop.Sys.SocketEvent>(Buffer, numEvents))
                {
                    Debug.Assert((uint)socketEvent.Data < (uint)s_registeredContexts.Length);

                    // The context may be null if the socket was unregistered right before the event was processed.
                    // The slot in s_registeredContexts may have been reused by a different context, in which case the
                    // incorrect socket will notice that no information is available yet and harmlessly retry, waiting for new events.
                    SocketAsyncContext? context = s_registeredContexts[(uint)socketEvent.Data];

                    if (context is not null)
                    {
                        if (context.PreferInlineCompletions)
                        {
                            context.HandleEventsInline(socketEvent.Events);
                        }
                        else
                        {
                            Interop.Sys.SocketEvents events = context.HandleSyncEventsSpeculatively(socketEvent.Events);

                            if (events != Interop.Sys.SocketEvents.None)
                            {
                                _eventQueue.Enqueue(new SocketIOEvent(context, events));
                                enqueuedEvent = true;
                            }
                        }
                    }
                }

                return enqueuedEvent;
            }
        }

        private sealed class SocketIOEventQueue
        {
#if TARGET_LINUX
            private readonly MpscQueue<SocketIOEvent> _queue = new MpscQueue<SocketIOEvent>();
#else
            private readonly ConcurrentQueue<SocketIOEvent> _queue = new ConcurrentQueue<SocketIOEvent>();
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            // Event delivery cannot drop entries. Use Enqueue's retrying contract here;
            // io_uring prepare/cancel queues use TryEnqueue where fallback paths exist.
            public void Enqueue(SocketIOEvent socketEvent) => _queue.Enqueue(socketEvent);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryDequeue(out SocketIOEvent socketEvent) => _queue.TryDequeue(out socketEvent);

            public bool IsEmpty => _queue.IsEmpty;
        }

        private readonly struct SocketIOEvent
        {
            public SocketAsyncContext Context { get; }
            public Interop.Sys.SocketEvents Events { get; }

            public SocketIOEvent(SocketAsyncContext context, Interop.Sys.SocketEvents events)
            {
                Context = context;
                Events = events;
            }
        }
    }
}
