// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Tracing.Tests.Common;
using Xunit;
using TestLibrary;

namespace Tracing.Tests.NonLossyBlockValidation
{
    public sealed class BlockModeEventSource : EventSource
    {
        private BlockModeEventSource() {}
        public static BlockModeEventSource Log = new BlockModeEventSource();
        public void BlockModeEvent() { WriteEvent(1, "BlockModeEvent"); }
    }

    public class NonLossyBlockValidation
    {
        // Four concurrent producers emit a serialized 100,000-event burst substantially larger than this buffer.
        // Block mode must park producers whenever the reader cannot keep up, then deliver every event after capacity
        // becomes available.
        private const int CircularBufferMB = 1;
        private const int EventCount = 100_000;
        private const int ProducerCount = 4;

        [ActiveIssue("WASM doesn't support diagnostics tracing", TestPlatforms.Browser)]
        [ActiveIssue("Can't find file dotnet-diagnostic-{pid}-*-socket", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsRiscv64Process))]
        [Fact]
        public static int InvalidBufferingModeIsRejected()
        {
            Logger.logger.Log("==Invalid buffering mode rejection: STARTING==");

            List<EventPipeProvider> providers = CreateProviders();

            DiagnosticsClient client = new(Process.GetCurrentProcess().Id);
            EventPipeSessionConfiguration invalidModeConfig = new(
                providers,
                circularBufferSizeMB: CircularBufferMB,
                rundownKeyword: 0,
                requestStackwalk: true,
                bufferingMode: (EventPipeBufferingMode)2);

            EventPipeSession session;
            try
            {
                session = client.StartEventPipeSession(invalidModeConfig);
            }
            catch (DiagnosticsClientException ex)
            {
                Logger.logger.Log($"Server rejected invalid buffering mode as expected: {ex.GetType().Name}: {ex.Message}");
                Logger.logger.Log("==Invalid buffering mode rejection: PASSED==");
                return 100;
            }

            using (session)
            {
                Logger.logger.Log("Server accepted an invalid buffering mode; expected it to be rejected.");
                session.Stop();
                return -1;
            }
        }

        [ActiveIssue("WASM doesn't support diagnostics tracing", TestPlatforms.Browser)]
        [ActiveIssue("Can't find file dotnet-diagnostic-{pid}-*-socket", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsRiscv64Process))]
        [Fact]
        public static int BlockModeDoesNotDropEvents()
        {
            Logger.logger.Log("==Block mode no-drop: STARTING==");

            return IpcTraceTest.RunAndValidateEventCounts(
                _expectedEventCounts,
                _eventGeneratingAction,
                CreateProviders(),
                circularBufferMB: CircularBufferMB,
                optionalTraceValidator: null,
                enableRundownProvider: false,
                bufferingMode: EventPipeBufferingMode.Block,
                failOnEventsLost: true);
        }

        private static List<EventPipeProvider> CreateProviders() =>
            new()
            {
                new EventPipeProvider("BlockModeEventSource", EventLevel.Verbose)
            };

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "BlockModeEventSource", new ExpectedEventCount(EventCount, 0.0f) }
        };

        private static Action _eventGeneratingAction = () =>
        {
            Debug.Assert(EventCount % ProducerCount == 0);
            int eventsPerProducer = EventCount / ProducerCount;

            Parallel.For(0, ProducerCount, _ =>
            {
                for (int i = 0; i < eventsPerProducer; i++)
                {
                    BlockModeEventSource.Log.BlockModeEvent();
                }
            });
        };
    }
}
