﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Text;
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

        public static IEnumerable<object[]> TestMethods_MemberData()
        {
            yield return new object[] { "GetAsync" };
            yield return new object[] { "SendAsync" };
            yield return new object[] { "GetStringAsync" };
            yield return new object[] { "GetByteArrayAsync" };
            yield return new object[] { "GetStreamAsync" };
            yield return new object[] { "InvokerSendAsync" };

            yield return new object[] { "Send" };
            yield return new object[] { "InvokerSend" };
        }

        [OuterLoop]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(TestMethods_MemberData))]
        public void EventSource_SuccessfulRequest_LogsStartStop(string testMethod)
        {
            if (UseVersion.Major != 1 && !testMethod.EndsWith("Async"))
            {
                // Synchronous requests are only supported for HTTP/1.1
                return;
            }

            RemoteExecutor.Invoke(async (useVersionString, testMethod) =>
            {
                const int ResponseContentLength = 42;

                Version version = Version.Parse(useVersionString);
                using var listener = new TestEventListener("System.Net.Http", EventLevel.Verbose, eventCounterInterval: 0.1d);

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    await GetFactoryForVersion(version).CreateClientAndServerAsync(
                        async uri =>
                        {
                            using HttpClientHandler handler = CreateHttpClientHandler(useVersionString);
                            using HttpClient client = CreateHttpClient(handler, useVersionString);
                            using var invoker = new HttpMessageInvoker(handler);

                            var request = new HttpRequestMessage(HttpMethod.Get, uri)
                            {
                                Version = version
                            };

                            switch (testMethod)
                            {
                                case "GetAsync":
                                    await client.GetAsync(uri);
                                    break;

                                case "Send":
                                    await Task.Run(() => client.Send(request));
                                    break;

                                case "SendAsync":
                                    await client.SendAsync(request);
                                    break;

                                case "GetStringAsync":
                                    await client.GetStringAsync(uri);
                                    break;

                                case "GetByteArrayAsync":
                                    await client.GetByteArrayAsync(uri);
                                    break;

                                case "GetStreamAsync":
                                    Stream responseStream = await client.GetStreamAsync(uri);
                                    await responseStream.CopyToAsync(Stream.Null);
                                    break;

                                case "InvokerSend":
                                    HttpResponseMessage syncResponse = await Task.Run(() => invoker.Send(request, cancellationToken: default));
                                    await syncResponse.Content.CopyToAsync(Stream.Null);
                                    break;

                                case "InvokerSendAsync":
                                    HttpResponseMessage asyncResponse = await invoker.SendAsync(request, cancellationToken: default);
                                    await asyncResponse.Content.CopyToAsync(Stream.Null);
                                    break;
                            }
                        },
                        async server =>
                        {
                            await server.AcceptConnectionAsync(async connection =>
                            {
                                await Task.Delay(300);
                                await connection.ReadRequestDataAsync();
                                await connection.SendResponseAsync(content: new string('a', ResponseContentLength));
                            });
                        });

                    await Task.Delay(300);
                });
                Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself

                EventWrittenEventArgs start = Assert.Single(events, e => e.EventName == "RequestStart");
                ValidateStartEventPayload(start);

                EventWrittenEventArgs stop = Assert.Single(events, e => e.EventName == "RequestStop");
                Assert.Empty(stop.Payload);

                Assert.DoesNotContain(events, e => e.EventName == "RequestFailed");

                ValidateConnectionEstablishedClosed(events, version);

                ValidateRequestResponseStartStopEvents(
                    events,
                    requestContentLength: null,
                    responseContentLength: testMethod.StartsWith("InvokerSend") ? null : ResponseContentLength,
                    count: 1);

                VerifyEventCounters(events, requestCount: 1, shouldHaveFailures: false);
            }, UseVersion.ToString(), testMethod).Dispose();
        }

        [OuterLoop]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(TestMethods_MemberData))]
        public void EventSource_UnsuccessfulRequest_LogsStartFailedStop(string testMethod)
        {
            if (UseVersion.Major != 1 && !testMethod.EndsWith("Async"))
            {
                // Synchronous requests are only supported for HTTP/1.1
                return;
            }

            RemoteExecutor.Invoke(async (useVersionString, testMethod) =>
            {
                Version version = Version.Parse(useVersionString);
                using var listener = new TestEventListener("System.Net.Http", EventLevel.Verbose, eventCounterInterval: 0.1d);

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    var semaphore = new SemaphoreSlim(0, 1);
                    var cts = new CancellationTokenSource();

                    await GetFactoryForVersion(version).CreateClientAndServerAsync(
                        async uri =>
                        {
                            using HttpClientHandler handler = CreateHttpClientHandler(useVersionString);
                            using HttpClient client = CreateHttpClient(handler, useVersionString);
                            using var invoker = new HttpMessageInvoker(handler);

                            var request = new HttpRequestMessage(HttpMethod.Get, uri)
                            {
                                Version = version
                            };

                            switch (testMethod)
                            {
                                case "GetAsync":
                                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await client.GetAsync(uri, cts.Token));
                                    break;

                                case "Send":
                                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await Task.Run(() => client.Send(request, cts.Token)));
                                    break;

                                case "SendAsync":
                                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await client.SendAsync(request, cts.Token));
                                    break;

                                case "GetStringAsync":
                                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await client.GetStringAsync(uri, cts.Token));
                                    break;

                                case "GetByteArrayAsync":
                                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await client.GetByteArrayAsync(uri, cts.Token));
                                    break;

                                case "GetStreamAsync":
                                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await client.GetStreamAsync(uri, cts.Token));
                                    break;

                                case "InvokerSend":
                                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await Task.Run(() => invoker.Send(request, cts.Token)));
                                    break;

                                case "InvokerSendAsync":
                                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await invoker.SendAsync(request, cts.Token));
                                    break;
                            }

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

                EventWrittenEventArgs failure = Assert.Single(events, e => e.EventName == "RequestFailed");
                Assert.Empty(failure.Payload);

                EventWrittenEventArgs stop = Assert.Single(events, e => e.EventName == "RequestStop");
                Assert.Empty(stop.Payload);

                ValidateConnectionEstablishedClosed(events, version);

                VerifyEventCounters(events, requestCount: 1, shouldHaveFailures: true);
            }, UseVersion.ToString(), testMethod).Dispose();
        }

        [OuterLoop]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("PostAsync")]
        [InlineData("Send")]
        [InlineData("SendAsync")]
        [InlineData("SendChunkedAsync")]
        [InlineData("InvokerSend")]
        [InlineData("InvokerSendAsync")]
        public void EventSource_SendingRequestContent_LogsRequestContentStartStop(string testMethod)
        {
            if (UseVersion.Major != 1 && !testMethod.EndsWith("Async"))
            {
                // Synchronous requests are only supported for HTTP/1.1
                return;
            }

            RemoteExecutor.Invoke(async (useVersionString, testMethod) =>
            {
                const int RequestContentLength = 42;
                const int ResponseContentLength = 43;

                Version version = Version.Parse(useVersionString);
                using var listener = new TestEventListener("System.Net.Http", EventLevel.Verbose, eventCounterInterval: 0.1d);

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    await GetFactoryForVersion(version).CreateClientAndServerAsync(
                        async uri =>
                        {
                            using HttpClientHandler handler = CreateHttpClientHandler(useVersionString);
                            using HttpClient client = CreateHttpClient(handler, useVersionString);
                            using var invoker = new HttpMessageInvoker(handler);

                            var request = new HttpRequestMessage(HttpMethod.Get, uri)
                            {
                                Version = version
                            };

                            var content = new ByteArrayContent(Encoding.ASCII.GetBytes(new string('a', RequestContentLength)));
                            request.Content = content;

                            switch (testMethod)
                            {
                                case "PostAsync":
                                    await client.PostAsync(uri, content);
                                    break;

                                case "Send":
                                    await Task.Run(() => client.Send(request));
                                    break;

                                case "SendAsync":
                                    await client.SendAsync(request);
                                    break;

                                case "SendChunkedAsync":
                                    request.Headers.TransferEncodingChunked = true;
                                    await client.SendAsync(request);
                                    break;

                                case "InvokerSend":
                                    HttpResponseMessage syncResponse = await Task.Run(() => invoker.Send(request, cancellationToken: default));
                                    await syncResponse.Content.CopyToAsync(Stream.Null);
                                    break;

                                case "InvokerSendAsync":
                                    HttpResponseMessage asyncResponse = await invoker.SendAsync(request, cancellationToken: default);
                                    await asyncResponse.Content.CopyToAsync(Stream.Null);
                                    break;
                            }
                        },
                        async server =>
                        {
                            await server.AcceptConnectionAsync(async connection =>
                            {
                                await Task.Delay(300);
                                await connection.ReadRequestDataAsync();
                                await connection.SendResponseAsync(content: new string('a', ResponseContentLength));
                            });
                        });

                    await Task.Delay(300);
                });
                Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself

                EventWrittenEventArgs start = Assert.Single(events, e => e.EventName == "RequestStart");
                ValidateStartEventPayload(start);

                EventWrittenEventArgs stop = Assert.Single(events, e => e.EventName == "RequestStop");
                Assert.Empty(stop.Payload);

                Assert.DoesNotContain(events, e => e.EventName == "RequestFailed");

                ValidateConnectionEstablishedClosed(events, version);

                ValidateRequestResponseStartStopEvents(
                    events,
                    RequestContentLength,
                    responseContentLength: testMethod.StartsWith("InvokerSend") ? null : ResponseContentLength,
                    count: 1);

                VerifyEventCounters(events, requestCount: 1, shouldHaveFailures: false);
            }, UseVersion.ToString(), testMethod).Dispose();
        }

        private static void ValidateStartEventPayload(EventWrittenEventArgs startEvent)
        {
            Assert.Equal("RequestStart", startEvent.EventName);
            Assert.Equal(7, startEvent.Payload.Count);

            Assert.StartsWith("http", (string)startEvent.Payload[0]);
            Assert.NotEmpty((string)startEvent.Payload[1]); // host
            Assert.True(startEvent.Payload[2] is int port && port >= 0 && port <= 65535);
            Assert.NotEmpty((string)startEvent.Payload[3]); // pathAndQuery
            Assert.True(startEvent.Payload[4] is byte versionMajor && (versionMajor == 1 || versionMajor == 2));
            Assert.True(startEvent.Payload[5] is byte versionMinor && (versionMinor == 1 || versionMinor == 0));
            Assert.InRange((HttpVersionPolicy)startEvent.Payload[6], HttpVersionPolicy.RequestVersionOrLower, HttpVersionPolicy.RequestVersionExact);
        }

        private static void ValidateConnectionEstablishedClosed(ConcurrentQueue<EventWrittenEventArgs> events, Version version, int count = 1)
        {
            EventWrittenEventArgs[] connectionsEstablished = events.Where(e => e.EventName == "ConnectionEstablished").ToArray();
            Assert.Equal(count, connectionsEstablished.Length);
            foreach (EventWrittenEventArgs connectionEstablished in connectionsEstablished)
            {
                Assert.Equal(2, connectionEstablished.Payload.Count);
                Assert.Equal(version.Major, (byte)connectionEstablished.Payload[0]);
                Assert.Equal(version.Minor, (byte)connectionEstablished.Payload[1]);
            }

            EventWrittenEventArgs[] connectionsClosed = events.Where(e => e.EventName == "ConnectionClosed").ToArray();
            Assert.Equal(count, connectionsClosed.Length);
            foreach (EventWrittenEventArgs connectionClosed in connectionsClosed)
            {
                Assert.Equal(2, connectionClosed.Payload.Count);
                Assert.Equal(version.Major, (byte)connectionClosed.Payload[0]);
                Assert.Equal(version.Minor, (byte)connectionClosed.Payload[1]);
            }
        }

        private static void ValidateRequestResponseStartStopEvents(ConcurrentQueue<EventWrittenEventArgs> events, int? requestContentLength, int? responseContentLength, int count)
        {
            EventWrittenEventArgs[] requestHeadersStarts = events.Where(e => e.EventName == "RequestHeadersStart").ToArray();
            Assert.Equal(count, requestHeadersStarts.Length);
            Assert.All(requestHeadersStarts, r => Assert.Empty(r.Payload));

            EventWrittenEventArgs[] requestHeadersStops = events.Where(e => e.EventName == "RequestHeadersStop").ToArray();
            Assert.Equal(count, requestHeadersStops.Length);
            Assert.All(requestHeadersStops, r => Assert.Empty(r.Payload));

            EventWrittenEventArgs[] requestContentStarts = events.Where(e => e.EventName == "RequestContentStart").ToArray();
            Assert.Equal(requestContentLength.HasValue ? count : 0, requestContentStarts.Length);
            Assert.All(requestContentStarts, r => Assert.Empty(r.Payload));

            EventWrittenEventArgs[] requestContentStops = events.Where(e => e.EventName == "RequestContentStop").ToArray();
            Assert.Equal(requestContentLength.HasValue ? count : 0, requestContentStops.Length);
            foreach (EventWrittenEventArgs requestContentStop in requestContentStops)
            {
                object payload = Assert.Single(requestContentStop.Payload);
                Assert.True(payload is long);
                Assert.Equal(requestContentLength.Value, (long)payload);
            }

            EventWrittenEventArgs[] responseHeadersStarts = events.Where(e => e.EventName == "ResponseHeadersStart").ToArray();
            Assert.Equal(count, responseHeadersStarts.Length);
            Assert.All(responseHeadersStarts, r => Assert.Empty(r.Payload));

            EventWrittenEventArgs[] responseHeadersStops = events.Where(e => e.EventName == "ResponseHeadersStop").ToArray();
            Assert.Equal(count, responseHeadersStops.Length);
            Assert.All(responseHeadersStops, r => Assert.Empty(r.Payload));

            EventWrittenEventArgs[] responseContentStarts = events.Where(e => e.EventName == "ResponseContentStart").ToArray();
            Assert.Equal(responseContentLength.HasValue ? count : 0, responseContentStarts.Length);
            Assert.All(responseContentStarts, r => Assert.Empty(r.Payload));

            EventWrittenEventArgs[] responseContentStops = events.Where(e => e.EventName == "ResponseContentStop").ToArray();
            Assert.Equal(responseContentLength.HasValue ? count : 0, responseContentStops.Length);
            Assert.All(responseContentStops, r => Assert.Empty(r.Payload));
        }

        private static void VerifyEventCounters(ConcurrentQueue<EventWrittenEventArgs> events, int requestCount, bool shouldHaveFailures, int requestsLeftQueueVersion = -1)
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

            Assert.True(eventCounters.TryGetValue("requests-failed", out double[] requestsFailures));
            Assert.True(eventCounters.TryGetValue("requests-failed-rate", out double[] requestsFailureRate));
            if (shouldHaveFailures)
            {
                Assert.Equal(1, requestsFailures[^1]);
                Assert.Contains(requestsFailureRate, r => r > 0);
            }
            else
            {
                Assert.All(requestsFailures, a => Assert.Equal(0, a));
                Assert.All(requestsFailureRate, r => Assert.Equal(0, r));
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

            Assert.True(eventCounters.TryGetValue("http11-requests-queue-duration", out double[] http11requestQueueDurations));
            Assert.Equal(0, http11requestQueueDurations[^1]);
            if (requestsLeftQueueVersion == 1)
            {
                Assert.Contains(http11requestQueueDurations, d => d > 0);
                Assert.All(http11requestQueueDurations, d => Assert.True(d >= 0));
            }
            else
            {
                Assert.All(http11requestQueueDurations, d => Assert.True(d == 0));
            }

            Assert.True(eventCounters.TryGetValue("http20-requests-queue-duration", out double[] http20requestQueueDurations));
            Assert.Equal(0, http20requestQueueDurations[^1]);
            if (requestsLeftQueueVersion == 2)
            {
                Assert.Contains(http20requestQueueDurations, d => d > 0);
                Assert.All(http20requestQueueDurations, d => Assert.True(d >= 0));
            }
            else
            {
                Assert.All(http20requestQueueDurations, d => Assert.True(d == 0));
            }
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EventSource_ConnectionPoolAtMaxConnections_LogsRequestLeftQueue()
        {
            RemoteExecutor.Invoke(async useVersionString =>
            {
                Version version = Version.Parse(useVersionString);
                using var listener = new TestEventListener("System.Net.Http", EventLevel.Verbose, eventCounterInterval: 0.1d);

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    var firstRequestReceived = new SemaphoreSlim(0, 1);
                    var secondRequestSent = new SemaphoreSlim(0, 1);

                    await GetFactoryForVersion(version).CreateClientAndServerAsync(
                        async uri =>
                        {
                            using HttpClientHandler handler = CreateHttpClientHandler(useVersionString);
                            using HttpClient client = CreateHttpClient(handler, useVersionString);

                            var socketsHttpHandler = GetUnderlyingSocketsHttpHandler(handler) as SocketsHttpHandler;
                            socketsHttpHandler.MaxConnectionsPerServer = 1;

                            // Dummy request to ensure that the MaxConcurrentStreams setting has been acknowledged
                            await client.GetStringAsync(uri);

                            Task firstRequest = client.GetStringAsync(uri);
                            Assert.True(await firstRequestReceived.WaitAsync(TimeSpan.FromSeconds(10)));

                            // We are now at the connection limit, the next request will wait for the first one to complete
                            Task secondRequest = client.GetStringAsync(uri);
                            secondRequestSent.Release();

                            await new[] { firstRequest, secondRequest }.WhenAllOrAnyFailed();
                        },
                        async server =>
                        {
                            GenericLoopbackConnection connection;
                            if (server is Http2LoopbackServer http2Server)
                            {
                                http2Server.AllowMultipleConnections = true;
                                connection = await http2Server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 1 });
                            }
                            else
                            {
                                connection = await server.EstablishGenericConnectionAsync();
                            }

                            using (connection)
                            {
                                // Dummy request to ensure that the MaxConcurrentStreams setting has been acknowledged
                                await connection.ReadRequestDataAsync(readBody: false);
                                await connection.SendResponseAsync();

                                // First request
                                await connection.ReadRequestDataAsync(readBody: false);
                                firstRequestReceived.Release();
                                Assert.True(await secondRequestSent.WaitAsync(TimeSpan.FromSeconds(10)));
                                await Task.Delay(100);
                                await connection.SendResponseAsync();

                                // Second request
                                await connection.ReadRequestDataAsync(readBody: false);
                                await connection.SendResponseAsync();
                            };
                        });

                    await Task.Delay(300);
                });
                Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself

                EventWrittenEventArgs[] starts = events.Where(e => e.EventName == "RequestStart").ToArray();
                Assert.Equal(3, starts.Length);
                Assert.All(starts, s => ValidateStartEventPayload(s));

                EventWrittenEventArgs[] stops = events.Where(e => e.EventName == "RequestStop").ToArray();
                Assert.Equal(3, stops.Length);
                Assert.All(stops, s => Assert.Empty(s.Payload));

                Assert.DoesNotContain(events, e => e.EventName == "RequestFailed");

                ValidateConnectionEstablishedClosed(events, version);

                EventWrittenEventArgs requestLeftQueue = Assert.Single(events, e => e.EventName == "RequestLeftQueue");
                Assert.Equal(3, requestLeftQueue.Payload.Count);
                Assert.True((double)requestLeftQueue.Payload.Count > 0); // timeSpentOnQueue
                Assert.Equal(version.Major, (byte)requestLeftQueue.Payload[1]);
                Assert.Equal(version.Minor, (byte)requestLeftQueue.Payload[2]);

                ValidateRequestResponseStartStopEvents(events, requestContentLength: null, responseContentLength: 0, count: 3);

                VerifyEventCounters(events, requestCount: 3, shouldHaveFailures: false, requestsLeftQueueVersion: version.Major);
            }, UseVersion.ToString()).Dispose();
        }
    }

    public sealed class TelemetryTest_Http11 : TelemetryTest
    {
        public TelemetryTest_Http11(ITestOutputHelper output) : base(output) { }
    }

    public sealed class TelemetryTest_Http20 : TelemetryTest
    {
        protected override Version UseVersion => HttpVersion.Version20;

        public TelemetryTest_Http20(ITestOutputHelper output) : base(output) { }
    }
}
