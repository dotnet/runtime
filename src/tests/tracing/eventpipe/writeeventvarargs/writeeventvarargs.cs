// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.NETCore.Client;
using Xunit;
using TestLibrary;

namespace Tracing.Tests.WriteEventVarargs
{
    [EventSource(Name = "WriteEventVarargsEventSource")]
    public sealed class WriteEventVarargsEventSource : EventSource
    {
        private WriteEventVarargsEventSource() { }
        public static WriteEventVarargsEventSource Log = new WriteEventVarargsEventSource();

        // Resolves to the strongly-typed WriteEvent(int, int, string) overload, which
        // writes to EventPipe based on the per-event EnabledForEventPipe flag.
        [Event(1)]
        public void SpecializedEvent(int id, string name) => WriteEvent(1, id, name);

        // Boxing the arguments forces the WriteEvent(int, params object?[]) overload, which
        // serializes through EventProvider.WriteEvent(object[]). This path was dropped for
        // EventPipe sessions enabled with keyword 0 because the keyword check did not treat
        // an "any" keyword mask of 0 as "match all keywords".
        [Event(2)]
        public void VarargsEvent(int id, string name) => WriteEvent(2, (object)id, (object)name);
    }

    public class WriteEventVarargs
    {
        [ActiveIssue("WASM doesn't support diagnostics tracing", TestPlatforms.Browser)]
        [ActiveIssue("Can't find file dotnet-diagnostic-{pid}-*-socket", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsRiscv64Process))]
        [Fact]
        public static int TestEntryPoint()
        {
            // This test validates that both the specialized and the object[] varargs
            // WriteEvent overloads are written to an EventPipe session that is enabled
            // with a keyword of 0 (which means "match all keywords").

            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("WriteEventVarargsEventSource", EventLevel.Verbose, keywords: 0),
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose)
            };

            var ret = IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, _optionalTraceValidator);
            if (ret < 0)
                return ret;
            else
                return 100;
        }

        private const int Iterations = 100_000;

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "WriteEventVarargsEventSource", new ExpectedEventCount(2 * Iterations, 0.30f) },
            { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
            { "Microsoft-DotNETCore-SampleProfiler", -1 }
        };

        private static Action _eventGeneratingAction = () =>
        {
            for (int i = 0; i < Iterations; i++)
            {
                if (i % 10_000 == 0)
                    Logger.logger.Log($"Fired events {i:N0}/{Iterations:N0} times...");

                WriteEventVarargsEventSource.Log.SpecializedEvent(i, "specialized");
                WriteEventVarargsEventSource.Log.VarargsEvent(i, "varargs");
            }
        };

        // Explicitly validate that the varargs (object[]) event is present in the stream.
        // Without the fix this count is 0 even though the specialized event is emitted.
        private static Func<EventPipeEventSource, Func<int>> _optionalTraceValidator = (source) =>
        {
            int specializedCount = 0;
            int varargsCount = 0;

            source.Dynamic.All += (eventData) =>
            {
                if (eventData.ProviderName != "WriteEventVarargsEventSource")
                    return;

                if (eventData.EventName == "SpecializedEvent")
                    specializedCount++;
                else if (eventData.EventName == "VarargsEvent")
                    varargsCount++;
            };

            return () =>
            {
                Logger.logger.Log($"SpecializedEvent count: {specializedCount}");
                Logger.logger.Log($"VarargsEvent count: {varargsCount}");

                if (varargsCount == 0)
                {
                    Logger.logger.Log("VarargsEvent (object[] WriteEvent overload) was not written to EventPipe.");
                    return -1;
                }

                // Both overloads fire the same number of times, so they should have comparable
                // counts. Allow generous slack for buffered-event loss under load.
                if (Math.Abs(varargsCount - specializedCount) > specializedCount * 0.30f)
                {
                    Logger.logger.Log("VarargsEvent count diverged too far from SpecializedEvent count.");
                    return -1;
                }

                return 100;
            };
        };
    }
}
