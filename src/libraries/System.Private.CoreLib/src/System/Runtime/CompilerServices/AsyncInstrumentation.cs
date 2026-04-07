// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace System.Runtime.CompilerServices
{
    internal static class AsyncInstrumentation
    {
        public static bool IsSupported => Debugger.IsSupported || EventSource.IsSupported;

        [Flags]
        public enum Flags : uint
        {
            Disabled = 0x0,
            CreateAsyncContext = 0x1,
            ResumeAsyncContext = 0x2,
            SuspendAsyncContext = 0x4,
            CompleteAsyncContext = 0x8,
            UnwindAsyncException = 0x10,
            ResumeAsyncMethod = 0x20,
            CompleteAsyncMethod = 0x40,
            AsyncProfiler = 0x10000,
            Tpl = 0x20000,
            Debugger = 0x40000
        }

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
            public static bool Tpl(Flags flags) => (Flags.Tpl & flags) != 0;
            public static bool Debugger(Flags flags) => (Flags.Debugger & flags) != 0 && Task.s_asyncDebuggingEnabled;
            public static bool DebuggerOrTpl(Flags flags) => Debugger(flags) || Tpl(flags);
        }

        public static Flags ActiveFlags => s_activeFlags;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Flags SyncActiveFlags()
        {
            Flags flags = ActiveFlags;
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
                    s_activeFlags = s_asyncProfilerActiveFlags | s_tplActiveFlags | s_debuggerActiveFlags;
                }
            }
        }

        public static void UpdateTplFlags(Flags tplFlags)
        {
            // Until debugger sets its flags directly, piggy back on TPL instrumentation since debugger will enable/disable TPL events
            // when attaching/detaching to the runtime.
            Flags debuggerFlags = Flags.Disabled;

            if (tplFlags != Flags.Disabled)
            {
                tplFlags |= Flags.Tpl;
                debuggerFlags |= DebuggerFlags | Flags.Debugger;
            }

            lock (s_lock)
            {
                s_tplActiveFlags = tplFlags;
                s_debuggerActiveFlags = debuggerFlags;
                if (IsInitialized(s_activeFlags))
                {
                    s_activeFlags = s_asyncProfilerActiveFlags | s_tplActiveFlags | s_debuggerActiveFlags;
                }
            }
        }

        private const uint UninitializedFlag = 0x80000000;

        private static bool IsInitialized(Flags flags) => !IsUninitialized(flags);
        private static bool IsUninitialized(Flags flags) => (flags & (Flags)UninitializedFlag) != 0;

        private static Flags InitializeFlags()
        {
            _ = TplEventSource.Log; // Touch TplEventSource to trigger static constructor which will initialize TPL flags if EventSource is supported.

            lock (s_lock)
            {
                if (IsUninitialized(s_activeFlags))
                {
                    s_activeFlags = s_asyncProfilerActiveFlags | s_tplActiveFlags | s_debuggerActiveFlags;
                }

                return s_activeFlags;
            }
        }

        private static Flags s_activeFlags = (Flags)UninitializedFlag;

        private static Flags s_asyncProfilerActiveFlags;

        private static Flags s_tplActiveFlags;

        private static Flags s_debuggerActiveFlags;

        private static readonly object s_lock = new object();

        private const Flags DebuggerFlags =
            Flags.CreateAsyncContext | Flags.SuspendAsyncContext |
            Flags.CompleteAsyncContext | Flags.UnwindAsyncException |
            Flags.ResumeAsyncMethod | Flags.CompleteAsyncMethod;
    }
}
