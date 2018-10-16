// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Net
{
    internal static class DebugThreadTracking
    {
        [ThreadStatic]
        private static Stack<ThreadKinds> t_threadKindStack;

        private static Stack<ThreadKinds> ThreadKindStack => t_threadKindStack ?? (t_threadKindStack = new Stack<ThreadKinds>());

        internal static ThreadKinds CurrentThreadKind => ThreadKindStack.Count > 0 ? ThreadKindStack.Peek() : ThreadKinds.Other;

        internal static IDisposable SetThreadKind(ThreadKinds kind)
        {
            if ((kind & ThreadKinds.SourceMask) != ThreadKinds.Unknown)
            {
                throw new InternalException(kind);
            }

            // Ignore during shutdown.
            if (Environment.HasShutdownStarted)
            {
                return null;
            }

            ThreadKinds threadKind = CurrentThreadKind;
            ThreadKinds source = threadKind & ThreadKinds.SourceMask;

            // Special warnings when doing dangerous things on a thread.
            if ((threadKind & ThreadKinds.User) != 0 && (kind & ThreadKinds.System) != 0)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, "Thread changed from User to System; user's thread shouldn't be hijacked.");
            }

            if ((threadKind & ThreadKinds.Async) != 0 && (kind & ThreadKinds.Sync) != 0)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, "Thread changed from Async to Sync, may block an Async thread.");
            }
            else if ((threadKind & (ThreadKinds.Other | ThreadKinds.CompletionPort)) == 0 && (kind & ThreadKinds.Sync) != 0)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, "Thread from a limited resource changed to Sync, may deadlock or bottleneck.");
            }

            ThreadKindStack.Push(
                (((kind & ThreadKinds.OwnerMask) == 0 ? threadKind : kind) & ThreadKinds.OwnerMask) |
                (((kind & ThreadKinds.SyncMask) == 0 ? threadKind : kind) & ThreadKinds.SyncMask) |
                (kind & ~(ThreadKinds.OwnerMask | ThreadKinds.SyncMask)) |
                source);

            if (CurrentThreadKind != threadKind)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"Thread becomes:({CurrentThreadKind})");
            }

            return new ThreadKindFrame();
        }

        private class ThreadKindFrame : IDisposable
        {
            private readonly int _frameNumber;

            internal ThreadKindFrame()
            {
                _frameNumber = ThreadKindStack.Count;
            }

            void IDisposable.Dispose()
            {
                // Ignore during shutdown.
                if (Environment.HasShutdownStarted)
                {
                    return;
                }

                if (_frameNumber != ThreadKindStack.Count)
                {
                    throw new InternalException(_frameNumber);
                }

                ThreadKinds previous = ThreadKindStack.Pop();

                if (CurrentThreadKind != previous && NetEventSource.IsEnabled)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Thread reverts:({CurrentThreadKind})");
                }
            }
        }

        internal static void SetThreadSource(ThreadKinds source)
        {
            if ((source & ThreadKinds.SourceMask) != source || source == ThreadKinds.Unknown)
            {
                throw new ArgumentException("Must specify the thread source.", nameof(source));
            }

            if (ThreadKindStack.Count == 0)
            {
                ThreadKindStack.Push(source);
                return;
            }

            if (ThreadKindStack.Count > 1)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, "SetThreadSource must be called at the base of the stack, or the stack has been corrupted.");
                while (ThreadKindStack.Count > 1)
                {
                    ThreadKindStack.Pop();
                }
            }

            if (ThreadKindStack.Peek() != source)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, "The stack has been corrupted.");
                ThreadKinds last = ThreadKindStack.Pop() & ThreadKinds.SourceMask;
                if (last != source && last != ThreadKinds.Other && NetEventSource.IsEnabled)
                {
                    NetEventSource.Fail(null, $"Thread source changed.|Was:({last}) Now:({source})");
                }
                ThreadKindStack.Push(source);
            }
        }
    }

    [Flags]
    internal enum ThreadKinds
    {
        Unknown = 0x0000,

        // Mutually exclusive.
        User = 0x0001,     // Thread has entered via an API.
        System = 0x0002,     // Thread has entered via a system callback (e.g. completion port) or is our own thread.

        // Mutually exclusive.
        Sync = 0x0004,     // Thread should block.
        Async = 0x0008,     // Thread should not block.

        // Mutually exclusive, not always known for a user thread.  Never changes.
        Timer = 0x0010,     // Thread is the timer thread.  (Can't call user code.)
        CompletionPort = 0x0020,     // Thread is a ThreadPool completion-port thread.
        Worker = 0x0040,     // Thread is a ThreadPool worker thread.
        Finalization = 0x0080,     // Thread is the finalization thread.
        Other = 0x0100,     // Unknown source.

        OwnerMask = User | System,
        SyncMask = Sync | Async,
        SourceMask = Timer | CompletionPort | Worker | Finalization | Other,

        // Useful "macros"
        SafeSources = SourceMask & ~(Timer | Finalization),  // Methods that "unsafe" sources can call must be explicitly marked.
        ThreadPool = CompletionPort | Worker,               // Like Thread.CurrentThread.IsThreadPoolThread
    }
}
