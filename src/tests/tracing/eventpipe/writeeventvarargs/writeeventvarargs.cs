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

        public static class Keywords
        {
            public const EventKeywords MyKeyword = (EventKeywords)0x1;
        }

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

        // A varargs event declared with a non-zero keyword, used to validate that the varargs
        // path still flows when a session's keyword mask matches the event's keyword.
        [Event(3, Keywords = Keywords.MyKeyword)]
        public void KeywordVarargsEvent(int id, string name) => WriteEvent(3, (object)id, (object)name);
    }

    public class WriteEventVarargs
    {
        [ActiveIssue("WASM doesn't support diagnostics tracing", TestPlatforms.Browser)]
        [ActiveIssue("Can't find file dotnet-diagnostic-{pid}-*-socket", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsRiscv64Process))]
        [Fact]
        public static int TestEntryPoint()
        {
            int ret = RunKeywordZeroScenario();
            if (ret != 100)
                return ret;

            return RunKeywordMatchScenario();
        }

        private const int Iterations = 100_000;

        // A session enabled with keyword 0 means "match all keywords". Both the specialized and the
        // object[] varargs overloads must be written to it. Prior to the fix the varargs overload
        // was dropped because the keyword check did not treat an "any" keyword mask of 0 as match-all.
        private static int RunKeywordZeroScenario()
        {
            Logger.logger.Log("=== Scenario: provider enabled with keyword 0 ===");

            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("WriteEventVarargsEventSource", EventLevel.Verbose, keywords: 0),
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose)
            };

            var expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
            {
                { "WriteEventVarargsEventSource", new ExpectedEventCount(2 * Iterations, 0.30f) },
                { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
                { "Microsoft-DotNETCore-SampleProfiler", -1 }
            };

            Action eventGeneratingAction = () =>
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
            Func<EventPipeEventSource, Func<int>> optionalTraceValidator = (source) =>
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

            return IpcTraceTest.RunAndValidateEventCounts(expectedEventCounts, eventGeneratingAction, providers, 1024, optionalTraceValidator);
        }

        // A session enabled with a non-zero keyword that matches the event's keyword must also see
        // the varargs overload. This guards the regular (non-zero) keyword path of the varargs write.
        private static int RunKeywordMatchScenario()
        {
            Logger.logger.Log("=== Scenario: provider enabled with a matching non-zero keyword ===");

            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("WriteEventVarargsEventSource", EventLevel.Verbose, keywords: (long)WriteEventVarargsEventSource.Keywords.MyKeyword),
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose)
            };

            var expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
            {
                { "WriteEventVarargsEventSource", new ExpectedEventCount(Iterations, 0.30f) },
                { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
                { "Microsoft-DotNETCore-SampleProfiler", -1 }
            };

            Action eventGeneratingAction = () =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    if (i % 10_000 == 0)
                        Logger.logger.Log($"Fired events {i:N0}/{Iterations:N0} times...");

                    WriteEventVarargsEventSource.Log.KeywordVarargsEvent(i, "keyword");
                }
            };

            Func<EventPipeEventSource, Func<int>> optionalTraceValidator = (source) =>
            {
                int keywordVarargsCount = 0;

                source.Dynamic.All += (eventData) =>
                {
                    if (eventData.ProviderName != "WriteEventVarargsEventSource")
                        return;

                    if (eventData.EventName == "KeywordVarargsEvent")
                        keywordVarargsCount++;
                };

                return () =>
                {
                    Logger.logger.Log($"KeywordVarargsEvent count: {keywordVarargsCount}");

                    if (keywordVarargsCount == 0)
                    {
                        Logger.logger.Log("KeywordVarargsEvent (object[] WriteEvent overload) was not written to EventPipe.");
                        return -1;
                    }

                    return 100;
                };
            };

            return IpcTraceTest.RunAndValidateEventCounts(expectedEventCounts, eventGeneratingAction, providers, 1024, optionalTraceValidator);
        }
    }
}
