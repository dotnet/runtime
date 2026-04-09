// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace System.Runtime.CompilerServices
{
    internal static class AsyncInstrumentation
    {
        public static bool IsSupported => Debugger.IsSupported && EventSource.IsSupported;

        [Flags]
        public enum Flags : uint
        {
            Disabled = 0x0,

            // Bit 1 - 24 reserved for async instrumentation points.
            CreateAsyncContext = 0x1,
            ResumeAsyncContext = 0x2,
            SuspendAsyncContext = 0x4,
            CompleteAsyncContext = 0x8,
            UnwindAsyncException = 0x10,
            ResumeAsyncMethod = 0x20,
            CompleteAsyncMethod = 0x40,

            // Bit 25 - 31 reserved for instrumentation clients.
            AsyncProfiler = 0x1000000,
            AsyncDebugger = 0x2000000,

            // Bit 32 reserved for initialization state.
            Uninitialized = 0x80000000
        }

        public const Flags DefaultFlags =
            Flags.CreateAsyncContext | Flags.ResumeAsyncContext | Flags.SuspendAsyncContext |
            Flags.CompleteAsyncContext | Flags.UnwindAsyncException |
            Flags.ResumeAsyncMethod | Flags.CompleteAsyncMethod;

        public static class IsEnabled
        {
            public static bool CreateAsyncContext(Flags flags) => (Flags.CreateAsyncContext & flags) != 0;
            public static bool ResumeAsyncContext(Flags flags) => (Flags.ResumeAsyncContext & flags) != 0;
            public static bool SuspendAsyncContext(Flags flags) => (Flags.SuspendAsyncContext & flags) != 0;
            public static bool CompleteAsyncContext(Flags flags) => (Flags.CompleteAsyncContext & flags) != 0;
            public static bool UnwindAsyncException(Flags flags) => (Flags.UnwindAsyncException & flags) != 0;
            public static bool ResumeAsyncMethod(Flags flags) => (Flags.ResumeAsyncMethod & flags) != 0;
            public static bool CompleteAsyncMethod(Flags flags) => (Flags.CompleteAsyncMethod & flags) != 0;
            public static bool AsyncProfiler(Flags flags) => (Flags.AsyncProfiler & flags) != 0;
            public static bool AsyncDebugger(Flags flags) => (Flags.AsyncDebugger & flags) != 0 && Task.s_asyncDebuggingEnabled;
        }

        public static Flags ActiveFlags => s_activeFlags;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Flags SyncActiveFlags()
        {
            Flags flags = s_activeFlags;
            if (IsUninitialized(flags))
            {
                return InitializeFlags();
            }
            return flags;
        }

        public static void UpdateAsyncProfilerFlags(Flags asyncProfilerFlags)
        {
            if (asyncProfilerFlags != Flags.Disabled)
            {
                asyncProfilerFlags |= Flags.AsyncProfiler;
            }

            lock (s_lock)
            {
                s_asyncProfilerActiveFlags = asyncProfilerFlags;
                if (IsInitialized(s_activeFlags))
                {
                    s_activeFlags = s_asyncProfilerActiveFlags | s_asyncDebuggerActiveFlags;
                }
            }
        }

        public static void UpdateAsyncDebuggerFlags(Flags asyncDebuggerFlags)
        {
            if (asyncDebuggerFlags != Flags.Disabled)
            {
                asyncDebuggerFlags |= Flags.AsyncDebugger;
            }

            lock (s_lock)
            {
                s_asyncDebuggerActiveFlags = asyncDebuggerFlags;
                if (IsInitialized(s_activeFlags))
                {
                    s_activeFlags = s_asyncProfilerActiveFlags | s_asyncDebuggerActiveFlags;
                }
            }
        }

        private static Flags InitializeFlags()
        {
            _ = TplEventSource.Log; // Touch TplEventSource to trigger static constructor which will initialize TPL flags if EventSource is supported.

            lock (s_lock)
            {
                if (IsUninitialized(s_activeFlags))
                {
                    s_activeFlags = s_asyncProfilerActiveFlags | s_asyncDebuggerActiveFlags;
                }

                return s_activeFlags;
            }
        }

        private static bool IsInitialized(Flags flags) => !IsUninitialized(flags);

        private static bool IsUninitialized(Flags flags) => (flags & Flags.Uninitialized) != 0;

        private static Flags s_activeFlags = Flags.Uninitialized;

        private static Flags s_asyncProfilerActiveFlags;

        private static Flags s_asyncDebuggerActiveFlags;

        private static readonly Lock s_lock = new();
    }
}
