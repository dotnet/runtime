// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;
using Xunit;

namespace Tracing.Tests.SampleProfilerSampleType
{
    // Validates that ThreadSample events from the SampleProfiler report
    // SampleType == Managed (2) when threads are executing managed code.
    // Regression test for https://github.com/dotnet/runtime/issues/123996
    public class SampleProfilerSampleType
    {
        private const uint SampleTypeExternal = 1;
        private const uint SampleTypeManaged = 2;

        [Fact]
        public static int TestEntryPoint()
        {
            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose)
            };

            return IpcTraceTest.RunAndValidateEventCounts(
                _expectedEventCounts,
                _eventGeneratingAction,
                providers,
                1024,
                _DoesTraceContainEvents);
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
            { "Microsoft-DotNETCore-SampleProfiler", -1 }
        };

        private static Action _eventGeneratingAction = () =>
        {
            // Spin doing managed work so the sample profiler can capture
            // ThreadSample events while we are in cooperative (managed) mode.
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 3000)
            {
                DoManagedWork();
            }
        };

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DoManagedWork()
        {
            long sum = 0;
            for (int i = 0; i < 100_000; i++)
            {
                sum += i;
            }

            GC.KeepAlive(sum);
        }

        private static Func<EventPipeEventSource, Func<int>> _DoesTraceContainEvents = (source) =>
        {
            int managedSamples = 0;
            int externalSamples = 0;
            int totalThreadSamples = 0;

            source.Dynamic.All += (eventData) =>
            {
                if (eventData.ProviderName != "Microsoft-DotNETCore-SampleProfiler")
                    return;

                totalThreadSamples++;
                try
                {
                    // The ThreadSample event payload is a single uint32 representing the sample type.
                    Span<byte> data = eventData.EventData().AsSpan();
                    if (data.Length >= 4)
                    {
                        uint sampleType = BitConverter.ToUInt32(data.Slice(0, 4));
                        if (sampleType == SampleTypeManaged)
                            managedSamples++;
                        else if (sampleType == SampleTypeExternal)
                            externalSamples++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.logger.Log($"Exception reading SampleType payload: {ex}");
                }
            };

            return () =>
            {
                Logger.logger.Log($"Total ThreadSample events: {totalThreadSamples}");
                Logger.logger.Log($"Managed samples: {managedSamples}");
                Logger.logger.Log($"External samples: {externalSamples}");

                if (totalThreadSamples == 0)
                {
                    Logger.logger.Log("FAIL: No ThreadSample events were received.");
                    return -1;
                }

                if (managedSamples == 0)
                {
                    Logger.logger.Log("FAIL: No ThreadSample events had SampleType == Managed.");
                    return -1;
                }

                Logger.logger.Log("PASS: At least some ThreadSample events reported SampleType == Managed.");
                return 100;
            };
        };
    }
}
