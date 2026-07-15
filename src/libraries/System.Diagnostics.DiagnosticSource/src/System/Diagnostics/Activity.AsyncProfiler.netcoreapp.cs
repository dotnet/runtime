// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics
{
    partial class Activity
    {
        static Activity()
        {
            // Register the synchronous trace-id bridge on first use of Activity. Registration only stores a
            // keyword-toggle callback in a lightweight CoreLib holder; it does not construct the
            // AsyncProfilerEventSource, which is built only when a diagnostics session enables it.
            AsyncProfilerTraceIdBridge.Initialize();
        }

        // Copies the current Activity's 16-byte trace id into <paramref name="traceId"/>. The trace id is a
        // W3C concept, so this returns false (leaving the span untouched) when there is no current Activity or
        // it is not in the W3C id format. Called from System.Private.CoreLib via UnsafeAccessor; CoreLib cannot
        // reference this assembly directly (it is the one referencing CoreLib), so the reader lives here.
        internal static bool TryGetCurrentTraceId(Span<byte> traceId)
        {
            Activity? activity = Current;
            if (activity is null || activity.IdFormat != ActivityIdFormat.W3C)
            {
                return false;
            }

            activity.TraceId.CopyTo(traceId);
            return true;
        }
    }

    // Bridges the synchronous Activity.Current timeline into System.Private.CoreLib's
    // AsyncProfilerEventSource. Initialized from Activity's static ctor (first use of Activity). CoreLib pushes
    // keyword on/off transitions, and this type (un)subscribes Activity.CurrentChanged accordingly. On each
    // change it forwards the new Activity's trace id (or a zero "cleared" marker) to CoreLib, which adds the OS
    // thread id and routes it to the per-thread buffer.
    //
    // NET-only: relies on UnsafeAccessor/UnsafeAccessorType, absent on the netstandard2.0 and net462 targets
    // this assembly also builds. Compiled only for .NETCoreApp via the csproj (see Activity.AsyncProfiler.netcoreapp.cs).
    internal static class AsyncProfilerTraceIdBridge
    {
        private static int s_subscribed;

        internal static void Initialize()
        {
            Register(null, OnKeywordToggled);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Register")]
            static extern void Register(
                [UnsafeAccessorType("System.Runtime.CompilerServices.AsyncProfilerTraceIdKeyword, System.Private.CoreLib")] object? keywordType,
                Action<bool> callback);
        }

        private static void OnKeywordToggled(bool enabled)
        {
            if (enabled)
            {
                if (Interlocked.Exchange(ref s_subscribed, 1) == 0)
                {
                    Activity.CurrentChanged += OnCurrentChanged;
                }
            }
            else
            {
                if (Interlocked.Exchange(ref s_subscribed, 0) == 1)
                {
                    Activity.CurrentChanged -= OnCurrentChanged;
                }
            }
        }

        private static void OnCurrentChanged(object? sender, ActivityChangedEventArgs e)
        {
            Span<byte> traceId = stackalloc byte[16];
            Activity? current = e.Current;
            if (current is not null && current.IdFormat == ActivityIdFormat.W3C)
            {
                current.TraceId.CopyTo(traceId);
            }
            else
            {
                traceId.Clear(); // all-zero "cleared" marker: no active W3C trace on this thread
            }

            // Dedup and baseline-after-reenable are owned by the per-thread context in CoreLib (one last-seen
            // shared with the async change-points), so this bridge forwards every transition and lets CoreLib
            // decide whether it is a real change.
            EmitCurrentTraceIdChanged(null, traceId);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "EmitCurrentTraceIdChanged")]
            static extern void EmitCurrentTraceIdChanged(
                [UnsafeAccessorType("System.Runtime.CompilerServices.AsyncProfilerEventSource, System.Private.CoreLib")] object? asyncProfilerType,
                ReadOnlySpan<byte> traceId);
        }
    }
}
