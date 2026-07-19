// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.Tests
{
    // Verifies the W3C TraceId timeline that AsyncProfilerEventSource surfaces: as Activity.Current changes on
    // a thread, a change-point carrying (osThreadId, timestamp, 16-byte traceId) is emitted so a consumer can
    // resolve the active trace for any (osThreadId, timestamp).
    //
    // Two producers feed one per-thread timeline under a single dedup:
    //   * asynchronous change-points captured at the async create/suspend hooks, written into the per-thread
    //     byte buffer as AsyncEventID.TraceIdChanged (24) and delivered inside the AsyncEvents blob (id 1);
    //   * synchronous change-points raised from Activity.CurrentChanged, routed into the same buffer.
    // Both feed one Stopwatch (QPC) stamped stream, so tests assert on the merged per-thread timeline.
    // This file holds the scenarios and the workloads that drive them. The listener, buffer parser, event
    // manifest, and timeline helpers that decode what the scenarios produce live in the .Parsing.cs partial.
    public partial class AsyncProfilerTraceIdTests
    {
        private const string AsyncProfilerSourceName = "System.Runtime.CompilerServices.AsyncProfilerEventSource";
        private const int FlushCommand = 1;               // AsyncProfilerEventSource.FlushCommand

        // The TraceId keyword is decoupled from the async keywords: enabling it alone must still deliver
        // change-points, and enabling the async keywords without it must deliver none.
        private const EventKeywords TraceIdChangedKeyword = (EventKeywords)0x40000;

        // Async create/suspend/resume for both RuntimeAsync (V2) and StateMachineAsync (V1), so the workload
        // produces async change-points regardless of how it is lowered.
        private const EventKeywords AsyncContextKeywords = (EventKeywords)(
            0x1 | 0x2 | 0x4 |          // Create/Resume/Suspend RuntimeAsyncContext
            0x400 | 0x800 | 0x1000);   // Create/Resume/Suspend StateMachineAsyncContext

        private const EventKeywords AsyncAndTraceIdKeywords = AsyncContextKeywords | TraceIdChangedKeyword;

        // Ensures AsyncProfilerEventSource exists before a listener attaches. Reading Activity.Current runs
        // Activity's static init (registering the synchronous trace-id bridge) but does not construct the event
        // source; the source is built the first time the async instrumentation synchronizes its flags
        // (AsyncInstrumentation.SynchronizeFlags touches AsyncProfilerEventSource.Log), which happens at the
        // first real await in the process. Forcing one here keeps each test self-contained: the listener
        // created next captures the source in OnEventSourceCreated regardless of incidental prior async usage.
        private static void EnsureAsyncProfilerSourceConstructed()
        {
            _ = Activity.Current;
            ForceAwait().GetAwaiter().GetResult();

            static async Task ForceAwait() => await Task.Yield();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void AsyncChangePoint_EmitsTraceIdIntoTimeline()
        {
            EnsureAsyncProfilerSourceConstructed();

            using var listener = new ChangePointListener();
            Assert.NotNull(listener.Source);
            listener.Enable(AsyncAndTraceIdKeywords);

            Activity activity = new Activity("async-op");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();
            string expectedTraceId = activity.TraceId.ToHexString();

            RunScenarioAndFlush(RunAsyncWorkload);

            activity.Stop();
            listener.Disable();

            List<ChangePoint> points = listener.Points.ToList();
            ChangePoint? matched = points.FirstOrDefault(p => p.TraceIdHex == expectedTraceId);
            Assert.True(matched is not null,
                $"No change-point carried the async trace id {expectedTraceId}. points={Describe(points)}");
            Assert.NotEqual(0UL, matched!.OsThreadId);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void AsyncChangePoint_EmitsClearedMarkerWhenActivityCleared()
        {
            // Once an async flow leaves the W3C activity (Activity.Current becomes null), an all-zero
            // change-point is emitted so a consumer knows the trace is no longer active on that thread and a
            // recycled thread cannot inherit a stale trace id.
            EnsureAsyncProfilerSourceConstructed();

            using var listener = new ChangePointListener();
            Assert.NotNull(listener.Source);
            listener.Enable(AsyncAndTraceIdKeywords);

            Activity activity = new Activity("async-op");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();
            string expectedTraceId = activity.TraceId.ToHexString();

            RunScenarioAndFlush(RunAsyncWorkloadThenClearActivity);

            activity.Stop();
            listener.Disable();

            List<ChangePoint> points = listener.Points.ToList();
            Assert.True(points.Any(p => p.TraceIdHex == expectedTraceId),
                $"No change-point carried the async trace id {expectedTraceId}. points={Describe(points)}");
            Assert.True(points.Any(p => p.IsCleared),
                $"No cleared (all-zero) change-point was emitted after the activity was cleared. points={Describe(points)}");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void SyncChangePoint_EmitsTraceIdIntoTimeline()
        {
            // A purely synchronous span on a dedicated (non-async) thread must still produce a change-point,
            // proving the synchronous producer feeds the same timeline as the asynchronous one.
            EnsureAsyncProfilerSourceConstructed();

            using var listener = new ChangePointListener();
            Assert.NotNull(listener.Source);
            listener.Enable(AsyncAndTraceIdKeywords);

            string traceId = RunSynchronousSpanOnDedicatedThread();
            SendFlushCommand();

            listener.Disable();

            List<ChangePoint> points = listener.Points.ToList();
            ChangePoint? matched = points.FirstOrDefault(p => p.TraceIdHex == traceId);
            Assert.True(matched is not null,
                $"No change-point carried the synchronous trace id {traceId}. points={Describe(points)}");
            Assert.NotEqual(0UL, matched!.OsThreadId);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void TraceIdKeywordAlone_DeliversChangePoints()
        {
            // The TraceId keyword is decoupled from the async keywords: enabling only it (no async
            // instrumentation) must still deliver the synchronous change-points, which requires the buffer
            // carrier to be enabled under the TraceId keyword.
            EnsureAsyncProfilerSourceConstructed();

            using var listener = new ChangePointListener();
            Assert.NotNull(listener.Source);
            listener.Enable(TraceIdChangedKeyword);

            string traceId = RunSynchronousSpanOnDedicatedThread();
            SendFlushCommand();

            listener.Disable();

            List<ChangePoint> points = listener.Points.ToList();
            Assert.True(points.Any(p => p.TraceIdHex == traceId),
                $"TraceId keyword alone did not deliver the synchronous trace id {traceId}. points={Describe(points)}");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void NoChangePoints_WhenTraceIdKeywordOff()
        {
            // Enabling the async keywords without the TraceId keyword must not emit any TraceId change-point,
            // and the Activity.CurrentChanged bridge must not subscribe.
            EnsureAsyncProfilerSourceConstructed();

            using var listener = new ChangePointListener();
            Assert.NotNull(listener.Source);
            listener.Enable(AsyncContextKeywords);

            string traceId = RunSynchronousSpanOnDedicatedThread();
            SendFlushCommand();

            listener.Disable();

            List<ChangePoint> points = listener.Points.ToList();
            Assert.False(points.Any(p => p.TraceIdHex == traceId),
                $"Unexpected TraceId change-point for {traceId} while the TraceId keyword was off. points={Describe(points)}");
        }

        // Windows-only: the probe needs the current OS thread id to key into the merged timeline, and
        // GetCurrentThreadId matches the osThreadId the buffer stamps (Thread.CurrentOSThreadId) on Windows.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public void SyncAndAsyncChangePoints_ResolveOnMergedPerThreadTimeline()
        {
            // A synchronous span nested inside an async flow shares the async flow's thread. Because both
            // producers stamp the same Stopwatch clock and key on the OS thread id, a merged per-thread
            // timeline resolves the nested synchronous trace id at the moment it was active -- which is only
            // possible if the two producers fold into one ordered stream.
            EnsureAsyncProfilerSourceConstructed();

            using var listener = new ChangePointListener();
            Assert.NotNull(listener.Source);
            listener.Enable(AsyncAndTraceIdKeywords);

            ulong runStart = (ulong)Stopwatch.GetTimestamp();

            Activity asyncActivity = new Activity("async-op");
            asyncActivity.SetIdFormat(ActivityIdFormat.W3C);
            asyncActivity.Start();
            string asyncTraceId = asyncActivity.TraceId.ToHexString();

            Probe probe = new();
            RunScenarioAndFlush(() => RunAsyncWorkloadWithNestedSyncSpan(probe));

            asyncActivity.Stop();
            ulong runEnd = (ulong)Stopwatch.GetTimestamp();

            listener.Disable();

            List<ChangePoint> points = listener.Points.ToList();
            string diag = Describe(points);

            Assert.True(points.Any(p => p.TraceIdHex == asyncTraceId),
                $"async trace id {asyncTraceId} was never emitted. {diag}");

            // Shared clock: every change-point falls within the QPC window of the run, which is only possible
            // if both producers stamp the same Stopwatch clock.
            Assert.All(points, p =>
                Assert.True(p.Timestamp >= runStart && p.Timestamp <= runEnd,
                    $"change-point timestamp {p.Timestamp} outside run window [{runStart},{runEnd}]. {diag}"));

            Assert.NotEqual("", probe.TraceIdHex);
            Assert.NotEqual(asyncTraceId, probe.TraceIdHex); // nested span is a fresh root, distinct trace

            Dictionary<ulong, List<ChangePoint>> byThread = BuildPerThreadTimelines(points);
            Assert.True(byThread.TryGetValue(probe.OsThreadId, out List<ChangePoint>? timeline),
                $"probe thread {probe.OsThreadId} has no timeline. {diag}");

            string? resolved = Resolve(timeline!, probe.Timestamp);
            Assert.True(resolved == probe.TraceIdHex,
                $"merged timeline resolved ({probe.OsThreadId},{probe.Timestamp}) to '{resolved}', expected '{probe.TraceIdHex}'. " +
                $"timeline={Describe(timeline!)}");
        }

        // Captured inside the workload at a point where the current trace id is known synchronously.
        private sealed class Probe
        {
            public ulong OsThreadId;
            public ulong Timestamp;
            public string TraceIdHex = "";
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private static async Task RunAsyncWorkload()
        {
            // Force real continuation dispatches on thread-pool threads. Activity.Current flows via
            // ExecutionContext, so each suspend/create hook observes the started activity's trace id.
            for (int i = 0; i < 50; i++)
            {
                await Task.Yield();
                await Task.Delay(1).ConfigureAwait(false);
            }
        }

        private static async Task RunAsyncWorkloadThenClearActivity()
        {
            for (int i = 0; i < 25; i++)
            {
                await Task.Yield();
                await Task.Delay(1).ConfigureAwait(false);
            }

            Activity.Current = null;

            for (int i = 0; i < 25; i++)
            {
                await Task.Yield();
                await Task.Delay(1).ConfigureAwait(false);
            }
        }

        private static async Task RunAsyncWorkloadWithNestedSyncSpan(Probe probe)
        {
            // Runs under the caller's async activity (flows in via ExecutionContext). Partway through, drop out
            // of it and run a nested synchronous root span so the synchronous producer fires (a fresh trace id)
            // on the same thread the asynchronous producer is capturing on.
            for (int i = 0; i < 40; i++)
            {
                await Task.Yield();
                await Task.Delay(1).ConfigureAwait(false);

                if (i == 20)
                {
                    Activity? saved = Activity.Current;
                    Activity.Current = null;

                    Activity nested = new Activity("nested-sync-root");
                    nested.SetIdFormat(ActivityIdFormat.W3C);
                    nested.Start();

                    probe.OsThreadId = GetCurrentThreadId();
                    probe.TraceIdHex = nested.TraceId.ToHexString();
                    Thread.SpinWait(50_000);
                    probe.Timestamp = (ulong)Stopwatch.GetTimestamp();
                    Thread.SpinWait(50_000);

                    nested.Stop();
                    Activity.Current = saved;
                }
            }
        }

        private static string RunSynchronousSpanOnDedicatedThread()
        {
            string traceId = "";
            Thread t = new Thread(() =>
            {
                Activity a = new Activity("sync-only-op");
                a.SetIdFormat(ActivityIdFormat.W3C);
                a.Start();
                traceId = a.TraceId.ToHexString();
                Thread.SpinWait(100_000); // synchronous CPU work under the span
                a.Stop();
            });
            t.Start();
            t.Join();
            return traceId;
        }

        // Isolate the state-machine chain on a thread-pool thread (clearing the xunit SynchronizationContext so
        // continuations inline instead of re-queuing), then force a synchronous flush of every per-thread buffer.
        private static void RunScenarioAndFlush(Func<Task> scenario)
        {
            SynchronizationContext? prevCtx = SynchronizationContext.Current;
            int originalThreadId = Environment.CurrentManagedThreadId;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                Task.Run(scenario).GetAwaiter().GetResult();
            }
            finally
            {
                Thread.Sleep(50);
                if (Environment.CurrentManagedThreadId == originalThreadId)
                {
                    SynchronizationContext.SetSynchronizationContext(prevCtx);
                }
                SendFlushCommand();
            }
        }

        private static void SendFlushCommand()
        {
            foreach (EventSource source in EventSource.GetSources())
            {
                if (source.Name == AsyncProfilerSourceName)
                {
                    EventSource.SendCommand(source, (EventCommand)FlushCommand, null);
                }
            }
        }
    }
}
