﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Security.Tests
{
    public class TelemetryTest
    {
        private const string ActivitySourceName = "Experimental.System.Net.Security";
        private const string ActivityName = ActivitySourceName + ".TlsHandshake";

        [Fact]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(SslStream).Assembly.GetType("System.Net.Security.NetSecurityTelemetry", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("System.Net.Security", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("7beee6b1-e3fa-5ddb-34be-1404ad0e2520"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "X509 certificate store is not supported on iOS or tvOS.")] // Match SslStream_StreamToStream_Authentication_Success
        [InlineData(false)]
        [InlineData(true)]
        public async Task SuccessfulHandshake_ActivityRecorded(bool synchronousApi)
        {
            await RemoteExecutor.Invoke(async synchronousApiStr =>
            {
                using ActivityRecorder recorder = new ActivityRecorder(ActivitySourceName, ActivityName);

                SslStreamStreamToStreamTest test = bool.Parse(synchronousApiStr)
                    ? new SslStreamStreamToStreamTest_SyncParameters()
                    : new SslStreamStreamToStreamTest_Async();
                await test.SslStream_StreamToStream_Authentication_Success();

                recorder.VerifyActivityRecorded(2); // client + server
                Activity clientActivity = recorder.FinishedActivities.Single(a => a.DisplayName.StartsWith("TLS client"));
                Activity serverActivity = recorder.FinishedActivities.Single(a => a.DisplayName.StartsWith("TLS server"));
                Assert.True(Enum.GetValues(typeof(SslProtocols)).Length == 8, "We need to extend the mapping in case new values are added to SslProtocols.");
#pragma warning disable 0618, SYSLIB0039
                (string protocolName, string protocolVersion) = test.SslProtocol switch
                {
                    SslProtocols.Ssl2 => ("ssl", "2"),
                    SslProtocols.Ssl3 => ("ssl", "3"),
                    SslProtocols.Tls => ("tls", "1"),
                    SslProtocols.Tls11 => ("tls", "1.1"),
                    SslProtocols.Tls12 => ("tls", "1.2"),
                    SslProtocols.Tls13 => ("tls", "1.3"),
                    _ => throw new Exception("unknown protocol")
                };
#pragma warning restore 0618, SYSLIB0039

                Assert.Equal(ActivityKind.Internal, clientActivity.Kind);
                Assert.True(clientActivity.Duration > TimeSpan.Zero);
                Assert.Equal(ActivityName, clientActivity.OperationName);
                Assert.Equal($"TLS client handshake {test.Name}", clientActivity.DisplayName);
                ActivityAssert.HasTag(clientActivity, "server.address", test.Name);
                ActivityAssert.HasTag(clientActivity, "tls.protocol.name", protocolName);
                ActivityAssert.HasTag(clientActivity, "tls.protocol.version", protocolVersion);
                ActivityAssert.HasNoTag(clientActivity, "error.type");

                Assert.Equal(ActivityKind.Internal, serverActivity.Kind);
                Assert.True(serverActivity.Duration > TimeSpan.Zero);
                Assert.Equal(ActivityName, serverActivity.OperationName);
                Assert.StartsWith($"TLS server handshake", serverActivity.DisplayName);
                ActivityAssert.HasTag(serverActivity, "tls.protocol.name", protocolName);
                ActivityAssert.HasTag(serverActivity, "tls.protocol.version", protocolVersion);
                ActivityAssert.HasNoTag(serverActivity, "error.type");

            }, synchronousApi.ToString()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task FailingHandshake_ActivityRecorded()
        {
            await RemoteExecutor.Invoke(async () =>
            {
                using ActivityRecorder recorder = new ActivityRecorder(ActivitySourceName, ActivityName);

                var test = new SslStreamStreamToStreamTest_Async();
                await test.SslStream_StreamToStream_Authentication_IncorrectServerName_Fail();

                recorder.VerifyActivityRecorded(2); // client + server

                Activity clientActivity = recorder.FinishedActivities.Single(a => a.DisplayName.StartsWith("TLS client"));
                Activity serverActivity = recorder.FinishedActivities.Single(a => a.DisplayName.StartsWith("TLS server"));

                Assert.Equal(ActivityKind.Internal, clientActivity.Kind);
                Assert.Equal(ActivityStatusCode.Error, clientActivity.Status);
                Assert.True(clientActivity.Duration > TimeSpan.Zero);
                Assert.Equal(ActivityName, clientActivity.OperationName);
                Assert.Equal($"TLS client handshake {test.Name}", clientActivity.DisplayName);
                ActivityAssert.HasTag(clientActivity, "server.address", test.Name);
                ActivityAssert.HasTag(clientActivity, "error.type", typeof(AuthenticationException).FullName);

                Assert.Equal(ActivityKind.Internal, serverActivity.Kind);
                Assert.True(serverActivity.Duration > TimeSpan.Zero);
                Assert.Equal(ActivityName, serverActivity.OperationName);
                Assert.StartsWith($"TLS server handshake", serverActivity.DisplayName);
            }).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "X509 certificate store is not supported on iOS or tvOS.")] // Match SslStream_StreamToStream_Authentication_Success
        public static async Task EventSource_SuccessfulHandshake_LogsStartStop()
        {
            await RemoteExecutor.Invoke(async () =>
            {
                try
                {
                    using var listener = new TestEventListener("System.Net.Security", EventLevel.Verbose, eventCounterInterval: 0.1d);
                    listener.AddActivityTracking();

                    await PrepareEventCountersAsync(listener);

                    var events = new ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)>();
                    await listener.RunWithCallbackAsync(e =>
                    {
                        events.Enqueue((e, e.ActivityId));

                        if (e.EventName == "HandshakeStart")
                        {
                            // Wait for a new counter group so that current-tls-handshakes is guaranteed a non-zero value
                            WaitForEventCountersAsync(events).GetAwaiter().GetResult();
                        }
                    },
                    async () =>
                    {
                        // Invoke tests that'll cause some events to be generated
                        var test = new SslStreamStreamToStreamTest_Async();
                        await test.SslStream_StreamToStream_Authentication_Success();
                        await WaitForEventCountersAsync(events);
                    });
                    Assert.DoesNotContain(events, ev => ev.Event.EventId == 0); // errors from the EventSource itself

                    (EventWrittenEventArgs Event, Guid ActivityId)[] starts = events.Where(e => e.Event.EventName == "HandshakeStart").ToArray();
                    Assert.Equal(2, starts.Length);
                    Assert.All(starts, s => Assert.Equal(2, s.Event.Payload.Count));
                    Assert.All(starts, s => Assert.NotEqual(Guid.Empty, s.ActivityId));

                    // isServer
                    (EventWrittenEventArgs Event, Guid ActivityId) serverStart = Assert.Single(starts, s => (bool)s.Event.Payload[0]);
                    (EventWrittenEventArgs Event, Guid ActivityId) clientStart = Assert.Single(starts, s => !(bool)s.Event.Payload[0]);

                    // targetHost
                    Assert.Empty(Assert.IsType<string>(serverStart.Event.Payload[1]));
                    Assert.NotEmpty(Assert.IsType<string>(clientStart.Event.Payload[1]));

                    Assert.NotEqual(serverStart.ActivityId, clientStart.ActivityId);

                    (EventWrittenEventArgs Event, Guid ActivityId)[] stops = events.Where(e => e.Event.EventName == "HandshakeStop").ToArray();
                    Assert.Equal(2, stops.Length);

                    EventWrittenEventArgs serverStop = Assert.Single(stops, s => s.ActivityId == serverStart.ActivityId).Event;
                    EventWrittenEventArgs clientStop = Assert.Single(stops, s => s.ActivityId == clientStart.ActivityId).Event;

                    SslProtocols serverProtocol = ValidateHandshakeStopEventPayload(serverStop);
                    SslProtocols clientProtocol = ValidateHandshakeStopEventPayload(clientStop);
                    Assert.Equal(serverProtocol, clientProtocol);

                    Assert.DoesNotContain(events, e => e.Event.EventName == "HandshakeFailed");

                    VerifyEventCounters(events, shouldHaveFailures: false);
                }
                catch (SkipTestException)
                {
                    // Don't throw inside RemoteExecutor if SslStream_StreamToStream_Authentication_Success chose to skip the test
                }
            }).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "X509 certificate store is not supported on iOS or tvOS.")] // Match SslStream_StreamToStream_Authentication_Success
        public static async Task EventSource_UnsuccessfulHandshake_LogsStartFailureStop()
        {
            await RemoteExecutor.Invoke(async () =>
            {
                using var listener = new TestEventListener("System.Net.Security", EventLevel.Verbose, eventCounterInterval: 0.1d);
                listener.AddActivityTracking();

                await PrepareEventCountersAsync(listener);

                var events = new ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)>();
                await listener.RunWithCallbackAsync(e =>
                {
                    events.Enqueue((e, e.ActivityId));

                    if (e.EventName == "HandshakeStart")
                    {
                        // Wait for a new counter group so that current-tls-handshakes is guaranteed a non-zero value
                        WaitForEventCountersAsync(events).GetAwaiter().GetResult();
                    }
                },
                async () =>
                {
                    // Invoke tests that'll cause some events to be generated
                    var test = new SslStreamStreamToStreamTest_Async();
                    await test.SslStream_ServerLocalCertificateSelectionCallbackReturnsNull_Throw();
                    await WaitForEventCountersAsync(events);
                });
                Assert.DoesNotContain(events, ev => ev.Event.EventId == 0); // errors from the EventSource itself

                (EventWrittenEventArgs Event, Guid ActivityId)[] starts = events.Where(e => e.Event.EventName == "HandshakeStart").ToArray();
                Assert.Equal(2, starts.Length);
                Assert.All(starts, s => Assert.Equal(2, s.Event.Payload.Count));
                Assert.All(starts, s => Assert.NotEqual(Guid.Empty, s.ActivityId));

                // isServer
                (EventWrittenEventArgs Event, Guid ActivityId) serverStart = Assert.Single(starts, s => (bool)s.Event.Payload[0]);
                (EventWrittenEventArgs Event, Guid ActivityId) clientStart = Assert.Single(starts, s => !(bool)s.Event.Payload[0]);

                // targetHost
                Assert.Empty(Assert.IsType<string>(serverStart.Event.Payload[1]));
                Assert.NotEmpty(Assert.IsType<string>(clientStart.Event.Payload[1]));

                Assert.NotEqual(serverStart.ActivityId, clientStart.ActivityId);

                (EventWrittenEventArgs Event, Guid ActivityId)[] stops = events.Where(e => e.Event.EventName == "HandshakeStop").ToArray();
                Assert.Equal(2, stops.Length);
                Assert.All(stops, s => ValidateHandshakeStopEventPayload(s.Event, failure: true));

                EventWrittenEventArgs serverStop = Assert.Single(stops, s => s.ActivityId == serverStart.ActivityId).Event;
                EventWrittenEventArgs clientStop = Assert.Single(stops, s => s.ActivityId == clientStart.ActivityId).Event;

                (EventWrittenEventArgs Event, Guid ActivityId)[] failures = events.Where(e => e.Event.EventName == "HandshakeFailed").ToArray();
                Assert.Equal(2, failures.Length);
                Assert.All(failures, f => Assert.Equal(3, f.Event.Payload.Count));
                Assert.All(failures, f => Assert.NotEmpty(f.Event.Payload[2] as string)); // exceptionMessage

                EventWrittenEventArgs serverFailure = Assert.Single(failures, f => f.ActivityId == serverStart.ActivityId).Event;
                EventWrittenEventArgs clientFailure = Assert.Single(failures, f => f.ActivityId == clientStart.ActivityId).Event;

                // isServer
                Assert.Equal(true, serverFailure.Payload[0]);
                Assert.Equal(false, clientFailure.Payload[0]);

                VerifyEventCounters(events, shouldHaveFailures: true);
            }).DisposeAsync();
        }

        private static SslProtocols ValidateHandshakeStopEventPayload(EventWrittenEventArgs stopEvent, bool failure = false)
        {
            Assert.Equal("HandshakeStop", stopEvent.EventName);
            Assert.Equal(1, stopEvent.Payload.Count);

            var protocol = (SslProtocols)stopEvent.Payload[0];
            Assert.True(Enum.IsDefined(protocol));

            if (failure)
            {
                Assert.Equal(SslProtocols.None, protocol);
            }
            else
            {
                Assert.NotEqual(SslProtocols.None, protocol);
            }

            return protocol;
        }

        private static void VerifyEventCounters(ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)> events, bool shouldHaveFailures)
        {
            Dictionary<string, double[]> eventCounters = events
                .Select(e => e.Event)
                .Where(e => e.EventName == "EventCounters")
                .Select(e => (IDictionary<string, object>)e.Payload.Single())
                .GroupBy(d => (string)d["Name"], d => (double)(d.ContainsKey("Mean") ? d["Mean"] : d["Increment"]))
                .ToDictionary(p => p.Key, p => p.ToArray());

            Assert.True(eventCounters.TryGetValue("total-tls-handshakes", out double[] totalHandshakes));
            // 4 instead of 2 to account for the handshake we made in PrepareEventCountersAsync.
            Assert.Equal(4, totalHandshakes[^1]);

            Assert.True(eventCounters.TryGetValue("tls-handshake-rate", out double[] handshakeRate));
            Assert.Contains(handshakeRate, r => r > 0);

            Assert.True(eventCounters.TryGetValue("failed-tls-handshakes", out double[] failedHandshakes));
            if (shouldHaveFailures)
            {
                Assert.Equal(2, failedHandshakes[^1]);
            }
            else
            {
                Assert.All(failedHandshakes, f => Assert.Equal(0, f));
            }

            Assert.True(eventCounters.TryGetValue("current-tls-handshakes", out double[] currentHandshakes));
            Assert.Contains(currentHandshakes, h => h > 0);
            Assert.Equal(0, currentHandshakes[^1]);

            double[] openedSessions = eventCounters
                .Where(pair => pair.Key.EndsWith("-sessions-open"))
                .Select(pair => pair.Value[^1])
                .ToArray();

            // Events should be emitted for all 5 sessions-open counters
            Assert.Equal(5, openedSessions.Length);
            Assert.All(openedSessions, oc => Assert.Equal(0, oc));


            double[] allHandshakeDurations = eventCounters["all-tls-handshake-duration"];
            double[][] tlsHandshakeDurations = eventCounters
                .Where(pair => pair.Key.StartsWith("tls") && pair.Key.EndsWith("-handshake-duration"))
                .Select(pair => pair.Value)
                .ToArray();

            // Events should be emitted for all 4 tls**-handshake-duration counters
            Assert.Equal(4, tlsHandshakeDurations.Length);

            if (shouldHaveFailures)
            {
                Assert.All(tlsHandshakeDurations, durations => Assert.All(durations, d => Assert.Equal(0, d)));
                Assert.All(allHandshakeDurations, d => Assert.Equal(0, d));
            }
            else
            {
                Assert.Contains(tlsHandshakeDurations, durations => durations.Any(d => d > 0));
                Assert.Contains(allHandshakeDurations, d => d > 0);
            }
        }

        private static async Task WaitForEventCountersAsync(ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)> events)
        {
            DateTime startTime = DateTime.UtcNow;
            int startCount = events.Count;

            while (events.Skip(startCount).Count(e => IsTlsHandshakeRateEventCounter(e.Event)) < 3)
            {
                if (DateTime.UtcNow.Subtract(startTime) > TimeSpan.FromSeconds(30))
                    throw new TimeoutException($"Timed out waiting for EventCounters");

                await Task.Delay(100);
            }

            static bool IsTlsHandshakeRateEventCounter(EventWrittenEventArgs e)
            {
                if (e.EventName != "EventCounters")
                    return false;

                var dictionary = (IDictionary<string, object>)e.Payload.Single();

                return (string)dictionary["Name"] == "tls-handshake-rate";
            }
        }

        private static async Task PrepareEventCountersAsync(TestEventListener listener)
        {
            // There is a race condition in EventSource where counters using IncrementingPollingCounter
            // will drop increments that happened before the background timer thread first runs.
            // See https://github.com/dotnet/runtime/issues/106268#issuecomment-2284626183.
            // To workaround this issue, we ensure that the EventCounters timer is running before
            // executing any of the interesting logic under test.

            var events = new ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)>();

            await listener.RunWithCallbackAsync(e => events.Enqueue((e, e.ActivityId)), async () =>
            {
                var test = new SslStreamStreamToStreamTest_Async();
                await test.SslStream_StreamToStream_Authentication_Success();

                await WaitForEventCountersAsync(events);
            });
        }
    }
}
