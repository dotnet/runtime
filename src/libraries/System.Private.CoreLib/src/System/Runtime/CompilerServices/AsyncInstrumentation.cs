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

        public enum Flags
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

        public static Flags ActiveFlags => _activeFlags;

        public static Flags UpdateAsyncProfilerFlags(Flags flags)
        {
            if (flags != Flags.Disabled)
            {
                flags |= Flags.AsyncProfiler;
            }

            lock (_lock)
            {
                _asyncProfilerActiveFlags = flags;
                _activeFlags = _asyncProfilerActiveFlags | _tplActiveFlags | _debuggerActiveFlags;

                return _activeFlags;
            }
        }

        public static Flags UpdateTplFlags(EventSource tplEventSource)
        {
            // Until debugger sets its flags directly, piggy back on TPL instrumentation since debugger will enable/disable TPL events
            // when attaching/detaching to the runtime.
            Flags tplFlags = Flags.Disabled;
            Flags debuggerFlags = Flags.Disabled;

            tplFlags |= tplEventSource.IsEnabled(EventLevel.Informational, TplEventSource.Keywords.AsyncCausalitySynchronousWork) ?
                Flags.ResumeAsyncContext |
                Flags.SuspendAsyncContext |
                Flags.CompleteAsyncContext |
                Flags.UnwindAsyncException : 0;

            tplFlags |= tplEventSource.IsEnabled(EventLevel.Informational, TplEventSource.Keywords.AsyncCausalityOperation) ?
                Flags.CreateAsyncContext |
                Flags.CompleteAsyncContext |
                Flags.UnwindAsyncException : 0;

            if (tplFlags != Flags.Disabled)
            {
                tplFlags |= Flags.Tpl;
                debuggerFlags |= DebuggerFlags | Flags.Debugger;
            }

            lock (_lock)
            {
                _tplActiveFlags = tplFlags;
                _debuggerActiveFlags = debuggerFlags;
                _activeFlags = _asyncProfilerActiveFlags | _tplActiveFlags | _debuggerActiveFlags;

                return _activeFlags;
            }
        }

        private static Flags _activeFlags;

        private static Flags _asyncProfilerActiveFlags;

        private static Flags _tplActiveFlags;

        private static Flags _debuggerActiveFlags;

        private static readonly object _lock = new object();

        private const Flags DebuggerFlags =
            Flags.CreateAsyncContext | Flags.SuspendAsyncContext |
            Flags.CompleteAsyncContext | Flags.UnwindAsyncException |
            Flags.ResumeAsyncMethod | Flags.CompleteAsyncMethod;
    }
}
