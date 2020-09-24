// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class TelemetryTest
    {
        public readonly ITestOutputHelper _output;

        public TelemetryTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(Socket).Assembly.GetType("System.Net.Sockets.SocketsTelemetry", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("System.Net.Sockets", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("d5b2e7d4-b6ec-50ae-7cde-af89427ad21f"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }

        public static IEnumerable<object[]> SocketMethods_MemberData()
        {
            yield return new[] { "Sync" };
            yield return new[] { "Task" };
            yield return new[] { "Apm" };
            yield return new[] { "Eap" };
        }

        public static IEnumerable<object[]> SocketMethods_Matrix_MemberData()
        {
            return from m1 in SocketMethods_MemberData()
                   from m2 in SocketMethods_MemberData()
                   select new[] { m1[0], m2[0] };
        }

        public static IEnumerable<object[]> SocketMethods_WithBools_MemberData()
        {
            return from m1 in SocketMethods_MemberData()
                   from @bool in new[] { true, false }
                   select new[] { m1[0], @bool };
        }

        private static async Task<EndPoint> GetRemoteEndPointAsync(string useDnsEndPointString, int port)
        {
            const string Address = "microsoft.com";

            if (bool.Parse(useDnsEndPointString))
            {
                return new DnsEndPoint(Address, port);
            }
            else
            {
                IPAddress ip = (await Dns.GetHostAddressesAsync(Address))[0];
                return new IPEndPoint(ip, port);
            }
        }

        // RemoteExecutor only supports simple argument types such as strings
        // That's why we use this helper method instead of returning SocketHelperBases from MemberDatas directly
        private static SocketHelperBase GetHelperBase(string socketMethod)
        {
            return socketMethod switch
            {
                "Sync" => new SocketHelperArraySync(),
                "Task" => new SocketHelperTask(),
                "Apm" => new SocketHelperApm(),
                "Eap" => new SocketHelperEap(),
                _ => throw new ArgumentException(socketMethod)
            };
        }

        [OuterLoop]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(SocketMethods_Matrix_MemberData))]
        public void EventSource_SocketConnectsLoopback_LogsConnectAcceptStartStop(string connectMethod, string acceptMethod)
        {
            RemoteExecutor.Invoke(async (connectMethod, acceptMethod) =>
            {
                using var listener = new TestEventListener("System.Net.Sockets", EventLevel.Verbose, 0.1);
                listener.AddActivityTracking();

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    using var server = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    server.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    server.Listen();

                    using var client = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    Task acceptTask = GetHelperBase(acceptMethod).AcceptAsync(server);
                    await WaitForEventAsync(events, "AcceptStart");

                    await GetHelperBase(connectMethod).ConnectAsync(client, server.LocalEndPoint);
                    await acceptTask;

                    await WaitForEventAsync(events, "AcceptStop");
                    await WaitForEventAsync(events, "ConnectStop");

                    await WaitForEventCountersAsync(events);
                });
                Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself

                VerifyStartStopEvents(events, connect: true, expectedCount: 1);
                VerifyStartStopEvents(events, connect: false, expectedCount: 1);

                Assert.DoesNotContain(events, e => e.EventName == "ConnectFailed");
                Assert.DoesNotContain(events, e => e.EventName == "ConnectCanceled");
                Assert.DoesNotContain(events, e => e.EventName == "AcceptFailed");

                VerifyEventCounters(events, connectCount: 1);
            }, connectMethod, acceptMethod).Dispose();
        }

        [OuterLoop]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(SocketMethods_WithBools_MemberData))]
        public void EventSource_SocketConnectsRemote_LogsConnectStartStop(string connectMethod, bool useDnsEndPoint)
        {
            RemoteExecutor.Invoke(async (connectMethod, useDnsEndPointString) =>
            {
                using var listener = new TestEventListener("System.Net.Sockets", EventLevel.Verbose, 0.1);
                listener.AddActivityTracking();

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    using var client = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    SocketHelperBase socketHelper = GetHelperBase(connectMethod);

                    EndPoint endPoint = await GetRemoteEndPointAsync(useDnsEndPointString, port: 443);
                    await socketHelper.ConnectAsync(client, endPoint);

                    await WaitForEventAsync(events, "ConnectStop");

                    await WaitForEventCountersAsync(events);
                });
                Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself

                VerifyStartStopEvents(events, connect: true, expectedCount: 1);

                Assert.DoesNotContain(events, e => e.EventName == "ConnectFailed");
                Assert.DoesNotContain(events, e => e.EventName == "ConnectCanceled");

                VerifyEventCounters(events, connectCount: 1, connectOnly: true);
            }, connectMethod, useDnsEndPoint.ToString()).Dispose();
        }

        [OuterLoop]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(SocketMethods_WithBools_MemberData))]
        public void EventSource_SocketConnectFailure_LogsConnectFailed(string connectMethod, bool useDnsEndPoint)
        {
            RemoteExecutor.Invoke(async (connectMethod, useDnsEndPointString) =>
            {
                EndPoint endPoint = await GetRemoteEndPointAsync(useDnsEndPointString, port: 12345);

                using var listener = new TestEventListener("System.Net.Sockets", EventLevel.Verbose, 0.1);
                listener.AddActivityTracking();

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    using var client = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    SocketHelperBase socketHelper = GetHelperBase(connectMethod);

                    Exception ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
                    {
                        Task connectTask = socketHelper.ConnectAsync(client, endPoint);
                        await WaitForEventAsync(events, "ConnectStart");
                        Task disposeTask = Task.Run(() => client.Dispose());
                        await new[] { connectTask, disposeTask }.WhenAllOrAnyFailed();
                    });

                    if (ex is SocketException se)
                    {
                        Assert.NotEqual(SocketError.TimedOut, se.SocketErrorCode);
                    }

                    await WaitForEventAsync(events, "ConnectStop");

                    await WaitForEventCountersAsync(events);
                });

                VerifyConnectFailureEvents(events);
            }, connectMethod, useDnsEndPoint.ToString()).Dispose();
        }

        [OuterLoop]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(SocketMethods_MemberData))]
        public void EventSource_SocketAcceptFailure_LogsAcceptFailed(string acceptMethod)
        {
            RemoteExecutor.Invoke(async acceptMethod =>
            {
                using var listener = new TestEventListener("System.Net.Sockets", EventLevel.Verbose, 0.1);
                listener.AddActivityTracking();

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    using var server = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    server.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    server.Listen();

                    await Assert.ThrowsAnyAsync<Exception>(async () =>
                    {
                        Task acceptTask = GetHelperBase(acceptMethod).AcceptAsync(server);
                        await WaitForEventAsync(events, "AcceptStart");
                        Task disposeTask = Task.Run(() => server.Dispose());
                        await new[] { acceptTask, disposeTask }.WhenAllOrAnyFailed();
                    });

                    await WaitForEventAsync(events, "AcceptStop");

                    await WaitForEventCountersAsync(events);
                });
                Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself

                VerifyStartStopEvents(events, connect: false, expectedCount: 1);

                EventWrittenEventArgs failed = Assert.Single(events, e => e.EventName == "AcceptFailed");
                Assert.Equal(2, failed.Payload.Count);
                Assert.True(Enum.IsDefined((SocketError)failed.Payload[0]));
                Assert.IsType<string>(failed.Payload[1]);

                EventWrittenEventArgs start = Assert.Single(events, e => e.EventName == "AcceptStart");
                Assert.Equal(start.ActivityId, failed.ActivityId);

                VerifyEventCounters(events, connectCount: 0);
            }, acceptMethod).Dispose();
        }

        [OuterLoop]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("Task", true)]
        [InlineData("Task", false)]
        [InlineData("Eap", true)]
        [InlineData("Eap", false)]
        public void EventSource_ConnectAsyncCanceled_LogsConnectCanceled(string connectMethod, bool useDnsEndPoint)
        {
            RemoteExecutor.Invoke(async (connectMethod, useDnsEndPointString) =>
            {
                EndPoint endPoint = await GetRemoteEndPointAsync(useDnsEndPointString, port: 12345);

                using var listener = new TestEventListener("System.Net.Sockets", EventLevel.Verbose, 0.1);
                listener.AddActivityTracking();

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    using var client = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    await Assert.ThrowsAnyAsync<Exception>(async () =>
                    {
                        switch (connectMethod)
                        {
                            case "Task":
                                using (var cts = new CancellationTokenSource())
                                {
                                    ValueTask connectTask = client.ConnectAsync(endPoint, cts.Token);
                                    await WaitForEventAsync(events, "ConnectStart");
                                    cts.Cancel();
                                    await connectTask;
                                }
                                break;

                            case "Eap":
                                using (var saea = new SocketAsyncEventArgs())
                                {
                                    var tcs = new TaskCompletionSource();
                                    saea.RemoteEndPoint = endPoint;
                                    saea.Completed += (_, __) =>
                                    {
                                        Assert.NotEqual(SocketError.Success, saea.SocketError);
                                        tcs.SetException(new SocketException((int)saea.SocketError));
                                    };
                                    Assert.True(client.ConnectAsync(saea));
                                    await WaitForEventAsync(events, "ConnectStart");
                                    Socket.CancelConnectAsync(saea);
                                    await tcs.Task;
                                }
                                break;
                        }
                    });

                    await WaitForEventAsync(events, "ConnectStop");

                    await WaitForEventCountersAsync(events);
                });

                VerifyConnectFailureEvents(events);
            }, connectMethod, useDnsEndPoint.ToString()).Dispose();
        }

        private static void VerifyConnectFailureEvents(ConcurrentQueue<EventWrittenEventArgs> events)
        {
            Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself

            VerifyStartStopEvents(events, connect: true, expectedCount: 1);

            EventWrittenEventArgs canceled = Assert.Single(events, e => e.EventName == "ConnectCanceled");
            Assert.Empty(canceled.Payload);

            EventWrittenEventArgs failed = Assert.Single(events, e => e.EventName == "ConnectFailed");
            Assert.Equal(2, failed.Payload.Count);
            Assert.True(Enum.IsDefined((SocketError)failed.Payload[0]));
            Assert.IsType<string>(failed.Payload[1]);

            EventWrittenEventArgs start = Assert.Single(events, e => e.EventName == "ConnectStart");
            Assert.Equal(start.ActivityId, canceled.ActivityId);
            Assert.Equal(start.ActivityId, failed.ActivityId);

            VerifyEventCounters(events, connectCount: 0);
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EventSource_EventsRaisedAsExpected()
        {
            RemoteExecutor.Invoke(async () =>
            {
                using (var listener = new TestEventListener("System.Net.Sockets", EventLevel.Verbose, 0.1))
                {
                    listener.AddActivityTracking();

                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                    {
                        // Invoke several tests to execute code paths while tracing is enabled

                        await new SendReceiveSync(null).SendRecv_Stream_TCP(IPAddress.Loopback, false).ConfigureAwait(false);
                        await new SendReceiveSync(null).SendRecv_Stream_TCP(IPAddress.Loopback, true).ConfigureAwait(false);

                        await new SendReceiveTask(null).SendRecv_Stream_TCP(IPAddress.Loopback, false).ConfigureAwait(false);
                        await new SendReceiveTask(null).SendRecv_Stream_TCP(IPAddress.Loopback, true).ConfigureAwait(false);

                        await new SendReceiveEap(null).SendRecv_Stream_TCP(IPAddress.Loopback, false).ConfigureAwait(false);
                        await new SendReceiveEap(null).SendRecv_Stream_TCP(IPAddress.Loopback, true).ConfigureAwait(false);

                        await new SendReceiveApm(null).SendRecv_Stream_TCP(IPAddress.Loopback, false).ConfigureAwait(false);
                        await new SendReceiveApm(null).SendRecv_Stream_TCP(IPAddress.Loopback, true).ConfigureAwait(false);

                        await new SendReceiveUdpClient().SendToRecvFromAsync_Datagram_UDP_UdpClient(IPAddress.Loopback).ConfigureAwait(false);
                        await new SendReceiveUdpClient().SendToRecvFromAsync_Datagram_UDP_UdpClient(IPAddress.Loopback).ConfigureAwait(false);

                        await new NetworkStreamTest().CopyToAsync_AllDataCopied(4096, true).ConfigureAwait(false);
                        await new NetworkStreamTest().Timeout_ValidData_Roundtrips().ConfigureAwait(false);

                        await WaitForEventCountersAsync(events);
                    });
                    Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself

                    VerifyStartStopEvents(events, connect: true, expectedCount: 10);

                    Assert.DoesNotContain(events, e => e.EventName == "ConnectFailed");
                    Assert.DoesNotContain(events, e => e.EventName == "ConnectCanceled");

                    VerifyEventCounters(events, connectCount: 10, shouldHaveTransferedBytes: true, shouldHaveDatagrams: true);
                }
            }).Dispose();
        }

        private static void VerifyStartStopEvents(ConcurrentQueue<EventWrittenEventArgs> events, bool connect, int expectedCount)
        {
            string startName = connect ? "ConnectStart" : "AcceptStart";
            EventWrittenEventArgs[] starts = events.Where(e => e.EventName == startName).ToArray();
            Assert.Equal(expectedCount, starts.Length);
            foreach (EventWrittenEventArgs start in starts)
            {
                object startPayload = Assert.Single(start.Payload);
                Assert.False(string.IsNullOrWhiteSpace(startPayload as string));
            }

            string stopName = connect ? "ConnectStop" : "AcceptStop";
            EventWrittenEventArgs[] stops = events.Where(e => e.EventName == stopName).ToArray();
            Assert.Equal(expectedCount, stops.Length);
            Assert.All(stops, stop => Assert.Empty(stop.Payload));

            for (int i = 0; i < expectedCount; i++)
            {
                Assert.NotEqual(Guid.Empty, starts[i].ActivityId);
                Assert.Equal(starts[i].ActivityId, stops[i].ActivityId);
            }
        }

        private static async Task WaitForEventAsync(ConcurrentQueue<EventWrittenEventArgs> events, string name)
        {
            DateTime startTime = DateTime.UtcNow;
            while (!events.Any(e => e.EventName == name))
            {
                if (DateTime.UtcNow.Subtract(startTime) > TimeSpan.FromSeconds(30))
                    throw new TimeoutException($"Timed out waiting for {name}");

                await Task.Delay(100);
            }
        }

        private static async Task WaitForEventCountersAsync(ConcurrentQueue<EventWrittenEventArgs> events)
        {
            DateTime startTime = DateTime.UtcNow;
            int startCount = events.Count;

            while (events.Skip(startCount).Count(IsBytesSentEventCounter) < 2)
            {
                if (DateTime.UtcNow.Subtract(startTime) > TimeSpan.FromSeconds(30))
                    throw new TimeoutException($"Timed out waiting for EventCounters");

                await Task.Delay(100);
            }

            static bool IsBytesSentEventCounter(EventWrittenEventArgs e)
            {
                if (e.EventName != "EventCounters")
                    return false;

                var dictionary = (IDictionary<string, object>)e.Payload.Single();

                return (string)dictionary["Name"] == "bytes-sent";
            }
        }

        private static void VerifyEventCounters(ConcurrentQueue<EventWrittenEventArgs> events, int connectCount, bool connectOnly = false, bool shouldHaveTransferedBytes = false, bool shouldHaveDatagrams = false)
        {
            Dictionary<string, double[]> eventCounters = events
                .Where(e => e.EventName == "EventCounters")
                .Select(e => (IDictionary<string, object>)e.Payload.Single())
                .GroupBy(d => (string)d["Name"], d => (double)(d.ContainsKey("Mean") ? d["Mean"] : d["Increment"]))
                .ToDictionary(p => p.Key, p => p.ToArray());

            Assert.True(eventCounters.TryGetValue("outgoing-connections-established", out double[] outgoingConnections));
            Assert.Equal(connectCount, outgoingConnections[^1]);

            Assert.True(eventCounters.TryGetValue("incoming-connections-established", out double[] incomingConnections));
            Assert.Equal(connectOnly ? 0 : connectCount, incomingConnections[^1]);

            Assert.True(eventCounters.TryGetValue("bytes-received", out double[] bytesReceived));
            if (shouldHaveTransferedBytes)
            {
                Assert.True(bytesReceived[^1] > 0);
            }

            Assert.True(eventCounters.TryGetValue("bytes-sent", out double[] bytesSent));
            if (shouldHaveTransferedBytes)
            {
                Assert.True(bytesSent[^1] > 0);
            }

            Assert.True(eventCounters.TryGetValue("datagrams-received", out double[] datagramsReceived));
            if (shouldHaveDatagrams)
            {
                Assert.True(datagramsReceived[^1] > 0);
            }

            Assert.True(eventCounters.TryGetValue("datagrams-sent", out double[] datagramsSent));
            if (shouldHaveDatagrams)
            {
                Assert.True(datagramsSent[^1] > 0);
            }
        }
    }
}
