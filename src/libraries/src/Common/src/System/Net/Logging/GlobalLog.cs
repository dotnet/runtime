// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace System.Net
{
    internal static class GlobalLog
    {
        [ThreadStatic]
        private static Stack<ThreadKinds> t_ThreadKindStack;

        private static Stack<ThreadKinds> ThreadKindStack
        {
            get
            {
                if (t_ThreadKindStack == null)
                {
                    t_ThreadKindStack = new Stack<ThreadKinds>();
                }

                return t_ThreadKindStack;
            }
        }

        internal static ThreadKinds CurrentThreadKind
        {
            get
            {
                return ThreadKindStack.Count > 0 ? ThreadKindStack.Peek() : ThreadKinds.Other;
            }
        }

        internal static IDisposable SetThreadKind(ThreadKinds kind)
        {
            if ((kind & ThreadKinds.SourceMask) != ThreadKinds.Unknown)
            {
                throw new InternalException();
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
                EventSourceLogging.Log.WarningMessage("Thread changed from User to System; user's thread shouldn't be hijacked.");
            }

            if ((threadKind & ThreadKinds.Async) != 0 && (kind & ThreadKinds.Sync) != 0)
            {
                EventSourceLogging.Log.WarningMessage("Thread changed from Async to Sync, may block an Async thread.");
            }
            else if ((threadKind & (ThreadKinds.Other | ThreadKinds.CompletionPort)) == 0 && (kind & ThreadKinds.Sync) != 0)
            {
                EventSourceLogging.Log.WarningMessage("Thread from a limited resource changed to Sync, may deadlock or bottleneck.");
            }

            ThreadKindStack.Push(
                (((kind & ThreadKinds.OwnerMask) == 0 ? threadKind : kind) & ThreadKinds.OwnerMask) |
                (((kind & ThreadKinds.SyncMask) == 0 ? threadKind : kind) & ThreadKinds.SyncMask) |
                (kind & ~(ThreadKinds.OwnerMask | ThreadKinds.SyncMask)) |
                source);

            if (CurrentThreadKind != threadKind)
            {
                Print("Thread becomes:(" + CurrentThreadKind.ToString() + ")");
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
                    throw new InternalException();
                }

                ThreadKinds previous = ThreadKindStack.Pop();

                if (CurrentThreadKind != previous)
                {
                    Print("Thread reverts:(" + CurrentThreadKind.ToString() + ")");
                }
            }
        }

        internal static void SetThreadSource(ThreadKinds source)
        {
            if ((source & ThreadKinds.SourceMask) != source || source == ThreadKinds.Unknown)
            {
                throw new ArgumentException("Must specify the thread source.", "source");
            }

            if (ThreadKindStack.Count == 0)
            {
                ThreadKindStack.Push(source);
                return;
            }

            if (ThreadKindStack.Count > 1)
            {
                EventSourceLogging.Log.WarningMessage("SetThreadSource must be called at the base of the stack, or the stack has been corrupted.");
                while (ThreadKindStack.Count > 1)
                {
                    ThreadKindStack.Pop();
                }
            }

            if (ThreadKindStack.Peek() != source)
            {
                EventSourceLogging.Log.WarningMessage("The stack has been corrupted.");
                ThreadKinds last = ThreadKindStack.Pop() & ThreadKinds.SourceMask;
                Assert(last == source || last == ThreadKinds.Other, "Thread source changed.|Was:({0}) Now:({1})", last, source);
                ThreadKindStack.Push(source);
            }
        }

        internal static void ThreadContract(ThreadKinds kind, string errorMsg)
        {
            ThreadContract(kind, ThreadKinds.SafeSources, errorMsg);
        }

        internal static void ThreadContract(ThreadKinds kind, ThreadKinds allowedSources, string errorMsg)
        {
            if ((kind & ThreadKinds.SourceMask) != ThreadKinds.Unknown || (allowedSources & ThreadKinds.SourceMask) != allowedSources)
            {
                throw new InternalException();
            }

            ThreadKinds threadKind = CurrentThreadKind;
            Assert((threadKind & allowedSources) != 0, errorMsg, "Thread Contract Violation.|Expected source:({0}) Actual source:({1})", allowedSources, threadKind & ThreadKinds.SourceMask);
            Assert((threadKind & kind) == kind, errorMsg, "Thread Contract Violation.|Expected kind:({0}) Actual kind:({1})", kind, threadKind & ~ThreadKinds.SourceMask);
        }

        public static void Print(string msg)
        {
            EventSourceLogging.Log.DebugMessage(msg);
        }

        public static void Enter(string functionName)
        {
            EventSourceLogging.Log.FunctionStart(functionName);
        }

        public static void Enter(string functionName, string parameters)
        {
            EventSourceLogging.Log.FunctionStart(functionName, parameters);
        }

        public static void Assert(bool condition, string messageFormat, params object[] data)
        {
            if (!condition)
            {
                string fullMessage = string.Format(CultureInfo.InvariantCulture, messageFormat, data);
                int pipeIndex = fullMessage.IndexOf('|');
                if (pipeIndex == -1)
                {
                    Assert(fullMessage);
                }
                else
                {
                    int detailLength = fullMessage.Length - pipeIndex - 1;
                    Assert(fullMessage.Substring(0, pipeIndex), detailLength > 0 ? fullMessage.Substring(pipeIndex + 1, detailLength) : null);
                }
            }
        }

        public static void Assert(string message)
        {
            Assert(message, null);
        }

        public static void Assert(string message, string detailMessage)
        {
            try
            {
                EventSourceLogging.Log.AssertFailed(message, detailMessage);
            }
            finally
            {
                Debug.Fail(message, detailMessage);
            }
        }

        public static void Leave(string functionName)
        {
            EventSourceLogging.Log.FunctionStop(functionName);
        }

        public static void Leave(string functionName, string result)
        {
            EventSourceLogging.Log.FunctionStop(functionName, result);
        }

        public static void Leave(string functionName, int returnval)
        {
            EventSourceLogging.Log.FunctionStop(functionName, returnval.ToString());
        }

        public static void Leave(string functionName, bool returnval)
        {
            EventSourceLogging.Log.FunctionStop(functionName, returnval.ToString());
        }

        public static void Dump(byte[] buffer, int length)
        {
            Dump(buffer, 0, length);
        }

        public static void Dump(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
            {
                EventSourceLogging.Log.WarningDumpArray("buffer is null");
                return;
            }

            if (offset >= buffer.Length)
            {
                EventSourceLogging.Log.WarningDumpArray("offset out of range");
                return;
            }

            if ((length < 0) || (length > buffer.Length - offset))
            {
                EventSourceLogging.Log.WarningDumpArray("length out of range");
                return;
            }

            var bufferSegment = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bufferSegment[i] = buffer[offset + i];
            }

            EventSourceLogging.Log.DebugDumpArray(bufferSegment);
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
