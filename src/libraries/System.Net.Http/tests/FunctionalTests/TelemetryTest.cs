// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.Quic;
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
            yield return new object[] { "UnbufferedSendAsync" };
            yield return new object[] { "GetStringAsync" };
            yield return new object[] { "GetByteArrayAsync" };
            yield return new object[] { "GetStreamAsync" };
            yield return new object[] { "InvokerSendAsync" };

            yield return new object[] { "Send" };
            yield return new object[] { "UnbufferedSend" };
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

            RemoteExecutor.Invoke(static async (useVersionString, testMethod) =>
            {
                const int ResponseContentLength = 42;

                Version version = Version.Parse(useVersionString);
                using var listener = new TestEventListener("System.Net.Http", EventLevel.Verbose, eventCounterInterval: 0.1d);
                listener.AddActivityTracking();

                bool buffersResponse = false;
                var events = new ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)>();
                await listener.RunWithCallbackAsync(e => events.Enqueue((e, e.ActivityId)), async () =>
                {
                    await GetFactoryForVersion(version).CreateClientAndServerAsync(
                        async uri =>
                        {
                            using HttpClientHandler handler = CreateHttpClientHandler(version);
                            using HttpClient client = CreateHttpClient(handler, useVersionString);
                            using var invoker = new HttpMessageInvoker(handler);

                            var request = new HttpRequestMessage(HttpMethod.Get, uri)
                            {
                                Version = version
                            };

                            switch (testMethod)
                            {
                                case "GetAsync":
                                    {
                                        buffersResponse = true;
                                        await client.GetAsync(uri);
                                    }
                                    break;

                                case "Send":
                                    {
                                        buffersResponse = true;
                                        await Task.Run(() => client.Send(request));
                                    }
                                    break;

                                case "UnbufferedSend":
                                    {
                                        buffersResponse = false;
                                        using HttpResponseMessage response = await Task.Run(() => client.Send(request, HttpCompletionOption.ResponseHeadersRead));
                                        response.Content.CopyTo(Stream.Null, null, default);
                                    }
                                    break;

                                case "SendAsync":
                                    {
                                        buffersResponse = true;
                                        await client.SendAsync(request);
                                    }
                                    break;

                                case "UnbufferedSendAsync":
                                    {
                                        buffersResponse = false;
                                        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                                        await response.Content.CopyToAsync(Stream.Null);
                                    }
                                    break;

                                case "GetStringAsync":
                                    {
                                        buffersResponse = true;
                                        await client.GetStringAsync(uri);
                                    }
                                    break;

                                case "GetByteArrayAsync":
                                    {
                                        buffersResponse = true;
                                        await client.GetByteArrayAsync(uri);
                                    }
                                    break;

                                case "GetStreamAsync":
                                    {
                                        buffersResponse = false;
                                        using Stream responseStream = await client.GetStreamAsync(uri);
                                        await responseStream.CopyToAsync(Stream.Null);
                                    }
                                    break;

                                case "InvokerSend":
                                    {
                                        buffersResponse = false;
                                        using HttpResponseMessage response = await Task.Run(() => invoker.Send(request, cancellationToken: default));
                                        await response.Content.CopyToAsync(Stream.Null);
                                    }
                                    break;

                                case "InvokerSendAsync":
                                    {
                                        buffersResponse = false;
                                        using HttpResponseMessage response = await invoker.SendAsync(request, cancellationToken: default);
                                        await response.Content.CopyToAsync(Stream.Null);
                                    }
                                    break;
                            }
                        },
                        async server =>
                        {
                            await server.AcceptConnectionAsync(async connection =>
                            {
                                await connection.ReadRequestDataAsync();
                                await WaitForEventCountersAsync(events);
                                await connection.SendResponseAsync(content: new string('a', ResponseContentLength));
                            });
                        });

                    await WaitForEventCountersAsync(events);
                });
                Assert.DoesNotContain(events, e => e.Event.EventId == 0); // errors from the EventSource itself

                ValidateStartFailedStopEvents(events, version);

                ValidateConnectionEstablishedClosed(events, version);

                ValidateRequestResponseStartStopEvents(
                    events,
                    requestContentLength: null,
                    responseContentLength: buffersResponse ? ResponseContentLength : null,
                    count: 1);

                ValidateEventCounters(events, requestCount: 1, shouldHaveFailures: false, versionMajor: version.Major);
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

            RemoteExecutor.Invoke(static async (useVersionString, testMethod) =>
            {
                Version version = Version.Parse(useVersionString);
                using var listener = new TestEventListener("System.Net.Http", EventLevel.Verbose, eventCounterInterval: 0.1d);
                listener.AddActivityTracking();

                var events = new ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)>();
                await listener.RunWithCallbackAsync(e => events.Enqueue((e, e.ActivityId)), async () =>
                {
                    var semaphore = new SemaphoreSlim(0, 1);
                    var cts = new CancellationTokenSource();

                    await GetFactoryForVersion(version).CreateClientAndServerAsync(
                        async uri =>
                        {
                            using HttpClientHandler handler = CreateHttpClientHandler(version);
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

                                case "UnbufferedSend":
                                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await Task.Run(() => client.Send(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)));
                                    break;

                                case "SendAsync":
                                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await client.SendAsync(request, cts.Token));
                                    break;

                                case "UnbufferedSendAsync":
                                    await Assert.ThrowsAsync<TaskCanceledException>(async () => await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token));
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
                                await connection.ReadRequestDataAsync();
                                await WaitForEventCountersAsync(events);
                                cts.Cancel();
                                Assert.True(await semaphore.WaitAsync(TimeSpan.FromSeconds(30)));
                            });
                        });

                    await WaitForEventCountersAsync(events);
                });
                Assert.DoesNotContain(events, e => e.Event.EventId == 0); // errors from the EventSource itself

                ValidateStartFailedStopEvents(events, version, shouldHaveFailures: true);

                ValidateConnectionEstablishedClosed(events, version);

                ValidateEventCounters(events, requestCount: 1, shouldHaveFailures: true, versionMajor: version.Major);
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

            RemoteExecutor.Invoke(static async (useVersionString, testMethod) =>
            {
                const int RequestContentLength = 42;
                const int ResponseContentLength = 43;

                Version version = Version.Parse(useVersionString);
                using var listener = new TestEventListener("System.Net.Http", EventLevel.Verbose, eventCounterInterval: 0.1d);
                listener.AddActivityTracking();

                var events = new ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)>();
                await listener.RunWithCallbackAsync(e => events.Enqueue((e, e.ActivityId)), async () =>
                {
                    await GetFactoryForVersion(version).CreateClientAndServerAsync(
                        async uri =>
                        {
                            using HttpClientHandler handler = CreateHttpClientHandler(version);
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
                                await connection.ReadRequestDataAsync();
                                await WaitForEventCountersAsync(events);
                                await connection.SendResponseAsync(content: new string('a', ResponseContentLength));
                            });
                        });

                    await WaitForEventCountersAsync(events);
                });
                Assert.DoesNotContain(events, e => e.Event.EventId == 0); // errors from the EventSource itself

                ValidateStartFailedStopEvents(events, version);

                ValidateConnectionEstablishedClosed(events, version);

                ValidateRequestResponseStartStopEvents(
                    events,
                    RequestContentLength,
                    responseContentLength: testMethod.StartsWith("InvokerSend") ? null : ResponseContentLength,
                    count: 1);

                ValidateEventCounters(events, requestCount: 1, shouldHaveFailures: false, versionMajor: version.Major);
            }, UseVersion.ToString(), testMethod).Dispose();
        }

        private static void ValidateStartFailedStopEvents(ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)> events, Version version, bool shouldHaveFailures = false, int count = 1)
        {
            (EventWrittenEventArgs Event, Guid ActivityId)[] starts = events.Where(e => e.Event.EventName == "RequestStart").ToArray();
            foreach (EventWrittenEventArgs startEvent in starts.Select(e => e.Event))
            {
                Assert.Equal(7, startEvent.Payload.Count);
                Assert.StartsWith("http", (string)startEvent.Payload[0]);
                Assert.NotEmpty((string)startEvent.Payload[1]); // host
                Assert.True(startEvent.Payload[2] is int port && port >= 0 && port <= 65535);
                Assert.NotEmpty((string)startEvent.Payload[3]); // pathAndQuery
                byte versionMajor = Assert.IsType<byte>(startEvent.Payload[4]);
                Assert.Equal(version.Major, versionMajor);
                byte versionMinor = Assert.IsType<byte>(startEvent.Payload[5]);
                Assert.Equal(version.Minor, versionMinor);
                Assert.InRange((HttpVersionPolicy)startEvent.Payload[6], HttpVersionPolicy.RequestVersionOrLower, HttpVersionPolicy.RequestVersionExact);
            }
            Assert.Equal(count, starts.Length);

            (EventWrittenEventArgs Event, Guid ActivityId)[] stops = events.Where(e => e.Event.EventName == "RequestStop").ToArray();
            Assert.All(stops, stopEvent => Assert.Empty(stopEvent.Event.Payload));

            ValidateSameActivityIds(starts, stops);

            (EventWrittenEventArgs Event, Guid ActivityId)[] failures = events.Where(e => e.Event.EventName == "RequestFailed").ToArray();
            Assert.All(failures, failedEvent => Assert.Empty(failedEvent.Event.Payload));
            if (shouldHaveFailures)
            {
                ValidateSameActivityIds(starts, failures);
            }
            else
            {
                Assert.Empty(failures);
            }
        }

        private static void ValidateConnectionEstablishedClosed(ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)> events, Version version, int count = 1)
        {
            EventWrittenEventArgs[] connectionsEstablished = events.Select(e => e.Event).Where(e => e.EventName == "ConnectionEstablished").ToArray();
            Assert.Equal(count, connectionsEstablished.Length);
            foreach (EventWrittenEventArgs connectionEstablished in connectionsEstablished)
            {
                Assert.Equal(2, connectionEstablished.Payload.Count);
                Assert.Equal(version.Major, (byte)connectionEstablished.Payload[0]);
                Assert.Equal(version.Minor, (byte)connectionEstablished.Payload[1]);
            }

            EventWrittenEventArgs[] connectionsClosed = events.Select(e => e.Event).Where(e => e.EventName == "ConnectionClosed").ToArray();
            Assert.Equal(count, connectionsClosed.Length);
            foreach (EventWrittenEventArgs connectionClosed in connectionsClosed)
            {
                Assert.Equal(2, connectionClosed.Payload.Count);
                Assert.Equal(version.Major, (byte)connectionClosed.Payload[0]);
                Assert.Equal(version.Minor, (byte)connectionClosed.Payload[1]);
            }
        }

        private static void ValidateRequestResponseStartStopEvents(ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)> events, int? requestContentLength, int? responseContentLength, int count)
        {
            (EventWrittenEventArgs Event, Guid ActivityId)[] requestHeadersStarts = events.Where(e => e.Event.EventName == "RequestHeadersStart").ToArray();
            Assert.Equal(count, requestHeadersStarts.Length);
            Assert.All(requestHeadersStarts, r => Assert.Empty(r.Event.Payload));

            (EventWrittenEventArgs Event, Guid ActivityId)[] requestHeadersStops = events.Where(e => e.Event.EventName == "RequestHeadersStop").ToArray();
            Assert.Equal(count, requestHeadersStops.Length);
            Assert.All(requestHeadersStops, r => Assert.Empty(r.Event.Payload));

            ValidateSameActivityIds(requestHeadersStarts, requestHeadersStops);

            (EventWrittenEventArgs Event, Guid ActivityId)[] requestContentStarts = events.Where(e => e.Event.EventName == "RequestContentStart").ToArray();
            Assert.Equal(requestContentLength.HasValue ? count : 0, requestContentStarts.Length);
            Assert.All(requestContentStarts, r => Assert.Empty(r.Event.Payload));

            (EventWrittenEventArgs Event, Guid ActivityId)[] requestContentStops = events.Where(e => e.Event.EventName == "RequestContentStop").ToArray();
            Assert.Equal(requestContentLength.HasValue ? count : 0, requestContentStops.Length);
            foreach (EventWrittenEventArgs requestContentStop in requestContentStops.Select(e => e.Event))
            {
                object payload = Assert.Single(requestContentStop.Payload);
                Assert.True(payload is long);
                Assert.Equal(requestContentLength.Value, (long)payload);
            }

            ValidateSameActivityIds(requestContentStarts, requestContentStops);

            (EventWrittenEventArgs Event, Guid ActivityId)[] responseHeadersStarts = events.Where(e => e.Event.EventName == "ResponseHeadersStart").ToArray();
            Assert.Equal(count, responseHeadersStarts.Length);
            Assert.All(responseHeadersStarts, r => Assert.Empty(r.Event.Payload));

            (EventWrittenEventArgs Event, Guid ActivityId)[] responseHeadersStops = events.Where(e => e.Event.EventName == "ResponseHeadersStop").ToArray();
            Assert.Equal(count, responseHeadersStops.Length);
            Assert.All(responseHeadersStops, r => Assert.Empty(r.Event.Payload));

            ValidateSameActivityIds(responseHeadersStarts, responseHeadersStops);

            (EventWrittenEventArgs Event, Guid ActivityId)[] responseContentStarts = events.Where(e => e.Event.EventName == "ResponseContentStart").ToArray();
            Assert.Equal(responseContentLength.HasValue ? count : 0, responseContentStarts.Length);
            Assert.All(responseContentStarts, r => Assert.Empty(r.Event.Payload));

            (EventWrittenEventArgs Event, Guid ActivityId)[] responseContentStops = events.Where(e => e.Event.EventName == "ResponseContentStop").ToArray();
            Assert.Equal(responseContentLength.HasValue ? count : 0, responseContentStops.Length);
            Assert.All(responseContentStops, r => Assert.Empty(r.Event.Payload));

            ValidateSameActivityIds(responseContentStarts, responseContentStops);
        }

        private static void ValidateSameActivityIds((EventWrittenEventArgs Event, Guid ActivityId)[] a, (EventWrittenEventArgs Event, Guid ActivityId)[] b)
        {
            Assert.Equal(a.Length, b.Length);

            for (int i = 0; i < a.Length; i++)
            {
                Assert.NotEqual(Guid.Empty, a[i].ActivityId);
                Assert.Equal(a[i].ActivityId, b[i].ActivityId);
            }
        }

        private static void ValidateEventCounters(ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)> events, int requestCount, bool shouldHaveFailures, int versionMajor, bool requestLeftQueue = false)
        {
            Dictionary<string, double[]> eventCounters = events
                .Select(e => e.Event)
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

            Assert.True(eventCounters.TryGetValue("http30-connections-current-total", out double[] http30ConnectionsTotal));
            Assert.All(http30ConnectionsTotal, c => Assert.True(c >= 0));
            Assert.Equal(0, http30ConnectionsTotal[^1]);

            if (versionMajor == 1)
            {
                Assert.Contains(http11ConnectionsTotal, d => d > 0);
                Assert.DoesNotContain(http20ConnectionsTotal, d => d > 0);
                Assert.DoesNotContain(http30ConnectionsTotal, d => d > 0);
            }
            else if (versionMajor == 2)
            {
                Assert.DoesNotContain(http11ConnectionsTotal, d => d > 0);
                Assert.Contains(http20ConnectionsTotal, d => d > 0);
                Assert.DoesNotContain(http30ConnectionsTotal, d => d > 0);
            }
            else
            {
                Assert.DoesNotContain(http11ConnectionsTotal, d => d > 0);
                Assert.DoesNotContain(http20ConnectionsTotal, d => d > 0);
                Assert.Contains(http30ConnectionsTotal, d => d > 0);
            }

            Assert.True(eventCounters.TryGetValue("http11-requests-queue-duration", out double[] http11requestQueueDurations));
            Assert.All(http11requestQueueDurations, d => Assert.True(d >= 0));
            Assert.Equal(0, http11requestQueueDurations[^1]);

            Assert.True(eventCounters.TryGetValue("http20-requests-queue-duration", out double[] http20requestQueueDurations));
            Assert.All(http20requestQueueDurations, d => Assert.True(d >= 0));
            Assert.Equal(0, http20requestQueueDurations[^1]);

            Assert.True(eventCounters.TryGetValue("http30-requests-queue-duration", out double[] http30requestQueueDurations));
            Assert.All(http30requestQueueDurations, d => Assert.True(d >= 0));
            Assert.Equal(0, http30requestQueueDurations[^1]);

            if (requestLeftQueue)
            {
                if (versionMajor == 1)
                {
                    Assert.Contains(http11requestQueueDurations, d => d > 0);
                    Assert.DoesNotContain(http20requestQueueDurations, d => d > 0);
                    Assert.DoesNotContain(http30requestQueueDurations, d => d > 0);
                }
                else if (versionMajor == 2)
                {
                    Assert.DoesNotContain(http11requestQueueDurations, d => d > 0);
                    Assert.Contains(http20requestQueueDurations, d => d > 0);
                    Assert.DoesNotContain(http30requestQueueDurations, d => d > 0);
                }
                else
                {
                    Assert.DoesNotContain(http11requestQueueDurations, d => d > 0);
                    Assert.DoesNotContain(http20requestQueueDurations, d => d > 0);
                    Assert.Contains(http30requestQueueDurations, d => d > 0);
                }
            }
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EventSource_ConnectionPoolAtMaxConnections_LogsRequestLeftQueue()
        {
            RemoteExecutor.Invoke(static async (useVersionString) =>
            {
                Version version = Version.Parse(useVersionString);
                using var listener = new TestEventListener("System.Net.Http", EventLevel.Verbose, eventCounterInterval: 0.1d);
                listener.AddActivityTracking();

                var events = new ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)>();
                await listener.RunWithCallbackAsync(e => events.Enqueue((e, e.ActivityId)), async () =>
                {
                    var firstRequestReceived = new SemaphoreSlim(0, 1);
                    var secondRequestSent = new SemaphoreSlim(0, 1);
                    var firstRequestFinished = new SemaphoreSlim(0, 1);

                    await GetFactoryForVersion(version).CreateClientAndServerAsync(
                        async uri =>
                        {
                            using HttpClientHandler handler = CreateHttpClientHandler(version, allowAllCertificates: true);
                            using HttpClient client = CreateHttpClient(handler, useVersionString);

                            var socketsHttpHandler = GetUnderlyingSocketsHttpHandler(handler);
                            socketsHttpHandler.MaxConnectionsPerServer = 1;

                            // Dummy request to establish connection and ensure that the MaxConcurrentStreams setting has been acknowledged
                            await client.GetStringAsync(uri);

                            Task firstRequest = client.GetStringAsync(uri);
                            Assert.True(await firstRequestReceived.WaitAsync(TimeSpan.FromSeconds(10)));

                            // We are now at the connection limit, the next request will wait for the first one to complete
                            Task secondRequest = client.GetStringAsync(uri);
                            secondRequestSent.Release();

                            // We are asserting that ActivityIds between Start/Stop pairs match below
                            // We wait for the first request to finish to ensure that RequestStop events
                            // are logged in the same order as RequestStarts
                            await firstRequest;
                            firstRequestFinished.Release();

                            await secondRequest;
                        },
                        async server =>
                        {
                            GenericLoopbackConnection connection;

                            if (server is Http2LoopbackServer http2Server)
                            {
                                connection = await http2Server.EstablishConnectionAsync(new SettingsEntry { SettingId = SettingId.MaxConcurrentStreams, Value = 1 });
                            }
                            else
                            {
                                connection = await server.EstablishGenericConnectionAsync();
                            }

                            await using (connection)
                            {
                                // Dummy request to ensure that the MaxConcurrentStreams setting has been acknowledged
                                await connection.ReadRequestDataAsync(readBody: false);
                                await connection.SendResponseAsync();

                                // First request
                                await connection.ReadRequestDataAsync(readBody: false);
                                firstRequestReceived.Release();
                                Assert.True(await secondRequestSent.WaitAsync(TimeSpan.FromSeconds(10)));
                                await WaitForEventCountersAsync(events);
                                await connection.SendResponseAsync();

                                // Second request
                                Assert.True(await firstRequestFinished.WaitAsync(TimeSpan.FromSeconds(10)));
                                await connection.ReadRequestDataAsync(readBody: false);
                                await connection.SendResponseAsync();
                            };
                        }, options: new Http3Options { MaxInboundBidirectionalStreams = 1 });

                    await WaitForEventCountersAsync(events);
                });
                Assert.DoesNotContain(events, e => e.Event.EventId == 0); // errors from the EventSource itself

                ValidateStartFailedStopEvents(events, version, count: 3);

                ValidateConnectionEstablishedClosed(events, version);

                var requestLeftQueueEvents = events.Where(e => e.Event.EventName == "RequestLeftQueue");
                var (minCount, maxCount) = version.Major switch
                {
                    1 => (2, 2),
                    2 => (2, 3), // race condition: if a connection hits its stream limit, it will be removed from the list and re-added on a separate thread
                    3 => (3, 3),
                    _ => throw new ArgumentOutOfRangeException()
                };
                Assert.InRange(requestLeftQueueEvents.Count(), minCount, maxCount);

                foreach (var (e, _) in requestLeftQueueEvents)
                {
                    Assert.Equal(3, e.Payload.Count);
                    Assert.True((double)e.Payload[0] > 0); // timeSpentOnQueue
                    Assert.Equal(version.Major, (byte)e.Payload[1]);
                    Assert.Equal(version.Minor, (byte)e.Payload[2]);
                }

                Guid requestLeftQueueId = requestLeftQueueEvents.Last().ActivityId;
                Assert.Equal(requestLeftQueueId, events.Where(e => e.Event.EventName == "RequestStart").Last().ActivityId);

                ValidateRequestResponseStartStopEvents(events, requestContentLength: null, responseContentLength: 0, count: 3);

                ValidateEventCounters(events, requestCount: 3, shouldHaveFailures: false, versionMajor: version.Major, requestLeftQueue: true);
            }, UseVersion.ToString()).Dispose();
        }

        private static async Task WaitForEventCountersAsync(ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)> events)
        {
            DateTime startTime = DateTime.UtcNow;
            int startCount = events.Count;

            while (events.Skip(startCount).Count(e => IsRequestsStartedEventCounter(e.Event)) < 3)
            {
                if (DateTime.UtcNow.Subtract(startTime) > TimeSpan.FromSeconds(30))
                    throw new TimeoutException($"Timed out waiting for EventCounters");

                await Task.Delay(100);
            }

            static bool IsRequestsStartedEventCounter(EventWrittenEventArgs e)
            {
                if (e.EventName != "EventCounters")
                    return false;

                var dictionary = (IDictionary<string, object>)e.Payload.Single();

                return (string)dictionary["Name"] == "requests-started";
            }
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

    [Collection(nameof(DisableParallelization))]
    [ConditionalClass(typeof(HttpClientHandlerTestBase), nameof(IsQuicSupported))]
    public sealed class TelemetryTest_Http30 : TelemetryTest
    {
        protected override Version UseVersion => HttpVersion.Version30;
        public TelemetryTest_Http30(ITestOutputHelper output) : base(output) { }
    }
}
