// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    internal static class AsyncInstrumentation
    {
        public static bool IsAsyncProfilerSupported => EventSource.IsSupported;
        public static bool IsAsyncDebuggerSupported => Debugger.IsSupported;
        public static bool IsTplSupported => EventSource.IsSupported;

        public static bool IsSupported
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsAsyncProfilerSupported || IsTplSupported || IsAsyncDebuggerSupported;
        }

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
            Tpl = 0x4000000,

            // Bit 32 reserved for synchronization flag.
            Synchronize = 0x80000000
        }

        public const Flags DefaultProfilerFlags =
            Flags.ResumeAsyncContext | Flags.SuspendAsyncContext | Flags.CompleteAsyncContext;

        public const Flags DefaultDebuggerFlags =
            Flags.CreateAsyncContext | Flags.ResumeAsyncContext | Flags.SuspendAsyncContext |
            Flags.CompleteAsyncContext | Flags.UnwindAsyncException |
            Flags.ResumeAsyncMethod | Flags.CompleteAsyncMethod;

        public const Flags DefaultTplFlags = Flags.Tpl;

        public static class IsEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool CreateAsyncContext(Flags flags) => (Flags.CreateAsyncContext & flags) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool ResumeAsyncContext(Flags flags) => (Flags.ResumeAsyncContext & flags) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool SuspendAsyncContext(Flags flags) => (Flags.SuspendAsyncContext & flags) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool CompleteAsyncContext(Flags flags) => (Flags.CompleteAsyncContext & flags) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool UnwindAsyncException(Flags flags) => (Flags.UnwindAsyncException & flags) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool ResumeAsyncMethod(Flags flags) => (Flags.ResumeAsyncMethod & flags) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool CompleteAsyncMethod(Flags flags) => (Flags.CompleteAsyncMethod & flags) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool AsyncProfiler(Flags flags) => AsyncInstrumentation.IsAsyncProfilerSupported && (Flags.AsyncProfiler & flags) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool AsyncDebugger(Flags flags) => AsyncInstrumentation.IsAsyncDebuggerSupported && (Flags.AsyncDebugger & flags) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Tpl(Flags flags) => AsyncInstrumentation.IsTplSupported && (Flags.Tpl & flags) != 0;
        }

        public static bool IsActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AsyncInstrumentation.IsSupported && s_activeFlags != Flags.Disabled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Flags LoadFlags()
        {
            if (!IsSupported)
            {
                return Flags.Disabled;
            }
            return LoadAndSynchronizeFlags(s_activeFlags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LoadFlags(out Flags flags)
        {
            if (!IsSupported)
            {
                flags = Flags.Disabled;
                return false;
            }
            flags = LoadAndSynchronizeFlags(s_activeFlags);
            return flags != Flags.Disabled;
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
                s_activeFlags |= Flags.Synchronize;
            }
        }

        public static void UpdateTplFlags(Flags tplFlags)
        {
            if (tplFlags != Flags.Disabled)
            {
                tplFlags |= Flags.Tpl;
            }

            lock (s_lock)
            {
                s_tplActiveFlags = tplFlags;
                s_activeFlags |= Flags.Synchronize;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Flags LoadAndSynchronizeFlags(Flags flags)
        {
            if ((flags & Flags.Synchronize) != 0)
            {
                return SynchronizeFlags();
            }
            return flags;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Flags SynchronizeFlags()
        {
            if (IsTplSupported)
            {
                _ = TplEventSource.Log; // Touch TplEventSource to trigger static constructor which will initialize TPL flags if EventSource is supported.
            }

            if (IsAsyncProfilerSupported)
            {
                _ = AsyncProfilerEventSource.Log; // Touch AsyncProfilerEventSource to trigger static constructor which will initialize async profiler flags if EventSource is supported.
            }

            lock (s_lock)
            {
                // Read Task.s_asyncDebuggingEnabled live: unlike the profiler and TPL clients (which push their
                // state via UpdateAsyncProfilerFlags/UpdateTplFlags), the debugger sets s_asyncDebuggingEnabled
                // directly and, in the same update, sets the Flags.Synchronize bit on s_activeFlags to trigger this
                // re-sync. Keep this a live read and keep the Synchronize bit value/composition stable; the debugger
                // depends on it to make the AsyncDebugger client observable at the instrumentation checkpoints.
                Flags asyncDebuggerActiveFlags = Flags.Disabled;
                if (Task.s_asyncDebuggingEnabled)
                {
                    asyncDebuggerActiveFlags = DefaultDebuggerFlags | Flags.AsyncDebugger;
                }

                s_activeFlags = (s_asyncProfilerActiveFlags | s_tplActiveFlags | asyncDebuggerActiveFlags) & ~Flags.Synchronize;
                return s_activeFlags;
            }
        }

        private static Flags s_activeFlags = Flags.Synchronize;

        private static Flags s_asyncProfilerActiveFlags;

        private static Flags s_tplActiveFlags;

        private static readonly Lock s_lock = new();
    }
}
