// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;
using PauseSample = (ulong Duration, uint Count, int Depth, int Type);

namespace System.Diagnostics.Metrics.Tests
{
    public class RuntimeMetricsTests(ITestOutputHelper output)
    {
        private const string GreaterThanZeroMessage = "Expected value to be greater than zero.";
        private const string GreaterThanOrEqualToZeroMessage = "Expected value to be greater than or equal to zero.";
        private const string RuntimeProviderName = "Microsoft-Windows-DotNETRuntime";
        private const int GCPauseEventId = 304;
        private const EventKeywords GCPauseKeyword = (EventKeywords)0x04000000;

        private static readonly string[] s_genNames = ["gen0", "gen1", "gen2", "loh", "poh"];

        // On some platforms and AoT scenarios, the JIT may not be in use. Some assertions will consider zero as a valid in such cases.
        private static bool s_jitHasRun = JitInfo.GetCompiledMethodCount() > 0;

        private static readonly Func<bool> s_forceGc = () =>
        {
            for (var gen = 0; gen <= GC.MaxGeneration; gen++)
            {
                GC.Collect(gen, GCCollectionMode.Forced);
            }

            return GC.GetGCMemoryInfo().Index > 0;
        };

        private static readonly Func<long, (bool, string?)> s_longGreaterThanZero = v => v > 0
            ? (true, null)
            : (false, $"{GreaterThanZeroMessage} Actual value was: {v}.");

        private static readonly Func<long, (bool, string?)> s_longGreaterThanOrEqualToZero = v => v >= 0
            ? (true, null)
            : (false, $"{GreaterThanOrEqualToZeroMessage} Actual value was: {v}.");

        private static readonly Func<double, (bool, string?)> s_doubleGreaterThanZero = v => v > 0
            ? (true, null)
            : (false, $"{GreaterThanZeroMessage} Actual value was: {v}.");

        private static readonly Func<double, (bool, string?)> s_doubleGreaterThanOrEqualToZero = v => v >= 0
            ? (true, null)
            : (false, $"{GreaterThanOrEqualToZeroMessage} Actual value was: {v}.");

        private readonly ITestOutputHelper _output = output;

        public static bool IsCoreClrRemoteExecutorSupported => PlatformDetection.IsCoreCLR && RemoteExecutor.IsSupported;
        public static bool IsCoreClrOrNativeAot => PlatformDetection.IsCoreCLR || PlatformDetection.IsNativeAot;

        [ConditionalFact(nameof(IsCoreClrOrNativeAot))]
        public async Task GcPauseDurationInProcess()
        {
            using InstrumentRecorder<double> recorder = new("dotnet.gc.pause.duration");
            Assert.Equal("s", Assert.IsType<Histogram<double>>(recorder.Instrument).Unit);

            // Other in-process EventListeners can reconfigure the shared session and discard an event.
            long deadline = Environment.TickCount64 + 30_000;
            while (!recorder.Measurements.Any(measurement => PauseTag(measurement, "gc.pause.type") == "blocking"))
            {
                Assert.True(Environment.TickCount64 < deadline, "No blocking GC pause was delivered after repeated collections.");
                GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                await Task.Delay(10);
            }
            foreach (Measurement<double> measurement in recorder.Measurements)
            {
                Assert.True(measurement.Value >= 0);
                Assert.Equal(2, measurement.Tags.Length);
                Assert.True(PauseTag(measurement, "gc.heap.generation") is "gen0" or "gen1" or "gen2");
                Assert.True(PauseTag(measurement, "gc.pause.type") is "blocking" or "background");
            }
        }

        [ConditionalTheory(nameof(IsCoreClrRemoteExecutorSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void GcPauseDurationMetricsEventSource(bool enableInCallback)
        {
            RemoteExecutor.Invoke(static enableInCallbackValue =>
            {
                using MeterListener initializeRuntimeMetrics = new();
                PauseMetricsEventListener.EnableInCallback = bool.Parse(enableInCallbackValue);
                using PauseMetricsEventListener listener = new();
                if (!PauseMetricsEventListener.EnableInCallback)
                {
                    listener.EnableMetrics();
                }

                Assert.Empty(listener.Errors);
                long deadline = Environment.TickCount64 + 30_000;
                while (listener.HistogramEvents == 0)
                {
                    Assert.Empty(listener.Errors);
                    Assert.True(Environment.TickCount64 < deadline, "No GC pause histogram was reported through the metrics EventSource.");
                    GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                    Thread.Sleep(10);
                }
                Assert.Empty(listener.Errors);
            }, enableInCallback.ToString(), CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        private sealed class PauseMetricsEventListener : EventListener
        {
            internal static bool EnableInCallback;
            private EventSource? _source;
            private ConcurrentQueue<string>? _errors;
            private int _histogramEvents;

            internal ConcurrentQueue<string> Errors => LazyInitializer.EnsureInitialized(ref _errors);
            internal int HistogramEvents => Volatile.Read(ref _histogramEvents);

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name == "System.Diagnostics.Metrics")
                {
                    _source = eventSource;
                    if (EnableInCallback)
                    {
                        EnableMetrics();
                    }
                }
            }

            internal void EnableMetrics() => EnableEvents(_source!, EventLevel.Verbose, (EventKeywords)7,
                new Dictionary<string, string?>
                {
                    ["Metrics"] = @"System.Runtime\dotnet.gc.pause.duration",
                    ["RefreshInterval"] = "0.1"
                });

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (eventData.EventId == 0 || eventData.EventName == "Error")
                {
                    Errors.Enqueue(string.Join("; ", eventData.Payload!));
                }
                else if (eventData.EventName == "HistogramValuePublished")
                {
                    Interlocked.Increment(ref _histogramEvents);
                }
            }
        }

        [ConditionalTheory(nameof(IsCoreClrRemoteExecutorSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void GcPauseDuration(bool serverGc)
        {
            RemoteInvokeOptions options = CreateGCPauseOptions(serverGc);
            RemoteExecutor.Invoke(static serverMode =>
            {
                Assert.Equal(bool.Parse(serverMode) && Environment.ProcessorCount > 1, GCSettings.IsServerGC);
                using InstrumentRecorder<double> recorder = new("dotnet.gc.pause.duration");
                Assert.Equal("s", Assert.IsType<Histogram<double>>(recorder.Instrument).Unit);
                CollectUntil(() => recorder.Measurements.Length > 0);

                HashSet<string> expectedGenerations = new();
                for (int generation = 0; generation <= GC.MaxGeneration; generation++)
                {
                    GC.Collect(generation, GCCollectionMode.Forced, blocking: true);
                    expectedGenerations.Add(s_genNames[GC.GetGCMemoryInfo().Generation]);
                }

                WaitFor(() => expectedGenerations.All(generation =>
                    recorder.Measurements.Any(measurement => PauseTag(measurement, "gc.heap.generation") == generation)));
                Measurement<double>[] measurements = recorder.Measurements;
                double totalPauseSeconds = GC.GetTotalPauseDuration().TotalSeconds;
                foreach (Measurement<double> measurement in measurements)
                {
                    Assert.Equal(2, measurement.Tags.Length);
                    Assert.True(PauseTag(measurement, "gc.heap.generation") is "gen0" or "gen1" or "gen2");
                    Assert.Equal("blocking", PauseTag(measurement, "gc.pause.type"));
                    Assert.False(double.IsNaN(measurement.Value));
                    Assert.InRange(measurement.Value, 0, totalPauseSeconds);
                }
            }, serverGc.ToString(), options).Dispose();
        }

        [ConditionalTheory(nameof(IsCoreClrRemoteExecutorSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void GcPauseDurationMatchesNativeAccounting(bool serverGc)
        {
            RemoteInvokeOptions options = CreateGCPauseOptions(serverGc);
            RemoteExecutor.Invoke(static () =>
            {
                using PauseRecorder recorder = new();
                using InstrumentRecorder<double> histogram = new("dotnet.gc.pause.duration");
                CollectUntil(() => histogram.Measurements.Length > 0);
                for (int generation = 0; generation <= GC.MaxGeneration; generation++)
                {
                    GC.Collect(generation, GCCollectionMode.Forced, blocking: true);
                    GCMemoryInfo info = GC.GetGCMemoryInfo();
                    PauseSample sample = Assert.Single(recorder.WaitForCollection(info.Index));
                    Assert.Equal(info.Generation, sample.Depth);
                    Assert.Equal(0, sample.Type);
                    Assert.Equal(info.PauseDurations[0].Ticks, checked((long)sample.Duration * 10));
                    WaitFor(() => histogram.Measurements.Any(measurement =>
                        measurement.Value == sample.Duration / 1_000_000d &&
                        PauseTag(measurement, "gc.heap.generation") == s_genNames[sample.Depth] &&
                        PauseTag(measurement, "gc.pause.type") == "blocking"));
                }
            }, options).Dispose();
        }

        private static RemoteInvokeOptions CreateGCPauseOptions(bool serverGc, bool concurrent = false)
        {
            RemoteInvokeOptions options = new();
            options.StartInfo.Environment["DOTNET_gcServer"] = serverGc ? "1" : "0";
            options.StartInfo.Environment["DOTNET_gcConcurrent"] = concurrent ? "1" : "0";
            return options;
        }

        [ConditionalTheory(nameof(IsCoreClrRemoteExecutorSupported))]
        [InlineData(0x1L, false)]
        [InlineData(0x04000000L, true)]
        [InlineData(-1L, true)]
        public void GcPauseDurationRuntimeProviderKeywords(long keywords, bool expectsPause)
        {
            RemoteExecutor.Invoke(static (keywordValue, expectedValue) =>
            {
                using PauseRecorder recorder = new(keywords: (EventKeywords)long.Parse(keywordValue));
                GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                long index = GC.GetGCMemoryInfo().Index;

                if (bool.Parse(expectedValue))
                {
                    Assert.Single(recorder.WaitForCollection(index));
                }
                else
                {
                    // A later collection's end ensures the first collection's events have been dispatched.
                    GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                    recorder.WaitForCollectionEnd(GC.GetGCMemoryInfo().Index);
                    Assert.Empty(recorder.GetCollection(index));
                }
            }, keywords.ToString(), expectsPause.ToString(), CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [ConditionalTheory(nameof(IsCoreClrRemoteExecutorSupported))]
        [InlineData(0u, "blocking")]
        [InlineData(1u, "background")]
        [InlineData(2u, "blocking")]
        public void GcPauseDurationMapsCollectionType(uint type, string expectedPauseType)
        {
            RemoteExecutor.Invoke(static (typeValue, expectedType) =>
            {
                using InstrumentRecorder<double> histogram = new("dotnet.gc.pause.duration");
                CollectUntil(() => histogram.Measurements.Length > 0);
                const ulong DurationMicroseconds = 123_456_789;
                uint collectionType = uint.Parse(typeValue);
                int armed = 1;
                using PauseRecorder interceptor = new(callback: eventData =>
                {
                    if (Interlocked.Exchange(ref armed, 0) != 0)
                    {
                        // Retain native event metadata while covering all type mappings without racing a BGC.
                        ReadOnlyCollection<object?> payload = new(new object?[]
                        {
                            1u, DurationMicroseconds, collectionType == 1 ? 2u : 0u, collectionType, (ushort)0
                        });
                        typeof(EventWrittenEventArgs).GetProperty(nameof(EventWrittenEventArgs.Payload))!.SetValue(eventData, payload);
                    }
                });
                GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                CollectUntil(() => histogram.Measurements.Any(measurement => measurement.Value == DurationMicroseconds / 1_000_000d));
                Measurement<double> measurement = Assert.Single(histogram.Measurements, measurement => measurement.Value == DurationMicroseconds / 1_000_000d);
                Assert.Equal(expectedType, PauseTag(measurement, "gc.pause.type"));
                Assert.Equal(collectionType == 1 ? "gen2" : "gen0", PauseTag(measurement, "gc.heap.generation"));
            }, type.ToString(), expectedPauseType, CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [ConditionalTheory(nameof(IsCoreClrRemoteExecutorSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void GcPauseDurationEventSourceSupport(bool enabled)
        {
            RemoteInvokeOptions options = CreateGCPauseOptions(serverGc: false);
            options.RuntimeConfigurationOptions.Add("System.Diagnostics.Tracing.EventSource.IsSupported", enabled);
            RemoteExecutor.Invoke(static enabledValue =>
            {
                using MeterListener listener = new();
                List<string> instruments = new();
                int observed = 0;
                listener.SetMeasurementEventCallback<double>((_, _, _, _) => Interlocked.Increment(ref observed));
                listener.InstrumentPublished = (instrument, subscriber) =>
                {
                    if (instrument.Meter.Name == "System.Runtime")
                    {
                        instruments.Add(instrument.Name);
                        if (instrument.Name == "dotnet.gc.pause.duration")
                        {
                            subscriber.EnableMeasurementEvents(instrument);
                        }
                    }
                };
                listener.Start();
                Assert.Contains("dotnet.gc.pause.duration", instruments);
                Assert.Contains("dotnet.gc.pause.time", instruments);
                Assert.DoesNotContain("dotnet.gc.pause.dropped", instruments);
                GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                if (bool.Parse(enabledValue))
                {
                    CollectUntil(() => Volatile.Read(ref observed) > 0);
                }
                else
                {
                    Assert.Equal(0, Volatile.Read(ref observed));
                }
            }, enabled.ToString(), options).Dispose();
        }

        [ConditionalFact(nameof(IsCoreClrRemoteExecutorSupported))]
        public void GcPauseDurationListenerLifecycle()
        {
            RemoteExecutor.Invoke(static () =>
            {
                using PauseRecorder monitor = new(enable: false);
                using MeterListener first = new();
                using MeterListener second = new();
                Histogram<double>? histogram = null;
                ObservableCounter<double>? totalPause = null;
                bool droppedPublished = false;
                int firstCalls = 0;
                int secondCalls = 0;
                first.InstrumentPublished = (instrument, _) =>
                {
                    if (instrument.Meter.Name == "System.Runtime")
                    {
                        if (instrument.Name == "dotnet.gc.pause.duration")
                        {
                            histogram = Assert.IsType<Histogram<double>>(instrument);
                        }
                        else if (instrument.Name == "dotnet.gc.pause.dropped")
                        {
                            droppedPublished = true;
                        }
                        else if (instrument.Name == "dotnet.gc.pause.time")
                        {
                            totalPause = Assert.IsType<ObservableCounter<double>>(instrument);
                        }
                    }
                };
                first.SetMeasurementEventCallback<double>((_, _, _, _) => Interlocked.Increment(ref firstCalls));
                second.SetMeasurementEventCallback<double>((_, _, _, _) => Interlocked.Increment(ref secondCalls));
                first.Start();
                Assert.NotNull(histogram);
                Assert.NotNull(totalPause);
                Assert.False(droppedPublished);
                WaitFor(() => !monitor.IsActive);

                first.EnableMeasurementEvents(totalPause);
                first.RecordObservableInstruments();
                Assert.False(monitor.IsActive);
                first.DisableMeasurementEvents(totalPause);
                first.EnableMeasurementEvents(histogram);
                WaitFor(() => monitor.IsActive);
                first.EnableMeasurementEvents(histogram, new object());
                second.EnableMeasurementEvents(histogram);
                int previousFirstCalls = Volatile.Read(ref firstCalls);
                CollectUntil(() => Volatile.Read(ref firstCalls) > previousFirstCalls && Volatile.Read(ref secondCalls) > 0);

                first.DisableMeasurementEvents(histogram);
                Assert.True(monitor.IsActive);
                int previousSecondCalls = Volatile.Read(ref secondCalls);
                CollectUntil(() => Volatile.Read(ref secondCalls) > previousSecondCalls);
                int disableCommands = monitor.DisableCommands;
                second.Dispose();
                Assert.False(histogram.Enabled);
                monitor.WaitForDisable(disableCommands);

                int stoppedFirstCalls = Volatile.Read(ref firstCalls);
                int stoppedSecondCalls = Volatile.Read(ref secondCalls);
                using (PauseRecorder probe = new())
                {
                    GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                    probe.WaitForCollection(GC.GetGCMemoryInfo().Index);
                }
                Assert.Equal(stoppedFirstCalls, Volatile.Read(ref firstCalls));
                Assert.Equal(stoppedSecondCalls, Volatile.Read(ref secondCalls));

                first.EnableMeasurementEvents(histogram);
                WaitFor(() => monitor.IsActive);
                CollectUntil(() => Volatile.Read(ref firstCalls) > stoppedFirstCalls);
                disableCommands = monitor.DisableCommands;
                histogram.Meter.Dispose();
                Assert.False(histogram.Enabled);
                monitor.WaitForDisable(disableCommands);
            }, CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [ConditionalFact(nameof(IsCoreClrRemoteExecutorSupported))]
        public void GcPauseDurationSlowListenerDoesNotBlockGC()
        {
            RemoteExecutor.Invoke(static () =>
            {
                using ManualResetEventSlim entered = new();
                using ManualResetEventSlim release = new();
                using ManualResetEventSlim returned = new();
                using PauseRecorder monitor = new(enable: false);
                int delivered = 0;
                bool released = false;
                using MeterListener listener = CreatePauseListener((_, _, _, _) =>
                {
                    if (Interlocked.Increment(ref delivered) == 1)
                    {
                        entered.Set();
                        try
                        {
                            released = release.Wait(TimeSpan.FromSeconds(30));
                        }
                        finally
                        {
                            returned.Set();
                        }
                    }
                });
                WaitFor(() => monitor.IsActive);
                CollectUntil(() => entered.IsSet);
                try
                {
                    int previousCollections = GC.CollectionCount(0);
                    Task collections = Task.Run(static () =>
                    {
                        for (int i = 0; i < 256; i++)
                        {
                            GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                        }
                    });
                    Assert.True(collections.Wait(TimeSpan.FromSeconds(30)), "GC was blocked by a slow measurement callback.");
                    Assert.True(GC.CollectionCount(0) >= previousCollections + 256);
                }
                finally
                {
                    release.Set();
                    Assert.True(returned.Wait(TimeSpan.FromSeconds(30)));
                }

                Assert.True(released);
                WaitFor(() => Volatile.Read(ref delivered) > 1);
            }, CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [ConditionalTheory(nameof(IsCoreClrRemoteExecutorSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void GcPauseDurationBackground(bool serverGc)
        {
            RemoteInvokeOptions options = CreateGCPauseOptions(serverGc, concurrent: true);
            if (serverGc)
            {
                // Keep heap-count changes from converting the nonblocking request into a blocking collection.
                options.StartInfo.Environment["DOTNET_GCHeapCount"] = "4";
                options.StartInfo.Environment["DOTNET_GCDynamicAdaptationMode"] = "0";
            }
            RemoteExecutor.Invoke(static () =>
            {
                // A single retained LOH object exceeds the per-heap BGC threshold on both x86 and x64.
                byte[] root = new byte[8 * 1024 * 1024];
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                long previousIndex = GC.GetGCMemoryInfo(GCKind.FullBlocking).Index;
                using PauseRecorder recorder = new(keywords: GCPauseKeyword | (EventKeywords)0x1);
                using InstrumentRecorder<double> histogram = new("dotnet.gc.pause.duration");
                CollectUntil(() => histogram.Measurements.Length > 0);

                long deadline = Environment.TickCount64 + 30_000;
                long backgroundIndex;
                do
                {
                    GC.Collect(2, GCCollectionMode.Forced, blocking: false);

                    // Wait for any BGC to finish, then drain its events through the following blocking GC's end.
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
                    recorder.WaitForCollectionEnd(GC.GetGCMemoryInfo(GCKind.FullBlocking).Index);
                    backgroundIndex = recorder.GetLastBackgroundCollectionIndex();
                    if (backgroundIndex <= previousIndex)
                    {
                        Assert.True(Environment.TickCount64 < deadline, "No background GC pause events were delivered after nonblocking collection requests.");
                        Thread.Sleep(1);
                    }
                }
                while (backgroundIndex <= previousIndex);

                GCMemoryInfo info = GC.GetGCMemoryInfo(GCKind.Background);
                Assert.Equal(backgroundIndex, info.Index);
                Assert.True(info.Concurrent);
                Assert.Equal(2, info.Generation);
                PauseSample[] background = recorder.WaitForCollection(info.Index, minimumCount: 2);
                Assert.All(background, sample =>
                {
                    Assert.Equal(2, sample.Depth);
                    Assert.Equal(1, sample.Type);
                });
                Assert.Contains(background, sample => checked((long)sample.Duration * 10) == info.PauseDurations[1].Ticks);
                ulong finalPauseMicroseconds = (ulong)(info.PauseDurations[1].Ticks / 10);
                WaitFor(() => histogram.Measurements.Any(measurement =>
                    measurement.Value == finalPauseMicroseconds / 1_000_000d &&
                    PauseTag(measurement, "gc.heap.generation") == "gen2" &&
                    PauseTag(measurement, "gc.pause.type") == "background"));
                GC.KeepAlive(root);
            }, options).Dispose();
        }

        private static void WaitFor(Func<bool> condition, [CallerArgumentExpression(nameof(condition))] string? expression = null) =>
            Assert.True(SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(30)), $"Timed out waiting for GC pause condition: {expression}");

        private static void CollectUntil(Func<bool> condition, int generation = 0, [CallerArgumentExpression(nameof(condition))] string? expression = null)
        {
            long deadline = Environment.TickCount64 + 30_000;
            while (!condition())
            {
                Assert.True(Environment.TickCount64 < deadline, $"Timed out collecting for GC pause condition: {expression}");
                GC.Collect(generation, GCCollectionMode.Forced, blocking: true);
                Thread.Sleep(10);
            }
        }

        private static string PauseTag(Measurement<double> measurement, string key) =>
            Assert.IsType<string>(measurement.Tags.ToArray().Single(tag => tag.Key == key).Value);

        private static MeterListener CreatePauseListener(MeasurementCallback<double> callback)
        {
            MeterListener listener = new();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == "System.Runtime" && instrument.Name == "dotnet.gc.pause.duration")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback(callback);
            listener.Start();
            return listener;
        }

        private sealed class PauseRecorder : EventListener
        {
            private readonly ConcurrentQueue<EventWrittenEventArgs> _events = new();
            private readonly ConcurrentQueue<uint> _completedCollections = new();
            private readonly ConcurrentQueue<string> _errors = new();
            private readonly Action<EventWrittenEventArgs>? _callback;
            private EventSource? _source;
            private int _disableCommands;

            internal bool IsActive => _source!.IsEnabled(EventLevel.Informational, GCPauseKeyword);
            internal int DisableCommands => Volatile.Read(ref _disableCommands);

            internal PauseRecorder(bool enable = true, Action<EventWrittenEventArgs>? callback = null, EventKeywords keywords = GCPauseKeyword)
            {
                Assert.NotNull(_source);
                _callback = callback;
                _source.EventCommandExecuted += OnCommandExecuted;
                if (enable)
                {
                    EnableEvents(_source, EventLevel.Informational, keywords);
                }
            }

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name == RuntimeProviderName)
                {
                    _source = eventSource;
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (eventData.EventId == GCPauseEventId)
                {
                    _events.Enqueue(eventData);
                    _callback?.Invoke(eventData);
                }
                else if (eventData.EventId == 2 && eventData.Payload is [uint index, uint, ushort])
                {
                    _completedCollections.Enqueue(index);
                }
                else if (eventData.EventId == 0)
                {
                    _errors.Enqueue(eventData.Payload?[0]?.ToString() ?? eventData.Message ?? "EventSource error.");
                }
            }

            private void OnCommandExecuted(object? sender, EventCommandEventArgs args)
            {
                if (args.Command == EventCommand.Disable)
                {
                    Interlocked.Increment(ref _disableCommands);
                }
            }

            internal void WaitForDisable(int previousCount)
            {
                Assert.True(SpinWait.SpinUntil(() => DisableCommands > previousCount, TimeSpan.FromSeconds(30)),
                    $"No runtime-provider disable command was observed. EventSource errors: {string.Join("; ", _errors)}");
                Assert.Empty(_errors);
            }

            public override void Dispose()
            {
                _source!.EventCommandExecuted -= OnCommandExecuted;
                base.Dispose();
            }

            internal PauseSample[] WaitForCollection(long index, int minimumCount = 1)
            {
                WaitFor(() => _events.Select(ReadSample).Count(sample => sample.Count == unchecked((uint)index)) >= minimumCount);
                return GetCollection(index);
            }

            internal PauseSample[] GetCollection(long index) =>
                _events.Select(ReadSample).Where(sample => sample.Count == unchecked((uint)index)).ToArray();

            internal void WaitForCollectionEnd(long index) =>
                WaitFor(() => _completedCollections.Contains((uint)index));

            internal long GetLastBackgroundCollectionIndex() =>
                _events.Select(ReadSample).Where(sample => sample.Type == 1)
                    .Select(sample => sample.Count).DefaultIfEmpty().Max();

            private static PauseSample ReadSample(EventWrittenEventArgs eventData)
            {
                Assert.Equal(RuntimeProviderName, eventData.EventSource.Name);
                Assert.Equal("GCPause", eventData.EventName);
                Assert.Equal(0, eventData.Version);
                Assert.Equal(GCPauseKeyword, eventData.Keywords & GCPauseKeyword);
                Assert.Equal(new[] { "Count", "DurationMicroseconds", "Depth", "Type", "ClrInstanceID" }, eventData.PayloadNames);
                Assert.NotNull(eventData.Payload);
                Assert.Equal(5, eventData.Payload.Count);
                uint count = Assert.IsType<uint>(eventData.Payload[0]);
                ulong duration = Assert.IsType<ulong>(eventData.Payload[1]);
                uint generation = Assert.IsType<uint>(eventData.Payload[2]);
                uint type = Assert.IsType<uint>(eventData.Payload[3]);
                Assert.IsType<ushort>(eventData.Payload[4]);
                Assert.InRange(generation, 0u, 2u);
                Assert.InRange(type, 0u, 2u);
                Assert.True(type != 1 || generation == 2);
                return (duration, count, (int)generation, (int)type);
            }
        }

        [ConditionalTheory(nameof(IsCoreClrRemoteExecutorSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void GcPauseDurationSubscriptionsDoNotReplayBacklog(bool reenable)
        {
            RemoteExecutor.Invoke(static reenableValue =>
            {
                using ManualResetEventSlim entered = new();
                using ManualResetEventSlim release = new();
                using ManualResetEventSlim returned = new();
                using MeterListener listener = new();
                Histogram<double>? histogram = null;
                ConcurrentQueue<string> generations = new();
                listener.InstrumentPublished = (instrument, _) =>
                {
                    if (instrument.Meter.Name == "System.Runtime" && instrument.Name == "dotnet.gc.pause.duration")
                    {
                        histogram = Assert.IsType<Histogram<double>>(instrument);
                    }
                };
                listener.SetMeasurementEventCallback<double>((_, duration, tags, _) =>
                    generations.Enqueue(PauseTag(new Measurement<double>(duration, tags), "gc.heap.generation")));
                listener.Start();
                Assert.NotNull(histogram);
                using PauseRecorder monitor = new(enable: false);
                if (bool.Parse(reenableValue))
                {
                    listener.EnableMeasurementEvents(histogram);
                    WaitFor(() => monitor.IsActive);
                }

                int calls = 0;
                bool released = false;
                using PauseRecorder blocker = new(callback: _ =>
                {
                    if (Interlocked.Increment(ref calls) == 1)
                    {
                        entered.Set();
                        try
                        {
                            released = release.Wait(TimeSpan.FromSeconds(30));
                        }
                        finally
                        {
                            returned.Set();
                        }
                    }
                });
                GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                Assert.True(entered.Wait(TimeSpan.FromSeconds(30)));
                try
                {
                    GC.Collect(1, GCCollectionMode.Forced, blocking: true);
                    listener.DisableMeasurementEvents(histogram);
                    generations.Clear();
                    listener.EnableMeasurementEvents(histogram);
                }
                finally
                {
                    release.Set();
                    Assert.True(returned.Wait(TimeSpan.FromSeconds(30)));
                }
                Assert.True(released);
                CollectUntil(() => generations.Contains("gen2"), generation: 2);
                Assert.DoesNotContain("gen1", generations);
            }, reenable.ToString(), CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [ConditionalFact(nameof(IsCoreClrRemoteExecutorSupported))]
        public void GcPauseDurationCallbackCanCollectAndDispose()
        {
            RemoteExecutor.Invoke(static () =>
            {
                using PauseRecorder monitor = new(enable: false);
                using ManualResetEventSlim completed = new();
                using MeterListener listener = new();
                Histogram<double>? histogram = null;
                int entered = 0;
                int disableCommands = monitor.DisableCommands;
                listener.InstrumentPublished = (instrument, meterListener) =>
                {
                    if (instrument.Meter.Name == "System.Runtime" && instrument.Name == "dotnet.gc.pause.duration")
                    {
                        histogram = Assert.IsType<Histogram<double>>(instrument);
                        meterListener.EnableMeasurementEvents(instrument);
                    }
                };
                listener.SetMeasurementEventCallback<double>((instrument, duration, tags, state) =>
                {
                    if (Interlocked.CompareExchange(ref entered, 1, 0) == 0)
                    {
                        GC.Collect(0, GCCollectionMode.Forced, blocking: true);
                        listener.Dispose();
                        completed.Set();
                    }
                });
                listener.Start();
                Assert.NotNull(histogram);
                WaitFor(() => monitor.IsActive);
                CollectUntil(() => completed.IsSet);
                Assert.False(histogram.Enabled);
                monitor.WaitForDisable(disableCommands);
            }, CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [ConditionalFact(nameof(IsCoreClrRemoteExecutorSupported))]
        public void GcPauseDurationSubscriptionBeforeDispatchStarts()
        {
            RemoteExecutor.Invoke(static () =>
            {
                using ManualResetEventSlim entered = new();
                using ManualResetEventSlim release = new();
                using ManualResetEventSlim returned = new();
                using ManualResetEventSlim replacementStarted = new();
                bool released = false;
                using DispatchStartListener blocker = new(() =>
                {
                    entered.Set();
                    try
                    {
                        released = release.Wait(TimeSpan.FromSeconds(30));
                    }
                    finally
                    {
                        returned.Set();
                    }
                }, () => replacementStarted.Set());
                using MeterListener listener = new();
                Histogram<double>? histogram = null;
                int observed = 0;
                listener.InstrumentPublished = (instrument, _) =>
                {
                    if (instrument.Meter.Name == "System.Runtime" && instrument.Name == "dotnet.gc.pause.duration")
                    {
                        histogram = Assert.IsType<Histogram<double>>(instrument);
                    }
                };
                listener.SetMeasurementEventCallback<double>((_, _, _, _) => Interlocked.Increment(ref observed));
                listener.Start();
                Assert.NotNull(histogram);

                using PauseRecorder monitor = new(enable: false);
                blocker.Arm();
                using PauseRecorder previousSession = new(keywords: (EventKeywords)0x1);
                try
                {
                    Assert.True(entered.Wait(TimeSpan.FromSeconds(30)));
                    listener.EnableMeasurementEvents(histogram);
                    Assert.True(replacementStarted.Wait(TimeSpan.FromSeconds(30)));
                }
                finally
                {
                    release.Set();
                }
                Assert.True(returned.Wait(TimeSpan.FromSeconds(30)));
                Assert.True(released);

                CollectUntil(() => Volatile.Read(ref observed) > 0);
            }, CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        private sealed class DispatchStartListener(Action callback, Action replacementStarted) : EventListener
        {
            private int _armed;
            private int _entered;

            internal void Arm() => Volatile.Write(ref _armed, 1);

            protected override void OnEventSourceCreated(EventSource source)
            {
                if (source.Name == "System.Threading.Tasks.TplEventSource")
                {
                    EnableEvents(source, EventLevel.Informational, (EventKeywords)2);
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                // TaskStarted is synchronous and precedes invocation of the queued delegate.
                if (eventData.EventId == 8 && !Thread.CurrentThread.IsThreadPoolThread && Volatile.Read(ref _armed) != 0)
                {
                    if (Interlocked.Increment(ref _entered) == 1)
                    {
                        callback();
                    }
                    else
                    {
                        replacementStarted();
                    }
                }
            }
        }

        [ConditionalFact(nameof(IsCoreClrRemoteExecutorSupported))]
        public void GcPauseDurationRapidResubscription()
        {
            RemoteExecutor.Invoke(static () =>
            {
                using PauseRecorder monitor = new(enable: false);
                using MeterListener listener = new();
                Histogram<double>? histogram = null;
                int observed = 0;
                listener.InstrumentPublished = (instrument, _) =>
                {
                    if (instrument.Meter.Name == "System.Runtime" && instrument.Name == "dotnet.gc.pause.duration")
                    {
                        histogram = Assert.IsType<Histogram<double>>(instrument);
                    }
                };
                listener.SetMeasurementEventCallback<double>((_, _, _, _) => Interlocked.Increment(ref observed));
                listener.Start();
                Assert.NotNull(histogram);
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        listener.EnableMeasurementEvents(histogram);
                        listener.DisableMeasurementEvents(histogram);
                    }
                    listener.EnableMeasurementEvents(histogram);
                    int previousObserved = Volatile.Read(ref observed);
                    CollectUntil(() => Volatile.Read(ref observed) > previousObserved);
                    int disableCommands = monitor.DisableCommands;
                    listener.DisableMeasurementEvents(histogram);
                    Assert.False(histogram.Enabled);
                    monitor.WaitForDisable(disableCommands);
                }
            }, CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [ConditionalFact(nameof(IsCoreClrRemoteExecutorSupported))]
        public void GcPauseDurationCallbackExceptionDoesNotStopDispatch()
        {
            RemoteExecutor.Invoke(static () =>
            {
                int observed = 0;
                using MeterListener listener = CreatePauseListener((_, _, _, _) =>
                {
                    Interlocked.Increment(ref observed);
                    throw new InstrumentRecorderException();
                });
                for (int i = 0; i < 2; i++)
                {
                    int previousObserved = Volatile.Read(ref observed);
                    CollectUntil(() => Volatile.Read(ref observed) > previousObserved);
                }
            }, CreateGCPauseOptions(serverGc: false)).Dispose();
        }

        [Fact]
        public void GcCollectionsCount()
        {
            using InstrumentRecorder<long> instrumentRecorder = new("dotnet.gc.collections");

            for (var gen = 0; gen <= GC.MaxGeneration; gen++)
            {
                GC.Collect(gen, GCCollectionMode.Forced);
            }

            instrumentRecorder.RecordObservableInstruments();

            bool[] foundGenerations = new bool[GC.MaxGeneration + 1];
            for (int i = 0; i < GC.MaxGeneration + 1; i++)
            {
                foundGenerations[i] = false;
            }

            var measurements = instrumentRecorder.GetMeasurements();

            var gensExpected = GC.MaxGeneration + 1;
            Assert.True(measurements.Count >= gensExpected, $"Expected to find at least one measurement for each generation ({gensExpected}) " +
                $"but received {measurements.Count} measurements.");

            foreach (Measurement<long> measurement in measurements)
            {
                var tags = measurement.Tags.ToArray();
                var tag = tags.SingleOrDefault(k => k.Key == "gc.heap.generation");

                if (tag.Key is not null)
                {
                    Assert.True(tag.Value is string, "Expected generation tag to be a string.");

                    string tagValue = (string)tag.Value;

                    switch (tagValue)
                    {
                        case "gen0":
                            foundGenerations[0] = true;
                            break;
                        case "gen1":
                            foundGenerations[1] = true;
                            break;
                        case "gen2":
                            foundGenerations[2] = true;
                            break;
                        default:
                            Assert.Fail($"Unexpected generation tag value '{tagValue}'.");
                            break;
                    }
                }
            }

            for (int i = 0; i < foundGenerations.Length; i++)
            {
                var generation = i switch
                {
                    0 => "gen0",
                    1 => "gen1",
                    2 => "gen2",
                    _ => throw new InvalidOperationException("Unexpected generation.")
                };

                Assert.True(foundGenerations[i], $"Expected to find a measurement for '{generation}'.");
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        public void CpuTime()
        {
           using InstrumentRecorder<double> instrumentRecorder = new("dotnet.process.cpu.time");

           instrumentRecorder.RecordObservableInstruments();

           bool[] foundCpuModes = [false, false];

           foreach (Measurement<double> measurement in instrumentRecorder.GetMeasurements())
           {
               var tags = measurement.Tags.ToArray();
               var tag = tags.SingleOrDefault(k => k.Key == "cpu.mode");

               if (tag.Key is not null)
               {
                   Assert.True(tag.Value is string, "Expected CPU mode tag to be a string.");

                   string tagValue = (string)tag.Value;

                   switch (tagValue)
                   {
                       case "user":
                           foundCpuModes[0] = true;
                           break;
                       case "system":
                           foundCpuModes[1] = true;
                           break;
                       default:
                           Assert.Fail($"Unexpected CPU mode tag value '{tagValue}'.");
                           break;
                   }
               }
           }

           for (int i = 0; i < foundCpuModes.Length; i++)
           {
               var mode = i == 0 ? "user" : "system";
               Assert.True(foundCpuModes[i], $"Expected to find a measurement for '{mode}' CPU mode.");
           }
        }

        [Fact]
        public void ExceptionsCount()
        {
            // We inject an exception into the MeterListener callback here, so we can test that we don't recursively record exceptions.
            using InstrumentRecorder<long> instrumentRecorder = new("dotnet.exceptions", injectException: true);

            try
            {
                throw new RuntimeMeterException();
            }
            catch
            {
                // Ignore the exception.
            }

            var measurements = instrumentRecorder.GetMeasurements();

            AssertExceptions(measurements, 1);

            try
            {
                throw new RuntimeMeterException();
            }
            catch
            {
                // Ignore the exception.
            }

            measurements = instrumentRecorder.GetMeasurements();

            AssertExceptions(measurements, 2);

            static void AssertExceptions(IReadOnlyList<Measurement<long>> measurements, int expectedCount)
            {
                int foundExpectedExceptions = 0;
                int foundUnexpectedExceptions = 0;

                foreach (Measurement<long> measurement in measurements)
                {
                    var tags = measurement.Tags.ToArray();
                    var tag = tags.Single(k => k.Key == "error.type");

                    Assert.NotNull(tag.Key);
                    Assert.NotNull(tag.Value);

                    if (tag.Value is not string tagValue)
                    {
                        Assert.Fail("Expected error type tag to be a string.");
                        return;
                    }

                    if (tagValue == nameof(RuntimeMeterException))
                    {
                        foundExpectedExceptions++;
                    }
                    else if (tagValue == nameof(InstrumentRecorderException))
                    {
                        foundUnexpectedExceptions++;
                    }
                }

                Assert.Equal(expectedCount, foundExpectedExceptions);
                Assert.Equal(0, foundUnexpectedExceptions);
            }
        }

        public static IEnumerable<object[]> Measurements => new List<object[]>
        {
            new object[] { "dotnet.process.memory.working_set", s_longGreaterThanZero, null },
            new object[] { "dotnet.assembly.count", s_longGreaterThanZero, null },
            new object[] { "dotnet.process.cpu.count", s_longGreaterThanZero, null },
            new object[] { "dotnet.gc.heap.total_allocated", s_longGreaterThanZero, null },
            new object[] { "dotnet.gc.last_collection.memory.committed_size", s_longGreaterThanZero, s_forceGc },
            new object[] { "dotnet.gc.pause.time", s_doubleGreaterThanOrEqualToZero, s_forceGc }, // may be zero if no GC has occurred
            new object[] { "dotnet.jit.compiled_il.size", s_jitHasRun ? s_longGreaterThanZero : s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.jit.compiled_methods", s_jitHasRun ? s_longGreaterThanZero : s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.jit.compilation.time", s_jitHasRun ? s_doubleGreaterThanZero : s_doubleGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.monitor.lock_contentions", s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.thread_pool.thread.count", s_longGreaterThanZero, null },
            new object[] { "dotnet.thread_pool.work_item.count", s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.thread_pool.queue.length", s_longGreaterThanOrEqualToZero, null },
            new object[] { "dotnet.timer.count", s_longGreaterThanOrEqualToZero, null },
        };

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        [MemberData(nameof(Measurements))]
        public void ValidateMeasurements<T>(string metricName, Func<T, (bool, string?)>? valueAssertion, Func<bool>? beforeRecord)
            where T : struct
        {
            ValidateSingleMeasurement(metricName, valueAssertion, beforeRecord);
        }

        private static void ValidateSingleMeasurement<T>(string metricName, Func<T, (bool, string?)>? valueAssertion = null, Func<bool>? beforeRecord = null)
            where T : struct
        {
            using InstrumentRecorder<T> instrumentRecorder = new(metricName);

            var shouldContinue = beforeRecord?.Invoke() ?? true;

            if (!shouldContinue)
                return;

            instrumentRecorder.RecordObservableInstruments();
            var measurements = instrumentRecorder.GetMeasurements();
            Assert.Single(measurements);

            if (valueAssertion is not null)
            {
                var (isExpected, message) = valueAssertion(measurements[0].Value);
                Assert.True(isExpected, message);
            }
        }

        [Theory]
        [InlineData("dotnet.gc.last_collection.heap.size")]
        [InlineData("dotnet.gc.last_collection.heap.fragmentation.size")]
        public void HeapTags(string metricName) => EnsureAllHeapTags(metricName);

        private void EnsureAllHeapTags(string metricName)
        {
            using InstrumentRecorder<long> instrumentRecorder = new(metricName);

            for (var gen = 0; gen <= GC.MaxGeneration; gen++)
            {
                GC.Collect(gen, GCCollectionMode.Forced);
            }

            instrumentRecorder.RecordObservableInstruments();
            var measurements = instrumentRecorder.GetMeasurements();

            bool[] foundGenerations = new bool[s_genNames.Length];
            for (int i = 0; i < 5; i++)
            {
                foundGenerations[i] = false;
            }

            var gensExpected = GC.MaxGeneration + 1;
            Assert.True(measurements.Count >= gensExpected, $"Expected to find at least one measurement for each generation ({gensExpected}) " +
                $"but received {measurements.Count} measurements.");

            foreach (Measurement<long> measurement in measurements)
            {
                var tags = measurement.Tags.ToArray();
                var tag = tags.SingleOrDefault(k => k.Key == "gc.heap.generation");

                if (tag.Key is not null)
                {
                    Assert.True(tag.Value is string, "Expected generation tag to be a string.");

                    string tagValue = (string)tag.Value;

                    var index = Array.FindIndex(s_genNames, x => x == tagValue);

                    if (index == -1)
                        Assert.Fail($"Unexpected generation tag value '{tagValue}'.");

                    foundGenerations[index] = true;
                }
            }

            for (int i = 0; i < foundGenerations.Length; i++)
            {
                Assert.True(foundGenerations[i], $"Expected to find a measurement for '{s_genNames[i]}'.");
            }
        }

        [Fact]
        public void ThreadPoolQueueLengthIsUpDownCounter()
        {
            using MeterListener listener = new();
            Instrument? instrument = null;

            listener.InstrumentPublished = (inst, l) =>
            {
                if (inst.Meter.Name == "System.Runtime" && inst.Name == "dotnet.thread_pool.queue.length")
                {
                    instrument = inst;
                }
            };

            listener.Start();

            Assert.NotNull(instrument);
            Assert.IsType<ObservableUpDownCounter<long>>(instrument);
        }

        private sealed class RuntimeMeterException() : Exception { }

        private sealed class InstrumentRecorderException() : Exception { }

        private sealed class InstrumentRecorder<T> : IDisposable where T : struct
        {
            private readonly MeterListener _meterListener = new();
            private readonly ConcurrentQueue<Measurement<T>> _values = new();
            private readonly bool _injectException;

            public Instrument? Instrument { get; private set; }
            public Measurement<T>[] Measurements => _values.ToArray();

            public InstrumentRecorder(string instrumentName, bool injectException = false)
            {
                _injectException = injectException;
                _meterListener.InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == "System.Runtime" && instrument.Name == instrumentName)
                    {
                        Instrument = instrument;
                        listener.EnableMeasurementEvents(instrument);
                    }
                };
                _meterListener.SetMeasurementEventCallback<T>(OnMeasurementRecorded);
                _meterListener.Start();
            }

            private void OnMeasurementRecorded(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
            {
                _values.Enqueue(new Measurement<T>(measurement, tags));

                if (_injectException)
                {
                    try
                    {
                        throw new InstrumentRecorderException();
                    }
                    catch
                    {
                        // Ignore the exception.
                    }
                }
            }

            public IReadOnlyList<Measurement<T>> GetMeasurements()
            {
                // Wait enough time for all the measurements to be enqueued via the
                // OnMeasurementRecorded callback. This value seems to be sufficient.
                Thread.Sleep(100);
                return _values.ToArray();
            }

            public void RecordObservableInstruments() => _meterListener.RecordObservableInstruments();

            public void Dispose() => _meterListener.Dispose();
        }
    }
}
