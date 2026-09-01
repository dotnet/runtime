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

        // Using the EventSourcePrimitive varargs overload funnels through WriteEventVarargs /
        // EventProvider.WriteEvent(object[]). This path was dropped for
        // EventPipe sessions enabled with keyword 0 because the keyword check did not treat
        // an "any" keyword mask of 0 as "match all keywords".
        [Event(1)]
        public void VarargsEvent(int id, string name) => WriteEvent(1, new EventSource.EventSourcePrimitive[] { id, name });

        // A varargs event declared with a non-zero keyword, used to validate that the varargs
        // path still flows when a session's keyword mask matches the event's keyword.
        [Event(2, Keywords = Keywords.MyKeyword)]
        public void KeywordVarargsEvent(int id, string name) => WriteEvent(2, new EventSource.EventSourcePrimitive[] { id, name });
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

            ret = RunKeywordMatchScenario();
            if (ret != 100)
                return ret;

            return RunNonZeroKeywordScenarioWithKeywordlessAndKeywordedEvents();
        }

        // A session enabled with keyword 0 means "match all keywords". The object[] varargs overload
        // must be written to it. Prior to the fix this path was dropped
        // because the keyword check did not treat an "any" keyword mask of 0 as match-all.
        private static int RunKeywordZeroScenario()
        {
            return RunScenario(
                "=== Scenario: provider enabled with keyword 0 ===",
                sessionKeywords: 0,
                expectedEventName: nameof(WriteEventVarargsEventSource.VarargsEvent),
                emitEvent: () => WriteEventVarargsEventSource.Log.VarargsEvent(1, "varargs"),
                missingEventMessage: "VarargsEvent (EventSourcePrimitive[] WriteEvent overload) was not written to EventPipe.");
        }

        // A session enabled with a non-zero keyword that matches the event's keyword must also see
        // the varargs overload. This guards the regular (non-zero) keyword path of the varargs write.
        private static int RunKeywordMatchScenario()
        {
            return RunScenario(
                "=== Scenario: provider enabled with a matching non-zero keyword ===",
                sessionKeywords: (long)WriteEventVarargsEventSource.Keywords.MyKeyword,
                expectedEventName: nameof(WriteEventVarargsEventSource.KeywordVarargsEvent),
                emitEvent: () => WriteEventVarargsEventSource.Log.KeywordVarargsEvent(1, "keyword"),
                missingEventMessage: "KeywordVarargsEvent (EventSourcePrimitive[] WriteEvent overload) was not written to EventPipe.");
        }

        // Under a non-zero session keyword, keywordless events (Keywords=0) should still pass
        // keyword filtering, while keyworded events should continue to match normally.
        private static int RunNonZeroKeywordScenarioWithKeywordlessAndKeywordedEvents()
        {
            Logger.logger.Log("=== Scenario: non-zero keyword session still includes keywordless events ===");

            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("WriteEventVarargsEventSource", EventLevel.Verbose, keywords: (long)WriteEventVarargsEventSource.Keywords.MyKeyword)
            };

            var expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
            {
                { "WriteEventVarargsEventSource", -1 }
            };

            Action emitEvent = () =>
            {
                WriteEventVarargsEventSource.Log.VarargsEvent(1, "varargs");
                WriteEventVarargsEventSource.Log.KeywordVarargsEvent(2, "keyword");
            };

            Func<EventPipeEventSource, Func<int>> optionalTraceValidator = (source) =>
            {
                int varargsCount = 0;
                int keywordVarargsCount = 0;

                source.Dynamic.All += (eventData) =>
                {
                    if (eventData.ProviderName != "WriteEventVarargsEventSource")
                        return;

                    if (eventData.EventName == nameof(WriteEventVarargsEventSource.VarargsEvent))
                        varargsCount++;
                    else if (eventData.EventName == nameof(WriteEventVarargsEventSource.KeywordVarargsEvent))
                        keywordVarargsCount++;
                };

                return () =>
                {
                    Logger.logger.Log($"VarargsEvent count: {varargsCount}");
                    Logger.logger.Log($"KeywordVarargsEvent count: {keywordVarargsCount}");

                    if (varargsCount == 0)
                    {
                        Logger.logger.Log("VarargsEvent (keywordless event) was not written under non-zero session keywords.");
                        return -1;
                    }

                    if (keywordVarargsCount == 0)
                    {
                        Logger.logger.Log("KeywordVarargsEvent was not written under matching non-zero session keywords.");
                        return -1;
                    }

                    return 100;
                };
            };

            return IpcTraceTest.RunAndValidateEventCounts(expectedEventCounts, emitEvent, providers, 1024, optionalTraceValidator);
        }

        private static int RunScenario(string scenarioName, long sessionKeywords, string expectedEventName, Action emitEvent, string missingEventMessage)
        {
            Logger.logger.Log(scenarioName);

            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("WriteEventVarargsEventSource", EventLevel.Verbose, keywords: sessionKeywords)
            };

            var expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
            {
                { "WriteEventVarargsEventSource", -1 }
            };

            Func<EventPipeEventSource, Func<int>> optionalTraceValidator = (source) =>
            {
                int count = 0;

                source.Dynamic.All += (eventData) =>
                {
                    if (eventData.ProviderName != "WriteEventVarargsEventSource")
                        return;

                    if (eventData.EventName == expectedEventName)
                        count++;
                };

                return () =>
                {
                    Logger.logger.Log($"{expectedEventName} count: {count}");

                    if (count == 0)
                    {
                        Logger.logger.Log(missingEventMessage);
                        return -1;
                    }

                    return 100;
                };
            };

            return IpcTraceTest.RunAndValidateEventCounts(expectedEventCounts, emitEvent, providers, 1024, optionalTraceValidator);
        }
    }
}
