// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal sealed unsafe class PollThread
    {
        private const int EventBufferCount =
#if DEBUG
            32;
#else
            1024;
#endif

        // The events of a batch are packed into balanced binary trees, one work item per tree.
        // A tree is unpacked by the thread that executes its root, so the events of a large tree can
        // end up serialized behind that thread unless other threads steal them, which only happens
        // when they run out of work. Capping the size bounds that delay, at the cost of posting more
        // work items - which is fine, since it only happens for batches that are large to begin with.
        // Anything in the 8 - 64 range performs the same, larger values give up the latency benefit.
        private const int MaxTreeSize = 32;

        private static int GetEngineCount()
        {
            // The responsibility of PollThread is to get notifications from epoll|kqueue
            // and schedule corresponding work items to ThreadPool.
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
            bool inlineSocketCompletionsEnabled = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS") == "1";
            if (inlineSocketCompletionsEnabled)
            {
                return Environment.ProcessorCount;
            }

            Architecture architecture = RuntimeInformation.ProcessArchitecture;
            int coresPerEngine = architecture == Architecture.Arm64 || architecture == Architecture.Arm
                ? 12
                : 30;

            return Math.Max(1, (int)Math.Round(Environment.ProcessorCount / (double)coresPerEngine));
        }

        private static readonly PollThread[] s_engines = CreateEngines();
        private static int s_allocateFromEngine = -1;

        private static PollThread[] CreateEngines()
        {
            int engineCount = GetEngineCount();

            var engines = new PollThread[engineCount];

            for (int i = 0; i < engineCount; i++)
            {
                engines[i] = new PollThread();
            }

            return engines;
        }

        /// <summary>
        /// Each <see cref="UnixHandleAsyncContext"/> is assigned an index into this table while registered with a <see cref="PollThread"/>.
        /// <para>The index is used as the <see cref="Interop.Sys.HandleEvent.Data"/> to quickly map events to <see cref="UnixHandleAsyncContext"/>s.</para>
        /// <para>It is also stored in <see cref="UnixHandleAsyncContext.ContextIndex"/> so that we can efficiently remove it when unregistering.</para>
        /// </summary>
        private static UnixHandleAsyncContext?[] s_registeredHandles = [];
        private static readonly Queue<int> s_registeredHandlesFreeList = [];

        private readonly IntPtr _port;
        private readonly Interop.Sys.HandleEvent* _buffer;

        //
        // Pool of reusable PollIOEvent objects to avoid allocating one per event.
        //
        private readonly ConcurrentQueue<PollIOEvent> _eventPool = new ConcurrentQueue<PollIOEvent>();

        // Reusable, preallocated scratch buffer used by the event loop to collect the async events produced by a
        // single WaitForHandleEvents call before packing them into a balanced binary tree.
        // The number of async events can never exceed the number of handle events, which is
        // bounded by EventBufferCount, so this array never needs to grow.
        private readonly PollIOEvent[] _asyncEvents = new PollIOEvent[EventBufferCount];

        //
        // Registers a UnixHandleAsyncContext with a PollThread.
        //
        public static bool TryRegister(IntPtr socketHandle, UnixHandleAsyncContext asyncContext, out Interop.Error error)
        {
            int engineIndex = Math.Abs(Interlocked.Increment(ref s_allocateFromEngine) % s_engines.Length);
            PollThread nextEngine = s_engines[engineIndex];
            return nextEngine.TryRegisterCore(socketHandle, asyncContext, out error);
        }

        private bool TryRegisterCore(IntPtr socketHandle, UnixHandleAsyncContext asyncContext, out Interop.Error error)
        {
            Debug.Assert(asyncContext.ContextIndex == -1);

            lock (s_registeredHandlesFreeList)
            {
                if (!s_registeredHandlesFreeList.TryDequeue(out int index))
                {
                    int previousLength = s_registeredHandles.Length;
                    int newLength = Math.Max(4, 2 * previousLength);

                    Array.Resize(ref s_registeredHandles, newLength);

                    for (int i = previousLength + 1; i < newLength; i++)
                    {
                        s_registeredHandlesFreeList.Enqueue(i);
                    }

                    index = previousLength;
                }

                Debug.Assert(s_registeredHandles[index] is null);

                s_registeredHandles[index] = asyncContext;
                asyncContext.ContextIndex = index;
            }

            error = Interop.Sys.TryChangeHandleEventRegistration(_port, socketHandle, Interop.Sys.HandleEvents.None,
                Interop.Sys.HandleEvents.Read | Interop.Sys.HandleEvents.Write, asyncContext.ContextIndex);
            if (error == Interop.Error.SUCCESS)
            {
                return true;
            }

            Unregister(asyncContext);
            return false;
        }

        public static void Unregister(UnixHandleAsyncContext asyncContext)
        {
            Debug.Assert(asyncContext.ContextIndex >= 0);
            Debug.Assert(ReferenceEquals(s_registeredHandles[asyncContext.ContextIndex], asyncContext));

            lock (s_registeredHandlesFreeList)
            {
                s_registeredHandles[asyncContext.ContextIndex] = null;
                s_registeredHandlesFreeList.Enqueue(asyncContext.ContextIndex);
            }

            asyncContext.ContextIndex = -1;
        }

        private PollThread()
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
                    err = Interop.Sys.CreateHandleEventPort(portPtr);
                    if (err != Interop.Error.SUCCESS)
                    {
                        throw new InvalidOperationException($"Unexpected error: {err}");
                    }
                }

                fixed (Interop.Sys.HandleEvent** bufferPtr = &_buffer)
                {
                    err = Interop.Sys.CreateHandleEventBuffer(EventBufferCount, bufferPtr);
                    if (err != Interop.Error.SUCCESS)
                    {
                        throw new InvalidOperationException($"Unexpected error: {err}");
                    }
                }

                var thread = new Thread(static s => ((PollThread)s!).EventLoop())
                {
                    IsBackground = true,
                    Name = ".NET I/O Events"
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
                    Interop.Error err = Interop.Sys.WaitForHandleEvents(_port, _buffer, &numEvents);
                    if (err != Interop.Error.SUCCESS)
                    {
                        throw new InvalidOperationException($"Unexpected error: {err}");
                    }

                    // The native shim is responsible for ensuring this condition.
                    Debug.Assert(numEvents > 0, $"Unexpected numEvents: {numEvents}");

                    HandleAndDispatchEvents(numEvents);
                }
            }
            catch (Exception e)
            {
                Environment.FailFast("Exception thrown from PollThread event loop: " + e.ToString(), e);
            }
        }

        // Handles the events currently in the buffer, packing the ones that need to be completed
        // asynchronously into balanced binary trees and posting each tree to the thread pool queue as one
        // item. The trees are unpacked into the local queues as the items execute.
        //
        // The JIT is allowed to arbitrarily extend the lifetime of locals, which may retain UnixHandleAsyncContext references,
        // indirectly preventing instances to be finalized, despite being no longer referenced by user code.
        // To avoid this, the event handling logic is delegated to a non-inlined processing method so that the
        // UnixHandleAsyncContext references held in its locals do not extend onto the EventLoop frame across the
        // (potentially long) WaitForHandleEvents wait.
        // See discussion: https://github.com/dotnet/runtime/issues/37064
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandleAndDispatchEvents(int numEvents)
        {
            PollIOEvent[] asyncEvents = _asyncEvents;
            int count = 0;

            foreach (var handleEvent in new ReadOnlySpan<Interop.Sys.HandleEvent>(_buffer, numEvents))
            {
                Debug.Assert((uint)handleEvent.Data < (uint)s_registeredHandles.Length);

                // The context may be null if the handle was unregistered right before the event was processed.
                // The slot in s_registeredHandles may have been reused by a different context, in which case the
                // incorrect handle will notice that no information is available yet and harmlessly retry, waiting for new events.
                UnixHandleAsyncContext? asyncContext = s_registeredHandles[(uint)handleEvent.Data];

                if (asyncContext is not null)
                {
                    if (asyncContext.InlineCompletions)
                    {
                        asyncContext.HandleEventsInline(handleEvent.Events);
                    }
                    else
                    {
                        Interop.Sys.HandleEvents events = asyncContext.ProcessInlineSpeculatively(handleEvent.Events);

                        if (events != Interop.Sys.HandleEvents.None)
                        {
                            PollIOEvent newEvent = RentEvent();
                            newEvent.With(asyncContext, events);
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

                PollIOEvent root = asyncEvents[i];
                LinkChildren(root, new ReadOnlySpan<PollIOEvent>(asyncEvents, i + 1, treeSize - 1));

                ThreadPool.UnsafeQueueUserWorkItem(root, preferLocal: false);
            }

            // Clear the references so the scratch buffer doesn't keep contexts alive.
            Array.Clear(asyncEvents, 0, count);
        }

        private void FreeNativeResources()
        {
            if (_buffer != null)
            {
                Interop.Sys.FreeHandleEventBuffer(_buffer);
            }
            if (_port != (IntPtr)(-1))
            {
                Interop.Sys.CloseHandleEventPort(_port);
            }
        }

        private PollIOEvent RentEvent() =>
            _eventPool.TryDequeue(out PollIOEvent? existingEvent) ?
                existingEvent :
                new PollIOEvent(_eventPool);

        // Arranges the events in the span into a balanced binary tree hanging off the given root.
        private static void LinkChildren(PollIOEvent root, ReadOnlySpan<PollIOEvent> rest)
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

            ReadOnlySpan<PollIOEvent> left = rest.Slice(0, leftCount);
            ReadOnlySpan<PollIOEvent> right = rest.Slice(leftCount);

            root._left = left[0];
            LinkChildren(left[0], left.Slice(1));

            if (!right.IsEmpty)
            {
                root._right = right[0];
                LinkChildren(right[0], right.Slice(1));
            }
        }

        private sealed class PollIOEvent : IThreadPoolWorkItem
        {
            private readonly ConcurrentQueue<PollIOEvent> _pool;
            public PollIOEvent? _left;
            public PollIOEvent? _right;

            public UnixHandleAsyncContext? _asyncContext;
            public Interop.Sys.HandleEvents _events;

            // Assuming that PollIOEvent + overhead of a queue slot takes ~ 64bytes,
            // we will limit the number of events in the pool to 1MB / 64bytes = 16k items
            // to prevent unlimited growth in edge cases.
            // The count of events in flight per engine should normally be much less than this.
            private const int MaxEventPoolCount = 1024 * 1024 / 64;

            public PollIOEvent(ConcurrentQueue<PollIOEvent> pool)
            {
                _pool = pool;
            }

            public void With(UnixHandleAsyncContext asyncContext, Interop.Sys.HandleEvents events)
            {
                _asyncContext = asyncContext;
                _events = events;
            }

            void IThreadPoolWorkItem.Execute()
            {
                // Unpack the child subtrees into the local queue. Each of them will in turn
                // unpack its own children when it executes.
                PollIOEvent? left = _left;
                PollIOEvent? right = _right;

                if (left is not null)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(left, preferLocal: true);
                }
                if (right is not null)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(right, preferLocal: true);
                }

                UnixHandleAsyncContext asyncContext = _asyncContext!;
                Interop.Sys.HandleEvents events = _events;

                if (_pool.Count < MaxEventPoolCount)
                {
                    _asyncContext = null;
                    _events = Interop.Sys.HandleEvents.None;
                    _left = null;
                    _right = null;
                    _pool.Enqueue(this);
                }

                asyncContext.HandleEventsOnThreadPool(events);
            }
        }
    }
}
