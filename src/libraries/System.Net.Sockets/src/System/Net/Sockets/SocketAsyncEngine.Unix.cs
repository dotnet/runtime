// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed unsafe class SocketAsyncEngine
    {
        private const int EventBufferCount =
#if DEBUG
            32;
#else
            1024;
#endif

        // Socket continuations are dispatched to the ThreadPool from the event thread.
        // This avoids continuations blocking the event handling.
        // Setting PreferInlineCompletions allows continuations to run directly on the event thread.
        // PreferInlineCompletions defaults to false and can be set to true using the DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS envvar.
        internal static readonly bool InlineSocketCompletionsEnabled = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS") == "1";

        // The events of a batch are packed into balanced binary trees, one work item per tree.
        // A tree is unpacked by the thread that executes its root, so the events of a large tree can
        // end up serialized behind that thread unless other threads steal them, which only happens
        // when they run out of work. Capping the size bounds that delay, at the cost of posting more
        // work items - which is fine, since it only happens for batches that are large to begin with.
        // Anything in the 8 - 64 range performs the same, larger values give up the latency benefit.
        private const int MaxTreeSize = 32;

        // Set when some socket is given a PreferInlineCompletions value that differs from the
        // process-wide default above. That is done through an experimental API and virtually never
        // happens, so until it does, the event loop can use the default without reading per-context state.
        // This is a one-way latch - it is never reset back to false.
        private static bool s_anyInlineCompletionsOverride;

        internal static void OnInlineCompletionsOverride() => s_anyInlineCompletionsOverride = true;

        private static bool PrefersInlineCompletions(SocketAsyncContext context) =>
            // InlineSocketCompletionsEnabled is a static readonly bool, so in the common case this
            // folds into a constant and the context is not touched at all.
            s_anyInlineCompletionsOverride ? context.PreferInlineCompletions : InlineSocketCompletionsEnabled;

        private static int GetEngineCount()
        {
            // The responsibility of SocketAsyncEngine is to get notifications from epoll|kqueue
            // and schedule corresponding work items to ThreadPool (socket reads and writes).
            //
            // Using TechEmpower benchmarks that generate a LOT of SMALL socket reads and writes under a VERY HIGH load
            // we have observed that a single engine is capable of keeping busy up to thirty x64 and twelve ARM64 CPU Cores.
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
                ? 12
                : 30;

            return Math.Max(1, (int)Math.Round(Environment.ProcessorCount / (double)coresPerEngine));
        }

        private static readonly SocketAsyncEngine[] s_engines = CreateEngines();
        private static int s_allocateFromEngine = -1;

        private static SocketAsyncEngine[] CreateEngines()
        {
            int engineCount = GetEngineCount();

            var engines = new SocketAsyncEngine[engineCount];

            for (int i = 0; i < engineCount; i++)
            {
                engines[i] = new SocketAsyncEngine();
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

        //
        // Pool of reusable SocketIOEvent objects to avoid allocating one per event.
        //
        private readonly ConcurrentQueue<SocketIOEvent> _eventPool = new ConcurrentQueue<SocketIOEvent>();

        // Reusable, preallocated scratch buffer used by the event loop to collect the async events produced by a
        // single WaitForSocketEvents call before packing them into a balanced binary tree.
        // The number of async events can never exceed the number of socket events, which is
        // bounded by EventBufferCount, so this array never needs to grow.
        private readonly SocketIOEvent[] _asyncEvents = new SocketIOEvent[EventBufferCount];

        //
        // Registers the Socket with a SocketAsyncEngine, and returns the associated engine.
        //
        public static bool TryRegisterSocket(IntPtr socketHandle, SocketAsyncContext context, out SocketAsyncEngine? engine, out Interop.Error error)
        {
            int engineIndex = Math.Abs(Interlocked.Increment(ref s_allocateFromEngine) % s_engines.Length);
            SocketAsyncEngine nextEngine = s_engines[engineIndex];
            bool registered = nextEngine.TryRegisterCore(socketHandle, context, out error);
            engine = registered ? nextEngine : null;
            return registered;
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

            error = Interop.Sys.TryChangeSocketEventRegistration(_port, socketHandle, Interop.Sys.SocketEvents.None,
                Interop.Sys.SocketEvents.Read | Interop.Sys.SocketEvents.Write, context.GlobalContextIndex);
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

        private SocketAsyncEngine()
        {
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
                        throw new InternalException(err);
                    }
                }

                fixed (Interop.Sys.SocketEvent** bufferPtr = &_buffer)
                {
                    err = Interop.Sys.CreateSocketEventBuffer(EventBufferCount, bufferPtr);
                    if (err != Interop.Error.SUCCESS)
                    {
                        throw new InternalException(err);
                    }
                }

                var thread = new Thread(static s => ((SocketAsyncEngine)s!).EventLoop())
                {
                    IsBackground = true,
                    Name = ".NET Sockets"
                };
                thread.UnsafeStart(this);
            }
            catch
            {
                FreeNativeResources();
                throw;
            }
        }

        private void EventLoop()
        {
            try
            {
                while (true)
                {
                    int numEvents = EventBufferCount;
                    Interop.Error err = Interop.Sys.WaitForSocketEvents(_port, _buffer, &numEvents);
                    if (err != Interop.Error.SUCCESS)
                    {
                        throw new InternalException(err);
                    }

                    // The native shim is responsible for ensuring this condition.
                    Debug.Assert(numEvents > 0, $"Unexpected numEvents: {numEvents}");

                    HandleAndDispatchSocketEvents(numEvents);
                }
            }
            catch (Exception e)
            {
                Environment.FailFast("Exception thrown from SocketAsyncEngine event loop: " + e.ToString(), e);
            }
        }

        // Handles the socket events currently in the buffer, packing the ones that need to be completed
        // asynchronously into balanced binary trees and posting each tree to the thread pool queue as one
        // item. The trees are unpacked into the local queues as the items execute.
        //
        // The JIT is allowed to arbitrarily extend the lifetime of locals, which may retain SocketAsyncContext references,
        // indirectly preventing Socket instances to be finalized, despite being no longer referenced by user code.
        // To avoid this, the event handling logic is delegated to a non-inlined processing method so that the
        // SocketAsyncContext references held in its locals do not extend onto the EventLoop frame across the
        // (potentially long) WaitForSocketEvents wait.
        // See discussion: https://github.com/dotnet/runtime/issues/37064
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandleAndDispatchSocketEvents(int numEvents)
        {
            SocketIOEvent[] asyncEvents = _asyncEvents;
            int count = 0;

            foreach (var socketEvent in new ReadOnlySpan<Interop.Sys.SocketEvent>(_buffer, numEvents))
            {
                Debug.Assert((uint)socketEvent.Data < (uint)s_registeredContexts.Length);

                // The context may be null if the socket was unregistered right before the event was processed.
                // The slot in s_registeredContexts may have been reused by a different context, in which case the
                // incorrect socket will notice that no information is available yet and harmlessly retry, waiting for new events.
                SocketAsyncContext? context = s_registeredContexts[(uint)socketEvent.Data];

                if (context is not null)
                {
                    if (PrefersInlineCompletions(context))
                    {
                        context.HandleEventsInline(socketEvent.Events);
                    }
                    else
                    {
                        Interop.Sys.SocketEvents events = context.HandleSyncEventsSpeculatively(socketEvent.Events);

                        if (events != Interop.Sys.SocketEvents.None)
                        {
                            SocketIOEvent newEvent = RentEvent();
                            newEvent.With(context, events);
                            asyncEvents[count++] = newEvent;
                        }
                    }
                }
            }

            if (count == 0)
            {
                return;
            }

            for (int i = 0; i < count; i += MaxTreeSize)
            {
                int treeSize = Math.Min(MaxTreeSize, count - i);

                SocketIOEvent root = asyncEvents[i];
                LinkChildren(root, new ReadOnlySpan<SocketIOEvent>(asyncEvents, i + 1, treeSize - 1));

                ThreadPool.UnsafeQueueUserWorkItem(root, preferLocal: false);
            }

            // Clear the references so the scratch buffer doesn't keep contexts alive.
            Array.Clear(asyncEvents, 0, count);
        }

        private void FreeNativeResources()
        {
            if (_buffer != null)
            {
                Interop.Sys.FreeSocketEventBuffer(_buffer);
            }
            if (_port != (IntPtr)(-1))
            {
                Interop.Sys.CloseSocketEventPort(_port);
            }
        }

        private SocketIOEvent RentEvent() =>
            _eventPool.TryDequeue(out SocketIOEvent? existingEvent) ?
                existingEvent :
                new SocketIOEvent(_eventPool);

        // Arranges the events in the span into a balanced binary tree hanging off the given root.
        private static void LinkChildren(SocketIOEvent root, ReadOnlySpan<SocketIOEvent> rest)
        {
            // Events are handed out with null children, either fresh or cleared when recycled.
            Debug.Assert(root._left is null && root._right is null);

            switch (rest.Length)
            {
                case 0:
                    return;

                case 1:
                    root._left = rest[0];
                    return;

                case 2:
                    root._left = rest[0];
                    root._right = rest[1];
                    return;
            }

            // Give the left side the extra element when the count is odd.
            int leftCount = (rest.Length + 1) / 2;

            ReadOnlySpan<SocketIOEvent> left = rest.Slice(0, leftCount);
            ReadOnlySpan<SocketIOEvent> right = rest.Slice(leftCount);

            root._left = left[0];
            LinkChildren(left[0], left.Slice(1));

            if (!right.IsEmpty)
            {
                root._right = right[0];
                LinkChildren(right[0], right.Slice(1));
            }
        }

        private sealed class SocketIOEvent : IThreadPoolWorkItem
        {
            private readonly ConcurrentQueue<SocketIOEvent> _pool;
            public SocketIOEvent? _left;
            public SocketIOEvent? _right;

            public SocketAsyncContext? _context;
            public Interop.Sys.SocketEvents _events;

            // Assuming that SocketIOEvent + overhead of a queue slot takes ~ 64bytes,
            // we will limit the number of events in the pool to 1MB / 64bytes = 16k items
            // to prevent unlimited growth in edge cases.
            // The count of events in flight per engine should normally be much less than this.
            private const int MaxEventPoolCount = 1024 * 1024 / 64;

            public SocketIOEvent(ConcurrentQueue<SocketIOEvent> pool)
            {
                _pool = pool;
            }

            public void With(SocketAsyncContext context, Interop.Sys.SocketEvents events)
            {
                _context = context;
                _events = events;
            }

            void IThreadPoolWorkItem.Execute()
            {
                // Unpack the child subtrees into the local queue. Each of them will in turn
                // unpack its own children when it executes.
                SocketIOEvent? left = _left;
                SocketIOEvent? right = _right;

                if (left is not null)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(left, preferLocal: true);
                }
                if (right is not null)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(right, preferLocal: true);
                }

                SocketAsyncContext context = _context!;
                Interop.Sys.SocketEvents events = _events;

                if (_pool.Count < MaxEventPoolCount)
                {
                    _context = null;
                    _events = Interop.Sys.SocketEvents.None;
                    _left = null;
                    _right = null;
                    _pool.Enqueue(this);
                }

                context.HandleEvents(events);
            }
        }
    }
}
