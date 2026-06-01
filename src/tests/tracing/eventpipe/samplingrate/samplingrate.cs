// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;

namespace Tracing.Tests.SamplingRate
{
    public class SamplingRate
    {
        private const string ChildArg = "child";
        private const string SampleProviderName = "Microsoft-DotNETCore-SampleProfiler";

        // Configured sample interval (ms) passed to the child via DOTNET_EventPipeThreadSamplingRate.
        // Picked well above the per-platform default (1ms on CoreCLR, 5ms with PERFTRACING_DISABLE_THREADS)
        // so the observed rate is unambiguously different.
        private const int ConfiguredSampleIntervalMs = 50;

        // Length of the CPU-burn workload in the child.
        private const int WorkloadDurationMs = 3000;

        // Lower bound on samples for the busiest thread. With 50ms interval over ~3s we expect ~60.
        private const int MinExpectedSamples = 30;

        // Upper bound on samples for the busiest thread. With default 1ms over ~3s we'd expect ~3000;
        // 600 leaves >5x headroom above the 50ms expectation while still excluding default-rate behavior.
        private const int MaxAllowedSamples = 600;

        // Lower bound on median inter-sample interval. With 50ms configured we expect ~50ms; the default
        // 1ms would produce ~1ms. 20ms cleanly separates the two while tolerating scheduling jitter.
        private const double MinMedianIntervalMs = 20.0;

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == ChildArg)
            {
                return RunChild();
            }

            return RunParent();
        }

        private static int RunParent()
        {
            Process process;
            try
            {
                process = new Process();
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("Child process launch not supported on this platform; skipping.");
                return 100;
            }

            string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
            string hostPath = Process.GetCurrentProcess().MainModule.FileName;
            string assemblyPath = typeof(SamplingRate).Assembly.Location;

            process.StartInfo.FileName = hostPath;
            process.StartInfo.Arguments = TestLibrary.Utilities.IsNativeAot
                ? ChildArg
                : $"{assemblyPath} {ChildArg}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.StartInfo.Environment["DOTNET_EventPipeThreadSamplingRate"] = ConfiguredSampleIntervalMs.ToString();
            if (!string.IsNullOrEmpty(coreRoot))
            {
                process.StartInfo.Environment["CORE_ROOT"] = coreRoot;
            }

            // Drain child stdio asynchronously so the child cannot deadlock on a full pipe (IpcTraceTest
            // is chatty).
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    Console.WriteLine($"[child stdout] {e.Data}");
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    Console.Error.WriteLine($"[child stderr] {e.Data}");
                }
            };

            Console.WriteLine($"Starting child '{process.StartInfo.FileName}' '{process.StartInfo.Arguments}' " +
                              $"with DOTNET_EventPipeThreadSamplingRate={ConfiguredSampleIntervalMs}");
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
            int exitCode = process.ExitCode;
            Console.WriteLine($"Child exited with code {exitCode}");
            return exitCode;
        }

        private static int RunChild()
        {
            Logger.logger.Log($"Child started with DOTNET_EventPipeThreadSamplingRate=" +
                              $"{Environment.GetEnvironmentVariable("DOTNET_EventPipeThreadSamplingRate") ?? "<unset>"}");

            var providers = new List<EventPipeProvider>
            {
                new EventPipeProvider(SampleProviderName, EventLevel.Verbose),
            };

            var expectedEventCounts = new Dictionary<string, ExpectedEventCount>
            {
                { SampleProviderName, -1 },
                { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
            };

            // Per-thread (ThreadID -> TimeStampRelativeMSec list) collected from the trace.
            var samplesByThread = new Dictionary<int, List<double>>();
            object samplesLock = new();

            Action eventGeneratingAction = () =>
            {
                Logger.logger.Log($"Burning CPU for {WorkloadDurationMs} ms");
                long deadline = Environment.TickCount64 + WorkloadDurationMs;
                long counter = 0;
                while (Environment.TickCount64 < deadline)
                {
                    for (int i = 0; i < 100_000; i++)
                    {
                        counter++;
                    }
                }
                Logger.logger.Log($"Workload done (counter={counter})");
            };

            Func<EventPipeEventSource, Func<int>> validator = (source) =>
            {
                source.Dynamic.All += (eventData) =>
                {
                    // Filter primarily by provider name and the well-known sample event id (0).
                    // Matching by event name alone is brittle when metadata is unavailable.
                    if (eventData.ProviderName != SampleProviderName)
                        return;
                    if ((int)eventData.ID != 0)
                        return;

                    int tid = eventData.ThreadID;
                    double ts = eventData.TimeStampRelativeMSec;
                    lock (samplesLock)
                    {
                        if (!samplesByThread.TryGetValue(tid, out var list))
                        {
                            list = new List<double>();
                            samplesByThread[tid] = list;
                        }
                        list.Add(ts);
                    }
                };

                return () =>
                {
                    lock (samplesLock)
                    {
                        if (samplesByThread.Count == 0)
                        {
                            Logger.logger.Log("FAIL: No ThreadSample events observed at all.");
                            return -1;
                        }

                        Logger.logger.Log($"Observed sample events from {samplesByThread.Count} thread(s):");
                        foreach (var (tid, samples) in samplesByThread.OrderByDescending(kv => kv.Value.Count))
                        {
                            Logger.logger.Log($"  thread {tid}: {samples.Count} samples");
                        }

                        // Pick the busiest thread — it's the one the CPU-burn loop ran on, and the most
                        // reliable signal. Background threads (finalizer, GC, etc.) sample sparsely.
                        var busiest = samplesByThread.OrderByDescending(kv => kv.Value.Count).First();
                        int busiestTid = busiest.Key;
                        List<double> sortedTimes = busiest.Value.OrderBy(t => t).ToList();
                        int count = sortedTimes.Count;

                        Logger.logger.Log($"Busiest thread: {busiestTid} with {count} samples");

                        if (count < MinExpectedSamples)
                        {
                            Logger.logger.Log($"FAIL: busiest thread has {count} samples, expected >= {MinExpectedSamples}.");
                            return -1;
                        }

                        if (count > MaxAllowedSamples)
                        {
                            Logger.logger.Log($"FAIL: busiest thread has {count} samples, expected <= {MaxAllowedSamples}. " +
                                              $"This suggests DOTNET_EventPipeThreadSamplingRate was ignored " +
                                              $"and the default (~1ms) rate was used.");
                            return -1;
                        }

                        var intervals = new List<double>(count - 1);
                        for (int i = 1; i < count; i++)
                        {
                            intervals.Add(sortedTimes[i] - sortedTimes[i - 1]);
                        }
                        intervals.Sort();
                        double median = intervals[intervals.Count / 2];
                        double mean = intervals.Average();
                        Logger.logger.Log($"Busiest thread inter-sample interval (ms): " +
                                          $"min={intervals.First():F2} median={median:F2} mean={mean:F2} max={intervals.Last():F2}");

                        if (median < MinMedianIntervalMs)
                        {
                            Logger.logger.Log($"FAIL: median inter-sample interval is {median:F2}ms, expected >= {MinMedianIntervalMs}ms.");
                            return -1;
                        }

                        Logger.logger.Log("PASS: sample interval is consistent with the configured rate.");
                        return 100;
                    }
                };
            };

            return IpcTraceTest.RunAndValidateEventCounts(
                expectedEventCounts,
                eventGeneratingAction,
                providers,
                circularBufferMB: 1024,
                optionalTraceValidator: validator);
        }
    }
}
