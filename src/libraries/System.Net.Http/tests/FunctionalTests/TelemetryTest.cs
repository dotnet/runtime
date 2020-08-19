// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class TelemetryTest : HttpClientHandlerTestBase
    {
        public TelemetryTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(HttpClient).Assembly.GetType("System.Net.Http.HttpTelemetry", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("System.Net.Http", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("d30b5633-7ef1-5485-b4e0-94979b102068"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EventSource_SuccessfulRequest_LogsStartStop()
        {
            RemoteExecutor.Invoke(async useVersionString =>
            {
                Version version = Version.Parse(useVersionString);
                using var listener = new TestEventListener("System.Net.Http", EventLevel.Verbose, eventCounterInterval: 0.1d);

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    await GetFactoryForVersion(version).CreateClientAndServerAsync(
                        async uri =>
                        {
                            using HttpClient client = CreateHttpClient(useVersionString);
                            await client.GetStringAsync(uri);
                        },
                        async server =>
                        {
                            await server.AcceptConnectionAsync(async connection =>
                            {
                                await Task.Delay(300);
                                await connection.ReadRequestDataAsync();
                                await connection.SendResponseAsync();
                            });
                        });

                    await Task.Delay(300);
                });
                Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself

                EventWrittenEventArgs start = Assert.Single(events, e => e.EventName == "RequestStart");
                ValidateStartEventPayload(start);

                EventWrittenEventArgs stop = Assert.Single(events, e => e.EventName == "RequestStop");
                Assert.Empty(stop.Payload);

                Assert.DoesNotContain(events, e => e.EventName == "RequestAborted");

                if (version.Major == 1)
                {
                    Assert.Single(events, e => e.EventName == "Http11ConnectionEstablished");
                    Assert.Single(events, e => e.EventName == "Http11ConnectionClosed");
                }
                else
                {
                    Assert.Single(events, e => e.EventName == "Http20ConnectionEstablished");
                    Assert.Single(events, e => e.EventName == "Http20ConnectionClosed");
                }

                Assert.Single(events, e => e.EventName == "ResponseHeadersBegin");

                VerifyEventCounters(events, requestCount: 1, shouldHaveFailures: false);
            }, UseVersion.ToString()).Dispose();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EventSource_UnsuccessfulRequest_LogsStartAbortedStop()
        {
            RemoteExecutor.Invoke(async useVersionString =>
            {
                using var listener = new TestEventListener("System.Net.Http", EventLevel.Verbose, eventCounterInterval: 0.1d);

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    var semaphore = new SemaphoreSlim(0, 1);
                    var cts = new CancellationTokenSource();

                    await GetFactoryForVersion(Version.Parse(useVersionString)).CreateClientAndServerAsync(
                        async uri =>
                        {
                            using HttpClient client = CreateHttpClient(useVersionString);
                            await Assert.ThrowsAsync<TaskCanceledException>(async () => await client.GetStringAsync(uri, cts.Token));
                            semaphore.Release();
                        },
                        async server =>
                        {
                            await server.AcceptConnectionAsync(async connection =>
                            {
                                cts.CancelAfter(TimeSpan.FromMilliseconds(300));
                                Assert.True(await semaphore.WaitAsync(TimeSpan.FromSeconds(5)));
                                connection.Dispose();
                            });
                        });

                    await Task.Delay(300);
                });
                Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself

                EventWrittenEventArgs start = Assert.Single(events, e => e.EventName == "RequestStart");
                ValidateStartEventPayload(start);

                EventWrittenEventArgs abort = Assert.Single(events, e => e.EventName == "RequestAborted");
                Assert.Empty(abort.Payload);

                EventWrittenEventArgs stop = Assert.Single(events, e => e.EventName == "RequestStop");
                Assert.Empty(stop.Payload);

                VerifyEventCounters(events, requestCount: 1, shouldHaveFailures: true);
            }, UseVersion.ToString()).Dispose();
        }

        protected static void ValidateStartEventPayload(EventWrittenEventArgs startEvent)
        {
            Assert.Equal("RequestStart", startEvent.EventName);
            Assert.Equal(6, startEvent.Payload.Count);

            Assert.StartsWith("http", (string)startEvent.Payload[0]);
            Assert.NotEmpty((string)startEvent.Payload[1]); // host
            Assert.True(startEvent.Payload[2] is int port && port >= 0 && port <= 65535);
            Assert.NotEmpty((string)startEvent.Payload[3]); // pathAndQuery
            Assert.True(startEvent.Payload[4] is int versionMajor && (versionMajor == 1 || versionMajor == 2));
            Assert.True(startEvent.Payload[5] is int versionMinor && (versionMinor == 1 || versionMinor == 0));
        }

        protected static void VerifyEventCounters(ConcurrentQueue<EventWrittenEventArgs> events, int requestCount, bool shouldHaveFailures, bool shouldHaveQueuedRequests = false)
        {
            Dictionary<string, double[]> eventCounters = events
                .Where(e => e.EventName == "EventCounters")
                .Select(e => (IDictionary<string, object>)e.Payload.Single())
                .GroupBy(d => (string)d["Name"], d => (double)(d.ContainsKey("Mean") ? d["Mean"] : d["Increment"]))
                .ToDictionary(p => p.Key, p => p.ToArray());

            Assert.True(eventCounters.TryGetValue("requests-started", out double[] requestsStarted));
            Assert.Equal(requestCount, requestsStarted[^1]);

            Assert.True(eventCounters.TryGetValue("requests-started-rate", out double[] requestRate));
            Assert.Contains(requestRate, r => r > 0);

            Assert.True(eventCounters.TryGetValue("requests-aborted", out double[] requestsAborted));
            Assert.True(eventCounters.TryGetValue("requests-aborted-rate", out double[] requestsAbortedRate));
            if (shouldHaveFailures)
            {
                Assert.Equal(1, requestsAborted[^1]);
                Assert.Contains(requestsAbortedRate, r => r > 0);
            }
            else
            {
                Assert.All(requestsAborted, a => Assert.Equal(0, a));
                Assert.All(requestsAbortedRate, r => Assert.Equal(0, r));
            }

            Assert.True(eventCounters.TryGetValue("current-requests", out double[] currentRequests));
            Assert.Contains(currentRequests, c => c > 0);
            Assert.Equal(0, currentRequests[^1]);

            Assert.True(eventCounters.TryGetValue("http11-connections-current-total", out double[] http11ConnectionsTotal));
            Assert.All(http11ConnectionsTotal, c => Assert.True(c >= 0));
            Assert.Equal(0, http11ConnectionsTotal[^1]);

            Assert.True(eventCounters.TryGetValue("http20-connections-current-total", out double[] http20ConnectionsTotal));
            Assert.All(http20ConnectionsTotal, c => Assert.True(c >= 0));
            Assert.Equal(0, http20ConnectionsTotal[^1]);

            Assert.True(eventCounters.TryGetValue("http11-requests-queue-duration", out double[] requestQueueDurations));
            Assert.Equal(0, requestQueueDurations[^1]);
            if (shouldHaveQueuedRequests)
            {
                Assert.Contains(requestQueueDurations, d => d > 0);
                Assert.All(requestQueueDurations, d => Assert.True(d >= 0));
            }
            else
            {
                Assert.All(requestQueueDurations, d => Assert.True(d == 0));
            }
        }
    }

    public sealed class TelemetryTest_Http11 : TelemetryTest
    {
        public TelemetryTest_Http11(ITestOutputHelper output) : base(output) { }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EventSource_ConnectionPoolAtMaxConnections_LogsRequestLeftQueue()
        {
            RemoteExecutor.Invoke(async () =>
            {
                using var listener = new TestEventListener("System.Net.Http", EventLevel.Verbose, eventCounterInterval: 0.1d);

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    var firstRequestReceived = new SemaphoreSlim(0, 1);
                    var secondRequestSent = new SemaphoreSlim(0, 1);

                    await LoopbackServer.CreateClientAndServerAsync(
                        async uri =>
                        {
                            using var handler = new SocketsHttpHandler
                            {
                                MaxConnectionsPerServer = 1
                            };

                            using var client = new HttpClient(handler);

                            Task firstRequest = client.GetStringAsync(uri);
                            Assert.True(await firstRequestReceived.WaitAsync(TimeSpan.FromSeconds(10)));

                            // We are now at the connection limit, the next request will wait for the first one to complete
                            Task secondRequest = client.GetStringAsync(uri);
                            secondRequestSent.Release();

                            await new[] { firstRequest, secondRequest }.WhenAllOrAnyFailed();
                        },
                        async server =>
                        {
                            await server.AcceptConnectionAsync(async connection =>
                            {
                                firstRequestReceived.Release();
                                await connection.ReadRequestDataAsync();
                                Assert.True(await secondRequestSent.WaitAsync(TimeSpan.FromSeconds(10)));
                                await Task.Delay(100);
                                await connection.SendResponseAsync();
                            });

                            await server.HandleRequestAsync();
                        });

                    await Task.Delay(300);
                });
                Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself

                EventWrittenEventArgs[] starts = events.Where(e => e.EventName == "RequestStart").ToArray();
                Assert.Equal(2, starts.Length);
                Assert.All(starts, s => ValidateStartEventPayload(s));

                EventWrittenEventArgs[] stops = events.Where(e => e.EventName == "RequestStop").ToArray();
                Assert.Equal(2, stops.Length);
                Assert.All(stops, s => Assert.Empty(s.Payload));

                Assert.DoesNotContain(events, e => e.EventName == "RequestAborted");

                EventWrittenEventArgs[] connectionsEstablished = events.Where(e => e.EventName == "Http11ConnectionEstablished").ToArray();
                Assert.Equal(2, connectionsEstablished.Length);
                Assert.All(connectionsEstablished, c => Assert.Empty(c.Payload));

                EventWrittenEventArgs[] connectionsClosed = events.Where(e => e.EventName == "Http11ConnectionClosed").ToArray();
                Assert.Equal(2, connectionsClosed.Length);
                Assert.All(connectionsClosed, c => Assert.Empty(c.Payload));

                EventWrittenEventArgs requestLeftQueue = Assert.Single(events, e => e.EventName == "Http11RequestLeftQueue");
                double duration = Assert.IsType<double>(Assert.Single(requestLeftQueue.Payload));
                Assert.True(duration > 0);

                EventWrittenEventArgs[] responseHeadersBegin = events.Where(e => e.EventName == "ResponseHeadersBegin").ToArray();
                Assert.Equal(2, responseHeadersBegin.Length);
                Assert.All(responseHeadersBegin, r => Assert.Empty(r.Payload));

                VerifyEventCounters(events, requestCount: 2, shouldHaveFailures: false, shouldHaveQueuedRequests: true);
            }).Dispose();
        }
    }

    public sealed class TelemetryTest_Http20 : TelemetryTest
    {
        protected override Version UseVersion => HttpVersion.Version20;

        public TelemetryTest_Http20(ITestOutputHelper output) : base(output) { }
    }
}
